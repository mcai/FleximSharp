/*
 * Mem.cs
 * 
 * Copyright © 2010 Min Cai (itecgo@163.com). 
 * 
 * This file is part of the Flexim# multicore architectural simulator.
 * 
 * Flexim is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Flexim is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Flexim#.  If not, see <http ://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.Pipelines;

namespace MinCai.Simulators.Flexim.MemoryHierarchy
{
	public static class MemoryConstants
	{
		public static uint MEM_LOGPAGESIZE = 12;
		public static uint MEM_PAGESHIFT = MEM_LOGPAGESIZE;
		public static uint MEM_PAGESIZE = (uint)(1 << (int)MEM_LOGPAGESIZE);
		public static uint MEM_PAGEMASK = MEM_PAGESIZE - 1;
		public static uint MEM_PAGE_COUNT = 1024;

		public static uint MEM_BLOCK_SIZE = 64;

		public static uint MEM_PROT_READ = 0x01;
		public static uint MEM_PROT_WRITE = 0x02;
	}

	public enum MemoryAccessType : uint
	{
		NONE = 0x00,
		READ = 0x01,
		WRITE = 0x02,
		EXEC = 0x04,
		INIT = 0x08
	}

	public class MemoryPage
	{
		public MemoryPage ()
		{
			this.Tag = 0;
			this.perm = MemoryAccessType.NONE;
			this.Data = new byte[MemoryConstants.MEM_PAGESIZE];
			this.Next = null;
		}

		public uint Tag { get; set; }
		public MemoryAccessType perm { get; set; }
		public byte[] Data { get; set; }

		public MemoryPage Next { get; set; }
	}

	public class SegmentationFaultException : Exception
	{
		public SegmentationFaultException (uint addr) : base(string.Format ("SegmentationFaultException @ 0x{0:x8}", addr))
		{
			this.addr = addr;
		}

		public uint addr;
	}

	public class Memory
	{
		public Memory ()
		{
			this.safe = true;
			
			this.pages = new Dictionary<uint, MemoryPage> ();
		}

		unsafe public void InitByte (uint addr, byte data)
		{
			this.Access (addr, 1, &data, MemoryAccessType.INIT);
		}

		unsafe public void InitHalfWord (uint addr, ushort data)
		{
			this.Access (addr, 2, (byte*)&data, MemoryAccessType.INIT);
		}

		unsafe public void InitWord (uint addr, uint data)
		{
			this.Access (addr, 4, (byte*)&data, MemoryAccessType.INIT);
		}

		unsafe public void InitDoubleWord (uint addr, ulong data)
		{
			this.Access (addr, 8, (byte*)&data, MemoryAccessType.INIT);
		}

		unsafe public void InitString (uint addr, char* str)
		{
			this.Access (addr, (int)(PtrUtils.Strlen (str) + 1), (byte*)str, MemoryAccessType.INIT);
		}

		unsafe public void InitBlock (uint addr, uint size, byte* p)
		{
			for (uint i = 0; i < size; i++) {
				this.InitByte (addr + i, *(p + i));
			}
		}

		unsafe public void WriteByte (uint addr, byte data)
		{
			this.Access (addr, 1, &data, MemoryAccessType.WRITE);
		}

		unsafe public void WriteHalfWord (uint addr, ushort data)
		{
			this.Access (addr, 2, (byte*)&data, MemoryAccessType.WRITE);
		}

		unsafe public void WriteWord (uint addr, uint data)
		{
			this.Access (addr, 4, (byte*)&data, MemoryAccessType.WRITE);
		}

		unsafe public void WriteDoubleWord (uint addr, ulong data)
		{
			this.Access (addr, 8, (byte*)&data, MemoryAccessType.WRITE);
		}

		unsafe public void WriteString (uint addr, char* str)
		{
			this.Access (addr, (int)(PtrUtils.Strlen (str) + 1), (byte*)str, MemoryAccessType.WRITE);
		}

		unsafe public void WriteBlock (uint addr, uint size, byte* data)
		{
			this.Access (addr, (int)size, data, MemoryAccessType.WRITE);
		}

		unsafe public void ReadByte (uint addr, byte* data)
		{
			this.Access (addr, 1, data, MemoryAccessType.READ);
		}

		unsafe public void ReadHalfWord (uint addr, ushort* data)
		{
			this.Access (addr, 2, (byte*)data, MemoryAccessType.READ);
		}

		unsafe public void ReadWord (uint addr, uint* data)
		{
			this.Access (addr, 4, (byte*)data, MemoryAccessType.READ);
		}

		unsafe public void ReadDoubleWord (uint addr, ulong* data)
		{
			this.Access (addr, 8, (byte*)data, MemoryAccessType.READ);
		}

		unsafe public int ReadString (uint addr, int size, char* str)
		{
			int i;
			for (i = 0; i < size; i++) {
				this.Access ((uint)(addr + i), 1, (byte*)(str + i), MemoryAccessType.READ);
				if (str[i] == 0)
					break;
			}
			return i;
		}

		unsafe public void ReadBlock (uint addr, int size, byte* p)
		{
			this.Access (addr, size, p, MemoryAccessType.READ);
		}

		unsafe public void Zero (uint addr, int size)
		{
			byte zero = 0;
			while (size-- > 0) {
				this.Access (addr++, 0, &zero, MemoryAccessType.WRITE);
			}
		}

		public uint GetTag (uint addr)
		{
			return addr & ~(MemoryConstants.MEM_PAGESIZE - 1);
		}

		public uint GetOffset (uint addr)
		{
			return addr & (MemoryConstants.MEM_PAGESIZE - 1);
		}

		public uint GetIndex (uint addr)
		{
			return (addr >> (int)MemoryConstants.MEM_LOGPAGESIZE) % MemoryConstants.MEM_PAGE_COUNT;
		}

		public bool IsAligned (uint addr)
		{
			return this.GetOffset (addr) == 0;
		}

		public MemoryPage GetPage (uint addr)
		{
			uint tag = this.GetTag (addr);
			uint index = this.GetIndex (addr);
			MemoryPage page = this[index];
			MemoryPage prev = null;
			
			while (page != null && page.Tag != tag) {
				prev = page;
				page = page.Next;
			}
			
			if (prev != null && page != null) {
				prev.Next = page.Next;
				page.Next = this[index];
				this[index] = page;
			}
			
			return page;
		}

		public MemoryPage AddPage (uint addr, MemoryAccessType perm)
		{
			uint tag = GetTag (addr);
			uint index = GetIndex (addr);
			
			MemoryPage page = new MemoryPage ();
			page.Tag = tag;
			page.perm = perm;
			
			page.Next = this[index];
			this[index] = page;
			this.mappedSpace += MemoryConstants.MEM_PAGESIZE;
			this.maxMappedSpace = Math.Max (this.maxMappedSpace, this.mappedSpace);
			
			return page;
		}

		public void RemovePage (uint addr)
		{
			uint tag = this.GetTag (addr);
			uint index = this.GetIndex (addr);
			MemoryPage prev = null;
			
			MemoryPage page = this[index];
			while (page != null && page.Tag != tag) {
				prev = page;
				page = page.Next;
			}
			
			if (page == null) {
				return;
			}
			
			if (prev != null) {
				prev.Next = page.Next;
			} else {
				this[index] = page.Next;
			}
			
			this.mappedSpace -= MemoryConstants.MEM_PAGESIZE;
			
			page = null;
		}

		unsafe public void Copy (uint dest, uint src, int size)
		{
			Debug.Assert (IsAligned (dest));
			Debug.Assert (IsAligned (src));
			Debug.Assert (IsAligned ((uint)size));
			if ((src < dest && src + size > dest) || (dest < src && dest + size > src))
				Logger.Fatal (LogCategory.MEMORY, "mem_copy: cannot copy overlapping regions");
			
			while (size > 0) {
				MemoryPage pageDest = this.GetPage (dest);
				MemoryPage pageSrc = this.GetPage (src);
				Debug.Assert (pageSrc != null && pageDest != null);
				Array.Copy (pageSrc.Data, pageDest.Data, MemoryConstants.MEM_PAGESIZE);
				src += MemoryConstants.MEM_PAGESIZE;
				dest += MemoryConstants.MEM_PAGESIZE;
				size -= (int)MemoryConstants.MEM_PAGESIZE;
			}
		}

		unsafe public void AccessPageBoundary (uint addr, int size, byte* buf, MemoryAccessType access)
		{
			MemoryPage page = this.GetPage (addr);
			
			if (page == null && !this.safe) {
				switch (access) {
				case MemoryAccessType.READ:
				case MemoryAccessType.EXEC:
					Logger.Warnf(LogCategory.MEMORY, "Memory.accessPageBoundary: unsafe reading 0x{0:x8}", addr);
					PtrUtils.memset (buf, 0, size);
					return;
				
				case MemoryAccessType.WRITE:
				case MemoryAccessType.INIT:
					Logger.Warnf(LogCategory.MEMORY, "Memory.accessPageBoundary: unsafe writing 0x{0:x8}", addr);
					page = AddPage (addr, MemoryAccessType.READ | MemoryAccessType.WRITE | MemoryAccessType.EXEC | MemoryAccessType.INIT);
					break;
				default:
					Logger.Panic(LogCategory.MEMORY, "Memory.accessPageBoundary: unknown access");
					break;
				}
			}
			
			if (this.safe) {
				if (page == null) {
					throw new SegmentationFaultException (addr);
				}
				if ((page.perm & access) != access) {
					Logger.Fatalf(LogCategory.MEMORY, "Memory.accessPageBoundary: permission denied at 0x{0:x8}, page.perm: 0x{1:x8}, access: 0x{2:x8}", addr, page.perm, access);
				}
			}
			
			uint offset = this.GetOffset (addr);
			Debug.Assert (offset + size <= MemoryConstants.MEM_PAGESIZE);
			
			fixed (byte* data = &page.Data[offset]) {
				switch (access) {
				case MemoryAccessType.READ:
				case MemoryAccessType.EXEC:
					PtrUtils.memcpy (buf, data, size);
					break;
				case MemoryAccessType.WRITE:
				case MemoryAccessType.INIT:
					PtrUtils.memcpy (data, buf, size);
					break;
				default:
					Logger.Panic(LogCategory.MEMORY, "Memory.accessPageBoundary: unknown access");
					break;
				}
			}
		}

		unsafe public void Access (uint addr, int size, byte* buf, MemoryAccessType access)
		{
			while (size > 0) {
				uint offset = this.GetOffset (addr);
				int chunksize = Math.Min (size, (int)(MemoryConstants.MEM_PAGESIZE - offset));
				this.AccessPageBoundary (addr, chunksize, buf, access);
				
				size -= chunksize;
				buf += chunksize;
				addr += (uint)chunksize;
			}
		}

		public uint MapSpace (uint addr, int size)
		{
			Debug.Assert (IsAligned (addr));
			Debug.Assert (IsAligned ((uint)size));
			
			uint tagStart = addr;
			uint tagEnd = addr;
			
			for (;;) {
				if (tagEnd == 0) {
					return uint.MaxValue;
				}
				
				if (this.GetPage (tagEnd) != null) {
					tagEnd += MemoryConstants.MEM_PAGESIZE;
					tagStart = tagEnd;
					continue;
				}
				
				if (tagEnd - tagStart + MemoryConstants.MEM_PAGESIZE == size)
					break;
				
				Debug.Assert (tagEnd - tagStart + MemoryConstants.MEM_PAGESIZE < size);
				
				tagEnd += MemoryConstants.MEM_PAGESIZE;
			}
			
			return tagStart;
		}

		public uint MapSpaceDown (uint addr, int size)
		{
			Debug.Assert (IsAligned (addr));
			Debug.Assert (IsAligned ((uint)size));
			
			uint tagStart = addr;
			uint tagEnd = addr;
			
			for (;;) {
				if (tagStart == 0) {
					return uint.MaxValue;
				}
				
				if (this.GetPage (tagStart) != null) {
					tagStart += MemoryConstants.MEM_PAGESIZE;
					tagEnd = tagStart;
					continue;
				}
				
				if (tagEnd - tagStart + MemoryConstants.MEM_PAGESIZE == size)
					break;
				
				Debug.Assert (tagEnd - tagStart + MemoryConstants.MEM_PAGESIZE < size);
				
				tagEnd -= MemoryConstants.MEM_PAGESIZE;
			}
			
			return tagStart;
		}

		public void Map (uint addr, int size, MemoryAccessType perm)
		{
//			Logger.Infof(LogCategory.MEMORY, "Memory.Map(), addr: 0x{0:x8} ~ 0x{1:x8}, size: {2:d}, perm: 0x{3:x8}", addr, addr + size, size, perm);
			
			for (uint tag = this.GetTag (addr); tag <= this.GetTag ((uint)(addr + size - 1)); tag += MemoryConstants.MEM_PAGESIZE) {
				MemoryPage page = this.GetPage (tag);
				if (page == null) {
					page = this.AddPage (tag, perm);
					page.perm |= perm;
				}
			}
		}

		public void Unmap (uint addr, int size)
		{
			Debug.Assert (IsAligned (addr));
			Debug.Assert (IsAligned ((uint)size));
			
			for (uint tag = GetTag (addr); tag <= GetTag ((uint)(addr + size - 1)); tag += MemoryConstants.MEM_PAGESIZE) {
				if (this.GetPage (tag) != null) {
					this.RemovePage (tag);
				}
			}
		}

		public void Protect (uint addr, int size, MemoryAccessType perm)
		{
			Debug.Assert (IsAligned (addr));
			Debug.Assert (IsAligned ((uint)size));
			
			for (uint tag = GetTag (addr); tag <= GetTag ((uint)(addr + size - 1)); tag += MemoryConstants.MEM_PAGESIZE) {
				MemoryPage page = this.GetPage (tag);
				if (page != null) {
					page.perm = perm;
				}
			}
		}

		private MemoryPage this[uint index] {
			get {
				if (this.pages.ContainsKey (index)) {
					return this.pages[index];
				}
				
				return null;
			}
			set { this.pages[index] = value; }
		}

		private Dictionary<uint, MemoryPage> pages;

		private ulong mappedSpace = 0;
		private ulong maxMappedSpace = 0;

		private bool safe = true;
	}

	public class CAM<T, K, V>
	{
		public CAM ()
		{
		}

		public K Tag { get; set; }
		public V Content { get; set; }
		public T Next { get; set; }
	}

	public class MMUPage : CAM<MMUPage, uint, uint>
	{
		public MMUPage ()
		{
		}

		public uint VirtualAddress {
			get { return this.Tag; }
			set { this.Tag = value; }
		}

		public uint PhysicalAddress {
			get { return this.Content; }
			set { this.Content = value; }
		}

		public Dir Dir { get; set; }
	}

	public class MMU
	{
		public MMU()
		{
			this.Pages = new Dictionary<uint, MMUPage>();
		}
		
		public MMUPage getPage (uint vtladdr)
		{
			uint idx = Page (vtladdr);
			uint tag = Tag (vtladdr);
			
			MMUPage page = this[idx];
			
			while (page != null) {
				if (page.VirtualAddress == tag)
					break;
				page = page.Next;
			}
			
			if (page == null) {
				page = new MMUPage ();
				page.Dir = new Dir (MemoryConstants.MEM_PAGESIZE / MemoryConstants.MEM_BLOCK_SIZE, 1);
				
				page.VirtualAddress = tag;
				page.PhysicalAddress = this.PageCount << (int)MemoryConstants.MEM_LOGPAGESIZE;
				
				page.Next = this[idx];
				this[idx] = page;
				
				this.PageCount++;
			}
			
			return page;
		}

		public uint Translate (uint vtladdr)
		{
			MMUPage page = this.getPage (vtladdr);
			return page.PhysicalAddress | Offset (vtladdr);
		}

		public Dir getDir (uint phaddr)
		{
			uint idx = Page (phaddr);
			if (idx >= this.PageCount) {
				return null;
			}
			
			return this[idx].Dir;
		}

		public bool validPhysicalAddress (uint phaddr)
		{
			return Page (phaddr) < this.PageCount;
		}

		public MMUPage this[uint index] {
			get {
				if (this.Pages.ContainsKey (index)) {
					return this.Pages[index];
				}
				
				return null;
			}
			set { this.Pages[index] = value; }
		}

		public Dictionary<uint, MMUPage> Pages { get; private set; }
		public uint PageCount { get; set; }

		public static uint Page (uint virtualAddress)
		{
			return virtualAddress >> (int)MemoryConstants.MEM_LOGPAGESIZE;
		}

		public static uint Tag (uint virtualAddress)
		{
			return virtualAddress & ~MemoryConstants.MEM_PAGEMASK;
		}

		public static uint Offset (uint virtualAddress)
		{
			return virtualAddress & MemoryConstants.MEM_PAGEMASK;
		}
	}

	public class DirEntry
	{
		public DirEntry (uint x, uint y)
		{
			this.X = x;
			this.Y = y;
			
			this.Sharers = new List<CoherentCacheNode> ();
		}

		public void SetSharer (CoherentCacheNode node)
		{
			Debug.Assert (node != null);
			
			if (!this.Sharers.Contains (node)) {
				this.Sharers.Add (node);
			}
		}

		public void UnsetSharer (CoherentCacheNode node)
		{
			Debug.Assert (node != null);
			
			if (this.Sharers.Contains (node)) {
				this.Sharers.Remove (node);
			}
		}

		public bool IsSharer (CoherentCacheNode node)
		{
			return this.Sharers.Contains (node);
		}
		
		public override string ToString ()
		{
			return string.Format ("[DirEntry: X={0}, Y={1}, Owner={2}, Sharers.Count={3}]", this.X, this.Y, this.Owner, this.Sharers.Count);
		}

		public bool IsShared {
			get { return this.Sharers.Count > 0; }
		}

		public bool IsOwned {
			get { return this.Owner != null; }
		}

		public bool IsSharedOrOwned {
			get { return this.IsShared || this.IsOwned; }
		}

		public uint X { get; set; }
		public uint Y { get; set; }

		public CoherentCacheNode Owner { get; set; }
		public List<CoherentCacheNode> Sharers { get; set; }
	}

	public class DirLock
	{
		public DirLock (uint x)
		{
			this.X = x;
		}

		public bool Lock ()
		{
			if (this.Locked) {
				return false;
			} else {
				this.Locked = true;
				return true;
			}
		}

		public void Unlock ()
		{
			this.Locked = false;
		}
		
		public override string ToString ()
		{
			return string.Format ("[DirLock: X={0}, Locked={1}]", this.X, this.Locked);
		}

		public uint X { get; set; }
		public bool Locked { get; set; }
	}

	public class Dir
	{
		public Dir (uint xSize, uint ySize)
		{
			this.XSize = xSize;
			this.YSize = ySize;
			
			this.DirEntries = new List<List<DirEntry>> ();
			for (uint i = 0; i < this.XSize; i++) {
				List<DirEntry> dirEntries = new List<DirEntry> ();
				this.DirEntries.Add (dirEntries);
				
				for (uint j = 0; j < this.YSize; j++) {
					dirEntries.Add (new DirEntry (i, j));
				}
			}
			
			this.DirLocks = new List<DirLock> ();
			for (uint i = 0; i < this.XSize; i++) {
				this.DirLocks.Add (new DirLock (i));
			}
		}

		public bool IsSharedOrOwned (int x, int y)
		{
			return this.DirEntries[x][y].IsSharedOrOwned;
		}

		public uint XSize { get; set; }
		public uint YSize { get; set; }

		public List<List<DirEntry>> DirEntries { get; set; }
		public List<DirLock> DirLocks { get; set; }
	}

	public enum MESIState
	{
		MODIFIED,
		EXCLUSIVE,
		SHARED,
		INVALID
	}

	public static class MESIUtils
	{
		public static bool IsReadHit (MESIState state)
		{
			return state != MESIState.INVALID;
		}

		public static bool IsWriteHit (MESIState state)
		{
			return state == MESIState.MODIFIED || state == MESIState.EXCLUSIVE;
		}
	}

	public enum CacheReplacementPolicy
	{
		LRU,
		FIFO,
		Random
	}

	public class CacheBlock
	{
		public CacheBlock (CacheSet st, uint way)
		{
			this.Set = st;
			this.Way = way;
			
			this.Tag = 0;
			this.TransientTag = 0;
			this.State = MESIState.INVALID;
			
			this.LastAccess = 0;
		}
		
		public override string ToString ()
		{
			return string.Format ("[CacheBlock: Set={0}, Way={1}, Tag={2}, TransientTag={3}, State={4}, LastAccess={5}]", this.Set, this.Way, this.Tag, this.TransientTag, this.State, this.LastAccess);
		}

		public CacheSet Set { get; set; }
		public uint Way { get; set; }

		public uint Tag { get; set; }
		public uint TransientTag { get; set; }
		public MESIState State { get; set; }

		public ulong LastAccess { get; set; }
	}

	public class CacheSet
	{
		public CacheSet (Cache cache, uint assoc, uint num)
		{
			this.Cache = cache;
			this.Assoc = assoc;
			this.Num = num;
			
			this.Blks = new List<CacheBlock> ();
			for (uint i = 0; i < this.Assoc; i++) {
				this.Blks.Add (new CacheBlock (this, i));
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[CacheSet: Assoc={0}, Cache={1}, Num={2}]", this.Assoc, this.Cache, this.Num);
		}

		public CacheBlock this[uint index] {
			get { return this.Blks[(int)index]; }
		}

		public uint Assoc { get; set; }
		public List<CacheBlock> Blks { get; set; }

		public Cache Cache { get; set; }

		public uint Num { get; set; }
	}

	public class Cache
	{
		public Cache (CacheConfig cacheConfig)
		{
			this.CacheConfig = cacheConfig;
			
			this.Sets = new List<CacheSet> ();
			for (uint i = 0; i < this.NumSets; i++) {
				this.Sets.Add (new CacheSet (this, this.Assoc, i));
			}
			
			this.Dir = new Dir (this.NumSets, this.Assoc);
		}

		public CacheBlock BlockOf (uint addr, bool checkTransientTag)
		{
			uint tag = this.Tag (addr);
			uint @set = this.Set (addr);
			
			for (uint way = 0; way < this[@set].Assoc; way++) {
				CacheBlock blk = this[@set][way];
				
				if ((blk.Tag == tag && blk.State != MESIState.INVALID) || (checkTransientTag && blk.TransientTag == tag && this.Dir.DirLocks[(int)@set].Locked)) {
					return blk;
				}
			}
			
			return null;
		}

		public bool FindBlock (uint addr, out uint @set, out uint way, out uint tag, out MESIState state, bool checkTransientTag)
		{
			@set = this.Set (addr);
			tag = this.Tag (addr);
			
			CacheBlock blkFound = this.BlockOf (addr, checkTransientTag);
			
			way = blkFound != null ? blkFound.Way : 0;
			state = blkFound != null ? blkFound.State : MESIState.INVALID;
			
			return blkFound != null;
		}

		public void SetBlock (uint @set, uint way, uint tag, MESIState state)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			Debug.Assert (way >= 0 && way < this.Assoc);
			
			this.AccessBlock (@set, way);
			this[@set][way].Tag = tag;
			this[@set][way].State = state;
		}

		public void GetBlock (uint @set, uint way, out uint tag, out MESIState state)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			Debug.Assert (way >= 0 && way < this.Assoc);
			
			tag = this[@set][way].Tag;
			state = this[@set][way].State;
		}

		public void AccessBlock (uint @set, uint way)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			Debug.Assert (way >= 0 && way < this.Assoc);
			
			this[@set][way].LastAccess = Simulator.CurrentCycle;
		}

		public uint ReplaceBlock (uint @set)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			
			ulong smallestTime = this[@set][0].LastAccess;
			uint smallestIndex = 0;
			
			for (uint way = 0; way < this[@set].Assoc; way++) {
				CacheBlock blk = this[@set][way];
				
				ulong time = blk.LastAccess;
				if (time < smallestTime) {
					smallestIndex = way;
					smallestTime = time;
				}
			}
			
			return smallestIndex;
		}

		public CacheSet this[uint index] {
			get { return this.Sets[(int)index]; }
			set { this.Sets[(int)index] = value; }
		}

		public uint LogBlockSize {
			get { return (uint)Math.Log (this.BlockSize, 2); }
		}

		public uint BlockMask {
			get { return this.BlockSize - 1; }
		}

		public uint Set (uint addr)
		{
			return (addr >> (int)this.LogBlockSize) % this.NumSets;
		}

		public uint Tag (uint addr)
		{
			return addr & ~this.BlockMask;
		}

		public uint Offset (uint addr)
		{
			return addr & this.BlockMask;
		}

		public uint Assoc {
			get { return this.CacheConfig.Assoc; }
		}

		public uint NumSets {
			get { return this.CacheConfig.NumSets; }
		}

		public uint BlockSize {
			get { return this.CacheConfig.BlockSize; }
		}

		public List<CacheSet> Sets { get; set; }
		public Dir Dir { get; set; }

		public CacheConfig CacheConfig { get; set; }
	}

	public abstract class CoherentCacheNode
	{
		public CoherentCacheNode (MemorySystem memorySystem, string name)
		{
			this.Name = name;
			this.MemorySystem = memorySystem;
			
			this.EventQueue = new DelegateEventQueue ();
			Simulator.SingleInstance.AddEventProcessor (this.EventQueue);
		}

		public void Schedule (VoidDelegate evt, ulong delay)
		{
			this.EventQueue.Schedule (evt, delay);
		}

		public delegate void FindAndLockDelegate (bool hasError, uint @set, uint way, MESIState state, uint tag, DirLock dirLock);

		public virtual void FindAndLock (uint addr, bool isBlocking, bool isRead, bool isRetry, FindAndLockDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void Load (uint addr, bool isRetry, VoidDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void Store (uint addr, bool isRetry, VoidDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void Evict (uint @set, uint way, HasErrorDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void EvictReceive (CoherentCacheNode source, uint addr, bool isWriteback, HasErrorDelegate onReceiveReplyCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void ReadRequest (CoherentCacheNode target, uint addr, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void ReadRequestReceive (CoherentCacheNode source, uint addr, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void WriteRequest (CoherentCacheNode target, uint addr, HasErrorDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void WriteRequestReceive (CoherentCacheNode source, uint addr, HasErrorDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void Invalidate (CoherentCacheNode except, uint @set, uint way, VoidDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}
		
		public override string ToString ()
		{
			return string.Format ("[CoherentCacheNode: Name={0}, Level={1}, MemorySystem={2}, Next={3}, EventQueue={4}]", this.Name, this.Level, this.MemorySystem, this.Next, this.EventQueue);
		}

		public abstract uint Level { get; }

		public string Name { get; set; }
		public MemorySystem MemorySystem { get; set; }
		public CoherentCacheNode Next { get; set; }
		public DelegateEventQueue EventQueue { get; set; }
	}

	public class Sequencer : CoherentCacheNode
	{
		public Sequencer (string name, CoherentCache l1Cache) : base(l1Cache.MemorySystem, name)
		{
			this.L1Cache = l1Cache;
		}

		public void Load (uint addr, bool isRetry, ReorderBufferEntry reorderBufferEntry, ReorderBufferEntryDelegate onCompletedCallback)
		{
			this.Load (addr, isRetry, delegate() { onCompletedCallback (reorderBufferEntry); });
		}

		public override void Load (uint addr, bool isRetry, VoidDelegate onCompletedCallback)
		{
			this.L1Cache.Load (addr, isRetry, onCompletedCallback);
		}

		public override void Store (uint addr, bool isRetry, VoidDelegate onCompletedCallback)
		{
			this.L1Cache.Store (addr, isRetry, onCompletedCallback);
		}
		
		public override string ToString ()
		{
			return string.Format ("[Sequencer: Name={0}]", this.Name);
		}

		public uint BlockSize {
			get { return this.L1Cache.Cache.BlockSize; }
		}

		public uint BlockAddress (uint addr)
		{
			return this.L1Cache.Cache.Tag (addr);
		}

		public override uint Level {
			get {
				throw new NotImplementedException ();
			}
		}

		CoherentCache L1Cache { get; set; }
	}

	public class CoherentCache : CoherentCacheNode
	{
		public CoherentCache (MemorySystem memorySystem, CacheConfig config, CacheStat stat) : base(memorySystem, config.Name)
		{
			this.Cache = new Cache (config);
			this.Config = config;
			this.Stat = stat;
		}

		public uint RetryLat {
			get { return (uint)(this.HitLatency + (new Random ().Next (0, (int)(this.HitLatency + 2)))); }
		}

		public void Retry (VoidDelegate action)
		{
			this.EventQueue.Schedule (action, this.RetryLat);
		}

		public uint HitLatency {
			get { return this.Config.HitLatency; }
		}

		public override uint Level {
			get { return this.Config.Level; }
		}

		public override void FindAndLock (uint addr, bool isBlocking, bool isRead, bool isRetry, FindAndLockDelegate onCompletedCallback)
		{
			uint @set, way, tag;
			MESIState state;
			
			bool hit = this.Cache.FindBlock (addr, out @set, out way, out tag, out state, true);
			
			this.Stat.Accesses++;
			if (hit) {
				this.Stat.Hits++;
			}
			if (isRead) {
				this.Stat.Reads++;
				
				if (isBlocking) {
					this.Stat.BlockingReads++;
				} else {
					this.Stat.NonblockingReads++;
				}
				
				if (hit) {
					this.Stat.ReadHits++;
				}
			} else {
				this.Stat.Writes++;
				
				if (isBlocking) {
					this.Stat.BlockingWrites++;
				} else {
					this.Stat.NonblockingWrites++;
				}
				
				if (hit) {
					this.Stat.WriteHits++;
				}
			}
			
			if (!isRetry) {
				this.Stat.NoRetryAccesses++;
				
				if (hit) {
					this.Stat.NoRetryHits++;
				}
				
				if (isRead) {
					this.Stat.NoRetryReads++;
					
					if (hit) {
						this.Stat.NoRetryReadHits++;
					}
				} else {
					this.Stat.NoRetryWrites++;
					
					if (hit) {
						this.Stat.NoRetryWriteHits++;
					}
				}
			}
			
			uint dumbTag;
			
			if (!hit) {
				way = this.Cache.ReplaceBlock (@set);
				this.Cache.GetBlock (@set, way, out dumbTag, out state);
			}
			
			DirLock dirLock = this.Cache.Dir.DirLocks[(int)@set];
			if (!dirLock.Lock ()) {
				if (isBlocking) {
					onCompletedCallback (true, @set, way, state, tag, dirLock);
				} else {
					this.Retry (delegate() { this.FindAndLock (addr, isBlocking, isRead, true, onCompletedCallback); });
				}
			} else {
				this.Cache[@set][way].TransientTag = tag;
				
				if (!hit && state != MESIState.INVALID) {
					
					this.Schedule (delegate() { this.Evict (@set, way, delegate(bool hasError) {uint dumbTag1;if (!hasError) {this.Stat.Evictions++;this.Cache.GetBlock (@set, way, out dumbTag1, out state);onCompletedCallback (false, @set, way, state, tag, dirLock);} else {this.Cache.GetBlock (@set, way, out dumbTag, out state);dirLock.Unlock ();onCompletedCallback (true, @set, way, state, tag, dirLock);}}); }, this.HitLatency);
				} else {
					this.Schedule (delegate() { onCompletedCallback (false, @set, way, state, tag, dirLock); }, this.HitLatency);
				}
			}
		}

		public override void Load (uint addr, bool isRetry, VoidDelegate onCompletedCallback)
		{
			this.FindAndLock (addr, false, true, isRetry, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirLock dirLock) {
				if (!hasError) {
					if (!MESIUtils.IsReadHit (state)) {
						this.ReadRequest (this.Next, tag, delegate(bool hasError1, bool isShared) {
							if (!hasError1) {
								this.Cache.SetBlock (@set, way, tag, isShared ? MESIState.SHARED : MESIState.EXCLUSIVE);
								this.Cache.AccessBlock (@set, way);
								dirLock.Unlock ();
								onCompletedCallback ();
							} else {
								this.Stat.ReadRetries++;
								dirLock.Unlock ();
								this.Retry (delegate() { this.Load (addr, true, onCompletedCallback); });
							}
						});
					} else {
						this.Cache.AccessBlock (@set, way);
						dirLock.Unlock ();
						onCompletedCallback ();
					}
				} else {
					this.Stat.ReadRetries++;
					this.Retry (delegate() { this.Load (addr, true, onCompletedCallback); });
				}
			});
		}

		public override void Store (uint addr, bool isRetry, VoidDelegate onCompletedCallback)
		{
			this.FindAndLock (addr, false, false, isRetry, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirLock dirLock) {
				if (!hasError) {
					if (!MESIUtils.IsWriteHit (state)) {
						this.WriteRequest (this.Next, tag, delegate(bool hasError1) {
							if (!hasError1) {
								this.Cache.AccessBlock (@set, way);
								this.Cache.SetBlock (@set, way, tag, MESIState.MODIFIED);
								dirLock.Unlock ();
								onCompletedCallback ();
							} else {
								this.Stat.WriteRetries++;
								dirLock.Unlock ();
								this.Retry (delegate() { this.Store (addr, true, onCompletedCallback); });
							}
						});
					} else {
						this.Cache.AccessBlock (@set, way);
						this.Cache.SetBlock (@set, way, tag, MESIState.MODIFIED);
						dirLock.Unlock ();
						onCompletedCallback ();
					}
				} else {
					this.Stat.WriteRetries++;
					this.Retry (delegate() { this.Store (addr, true, onCompletedCallback); });
				}
			});
		}

		public override void Evict (uint @set, uint way, HasErrorDelegate onCompletedCallback)
		{
			uint tag;
			MESIState state;
			
			this.Cache.GetBlock (@set, way, out tag, out state);
			
			uint srcSet = @set; //TODO: is it necessary or bug?
			uint srcWay = way;
			uint srcTag = tag;
			CoherentCacheNode target = this.Next;
			
			this.Invalidate (null, @set, way, delegate() {
				if (state == MESIState.INVALID) {
					onCompletedCallback (false);
				} else if (state == MESIState.MODIFIED) {
					this.Schedule (delegate() { target.EvictReceive (this, srcTag, true, delegate(bool hasError) { this.Schedule (delegate() { this.EvictReplyReceive (hasError, srcSet, srcWay, onCompletedCallback); }, 2); }); }, 2);
				} else {
					this.Schedule (delegate() { target.EvictReceive (this, srcTag, false, delegate(bool hasError) { this.Schedule (delegate() { this.EvictReplyReceive (hasError, srcSet, srcWay, onCompletedCallback); }, 2); }); }, 2);
				}
			});
		}

		public override void EvictReceive (CoherentCacheNode source, uint addr, bool isWriteback, HasErrorDelegate onReceiveReplyCallback)
		{
			this.FindAndLock (addr, false, false, false, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirLock dirLock) {
				if (!hasError) {
					if (!isWriteback) {
						this.EvictProcess (source, @set, way, dirLock, onReceiveReplyCallback);
					} else {
						this.Invalidate (source, @set, way, delegate() {
							if (state == MESIState.SHARED) {
								this.WriteRequest (this.Next, tag, delegate(bool hasError1) { this.EvictWritebackFinish (source, hasError1, @set, way, tag, dirLock, onReceiveReplyCallback); });
							} else {
								this.EvictWritebackFinish (source, false, @set, way, tag, dirLock, onReceiveReplyCallback);
							}
						});
					}
				} else {
					onReceiveReplyCallback (true);
				}
			});
		}

		public void EvictWritebackFinish (CoherentCacheNode source, bool hasError, uint @set, uint way, uint tag, DirLock dirLock, HasErrorDelegate onReceiveReplyCallback)
		{
			if (!hasError) {
				this.Cache.SetBlock (@set, way, tag, MESIState.MODIFIED);
				this.Cache.AccessBlock (@set, way);
				this.EvictProcess (source, @set, way, dirLock, onReceiveReplyCallback);
			} else {
				dirLock.Unlock ();
				onReceiveReplyCallback (true);
			}
		}

		public void EvictProcess (CoherentCacheNode source, uint @set, uint way, DirLock dirLock, HasErrorDelegate onReceiveReplyCallback)
		{
			DirEntry dirEntry = this.Cache.Dir.DirEntries[(int)@set][(int)way];
			dirEntry.UnsetSharer (source);
			if (dirEntry.Owner == source) {
				dirEntry.Owner = null;
			}
			dirLock.Unlock ();
			onReceiveReplyCallback (false);
		}

		public void EvictReplyReceive (bool hasError, uint srcSet, uint srcWay, HasErrorDelegate onCompletedCallback)
		{
			this.Schedule (delegate() {
				if (!hasError) {
					this.Cache.SetBlock (srcSet, srcWay, 0, MESIState.INVALID);
				}
				onCompletedCallback (hasError);
			}, 2);
		}

		public override void ReadRequest (CoherentCacheNode target, uint addr, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			this.Schedule (delegate() { target.ReadRequestReceive (this, addr, onCompletedCallback); }, 2);
		}

		public override void ReadRequestReceive (CoherentCacheNode source, uint addr, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			this.FindAndLock (addr, this.Next == source, true, false, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirLock dirLock) {
				if (!hasError) {
					if (source.Next == this) {
						this.ReadRequestUpdown (source, @set, way, tag, state, dirLock, onCompletedCallback);
					} else {
						this.ReadRequestDownup (@set, way, tag, dirLock, onCompletedCallback);
					}
				} else {
					this.Schedule (delegate() { onCompletedCallback (true, false); }, 2);
				}
			});
		}

		public void ReadRequestUpdown (CoherentCacheNode source, uint @set, uint way, uint tag, MESIState state, DirLock dirLock, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			uint pending = 1;
			
			if (state != MESIState.INVALID) {
				DirEntry dirEntry = this.Cache.Dir.DirEntries[(int)@set][(int)way];
				
				if (dirEntry.Owner != null && dirEntry.Owner != source) {
					pending++;
					this.ReadRequest (dirEntry.Owner, tag, delegate(bool hasError, bool isShared) { this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback); });
				}
				
				this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback);
			} else {
				this.ReadRequest (this.Next, tag, delegate(bool hasError, bool isShared) {
					if (!hasError) {
						this.Cache.SetBlock (@set, way, tag, isShared ? MESIState.SHARED : MESIState.EXCLUSIVE);
						this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback);
					} else {
						dirLock.Unlock ();
						this.Schedule (delegate() { onCompletedCallback (true, false); }, 2);
					}
				});
			}
		}

		public void ReadRequestUpdownFinish (CoherentCacheNode source, uint @set, uint way, DirLock dirLock, ref uint pending, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			pending--;
			
			if (pending == 0) {
				DirEntry dirEntry = this.Cache.Dir.DirEntries[(int)@set][(int)way];
				if (dirEntry.Owner != null && dirEntry.Owner != source) {
					dirEntry.Owner = null;
				}
				
				dirEntry.SetSharer (source);
				if (!dirEntry.IsShared) {
					dirEntry.Owner = source;
				}
				
				this.Cache.AccessBlock (@set, way);
				dirLock.Unlock ();
				this.Schedule (delegate() { onCompletedCallback (false, dirEntry.IsShared); }, 2);
			}
		}

		public void ReadRequestDownup (uint @set, uint way, uint tag, DirLock dirLock, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			uint pending = 1;
			
			DirEntry dirEntry = this.Cache.Dir.DirEntries[(int)@set][(int)way];
			if (dirEntry.Owner != null) {
				pending++;
				
				this.ReadRequest (dirEntry.Owner, tag, delegate(bool hasError, bool isShared) { this.ReadRequestDownupFinish (@set, way, tag, dirLock, ref pending, onCompletedCallback); });
			}
			
			this.ReadRequestDownupFinish (@set, way, tag, dirLock, ref pending, onCompletedCallback);
		}

		public void ReadRequestDownupFinish (uint @set, uint way, uint tag, DirLock dirLock, ref uint pending, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			pending--;
			
			if (pending == 0) {
				DirEntry dirEntry = this.Cache.Dir.DirEntries[(int)@set][(int)way];
				dirEntry.Owner = null;
				
				this.Cache.SetBlock (@set, way, tag, MESIState.SHARED);
				this.Cache.AccessBlock (@set, way);
				dirLock.Unlock ();
				this.Schedule (delegate() { onCompletedCallback (false, false); }, 2);
			}
		}

		public override void WriteRequest (CoherentCacheNode target, uint addr, HasErrorDelegate onCompletedCallback)
		{
			this.Schedule (delegate() { target.WriteRequestReceive (this, addr, onCompletedCallback); }, 2);
		}

		public override void WriteRequestReceive (CoherentCacheNode source, uint addr, HasErrorDelegate onCompletedCallback)
		{
			this.FindAndLock (addr, this.Next == source, false, false, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirLock dirLock) {
				if (!hasError) {
					this.Invalidate (source, @set, way, delegate() {
						if (source.Next == this) {
							if (state == MESIState.MODIFIED || state == MESIState.EXCLUSIVE) {
								this.WriteRequestUpdownFinish (source, false, @set, way, tag, state, dirLock, onCompletedCallback);
							} else {
								this.WriteRequest (this.Next, tag, delegate(bool hasSError) { this.WriteRequestUpdownFinish (source, hasError, @set, way, tag, state, dirLock, onCompletedCallback); });
							}
						} else {
							this.Cache.SetBlock (@set, way, 0, MESIState.INVALID);
							dirLock.Unlock ();
							this.Schedule (delegate() { onCompletedCallback (false); }, 2);
						}
					});
				} else {
					this.Schedule (delegate() { onCompletedCallback (true); }, 2);
				}
			});
		}

		public void WriteRequestUpdownFinish (CoherentCacheNode source, bool hasError, uint @set, uint way, uint tag, MESIState state, DirLock dirLock, HasErrorDelegate onCompletedCallback)
		{
			if (!hasError) {
				DirEntry dirEntry = this.Cache.Dir.DirEntries[(int)@set][(int)way];
				dirEntry.SetSharer (source);
				dirEntry.Owner = source;
				
				this.Cache.AccessBlock (@set, way);
				if (state != MESIState.MODIFIED) {
					this.Cache.SetBlock (@set, way, tag, MESIState.EXCLUSIVE);
				}
				
				dirLock.Unlock ();
				this.Schedule (delegate() { onCompletedCallback (false); }, 2);
			} else {
				dirLock.Unlock ();
				this.Schedule (delegate() { onCompletedCallback (true); }, 2);
			}
		}

		public override void Invalidate (CoherentCacheNode except, uint @set, uint way, VoidDelegate onCompletedCallback)
		{
			uint tag;
			MESIState state;
			
			this.Cache.GetBlock (@set, way, out tag, out state);
			
			uint pending = 1;
			
			DirEntry dirEntry = this.Cache.Dir.DirEntries[(int)@set][(int)way];
			
			foreach (CoherentCacheNode sharer in dirEntry.Sharers.FindAll(sharer => sharer != except)) {
				dirEntry.UnsetSharer (sharer);
				if (dirEntry.Owner == sharer) {
					dirEntry.Owner = null;
				}
				
				this.WriteRequest (sharer, tag, delegate(bool hasError) {
					pending--;
					
					if (pending == 0) {
						onCompletedCallback ();
					}
				});
				
				pending++;
			}
			
			pending--;
			
			if (pending == 0) {
				onCompletedCallback ();
			}
		}

		public Cache Cache { get; set; }
		public CacheConfig Config { get; set; }
		public CacheStat Stat { get; set; }
	}

	public class MemoryController : CoherentCacheNode
	{
		public MemoryController (MemorySystem memorySystem, MainMemoryConfig config, MainMemoryStat stat) : base(memorySystem, "mem")
		{
			this.Config = config;
			this.Stat = stat;
		}

		public override uint Level {
			get {
				throw new NotImplementedException ();
			}
		}

		public uint Latency {
			get { return this.Config.Latency; }
		}

		public override void EvictReceive (CoherentCacheNode source, uint addr, bool isWriteback, HasErrorDelegate onReceiveReplyCallback)
		{
			this.Stat.Accesses++;
			this.Stat.Writes++;
			
			this.Schedule (delegate() { onReceiveReplyCallback (false); }, this.Latency);
		}

		public override void ReadRequestReceive (CoherentCacheNode source, uint addr, HasErrorAndIsSharedDelegate onCompletedCallback)
		{
			this.Stat.Accesses++;
			this.Stat.Reads++;
			
			this.Schedule (delegate() { onCompletedCallback (false, false); }, this.Latency);
		}

		public override void WriteRequestReceive (CoherentCacheNode source, uint addr, HasErrorDelegate onCompletedCallback)
		{
			this.Stat.Accesses++;
			this.Stat.Writes++;
			
			this.Schedule (delegate() { onCompletedCallback (false); }, this.Latency);
		}

		public MainMemoryConfig Config { get; set; }
		public MainMemoryStat Stat { get; set; }
	}

	public class MemorySystem
	{
		public MemorySystem (Simulation simulation)
		{
			this.Simulation = simulation;
			this.EndNodeCount = (uint)this.Simulation.Config.Architecture.Processor.Cores.Count;
			
			this.SeqIs = new List<Sequencer> ();
			this.SeqDs = new List<Sequencer> ();
			
			this.L1Is = new List<CoherentCache> ();
			this.L1Ds = new List<CoherentCache> ();
			
			this.CreateMemoryHierarchy ();
		}

		private void CreateMemoryHierarchy ()
		{
			this.Mem = new MemoryController (this, this.Simulation.Config.Architecture.MainMemory, this.Simulation.Stat.MainMemory);
			
			this.L2 = new CoherentCache (this, this.Simulation.Config.Architecture.L2Cache, this.Simulation.Stat.L2Cache);
			this.L2.Next = this.Mem;
			
			for (int i = 0; i < this.EndNodeCount; i++) {
				CoherentCache l1I = new CoherentCache (this, this.Simulation.Config.Architecture.Processor.Cores[i].ICache, this.Simulation.Stat.Processor.Cores[i].ICache);
				Sequencer seqI = new Sequencer ("seqI-" + i, l1I);
				
				CoherentCache l1D = new CoherentCache (this, this.Simulation.Config.Architecture.Processor.Cores[i].DCache, this.Simulation.Stat.Processor.Cores[i].DCache);
				Sequencer seqD = new Sequencer ("seqD-" + i, l1D);
				
				this.SeqIs.Add (seqI);
				this.L1Is.Add (l1I);
				
				this.SeqDs.Add (seqD);
				this.L1Ds.Add (l1D);
				
				l1I.Next = l1D.Next = this.L2;
			}
			
			this.MMU = new MMU ();
		}

		public uint EndNodeCount { get; set; }

		public List<Sequencer> SeqIs { get; private set; }
		public List<Sequencer> SeqDs { get; private set; }

		public List<CoherentCache> L1Is { get; private set; }
		public List<CoherentCache> L1Ds { get; private set; }

		public CoherentCache L2 { get; set; }

		public MemoryController Mem { get; set; }

		public MMU MMU { get; set; }

		public Simulation Simulation { get; set; }
	}
}