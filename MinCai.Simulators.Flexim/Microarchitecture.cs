/*
 * Microarchitecture.cs
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MinCai.Simulators.Flexim.Architecture;
using MinCai.Simulators.Flexim.Architecture.Instructions;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.OperatingSystem;
using MinCai.Simulators.Flexim.Microarchitecture;
using Mono.Unix.Native;
using Process = MinCai.Simulators.Flexim.OperatingSystem.Process;

namespace MinCai.Simulators.Flexim.Microarchitecture
{
	public static class BranchPredictorConstants
	{
		public static uint BRANCH_SHIFT = 3;
	}

	public sealed class BranchTargetBufferEntry
	{
		public BranchTargetBufferEntry ()
		{
		}

		public uint Addr { get; set; }
		public StaticInstruction StaticInstruction { get; set; }
		public uint Target { get; set; }
		public BranchTargetBufferEntry Prev { get; set; }
		public BranchTargetBufferEntry Next { get; set; }
	}

	public interface BranchPredictorDir
	{
		byte[] Table { get; }
	}

	public sealed class BranchPredictorInfo
	{
		public BranchPredictorInfo (BranchPredictorDir dir, uint offset)
		{
			this.Dir = dir;
			this.Offset = offset;
		}

		public byte Value {
			get { return this.Dir.Table[this.Offset]; }
			set { this.Dir.Table[this.Offset] = value; }
		}

		public BranchPredictorDir Dir { get; private set; }
		public uint Offset { get; private set; }
	}

	public sealed class BimodBranchPredictorDir : BranchPredictorDir
	{
		public BimodBranchPredictorDir (uint size)
		{
			this.Size = size;
			this.Table = new byte[this.Size];
			
			byte flipFlop = 1;
			for (uint cnt = 0; cnt < this.Size; cnt++) {
				this.Table[cnt] = flipFlop;
				flipFlop = (byte)(3 - flipFlop);
			}
		}

		private uint Hash (uint baddr)
		{
			return (baddr >> 19) ^ (baddr >> (int)BranchPredictorConstants.BRANCH_SHIFT) & (this.Size - 1);
		}

		public BranchPredictorInfo Lookup (uint baddr)
		{
			return new BranchPredictorInfo (this, this.Hash (baddr));
		}

		public uint Size { get; private set; }
		public byte[] Table { get; private set; }
	}

	public sealed class TwoLevelBranchPredictorDir : BranchPredictorDir
	{
		public TwoLevelBranchPredictorDir (uint l1Size, uint l2Size, uint shiftWidth, bool xor)
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

		public BranchPredictorInfo Lookup (uint baddr)
		{
			uint l1Index = (baddr >> (int)BranchPredictorConstants.BRANCH_SHIFT) & (this.L1Size - 1);
			uint l2Index = this.ShiftRegs[l1Index];
			
			if (this.Xor) {
				l2Index = (uint)(((l2Index ^ (baddr >> (int)BranchPredictorConstants.BRANCH_SHIFT)) & ((1 << (int)this.ShiftWidth) - 1)) | ((baddr >> (int)BranchPredictorConstants.BRANCH_SHIFT) << (int)this.ShiftWidth));
			} else {
				l2Index |= (baddr >> (int)BranchPredictorConstants.BRANCH_SHIFT) << (int)this.ShiftWidth;
			}
			
			l2Index &= (this.L2Size - 1);
			
			return new BranchPredictorInfo (this, l2Index);
		}

		public byte[] Table {
			get { return this.L2Table; }
		}

		public uint L1Size { get; private set; }
		public uint L2Size { get; private set; }
		public uint ShiftWidth { get; private set; }
		public bool Xor { get; private set; }
		public uint[] ShiftRegs { get; private set; }
		public byte[] L2Table { get; private set; }
	}

	public sealed class BranchTargetBuffer
	{
		public BranchTargetBuffer (uint numSets, uint associativity)
		{
			this.NumSets = numSets;
			this.Associativity = associativity;
			
			this.Entries = new BranchTargetBufferEntry[this.NumSets * this.Associativity];
			for (uint i = 0; i < this.NumSets * this.Associativity; i++) {
				this[i] = new BranchTargetBufferEntry ();
			}
			
			if (this.Associativity > 1) {
				for (uint i = 0; i < this.NumSets * this.Associativity; i++) {
					if (i % this.Associativity != (this.Associativity - 1)) {
						this[i].Next = this[i + 1];
					} else {
						this[i].Next = null;
					}
					
					if (i % this.Associativity != (this.Associativity - 1)) {
						this[i + 1].Prev = this[i];
					}
				}
			}
		}

		public BranchTargetBufferEntry this[uint index] {
			get { return this.Entries[index]; }
			set { this.Entries[index] = value; }
		}

		public uint NumSets { get; private set; }
		public uint Associativity { get; private set; }
		public BranchTargetBufferEntry[] Entries { get; private set; }
	}

	public sealed class ReturnAddressStack
	{
		public ReturnAddressStack (uint size)
		{
			this.Size = size;
			this.Entries = new BranchTargetBufferEntry[this.Size];
			for (uint i = 0; i < this.Size; i++) {
				this[i] = new BranchTargetBufferEntry ();
			}
			
			this.TopOfStack = this.Size - 1;
		}

		public BranchTargetBufferEntry this[uint index] {
			get { return this.Entries[index]; }
			set { this.Entries[index] = value; }
		}

		public uint Size { get; private set; }
		public uint TopOfStack { get; set; }
		public BranchTargetBufferEntry[] Entries { get; private set; }
	}

	public sealed class BranchPredictorUpdate
	{
		public BranchPredictorUpdate ()
		{
		}

		public BranchPredictorInfo Pdir1 { get; set; }
		public BranchPredictorInfo Pdir2 { get; set; }
		public BranchPredictorInfo Pmeta { get; set; }

		public bool Ras { get; set; }
		public bool Bimod { get; set; }
		public bool TwoLevel { get; set; }
		public bool Meta { get; set; }
	}

	public interface IBranchPredictor
	{
		uint Lookup (uint baddr, uint btarget, DynamicInstruction dynamicInst, out BranchPredictorUpdate dirUpdate, out uint stackRecoverIndex);

		void Recover (uint baddr, uint stackRecoverIdx);

		void Update (uint baddr, uint btarget, bool isTaken, bool isPredTaken, bool isCorrect, DynamicInstruction dynamicInst, BranchPredictorUpdate dirUpdate);
	}

	public sealed class CombinedBranchPredictor : IBranchPredictor
	{
		public CombinedBranchPredictor () : this(65536, 1, 65536, 65536, 16, true, 1024, 4, 1024)
		{
		}

		public CombinedBranchPredictor (uint bimodSize, uint l1Size, uint l2Size, uint metaSize, uint shiftWidth, bool xor, uint btbSets, uint btbAssoc, uint rasSize)
		{
			this.TwoLevel = new TwoLevelBranchPredictorDir (l1Size, l2Size, shiftWidth, xor);
			this.Bimod = new BimodBranchPredictorDir (bimodSize);
			this.Meta = new BimodBranchPredictorDir (metaSize);
			
			this.Btb = new BranchTargetBuffer (btbSets, btbAssoc);
			this.Ras = new ReturnAddressStack (rasSize);
		}

		public uint Lookup (uint baddr, uint btarget, DynamicInstruction dynamicInst, out BranchPredictorUpdate dirUpdate, out uint stackRecoverIndex)
		{
			StaticInstruction staticInst = dynamicInst.StaticInstruction;
			
			if (!staticInst.IsControl) {
				dirUpdate = null;
				stackRecoverIndex = 0;
				
				return 0;
			}
			
			dirUpdate = new BranchPredictorUpdate ();
			dirUpdate.Ras = false;
			dirUpdate.Pdir1 = null;
			dirUpdate.Pdir2 = null;
			dirUpdate.Pmeta = null;
			
			if (staticInst.IsControl && !staticInst.IsUnconditionalBranch) {
				BranchPredictorInfo bimodCtr = this.Bimod.Lookup (baddr);
				BranchPredictorInfo twoLevelCtr = this.TwoLevel.Lookup (baddr);
				BranchPredictorInfo metaCtr = this.Meta.Lookup (baddr);
				
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
			
			if (this.Ras.Size > 0) {
				stackRecoverIndex = this.Ras.TopOfStack;
			} else {
				stackRecoverIndex = 0;
			}
			
			if (staticInst.IsFunctionReturn && this.Ras.Size > 0) {
				this.Ras.TopOfStack = (this.Ras.TopOfStack + this.Ras.Size - 1) % this.Ras.Size;
				dirUpdate.Ras = true;
			}
			
			if (staticInst.IsFunctionCall && this.Ras.Size > 0) {
				this.Ras.TopOfStack = (this.Ras.TopOfStack + 1) % this.Ras.Size;
				this.Ras[this.Ras.TopOfStack].Target = (uint)(baddr + Marshal.SizeOf (typeof(uint)));
			}
			
			uint index = (baddr >> (int)BranchPredictorConstants.BRANCH_SHIFT) & (this.Btb.NumSets - 1);
			
			BranchTargetBufferEntry btbEntry = null;
			
			if (this.Btb.Associativity > 1) {
				index *= this.Btb.Associativity;
				
				for (uint i = index; i < (index + this.Btb.Associativity); i++) {
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
			
			if (staticInst.IsControl && staticInst.IsUnconditionalBranch) {
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
			this.Ras.TopOfStack = stackRecoverIdx;
		}

		public void Update (uint baddr, uint btarget, bool isTaken, bool isPredTaken, bool isCorrect, DynamicInstruction dynamicInst, BranchPredictorUpdate dirUpdate)
		{
			StaticInstruction staticInst = dynamicInst.StaticInstruction;
			
			BranchTargetBufferEntry btbEntry = null;
			
			if (!staticInst.IsControl) {
				return;
			}
			
			if (staticInst.IsControl && !staticInst.IsUnconditionalBranch) {
				uint l1Index = (baddr >> (int)BranchPredictorConstants.BRANCH_SHIFT) & (this.TwoLevel.L1Size - 1);
				uint shiftReg = (this.TwoLevel.ShiftRegs[l1Index] << 1) | (uint)(isTaken ? 1 : 0);
				this.TwoLevel.ShiftRegs[l1Index] = (uint)(shiftReg & ((1 << (int)this.TwoLevel.ShiftWidth) - 1));
			}
			
			if (isTaken) {
				uint index = (baddr >> (int)BranchPredictorConstants.BRANCH_SHIFT) & (this.Btb.NumSets - 1);
				
				if (this.Btb.Associativity > 1) {
					index *= this.Btb.Associativity;
					
					BranchTargetBufferEntry lruHead = null, lruItem = null;
					
					for (uint i = index; i < (index + this.Btb.Associativity); i++) {
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
				if (isTaken) {
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
				if (isTaken) {
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
					if (dirUpdate.TwoLevel == isTaken) {
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
					if (!isCorrect) {
						btbEntry.Target = btarget;
					}
				} else {
					btbEntry.Addr = baddr;
					btbEntry.StaticInstruction = staticInst;
					btbEntry.Target = btarget;
				}
			}
		}

		public TwoLevelBranchPredictorDir TwoLevel { get; set; }
		public BimodBranchPredictorDir Bimod { get; set; }
		public BimodBranchPredictorDir Meta { get; set; }

		public BranchTargetBuffer Btb { get; set; }
		public ReturnAddressStack Ras { get; set; }
	}

	public sealed class FunctionalUnit
	{
		public enum Types : uint
		{
			None = 0,
			IntALU,
			IntMultiply,
			IntDivide,
			FloatAdd,
			FloatCompare,
			FloatConvert,
			FloatMultiply,
			FloatDivide,
			FloatSquareRoot,
			ReadPort,
			WritePort
		}

		public FunctionalUnit (FunctionalUnitPool pool, Types type, uint operationLatency, uint issueLatency)
		{
			this.Pool = pool;
			this.Type = type;
			this.OperationLatency = operationLatency;
			this.IssueLatency = issueLatency;
		}

		public void Acquire (ReorderBufferEntry reorderBufferEntry, Action onCompletedCallback)
		{
			this.Pool.EventQueue.Schedule (() =>
			{
				this.IsBusy = false;
				onCompletedCallback ();
			}, this.IssueLatency + this.OperationLatency);
			this.IsBusy = true;
		}

		public FunctionalUnitPool Pool { get; private set; }
		public Types Type { get; private set; }
		public uint OperationLatency { get; private set; }
		public uint IssueLatency { get; private set; }
		public bool IsBusy { get; private set; }
	}

	public sealed class FunctionalUnitPool
	{
		public FunctionalUnitPool (Core core)
		{
			this.Core = core;
			
			this.Name = "c" + this.Core.Num + ".fuPool";
			
			this.Entities = new Dictionary<FunctionalUnit.Types, List<FunctionalUnit>> ();
			
			this.Add (FunctionalUnit.Types.IntALU, 4, 1, 1);
			this.Add (FunctionalUnit.Types.IntMultiply, 1, 3, 1);
			this.Add (FunctionalUnit.Types.IntDivide, 1, 20, 19);
			this.Add (FunctionalUnit.Types.ReadPort, 2, 1, 1);
			this.Add (FunctionalUnit.Types.WritePort, 2, 1, 1);
			this.Add (FunctionalUnit.Types.FloatAdd, 4, 2, 1);
			this.Add (FunctionalUnit.Types.FloatCompare, 4, 2, 1);
			this.Add (FunctionalUnit.Types.FloatConvert, 4, 2, 1);
			this.Add (FunctionalUnit.Types.FloatMultiply, 1, 4, 1);
			this.Add (FunctionalUnit.Types.FloatDivide, 1, 12, 12);
			this.Add (FunctionalUnit.Types.FloatSquareRoot, 1, 24, 24);
			
			this.EventQueue = new ActionEventQueue ();
			this.Core.EventProcessors.Add (this.EventQueue);
		}

		public void Add (FunctionalUnit.Types type, uint quantity, uint operationLatency, uint issueLatency)
		{
			this.Entities[type] = new List<FunctionalUnit> ();
			for (uint i = 0; i < quantity; i++) {
				this.Entities[type].Add (new FunctionalUnit (this, type, operationLatency, issueLatency));
			}
		}

		public FunctionalUnit FindFree (FunctionalUnit.Types type)
		{
			return this.Entities[type].Find (fu => !fu.IsBusy);
		}

		public void Acquire (ReorderBufferEntry reorderBufferEntry, Action<ReorderBufferEntry> onCompletedCallback)
		{
			FunctionalUnit.Types type = reorderBufferEntry.DynamicInstruction.StaticInstruction.FunctionalUnitType;
			FunctionalUnit fu = this.FindFree (type);
			
			if (fu != null) {
				fu.Acquire (reorderBufferEntry, () => onCompletedCallback (reorderBufferEntry));
			} else {
				this.EventQueue.Schedule (() => this.Acquire (reorderBufferEntry, onCompletedCallback), 10);
			}
		}

		public Core Core { get; private set; }
		public string Name { get; private set; }

		public Dictionary<FunctionalUnit.Types, List<FunctionalUnit>> Entities { get; private set; }
		public ActionEventQueue EventQueue { get; private set; }
	}

	public sealed class PhysicalRegister
	{
		public enum States
		{
			Free,
			Allocated,
			WrittenBack,
			Architectural
		}

		public PhysicalRegister (PhysicalRegisterFile file)
		{
			this.File = file;
			this.State = States.Free;
		}

		public void Alloc (ReorderBufferEntry reorderBufferEntry)
		{
			this.State = States.Allocated;
			this.ReorderBufferEntry = reorderBufferEntry;
		}

		public void Writeback ()
		{
			this.State = States.WrittenBack;
		}

		public void Commit ()
		{
			this.State = States.Architectural;
		}

		public void Dealloc ()
		{
			this.State = States.Free;
			this.ReorderBufferEntry = null;
		}

		public bool IsReady {
			get { return this.State == States.WrittenBack || this.State == States.Architectural; }
		}

		public PhysicalRegisterFile File { get; private set; }
		public States State { get; private set; }

		public ReorderBufferEntry ReorderBufferEntry { get; set; }
	}

	public sealed class NoFreePhysicalRegisterException : Exception
	{
		public NoFreePhysicalRegisterException () : base("NoFreePhysicalRegisterException")
		{
		}
	}

	public sealed class PhysicalRegisterFile
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
			return this.Entries.Find (physReg => physReg.State == PhysicalRegister.States.Free);
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

		public PhysicalRegister this[uint index] {
			get { return this.Entries[(int)index]; }
			set { this.Entries[(int)index] = value; }
		}

		public Core Core { get; private set; }
		public string Name { get; private set; }
		public uint Capacity { get; private set; }
		public List<PhysicalRegister> Entries { get; private set; }
	}

	public sealed class RegisterRenameTable
	{
		public RegisterRenameTable (Thread thread)
		{
			this.Thread = thread;
			this.Name = "c" + this.Thread.Core.Num + "t" + this.Thread.Num + ".renameTable";
			
			this.Entries = new Dictionary<RegisterDependency.Types, Dictionary<uint, PhysicalRegister>> ();
			
			this.Entries[RegisterDependency.Types.Integer] = new Dictionary<uint, PhysicalRegister> ();
			this.Entries[RegisterDependency.Types.Float] = new Dictionary<uint, PhysicalRegister> ();
			this.Entries[RegisterDependency.Types.Misc] = new Dictionary<uint, PhysicalRegister> ();
		}

		public PhysicalRegister this[RegisterDependency dep] {
			get { return this[dep.Type, dep.Num]; }
			set { this[dep.Type, dep.Num] = value; }
		}

		public PhysicalRegister this[RegisterDependency.Types type, uint Num] {
			get { return this.Entries[type][Num]; }
			set { this.Entries[type][Num] = value; }
		}

		public Thread Thread { get; private set; }
		public string Name { get; private set; }
		public Dictionary<RegisterDependency.Types, Dictionary<uint, PhysicalRegister>> Entries { get; private set; }
	}

	public sealed class DecodeBufferEntry
	{
		public DecodeBufferEntry (DynamicInstruction dynamicInst)
		{
			this.Id = CurrentId++;
			this.DynamicInstruction = dynamicInst;
		}

		public ulong Id { get; private set; }
		public uint Npc { get; set; }
		public uint Nnpc { get; set; }
		public uint PredNpc { get; set; }
		public uint PredNnpc { get; set; }
		public DynamicInstruction DynamicInstruction { get; private set; }

		public bool IsSpeculative { get; set; }
		public uint StackRecoverIndex { get; set; }
		public BranchPredictorUpdate DirUpdate { get; set; }

		static DecodeBufferEntry ()
		{
			CurrentId = 0;
		}

		public static ulong CurrentId { get; private set; }
	}

	public sealed class ReorderBufferEntry
	{
		public ReorderBufferEntry (DynamicInstruction dynamicInst, List<RegisterDependency> iDeps, List<RegisterDependency> oDeps)
		{
			this.Id = CurrentId++;
			this.DynamicInstruction = dynamicInst;
			this.IDeps = iDeps;
			this.ODeps = oDeps;
			
			this.OldPhysRegs = new Dictionary<RegisterDependency, PhysicalRegister> ();
			this.PhysRegs = new Dictionary<RegisterDependency, PhysicalRegister> ();
			this.SrcPhysRegs = new Dictionary<RegisterDependency, PhysicalRegister> ();
			
			this.IsValid = true;
		}

		public void SignalCompleted ()
		{
			this.DynamicInstruction.Thread.Core.OoOEventQueue.Add (this);
		}

		public void Invalidate ()
		{
			this.IsValid = false;
		}

		public bool IsAllOperandsReady {
			get { return this.IDeps.All (iDep => this.SrcPhysRegs[iDep].IsReady); }
		}

		public bool IsStoreAddressReady {
			get {
				MemoryOp memOp = this.DynamicInstruction.StaticInstruction as MemoryOp;
				return this.SrcPhysRegs[memOp.MemIDeps[0]].IsReady;
			}
		}

		public bool IsStoreOperandReady {
			get {
				MemoryOp memOp = this.DynamicInstruction.StaticInstruction as MemoryOp;
				return memOp.MemIDeps.GetRange (1, memOp.MemIDeps.Count - 1).All (iDep => this.SrcPhysRegs[iDep].IsReady);
			}
		}

		public bool IsInLoadStoreQueue {
			get { return this.DynamicInstruction.StaticInstruction.IsMemory && this.LoadStoreQueueEntry == null; }
		}

		public bool IsEAComputation {
			get { return this.DynamicInstruction.StaticInstruction.IsMemory && this.LoadStoreQueueEntry != null; }
		}

		public ulong Id { get; private set; }

		public uint Npc { get; set; }
		public uint Nnpc { get; set; }
		public uint PredNpc { get; set; }
		public uint PredNnpc { get; set; }
		public DynamicInstruction DynamicInstruction { get; private set; }

		public List<RegisterDependency> IDeps { get; private set; }
		public List<RegisterDependency> ODeps { get; private set; }
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
		public BranchPredictorUpdate DirUpdate { get; set; }

		static ReorderBufferEntry ()
		{
			CurrentId = 0;
		}

		public static ulong CurrentId { get; private set; }
	}

	public sealed class Core : ICore
	{
		public Core (IProcessor processor, uint num)
		{
			this.Processor = processor;
			this.Num = num;
			
			this.CurrentCycle = 0;
			
			this.EventProcessors = new List<EventProcessor> ();
			
			this.Threads = new List<IThread> ();
			
			this.ICache = new CoherentCache (this, this.Processor.Config.Cores[(int)this.Num].ICache, this.Processor.Simulation.Stat.Processor.Cores[(int)this.Num].ICache);
			this.DCache = new CoherentCache (this, this.Processor.Config.Cores[(int)this.Num].DCache, this.Processor.Simulation.Stat.Processor.Cores[(int)this.Num].DCache);
			
			this.DecodeWidth = processor.Config.DecodeWidth;
			this.IssueWidth = processor.Config.IssueWidth;
			
			this.IntRegFile = new PhysicalRegisterFile (this, "intRegFile", processor.Config.PhysicalRegisterFileCapacity);
			this.FpRegFile = new PhysicalRegisterFile (this, "fpRegFile", processor.Config.PhysicalRegisterFileCapacity);
			this.MiscRegFile = new PhysicalRegisterFile (this, "miscRegFile", processor.Config.PhysicalRegisterFileCapacity);
			
			this.FuPool = new FunctionalUnitPool (this);
			
			this.Isa = new Mips32InstructionSetArchitecture ();
			
			this.ReadyQueue = new List<ReorderBufferEntry> ();
			this.WaitingQueue = new List<ReorderBufferEntry> ();
			this.OoOEventQueue = new List<ReorderBufferEntry> ();
			
			this.RegisterRenameScheduler = new RoundRobinScheduler<IThread> (this.Threads, thread => thread.DecodeBuffer.Any () && !thread.ReorderBuffer.IsFull (this.Processor.Config.ReorderBufferCapacity) && !thread.LoadStoreQueue.IsFull (this.Processor.Config.LoadStoreQueueCapacity), thread => thread.RegisterRenameOne (), (int)this.DecodeWidth);
			this.DispatchScheduler = new RoundRobinScheduler<IThread> (this.Threads, thread => thread.ReorderBuffer.Any (entry => !entry.IsDispatched), thread => thread.DispatchOne (), (int)this.DecodeWidth);
		}

		public void Fetch ()
		{
			this.Threads.ForEach (thread => thread.Fetch ());
		}

		public void RegisterRename ()
		{
			this.RegisterRenameScheduler.ConsumeNext ();
		}

		public void Dispatch ()
		{
			this.DispatchScheduler.ConsumeNext ();
		}

		public void Wakeup ()
		{
			List<ReorderBufferEntry> toWaitingQueue = new List<ReorderBufferEntry> ();
			
			while (this.WaitingQueue.Any ()) {
				ReorderBufferEntry waitingQueueEntry = this.WaitingQueue.First ();
				
				if (!waitingQueueEntry.IsValid) {
					this.WaitingQueue.RemoveFirst ();
					continue;
				}
				
				if (waitingQueueEntry.IsAllOperandsReady) {
					this.ReadyQueue.Add (waitingQueueEntry);
					waitingQueueEntry.IsInReadyQueue = true;
				} else {
					toWaitingQueue.Add (waitingQueueEntry);
				}
				
				this.WaitingQueue.RemoveFirst ();
			}
			
			this.WaitingQueue.AddRange (toWaitingQueue);
		}

		public void Selection ()
		{
			uint numIssued = 0;
			
			while (numIssued < this.IssueWidth && this.ReadyQueue.Any ()) {
				ReorderBufferEntry readyQueueEntry = this.ReadyQueue.First ();
				
				if (readyQueueEntry.IsInLoadStoreQueue && readyQueueEntry.DynamicInstruction.StaticInstruction.IsStore) {
					readyQueueEntry.IsIssued = true;
					readyQueueEntry.IsCompleted = true;
				} else if (readyQueueEntry.IsInLoadStoreQueue && readyQueueEntry.DynamicInstruction.StaticInstruction.IsLoad) {
					this.FuPool.Acquire (readyQueueEntry, readyQueueEntry1 =>
					{
						bool isHitInLoadStoreQueue = readyQueueEntry1.DynamicInstruction.Thread.LoadStoreQueue.Any (loadStoreQueueEntry => loadStoreQueueEntry.DynamicInstruction.StaticInstruction.IsStore && loadStoreQueueEntry.Ea == readyQueueEntry.Ea);
						
						if (isHitInLoadStoreQueue) {
							readyQueueEntry1.SignalCompleted ();
						} else {
							readyQueueEntry1.DynamicInstruction.Thread.Load (this.Processor.MMU.GetPhysicalAddress (readyQueueEntry1.DynamicInstruction.Thread.MemoryMapId, readyQueueEntry1.Ea), false, () => readyQueueEntry1.SignalCompleted ());
						}
					});
					
					readyQueueEntry.IsIssued = true;
				} else {
					if (readyQueueEntry.DynamicInstruction.StaticInstruction.FunctionalUnitType != FunctionalUnit.Types.None) {
						this.FuPool.Acquire (readyQueueEntry, readyQueueEntry1 => readyQueueEntry1.SignalCompleted ());
						readyQueueEntry.IsIssued = true;
					} else {
						readyQueueEntry.IsIssued = true;
						readyQueueEntry.IsCompleted = true;
					}
				}
				
				this.ReadyQueue.RemoveFirst ();
				readyQueueEntry.IsInReadyQueue = false;
				
				numIssued++;
			}
		}

		public void Writeback ()
		{
			while (this.OoOEventQueue.Any ()) {
				ReorderBufferEntry reorderBufferEntry = this.OoOEventQueue.First ();
				
				if (!reorderBufferEntry.IsValid) {
					this.OoOEventQueue.RemoveFirst ();
					continue;
				}
				
				reorderBufferEntry.IsCompleted = true;
				
				foreach (var oDep in reorderBufferEntry.ODeps) {
					reorderBufferEntry.PhysRegs[oDep].Writeback ();
				}
				
				if (reorderBufferEntry.IsSpeculative) {
					reorderBufferEntry.DynamicInstruction.Thread.Bpred.Recover (reorderBufferEntry.DynamicInstruction.PhysPc, reorderBufferEntry.StackRecoverIndex);
					
					reorderBufferEntry.DynamicInstruction.Thread.Regs.IsSpeculative = reorderBufferEntry.DynamicInstruction.Thread.IsSpeculative = false;
					reorderBufferEntry.DynamicInstruction.Thread.FetchNpc = reorderBufferEntry.DynamicInstruction.Thread.Regs.Npc;
					reorderBufferEntry.DynamicInstruction.Thread.FetchNnpc = reorderBufferEntry.DynamicInstruction.Thread.Regs.Nnpc;
					
					reorderBufferEntry.DynamicInstruction.Thread.RecoverReorderBuffer (reorderBufferEntry);
					
					break;
				}
				
				this.OoOEventQueue.RemoveFirst ();
			}
		}

		public void RefreshLoadStoreQueue ()
		{
			this.Threads.ForEach (thread => thread.RefreshLoadStoreQueue ());
		}

		public void Commit ()
		{
			this.Threads.ForEach (thread => thread.Commit ());
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
			
			this.EventProcessors.ForEach (eventProcessor => eventProcessor.AdvanceOneCycle ());
			
			this.CurrentCycle++;
		}

		public CoherentCache ICache { get; private set; }
		public CoherentCache DCache { get; private set; }

		public uint Num { get; private set; }
		public IProcessor Processor { get; private set; }
		public List<IThread> Threads { get; private set; }

		public uint DecodeWidth { get; private set; }
		public uint IssueWidth { get; private set; }

		public FunctionalUnitPool FuPool { get; private set; }

		public PhysicalRegisterFile IntRegFile { get; private set; }
		public PhysicalRegisterFile FpRegFile { get; private set; }
		public PhysicalRegisterFile MiscRegFile { get; private set; }

		public InstructionSetArchitecture Isa { get; private set; }

		public List<ReorderBufferEntry> ReadyQueue { get; private set; }
		public List<ReorderBufferEntry> WaitingQueue { get; private set; }
		public List<ReorderBufferEntry> OoOEventQueue { get; private set; }

		public ulong CurrentCycle { get; private set; }

		public List<EventProcessor> EventProcessors { get; private set; }

		private RoundRobinScheduler<IThread> RegisterRenameScheduler { get; set; }
		private RoundRobinScheduler<IThread> DispatchScheduler { get; set; }
	}

	public sealed class Thread : IThread
	{
		public enum ThreadState
		{
			Inactive,
			Active,
			Halted
		}

		public Thread (ICore core, ContextStat stat, uint num, Process process)
		{
			this.Core = core;
			
			this.Num = num;
			
			this.Process = process;
			
			this.Bpred = new CombinedBranchPredictor ();
			
			this.RenameTable = new RegisterRenameTable (this);
			
			this.ClearArchRegs ();
			
			this.MemoryMapId = MemoryManagementUnit.CurrentMemoryMapId++;
			this.Mem = new Memory ();
			
			this.Process.Load (this);
			
			this.Stat = stat;
			
			this.Itlb = new TranslationLookasideBuffer (this.Core, this.Core.Processor.Config.Tlb, this.Stat.Itlb);
			this.Dtlb = new TranslationLookasideBuffer (this.Core, this.Core.Processor.Config.Tlb, this.Stat.Dtlb);
			
			this.State = ThreadState.Active;
			
			for (uint i = 0; i < RegisterConstants.NUM_INT_REGS; i++) {
				PhysicalRegister physReg = this.Core.IntRegFile[this.Num * RegisterConstants.NUM_INT_REGS + i];
				physReg.Commit ();
				this.RenameTable[RegisterDependency.Types.Integer, i] = physReg;
			}
			
			for (uint i = 0; i < RegisterConstants.NUM_FLOAT_REGS; i++) {
				PhysicalRegister physReg = this.Core.FpRegFile[this.Num * RegisterConstants.NUM_FLOAT_REGS + i];
				physReg.Commit ();
				this.RenameTable[RegisterDependency.Types.Float, i] = physReg;
			}
			
			for (uint i = 0; i < RegisterConstants.NUM_MISC_REGS; i++) {
				PhysicalRegister physReg = this.Core.MiscRegFile[this.Num * RegisterConstants.NUM_MISC_REGS + i];
				physReg.Commit ();
				this.RenameTable[RegisterDependency.Types.Misc, i] = physReg;
			}
			
			this.CommitWidth = core.Processor.Config.CommitWidth;
			
			this.DecodeBuffer = new List<DecodeBufferEntry> ();
			this.ReorderBuffer = new List<ReorderBufferEntry> ();
			this.LoadStoreQueue = new List<ReorderBufferEntry> ();
			
			this.FetchNpc = this.Regs.Npc;
			this.FetchNnpc = this.Regs.Nnpc;
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

		private DynamicInstruction DecodeAndExecute ()
		{
			this.Regs.Pc = this.Regs.Npc;
			this.Regs.Npc = this.Regs.Nnpc;
			this.Regs.Nnpc += (uint)Marshal.SizeOf (typeof(uint));
			
			StaticInstruction staticInst = this.Core.Isa.Decode (this.Regs.Pc, this.Mem);
			DynamicInstruction dynamicInst = new DynamicInstruction (this, this.Regs.Pc, staticInst);
			
			dynamicInst.Execute ();
			
			return dynamicInst;
		}

		public void Fetch ()
		{
			uint cacheLineToFetch = BitHelper.Aligned (this.FetchNpc, this.Core.ICache.Cache.LineSize);
			if (cacheLineToFetch != this.LastFetchedCacheLine) {
				this.LastFetchedCacheLine = cacheLineToFetch;
				
				this.IFetch (this.Core.Processor.MMU.GetPhysicalAddress (this.MemoryMapId, this.FetchNpc), false, () => this.IsFetchStalled = false);
				
				this.IsFetchStalled = true;
			}
			
			bool hasDone = false;
			
			while (!hasDone && !this.DecodeBuffer.IsFull (this.Core.Processor.Config.DecodeBufferCapcity) && !this.IsFetchStalled) {
				if (this.SetNpc ()) {
					this.Regs.IsSpeculative = this.IsSpeculative = true;
				}
				
				this.FetchPc = this.FetchNpc;
				this.FetchNpc = this.FetchNnpc;
				
				DynamicInstruction dynamicInst = this.DecodeAndExecute ();
				
				if (this.FetchNpc != this.FetchPc + Marshal.SizeOf (typeof(uint))) {
					hasDone = true;
				}
				
				if ((this.FetchPc + Marshal.SizeOf (typeof(uint))) % this.Core.ICache.Cache.LineSize == 0) {
					hasDone = true;
				}
				
				uint stackRecoverIndex;
				BranchPredictorUpdate dirUpdate = new BranchPredictorUpdate ();
				
				uint dest = this.Bpred.Lookup (this.Core.Processor.MMU.GetPhysicalAddress (this.MemoryMapId, this.FetchPc), 0, dynamicInst, out dirUpdate, out stackRecoverIndex);
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

		private PhysicalRegisterFile GetPhysicalRegisterFile (RegisterDependency.Types type)
		{
			if (type == RegisterDependency.Types.Integer) {
				return this.Core.IntRegFile;
			} else if (type == RegisterDependency.Types.Float) {
				return this.Core.FpRegFile;
			} else {
				return this.Core.MiscRegFile;
			}
		}

		public void RegisterRenameOne ()
		{
			DecodeBufferEntry decodeBufferEntry = this.DecodeBuffer.First ();
			
			this.Regs.IntRegs[RegisterConstants.ZERO_REG] = 0;
			
			DynamicInstruction dynamicInst = decodeBufferEntry.DynamicInstruction;
			
			if (!dynamicInst.StaticInstruction.IsNop) {
				ReorderBufferEntry reorderBufferEntry = new ReorderBufferEntry (dynamicInst, dynamicInst.StaticInstruction.IDeps, dynamicInst.StaticInstruction.ODeps);
				reorderBufferEntry.Npc = decodeBufferEntry.Npc;
				reorderBufferEntry.Nnpc = decodeBufferEntry.Nnpc;
				reorderBufferEntry.PredNpc = decodeBufferEntry.PredNpc;
				reorderBufferEntry.PredNnpc = decodeBufferEntry.PredNnpc;
				reorderBufferEntry.StackRecoverIndex = decodeBufferEntry.StackRecoverIndex;
				reorderBufferEntry.DirUpdate = decodeBufferEntry.DirUpdate;
				reorderBufferEntry.IsSpeculative = decodeBufferEntry.IsSpeculative;
				
				foreach (var iDep in reorderBufferEntry.IDeps) {
					reorderBufferEntry.SrcPhysRegs[iDep] = this.RenameTable[iDep];
				}
				
				foreach (var oDep in reorderBufferEntry.ODeps) {
					reorderBufferEntry.OldPhysRegs[oDep] = this.RenameTable[oDep];
					this.RenameTable[oDep] = reorderBufferEntry.PhysRegs[oDep] = this.GetPhysicalRegisterFile (oDep.Type).Alloc (reorderBufferEntry);
				}
				
				if (dynamicInst.StaticInstruction.IsMemory) {
					ReorderBufferEntry loadStoreQueueEntry = new ReorderBufferEntry (dynamicInst, (dynamicInst.StaticInstruction as MemoryOp).MemIDeps, (dynamicInst.StaticInstruction as MemoryOp).MemODeps);
					
					loadStoreQueueEntry.Npc = decodeBufferEntry.Npc;
					loadStoreQueueEntry.Nnpc = decodeBufferEntry.Nnpc;
					loadStoreQueueEntry.PredNpc = decodeBufferEntry.PredNpc;
					loadStoreQueueEntry.PredNnpc = decodeBufferEntry.PredNnpc;
					loadStoreQueueEntry.StackRecoverIndex = 0;
					loadStoreQueueEntry.DirUpdate = null;
					loadStoreQueueEntry.IsSpeculative = false;
					
					loadStoreQueueEntry.Ea = (dynamicInst.StaticInstruction as MemoryOp).Ea (this);
					
					reorderBufferEntry.LoadStoreQueueEntry = loadStoreQueueEntry;
					
					foreach (var iDep in loadStoreQueueEntry.IDeps) {
						loadStoreQueueEntry.SrcPhysRegs[iDep] = this.RenameTable[iDep];
					}
					
					foreach (var oDep in loadStoreQueueEntry.ODeps) {
						loadStoreQueueEntry.OldPhysRegs[oDep] = this.RenameTable[oDep];
						this.RenameTable[oDep] = loadStoreQueueEntry.PhysRegs[oDep] = this.GetPhysicalRegisterFile (oDep.Type).Alloc (loadStoreQueueEntry);
					}
					
					this.LoadStoreQueue.Add (loadStoreQueueEntry);
				}
				
				this.ReorderBuffer.Add (reorderBufferEntry);
			}
			
			this.DecodeBuffer.RemoveFirst ();
		}

		public void DispatchOne ()
		{
			ReorderBufferEntry reorderBufferEntry = this.ReorderBuffer.Find (entry => !entry.IsDispatched);
			
			if (reorderBufferEntry.IsAllOperandsReady) {
				this.Core.ReadyQueue.Add (reorderBufferEntry);
				reorderBufferEntry.IsInReadyQueue = true;
			} else {
				this.Core.WaitingQueue.Add (reorderBufferEntry);
			}
			
			reorderBufferEntry.IsDispatched = true;
			
			if (reorderBufferEntry.LoadStoreQueueEntry != null) {
				ReorderBufferEntry loadStoreQueueEntry = reorderBufferEntry.LoadStoreQueueEntry;
				
				if (loadStoreQueueEntry.DynamicInstruction.StaticInstruction.IsStore) {
					if (loadStoreQueueEntry.IsAllOperandsReady) {
						this.Core.ReadyQueue.Add (loadStoreQueueEntry);
						loadStoreQueueEntry.IsInReadyQueue = true;
					} else {
						this.Core.WaitingQueue.Add (loadStoreQueueEntry);
					}
				}
				
				loadStoreQueueEntry.IsDispatched = true;
			}
		}

		public void RefreshLoadStoreQueue ()
		{
			List<uint> stdUnknowns = new List<uint> ();
			
			foreach (var loadStoreQueueEntry in this.LoadStoreQueue) {
				if (loadStoreQueueEntry.DynamicInstruction.StaticInstruction.IsStore) {
					if (loadStoreQueueEntry.IsStoreAddressReady) {
						break;
					} else if (!loadStoreQueueEntry.IsAllOperandsReady) {
						stdUnknowns.Add (loadStoreQueueEntry.Ea);
					} else {
						stdUnknowns.RemoveAll (stdUnknown => stdUnknown == loadStoreQueueEntry.Ea);
					}
				}
				
				if (loadStoreQueueEntry.DynamicInstruction.StaticInstruction.IsLoad && loadStoreQueueEntry.IsDispatched && !loadStoreQueueEntry.IsInReadyQueue && !loadStoreQueueEntry.IsIssued && !loadStoreQueueEntry.IsCompleted && loadStoreQueueEntry.IsAllOperandsReady) {
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
				Logger.Fatalf (Logger.Categories.Simulator, "Thread {0:s} - No instruction committed for {1:d} cycles", this.Name, COMMIT_TIMEOUT);
			}
			
			uint numCommitted = 0;
			
			while (this.ReorderBuffer.Any () && numCommitted < this.CommitWidth) {
				ReorderBufferEntry reorderBufferEntry = this.ReorderBuffer.First ();
				
				if (!reorderBufferEntry.IsCompleted) {
					break;
				}
				
				if (reorderBufferEntry.IsEAComputation) {
					ReorderBufferEntry loadStoreQueueEntry = this.LoadStoreQueue.First ();
					
					if (!loadStoreQueueEntry.IsCompleted) {
						break;
					}
					
					if (loadStoreQueueEntry.DynamicInstruction.StaticInstruction.IsStore) {
						this.Core.FuPool.Acquire (loadStoreQueueEntry, loadStoreQueueEntry1 => this.Store (this.Core.Processor.MMU.GetPhysicalAddress (this.MemoryMapId, loadStoreQueueEntry1.Ea), false, () => { }));
					}
					
					foreach (var oDep in loadStoreQueueEntry.ODeps) {
						loadStoreQueueEntry.OldPhysRegs[oDep].Dealloc ();
						loadStoreQueueEntry.PhysRegs[oDep].Commit ();
					}
					
					this.LoadStoreQueue.RemoveFirst ();
				}
				
				foreach (var oDep in reorderBufferEntry.ODeps) {
					reorderBufferEntry.OldPhysRegs[oDep].Dealloc ();
					reorderBufferEntry.PhysRegs[oDep].Commit ();
				}
				
				if (reorderBufferEntry.DynamicInstruction.StaticInstruction.IsControl) {
					this.Bpred.Update (reorderBufferEntry.DynamicInstruction.PhysPc, reorderBufferEntry.Nnpc, reorderBufferEntry.Nnpc != (reorderBufferEntry.Npc + Marshal.SizeOf (typeof(uint))), reorderBufferEntry.PredNnpc != (reorderBufferEntry.Npc + Marshal.SizeOf (typeof(uint))), reorderBufferEntry.PredNnpc == reorderBufferEntry.Nnpc, reorderBufferEntry.DynamicInstruction, reorderBufferEntry.DirUpdate);
				}
				
				this.ReorderBuffer.RemoveFirst ();
				
				this.Stat.TotalInsts++;
				
				this.LastCommitCycle = this.Core.CurrentCycle;
				
				numCommitted++;
				
				this.Core.Processor.Simulation.Stat.TotalInsts++;
				
//				Logger.Infof (Logger.Categories.Debug, "instruction committed (dynamicInst={0})", reorderBufferEntry.DynamicInstruction);
			}
		}

		public void RecoverReorderBuffer (ReorderBufferEntry branchReorderBufferEntry)
		{
			Logger.Infof (Logger.Categories.Simulator, "RecoverReorderBuffer({0:s})", branchReorderBufferEntry);
			//TODO
			while (this.ReorderBuffer.Any ()) {
				ReorderBufferEntry reorderBufferEntry = this.ReorderBuffer.Last ();
				
				if (!reorderBufferEntry.IsSpeculative) {
					break;
				}
				
				if (reorderBufferEntry.IsEAComputation) {
					ReorderBufferEntry loadStoreQueueEntry = this.LoadStoreQueue.Last ();
					
					loadStoreQueueEntry.Invalidate ();
					
					foreach (var oDep in loadStoreQueueEntry.ODeps) {
						loadStoreQueueEntry.PhysRegs[oDep].Dealloc ();
						this.RenameTable[oDep] = loadStoreQueueEntry.OldPhysRegs[oDep];
					}
					
					loadStoreQueueEntry.PhysRegs.Clear ();
					
					this.LoadStoreQueue.RemoveLast ();
				}
				
				reorderBufferEntry.Invalidate ();
				
				foreach (var oDep in reorderBufferEntry.ODeps) {
					reorderBufferEntry.PhysRegs[oDep].Dealloc ();
					this.RenameTable[oDep] = reorderBufferEntry.OldPhysRegs[oDep];
				}
				
				reorderBufferEntry.PhysRegs.Clear ();
				
				this.ReorderBuffer.RemoveLast ();
			}
		}

		public uint GetSyscallArg (int i)
		{
			Debug.Assert (i < 6);
			return this.Regs.IntRegs[(uint)(RegisterConstants.FIRST_ARGUMENT_REG + i)];
		}

		public void SetSyscallArg (int i, uint val)
		{
			Debug.Assert (i < 6);
			this.Regs.IntRegs[(uint)(RegisterConstants.FIRST_ARGUMENT_REG + i)] = val;
		}

		public void SetSyscallReturn (int returnVal)
		{
			this.Regs.IntRegs[RegisterConstants.RETURN_VALUE_REG] = (uint)returnVal;
			this.Regs.IntRegs[RegisterConstants.SYSCALL_SUCCESS_REG] = (returnVal == -(int)Errno.EINVAL ? 1u : 0);
		}

		public void Syscall (uint callNum)
		{
			SyscallEmulation.DoSyscall (callNum, this);
		}

		private void ClearArchRegs ()
		{
			this.Regs = new CombinedRegisterFile ();
			this.Regs.Pc = 0;
			this.Regs.Npc = 0;
			this.Regs.Nnpc = 0;
		}

		public void Halt (int exitCode)
		{
			if (this.State != ThreadState.Halted) {
				Logger.Infof (Logger.Categories.Simulator, "target called exit({0:d})", exitCode);
				this.State = ThreadState.Halted;
				this.Core.Processor.ActiveThreadCount--;
			} else {
//				throw new Exception ("Halted thread can not be halted again."); //TODO
			}
		}

		public void IFetch (uint addr, bool isRetry, Action onCompletedCallback)
		{
			uint pending = 2;
			
			this.Itlb.Access (addr, () =>
			{
				pending--;
				
				if (pending == 0) {
					onCompletedCallback ();
				}
			});
			
			this.Core.ICache.Load (addr, isRetry, () =>
			{
				pending--;
				
				if (pending == 0) {
					onCompletedCallback ();
				}
			});
		}

		public void Load (uint addr, bool isRetry, Action onCompletedCallback)
		{
			uint pending = 2;
			
			this.Dtlb.Access (addr, () =>
			{
				pending--;
				
				if (pending == 0) {
					onCompletedCallback ();
				}
			});
			
			this.Core.DCache.Load (addr, isRetry, () =>
			{
				pending--;
				
				if (pending == 0) {
					onCompletedCallback ();
				}
			});
		}

		public void Store (uint addr, bool isRetry, Action onCompletedCallback)
		{
			uint pending = 2;
			
			this.Dtlb.Access (addr, () =>
			{
				pending--;
				
				if (pending == 0) {
					onCompletedCallback ();
				}
			});
			
			this.Core.DCache.Store (addr, isRetry, () =>
			{
				pending--;
				
				if (pending == 0) {
					onCompletedCallback ();
				}
			});
		}

		public string Name {
			get { return "c" + this.Core.Num + "t" + this.Num; }
		}

		public uint Num { get; private set; }

		public uint MemoryMapId { get; private set; }

		public ThreadState State { get; private set; }

		public ICore Core { get; private set; }

		public Process Process { get; private set; }

		public Memory Mem { get; private set; }

		public TranslationLookasideBuffer Itlb { get; private set; }
		public TranslationLookasideBuffer Dtlb { get; private set; }

		public CombinedRegisterFile Regs { get; private set; }

		public uint FetchPc { get; set; }
		public uint FetchNpc { get; set; }
		public uint FetchNnpc { get; set; }

		private bool IsFetchStalled { get; set; }
		private uint LastFetchedCacheLine { get; set; }

		public IBranchPredictor Bpred { get; set; }

		public RegisterRenameTable RenameTable { get; private set; }

		public uint CommitWidth { get; private set; }
		private ulong LastCommitCycle { get; set; }

		public List<DecodeBufferEntry> DecodeBuffer { get; private set; }
		public List<ReorderBufferEntry> ReorderBuffer { get; private set; }
		public List<ReorderBufferEntry> LoadStoreQueue { get; private set; }

		public bool IsSpeculative { get; set; }

		public ContextStat Stat { get; private set; }

		public static uint COMMIT_TIMEOUT = 1000000;
	}

	public sealed class MemorySystem : ICycleProvider
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
			this.EventProcessors.ForEach (eventProcessor => eventProcessor.AdvanceOneCycle ());
			
			this.CurrentCycle++;
		}

		private void CreateMemoryHierarchy ()
		{
			this.Mem = new MemoryController (this, this.Processor.Simulation.Config.Architecture.MainMemory, this.Processor.Simulation.Stat.MainMemory);
			
			this.L2 = new CoherentCache (this, this.Processor.Simulation.Config.Architecture.L2Cache, this.Processor.Simulation.Stat.L2Cache);
			this.L2.Next = this.Mem;
			
			this.Processor.Cores.ForEach (core => core.ICache.Next = core.DCache.Next = this.L2);
			
			this.MMU = new MemoryManagementUnit ();
		}

		public CoherentCache L2 { get; private set; }

		public MemoryController Mem { get; private set; }

		public MemoryManagementUnit MMU { get; private set; }

		public Processor Processor { get; private set; }

		public ulong CurrentCycle { get; private set; }

		public List<EventProcessor> EventProcessors { get; private set; }
	}

	public sealed class Processor : IProcessor
	{
		public Processor (Simulation simulation)
		{
			this.Simulation = simulation;
			
			this.Cores = new List<ICore> ();
			
			this.CurrentCycle = 0;
			
			this.ActiveThreadCount = 0;
			
			for (uint i = 0; i < this.Simulation.Config.Architecture.Processor.Cores.Count; i++) {
				Core core = new Core (this, i);
				
				for (uint j = 0; j < this.Simulation.Config.Architecture.Processor.NumThreadsPerCore; j++) {
					ContextConfig context = this.Simulation.Config.Contexts[(int)(i * this.Simulation.Config.Architecture.Processor.NumThreadsPerCore + j)];
					
					List<string> args = new List<string> ();
					args.Add (context.Workload.Cwd + Path.DirectorySeparatorChar + context.Workload.Exe + ".mipsel");
					args.AddRange (context.Workload.Args.Split (' '));
					
					Process process = new Process (context.Workload.Cwd, args);
					
					uint threadNum = i * this.Simulation.Config.Architecture.Processor.NumThreadsPerCore + j;
					ContextStat contextStat = this.Simulation.Stat.Processor.Contexts[(int)threadNum];
					
					Thread thread = new Thread (core, contextStat, j, process);
					
					core.Threads.Add (thread);
					
					this.ActiveThreadCount++;
				}
				
				this.Cores.Add (core);
			}
			
			this.MemorySystem = new MemorySystem (this);
		}

		public void Run ()
		{
			DateTime beginTime = DateTime.Now;
			
			while (this.ActiveThreadCount > 0 && this.Simulation.IsRunning) {
				this.Cores.ForEach (core => core.AdvanceOneCycle ());
				this.MemorySystem.AdvanceOneCycle ();
				
				this.CurrentCycle++;
			}
			
			this.Simulation.Stat.TotalCycles = this.CurrentCycle;
			
			this.Simulation.Stat.Duration = (ulong)((DateTime.Now - beginTime).TotalSeconds);
			this.Simulation.Stat.InstsPerCycle = (double)this.Simulation.Stat.TotalInsts / this.Simulation.Stat.TotalCycles;
			this.Simulation.Stat.CyclesPerSecond = (double)this.Simulation.Stat.TotalCycles / this.Simulation.Stat.Duration;
		}

		public MemoryManagementUnit MMU {
			get { return this.MemorySystem != null ? this.MemorySystem.MMU : null; }
		}

		public List<ICore> Cores { get; private set; }
		public MemorySystem MemorySystem { get; private set; }

		public Simulation Simulation { get; private set; }

		public ProcessorConfig Config {
			get { return this.Simulation.Config.Architecture.Processor; }
		}

		public ulong CurrentCycle { get; private set; }

		public int ActiveThreadCount { get; set; }

		static Processor ()
		{
			WorkDirectory = DEFAULT_WORK_DIRECTORY;
		}

		public static string WorkDirectory { get; set; }

		public static string DEFAULT_WORK_DIRECTORY = "../../../";
	}
}
