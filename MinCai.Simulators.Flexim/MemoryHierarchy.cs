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
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.Microarchitecture;

namespace MinCai.Simulators.Flexim.MemoryHierarchy
{
	public sealed partial class CacheGeometry
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
			
			Page prev = null;
			Page page = this[pageIndex];
			
			while (page != null) {
				if (page.VirtualAddress == GetTag (virtualAddress) && page.MemoryMapId == memoryMapId)
					break;
				prev = page;
				page = page.Next;
			}
			
			if (page == null) {
				page = new Page ();
				page.MemoryMapId = memoryMapId;
				page.VirtualAddress = GetTag (virtualAddress);
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

	public sealed class CacheLine
	{
		public CacheLine (CacheSet @set, uint way)
		{
			this.Set = @set;
			this.Way = way;
			
			this.Tag = 0;
			this.TransientTag = 0;
			this.State = MESIState.Invalid;
			
			this.LastAccessCycle = 0;
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
		public CacheSet (Cache cache, uint associativity, uint num)
		{
			this.Cache = cache;
			this.Associativity = associativity;
			this.Num = num;
			
			this.Lines = new List<CacheLine> ();
			for (uint i = 0; i < this.Associativity; i++) {
				this.Lines.Add (new CacheLine (this, i));
			}
		}

		public CacheLine this[uint index] {
			get { return this.Lines[(int)index]; }
		}

		public uint Associativity { get; private set; }
		public List<CacheLine> Lines { get; private set; }

		public Cache Cache { get; private set; }

		public uint Num { get; private set; }
	}

	public sealed class Cache
	{
		public Cache (ICycleProvider cycleProvider, CacheGeometry geometry)
		{
			this.CycleProvider = cycleProvider;
			this.Geometry = geometry;
			
			this.Sets = new List<CacheSet> ();
			for (uint i = 0; i < this.NumSets; i++) {
				this.Sets.Add (new CacheSet (this, this.Associativity, i));
			}
			
			this.Directory = new Directory (this.NumSets, this.Associativity);
		}

		private CacheLine GetLine (uint addr, bool checkTransientTag)
		{
			uint tag = addr.GetTag (this.Geometry);
			uint @set = addr.GetIndex (this.Geometry);
			
			for (uint way = 0; way < this[@set].Associativity; way++) {
				CacheLine line = this[@set][way];
				
				if ((line.Tag == tag && line.State != MESIState.Invalid) || (checkTransientTag && line.TransientTag == tag && this.Directory.Locks[(int)@set].IsLocked)) {
					return line;
				}
			}
			
			return null;
		}

		public bool FindLine (uint addr, out uint @set, out uint way, out uint tag, out MESIState state, bool checkTransientTag)
		{
			@set = addr.GetIndex (this.Geometry);
			tag = addr.GetTag (this.Geometry);
			
			CacheLine lineFound = this.GetLine (addr, checkTransientTag);
			
			way = lineFound != null ? lineFound.Way : 0;
			state = lineFound != null ? lineFound.State : MESIState.Invalid;
			
			return lineFound != null;
		}

		public void SetLine (uint @set, uint way, uint tag, MESIState state)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			Debug.Assert (way >= 0 && way < this.Associativity);
			
			this.AccessLine (@set, way);
			this[@set][way].Tag = tag;
			this[@set][way].State = state;
		}

		public void GetLine (uint @set, uint way, out uint tag, out MESIState state)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			Debug.Assert (way >= 0 && way < this.Associativity);
			
			tag = this[@set][way].Tag;
			state = this[@set][way].State;
		}

		public void AccessLine (uint @set, uint way)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			Debug.Assert (way >= 0 && way < this.Associativity);
			
			this[@set][way].LastAccessCycle = this.CycleProvider.CurrentCycle;
		}

