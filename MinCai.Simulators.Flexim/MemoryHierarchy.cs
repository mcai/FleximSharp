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
using MinCai.Simulators.Flexim.Microarchitecture;

namespace MinCai.Simulators.Flexim.MemoryHierarchy
{
	public static class MemoryConstants
	{
		public static uint LOG_PAGE_SIZE = 12;
		public static uint PAGE_SHIFT = LOG_PAGE_SIZE;
		public static uint PAGE_SIZE = (uint)(1 << (int)LOG_PAGE_SIZE);
		public static uint PAGE_MASK = PAGE_SIZE - 1;
		public static uint PAGE_COUNT = 1024;

		public static uint BLOCK_SIZE = 64;
	}

	public enum MemoryAccessType : uint
	{
		None = 0x00,
		Read = 0x01,
		Write = 0x02,
		Execute = 0x04,
		Init = 0x08
	}

	public sealed class MemoryPage
	{
		public MemoryPage ()
		{
			this.Tag = 0;
			this.Permission = MemoryAccessType.None;
			this.Data = new byte[MemoryConstants.PAGE_SIZE];
			this.Next = null;
		}

		public uint Tag { get; set; }
		public MemoryAccessType Permission { get; set; }
		public byte[] Data { get; set; }

		public MemoryPage Next { get; set; }
	}

	public sealed class SegmentationFaultException : Exception
	{
		public SegmentationFaultException (uint addr) : base(string.Format ("SegmentationFaultException @ 0x{0:x8}", addr))
		{
			this.Addr = addr;
		}

		public uint Addr {get; private set;}
	}

	public sealed class Memory
	{
		public Memory ()
		{
			this.safe = true;
			
			this.pages = new Dictionary<uint, MemoryPage> ();
		}

		unsafe public void InitByte (uint addr, byte data)
		{
			this.Access (addr, 1, &data, MemoryAccessType.Init);
		}

		unsafe public void InitHalfWord (uint addr, ushort data)
		{
			this.Access (addr, 2, (byte*)&data, MemoryAccessType.Init);
		}

		unsafe public void InitWord (uint addr, uint data)
		{
			this.Access (addr, 4, (byte*)&data, MemoryAccessType.Init);
		}

		unsafe public void InitDoubleWord (uint addr, ulong data)
		{
			this.Access (addr, 8, (byte*)&data, MemoryAccessType.Init);
		}

		unsafe public void InitString (uint addr, char* str)
		{
			this.Access (addr, (int)(PtrHelper.Strlen (str) + 1), (byte*)str, MemoryAccessType.Init);
		}

		unsafe public void InitBlock (uint addr, uint size, byte* p)
		{
			for (uint i = 0; i < size; i++) {
				this.InitByte (addr + i, *(p + i));
			}
		}

		unsafe public void WriteByte (uint addr, byte data)
		{
			this.Access (addr, 1, &data, MemoryAccessType.Write);
		}

		unsafe public void WriteHalfWord (uint addr, ushort data)
		{
			this.Access (addr, 2, (byte*)&data, MemoryAccessType.Write);
		}

		unsafe public void WriteWord (uint addr, uint data)
		{
			this.Access (addr, 4, (byte*)&data, MemoryAccessType.Write);
		}

		unsafe public void WriteDoubleWord (uint addr, ulong data)
		{
			this.Access (addr, 8, (byte*)&data, MemoryAccessType.Write);
		}

		unsafe public void WriteString (uint addr, char* str)
		{
			this.Access (addr, (int)(PtrHelper.Strlen (str) + 1), (byte*)str, MemoryAccessType.Write);
		}

		unsafe public void WriteBlock (uint addr, uint size, byte* data)
		{
			this.Access (addr, (int)size, data, MemoryAccessType.Write);
		}

		unsafe public void ReadByte (uint addr, byte* data)
		{
			this.Access (addr, 1, data, MemoryAccessType.Read);
		}

		unsafe public void ReadHalfWord (uint addr, ushort* data)
		{
			this.Access (addr, 2, (byte*)data, MemoryAccessType.Read);
		}

		unsafe public void ReadWord (uint addr, uint* data)
		{
			this.Access (addr, 4, (byte*)data, MemoryAccessType.Read);
		}

		unsafe public void ReadDoubleWord (uint addr, ulong* data)
		{
			this.Access (addr, 8, (byte*)data, MemoryAccessType.Read);
		}

		unsafe public int ReadString (uint addr, int size, char* str)
		{
			int i;
			for (i = 0; i < size; i++) {
				this.Access ((uint)(addr + i), 1, (byte*)(str + i), MemoryAccessType.Read);
				if (str[i] == 0)
					break;
			}
			return i;
		}

		unsafe public void ReadBlock (uint addr, int size, byte* p)
		{
			this.Access (addr, size, p, MemoryAccessType.Read);
		}

