/*
 * Cpu.cs
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MinCai.Simulators.Flexim.Architecture;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.OperatingSystem;
using MinCai.Simulators.Flexim.Pipelines;
using Mono.Unix.Native;
using Process = MinCai.Simulators.Flexim.OperatingSystem.Process;

namespace MinCai.Simulators.Flexim.Pipelines
{
	public class PipelineList<EntryT> where EntryT : class
	{
		public PipelineList (string name)
		{
			this.Name = name;
			this.Entries = new List<EntryT> ();
		}

		public bool Empty {
			get { return this.Entries.Count == 0; }
		}

		public uint Size {
			get { return (uint)this.Entries.Count; }
		}

		public void TakeFront ()
		{
			this.Entries.RemoveAt (0);
		}

		public void TakeBack ()
		{
			this.Entries.RemoveAt (this.Entries.Count - 1);
		}

		public EntryT Front {
			get {
				if (!this.Empty) {
					return this.Entries[0];
				}
				
				return null;
			}
		}

		public EntryT Back {
			get {
				if (!this.Empty) {
					return this.Entries[this.Entries.Count - 1];
				}
				
				return null;
			}
		}

		public void Remove (EntryT val)
		{
			this.Entries.Remove (val);
		}

		public virtual void Add (EntryT val)
		{
			this.Entries.Add (val);
		}

		public void Clear ()
		{
			this.Entries.Clear ();
		}

		public List<EntryT>.Enumerator GetEnumerator ()
		{
			return this.Entries.GetEnumerator ();
		}

		public override string ToString ()
		{
			return string.Format ("[PipelineList: Name={0}, Size={1}]", this.Name, this.Size);
		}

		public string Name { get; set; }
		public List<EntryT> Entries { get; private set; }
	}

	public class PipelineQueue<EntryT> : PipelineList<EntryT> where EntryT : class
	{
		public PipelineQueue (string name, uint capacity) : base(name)
		{
			this.Capacity = capacity;
		}

		public bool Full {
			get { return this.Size >= this.Capacity; }
		}

		public override void Add (EntryT val)
		{
			if (this.Full) {
				Logger.Fatalf (LogCategory.MISC, "%s", this);
			}
			
			base.Add (val);
		}

		public override string ToString ()
		{
			return string.Format ("[PipelineQueue: Name={0}, Capacity={1}, Size={2}, Full={3}]", this.Name, this.Capacity, this.Size, this.Full);
		}

		public uint Capacity { get; private set; }
	}

	public static class BpredConstants
	{
		public static uint MD_BR_SHIFT = 3;
	}

	public class BpredBtbEntry
	{
		public BpredBtbEntry ()
		{
		}

		public uint Addr { get; set; }
		public StaticInst StaticInst { get; set; }
		public uint Target { get; set; }
		public BpredBtbEntry Prev { get; set; }
		public BpredBtbEntry Next { get; set; }
	}

	public interface BpredDir
	{
		byte[] Table { get; }
	}

	public class BpredDirInfo
	{
		public BpredDirInfo (BpredDir dir, uint offset)
		{
			this.Dir = dir;
			this.Offset = offset;
		}

		public byte Value {
			get { return this.Dir.Table[this.Offset]; }
			set { this.Dir.Table[this.Offset] = value; }
		}

		public BpredDir Dir { get; set; }
		public uint Offset { get; set; }
	}

	public class BimodBpredDir : BpredDir
	{
		public BimodBpredDir (uint size)
		{
			this.Size = size;
			this.Table = new byte[this.Size];
			
			byte flipFlop = 1;
			for (uint cnt = 0; cnt < this.Size; cnt++) {
				this.Table[cnt] = flipFlop;
				flipFlop = (byte)(3 - flipFlop);
			}
		}

		public uint Hash (uint baddr)
		{
			return (baddr >> 19) ^ (baddr >> (int)BpredConstants.MD_BR_SHIFT) & (this.Size - 1);
		}

		public BpredDirInfo Lookup (uint baddr)
		{
			return new BpredDirInfo (this, this.Hash (baddr));
		}

		public uint Size { get; set; }
		public byte[] Table { get; set; }
	}

	public class TwoLevelBpredDir : BpredDir
	{
		public TwoLevelBpredDir (uint l1Size, uint l2Size, uint shiftWidth, bool xor)
		{
			this.L1Size = l1Size;
			this.L2Size = l2Size;
			this.ShiftWidth = shiftWidth;
			this.Xor = xor;
			
			this.ShiftRegs = new uint[this.L1Size];
			this.L2Table = new byte[this.L2Size];
			
			byte flipFlop = 1;
			for (uint cnt = 0; cnt < this.L2Size; cnt++) {
				this.L2Table[cnt] = flipFlop;
				flipFlop = (byte)(3 - flipFlop);
			}
		}

		public BpredDirInfo Lookup (uint baddr)
		{
			uint l1Index = (baddr >> (int)BpredConstants.MD_BR_SHIFT) & (this.L1Size - 1);
			uint l2Index = this.ShiftRegs[l1Index];
			
			if (this.Xor) {
				l2Index = (uint)(((l2Index ^ (baddr >> (int)BpredConstants.MD_BR_SHIFT)) & ((1 << (int)this.ShiftWidth) - 1)) | ((baddr >> (int)BpredConstants.MD_BR_SHIFT) << (int)this.ShiftWidth));
			} else {
				l2Index |= (baddr >> (int)BpredConstants.MD_BR_SHIFT) << (int)this.ShiftWidth;
			}
			
			l2Index &= (this.L2Size - 1);
			
			return new BpredDirInfo (this, l2Index);
		}

		public byte[] Table {
			get { return this.L2Table; }
		}

		public uint L1Size { get; set; }
		public uint L2Size { get; set; }
		public uint ShiftWidth { get; set; }
		public bool Xor { get; set; }
		public uint[] ShiftRegs { get; set; }
		public byte[] L2Table { get; set; }
	}

	public class BTB
	{
		public BTB (uint sets, uint assoc)
		{
			this.Sets = sets;
			this.Assoc = assoc;
			
			this.Entries = new BpredBtbEntry[this.Sets * this.Assoc];
			for (uint i = 0; i < this.Sets * this.Assoc; i++) {
				this[i] = new BpredBtbEntry ();
			}
			
			if (this.Assoc > 1) {
				for (uint i = 0; i < this.Sets * this.Assoc; i++) {
					if (i % this.Assoc != (this.Assoc - 1)) {
						this[i].Next = this[i + 1];
					} else {
						this[i].Next = null;
					}
					
					if (i % this.Assoc != (this.Assoc - 1)) {
						this[i + 1].Prev = this[i];
					}
				}
			}
		}

		public BpredBtbEntry this[uint index] {
			get { return this.Entries[index]; }
			set { this.Entries[index] = value; }
		}

		public uint Sets { get; set; }
		public uint Assoc { get; set; }
		public BpredBtbEntry[] Entries { get; set; }
	}

	public class RAS
	{
		public RAS (uint size)
		{
			this.Size = size;
			this.Entries = new BpredBtbEntry[this.Size];
			for (uint i = 0; i < this.Size; i++) {
				this[i] = new BpredBtbEntry ();
			}
			
			this.Tos = this.Size - 1;
		}

		public BpredBtbEntry this[uint index] {
			get { return this.Entries[index]; }
			set { this.Entries[index] = value; }
		}

		public uint Size { get; set; }
		public uint Tos { get; set; }
		public BpredBtbEntry[] Entries { get; set; }
	}

	public class BpredUpdate
	{
		public BpredUpdate ()
		{
		}

		public BpredDirInfo Pdir1 { get; set; }
		public BpredDirInfo Pdir2 { get; set; }
		public BpredDirInfo Pmeta { get; set; }

		public bool Ras { get; set; }
		public bool Bimod { get; set; }
		public bool TwoLevel { get; set; }
		public bool Meta { get; set; }
	}

	public interface Bpred
	{
		uint Lookup (uint baddr, uint btarget, DynamicInst dynamicInst, out BpredUpdate dirUpdate, out uint stackRecoverIdx);

		void Recover (uint baddr, uint stackRecoverIdx);

		void Update (uint baddr, uint btarget, bool taken, bool predTaken, bool correct, DynamicInst dynamicInst, BpredUpdate dirUpdate);
	}

	public class CombinedBpred : Bpred
	{
		public CombinedBpred () : this(65536, 1, 65536, 65536, 16, true, 1024, 4, 1024)
		{
		}

		public CombinedBpred (uint bimodSize, uint l1Size, uint l2Size, uint metaSize, uint shiftWidth, bool xor, uint btbSets, uint btbAssoc, uint rasSize)
		{
			this.TwoLevel = new TwoLevelBpredDir (l1Size, l2Size, shiftWidth, xor);
			this.Bimod = new BimodBpredDir (bimodSize);
			this.Meta = new BimodBpredDir (metaSize);
			
			this.Btb = new BTB (btbSets, btbAssoc);
			this.RetStack = new RAS (rasSize);
		}

		public uint Lookup (uint baddr, uint btarget, DynamicInst dynamicInst, out BpredUpdate dirUpdate, out uint stackRecoverIdx)
		{
			StaticInst staticInst = dynamicInst.StaticInst;
			
			if (!staticInst.IsControl) {
				dirUpdate = null;
				stackRecoverIdx = 0;
				
				return 0;
			}
			
			dirUpdate = new BpredUpdate ();
			dirUpdate.Ras = false;
			dirUpdate.Pdir1 = null;
			dirUpdate.Pdir2 = null;
			dirUpdate.Pmeta = null;
			
			if (staticInst.IsControl && !staticInst.IsUnconditional) {
				BpredDirInfo bimodCtr = this.Bimod.Lookup (baddr);
				BpredDirInfo twoLevelCtr = this.TwoLevel.Lookup (baddr);
				BpredDirInfo metaCtr = this.Meta.Lookup (baddr);
				
				dirUpdate.Pmeta = metaCtr;
				dirUpdate.Meta = (metaCtr.Value >= 2);
				dirUpdate.Bimod = (bimodCtr.Value >= 2);
				dirUpdate.TwoLevel = (bimodCtr.Value >= 2);
				
				if (metaCtr.Value >= 2) {
					dirUpdate.Pdir1 = twoLevelCtr;
					dirUpdate.Pdir2 = bimodCtr;
				} else {
					dirUpdate.Pdir1 = bimodCtr;
					dirUpdate.Pdir2 = twoLevelCtr;
				}
			}
			
			if (this.RetStack.Size > 0) {
				stackRecoverIdx = this.RetStack.Tos;
			} else {
				stackRecoverIdx = 0;
			}
			
			if (staticInst.IsReturn && this.RetStack.Size > 0) {
				this.RetStack.Tos = (this.RetStack.Tos + this.RetStack.Size - 1) % this.RetStack.Size;
				dirUpdate.Ras = true;
			}
			
			if (staticInst.IsCall && this.RetStack.Size > 0) {
				this.RetStack.Tos = (this.RetStack.Tos + 1) % this.RetStack.Size;
				this.RetStack[this.RetStack.Tos].Target = (uint)(baddr + Marshal.SizeOf (typeof(uint)));
			}
			
			uint index = (baddr >> (int)BpredConstants.MD_BR_SHIFT) & (this.Btb.Sets - 1);
			
			BpredBtbEntry btbEntry = null;
			
			if (this.Btb.Assoc > 1) {
				index *= this.Btb.Assoc;
				
				for (uint i = index; i < (index + this.Btb.Assoc); i++) {
					if (this.Btb[i].Addr == baddr) {
						btbEntry = this.Btb[i];
						break;
					}
				}
			} else {
				btbEntry = this.Btb[index];
				if (btbEntry.Addr != baddr) {
					btbEntry = null;
				}
			}
			
			if (staticInst.IsControl && staticInst.IsUnconditional) {
				return btbEntry != null ? btbEntry.Target : 1;
			}
			
			if (btbEntry == null) {
				return (uint)(dirUpdate.Pdir1.Value >= 2 ? 1 : 0);
			} else {
				return (uint)(dirUpdate.Pdir1.Value >= 2 ? btbEntry.Target : 0);
			}
		}

		public void Recover (uint baddr, uint stackRecoverIdx)
		{
			this.RetStack.Tos = stackRecoverIdx;
		}

		public void Update (uint baddr, uint btarget, bool taken, bool predTaken, bool correct, DynamicInst dynamicInst, BpredUpdate dirUpdate)
		{
			StaticInst staticInst = dynamicInst.StaticInst;
			
			BpredBtbEntry btbEntry = null;
			
			if (!staticInst.IsControl) {
				return;
			}
			
			if (staticInst.IsControl && !staticInst.IsUnconditional) {
				uint l1Index = (baddr >> (int)BpredConstants.MD_BR_SHIFT) & (this.TwoLevel.L1Size - 1);
				uint shiftReg = (this.TwoLevel.ShiftRegs[l1Index] << 1) | (uint)(taken ? 1 : 0);
				this.TwoLevel.ShiftRegs[l1Index] = (uint)(shiftReg & ((1 << (int)this.TwoLevel.ShiftWidth) - 1));
			}
			
			if (taken) {
				uint index = (baddr >> (int)BpredConstants.MD_BR_SHIFT) & (this.Btb.Sets - 1);
				
				if (this.Btb.Assoc > 1) {
					index *= this.Btb.Assoc;
					
					BpredBtbEntry lruHead = null, lruItem = null;
					
					for (uint i = index; i < (index + this.Btb.Assoc); i++) {
						if (this.Btb[i].Addr == baddr) {
							btbEntry = this.Btb[i];
						}
						
						if (this.Btb[i].Prev == null) {
							lruHead = this.Btb[i];
						}
						
						if (this.Btb[i].Next == null) {
							lruItem = this.Btb[i];
						}
					}
					
					if (btbEntry == null) {
						btbEntry = lruItem;
					}
					
					if (btbEntry != lruHead) {
						if (btbEntry.Prev != null) {
							btbEntry.Prev.Next = btbEntry.Next;
						}
						
						if (btbEntry.Next != null) {
							btbEntry.Next.Prev = btbEntry.Prev;
						}
						
						btbEntry.Next = lruHead;
						btbEntry.Prev = null;
						lruHead.Prev = btbEntry;
					}
				} else {
					btbEntry = this.Btb[index];
				}
			}
			
			if (dirUpdate.Pdir1 != null) {
				if (taken) {
					if (dirUpdate.Pdir1.Value < 3) {
						dirUpdate.Pdir1.Value++;
					}
				} else {
					if (dirUpdate.Pdir1.Value > 0) {
						dirUpdate.Pdir1.Value--;
					}
				}
			}
			
			if (dirUpdate.Pdir2 != null) {
				if (taken) {
					if (dirUpdate.Pdir2.Value < 3) {
						dirUpdate.Pdir2.Value++;
					}
				} else {
					if (dirUpdate.Pdir2.Value > 0) {
						dirUpdate.Pdir2.Value--;
					}
				}
			}
			
			if (dirUpdate.Pmeta != null) {
				if (dirUpdate.Bimod != dirUpdate.TwoLevel) {
					if (dirUpdate.TwoLevel == taken) {
						if (dirUpdate.Pmeta.Value < 3) {
							dirUpdate.Pmeta.Value++;
						}
					} else {
						if (dirUpdate.Pmeta.Value > 0) {
							dirUpdate.Pmeta.Value--;
						}
					}
				}
			}
			
			if (btbEntry != null) {
				if (btbEntry.Addr == baddr) {
					if (!correct) {
						btbEntry.Target = btarget;
					}
				} else {
					btbEntry.Addr = baddr;
					btbEntry.StaticInst = staticInst;
					btbEntry.Target = btarget;
				}
			}
		}

		public TwoLevelBpredDir TwoLevel { get; set; }
		public BimodBpredDir Bimod { get; set; }
		public BimodBpredDir Meta { get; set; }

		public BTB Btb { get; set; }
		public RAS RetStack { get; set; }
	}

	public enum FunctionalUnitType : uint
	{
		NONE = 0,
		IntALU,
		IntMULT,
		IntDIV,
		FloatADD,
		FloatCMP,
		FloatCVT,
		FloatMULT,
		FloatDIV,
		FloatSQRT,
		RdPort,
		WrPort
	}

	public class FunctionalUnit
	{
		public FunctionalUnit (FunctionalUnitPool pool, FunctionalUnitType type, uint opLat, uint issueLat)
		{
			this.Pool = pool;
			this.Type = type;
			this.OpLat = opLat;
			this.IssueLat = issueLat;
		}

		public void Acquire (ReorderBufferEntry reorderBufferEntry, VoidDelegate onCompletedCallback)
		{
			this.Pool.EventQueue.Schedule (delegate() {
				this.Busy = false;
				onCompletedCallback ();
			}, this.IssueLat + this.OpLat);
			this.Busy = true;
		}

		public override string ToString ()
		{
			return string.Format ("[FunctionalUnit: Type={0}, OpLat={1}, IssueLat={2}, Busy={3}]", this.Type, this.OpLat, this.IssueLat, this.Busy);
		}

		public FunctionalUnitPool Pool { get; set; }
		public FunctionalUnitType Type { get; set; }
		public uint OpLat { get; set; }
		public uint IssueLat { get; set; }
		public bool Busy { get; set; }
	}

	public delegate void ReorderBufferEntryDelegate (ReorderBufferEntry reorderBufferEntry);

	public class FunctionalUnitPool
	{
		public FunctionalUnitPool (Core core)
		{
			this.Core = core;
			
			this.Name = "c" + this.Core.Num + ".fuPool";
			
			this.Entities = new Dictionary<FunctionalUnitType, List<FunctionalUnit>> ();
			
			this.Add (FunctionalUnitType.IntALU, 4, 1, 1);
			this.Add (FunctionalUnitType.IntMULT, 1, 3, 1);
			this.Add (FunctionalUnitType.IntDIV, 1, 20, 19);
			this.Add (FunctionalUnitType.RdPort, 2, 1, 1);
			this.Add (FunctionalUnitType.WrPort, 2, 1, 1);
			this.Add (FunctionalUnitType.FloatADD, 4, 2, 1);
			this.Add (FunctionalUnitType.FloatCMP, 4, 2, 1);
			this.Add (FunctionalUnitType.FloatCVT, 4, 2, 1);
			this.Add (FunctionalUnitType.FloatMULT, 1, 4, 1);
			this.Add (FunctionalUnitType.FloatDIV, 1, 12, 12);
			this.Add (FunctionalUnitType.FloatSQRT, 1, 24, 24);
			
			this.EventQueue = new DelegateEventQueue ();
			
			this.Core.EventProcessors.Add (this.EventQueue);
		}

		public void Add (FunctionalUnitType type, uint quantity, uint opLat, uint issueLat)
		{
			this.Entities[type] = new List<FunctionalUnit> ();
			for (uint i = 0; i < quantity; i++) {
				this.Entities[type].Add (new FunctionalUnit (this, type, opLat, issueLat));
			}
		}

		public FunctionalUnit FindFree (FunctionalUnitType type)
		{
			return this.Entities[type].Find (fu => !fu.Busy);
		}

		public void Acquire (ReorderBufferEntry reorderBufferEntry, ReorderBufferEntryDelegate onCompletedCallback2)
		{
			FunctionalUnitType type = reorderBufferEntry.DynamicInst.StaticInst.FuType;
			FunctionalUnit fu = this.FindFree (type);
			
			if (fu != null) {
				fu.Acquire (reorderBufferEntry, delegate() { onCompletedCallback2 (reorderBufferEntry); });
			} else {
				this.EventQueue.Schedule (delegate() { this.Acquire (reorderBufferEntry, onCompletedCallback2); }, 10);
			}
		}

		public Core Core { get; set; }
		public string Name { get; set; }

		public Dictionary<FunctionalUnitType, List<FunctionalUnit>> Entities { get; private set; }
		public DelegateEventQueue EventQueue { get; set; }
	}

	public enum PhysicalRegisterState
	{
		FREE,
		ALLOC,
		WB,
		ARCH
	}

	public class PhysicalRegister
	{
		public PhysicalRegister (PhysicalRegisterFile file)
		{
			this.File = file;
			this.State = PhysicalRegisterState.FREE;
		}

		public void Alloc (ReorderBufferEntry reorderBufferEntry)
		{
			this.State = PhysicalRegisterState.ALLOC;
			this.ReorderBufferEntry = reorderBufferEntry;
		}

		public void Writeback ()
		{
			this.State = PhysicalRegisterState.WB;
		}

		public void Commit ()
		{
			this.State = PhysicalRegisterState.ARCH;
		}

		public void Dealloc ()
		{
			this.State = PhysicalRegisterState.FREE;
			this.ReorderBufferEntry = null;
		}

		public override string ToString ()
		{
			return string.Format ("[PhysicalRegister: State={0}, ReorderBufferEntry={1}]", this.State, this.ReorderBufferEntry);
		}

		public bool IsReady {
			get { return this.State == PhysicalRegisterState.WB || this.State == PhysicalRegisterState.ARCH; }
		}

		public PhysicalRegisterFile File { get; set; }
		public PhysicalRegisterState State { get; set; }
		public ReorderBufferEntry ReorderBufferEntry { get; set; }
	}

	public class NoFreePhysicalRegisterException : Exception
	{
		public NoFreePhysicalRegisterException () : base("NoFreePhysicalRegisterException")
		{
		}
	}

	public class PhysicalRegisterFile
	{
		public PhysicalRegisterFile (Core core, string namePostfix, uint capacity)
		{
			this.Core = core;
			this.Name = "c" + this.Core.Num + "." + namePostfix;
			this.Capacity = capacity;
			
			this.Entries = new List<PhysicalRegister> ();
			for (uint i = 0; i < this.Capacity; i++) {
				this.Entries.Add (new PhysicalRegister (this));
			}
		}

		public PhysicalRegister FindFree ()
		{
			return this.Entries.Find (physReg => physReg.State == PhysicalRegisterState.FREE);
		}

		public PhysicalRegister Alloc (ReorderBufferEntry reorderBufferEntry)
		{
			PhysicalRegister freeReg = this.FindFree ();
			
			if (freeReg == null) {
				throw new NoFreePhysicalRegisterException ();
			}
			
			freeReg.Alloc (reorderBufferEntry);
			return freeReg;
		}

		public override string ToString ()
		{
			return string.Format ("[PhysicalRegisterFile: Name={0}, Capacity={1}, Core={2}]", this.Name, this.Capacity, this.Core);
		}

		public PhysicalRegister this[uint index] {
			get { return this.Entries[(int)index]; }
			set { this.Entries[(int)index] = value; }
		}

		public Core Core { get; set; }
		public string Name { get; set; }
		public uint Capacity { get; set; }
		public List<PhysicalRegister> Entries { get; set; }
	}

	public class RegisterRenameTable
	{
		public RegisterRenameTable (Thread thread)
		{
			this.Thread = thread;
			this.Name = "c" + this.Thread.Core.Num + "t" + this.Thread.Num + ".renameTable";
			
			this.Entries = new Dictionary<RegisterDependencyType, Dictionary<uint, PhysicalRegister>> ();
			
			this.Entries[RegisterDependencyType.INT] = new Dictionary<uint, PhysicalRegister> ();
			this.Entries[RegisterDependencyType.FP] = new Dictionary<uint, PhysicalRegister> ();
			this.Entries[RegisterDependencyType.MISC] = new Dictionary<uint, PhysicalRegister> ();
		}

		public PhysicalRegister this[RegisterDependency dep] {
			get { return this[dep.Type, dep.Num]; }
			set { this[dep.Type, dep.Num] = value; }
		}

		public PhysicalRegister this[RegisterDependencyType type, uint Num] {
			get { return this.Entries[type][Num]; }
			set { this.Entries[type][Num] = value; }
		}

		public Thread Thread { get; set; }
		public string Name { get; set; }
		Dictionary<RegisterDependencyType, Dictionary<uint, PhysicalRegister>> Entries { get; set; }
	}

	public class DecodeBufferEntry
	{
		public DecodeBufferEntry (DynamicInst dynamicInst)
		{
			this.Id = CurrentId++;
			this.DynamicInst = dynamicInst;
		}

		public override string ToString ()
		{
			return string.Format ("[DecodeBufferEntry: Id={0}, Npc=0x{1:x8}, Nnpc=0x{2:x8}, PredNpc=0x{3:x8}, PredNnpc=0x{4:x8}, DynamicInst={5}, IsSpeculative={6}, StackRecoverIndex={7}, DirUpdate={8}]", this.Id, this.Npc, this.Nnpc, this.PredNpc, this.PredNnpc, this.DynamicInst, this.IsSpeculative, this.StackRecoverIndex, this.DirUpdate);
		}

		public ulong Id { get; set; }
		public uint Npc { get; set; }
		public uint Nnpc { get; set; }
		public uint PredNpc { get; set; }
		public uint PredNnpc { get; set; }
		public DynamicInst DynamicInst { get; set; }

		public bool IsSpeculative { get; set; }
		public uint StackRecoverIndex { get; set; }
		public BpredUpdate DirUpdate { get; set; }

		static DecodeBufferEntry ()
		{
			CurrentId = 0;
		}

		public static ulong CurrentId;
	}

	public class DecodeBuffer : PipelineQueue<DecodeBufferEntry>
	{
		public DecodeBuffer (Thread thread, uint capacity) : base("c" + thread.Core.Num + "t" + thread.Num + ".decodeBuffer", capacity)
		{
		}
	}

	public class ReorderBufferEntry
	{
		public ReorderBufferEntry (DynamicInst dynamicInst, List<RegisterDependency> iDeps, List<RegisterDependency> oDeps)
		{
			this.Id = CurrentId++;
			this.DynamicInst = dynamicInst;
			this.IDeps = iDeps;
			this.ODeps = oDeps;
			
			this.OldPhysRegs = new Dictionary<RegisterDependency, PhysicalRegister> ();
			this.PhysRegs = new Dictionary<RegisterDependency, PhysicalRegister> ();
			this.SrcPhysRegs = new Dictionary<RegisterDependency, PhysicalRegister> ();
			
			this.IsValid = true;
		}

		public void SignalCompleted ()
		{
			this.DynamicInst.Thread.Core.OoOEventQueue.Add (this);
		}

		public void Invalidate ()
		{
			this.IsValid = false;
		}

		public override string ToString ()
		{
			return string.Format ("[ReorderBufferEntry: Id={0}, Npc=0x{1:x8}, Nnpc=0x{2:x8}, PredNpc=0x{3:x8}, PredNnpc=0x{4:x8}, DynamicInst={5}, IsDispatched={6}, IsInReadyQueue={7}, IsIssued={8}, IsCompleted={9}, IsValid={10}, LoadStoreQueueEntry={11}, Ea=0x{12:x8}, IsSpeculative={13}, StackRecoverIndex={14}, DirUpdate={15}]", this.Id, this.Npc, this.Nnpc, this.PredNpc, this.PredNnpc, this.DynamicInst, this.IsDispatched, this.IsInReadyQueue, this.IsIssued,
			this.IsCompleted, this.IsValid, this.LoadStoreQueueEntry, this.Ea, this.IsSpeculative, this.StackRecoverIndex, this.DirUpdate);
		}

		public bool AllOperandsReady {
			get { return this.IDeps.All (iDep => this.SrcPhysRegs[iDep].IsReady); }
		}

		public bool StoreAddressReady {
			get {
				MemoryOp memOp = this.DynamicInst.StaticInst as MemoryOp;
				return this.SrcPhysRegs[memOp.MemIDeps[0]].IsReady;
			}
		}

		public bool StoreOperandReady {
			get {
				MemoryOp memOp = this.DynamicInst.StaticInst as MemoryOp;
				
				return memOp.MemIDeps.GetRange (1, memOp.MemIDeps.Count - 1).All (iDep => this.SrcPhysRegs[iDep].IsReady);
			}
		}

		public bool IsInLoadStoreQueue {
			get { return this.DynamicInst.StaticInst.IsMem && this.LoadStoreQueueEntry == null; }
		}

		public bool IsEAComputation {
			get { return this.DynamicInst.StaticInst.IsMem && this.LoadStoreQueueEntry != null; }
		}

		public uint Npc { get; set; }
		public uint Nnpc { get; set; }
		public uint PredNpc { get; set; }
		public uint PredNnpc { get; set; }
		public DynamicInst DynamicInst { get; set; }

		public List<RegisterDependency> IDeps { get; set; }
		public List<RegisterDependency> ODeps { get; set; }
		public Dictionary<RegisterDependency, PhysicalRegister> OldPhysRegs { get; private set; }
		public Dictionary<RegisterDependency, PhysicalRegister> PhysRegs { get; private set; }
		public Dictionary<RegisterDependency, PhysicalRegister> SrcPhysRegs { get; private set; }

		public bool IsDispatched { get; set; }
		public bool IsInReadyQueue { get; set; }
		public bool IsIssued { get; set; }
		public bool IsCompleted { get; set; }
		public bool IsValid { get; set; }

		public ReorderBufferEntry LoadStoreQueueEntry { get; set; }
		public uint Ea { get; set; }

		public bool IsSpeculative { get; set; }
		public uint StackRecoverIndex { get; set; }
		public BpredUpdate DirUpdate { get; set; }

		public ulong Id { get; set; }

		static ReorderBufferEntry ()
		{
			CurrentId = 0;
		}

		public static ulong CurrentId;
	}

	public class ReadyQueue : PipelineList<ReorderBufferEntry>
	{
		public ReadyQueue (Core core) : base("c" + core.Num + ".readyQueue")
		{
		}
	}

	public class WaitingQueue : PipelineList<ReorderBufferEntry>
	{
		public WaitingQueue (Core core) : base("c" + core.Num + ".waitingQueue")
		{
		}
	}

	public class OoOEventQueue : PipelineList<ReorderBufferEntry>
	{
		public OoOEventQueue (Core core) : base("c" + core.Num + ".oooEventQueue")
		{
		}
	}

	public class ReorderBuffer : PipelineQueue<ReorderBufferEntry>
	{
		public ReorderBuffer (Thread thread, uint capacity) : base("c" + thread.Core.Num + "t" + thread.Num + ".reorderBuffer", capacity)
		{
		}
	}

	public class LoadStoreQueue : PipelineQueue<ReorderBufferEntry>
	{
		public LoadStoreQueue (Thread thread, uint capacity) : base("c" + thread.Core.Num + "t" + thread.Num + ".loadStoreQueue", capacity)
		{
		}
	}

	public class Core : ICycleProvider
	{
		public Core (Processor processor, ProcessorConfig config, uint num)
		{
			this.Processor = processor;
			this.Num = num;
			
			this.CurrentCycle = 0;
			
			this.EventProcessors = new List<EventProcessor> ();
			
			this.Threads = new List<Thread> ();
			
			this.DecodeWidth = config.DecodeWidth;
			this.IssueWidth = config.IssueWidth;
			
			this.IntRegFile = new PhysicalRegisterFile (this, "intRegFile", config.PhysicalRegisterFileCapacity);
			this.FpRegFile = new PhysicalRegisterFile (this, "fpRegFile", config.PhysicalRegisterFileCapacity);
			this.MiscRegFile = new PhysicalRegisterFile (this, "miscRegFile", config.PhysicalRegisterFileCapacity);
			
			this.FuPool = new FunctionalUnitPool (this);
			
			this.Isa = new MipsISA ();
			
			this.ReadyQueue = new ReadyQueue (this);
			this.WaitingQueue = new WaitingQueue (this);
			this.OoOEventQueue = new OoOEventQueue (this);
		}

		public void Fetch ()
		{
			foreach (Thread thread in this.Threads) {
				thread.Fetch ();
			}
		}

		private uint FindNextThreadIdToDecode (ref bool allStalled, ref Dictionary<uint, bool> decodeStalled)
		{
			for (uint i = 0; i < this.Threads.Count; i++) {
				Thread thread = this.Threads[(int)i];
				
				if (!decodeStalled[i] && !thread.DecodeBuffer.Empty && !thread.ReorderBuffer.Full && !thread.LoadStoreQueue.Full) {
					allStalled = false;
					return i;
				}
			}
			
			allStalled = true;
			return uint.MaxValue;
		}

		uint decodeThreadId = 0;

		public void RegisterRename ()
		{
			Dictionary<uint, bool> decodeStalled = new Dictionary<uint, bool> ();
			
			for (uint i = 0; i < this.Threads.Count; i++) {
				decodeStalled[i] = false;
			}
			
			decodeThreadId = (uint)((decodeThreadId + 1) % this.Threads.Count);
			
			uint numRenamed = 0;
			
			while (numRenamed < this.DecodeWidth) {
				bool allStalled = true;
				
				decodeThreadId = this.FindNextThreadIdToDecode (ref allStalled, ref decodeStalled);
				
				if (allStalled) {
					break;
				}
				
				DecodeBufferEntry decodeBufferEntry = this.Threads[(int)decodeThreadId].DecodeBuffer.Front;
				
				this.Threads[(int)decodeThreadId].Regs.IntRegs[RegisterConstants.ZeroReg] = 0;
				
				DynamicInst dynamicInst = decodeBufferEntry.DynamicInst;
				
				if (!dynamicInst.StaticInst.IsNop) {
					ReorderBufferEntry reorderBufferEntry = new ReorderBufferEntry (dynamicInst, dynamicInst.StaticInst.IDeps, dynamicInst.StaticInst.ODeps);
					reorderBufferEntry.Npc = decodeBufferEntry.Npc;
					reorderBufferEntry.Nnpc = decodeBufferEntry.Nnpc;
					reorderBufferEntry.PredNpc = decodeBufferEntry.PredNpc;
					reorderBufferEntry.PredNnpc = decodeBufferEntry.PredNnpc;
					reorderBufferEntry.StackRecoverIndex = decodeBufferEntry.StackRecoverIndex;
					reorderBufferEntry.DirUpdate = decodeBufferEntry.DirUpdate;
					reorderBufferEntry.IsSpeculative = decodeBufferEntry.IsSpeculative;
					
					foreach (RegisterDependency iDep in reorderBufferEntry.IDeps) {
						reorderBufferEntry.SrcPhysRegs[iDep] = this.Threads[(int)decodeThreadId].RenameTable[iDep];
					}
					
					try {
						foreach (RegisterDependency oDep in reorderBufferEntry.ODeps) {
							reorderBufferEntry.OldPhysRegs[oDep] = this.Threads[(int)decodeThreadId].RenameTable[oDep];
							this.Threads[(int)decodeThreadId].RenameTable[oDep] = reorderBufferEntry.PhysRegs[oDep] = this.GetPhysicalRegisterFile (oDep.Type).Alloc (reorderBufferEntry);
						}
					} catch (NoFreePhysicalRegisterException) {
						decodeStalled[decodeThreadId] = true;
						continue;
					}
					
					if (dynamicInst.StaticInst.IsMem) {
						ReorderBufferEntry loadStoreQueueEntry = new ReorderBufferEntry (dynamicInst, (dynamicInst.StaticInst as MemoryOp).MemIDeps, (dynamicInst.StaticInst as MemoryOp).MemODeps);
						
						loadStoreQueueEntry.Npc = decodeBufferEntry.Npc;
						loadStoreQueueEntry.Nnpc = decodeBufferEntry.Nnpc;
						loadStoreQueueEntry.PredNpc = decodeBufferEntry.PredNpc;
						loadStoreQueueEntry.PredNnpc = decodeBufferEntry.PredNnpc;
						loadStoreQueueEntry.StackRecoverIndex = 0;
						loadStoreQueueEntry.DirUpdate = null;
						loadStoreQueueEntry.IsSpeculative = false;
						
						loadStoreQueueEntry.Ea = (dynamicInst.StaticInst as MemoryOp).Ea (this.Threads[(int)decodeThreadId]);
						
						reorderBufferEntry.LoadStoreQueueEntry = loadStoreQueueEntry;
						
						foreach (RegisterDependency iDep in loadStoreQueueEntry.IDeps) {
							loadStoreQueueEntry.SrcPhysRegs[iDep] = this.Threads[(int)decodeThreadId].RenameTable[iDep];
						}
						
						try {
							foreach (RegisterDependency oDep in loadStoreQueueEntry.ODeps) {
								loadStoreQueueEntry.OldPhysRegs[oDep] = this.Threads[(int)decodeThreadId].RenameTable[oDep];
								this.Threads[(int)decodeThreadId].RenameTable[oDep] = loadStoreQueueEntry.PhysRegs[oDep] = this.GetPhysicalRegisterFile (oDep.Type).Alloc (loadStoreQueueEntry);
							}
						} catch (NoFreePhysicalRegisterException) {
							decodeStalled[decodeThreadId] = true;
							continue;
						}
						
						this.Threads[(int)decodeThreadId].LoadStoreQueue.Add (loadStoreQueueEntry);
					}
					
					this.Threads[(int)decodeThreadId].ReorderBuffer.Add (reorderBufferEntry);
				}
				
				this.Threads[(int)decodeThreadId].DecodeBuffer.TakeFront ();
				
				numRenamed++;
			}
		}

		uint dispatchThreadId = 0;

		public void Dispatch ()
		{
			uint numDispatched = 0;
			Dictionary<uint, bool> dispatchStalled = new Dictionary<uint, bool> ();
			Dictionary<uint, uint> numDispatchedPerThread = new Dictionary<uint, uint> ();
			
			for (uint i = 0; i < this.Threads.Count; i++) {
				dispatchStalled[i] = false;
				numDispatchedPerThread[i] = 0;
			}
			
			dispatchThreadId = (uint)((dispatchThreadId + 1) % this.Threads.Count);
			
			while (numDispatched < this.DecodeWidth) {
				bool allStalled = true;
				
				for (uint i = 0; i < this.Threads.Count; i++) {
					if (!dispatchStalled[i]) {
						allStalled = false;
					}
				}
				
				if (allStalled) {
					break;
				}
				
				ReorderBufferEntry reorderBufferEntry = this.Threads[(int)dispatchThreadId].GetNextReorderBufferEntryToDispatch ();
				
				dispatchStalled[dispatchThreadId] = (reorderBufferEntry == null);
				
				if (dispatchStalled[dispatchThreadId]) {
					dispatchThreadId = (uint)((dispatchThreadId + 1) % this.Threads.Count);
					continue;
				}
				
				numDispatchedPerThread[dispatchThreadId]++;
				
				if (reorderBufferEntry.AllOperandsReady) {
					this.ReadyQueue.Add (reorderBufferEntry);
					reorderBufferEntry.IsInReadyQueue = true;
				} else {
					this.WaitingQueue.Add (reorderBufferEntry);
				}
				
				reorderBufferEntry.IsDispatched = true;
				
				if (reorderBufferEntry.LoadStoreQueueEntry != null) {
					ReorderBufferEntry loadStoreQueueEntry = reorderBufferEntry.LoadStoreQueueEntry;
					
					if (loadStoreQueueEntry.DynamicInst.StaticInst.IsStore) {
						if (loadStoreQueueEntry.AllOperandsReady) {
							this.ReadyQueue.Add (loadStoreQueueEntry);
							loadStoreQueueEntry.IsInReadyQueue = true;
						} else {
							this.WaitingQueue.Add (loadStoreQueueEntry);
						}
					}
					
					loadStoreQueueEntry.IsDispatched = true;
				}
				
				numDispatched++;
			}
		}

		public void Wakeup ()
		{
			List<ReorderBufferEntry> toWaitingQueue = new List<ReorderBufferEntry> ();
			
			while (!this.WaitingQueue.Empty) {
				ReorderBufferEntry waitingQueueEntry = this.WaitingQueue.Front;
				
				if (!waitingQueueEntry.IsValid) {
					this.WaitingQueue.TakeFront ();
					continue;
				}
				
				if (waitingQueueEntry.AllOperandsReady) {
					this.ReadyQueue.Add (waitingQueueEntry);
					waitingQueueEntry.IsInReadyQueue = true;
				} else {
					toWaitingQueue.Add (waitingQueueEntry);
				}
				
				this.WaitingQueue.TakeFront ();
			}
			
			foreach (ReorderBufferEntry waitingQueueEntry in toWaitingQueue) {
				this.WaitingQueue.Add (waitingQueueEntry);
			}
		}

		public void Selection ()
		{
			uint numIssued = 0;
			
			while (numIssued < this.IssueWidth && !this.ReadyQueue.Empty) {
				ReorderBufferEntry readyQueueEntry = this.ReadyQueue.Front;
				
				if (readyQueueEntry.IsInLoadStoreQueue && readyQueueEntry.DynamicInst.StaticInst.IsStore) {
					readyQueueEntry.IsIssued = true;
					readyQueueEntry.IsCompleted = true;
				} else if (readyQueueEntry.IsInLoadStoreQueue && readyQueueEntry.DynamicInst.StaticInst.IsLoad) {
					this.FuPool.Acquire (readyQueueEntry, delegate(ReorderBufferEntry readyQueueEntry1) {
						bool hitInLoadStoreQueue = false;
						
						foreach (ReorderBufferEntry loadStoreQueueEntry in readyQueueEntry1.DynamicInst.Thread.LoadStoreQueue.Entries.AsReverseEnumerable ()) {
							if (loadStoreQueueEntry.DynamicInst.StaticInst.IsStore && loadStoreQueueEntry.Ea == readyQueueEntry.Ea) {
								hitInLoadStoreQueue = true;
								break;
							}
						}
						
						if (hitInLoadStoreQueue) {
							readyQueueEntry1.SignalCompleted ();
						} else {
							this.SeqD.Load (this.MMU.Translate (readyQueueEntry1.Ea), false, readyQueueEntry1, delegate(ReorderBufferEntry readyQueueEntry2) { readyQueueEntry2.SignalCompleted (); });
						}
					});
					
					readyQueueEntry.IsIssued = true;
				} else {
					if (readyQueueEntry.DynamicInst.StaticInst.FuType != FunctionalUnitType.NONE) {
						this.FuPool.Acquire (readyQueueEntry, delegate(ReorderBufferEntry readyQueueEntry1) { readyQueueEntry1.SignalCompleted (); });
						readyQueueEntry.IsIssued = true;
					} else {
						readyQueueEntry.IsIssued = true;
						readyQueueEntry.IsCompleted = true;
					}
				}
				
				this.ReadyQueue.TakeFront ();
				readyQueueEntry.IsInReadyQueue = false;
				
				numIssued++;
			}
		}

		public void Writeback ()
		{
			while (!this.OoOEventQueue.Empty) {
				ReorderBufferEntry reorderBufferEntry = this.OoOEventQueue.Front;
				
				if (!reorderBufferEntry.IsValid) {
					this.OoOEventQueue.TakeFront ();
					continue;
				}
				
				reorderBufferEntry.IsCompleted = true;
				
				foreach (RegisterDependency oDep in reorderBufferEntry.ODeps) {
					reorderBufferEntry.PhysRegs[oDep].Writeback ();
				}
				
				if (reorderBufferEntry.IsSpeculative) {
					reorderBufferEntry.DynamicInst.Thread.Bpred.Recover (reorderBufferEntry.DynamicInst.PhysPc, reorderBufferEntry.StackRecoverIndex);
					
					reorderBufferEntry.DynamicInst.Thread.Regs.Speculative = reorderBufferEntry.DynamicInst.Thread.IsSpeculative = false;
					reorderBufferEntry.DynamicInst.Thread.FetchNpc = reorderBufferEntry.DynamicInst.Thread.Regs.Npc;
					reorderBufferEntry.DynamicInst.Thread.FetchNnpc = reorderBufferEntry.DynamicInst.Thread.Regs.Nnpc;
					
					reorderBufferEntry.DynamicInst.Thread.RecoverReorderBuffer (reorderBufferEntry);
					
					break;
				}
				
				this.OoOEventQueue.TakeFront ();
			}
		}

		public void RefreshLoadStoreQueue ()
		{
			foreach (Thread thread in this.Threads) {
				thread.RefreshLoadStoreQueue ();
			}
		}

		public void Commit ()
		{
			foreach (Thread thread in this.Threads) {
				thread.Commit ();
			}
		}

		public void AdvanceOneCycle ()
		{
			this.Commit ();
			this.Writeback ();
			this.RefreshLoadStoreQueue ();
			this.Wakeup ();
			this.Selection ();
			this.Dispatch ();
			this.RegisterRename ();
			this.Fetch ();
			
			foreach (EventProcessor eventProcessor in this.EventProcessors) {
				eventProcessor.AdvanceOneCycle ();
			}
			
			this.CurrentCycle++;
		}

		public PhysicalRegisterFile GetPhysicalRegisterFile (RegisterDependencyType type)
		{
			if (type == RegisterDependencyType.INT) {
				return this.IntRegFile;
			} else if (type == RegisterDependencyType.FP) {
				return this.FpRegFile;
			} else {
				return this.MiscRegFile;
			}
		}

		public Sequencer SeqI {get;set;}

		public CoherentCacheNode L1I {get;set;}

		public Sequencer SeqD {get;set;}

		public CoherentCacheNode L1D {get;set;}

		public MMU MMU {
			get { return this.Processor.MemorySystem.MMU; }
		}

		public uint Num { get; set; }
		public Processor Processor { get; set; }
		public List<Thread> Threads { get; set; }

		public uint DecodeWidth { get; set; }
		public uint IssueWidth { get; set; }

		public FunctionalUnitPool FuPool { get; set; }

		public PhysicalRegisterFile IntRegFile { get; set; }
		public PhysicalRegisterFile FpRegFile { get; set; }
		public PhysicalRegisterFile MiscRegFile { get; set; }

		public ISA Isa { get; set; }

		public ReadyQueue ReadyQueue { get; set; }
		public WaitingQueue WaitingQueue { get; set; }
		public OoOEventQueue OoOEventQueue { get; set; }

		public ulong CurrentCycle { get; set; }

		public List<EventProcessor> EventProcessors { get; set; }
	}

	public class Thread
	{
		public enum ThreadState
		{
			Inactive,
			Active,
			Halted
		}

		public Thread (Core core, ProcessorConfig config, ContextStat stat, uint num, Process process)
		{
			this.Core = core;
			
			this.Num = num;
			
			this.Process = process;
			
			this.SyscallEmul = new SyscallEmul ();
			
			this.Bpred = new CombinedBpred ();
			
			this.RenameTable = new RegisterRenameTable (this);
			
			this.ClearArchRegs ();
			
			this.Mem = new Memory ();
			
			this.Process.Load (this);
			
			this.Stat = stat;
			
			this.State = ThreadState.Active;
			
			for (uint i = 0; i < RegisterConstants.NumIntRegs; i++) {
				PhysicalRegister physReg = this.Core.IntRegFile[this.Num * RegisterConstants.NumIntRegs + i];
				physReg.Commit ();
				this.RenameTable[RegisterDependencyType.INT, i] = physReg;
			}
			
			for (uint i = 0; i < RegisterConstants.NumFloatRegs; i++) {
				PhysicalRegister physReg = this.Core.FpRegFile[this.Num * RegisterConstants.NumFloatRegs + i];
				physReg.Commit ();
				this.RenameTable[RegisterDependencyType.FP, i] = physReg;
			}
			
			for (uint i = 0; i < RegisterConstants.NumMiscRegs; i++) {
				PhysicalRegister physReg = this.Core.MiscRegFile[this.Num * RegisterConstants.NumMiscRegs + i];
				physReg.Commit ();
				this.RenameTable[RegisterDependencyType.MISC, i] = physReg;
			}
			
			this.CommitWidth = config.CommitWidth;
			
			this.DecodeBuffer = new DecodeBuffer (this, config.DecodeBufferCapcity);
			this.ReorderBuffer = new ReorderBuffer (this, config.ReorderBufferCapacity);
			this.LoadStoreQueue = new LoadStoreQueue (this, config.LoadStoreQueueCapacity);
			
			this.FetchNpc = this.Regs.Npc;
			this.FetchNnpc = this.Regs.Nnpc;
		}

		public DynamicInst DecodeAndExecute ()
		{
			this.Regs.Pc = this.Regs.Npc;
			this.Regs.Npc = this.Regs.Nnpc;
			this.Regs.Nnpc += (uint)Marshal.SizeOf (typeof(uint));
			
			StaticInst staticInst = this.Core.Isa.Decode (this.Regs.Pc, this.Mem);
			DynamicInst dynamicInst = new DynamicInst (this, this.Regs.Pc, staticInst);
			
			dynamicInst.Execute ();
			
			return dynamicInst;
		}

		private bool SetNpc ()
		{
			if (this.Regs.Npc == this.FetchNpc) {
				return false;
			}
			
			if (this.IsSpeculative) {
				this.Regs.Npc = this.FetchNpc;
				return false;
			}
			
			return true;
		}

		public void Fetch ()
		{
			uint blockToFetch = BitUtils.Aligned (this.FetchNpc, this.Core.SeqI.BlockSize);
			if (blockToFetch != this.LastFetchedBlock) {
				this.LastFetchedBlock = blockToFetch;
				
				this.Core.SeqI.Load (this.Core.MMU.Translate (this.FetchNpc), false, delegate() { this.FetchStalled = false; });
				
				this.FetchStalled = true;
			}
			
			bool done = false;
			
			while (!done && !this.DecodeBuffer.Full && !this.FetchStalled) {
				if (this.SetNpc ()) {
					this.Regs.Speculative = this.IsSpeculative = true;
				}
				
				this.FetchPc = this.FetchNpc;
				this.FetchNpc = this.FetchNnpc;
				
				DynamicInst dynamicInst = this.DecodeAndExecute ();
				
				if (this.FetchNpc != this.FetchPc + Marshal.SizeOf (typeof(uint))) {
					done = true;
				}
				
				if ((this.FetchPc + Marshal.SizeOf (typeof(uint))) % this.Core.SeqI.BlockSize == 0) {
					done = true;
				}
				
				uint stackRecoverIndex;
				BpredUpdate dirUpdate = new BpredUpdate ();
				
				uint dest = this.Bpred.Lookup (this.Core.MMU.Translate (this.FetchPc), 0, dynamicInst, out dirUpdate, out stackRecoverIndex);
				this.FetchNnpc = dest <= 1 ? (uint)(this.FetchNpc + Marshal.SizeOf (typeof(uint))) : dest;
				
				this.FetchNnpc = this.Regs.Nnpc;
				//TODO: remove it
				DecodeBufferEntry decodeBufferEntry = new DecodeBufferEntry (dynamicInst);
				decodeBufferEntry.Npc = this.Regs.Npc;
				decodeBufferEntry.Nnpc = this.Regs.Nnpc;
				decodeBufferEntry.PredNpc = this.FetchNpc;
				decodeBufferEntry.PredNnpc = this.FetchNnpc;
				decodeBufferEntry.StackRecoverIndex = stackRecoverIndex;
				decodeBufferEntry.DirUpdate = dirUpdate;
				decodeBufferEntry.IsSpeculative = this.IsSpeculative;
				
				this.DecodeBuffer.Add (decodeBufferEntry);
			}
		}

		public ReorderBufferEntry GetNextReorderBufferEntryToDispatch ()
		{
			return this.ReorderBuffer.Entries.Find (reorderBufferEntry => !reorderBufferEntry.IsDispatched);
		}

		public void RefreshLoadStoreQueue ()
		{
			List<uint> stdUnknowns = new List<uint> ();
			
			foreach (ReorderBufferEntry loadStoreQueueEntry in this.LoadStoreQueue) {
				if (loadStoreQueueEntry.DynamicInst.StaticInst.IsStore) {
					if (loadStoreQueueEntry.StoreAddressReady) {
						break;
					} else if (!loadStoreQueueEntry.AllOperandsReady) {
						stdUnknowns.Add (loadStoreQueueEntry.Ea);
					} else {
						for (int i = 0; i < stdUnknowns.Count; i++) {
							if (stdUnknowns[i] == loadStoreQueueEntry.Ea) {
								stdUnknowns[i] = 0;
							}
						}
					}
				}
				
				if (loadStoreQueueEntry.DynamicInst.StaticInst.IsLoad && loadStoreQueueEntry.IsDispatched && !loadStoreQueueEntry.IsInReadyQueue && !loadStoreQueueEntry.IsIssued && !loadStoreQueueEntry.IsCompleted && loadStoreQueueEntry.AllOperandsReady) {
					if (!stdUnknowns.Contains (loadStoreQueueEntry.Ea)) {
						this.Core.ReadyQueue.Add (loadStoreQueueEntry);
						loadStoreQueueEntry.IsInReadyQueue = true;
					}
				}
			}
		}

		public void Commit ()
		{
			if (this.Core.CurrentCycle - this.LastCommitCycle > COMMIT_TIMEOUT) {
				Logger.Fatalf (LogCategory.SIMULATOR, "Thread {0:s} - No instruction committed for {1:d} cycles", this.Name, COMMIT_TIMEOUT);
			}
			
			uint numCommitted = 0;
			
			while (!this.ReorderBuffer.Empty && numCommitted < this.CommitWidth) {
				ReorderBufferEntry reorderBufferEntry = this.ReorderBuffer.Front;
				
				if (!reorderBufferEntry.IsCompleted) {
					break;
				}
				
				if (reorderBufferEntry.IsEAComputation) {
					ReorderBufferEntry loadStoreQueueEntry = this.LoadStoreQueue.Front;
					
					if (!loadStoreQueueEntry.IsCompleted) {
						break;
					}
					
					if (loadStoreQueueEntry.DynamicInst.StaticInst.IsStore) {
						this.Core.FuPool.Acquire (loadStoreQueueEntry, delegate(ReorderBufferEntry loadStoreQueueEntry1) { this.Core.SeqD.Store (this.Core.MMU.Translate (loadStoreQueueEntry1.Ea), false, delegate() { }); });
					}
					
					foreach (RegisterDependency oDep in loadStoreQueueEntry.ODeps) {
						loadStoreQueueEntry.OldPhysRegs[oDep].Dealloc ();
						loadStoreQueueEntry.PhysRegs[oDep].Commit ();
					}
					
					this.LoadStoreQueue.TakeFront ();
				}
				
				foreach (RegisterDependency oDep in reorderBufferEntry.ODeps) {
					reorderBufferEntry.OldPhysRegs[oDep].Dealloc ();
					reorderBufferEntry.PhysRegs[oDep].Commit ();
				}
				
				if (reorderBufferEntry.DynamicInst.StaticInst.IsControl) {
					this.Bpred.Update (reorderBufferEntry.DynamicInst.PhysPc, reorderBufferEntry.Nnpc, reorderBufferEntry.Nnpc != (reorderBufferEntry.Npc + Marshal.SizeOf (typeof(uint))), reorderBufferEntry.PredNnpc != (reorderBufferEntry.Npc + Marshal.SizeOf (typeof(uint))), reorderBufferEntry.PredNnpc == reorderBufferEntry.Nnpc, reorderBufferEntry.DynamicInst, reorderBufferEntry.DirUpdate);
				}
				
				this.ReorderBuffer.TakeFront ();
				
				this.Stat.TotalInsts++;
				
				this.LastCommitCycle = this.Core.CurrentCycle;
				
				numCommitted++;
				
				this.Core.Processor.Simulation.Stat.TotalInsts++;
				
				Logger.Infof (LogCategory.DEBUG, "instruction committed (dynamicInst={0})", reorderBufferEntry.DynamicInst);
			}
		}

		public void RecoverReorderBuffer (ReorderBufferEntry branchReorderBufferEntry)
		{
			Logger.Infof (LogCategory.SIMULATOR, "RecoverReorderBuffer({0:s})", branchReorderBufferEntry);
			
			while (!this.ReorderBuffer.Empty) {
				ReorderBufferEntry reorderBufferEntry = this.ReorderBuffer.Back;
				
				if (!reorderBufferEntry.IsSpeculative) {
					break;
				}
				
				if (reorderBufferEntry.IsEAComputation) {
					ReorderBufferEntry loadStoreQueueEntry = this.LoadStoreQueue.Back;
					
					loadStoreQueueEntry.Invalidate ();
					
					foreach (RegisterDependency oDep in loadStoreQueueEntry.ODeps) {
						loadStoreQueueEntry.PhysRegs[oDep].Dealloc ();
						this.RenameTable[oDep] = loadStoreQueueEntry.OldPhysRegs[oDep];
					}
					
					loadStoreQueueEntry.PhysRegs.Clear ();
					
					this.LoadStoreQueue.TakeBack ();
				}
				
				reorderBufferEntry.Invalidate ();
				
				foreach (RegisterDependency oDep in reorderBufferEntry.ODeps) {
					reorderBufferEntry.PhysRegs[oDep].Dealloc ();
					this.RenameTable[oDep] = reorderBufferEntry.OldPhysRegs[oDep];
				}
				
				reorderBufferEntry.PhysRegs.Clear ();
				
				this.ReorderBuffer.TakeBack ();
			}
		}

		public uint GetSyscallArg (int i)
		{
			Debug.Assert (i < 6);
			return this.Regs.IntRegs[(uint)(RegisterConstants.FirstArgumentReg + i)];
		}

		public void SetSyscallArg (int i, uint val)
		{
			Debug.Assert (i < 6);
			this.Regs.IntRegs[(uint)(RegisterConstants.FirstArgumentReg + i)] = val;
		}

		public void SetSyscallReturn (int returnVal)
		{
			this.Regs.IntRegs[RegisterConstants.ReturnValueReg] = (uint)returnVal;
			this.Regs.IntRegs[RegisterConstants.SyscallSuccessReg] = (returnVal == -(int)Errno.EINVAL ? 1u : 0);
		}

		public void Syscall (uint callNum)
		{
			this.SyscallEmul.DoSyscall (callNum, this);
		}

		public void ClearArchRegs ()
		{
			this.Regs = new CombinedRegisterFile ();
			this.Regs.Pc = 0;
			this.Regs.Npc = 0;
			this.Regs.Nnpc = 0;
		}

		public void Halt (int exitCode)
		{
			if (this.State != ThreadState.Halted) {
				Logger.Infof (LogCategory.SIMULATOR, "target called exit({0:d})", exitCode);
				this.State = ThreadState.Halted;
				this.Core.Processor.ActiveThreadCount--;
			} else {
				throw new Exception("Halted thread can not be halted again.");
			}
		}

		public string Name {
			get { return "c" + this.Core.Num + "t" + this.Num; }
		}

		public uint Num { get; set; }

		public ThreadState State { get; set; }

		public Core Core { get; set; }

		public Process Process { get; set; }
		public SyscallEmul SyscallEmul { get; set; }

		public Memory Mem { get; set; }

		public CombinedRegisterFile Regs { get; set; }

		public uint FetchPc { get; set; }
		public uint FetchNpc { get; set; }
		public uint FetchNnpc { get; set; }

		public bool FetchStalled { get; set; }
		public uint LastFetchedBlock { get; set; }

		public Bpred Bpred { get; set; }

		public RegisterRenameTable RenameTable { get; set; }

		public uint CommitWidth { get; set; }
		public ulong LastCommitCycle { get; set; }

		public DecodeBuffer DecodeBuffer { get; set; }
		public ReorderBuffer ReorderBuffer { get; set; }
		public LoadStoreQueue LoadStoreQueue { get; set; }

		public bool IsSpeculative { get; set; }

		public ContextStat Stat { get; set; }

		public static uint COMMIT_TIMEOUT = 1000;//000;
	}

	public class Event<EventTypeT, EventContextT>
	{
		public Event (EventTypeT eventType, EventContextT context, ulong scheduled, ulong when)
		{
			this.EventType = eventType;
			this.Context = context;
			this.Scheduled = scheduled;
			this.When = when;
		}

		public override string ToString ()
		{
			return string.Format ("[Event: EventType={0}, Context={1}, Scheduled={2}, When={3}]", this.EventType, this.Context, this.Scheduled, this.When);
		}

		public EventTypeT EventType { get; set; }
		public EventContextT Context { get; set; }
		public ulong Scheduled { get; set; }
		public ulong When { get; set; }
	}

	public class DelegateEventQueue : EventProcessor
	{
		public class EventT
		{
			public EventT (VoidDelegate del, ulong when)
			{
				this.Del = del;
				this.When = when;
			}

			public VoidDelegate Del { get; set; }
			public ulong When { get; set; }
		}

		public DelegateEventQueue ()
		{
			this.Events = new Dictionary<ulong, List<EventT>> ();
		}

		public void AdvanceOneCycle ()
		{
			if (this.Events.ContainsKey (this.CurrentCycle)) {
				foreach (EventT evt in this.Events[this.CurrentCycle]) {
					evt.Del ();
				}
				
				this.Events.Remove (this.CurrentCycle);
			}
			
			this.CurrentCycle++;
		}

		public void Schedule (VoidDelegate del, ulong delay)
		{
			ulong when = this.CurrentCycle + delay;
			
			if (!this.Events.ContainsKey (when)) {
				this.Events[when] = new List<EventT> ();
			}
			
			this.Events[when].Add (new EventT (del, when));
		}

		public ulong CurrentCycle { get; set; }
		public Dictionary<ulong, List<EventT>> Events { get; private set; }
	}

	public interface EventProcessor
	{
		void AdvanceOneCycle ();
	}

	public interface ICycleProvider
	{
		ulong CurrentCycle { get; set; }
		List<EventProcessor> EventProcessors { get; set; }
		void AdvanceOneCycle ();
	}

	public class MemorySystem : ICycleProvider
	{
		public MemorySystem (Processor processor)
		{
			this.Processor = processor;
			
			this.CurrentCycle = 0;
			
			this.EventProcessors = new List<EventProcessor> ();
			
			this.CreateMemoryHierarchy ();
		}

		public void AdvanceOneCycle ()
		{
			foreach (EventProcessor eventProcessor in this.EventProcessors) {
				eventProcessor.AdvanceOneCycle ();
			}
			
			this.CurrentCycle++;
		}

		private void CreateMemoryHierarchy ()
		{
			this.Mem = new MemoryController (this, this.Processor.Simulation.Config.Architecture.MainMemory, this.Processor.Simulation.Stat.MainMemory);
			
			this.L2 = new CoherentCache (this, this.Processor.Simulation.Config.Architecture.L2Cache, this.Processor.Simulation.Stat.L2Cache);
			this.L2.Next = this.Mem;
			
			for (int i = 0; i < this.Processor.Simulation.Config.Architecture.Processor.Cores.Count; i++) {
				Core core = this.Processor.Cores[i];
				
				CoherentCache l1I = new CoherentCache (core, 
				                                       this.Processor.Simulation.Config.Architecture.Processor.Cores[i].ICache, 
				                                       this.Processor.Simulation.Stat.Processor.Cores[i].ICache);
				Sequencer seqI = new Sequencer ("seqI-" + i, l1I);
				
				CoherentCache l1D = new CoherentCache (core, 
				                                       this.Processor.Simulation.Config.Architecture.Processor.Cores[i].DCache, 
				                                       this.Processor.Simulation.Stat.Processor.Cores[i].DCache);
				Sequencer seqD = new Sequencer ("seqD-" + i, l1D);
				
				core.SeqI = seqI;
				core.L1I = l1I;
				
				core.SeqD = seqD;
				core.L1D = l1D;
				
				l1I.Next = l1D.Next = this.L2;
			}
			
			this.MMU = new MMU ();
		}

		public CoherentCache L2 { get; set; }

		public MemoryController Mem { get; set; }

		public MMU MMU { get; set; }

		public Processor Processor {get;set;}
		
		public ulong CurrentCycle { get; set; }

		public List<EventProcessor> EventProcessors { get; set; }
	}

	public class Processor
	{
		public Processor (Simulation simulation)
		{
			this.Simulation = simulation;
			
			this.Cores = new List<Core> ();
			
			this.CurrentCycle = 0;
			
			this.ActiveThreadCount = 0;
			
			for (uint i = 0; i < this.Simulation.Config.Architecture.Processor.Cores.Count; i++) {
				Core core = new Core (this, this.Simulation.Config.Architecture.Processor, i);
				
				for (uint j = 0; j < this.Simulation.Config.Architecture.Processor.NumThreadsPerCore; j++) {
					ContextConfig context = this.Simulation.Config.Contexts[(int)(i * this.Simulation.Config.Architecture.Processor.NumThreadsPerCore + j)];
					
					List<string> args = new List<string> ();
					args.Add (context.Workload.Cwd + Path.DirectorySeparatorChar + context.Workload.Exe + ".mipsel");
					args.AddRange (context.Workload.Args.Split (' '));
					
					Process process = new Process (context.Workload.Cwd, args);
					
					uint threadNum = i * this.Simulation.Config.Architecture.Processor.NumThreadsPerCore + j;
					ContextStat contextStat = this.Simulation.Stat.Processor.Contexts[(int)threadNum];
					
					Thread thread = new Thread (core, this.Simulation.Config.Architecture.Processor, contextStat, j, process);
					
					core.Threads.Add (thread);
					
					this.ActiveThreadCount++;
				}
				
				this.Cores.Add (core);
			}
			
			this.MemorySystem = new MemorySystem (this);
		}

		public bool CanRun {
			get { return this.ActiveThreadCount > 0; }
		}

		public void Run ()
		{
			DateTime beginTime = DateTime.Now;
			
//			Barrier barrier = new Barrier(this.Cores.Count);
//			barrier.Wait();
			
			while (this.CanRun && this.Simulation.Running) {
				foreach (Core core in this.Cores) {
					core.AdvanceOneCycle ();
				}
				
				this.MemorySystem.AdvanceOneCycle ();
				
				this.CurrentCycle++;
			}
			
			this.Simulation.Stat.TotalCycles = this.CurrentCycle;
			
			this.Simulation.Stat.Duration = (ulong)((DateTime.Now - beginTime).TotalSeconds);
			this.Simulation.Stat.InstsPerCycle = (double)this.Simulation.Stat.TotalInsts / this.Simulation.Stat.TotalCycles;
			this.Simulation.Stat.CyclesPerSecond = (double)this.Simulation.Stat.TotalCycles / this.Simulation.Stat.Duration;
		}

		public List<Core> Cores { get; set; }
		public MemorySystem MemorySystem { get; set; }

		public Simulation Simulation { get; set; }

		public ulong CurrentCycle { get; set; }

		public int ActiveThreadCount { get; set; }

		static Processor ()
		{
			WorkDirectory = DEFAULT_WORK_DIRECTORY;
		}

		public static string WorkDirectory { get; set; }
		
		public static string DEFAULT_WORK_DIRECTORY = "../../../";
	}
}
