/*
 * MemoryHierarchy.cs
 * 
 * Copyright © 2010 Min Cai (min.cai.china@gmail.com). 
 * 
 * This file is part of the FleximSharp multicore architectural simulator.
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
 * along with FleximSharp.  If not, see <http ://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.Microarchitecture;

namespace MinCai.Simulators.Flexim.MemoryHierarchy
{
	public class CacheGeometry
	{
		public CacheGeometry (uint size, uint associativity, uint lineSize)
		{
			this.Size = size;
			this.Associativity = associativity;
			this.LineSize = lineSize;
		}

		public uint Size { get; private set; }
		public uint Associativity { get; private set; }
		public uint LineSize { get; private set; }

		public uint LineSizeInLog2 {
			get { return (uint)Math.Log (this.LineSize, 2); }
		}

		public uint LineMask {
			get { return this.LineSize - 1; }
		}

		public uint NumSets {
			get { return this.Size / this.Associativity / this.LineSize; }
		}
	}

	public static class CacheGeometryExtensions
	{
		public static uint GetDisplacement (this uint addr, CacheGeometry cacheGeometry)
		{
			return addr & (cacheGeometry.LineMask);
		}

		public static uint GetTag (this uint addr, CacheGeometry cacheGeometry)
		{
			return addr & ~(cacheGeometry.LineMask);
		}

		public static uint GetIndex (this uint addr, CacheGeometry cacheGeometry)
		{
			return (addr >> (int)cacheGeometry.LineSizeInLog2) % cacheGeometry.NumSets;
		}

		public static bool IsAligned (this uint addr, CacheGeometry cacheGeometry)
		{
			return addr.GetDisplacement (cacheGeometry) == 0;
		}
	}

	public static class MemoryConstants
	{
		static MemoryConstants ()
		{
			MemoryGeometry = new CacheGeometry (1 << 22, 1, 1 << 12);
		}

		public static uint GetDisplacement (this uint addr)
		{
			return addr.GetDisplacement (MemoryGeometry);
		}

		public static uint GetTag (this uint addr)
		{
			return addr.GetTag (MemoryGeometry);
		}

		public static uint GetIndex (this uint addr)
		{
			return addr.GetIndex (MemoryGeometry);
		}

		public static bool IsAligned (this uint addr)
		{
			return addr.IsAligned (MemoryGeometry);
		}

		public static uint LOG_PAGE_SIZE {
			get { return MemoryGeometry.LineSizeInLog2; }
		}

		public static uint PAGE_SIZE {
			get { return MemoryGeometry.LineSize; }
		}

		public static uint PAGE_MASK {
			get { return MemoryGeometry.LineMask; }
		}

		public static uint PAGE_COUNT {
			get { return MemoryGeometry.NumSets; }
		}

		public static CacheGeometry MemoryGeometry;
	}

	[Flags]
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
		public MemoryPage (uint tag, MemoryAccessType permission)
		{
			this.Tag = tag;
			this.Permission = permission;
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

		public uint Addr { get; private set; }
	}

	public sealed class Memory
	{
		public Memory ()
		{
			this.pages = new Dictionary<uint, MemoryPage> ();
		}

		public void WriteByte (uint addr, byte data)
		{
			byte[] dataToWrite = new byte[] { data };
			this.Access (addr, 1, ref dataToWrite, MemoryAccessType.Write);
		}

		public void WriteHalfWord (uint addr, ushort data)
		{
			byte[] dataToWrite = BitConverter.GetBytes (data);
			this.Access (addr, 2, ref dataToWrite, MemoryAccessType.Write);
		}

		public void WriteWord (uint addr, uint data)
		{
			byte[] dataToWrite = BitConverter.GetBytes (data);
			this.Access (addr, 4, ref dataToWrite, MemoryAccessType.Write);
		}

		public void WriteDoubleWord (uint addr, ulong data)
		{
			byte[] dataToWrite = BitConverter.GetBytes (data);
			this.Access (addr, 8, ref dataToWrite, MemoryAccessType.Write);
		}

		public void WriteBlock (uint addr, uint size, byte[] data)
		{
			this.Access (addr, (int)size, ref data, MemoryAccessType.Write);
		}

		public int WriteString (uint addr, string str)
		{
			int bytesCount;
			byte[] dataToWrite = StringHelper.StringToBytes (str, out bytesCount);
			this.WriteBlock (addr, (uint)bytesCount, dataToWrite);
			return bytesCount;
		}

		public byte ReadByte (uint addr)
		{
			byte[] data = new byte[1];
			this.Access (addr, 1, ref data, MemoryAccessType.Read);
			return data[0];
		}

		public ushort ReadHalfWord (uint addr)
		{
			byte[] data = new byte[2];
			this.Access (addr, 2, ref data, MemoryAccessType.Read);
			return BitConverter.ToUInt16 (data, 0);
		}

		public uint ReadWord (uint addr)
		{
			byte[] data = new byte[4];
			this.Access (addr, 4, ref data, MemoryAccessType.Read);
			return BitConverter.ToUInt32 (data, 0);
		}

		public ulong ReadDoubleWord (uint addr)
		{
			byte[] data = new byte[8];
			this.Access (addr, 8, ref data, MemoryAccessType.Read);
			return BitConverter.ToUInt64 (data, 0);
		}

		public byte[] ReadBlock (uint addr, int size)
		{
			byte[] data = new byte[size];
			this.Access (addr, size, ref data, MemoryAccessType.Read);
			return data;
		}

		public string ReadString (uint addr, int size)
		{
			byte[] data = ReadBlock (addr, size);
			return StringHelper.BytesToString (data);
		}

		public void Zero (uint addr, int size)
		{
			byte[] data = Enumerable.Repeat<byte> (0, size).ToArray ();
			this.Access (addr, size, ref data, MemoryAccessType.Write);
		}

		public void InitBlock (uint addr, int size, byte[] data)
		{
			this.Access (addr, (int)size, ref data, MemoryAccessType.Init);
		}

		private void Access (uint addr, int size, ref byte[] buf, MemoryAccessType access)
		{
			uint offset = 0;
			
			while (size > 0) {
				int chunksize = Math.Min (size, (int)(MemoryConstants.PAGE_SIZE - addr.GetDisplacement ()));
				this.AccessPageBoundary (addr, chunksize, ref buf, offset, access);
				
				size -= chunksize;
				offset += (uint)chunksize;
				addr += (uint)chunksize;
			}
		}

		public void Map (uint addr, int size, MemoryAccessType permission)
		{
			for (uint tag = addr.GetTag (); tag <= ((uint)(addr + size - 1)).GetTag (); tag += MemoryConstants.PAGE_SIZE) {
				MemoryPage page = this.GetPage (tag);
				if (page == null) {
					page = this.AddPage (tag, permission);
					page.Permission |= permission;
				}
			}
		}

		public void Unmap (uint addr, int size)
		{
			Debug.Assert (addr.IsAligned ());
			Debug.Assert (((uint)size).IsAligned ());
			
			for (uint tag = addr.GetTag (); tag <= ((uint)(addr + size - 1)).GetTag (); tag += MemoryConstants.PAGE_SIZE) {
				if (this.GetPage (tag) != null) {
					this.RemovePage (tag);
				}
			}
		}

		private MemoryPage GetPage (uint addr)
		{
			MemoryPage page = this[addr.GetIndex ()];
			MemoryPage prev = null;
			
			while (page != null && page.Tag != addr.GetTag ()) {
				prev = page;
				page = page.Next;
			}
			
			if (prev != null && page != null) {
				prev.Next = page.Next;
				page.Next = this[addr.GetIndex ()];
				this[addr.GetIndex ()] = page;
			}
			
			return page;
		}

		private MemoryPage AddPage (uint addr, MemoryAccessType permission)
		{
			MemoryPage page = new MemoryPage (addr.GetTag (), permission);
			
			page.Next = this[addr.GetIndex ()];
			this[addr.GetIndex ()] = page;
			this.mappedSpace += MemoryConstants.PAGE_SIZE;
			this.maxMappedSpace = Math.Max (this.maxMappedSpace, this.mappedSpace);
			
			return page;
		}

		private void RemovePage (uint addr)
		{
			MemoryPage prev = null;
			
			MemoryPage page = this[addr.GetIndex ()];
			while (page != null && page.Tag != addr.GetTag ()) {
				prev = page;
				page = page.Next;
			}
			
			if (page == null) {
				return;
			}
			
			if (prev != null) {
				prev.Next = page.Next;
			} else {
				this[addr.GetIndex ()] = page.Next;
			}
			
			this.mappedSpace -= MemoryConstants.PAGE_SIZE;
			
			page = null;
		}

		private void AccessPageBoundary (uint addr, int size, ref byte[] buf, uint offset, MemoryAccessType access)
		{
			MemoryPage page = this.GetPage (addr);
			
			if (page == null) {
				throw new SegmentationFaultException (addr);
			}
			if ((page.Permission & access) != access) {
				Logger.Fatalf (Logger.Categories.Memory, "Memory.accessPageBoundary: permission denied at 0x{0:x8}, page.Permission: 0x{1:x8}, access: 0x{2:x8}", addr, page.Permission, access);
			}
			
			Debug.Assert (addr.GetDisplacement () + size <= MemoryConstants.PAGE_SIZE);
			
			switch (access) {
			case MemoryAccessType.Read:
			case MemoryAccessType.Execute:
				Array.Copy (page.Data, addr.GetDisplacement (), buf, offset, size);
				break;
			case MemoryAccessType.Write:
			case MemoryAccessType.Init:
				Array.Copy (buf, offset, page.Data, addr.GetDisplacement (), size);
				break;
			default:
				Logger.Panic (Logger.Categories.Memory, "Memory.accessPageBoundary: unknown access");
				break;
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
	}

	public sealed class MemoryManagementUnit
	{
		private sealed class Page
		{
			public Page ()
			{
			}

			public uint MemoryMapId { get; set; }
			public uint VirtualAddress { get; set; }
			public uint PhysicalAddress { get; set; }
			public Page Next { get; set; }
		}

		public MemoryManagementUnit ()
		{
			this.Pages = new Dictionary<uint, Page> ();
		}

		private Page GetPage (uint memoryMapId, uint virtualAddress)
		{
			uint pageIndex = GetPageIndex (memoryMapId, virtualAddress);
			uint tag = GetTag (virtualAddress);
			
			Page prev = null;
			Page page = this[pageIndex];
			
			while (page != null) {
				if (page.VirtualAddress == tag && page.MemoryMapId == memoryMapId)
					break;
				prev = page;
				page = page.Next;
			}
			
			if (page == null) {
				page = new Page ();
				page.MemoryMapId = memoryMapId;
				page.VirtualAddress = tag;
				page.PhysicalAddress = (uint)this.PageCount << (int)MemoryConstants.LOG_PAGE_SIZE;
				
				this.PageCount++;
				page.Next = this[pageIndex];
				this[pageIndex] = page;
				prev = null;
			}
			
			if (prev != null) {
				prev.Next = page.Next;
				page.Next = this[pageIndex];
				this[pageIndex] = page;
			}
			
			return page;
		}

		public uint GetPhysicalAddress (uint memoryMapId, uint virtualAddress)
		{
			Page page = this.GetPage (memoryMapId, virtualAddress);
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

		private static uint GetPageIndex (uint memoryMapId, uint virtualAddress)
		{
			return ((virtualAddress >> (int)MemoryConstants.LOG_PAGE_SIZE) + memoryMapId * 23) % MemoryConstants.PAGE_COUNT;
		}

		private static uint GetTag (uint virtualAddress)
		{
			return virtualAddress & ~MemoryConstants.PAGE_MASK;
		}

		private static uint GetOffset (uint virtualAddress)
		{
			return virtualAddress & MemoryConstants.PAGE_MASK;
		}

		static MemoryManagementUnit ()
		{
			CurrentMemoryMapId = 0;
		}

		public static uint CurrentMemoryMapId;
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
			
			this.Geometry = new CacheGeometry (this.NumSets * this.Assoc * this.BlockSize, this.Assoc, this.BlockSize);
			
			this.Sets = new List<CacheSet> ();
			for (uint i = 0; i < this.NumSets; i++) {
				this.Sets.Add (new CacheSet (this, this.Assoc, i));
			}
			
			this.Directory = new Directory (this.NumSets, this.Assoc);
		}

		public CacheGeometry Geometry { get; private set; }

		public CacheBlock GetBlock (uint addr, bool checkTransientTag)
		{
			uint tag = addr.GetTag (this.Geometry);
			uint @set = addr.GetIndex (this.Geometry);
			
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
			@set = addr.GetIndex (this.Geometry);
			tag = addr.GetTag (this.Geometry);
			
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
			return string.Format ("[CoherentCacheNode: Name={0}, Next={1}, EventQueue={2}]", this.Name, this.Next, this.EventQueue);
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
			return addr.GetTag (this.L1Cache.Cache.Geometry);
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