		unsafe public void Zero (uint addr, int size)
		{
			byte zero = 0;
			while (size-- > 0) {
				this.Access (addr++, 0, &zero, MemoryAccessType.Write);
			}
		}

		public uint GetTag (uint addr)
		{
			return addr & ~(MemoryConstants.PAGE_SIZE - 1);
		}

		public uint GetOffset (uint addr)
		{
			return addr & (MemoryConstants.PAGE_SIZE - 1);
		}

		public uint GetIndex (uint addr)
		{
			return (addr >> (int)MemoryConstants.LOG_PAGE_SIZE) % MemoryConstants.PAGE_COUNT;
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

		public MemoryPage AddPage (uint addr, MemoryAccessType permission)
		{
			uint tag = GetTag (addr);
			uint index = GetIndex (addr);
			
			MemoryPage page = new MemoryPage ();
			page.Tag = tag;
			page.Permission = permission;
			
			page.Next = this[index];
			this[index] = page;
			this.mappedSpace += MemoryConstants.PAGE_SIZE;
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
			
			this.mappedSpace -= MemoryConstants.PAGE_SIZE;
			
			page = null;
		}

		unsafe public void Copy (uint dest, uint src, int size)
		{
			Debug.Assert (IsAligned (dest));
			Debug.Assert (IsAligned (src));
			Debug.Assert (IsAligned ((uint)size));
			if ((src < dest && src + size > dest) || (dest < src && dest + size > src))
				Logger.Fatal (LogCategory.Memory, "mem_copy: cannot copy overlapping regions");
			
			while (size > 0) {
				MemoryPage pageDest = this.GetPage (dest);
				MemoryPage pageSrc = this.GetPage (src);
				Debug.Assert (pageSrc != null && pageDest != null);
				Array.Copy (pageSrc.Data, pageDest.Data, MemoryConstants.PAGE_SIZE);
				src += MemoryConstants.PAGE_SIZE;
				dest += MemoryConstants.PAGE_SIZE;
				size -= (int)MemoryConstants.PAGE_SIZE;
			}
		}

		unsafe private void AccessPageBoundary (uint addr, int size, byte* buf, MemoryAccessType access)
		{
			MemoryPage page = this.GetPage (addr);
			
			if (page == null && !this.safe) {
				switch (access) {
				case MemoryAccessType.Read:
				case MemoryAccessType.Execute:
					Logger.Warnf (LogCategory.Memory, "Memory.accessPageBoundary: unsafe reading 0x{0:x8}", addr);
					PtrHelper.Memset (buf, 0, size);
					return;
				
				case MemoryAccessType.Write:
				case MemoryAccessType.Init:
					Logger.Warnf (LogCategory.Memory, "Memory.accessPageBoundary: unsafe writing 0x{0:x8}", addr);
					page = AddPage (addr, MemoryAccessType.Read | MemoryAccessType.Write | MemoryAccessType.Execute | MemoryAccessType.Init);
					break;
				default:
					Logger.Panic (LogCategory.Memory, "Memory.accessPageBoundary: unknown access");
					break;
				}
			}
			
			if (this.safe) {
				if (page == null) {
					throw new SegmentationFaultException (addr);
				}
				if ((page.Permission & access) != access) {
					Logger.Fatalf (LogCategory.Memory, "Memory.accessPageBoundary: permission denied at 0x{0:x8}, page.Permission: 0x{1:x8}, access: 0x{2:x8}", addr, page.Permission, access);
				}
			}
			
			uint offset = this.GetOffset (addr);
			Debug.Assert (offset + size <= MemoryConstants.PAGE_SIZE);
			
			fixed (byte* data = &page.Data[offset]) {
				switch (access) {
				case MemoryAccessType.Read:
				case MemoryAccessType.Execute:
					PtrHelper.Memcpy (buf, data, size);
					break;
				case MemoryAccessType.Write:
				case MemoryAccessType.Init:
					PtrHelper.Memcpy (data, buf, size);
					break;
				default:
					Logger.Panic (LogCategory.Memory, "Memory.accessPageBoundary: unknown access");
					break;
				}
			}
		}

		unsafe public void Access (uint addr, int size, byte* buf, MemoryAccessType access)
		{
			while (size > 0) {
				uint offset = this.GetOffset (addr);
				int chunksize = Math.Min (size, (int)(MemoryConstants.PAGE_SIZE - offset));
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
					tagEnd += MemoryConstants.PAGE_SIZE;
					tagStart = tagEnd;
					continue;
				}
				
				if (tagEnd - tagStart + MemoryConstants.PAGE_SIZE == size)
					break;
				
				Debug.Assert (tagEnd - tagStart + MemoryConstants.PAGE_SIZE < size);
				
				tagEnd += MemoryConstants.PAGE_SIZE;
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
					tagStart += MemoryConstants.PAGE_SIZE;
					tagEnd = tagStart;
					continue;
				}
				
				if (tagEnd - tagStart + MemoryConstants.PAGE_SIZE == size)
					break;
				
				Debug.Assert (tagEnd - tagStart + MemoryConstants.PAGE_SIZE < size);
				
				tagEnd -= MemoryConstants.PAGE_SIZE;
			}
			