		public uint FindVictimToEvict (uint @set)
		{
			Debug.Assert (@set >= 0 && @set < this.NumSets);
			
			ulong smallestTime = this[@set][0].LastAccessCycle;
			uint smallestIndex = 0;
			
			for (uint way = 0; way < this[@set].Associativity; way++) {
				CacheLine line = this[@set][way];
				
				ulong time = line.LastAccessCycle;
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

		public CacheGeometry Geometry { get; private set; }

		public uint NumSets {
			get { return this.Geometry.NumSets; }
		}
		public uint Associativity {
			get { return this.Geometry.Associativity; }
		}
		public uint LineSize {
			get { return this.Geometry.LineSize; }
		}

		public List<CacheSet> Sets { get; private set; }
		public Directory Directory { get; private set; }

		public ICycleProvider CycleProvider { get; private set; }
	}

	public sealed class TranslationLookasideBuffer
	{
		public TranslationLookasideBuffer (ICycleProvider cycleProvider, TlbConfig config, TlbStat stat)
		{
			this.CycleProvider = cycleProvider;
			this.Config = config;
			this.Stat = stat;
			
			this.Cache = new Cache (cycleProvider, config.Geometry);
			
			this.EventQueue = new ActionEventQueue ();
			this.CycleProvider.EventProcessors.Add (this.EventQueue);
		}

		public void Access (uint addr, Action onCompletedCallback)
		{
			uint @set, way, tag;
			
			uint dumbTag;
			MESIState dumbState;
			bool hit = this.Cache.FindLine (addr, out @set, out way, out dumbTag, out dumbState, false);
			
			this.Stat.Accesses++;
			if (hit) {
				this.Stat.Hits++;
			}
			
			if (!hit) {
				@set = addr.GetIndex (this.Cache.Geometry);
				tag = addr.GetTag (this.Cache.Geometry);
				way = this.Cache.FindVictimToEvict (@set);
				
				uint dumbTag1;
				MESIState state;
				this.Cache.GetLine (@set, way, out dumbTag1, out state);
				if (state != MESIState.Invalid) {
					this.Stat.Evictions++;
				}
				this.Cache.SetLine (@set, way, tag, MESIState.Modified);
			}
			
			this.Cache.AccessLine (@set, way);
			this.EventQueue.Schedule (onCompletedCallback, hit ? this.Config.HitLatency : this.Config.MissLatency);
		}

		public ICycleProvider CycleProvider { get; private set; }
		public TlbConfig Config { get; private set; }
		public TlbStat Stat { get; private set; }

		public Cache Cache { get; private set; }

		public ActionEventQueue EventQueue { get; private set; }
	}

	public abstract class CoherentCacheNode
	{
		public CoherentCacheNode (ICycleProvider cycleProvider, string name)
		{
			this.CycleProvider = cycleProvider;
			this.Name = name;
			
			this.EventQueue = new ActionEventQueue ();
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

		public ICycleProvider CycleProvider { get; private set; }
		public string Name { get; private set; }
		public CoherentCacheNode Next { get; set; }
		private ActionEventQueue EventQueue { get; set; }
	}

	public sealed class CoherentCache : CoherentCacheNode
	{
		public CoherentCache (ICycleProvider cycleProvider, CacheConfig config, CacheStat stat) : base(cycleProvider, config.Name)
		{
			this.Config = config;
			this.Stat = stat;
			this.Cache = new Cache (cycleProvider, config.Geometry);
			
			this.Random = new Random ();
		}

		public override void FindAndLock (uint addr, bool isBlocking, bool isRead, bool isRetry, FindAndLockDelegate onCompletedCallback)
		{
			uint @set, way, tag;
			MESIState state;
			
			bool hasHit = this.Cache.FindLine (addr, out @set, out way, out tag, out state, true);
			
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
				way = this.Cache.FindVictimToEvict (@set);
				this.Cache.GetLine (@set, way, out dumbTag, out state);
			}
			
			DirectoryLock dirLock = this.Cache.Directory.Locks[(int)@set];
			if (!dirLock.Lock ()) {
				if (isBlocking) {
					onCompletedCallback (true, @set, way, state, tag, dirLock);
				} else {
					this.Retry (() => this.FindAndLock (addr, isBlocking, isRead, true, onCompletedCallback));
				}
			} else {
				this.Cache[@set][way].TransientTag = tag;
				
				if (!hasHit && state != MESIState.Invalid) {
					this.Schedule (() => { this.Evict (@set, way, hasError =>{uint dumbTag1;if (!hasError) {this.Stat.Evictions++;this.Cache.GetLine (@set, way, out dumbTag1, out state);onCompletedCallback (false, @set, way, state, tag, dirLock);} else {this.Cache.GetLine (@set, way, out dumbTag, out state);dirLock.Unlock ();onCompletedCallback (true, @set, way, state, tag, dirLock);}}); }, this.HitLatency);
				} else {
					this.Schedule (() => onCompletedCallback (false, @set, way, state, tag, dirLock), this.HitLatency);
				}
			}
		}

		public override void Load (uint addr, bool isRetry, Action onCompletedCallback)
		{
			this.FindAndLock (addr, false, true, isRetry, (hasError, @set, way, state, tag, dirLock) =>
			{
				if (!hasError) {
					if (!IsReadHit (state)) {
						this.ReadRequest (this.Next, tag, (hasError1, isShared) =>
						{
							if (!hasError1) {
								this.Cache.SetLine (@set, way, tag, isShared ? MESIState.Shared : MESIState.Exclusive);
								this.Cache.AccessLine (@set, way);
								dirLock.Unlock ();
								onCompletedCallback ();
							} else {
								this.Stat.ReadRetries++;
								dirLock.Unlock ();
								this.Retry (() => this.Load (addr, true, onCompletedCallback));
							}
						});
					} else {
						this.Cache.AccessLine (@set, way);
						dirLock.Unlock ();
						onCompletedCallback ();
					}
				} else {
					this.Stat.ReadRetries++;
					this.Retry (() => this.Load (addr, true, onCompletedCallback));
				}
			});
		}

		public override void Store (uint addr, bool isRetry, Action onCompletedCallback)
		{
			this.FindAndLock (addr, false, false, isRetry, (hasError, @set, way, state, tag, dirLock) =>
			{
				if (!hasError) {
					if (!IsWriteHit (state)) {
						this.WriteRequest (this.Next, tag, hasError1 =>
						{
							if (!hasError1) {
								this.Cache.AccessLine (@set, way);
								this.Cache.SetLine (@set, way, tag, MESIState.Modified);
								dirLock.Unlock ();
								onCompletedCallback ();
							} else {
								this.Stat.WriteRetries++;
								dirLock.Unlock ();
								this.Retry (() => this.Store (addr, true, onCompletedCallback));
							}
						});
					} else {
						this.Cache.AccessLine (@set, way);
						this.Cache.SetLine (@set, way, tag, MESIState.Modified);
						dirLock.Unlock ();
						onCompletedCallback ();
					}
				} else {
					this.Stat.WriteRetries++;
					this.Retry (() => this.Store (addr, true, onCompletedCallback));
				}
			});
		}

		public override void Evict (uint @set, uint way, Action<bool> onCompletedCallback)
		{
			uint tag;
			MESIState state;
			
			this.Cache.GetLine (@set, way, out tag, out state);
			
			uint srcSet = @set;
			//TODO: is it necessary or bug?
			uint srcWay = way;
			uint srcTag = tag;
			CoherentCacheNode target = this.Next;
			
			this.Invalidate (null, @set, way, () =>
			{
				if (state == MESIState.Invalid) {
					onCompletedCallback (false);
				} else if (state == MESIState.Modified) {
					this.Schedule (() => { target.EvictReceive (this, srcTag, true, hasError => { this.Schedule (() => this.EvictReplyReceive (hasError, srcSet, srcWay, onCompletedCallback), 2); }); }, 2);
				} else {
					this.Schedule (() => { target.EvictReceive (this, srcTag, false, hasError => { this.Schedule (() => this.EvictReplyReceive (hasError, srcSet, srcWay, onCompletedCallback), 2); }); }, 2);
				}
			});
		}

		public override void EvictReceive (CoherentCacheNode source, uint addr, bool isWriteback, Action<bool> onReceiveReplyCallback)
		{
			this.FindAndLock (addr, false, false, false, (hasError, @set, way, state, tag, dirLock) =>
			{
				if (!hasError) {
					if (!isWriteback) {
						this.EvictProcess (source, @set, way, dirLock, onReceiveReplyCallback);
					} else {
						this.Invalidate (source, @set, way, () =>
						{
							if (state == MESIState.Shared) {
								this.WriteRequest (this.Next, tag, hasError1 => this.EvictWritebackFinish (source, hasError1, @set, way, tag, dirLock, onReceiveReplyCallback));
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
				this.Cache.SetLine (@set, way, tag, MESIState.Modified);
				this.Cache.AccessLine (@set, way);
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
			this.Schedule (() =>
			{
				if (!hasError) {
					this.Cache.SetLine (srcSet, srcWay, 0, MESIState.Invalid);
				}
				onCompletedCallback (hasError);
			}, 2);
		}

		public override void ReadRequest (CoherentCacheNode target, uint addr, Action<bool, bool> onCompletedCallback)
		{
			this.Schedule (() => target.ReadRequestReceive (this, addr, onCompletedCallback), 2);
		}

		public override void ReadRequestReceive (CoherentCacheNode source, uint addr, Action<bool, bool> onCompletedCallback)
		{
			this.FindAndLock (addr, this.Next == source, true, false, (hasError, @set, way, state, tag, dirLock) =>
			{
				if (!hasError) {
					if (source.Next == this) {
						this.ReadRequestUpdown (source, @set, way, tag, state, dirLock, onCompletedCallback);
					} else {
						this.ReadRequestDownup (@set, way, tag, dirLock, onCompletedCallback);
					}
				} else {
					this.Schedule (() => onCompletedCallback (true, false), 2);
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
					this.ReadRequest (dirEntry.Owner, tag, (hasError, isShared) => this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback));
				}
				
				this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback);
			} else {
				this.ReadRequest (this.Next, tag, (hasError, isShared) =>
				{
					if (!hasError) {
						this.Cache.SetLine (@set, way, tag, isShared ? MESIState.Shared : MESIState.Exclusive);
						this.ReadRequestUpdownFinish (source, @set, way, dirLock, ref pending, onCompletedCallback);
					} else {
						dirLock.Unlock ();
						this.Schedule (() => onCompletedCallback (true, false), 2);
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
				
				this.Cache.AccessLine (@set, way);
				dirLock.Unlock ();
				this.Schedule (() => onCompletedCallback (false, dirEntry.IsShared), 2);
			}
		}

		private void ReadRequestDownup (uint @set, uint way, uint tag, DirectoryLock dirLock, Action<bool, bool> onCompletedCallback)
		{
			uint pending = 1;
			
			DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
			if (dirEntry.Owner != null) {
				pending++;
				
				this.ReadRequest (dirEntry.Owner, tag, (hasError, isShared) => this.ReadRequestDownupFinish (@set, way, tag, dirLock, ref pending, onCompletedCallback));
			}
			
			this.ReadRequestDownupFinish (@set, way, tag, dirLock, ref pending, onCompletedCallback);
		}

		private void ReadRequestDownupFinish (uint @set, uint way, uint tag, DirectoryLock dirLock, ref uint pending, Action<bool, bool> onCompletedCallback)
		{
			pending--;
			
			if (pending == 0) {
				DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
				dirEntry.Owner = null;
				
				this.Cache.SetLine (@set, way, tag, MESIState.Shared);
				this.Cache.AccessLine (@set, way);
				dirLock.Unlock ();
				this.Schedule (() => onCompletedCallback (false, false), 2);
			}
		}

		public override void WriteRequest (CoherentCacheNode target, uint addr, Action<bool> onCompletedCallback)
		{
			this.Schedule (() => target.WriteRequestReceive (this, addr, onCompletedCallback), 2);
		}

		public override void WriteRequestReceive (CoherentCacheNode source, uint addr, Action<bool> onCompletedCallback)
		{
			this.FindAndLock (addr, this.Next == source, false, false, (hasError, @set, way, state, tag, dirLock) =>
			{
				if (!hasError) {
					this.Invalidate (source, @set, way, () =>
					{
						if (source.Next == this) {
							if (state == MESIState.Modified || state == MESIState.Exclusive) {
								this.WriteRequestUpdownFinish (source, false, @set, way, tag, state, dirLock, onCompletedCallback);
							} else {
								this.WriteRequest (this.Next, tag, hasError1 => { this.WriteRequestUpdownFinish (source, hasError1, @set, way, tag, state, dirLock, onCompletedCallback); });
							}
						} else {
							this.Cache.SetLine (@set, way, 0, MESIState.Invalid);
							dirLock.Unlock ();
							this.Schedule (() => onCompletedCallback (false), 2);
						}
					});
				} else {
					this.Schedule (() => onCompletedCallback (true), 2);
				}
			});
		}

		private void WriteRequestUpdownFinish (CoherentCacheNode source, bool hasError, uint @set, uint way, uint tag, MESIState state, DirectoryLock dirLock, Action<bool> onCompletedCallback)
		{
			if (!hasError) {
				DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
				dirEntry.SetSharer (source);
				dirEntry.Owner = source;
				
				this.Cache.AccessLine (@set, way);
				if (state != MESIState.Modified) {
					this.Cache.SetLine (@set, way, tag, MESIState.Exclusive);
				}
				
				dirLock.Unlock ();
				this.Schedule (() => onCompletedCallback (false), 2);
			} else {
				dirLock.Unlock ();
				this.Schedule (() => onCompletedCallback (true), 2);
			}
		}

		public override void Invalidate (CoherentCacheNode except, uint @set, uint way, Action onCompletedCallback)
		{
			uint tag;
			MESIState state;
			
			this.Cache.GetLine (@set, way, out tag, out state);
			
			uint pending = 1;
			
			DirectoryEntry dirEntry = this.Cache.Directory.Entries[(int)@set][(int)way];
			
			foreach (var sharer in dirEntry.Sharers.FindAll (sharer => sharer != except)) {
				dirEntry.UnsetSharer (sharer);
				if (dirEntry.Owner == sharer) {
					dirEntry.Owner = null;
				}
				
				this.WriteRequest (sharer, tag, hasError =>
				{
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
			
			this.Schedule (() => onReceiveReplyCallback (false), this.Latency);
		}

		public override void ReadRequestReceive (CoherentCacheNode source, uint addr, Action<bool, bool> onCompletedCallback)
		{
			this.Stat.Accesses++;
			this.Stat.Reads++;
			
			this.Schedule (() => onCompletedCallback (false, false), this.Latency);
		}

		public override void WriteRequestReceive (CoherentCacheNode source, uint addr, Action<bool> onCompletedCallback)
		{
			this.Stat.Accesses++;
			this.Stat.Writes++;
			
			this.Schedule (() => onCompletedCallback (false), this.Latency);
		}

		public uint Latency {
			get { return this.Config.Latency; }
		}

		public MainMemoryConfig Config { get; private set; }
		public MainMemoryStat Stat { get; private set; }
	}
}