			return tagStart;
		}

		public void Map (uint addr, int size, MemoryAccessType permission)
		{
//			Logger.Infof(LogCategory.MEMORY, "Memory.Map(), addr: 0x{0:x8} ~ 0x{1:x8}, size: {2:d}, permission: 0x{3:x8}", addr, addr + size, size, permission);
			
			for (uint tag = this.GetTag (addr); tag <= this.GetTag ((uint)(addr + size - 1)); tag += MemoryConstants.PAGE_SIZE) {
				MemoryPage page = this.GetPage (tag);
				if (page == null) {
					page = this.AddPage (tag, permission);
					page.Permission |= permission;
				}
			}
		}

		public void Unmap (uint addr, int size)
		{
			Debug.Assert (IsAligned (addr));
			Debug.Assert (IsAligned ((uint)size));
			
			for (uint tag = GetTag (addr); tag <= GetTag ((uint)(addr + size - 1)); tag += MemoryConstants.PAGE_SIZE) {
				if (this.GetPage (tag) != null) {
					this.RemovePage (tag);
				}
			}
		}

		public void Protect (uint addr, int size, MemoryAccessType permission)
		{
			Debug.Assert (IsAligned (addr));
			Debug.Assert (IsAligned ((uint)size));
			
			for (uint tag = GetTag (addr); tag <= GetTag ((uint)(addr + size - 1)); tag += MemoryConstants.PAGE_SIZE) {
				MemoryPage page = this.GetPage (tag);
				if (page != null) {
					page.Permission = permission;
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

		private bool safe {get; set;}
	}

	public sealed class MemoryManagementUnit //TODO: correctness?
	{
		private sealed class Page
		{
			public Page ()
			{
			}

			public uint VirtualAddress { get; set; }
			public uint PhysicalAddress { get; set; }
			public Page Next { get; set; }
			public Directory Directory { get; set; } //TODO: what about the usage?
		}

		public MemoryManagementUnit ()
		{
			this.Pages = new Dictionary<uint, Page> ();
		}

		private Page getPage (uint virtualAddress)
		{
			uint pageIndex = GetPageIndex (virtualAddress);
			uint tag = GetTag (virtualAddress);
			
			Page page = this[pageIndex];
			
			while (page != null) {
				if (page.VirtualAddress == tag)
					break;
				page = page.Next;
			}
			
			if (page == null) {
				page = new Page ();
				page.Directory = new Directory (MemoryConstants.PAGE_SIZE / MemoryConstants.BLOCK_SIZE, 1);
				
				page.VirtualAddress = tag;
				page.PhysicalAddress = this.PageCount << (int)MemoryConstants.LOG_PAGE_SIZE;
				
				page.Next = this[pageIndex];
				this[pageIndex] = page;
				
				this.PageCount++;
			}
			
			return page;
		}

		public uint GetPhysicalAddress (uint virtualAddress)
		{
			Page page = this.getPage (virtualAddress);
			return page.PhysicalAddress | GetOffset (virtualAddress);
		}

		private Page this[uint index] {
			get {
				if (this.Pages.ContainsKey (index)) {
					return this.Pages[index];
				}
				
				return null;
			}
			set { this.Pages[index] = value; }
		}

		private Dictionary<uint, Page> Pages { get; set; }
		private uint PageCount { get; set; }

		private static uint GetPageIndex (uint virtualAddress)
		{
			return virtualAddress >> (int)MemoryConstants.LOG_PAGE_SIZE;
		}

		private static uint GetTag (uint virtualAddress)
		{
			return virtualAddress & ~MemoryConstants.PAGE_MASK;
		}

		private static uint GetOffset (uint virtualAddress)
		{
			return virtualAddress & MemoryConstants.PAGE_MASK;
		}
	}

	public sealed class DirectoryEntry
	{
		public DirectoryEntry (uint x, uint y)
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

		public uint X { get; private set; }
		public uint Y { get; private set; }

		public CoherentCacheNode Owner { get; set; }
		public List<CoherentCacheNode> Sharers { get; private set; }
	}

	public sealed class DirectoryLock
	{
		public DirectoryLock (uint x)
		{
			this.X = x;
		}

		public bool Lock ()
		{
			if (this.IsLocked) {
				return false;
			} else {
				this.IsLocked = true;
				return true;
			}
		}

		public void Unlock ()
		{
			this.IsLocked = false;
		}

		public override string ToString ()
		{
			return string.Format ("[DirLock: X={0}, IsLocked={1}]", this.X, this.IsLocked);
		}

		public uint X { get; private set; }
		public bool IsLocked { get; private set; }
	}

	public sealed class Directory
	{
		public Directory (uint xSize, uint ySize)
		{
			this.XSize = xSize;
			this.YSize = ySize;
			
			this.Entries = new List<List<DirectoryEntry>> ();
			for (uint i = 0; i < this.XSize; i++) {
				List<DirectoryEntry> entries = new List<DirectoryEntry> ();
				this.Entries.Add (entries);
				
				for (uint j = 0; j < this.YSize; j++) {
					entries.Add (new DirectoryEntry (i, j));
				}
			}
			
			this.Locks = new List<DirectoryLock> ();
			for (uint i = 0; i < this.XSize; i++) {
				this.Locks.Add (new DirectoryLock (i));
			}
		}

		public bool IsSharedOrOwned (int x, int y)
		{
			return this.Entries[x][y].IsSharedOrOwned;
		}

		public uint XSize { get; private set; }
		public uint YSize { get; private set; }

		public List<List<DirectoryEntry>> Entries { get; private set; }
		public List<DirectoryLock> Locks { get; private set; }
	}

	public enum MESIState
	{
		Modified,
		Exclusive,
		Shared,
		Invalid
	}

	public enum CacheReplacementPolicy
	{
		LRU,
		FIFO,
		Random
	}

	public sealed class CacheBlock
	{
		public CacheBlock (CacheSet @set, uint way)
		{
			this.Set = @set;
			this.Way = way;
			
			this.Tag = 0;
			this.TransientTag = 0;
			this.State = MESIState.Invalid;
			
			this.LastAccessCycle = 0;
		}

		public override string ToString ()
		{
			return string.Format ("[CacheBlock: Set={0}, Way={1}, Tag={2}, TransientTag={3}, State={4}, LastAccessCycle={5}]", this.Set, this.Way, this.Tag, this.TransientTag, this.State, this.LastAccessCycle);
		}

		public CacheSet Set { get; private set; }
		public uint Way { get; private set; }

		public uint Tag { get; set; }
		public uint TransientTag { get; set; }
		public MESIState State { get; set; }

		public ulong LastAccessCycle { get; set; }
	}

	public sealed class CacheSet
	{
		public CacheSet (Cache cache, uint assoc, uint num)
		{
			this.Cache = cache;
			this.Assoc = assoc;
			this.Num = num;
			
			this.Blocks = new List<CacheBlock> ();
			for (uint i = 0; i < this.Assoc; i++) {
				this.Blocks.Add (new CacheBlock (this, i));
			}
		}

		public override string ToString ()
		{
			return string.Format ("[CacheSet: Assoc={0}, Cache={1}, Num={2}]", this.Assoc, this.Cache, this.Num);
		}

		public CacheBlock this[uint index] {
			get { return this.Blocks[(int)index]; }
		}

		public uint Assoc { get; private set; }
		public List<CacheBlock> Blocks { get; private set; }

		public Cache Cache { get; private set; }

		public uint Num { get; private set; }
	}

	public sealed class Cache
	{
		public Cache (CoherentCache coherentCache)
		{
			this.CoherentCache = coherentCache;
			
			this.Sets = new List<CacheSet> ();
			for (uint i = 0; i < this.NumSets; i++) {
				this.Sets.Add (new CacheSet (this, this.Assoc, i));
			}
			
			this.Directory = new Directory (this.NumSets, this.Assoc);
		}

		public CacheBlock GetBlock (uint addr, bool checkTransientTag)
		{
			uint tag = this.GetTag (addr);
			uint @set = this.GetSet (addr);
			
			for (uint way = 0; way < this[@set].Assoc; way++) {
				CacheBlock block = this[@set][way];
				
				if ((block.Tag == tag && block.State != MESIState.Invalid) || (checkTransientTag && block.TransientTag == tag && this.Directory.Locks[(int)@set].IsLocked)) {
					return block;
				}
			}
			
			return null;
		}

		public bool FindBlock (uint addr, out uint @set, out uint way, out uint tag, out MESIState state, bool checkTransientTag)
		{
			@set = this.GetSet (addr);
			tag = this.GetTag (addr);
			
			CacheBlock blockFound = this.GetBlock (addr, checkTransientTag);
			
			way = blockFound != null ? blockFound.Way : 0;
			state = blockFound != null ? blockFound.State : MESIState.Invalid;
			
			return blockFound != null;
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
			
			this[@set][way].LastAccessCycle = this.CoherentCache.CycleProvider.CurrentCycle;
		}

		public uint ReplaceBlock (uint @set)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			
			ulong smallestTime = this[@set][0].LastAccessCycle;
			uint smallestIndex = 0;
			
			for (uint way = 0; way < this[@set].Assoc; way++) {
				CacheBlock block = this[@set][way];
				
				ulong time = block.LastAccessCycle;
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

		public uint GetSet (uint addr)
		{
			return (addr >> (int)this.LogBlockSize) % this.NumSets;
		}

		public uint GetTag (uint addr)
		{
			return addr & ~this.BlockMask;
		}

		public uint GetOffset (uint addr)
		{
			return addr & this.BlockMask;
		}

		public uint Assoc {
			get { return this.CoherentCache.Config.Assoc; }
		}

		public uint NumSets {
			get { return this.CoherentCache.Config.NumSets; }
		}

		public uint BlockSize {
			get { return this.CoherentCache.Config.BlockSize; }
		}

		public List<CacheSet> Sets { get; private set; }
		public Directory Directory { get; private set; }

		public CoherentCache CoherentCache { get; private set; }
	}

	public abstract class CoherentCacheNode
	{
		public CoherentCacheNode (ICycleProvider cycleProvider, string name)
		{
			this.CycleProvider = cycleProvider;
			this.Name = name;
			
			this.EventQueue = new DelegateEventQueue ();
			this.CycleProvider.EventProcessors.Add (this.EventQueue);
		}

		protected void Schedule (Action action, ulong delay)
		{
			this.EventQueue.Schedule (action, delay);
		}

		public delegate void FindAndLockDelegate (bool hasError, uint @set, uint way, MESIState state, uint tag, DirectoryLock dirLock);

		public virtual void FindAndLock (uint addr, bool isBlocking, bool isRead, bool isRetry, FindAndLockDelegate onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void Load (uint addr, bool isRetry, Action onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void Store (uint addr, bool isRetry, Action onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void Evict (uint @set, uint way, Action<bool> onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void EvictReceive (CoherentCacheNode source, uint addr, bool isWriteback, Action<bool> onReceiveReplyCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void ReadRequest (CoherentCacheNode target, uint addr, Action<bool, bool> onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void ReadRequestReceive (CoherentCacheNode source, uint addr, Action<bool, bool> onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void WriteRequest (CoherentCacheNode target, uint addr, Action<bool> onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void WriteRequestReceive (CoherentCacheNode source, uint addr, Action<bool> onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public virtual void Invalidate (CoherentCacheNode except, uint @set, uint way, Action onCompletedCallback)
		{
			throw new NotImplementedException ();
		}

		public override string ToString ()
		{
			return string.Format ("[CoherentCacheNode: Name={0}, Level={1}, Next={3}, EventQueue={4}]", this.Name, this.Level, this.Next, this.EventQueue);
		}

		public abstract uint Level { get; }
			
		public ICycleProvider CycleProvider { get; private set; }
		public string Name { get; private set; }
		public CoherentCacheNode Next { get; set; }
		private DelegateEventQueue EventQueue { get; set; }
	}

	public sealed class Sequencer : CoherentCacheNode
	{
		public Sequencer (string name, CoherentCache l1Cache) : base(l1Cache.CycleProvider, name)
		{
			this.L1Cache = l1Cache;
		}

		public void Load (uint addr, bool isRetry, ReorderBufferEntry reorderBufferEntry, Action<ReorderBufferEntry> onCompletedCallback)
		{
			this.Load (addr, isRetry, delegate() { onCompletedCallback (reorderBufferEntry); });
		}

		public override void Load (uint addr, bool isRetry, Action onCompletedCallback)
		{
			this.L1Cache.Load (addr, isRetry, onCompletedCallback);
		}

		public override void Store (uint addr, bool isRetry, Action onCompletedCallback)
		{
			this.L1Cache.Store (addr, isRetry, onCompletedCallback);
		}

		public override string ToString ()
		{
			return string.Format ("[Sequencer: Name={0}]", this.Name);
		}

		public uint GetBlockAddress (uint addr)
		{
			return this.L1Cache.Cache.GetTag (addr);
		}

		public uint BlockSize {
			get { return this.L1Cache.Cache.BlockSize; }
		}

		public override uint Level {
			get {
				throw new NotImplementedException ();
			}
		}

		public CoherentCache L1Cache { get; private set; }
	}

	public sealed class CoherentCache : CoherentCacheNode
	{		
		public CoherentCache (ICycleProvider cycleProvider, CacheConfig config, CacheStat stat) : base(cycleProvider, config.Name)
		{
			this.Config = config;
			this.Stat = stat;
			this.Cache = new Cache (this);
			
			this.Random = new Random ();
		}

		public override void FindAndLock (uint addr, bool isBlocking, bool isRead, bool isRetry, FindAndLockDelegate onCompletedCallback)
		{
			uint @set, way, tag;
			MESIState state;
			
			bool hasHit = this.Cache.FindBlock (addr, out @set, out way, out tag, out state, true);
			
			this.Stat.Accesses++;
			if (hasHit) {
				this.Stat.Hits++;
			}
			if (isRead) {
				this.Stat.Reads++;
				
				if (isBlocking) {
					this.Stat.BlockingReads++;
				} else {
					this.Stat.NonblockingReads++;
				}
				
				if (hasHit) {
					this.Stat.ReadHits++;
				}
			} else {
				this.Stat.Writes++;
				
				if (isBlocking) {
					this.Stat.BlockingWrites++;
				} else {
					this.Stat.NonblockingWrites++;
				}
				
				if (hasHit) {
					this.Stat.WriteHits++;
				}
			}
			
			if (!isRetry) {
				this.Stat.NoRetryAccesses++;
				
				if (hasHit) {
					this.Stat.NoRetryHits++;
				}
				
				if (isRead) {
					this.Stat.NoRetryReads++;
					
					if (hasHit) {
						this.Stat.NoRetryReadHits++;
					}
				} else {
					this.Stat.NoRetryWrites++;
					
					if (hasHit) {
						this.Stat.NoRetryWriteHits++;
					}
				}
			}
			
			uint dumbTag;
			
			if (!hasHit) {
				way = this.Cache.ReplaceBlock (@set);
				this.Cache.GetBlock (@set, way, out dumbTag, out state);
			}
			
			DirectoryLock dirLock = this.Cache.Directory.Locks[(int)@set];
			if (!dirLock.Lock ()) {
				if (isBlocking) {
					onCompletedCallback (true, @set, way, state, tag, dirLock);
				} else {
					this.Retry (delegate() { this.FindAndLock (addr, isBlocking, isRead, true, onCompletedCallback); });
				}
			} else {
				this.Cache[@set][way].TransientTag = tag;
				
				if (!hasHit && state != MESIState.Invalid) {
					
					this.Schedule (delegate() { this.Evict (@set, way, delegate(bool hasError) {uint dumbTag1;if (!hasError) {this.Stat.Evictions++;this.Cache.GetBlock (@set, way, out dumbTag1, out state);onCompletedCallback (false, @set, way, state, tag, dirLock);} else {this.Cache.GetBlock (@set, way, out dumbTag, out state);dirLock.Unlock ();onCompletedCallback (true, @set, way, state, tag, dirLock);}}); }, this.HitLatency);
				} else {
					this.Schedule (delegate() { onCompletedCallback (false, @set, way, state, tag, dirLock); }, this.HitLatency);
				}
			}
		}

		public override void Load (uint addr, bool isRetry, Action onCompletedCallback)
		{
			this.FindAndLock (addr, false, true, isRetry, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirectoryLock dirLock) {
				if (!hasError) {
					if (!IsReadHit (state)) {
						this.ReadRequest (this.Next, tag, delegate(bool hasError1, bool isShared) {
							if (!hasError1) {
								this.Cache.SetBlock (@set, way, tag, isShared ? MESIState.Shared : MESIState.Exclusive);
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

		public override void Store (uint addr, bool isRetry, Action onCompletedCallback)
		{
			this.FindAndLock (addr, false, false, isRetry, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirectoryLock dirLock) {
				if (!hasError) {
					if (!IsWriteHit (state)) {
						this.WriteRequest (this.Next, tag, delegate(bool hasError1) {
							if (!hasError1) {
								this.Cache.AccessBlock (@set, way);
								this.Cache.SetBlock (@set, way, tag, MESIState.Modified);
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
						this.Cache.SetBlock (@set, way, tag, MESIState.Modified);
						dirLock.Unlock ();
						onCompletedCallback ();
					}
				} else {
					this.Stat.WriteRetries++;
					this.Retry (delegate() { this.Store (addr, true, onCompletedCallback); });
				}
			});
		}

		public override void Evict (uint @set, uint way, Action<bool> onCompletedCallback)
		{
			uint tag;
			MESIState state;
			
			this.Cache.GetBlock (@set, way, out tag, out state);
			
			uint srcSet = @set;
			//TODO: is it necessary or bug?
			uint srcWay = way;
			uint srcTag = tag;
			CoherentCacheNode target = this.Next;
			
			this.Invalidate (null, @set, way, delegate() {
				if (state == MESIState.Invalid) {
					onCompletedCallback (false);
				} else if (state == MESIState.Modified) {
					this.Schedule (delegate() { target.EvictReceive (this, srcTag, true, delegate(bool hasError) { this.Schedule (delegate() { this.EvictReplyReceive (hasError, srcSet, srcWay, onCompletedCallback); }, 2); }); }, 2);
				} else {
					this.Schedule (delegate() { target.EvictReceive (this, srcTag, false, delegate(bool hasError) { this.Schedule (delegate() { this.EvictReplyReceive (hasError, srcSet, srcWay, onCompletedCallback); }, 2); }); }, 2);
				}
			});
		}

		public override void EvictReceive (CoherentCacheNode source, uint addr, bool isWriteback, Action<bool> onReceiveReplyCallback)
		{
			this.FindAndLock (addr, false, false, false, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirectoryLock dirLock) {
				if (!hasError) {
					if (!isWriteback) {
						this.EvictProcess (source, @set, way, dirLock, onReceiveReplyCallback);
					} else {
						this.Invalidate (source, @set, way, delegate() {
							if (state == MESIState.Shared) {
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

		private void EvictWritebackFinish (CoherentCacheNode source, bool hasError, uint @set, uint way, uint tag, DirectoryLock dirLock, Action<bool> onReceiveReplyCallback)
		{
			if (!hasError) {
				this.Cache.SetBlock (@set, way, tag, MESIState.Modified);
				this.Cache.AccessBlock (@set, way);
				this.EvictProcess (source, @set, way, dirLock, onReceiveReplyCallback);
			} else {
				dirLock.Unlock ();
				onReceiveReplyCallback (true);
			}
		}

		private void EvictProcess (CoherentCacheNode source, uint @set, uint way, DirectoryLock dirLock, Action<bool> onReceiveReplyCallback)
		{
			DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
			dirEntry.UnsetSharer (source);
			if (dirEntry.Owner == source) {
				dirEntry.Owner = null;
			}
			dirLock.Unlock ();
			onReceiveReplyCallback (false);
		}

		private void EvictReplyReceive (bool hasError, uint srcSet, uint srcWay, Action<bool> onCompletedCallback)
		{
			this.Schedule (delegate() {
				if (!hasError) {
					this.Cache.SetBlock (srcSet, srcWay, 0, MESIState.Invalid);
				}
				onCompletedCallback (hasError);
			}, 2);
		}

		public override void ReadRequest (CoherentCacheNode target, uint addr, Action<bool, bool> onCompletedCallback)
		{
			this.Schedule (delegate() { target.ReadRequestReceive (this, addr, onCompletedCallback); }, 2);
		}

		public override void ReadRequestReceive (CoherentCacheNode source, uint addr, Action<bool, bool> onCompletedCallback)
		{
			this.FindAndLock (addr, this.Next == source, true, false, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirectoryLock dirLock) {
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

		private void ReadRequestUpdown (CoherentCacheNode source, uint @set, uint way, uint tag, MESIState state, DirectoryLock dirLock, Action<bool, bool> onCompletedCallback)
		{
			uint pending = 1;
			
			if (state != MESIState.Invalid) {
				DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
				
				if (dirEntry.Owner != null && dirEntry.Owner != source) {
					pending++;
					this.ReadRequest (dirEntry.Owner, tag, delegate(bool hasError, bool isShared) { this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback); });
				}
				
				this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback);
			} else {
				this.ReadRequest (this.Next, tag, delegate(bool hasError, bool isShared) {
					if (!hasError) {
						this.Cache.SetBlock (@set, way, tag, isShared ? MESIState.Shared : MESIState.Exclusive);
						this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback);
					} else {
						dirLock.Unlock ();
						this.Schedule (delegate() { onCompletedCallback (true, false); }, 2);
					}
				});
			}
		}

		private void ReadRequestUpdownFinish (CoherentCacheNode source, uint @set, uint way, DirectoryLock dirLock, ref uint pending, Action<bool, bool> onCompletedCallback)
		{
			pending--;
			
			if (pending == 0) {
				DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
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

		private void ReadRequestDownup (uint @set, uint way, uint tag, DirectoryLock dirLock, Action<bool, bool> onCompletedCallback)
		{
			uint pending = 1;
			
			DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
			if (dirEntry.Owner != null) {
				pending++;
				
				this.ReadRequest (dirEntry.Owner, tag, delegate(bool hasError, bool isShared) { this.ReadRequestDownupFinish (@set, way, tag, dirLock, ref pending, onCompletedCallback); });
			}
			
			this.ReadRequestDownupFinish (@set, way, tag, dirLock, ref pending, onCompletedCallback);
		}

		private void ReadRequestDownupFinish (uint @set, uint way, uint tag, DirectoryLock dirLock, ref uint pending, Action<bool, bool> onCompletedCallback)
		{
			pending--;
			
			if (pending == 0) {
				DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
				dirEntry.Owner = null;
				
				this.Cache.SetBlock (@set, way, tag, MESIState.Shared);
				this.Cache.AccessBlock (@set, way);
				dirLock.Unlock ();
				this.Schedule (delegate() { onCompletedCallback (false, false); }, 2);
			}
		}

		public override void WriteRequest (CoherentCacheNode target, uint addr, Action<bool> onCompletedCallback)
		{
			this.Schedule (delegate() { target.WriteRequestReceive (this, addr, onCompletedCallback); }, 2);
		}

		public override void WriteRequestReceive (CoherentCacheNode source, uint addr, Action<bool> onCompletedCallback)
		{
			this.FindAndLock (addr, this.Next == source, false, false, delegate(bool hasError, uint @set, uint way, MESIState state, uint tag, DirectoryLock dirLock) {
				if (!hasError) {
					this.Invalidate (source, @set, way, delegate() {
						if (source.Next == this) {
							if (state == MESIState.Modified || state == MESIState.Exclusive) {
								this.WriteRequestUpdownFinish (source, false, @set, way, tag, state, dirLock, onCompletedCallback);
							} else {
								this.WriteRequest (this.Next, tag, delegate(bool hasSError) { this.WriteRequestUpdownFinish (source, hasError, @set, way, tag, state, dirLock, onCompletedCallback); });
							}
						} else {
							this.Cache.SetBlock (@set, way, 0, MESIState.Invalid);
							dirLock.Unlock ();
							this.Schedule (delegate() { onCompletedCallback (false); }, 2);
						}
					});
				} else {
					this.Schedule (delegate() { onCompletedCallback (true); }, 2);
				}
			});
		}

		private void WriteRequestUpdownFinish (CoherentCacheNode source, bool hasError, uint @set, uint way, uint tag, MESIState state, DirectoryLock dirLock, Action<bool> onCompletedCallback)
		{
			if (!hasError) {
				DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
				dirEntry.SetSharer (source);
				dirEntry.Owner = source;
				
				this.Cache.AccessBlock (@set, way);
				if (state != MESIState.Modified) {
					this.Cache.SetBlock (@set, way, tag, MESIState.Exclusive);
				}
				
				dirLock.Unlock ();
				this.Schedule (delegate() { onCompletedCallback (false); }, 2);
			} else {
				dirLock.Unlock ();
				this.Schedule (delegate() { onCompletedCallback (true); }, 2);
			}
		}

		public override void Invalidate (CoherentCacheNode except, uint @set, uint way, Action onCompletedCallback)
		{
			uint tag;
			MESIState state;
			
			this.Cache.GetBlock (@set, way, out tag, out state);
			
			uint pending = 1;
			
			DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
			
			foreach (var sharer in dirEntry.Sharers.FindAll (sharer => sharer != except)) {
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

		private void Retry (Action action)
		{
			ulong retryLatency = (ulong)(this.HitLatency + (this.Random.Next (0, (int)(this.HitLatency + 2))));
			this.Schedule (action, retryLatency);
		}

		public override uint Level {
			get { return this.Config.Level; }
		}

		public uint HitLatency {
			get { return this.Config.HitLatency; }
		}

		public Cache Cache { get; private set; }
		public CacheConfig Config { get; private set; }
		public CacheStat Stat { get; private set; }

		private Random Random { get; set; }
		
		public static bool IsReadHit (MESIState state)
		{
			return state != MESIState.Invalid;
		}

		public static bool IsWriteHit (MESIState state)
		{
			return state == MESIState.Modified || state == MESIState.Exclusive;
		}
	}

	public sealed class MemoryController : CoherentCacheNode
	{
		public MemoryController (ICycleProvider cycleProvider, MainMemoryConfig config, MainMemoryStat stat) : base(cycleProvider, "mem")
		{
			this.Config = config;
			this.Stat = stat;
		}

		public override void EvictReceive (CoherentCacheNode source, uint addr, bool isWriteback, Action<bool> onReceiveReplyCallback)
		{
			this.Stat.Accesses++;
			this.Stat.Writes++;
			
			this.Schedule (delegate() { onReceiveReplyCallback (false); }, this.Latency);
		}

		public override void ReadRequestReceive (CoherentCacheNode source, uint addr, Action<bool, bool> onCompletedCallback)
		{
			this.Stat.Accesses++;
			this.Stat.Reads++;
			
			this.Schedule (delegate() { onCompletedCallback (false, false); }, this.Latency);
		}

		public override void WriteRequestReceive (CoherentCacheNode source, uint addr, Action<bool> onCompletedCallback)
		{
			this.Stat.Accesses++;
			this.Stat.Writes++;
			
			this.Schedule (delegate() { onCompletedCallback (false); }, this.Latency);
		}

		public override uint Level {
			get {
				throw new NotImplementedException ();
			}
		}

		public uint Latency {
			get { return this.Config.Latency; }
		}

		public MainMemoryConfig Config { get; private set; }
		public MainMemoryStat Stat { get; private set; }
	}
}
