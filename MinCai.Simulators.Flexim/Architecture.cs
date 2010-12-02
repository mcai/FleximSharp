/*
 * Isa.cs
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
using System.Runtime.InteropServices;
using System.Text;
using MinCai.Simulators.Flexim.Architecture;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.Microarchitecture;

namespace MinCai.Simulators.Flexim.Architecture
{
	public sealed class BitField
	{
		public BitField (string name, uint hi, uint lo)
		{
			this.name = name;
			this.Hi = hi;
			this.Lo = lo;
		}

		public string name { get; set; }
		public uint Hi { get; set; }
		public uint Lo { get; set; }

		public static BitField OPCODE = new BitField ("OPCODE", 31, 26);
		public static BitField OPCODE_HI = new BitField ("OPCODE_HI", 31, 29);
		public static BitField OPCODE_LO = new BitField ("OPCODE_LO", 28, 26);

		public static BitField REGIMM = new BitField ("REGGIMM", 20, 16);
		public static BitField REGIMM_HI = new BitField ("REGIMM_HI", 20, 19);
		public static BitField REGIMM_LO = new BitField ("REGIMM_LO", 18, 16);

		public static BitField FUNC = new BitField ("FUNC", 5, 0);
		public static BitField FUNC_HI = new BitField ("FUNC_HI", 5, 3);
		public static BitField FUNC_LO = new BitField ("FUNC_LO", 2, 0);

		public static BitField RS = new BitField ("BitField.RS", 25, 21);
		public static BitField RS_MSB = new BitField ("RS_MSB", 25, 25);
		public static BitField RS_HI = new BitField ("RS_HI", 25, 24);
		public static BitField RS_LO = new BitField ("RS_LO", 23, 21);
		public static BitField RS_SRL = new BitField ("RS_SRL", 25, 22);
		public static BitField RS_RT = new BitField ("RS_RT", 25, 16);
		public static BitField RT = new BitField ("BitField.RT", 20, 16);
		public static BitField RT_HI = new BitField ("RT_HI", 20, 19);
		public static BitField RT_LO = new BitField ("RT_LO", 18, 16);
		public static BitField RT_RD = new BitField ("RT_RD", 20, 11);
		public static BitField RD = new BitField ("BitField.RD", 15, 11);

		public static BitField INTIMM = new BitField ("INTIMM", 15, 0);
		public static BitField RS_RT_INTIMM = new BitField ("RS_RT_INTIMM", 25, 0);

		//Floating-point operate format
		public static BitField FMT = new BitField ("FMT", 25, 21);
		public static BitField FR = new BitField ("FR", 25, 21);
		public static BitField FT = new BitField ("FT", 20, 16);
		public static BitField FS = new BitField ("FS", 15, 11);
		public static BitField FD = new BitField ("FD", 10, 6);

		public static BitField ND = new BitField ("ND", 17, 17);
		public static BitField TF = new BitField ("TF", 16, 16);
		public static BitField MOVCI = new BitField ("MOVCI", 16, 16);
		public static BitField MOVCF = new BitField ("MOVCF", 16, 16);
		public static BitField SRL = new BitField ("SRL", 21, 21);
		public static BitField SRLV = new BitField ("SRLV", 6, 6);
		public static BitField SA = new BitField ("SA", 10, 6);

		// Floating Point Condition Codes
		public static BitField COND = new BitField ("COND", 3, 0);
		public static BitField CC = new BitField ("CC", 10, 8);
		public static BitField BRANCH_CC = new BitField ("BRANCH_CC", 20, 18);

		// CP0 Register Select
		public static BitField SEL = new BitField ("SEL", 2, 0);

		// INTERRUPTS
		public static BitField SC = new BitField ("SC", 5, 5);

		// Branch format
		public static BitField OFFSET = new BitField ("BitField.OFFSET", 15, 0);

		// Jmp format
		public static BitField JMPTARG = new BitField ("JMPTARG", 25, 0);
		public static BitField HINT = new BitField ("HINT", 10, 6);

		public static BitField SYSCALLCODE = new BitField ("SYSCALLCODE", 25, 6);
		public static BitField TRAPCODE = new BitField ("TRAPCODE", 15, 13);

		// EXT/INS instructions
		public static BitField MSB = new BitField ("MSB", 15, 11);
		public static BitField LSB = new BitField ("LSB", 10, 6);

		// DSP instructions
		public static BitField OP = new BitField ("OP", 10, 6);
		public static BitField OP_HI = new BitField ("OP_HI", 10, 9);
		public static BitField OP_LO = new BitField ("OP_LO", 8, 6);
		public static BitField DSPSA = new BitField ("DSPSA", 23, 21);
		public static BitField HILOSA = new BitField ("HILOSA", 25, 20);
		public static BitField RDDSPMASK = new BitField ("RDDSPMASK", 21, 16);
		public static BitField WRDSPMASK = new BitField ("WRDSPMASK", 16, 11);
		public static BitField ACSRC = new BitField ("ACSRC", 22, 21);
		public static BitField ACDST = new BitField ("ACDST", 12, 11);
		public static BitField BP = new BitField ("BP", 12, 11);

		// MT Instructions
		public static BitField POS = new BitField ("POS", 10, 6);
		public static BitField MT_U = new BitField ("MT_U", 5, 5);
		public static BitField MT_H = new BitField ("MT_H", 4, 4);

		//Cache Ops
		public static BitField CACHE_OP = new BitField ("CACHE_OP", 20, 16);
	}

	public enum MachInstType
	{
		R,
		I,
		J,
		F
	}

	public sealed class MachInst
	{
		public MachInst (uint data)
		{
			this.Data = data;
		}

		public override string ToString ()
		{
			return string.Format ("[MachInst: Data=0x{0:x8}]", this.Data);
		}

		public uint this[BitField field] {
			get { return BitHelper.Bits (this.Data, (int)(field.Hi), (int)(field.Lo)); }
		}

		public bool IsRMt {
			get {
				uint func = this[BitField.FUNC];
				return (func == 0x10 || func == 0x11);
			}
		}

		public bool IsRMf {
			get {
				uint func = this[BitField.FUNC];
				return (func == 0x12 || func == 0x13);
			}
		}

		public bool IsROneOp {
			get {
				uint func = this[BitField.FUNC];
				return (func == 0x08 || func == 0x09);
			}
		}

		public bool IsRTwoOp {
			get {
				uint func = this[BitField.FUNC];
				return (func >= 0x18 && func <= 0x1b);
			}
		}

		public bool IsLoadStore {
			get {
				uint opcode = this[BitField.OPCODE];
				return (((opcode >= 0x20) && (opcode <= 0x2e)) || (opcode == 0x30) || (opcode == 0x38));
			}
		}

		public bool IsFloatLoadStore {
			get {
				uint opcode = this[BitField.OPCODE];
				return (opcode == 0x31 || opcode == 0x39);
			}
		}

		public bool IsOneOpBranch {
			get {
				uint opcode = this[BitField.OPCODE];
				return ((opcode == 0x00) || (opcode == 0x01) || (opcode == 0x06) || (opcode == 0x07));
			}
		}

		public bool IsShift {
			get {
				uint func = this[BitField.FUNC];
				return (func == 0x00 || func == 0x01 || func == 0x03);
			}
		}

		public bool IsConvert {
			get {
				uint func = this[BitField.FUNC];
				return (func == 32 || func == 33 || func == 36);
			}
		}

		public bool IsCompare {
			get {
				uint func = this[BitField.FUNC];
				return (func >= 48);
			}
		}

		public bool IsGPRFloatMove {
			get {
				uint rs = this[BitField.RS];
				return (rs == 0 || rs == 4);
			}
		}

		public bool IsGPRFCRMove {
			get {
				uint rs = this[BitField.RS];
				return (rs == 2 || rs == 6);
			}
		}

		public bool IsFloatBranch {
			get {
				uint rs = this[BitField.RS];
				return (rs == 8);
			}
		}

		public bool IsSyscall {
			get { return (this[BitField.OPCODE_LO] == 0x0 && this[BitField.FUNC_HI] == 0x1 && this[BitField.FUNC_LO] == 0x4); }
		}

		public MachInstType MachInstType {
			get {
				uint opcode = this[BitField.OPCODE];
				
				if (opcode == 0)
					return MachInstType.R; else if ((opcode == 0x02) || (opcode == 0x03))
					return MachInstType.J; else if (opcode == 0x11)
					return MachInstType.F;
				else
					return MachInstType.I;
			}
		}

		public uint Data { get; private set; }
	}

	public enum StaticInstFlag : uint
	{
		None = 0x00000000,
		IntegerComputation = 0x00000001,
		FloatComputation = 0x00000002,
		Control = 0x00000004,
		Unconditional = 0x00000008,
		Conditional = 0x00000010,
		Memory = 0x00000020,
		Load = 0x00000040,
		Store = 0x00000080,
		DisplacedAddressing = 0x00000100,
		RRAddressing = 0x00000200,
		DirectAddressing = 0x00000400,
		Trap = 0x00000800,
		LongLatency = 0x00001000,
		DirectJump = 0x00002000,
		IndirectJump = 0x00004000,
		Call = 0x00008000,
		FloatConditional = 0x00010000,
		Immediate = 0x00020000,
		FunctionReturn = 0x00040000
	}

	public static class RegisterConstants
	{
		public static uint NUM_INT_REGS = 32;
		public static uint NUM_FLOAT_REGS = 32;
		public static uint NUM_MISC_REGS = 4;

		public static uint ZERO_REG = 0;
		public static uint ASSEMBLER_REG = 1;
		public static uint SYSCALL_SUCCESS_REG = 7;
		public static uint FIRST_ARGUMENT_REG = 4;
		public static uint RETURN_VALUE_REG = 2;

		public static uint KERNEL_REG0 = 26;
		public static uint KERNEL_REG1 = 27;
		public static uint GLOBAL_POINTER_REG = 28;
		public static uint STACK_POINTER_REG = 29;
		public static uint FRAME_POINTER_REG = 30;
		public static uint RETURN_ADDRESS_REG = 31;

		public static uint SYSCALL_PSEUDO_RETURN_REG = 3;
	}

	public enum MiscRegNums : int
	{
		Lo = 0,
		Hi = 1,
		Ea = 2,
		Fcsr = 3
	}

	public abstract class RegisterFile
	{
		public abstract void Clear ();
	}

	public sealed class IntRegisterFile : RegisterFile
	{
		public IntRegisterFile ()
		{
			this.Regs = new uint[RegisterConstants.NUM_INT_REGS];
			this.Clear ();
		}

		public void CopyTo (IntRegisterFile otherFile)
		{
			for (int i = 0; i < this.Regs.Length; i++) {
				otherFile.Regs[i] = this.Regs[i];
			}
		}

		public override void Clear ()
		{
			for (int i = 0; i < this.Regs.Length; i++) {
				this.Regs[i] = 0;
			}
		}

		public override string ToString ()
		{
			return string.Format ("[IntRegisterFile: Regs.Length={0}]", this.Regs.Length);
		}

		public uint this[uint index] {
			get {
				Debug.Assert (index < RegisterConstants.NUM_INT_REGS);
				
				uint val = this.Regs[index];
//				Logger.Infof(LogCategory.THREAD, "    Reading int reg {0:d} as 0x{1:x8}.", index, val);
				return val;
			}
			set {
				Debug.Assert (index < RegisterConstants.NUM_INT_REGS);
//				Logger.Infof(LogCategory.THREAD, "    Setting int reg {0:d} to 0x{1:x8}.", index, value);
				this.Regs[index] = value;
			}
		}

		public uint[] Regs { get; private set; }
	}

	public sealed class FloatRegisterFile : RegisterFile
	{
		public FloatRegisterFile ()
		{
			this.Clear ();
		}

		public void CopyTo (FloatRegisterFile otherFile)
		{
			otherFile.Regs = this.Regs;
		}

		public override void Clear ()
		{
			this.Regs.f = new float[RegisterConstants.NUM_FLOAT_REGS];
			this.Regs.i = new int[RegisterConstants.NUM_FLOAT_REGS];
			this.Regs.d = new double[RegisterConstants.NUM_FLOAT_REGS / 2];
			this.Regs.l = new long[RegisterConstants.NUM_FLOAT_REGS / 2];
		}

		public float GetFloat (uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			float val = this.Regs.f[index];
//			Logger.Infof(LogCategory.REGISTER, "    Reading float reg {0:d} as {1:f}.", index, val);
			return val;
		}

		public void SetFloat (float val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			this.Regs.f[index] = val;
//			Logger.Infof(LogCategory.REGISTER, "    Setting float reg {0:d} to {1:f}.", index, val);
		}

		public double GetDouble (uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			double val = this.Regs.d[index / 2];
//			Logger.Infof(LogCategory.REGISTER, "    Reading double reg {0:d} as {1:f}.", index, val);
			return val;
		}

		public void SetDouble (double val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			this.Regs.d[index / 2] = val;
//			Logger.Infof(LogCategory.REGISTER, "    Setting double reg {0:d} to {1:f}.", index, val);
		}

		public uint GetUint (uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			uint val = (uint)(this.Regs.i[index]);
//			Logger.Infof(LogCategory.REGISTER, "    Reading float reg {0:d} bits as 0x{1:x8}.", index, val);
			return val;
		}

		public void SetUint (uint val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			this.Regs.i[index] = (int)val;
//			Logger.Infof(LogCategory.REGISTER, "    Setting float reg (0:d} bits to 0x{1:x8}.", index, val);
		}

		public ulong GetUlong (uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			ulong val = (ulong)(this.Regs.l[index / 2]);
//			Logger.Infof(LogCategory.REGISTER, "    Reading double reg {0:d} bits as 0x{1:x8}.", index, val);
			return val;
		}

		public void SetUlong (ulong val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			this.Regs.l[index / 2] = (long)val;
//			Logger.Infof(LogCategory.REGISTER, "    Setting double reg {0:d} bits to 0x{1:x8}.", index, val);
		}

		public override string ToString ()
		{
			return string.Format ("[FloatRegisterFile]");
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct Cop1Reg
		{
			[FieldOffset(0)]
			public float[] f;

			[FieldOffset(0)]
			public int[] i;

			[FieldOffset(0)]
			public double[] d;

			[FieldOffset(0)]
			public long[] l;
		}

		public Cop1Reg Regs;
	}

	public sealed class MiscRegisterFile : RegisterFile
	{
		public MiscRegisterFile ()
		{
			this.Clear ();
		}

		public void CopyTo (MiscRegisterFile otherFile)
		{
			otherFile.Lo = this.Lo;
			otherFile.Hi = this.Hi;
			otherFile.Ea = this.Ea;
			otherFile.Fcsr = this.Fcsr;
		}

		public override void Clear ()
		{
			this.Lo = 0;
			this.Hi = 0;
			this.Ea = 0;
			this.Fcsr = 0;
		}

		public uint Lo { get; set; }
		public uint Hi { get; set; }
		public uint Ea { get; set; }
		public uint Fcsr { get; set; }
	}

	public sealed class CombinedRegisterFile : RegisterFile
	{
		public CombinedRegisterFile ()
		{
			this.isSpeculative = false;
			
			this.IntRegs = new IntRegisterFile ();
			this.FloatRegs = new FloatRegisterFile ();
			this.MiscRegs = new MiscRegisterFile ();
			
			this.RecIntRegs = new IntRegisterFile ();
			this.RecFloatRegs = new FloatRegisterFile ();
			this.RecMiscRegs = new MiscRegisterFile ();
			
			this.Clear ();
		}

		public override void Clear ()
		{
			this.IntRegs.Clear ();
			this.FloatRegs.Clear ();
			this.MiscRegs.Clear ();
			
			this.RecIntRegs.Clear ();
			this.RecFloatRegs.Clear ();
			this.RecMiscRegs.Clear ();
			
			this.Pc = this.Npc = this.Nnpc = 0;
			this.RecPc = this.RecNpc = this.RecNnpc = 0;
		}

		public IntRegisterFile IntRegs { get; set; }
		public FloatRegisterFile FloatRegs { get; set; }
		public MiscRegisterFile MiscRegs { get; set; }

		public IntRegisterFile RecIntRegs { get; set; }
		public FloatRegisterFile RecFloatRegs { get; set; }
		public MiscRegisterFile RecMiscRegs { get; set; }

		public uint Pc { get; set; }
		public uint Npc { get; set; }
		public uint Nnpc { get; set; }

		public uint RecPc { get; set; }
		public uint RecNpc { get; set; }
		public uint RecNnpc { get; set; }

		public bool IsSpeculative {
			get { return this.isSpeculative; }
			set {
				if (this.isSpeculative != value) {
					if (value) {
						this.IntRegs.CopyTo (this.RecIntRegs);
						this.FloatRegs.CopyTo (this.RecFloatRegs);
						this.MiscRegs.CopyTo (this.RecMiscRegs);
						
						this.RecPc = this.Pc;
						this.RecNpc = this.Npc;
						this.RecNnpc = this.Nnpc;
					} else {
						this.RecIntRegs.CopyTo (this.IntRegs);
						this.RecFloatRegs.CopyTo (this.FloatRegs);
						this.RecMiscRegs.CopyTo (this.MiscRegs);
						
						this.Pc = this.RecPc;
						this.Npc = this.RecNpc;
						this.Nnpc = this.RecNnpc;
					}
					
					this.isSpeculative = value;
				}
			}
		}

		private bool isSpeculative;
	}

	public abstract class InstructionSetArchitecture
	{
		public InstructionSetArchitecture ()
		{
			this.DecodedInsts = new Dictionary<uint, StaticInst> ();
		}

		unsafe public StaticInst Decode (uint pc, Memory mem)
		{
			if (this.DecodedInsts.ContainsKey (pc)) {
				return this.DecodedInsts[pc];
			} else {
				uint data = 0;
				mem.ReadWord (pc, &data);
				
				MachInst machInst = new MachInst (data);
				
				StaticInst staticInst = this.DecodeMachInst (machInst);
				
				this.DecodedInsts[pc] = staticInst;
				
				return staticInst;
			}
		}

		public abstract StaticInst DecodeMachInst (MachInst machInst);

		private Dictionary<uint, StaticInst> DecodedInsts { get; set; }
	}

	public enum RegisterDependencyType
	{
		Integer,
		Float,
		Misc
	}

	public sealed class RegisterDependency
	{
		public RegisterDependency (RegisterDependencyType type, uint num)
		{
			this.Type = type;
			this.Num = num;
		}

		public override string ToString ()
		{
			return string.Format ("[RegisterDependency: Type={0}, Num={1}]", this.Type, this.Num);
		}

		public RegisterDependencyType Type { get; private set; }
		public uint Num { get; private set; }
	}

	public abstract class StaticInst
	{
		public StaticInst (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType)
		{
			this.Mnemonic = mnemonic;
			this.MachInst = machInst;
			this.Flags = flags;
			this.FuType = fuType;
			
			this.IDeps = new List<RegisterDependency> ();
			this.ODeps = new List<RegisterDependency> ();
			
			this.SetupDeps ();
		}

		public virtual uint GetTargetPc (Thread thread)
		{
			throw new NotImplementedException();
		}

		protected abstract void SetupDeps ();

		public abstract void Execute (Thread thread);

		public override string ToString ()
		{
			return string.Format ("[StaticInst: MachInst={0}, Mnemonic={1}, Flags={2}, FuType={3}]", this.MachInst, this.Mnemonic, this.Flags, this.FuType);
		}

		public uint this[BitField field] {
			get { return this.MachInst[field]; }
		}

		public bool IsLongLatency {
			get { return (this.Flags & StaticInstFlag.LongLatency) == StaticInstFlag.LongLatency; }
		}

		public bool IsTrap {
			get { return (this.Flags & StaticInstFlag.Trap) == StaticInstFlag.Trap; }
		}

		public bool IsMemory {
			get { return (this.Flags & StaticInstFlag.Memory) == StaticInstFlag.Memory; }
		}

		public bool IsLoad {
			get { return this.IsMemory && (this.Flags & StaticInstFlag.Load) == StaticInstFlag.Load; }
		}

		public bool IsStore {
			get { return this.IsMemory && (this.Flags & StaticInstFlag.Store) == StaticInstFlag.Store; }
		}

		public bool IsConditional {
			get { return (this.Flags & StaticInstFlag.Conditional) == StaticInstFlag.Conditional; }
		}

		public bool IsUnconditional {
			get { return (this.Flags & StaticInstFlag.Unconditional) == StaticInstFlag.Unconditional; }
		}

		public bool IsDirectJump {
			get { return (this.Flags & StaticInstFlag.DirectJump) != StaticInstFlag.DirectJump; }
		}

		public bool IsControl {
			get { return (this.Flags & StaticInstFlag.Control) == StaticInstFlag.Control; }
		}

		public bool IsCall {
			get { return (this.Flags & StaticInstFlag.Call) == StaticInstFlag.Call; }
		}

		public bool IsFunctionReturn {
			get { return (this.Flags & StaticInstFlag.FunctionReturn) == StaticInstFlag.FunctionReturn; }
		}

		public bool IsNop {
			get { return (this as Nop) != null; }
		}

		public List<RegisterDependency> IDeps { get; protected set; }
		public List<RegisterDependency> ODeps { get; protected set; }
		
		public string Mnemonic { get; private set; }
		public MachInst MachInst { get; private set; }
		public StaticInstFlag Flags { get; private set; }
		public FunctionalUnitType FuType { get; private set; }
	}

	public sealed class DynamicInst
	{
		public DynamicInst (Thread thread, uint pc, StaticInst staticInst)
		{
			this.Thread = thread;
			this.Pc = pc;
			this.StaticInst = staticInst;
		}

		public void Execute ()
		{
			this.Thread.Regs.IntRegs[RegisterConstants.ZERO_REG] = 0;
			this.StaticInst.Execute (this.Thread);
		}

		public override string ToString ()
		{
			return string.Format ("[DynamicInst: Dis={0}, Thread.Name={1}]", Disassemble(this.StaticInst.MachInst, this.Pc, this.Thread), this.Thread.Name);
		}

		public uint PhysPc {
			get { return this.Thread.Core.MMU.GetPhysicalAddress (this.Pc); }
		}

		public uint Pc { get; set; }
		public StaticInst StaticInst { get; set; }
		public Thread Thread { get; set; }

		public static string[] MIPS_GPR_NAMES = { "zero", "at", "v0", "v1", "a0", "a1", "a2", "a3", "t0", "t1",
		"t2", "t3", "t4", "t5", "t6", "t7", "s0", "s1", "s2", "s3",
		"s4", "s5", "s6", "s7", "t8", "t9", "k0", "k1", "gp", "sp",
		"s8", "ra" };

		public static string Disassemble (MachInst machInst, uint pc, Thread thread)
		{
			StringBuilder buf = new StringBuilder ();
			
			buf.AppendFormat ("0x{0:x8} : 0x{1:x8} {2} ", pc, machInst.Data, thread.Core.Isa.DecodeMachInst (machInst).Mnemonic);
			
			if (machInst.Data == 0x00000000) {
				return buf.ToString ();
			}
			
			switch (machInst.MachInstType) {
			case MachInstType.J:
				buf.AppendFormat ("0x{0:x8}", machInst[BitField.JMPTARG]);
				break;
			case MachInstType.I:
				if (machInst.IsOneOpBranch) {
					buf.AppendFormat ("${0}, {1:d}", MIPS_GPR_NAMES[machInst[BitField.RS]], (short)machInst[BitField.INTIMM]);
				} else if (machInst.IsLoadStore) {
					buf.AppendFormat ("${0}, {1:d}(${2})", MIPS_GPR_NAMES[machInst[BitField.RT]], (short)machInst[BitField.INTIMM], MIPS_GPR_NAMES[machInst[BitField.RS]]);
				} else if (machInst.IsFloatLoadStore) {
					buf.AppendFormat ("$f{0}, {1:d}(${2})", machInst[BitField.FT], (short)machInst[BitField.INTIMM], MIPS_GPR_NAMES[machInst[BitField.RS]]);
				} else {
					buf.AppendFormat ("${0}, ${1}, {2:d}", MIPS_GPR_NAMES[machInst[BitField.RT]], MIPS_GPR_NAMES[machInst[BitField.RS]], (short)machInst[BitField.INTIMM]);
				}
				break;
			case MachInstType.F:
				if (machInst.IsConvert) {
					buf.AppendFormat ("$f{0:d}, $f{1:d}", machInst[BitField.FD], machInst[BitField.FS]);
				} else if (machInst.IsCompare) {
					buf.AppendFormat ("{0:d}, $f{1:d}, $f{2:d}", machInst[BitField.FD] >> 2, machInst[BitField.FS], machInst[BitField.FT]);
				} else if (machInst.IsFloatBranch) {
					buf.AppendFormat ("{0:d}, {1:d}", machInst[BitField.FD] >> 2, (short)machInst[BitField.INTIMM]);
				} else if (machInst.IsGPRFloatMove) {
					buf.AppendFormat ("${0}, $f{1:d}", MIPS_GPR_NAMES[machInst[BitField.RT]], machInst[BitField.FS]);
				} else if (machInst.IsGPRFCRMove) {
					buf.AppendFormat ("${0}, ${1:d}", MIPS_GPR_NAMES[machInst[BitField.RT]], machInst[BitField.FS]);
				} else {
					buf.AppendFormat ("$f{0:d}, $f{1:d}, $f{2:d}", machInst[BitField.FD], machInst[BitField.FS], machInst[BitField.FT]);
				}
				break;
			case MachInstType.R:
				if (machInst.IsSyscall) {
				} else if (machInst.IsShift) {
					buf.AppendFormat ("${0}, ${1}, {2:d}", MIPS_GPR_NAMES[machInst[BitField.RD]], MIPS_GPR_NAMES[machInst[BitField.RT]], machInst[BitField.SA]);
				} else if (machInst.IsROneOp) {
					buf.AppendFormat ("${0}", MIPS_GPR_NAMES[machInst[BitField.RS]]);
				} else if (machInst.IsRTwoOp) {
					buf.AppendFormat ("${0}, ${1}", MIPS_GPR_NAMES[machInst[BitField.RS]], MIPS_GPR_NAMES[machInst[BitField.RT]]);
				} else if (machInst.IsRMt) {
					buf.AppendFormat ("${0}", MIPS_GPR_NAMES[machInst[BitField.RS]]);
				} else if (machInst.IsRMf) {
					buf.AppendFormat ("${0}", MIPS_GPR_NAMES[machInst[BitField.RD]]);
				} else {
					buf.AppendFormat ("${0}, ${1}, ${2}", MIPS_GPR_NAMES[machInst[BitField.RD]], MIPS_GPR_NAMES[machInst[BitField.RS]], MIPS_GPR_NAMES[machInst[BitField.RT]]);
				}
				break;
			default:
				Logger.Fatal (LogCategory.Instruction, "you can not reach here");
				break;
			}
			
			return buf.ToString ();
		}
	}

	public sealed class Mips32InstructionSetArchitecture : InstructionSetArchitecture
	{
		public Mips32InstructionSetArchitecture ()
		{
		}

		public override StaticInst DecodeMachInst (MachInst machInst)
		{
			switch (machInst[BitField.OPCODE_HI]) {
			case 0x0:
				switch (machInst[BitField.OPCODE_LO]) {
				case 0x0:
					switch (machInst[BitField.FUNC_HI]) {
					case 0x0:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x1:
							switch (machInst[BitField.MOVCI]) {
							case 0x0:
								return new FailUnimplemented ("Movf", machInst);
							case 0x1:
								return new FailUnimplemented ("Movt", machInst);
							default:
								return new Unknown (machInst);
							}
						case 0x0:
							switch (machInst[BitField.RS]) {
							case 0x0:
								switch (machInst[BitField.RT_RD]) {
								case 0x0:
									switch (machInst[BitField.SA]) {
									case 0x1:
										return new FailUnimplemented ("Ssnop", machInst);
									case 0x3:
										return new FailUnimplemented ("Ehb", machInst);
									default:
										return new Nop (machInst);
									}
								default:
									return new Sll (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0x2:
							switch (machInst[BitField.RS_SRL]) {
							case 0x0:
								switch (machInst[BitField.SRL]) {
								case 0x0:
									return new Srl (machInst);
								case 0x1:
									return new FailUnimplemented ("Rotr", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0x3:
							switch (machInst[BitField.RS]) {
							case 0x0:
								return new Sra (machInst);
							default:
								return new Unknown (machInst);
							}
						case 0x4:
							return new Sllv (machInst);
						case 0x6:
							switch (machInst[BitField.SRLV]) {
							case 0x0:
								return new Srlv (machInst);
							case 0x1:
								return new FailUnimplemented ("Rotrv", machInst);
							default:
								return new Unknown (machInst);
							}
						case 0x7:
							return new Srav (machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x1:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							switch (machInst[BitField.HINT]) {
							case 0x1:
								return new FailUnimplemented ("Jr_hb", machInst);
							default:
								return new Jr (machInst);
							}
						case 0x1:
							switch (machInst[BitField.HINT]) {
							case 0x1:
								return new FailUnimplemented ("Jalr_hb", machInst);
							default:
								return new Jalr (machInst);
							}
						case 0x2:
							return new FailUnimplemented ("Movz", machInst);
						case 0x3:
							return new FailUnimplemented ("Movn", machInst);
						case 0x4:
							return new SyscallInst (machInst);
						case 0x7:
							return new FailUnimplemented ("Sync", machInst);
						case 0x5:
							return new FailUnimplemented ("Break", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x2:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new Mfhi (machInst);
						case 0x1:
							return new Mthi (machInst);
						case 0x2:
							return new Mflo (machInst);
						case 0x3:
							return new Mtlo (machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x3:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new Mult (machInst);
						case 0x1:
							return new Multu (machInst);
						case 0x2:
							return new Div (machInst);
						case 0x3:
							return new Divu (machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x4:
						switch (machInst[BitField.HINT]) {
						case 0x0:
							switch (machInst[BitField.FUNC_LO]) {
							case 0x0:
								return new Add (machInst);
							case 0x1:
								return new Addu (machInst);
							case 0x2:
								return new Sub (machInst);
							case 0x3:
								return new Subu (machInst);
							case 0x4:
								return new And (machInst);
							case 0x5:
								return new Or (machInst);
							case 0x6:
								return new Xor (machInst);
							case 0x7:
								return new Nor (machInst);
							default:
								return new Unknown (machInst);
							}
						default:
							return new Unknown (machInst);
						}
					case 0x5:
						switch (machInst[BitField.HINT]) {
						case 0x0:
							switch (machInst[BitField.FUNC_LO]) {
							case 0x2:
								return new Slt (machInst);
							case 0x3:
								return new Sltu (machInst);
							default:
								return new Unknown (machInst);
							}
						default:
							return new Unknown (machInst);
						}
					case 0x6:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Tge", machInst);
						case 0x1:
							return new FailUnimplemented ("Tgeu", machInst);
						case 0x2:
							return new FailUnimplemented ("Tlt", machInst);
						case 0x3:
							return new FailUnimplemented ("Tltu", machInst);
						case 0x4:
							return new FailUnimplemented ("Teq", machInst);
						case 0x6:
							return new FailUnimplemented ("Tne", machInst);
						default:
							return new Unknown (machInst);
						}
					default:
						return new Unknown (machInst);
					}
				case 0x1:
					switch (machInst[BitField.REGIMM_HI]) {
					case 0x0:
						switch (machInst[BitField.REGIMM_LO]) {
						case 0x0:
							return new Bltz (machInst);
						case 0x1:
							return new Bgez (machInst);
						case 0x2:
							return new FailUnimplemented ("Bltzl", machInst);
						case 0x3:
							return new FailUnimplemented ("Bgezl", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x1:
						switch (machInst[BitField.REGIMM_LO]) {
						case 0x0:
							return new FailUnimplemented ("Tgei", machInst);
						case 0x1:
							return new FailUnimplemented ("Tgeiu", machInst);
						case 0x2:
							return new FailUnimplemented ("Tlti", machInst);
						case 0x3:
							return new FailUnimplemented ("Tltiu", machInst);
						case 0x4:
							return new FailUnimplemented ("Teqi", machInst);
						case 0x6:
							return new FailUnimplemented ("Tnei", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x2:
						switch (machInst[BitField.REGIMM_LO]) {
						case 0x0:
							return new Bltzal (machInst);
						case 0x1:
							switch (machInst[BitField.RS]) {
							case 0x0:
								return new Bal (machInst);
							default:
								return new Bgezal (machInst);
							}
						case 0x2:
							return new FailUnimplemented ("Bltzall", machInst);
						case 0x3:
							return new FailUnimplemented ("Bgezall", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x3:
						switch (machInst[BitField.REGIMM_LO]) {
						case 0x4:
							return new FailUnimplemented ("Bposge32", machInst);
						case 0x7:
							return new FailUnimplemented ("WarnUnimplemented.synci", machInst);
						default:
							return new Unknown (machInst);
						}
					default:
						return new Unknown (machInst);
					}
				case 0x2:
					return new J (machInst);
				case 0x3:
					return new Jal (machInst);
				case 0x4:
					switch (machInst[BitField.RS_RT]) {
					case 0x0:
						return new B (machInst);
					default:
						return new Beq (machInst);
					}
				case 0x5:
					return new Bne (machInst);
				case 0x6:
					return new Blez (machInst);
				case 0x7:
					return new Bgtz (machInst);
				default:
					return new Unknown (machInst);
				}
			case 0x1:
				switch (machInst[BitField.OPCODE_LO]) {
				case 0x0:
					return new Addi (machInst);
				case 0x1:
					return new Addiu (machInst);
				case 0x2:
					return new Slti (machInst);
				case 0x3:
					switch (machInst[BitField.RS_RT_INTIMM]) {
					case 0xabc1:
						return new FailUnimplemented ("Fail", machInst);
					case 0xabc2:
						return new FailUnimplemented ("Pass", machInst);
					default:
						return new Sltiu (machInst);
					}
				case 0x4:
					return new Andi (machInst);
				case 0x5:
					return new Ori (machInst);
				case 0x6:
					return new Xori (machInst);
				case 0x7:
					switch (machInst[BitField.RS]) {
					case 0x0:
						return new Lui (machInst);
					default:
						return new Unknown (machInst);
					}
				default:
					return new Unknown (machInst);
				}
			case 0x2:
				switch (machInst[BitField.OPCODE_LO]) {
				case 0x0:
					switch (machInst[BitField.RS_MSB]) {
					case 0x0:
						switch (machInst[BitField.RS]) {
						case 0x0:
							return new FailUnimplemented ("Mfc0", machInst);
						case 0x4:
							return new FailUnimplemented ("Mtc0", machInst);
						case 0x1:
							return new CP0Unimplemented ("dmfc0", machInst);
						case 0x5:
							return new CP0Unimplemented ("dmtc0", machInst);
						default:
							return new CP0Unimplemented ("unknown", machInst);
						case 0x8:
							switch (machInst[BitField.MT_U]) {
							case 0x0:
								return new FailUnimplemented ("Mftc0", machInst);
							case 0x1:
								switch (machInst[BitField.SEL]) {
								case 0x0:
									return new FailUnimplemented ("Mftgpr", machInst);
								case 0x1:
									switch (machInst[BitField.RT]) {
									case 0x0:
										return new FailUnimplemented ("Mftlo_dsp0", machInst);
									case 0x1:
										return new FailUnimplemented ("Mfthi_dsp0", machInst);
									case 0x2:
										return new FailUnimplemented ("Mftacx_dsp0", machInst);
									case 0x4:
										return new FailUnimplemented ("Mftlo_dsp1", machInst);
									case 0x5:
										return new FailUnimplemented ("Mfthi_dsp1", machInst);
									case 0x6:
										return new FailUnimplemented ("Mftacx_dsp1", machInst);
									case 0x8:
										return new FailUnimplemented ("Mftlo_dsp2", machInst);
									case 0x9:
										return new FailUnimplemented ("Mfthi_dsp2", machInst);
									case 0x10:
										return new FailUnimplemented ("Mftacx_dsp2", machInst);
									case 0x12:
										return new FailUnimplemented ("Mftlo_dsp3", machInst);
									case 0x13:
										return new FailUnimplemented ("Mfthi_dsp3", machInst);
									case 0x14:
										return new FailUnimplemented ("Mftacx_dsp3", machInst);
									case 0x16:
										return new FailUnimplemented ("Mftdsp", machInst);
									default:
										return new CP0Unimplemented ("unknown", machInst);
									}
								case 0x2:
									switch (machInst[BitField.MT_H]) {
									case 0x0:
										return new FailUnimplemented ("Mftc1", machInst);
									case 0x1:
										return new FailUnimplemented ("Mfthc1", machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x3:
									return new FailUnimplemented ("Cftc1", machInst);
								default:
									return new CP0Unimplemented ("unknown", machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0xc:
							switch (machInst[BitField.MT_U]) {
							case 0x0:
								return new FailUnimplemented ("Mttc0", machInst);
							case 0x1:
								switch (machInst[BitField.SEL]) {
								case 0x0:
									return new FailUnimplemented ("Mttgpr", machInst);
								case 0x1:
									switch (machInst[BitField.RT]) {
									case 0x0:
										return new FailUnimplemented ("Mttlo_dsp0", machInst);
									case 0x1:
										return new FailUnimplemented ("Mtthi_dsp0", machInst);
									case 0x2:
										return new FailUnimplemented ("Mttacx_dsp0", machInst);
									case 0x4:
										return new FailUnimplemented ("Mttlo_dsp1", machInst);
									case 0x5:
										return new FailUnimplemented ("Mtthi_dsp1", machInst);
									case 0x6:
										return new FailUnimplemented ("Mttacx_dsp1", machInst);
									case 0x8:
										return new FailUnimplemented ("Mttlo_dsp2", machInst);
									case 0x9:
										return new FailUnimplemented ("Mtthi_dsp2", machInst);
									case 0x10:
										return new FailUnimplemented ("Mttacx_dsp2", machInst);
									case 0x12:
										return new FailUnimplemented ("Mttlo_dsp3", machInst);
									case 0x13:
										return new FailUnimplemented ("Mtthi_dsp3", machInst);
									case 0x14:
										return new FailUnimplemented ("Mttacx_dsp3", machInst);
									case 0x16:
										return new FailUnimplemented ("Mttdsp", machInst);
									default:
										return new CP0Unimplemented ("unknown", machInst);
									}
								case 0x2:
									return new FailUnimplemented ("Mttc1", machInst);
								case 0x3:
									return new FailUnimplemented ("Cttc1", machInst);
								default:
									return new CP0Unimplemented ("unknown", machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0xb:
							switch (machInst[BitField.RD]) {
							case 0x0:
								switch (machInst[BitField.POS]) {
								case 0x0:
									switch (machInst[BitField.SEL]) {
									case 0x1:
										switch (machInst[BitField.SC]) {
										case 0x0:
											return new FailUnimplemented ("Dvpe", machInst);
										case 0x1:
											return new FailUnimplemented ("Evpe", machInst);
										default:
											return new CP0Unimplemented ("unknown", machInst);
										}
									default:
										return new CP0Unimplemented ("unknown", machInst);
									}
								default:
									return new CP0Unimplemented ("unknown", machInst);
								}
							case 0x1:
								switch (machInst[BitField.POS]) {
								case 0xf:
									switch (machInst[BitField.SEL]) {
									case 0x1:
										switch (machInst[BitField.SC]) {
										case 0x0:
											return new FailUnimplemented ("Dmt", machInst);
										case 0x1:
											return new FailUnimplemented ("Emt", machInst);
										default:
											return new CP0Unimplemented ("unknown", machInst);
										}
									default:
										return new CP0Unimplemented ("unknown", machInst);
									}
								default:
									return new CP0Unimplemented ("unknown", machInst);
								}
							case 0xc:
								switch (machInst[BitField.POS]) {
								case 0x0:
									switch (machInst[BitField.SC]) {
									case 0x0:
										return new FailUnimplemented ("Di", machInst);
									case 0x1:
										return new FailUnimplemented ("Ei", machInst);
									default:
										return new CP0Unimplemented ("unknown", machInst);
									}
								default:
									return new Unknown (machInst);
								}
							default:
								return new CP0Unimplemented ("unknown", machInst);
							}
						case 0xa:
							return new FailUnimplemented ("Rdpgpr", machInst);
						case 0xe:
							return new FailUnimplemented ("Wrpgpr", machInst);
						}
					case 0x1:
						switch (machInst[BitField.FUNC]) {
						case 0x18:
							return new FailUnimplemented ("Eret", machInst);
						case 0x1f:
							return new FailUnimplemented ("Deret", machInst);
						case 0x1:
							return new FailUnimplemented ("Tlbr", machInst);
						case 0x2:
							return new FailUnimplemented ("Tlbwi", machInst);
						case 0x6:
							return new FailUnimplemented ("Tlbwr", machInst);
						case 0x8:
							return new FailUnimplemented ("Tlbp", machInst);
						case 0x20:
							return new CP0Unimplemented ("wait", machInst);
						default:
							return new CP0Unimplemented ("unknown", machInst);
						}
					default:
						return new Unknown (machInst);
					}
				case 0x1:
					switch (machInst[BitField.RS_MSB]) {
					case 0x0:
						switch (machInst[BitField.RS_HI]) {
						case 0x0:
							switch (machInst[BitField.RS_LO]) {
							case 0x0:
								return new Mfc1 (machInst);
							case 0x2:
								return new Cfc1 (machInst);
							case 0x3:
								return new FailUnimplemented ("Mfhc1", machInst);
							case 0x4:
								return new Mtc1 (machInst);
							case 0x6:
								return new Ctc1 (machInst);
							case 0x7:
								return new FailUnimplemented ("Mthc1", machInst);
							case 0x1:
								return new CP1Unimplemented ("dmfc1", machInst);
							case 0x5:
								return new CP1Unimplemented ("dmtc1", machInst);
							default:
								return new Unknown (machInst);
							}
						case 0x1:
							switch (machInst[BitField.RS_LO]) {
							case 0x0:
								switch (machInst[BitField.ND]) {
								case 0x0:
									switch (machInst[BitField.TF]) {
									case 0x0:
										return new Bc1f (machInst);
									case 0x1:
										return new Bc1t (machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x1:
									switch (machInst[BitField.TF]) {
									case 0x0:
										return new Bc1fl (machInst);
									case 0x1:
										return new Bc1tl (machInst);
									default:
										return new Unknown (machInst);
									}
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								return new CP1Unimplemented ("bc1any2", machInst);
							case 0x2:
								return new CP1Unimplemented ("bc1any4", machInst);
							default:
								return new CP1Unimplemented ("unknown", machInst);
							}
						default:
							return new Unknown (machInst);
						}
					case 0x1:
						switch (machInst[BitField.RS_HI]) {
						case 0x2:
							switch (machInst[BitField.RS_LO]) {
							case 0x0:
								switch (machInst[BitField.FUNC_HI]) {
								case 0x0:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new Add_s (machInst);
									case 0x1:
										return new Sub_s (machInst);
									case 0x2:
										return new Mul_s (machInst);
									case 0x3:
										return new Div_s (machInst);
									case 0x4:
										return new Sqrt_s (machInst);
									case 0x5:
										return new Abs_s (machInst);
									case 0x7:
										return new Neg_s (machInst);
									case 0x6:
										return new Mov_s (machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x1:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Round_l_s", machInst);
									case 0x1:
										return new FailUnimplemented ("Trunc_l_s", machInst);
									case 0x2:
										return new FailUnimplemented ("Ceil_l_s", machInst);
									case 0x3:
										return new FailUnimplemented ("Floor_l_s", machInst);
									case 0x4:
										return new FailUnimplemented ("Round_w_s", machInst);
									case 0x5:
										return new FailUnimplemented ("Trunc_w_s", machInst);
									case 0x6:
										return new FailUnimplemented ("Ceil_w_s", machInst);
									case 0x7:
										return new FailUnimplemented ("Floor_w_s", machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x2:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x1:
										switch (machInst[BitField.MOVCF]) {
										case 0x0:
											return new FailUnimplemented ("Movf_s", machInst);
										case 0x1:
											return new FailUnimplemented ("Movt_s", machInst);
										default:
											return new Unknown (machInst);
										}
									case 0x2:
										return new FailUnimplemented ("Movz_s", machInst);
									case 0x3:
										return new FailUnimplemented ("Movn_s", machInst);
									case 0x5:
										return new FailUnimplemented ("Recip_s", machInst);
									case 0x6:
										return new FailUnimplemented ("Rsqrt_s", machInst);
									default:
										return new CP1Unimplemented ("unknown", machInst);
									}
								case 0x3:
									return new CP1Unimplemented ("unknown", machInst);
								case 0x4:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x1:
										return new Cvt_d_s (machInst);
									case 0x4:
										return new Cvt_w_s (machInst);
									case 0x5:
										return new Cvt_l_s (machInst);
									case 0x6:
										return new FailUnimplemented ("Cvt_ps_s", machInst);
									default:
										return new CP1Unimplemented ("unknown", machInst);
									}
								case 0x5:
									return new CP1Unimplemented ("unknown", machInst);
								case 0x6:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new C_f_s (machInst);
									case 0x1:
										return new C_un_s (machInst);
									case 0x2:
										return new C_eq_s (machInst);
									case 0x3:
										return new C_ueq_s (machInst);
									case 0x4:
										return new C_olt_s (machInst);
									case 0x5:
										return new C_ult_s (machInst);
									case 0x6:
										return new C_ole_s (machInst);
									case 0x7:
										return new C_ule_s (machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x7:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new C_sf_s (machInst);
									case 0x1:
										return new C_ngle_s (machInst);
									case 0x2:
										return new C_seq_s (machInst);
									case 0x3:
										return new C_ngl_s (machInst);
									case 0x4:
										return new C_lt_s (machInst);
									case 0x5:
										return new C_nge_s (machInst);
									case 0x6:
										return new C_le_s (machInst);
									case 0x7:
										return new C_ngt_s (machInst);
									default:
										return new Unknown (machInst);
									}
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								switch (machInst[BitField.FUNC_HI]) {
								case 0x0:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new Add_d (machInst);
									case 0x1:
										return new Sub_d (machInst);
									case 0x2:
										return new Mul_d (machInst);
									case 0x3:
										return new Div_d (machInst);
									case 0x4:
										return new Sqrt_d (machInst);
									case 0x5:
										return new Abs_d (machInst);
									case 0x7:
										return new Neg_d (machInst);
									case 0x6:
										return new Mov_d (machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x1:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Round_l_d", machInst);
									case 0x1:
										return new FailUnimplemented ("Trunc_l_d", machInst);
									case 0x2:
										return new FailUnimplemented ("Ceil_l_d", machInst);
									case 0x3:
										return new FailUnimplemented ("Floor_l_d", machInst);
									case 0x4:
										return new FailUnimplemented ("Round_w_d", machInst);
									case 0x5:
										return new FailUnimplemented ("Trunc_w_d", machInst);
									case 0x6:
										return new FailUnimplemented ("Ceil_w_d", machInst);
									case 0x7:
										return new FailUnimplemented ("Floor_w_d", machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x2:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x1:
										switch (machInst[BitField.MOVCF]) {
										case 0x0:
											return new FailUnimplemented ("Movf_d", machInst);
										case 0x1:
											return new FailUnimplemented ("Movt_d", machInst);
										default:
											return new Unknown (machInst);
										}
									case 0x2:
										return new FailUnimplemented ("Movz_d", machInst);
									case 0x3:
										return new FailUnimplemented ("Movn_d", machInst);
									case 0x5:
										return new FailUnimplemented ("Recip_d", machInst);
									case 0x6:
										return new FailUnimplemented ("Rsqrt_d", machInst);
									default:
										return new CP1Unimplemented ("unknown", machInst);
									}
								case 0x4:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new Cvt_s_d (machInst);
									case 0x4:
										return new Cvt_w_d (machInst);
									case 0x5:
										return new Cvt_l_d (machInst);
									default:
										return new CP1Unimplemented ("unknown", machInst);
									}
								case 0x6:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new C_f_d (machInst);
									case 0x1:
										return new C_un_d (machInst);
									case 0x2:
										return new C_eq_d (machInst);
									case 0x3:
										return new C_ueq_d (machInst);
									case 0x4:
										return new C_olt_d (machInst);
									case 0x5:
										return new C_ult_d (machInst);
									case 0x6:
										return new C_ole_d (machInst);
									case 0x7:
										return new C_ule_d (machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x7:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new C_sf_d (machInst);
									case 0x1:
										return new C_ngle_d (machInst);
									case 0x2:
										return new C_seq_d (machInst);
									case 0x3:
										return new C_ngl_d (machInst);
									case 0x4:
										return new C_lt_d (machInst);
									case 0x5:
										return new C_nge_d (machInst);
									case 0x6:
										return new C_le_d (machInst);
									case 0x7:
										return new C_ngt_d (machInst);
									default:
										return new Unknown (machInst);
									}
								default:
									return new CP1Unimplemented ("unknown", machInst);
								}
							case 0x2:
								return new CP1Unimplemented ("unknown", machInst);
							case 0x3:
								return new CP1Unimplemented ("unknown", machInst);
							case 0x7:
								return new CP1Unimplemented ("unknown", machInst);
							case 0x4:
								switch (machInst[BitField.FUNC]) {
								case 0x20:
									return new Cvt_s_w (machInst);
								case 0x21:
									return new Cvt_d_w (machInst);
								case 0x26:
									return new CP1Unimplemented ("cvt_ps_w", machInst);
								default:
									return new CP1Unimplemented ("unknown", machInst);
								}
							case 0x5:
								switch (machInst[BitField.FUNC_HI]) {
								case 0x20:
									return new Cvt_s_l (machInst);
								case 0x21:
									return new Cvt_d_l (machInst);
								case 0x26:
									return new CP1Unimplemented ("cvt_ps_l", machInst);
								default:
									return new CP1Unimplemented ("unknown", machInst);
								}
							case 0x6:
								switch (machInst[BitField.FUNC_HI]) {
								case 0x0:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Add_ps", machInst);
									case 0x1:
										return new FailUnimplemented ("Sub_ps", machInst);
									case 0x2:
										return new FailUnimplemented ("Mul_ps", machInst);
									case 0x5:
										return new FailUnimplemented ("Abs_ps", machInst);
									case 0x6:
										return new FailUnimplemented ("Mov_ps", machInst);
									case 0x7:
										return new FailUnimplemented ("Neg_ps", machInst);
									default:
										return new CP1Unimplemented ("unknown", machInst);
									}
								case 0x1:
									return new CP1Unimplemented ("unknown", machInst);
								case 0x2:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x1:
										switch (machInst[BitField.MOVCF]) {
										case 0x0:
											return new FailUnimplemented ("Movf_ps", machInst);
										case 0x1:
											return new FailUnimplemented ("Movt_ps", machInst);
										default:
											return new Unknown (machInst);
										}
									case 0x2:
										return new FailUnimplemented ("Movz_ps", machInst);
									case 0x3:
										return new FailUnimplemented ("Movn_ps", machInst);
									default:
										return new CP1Unimplemented ("unknown", machInst);
									}
								case 0x3:
									return new CP1Unimplemented ("unknown", machInst);
								case 0x4:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Cvt_s_pu", machInst);
									default:
										return new CP1Unimplemented ("unknown", machInst);
									}
								case 0x5:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Cvt_s_pl", machInst);
									case 0x4:
										return new FailUnimplemented ("Pll", machInst);
									case 0x5:
										return new FailUnimplemented ("Plu", machInst);
									case 0x6:
										return new FailUnimplemented ("Pul", machInst);
									case 0x7:
										return new FailUnimplemented ("Puu", machInst);
									default:
										return new CP1Unimplemented ("unknown", machInst);
									}
								case 0x6:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("C_f_ps", machInst);
									case 0x1:
										return new FailUnimplemented ("C_un_ps", machInst);
									case 0x2:
										return new FailUnimplemented ("C_eq_ps", machInst);
									case 0x3:
										return new FailUnimplemented ("C_ueq_ps", machInst);
									case 0x4:
										return new FailUnimplemented ("C_olt_ps", machInst);
									case 0x5:
										return new FailUnimplemented ("C_ult_ps", machInst);
									case 0x6:
										return new FailUnimplemented ("C_ole_ps", machInst);
									case 0x7:
										return new FailUnimplemented ("C_ule_ps", machInst);
									default:
										return new Unknown (machInst);
									}
								case 0x7:
									switch (machInst[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("C_sf_ps", machInst);
									case 0x1:
										return new FailUnimplemented ("C_ngle_ps", machInst);
									case 0x2:
										return new FailUnimplemented ("C_seq_ps", machInst);
									case 0x3:
										return new FailUnimplemented ("C_ngl_ps", machInst);
									case 0x4:
										return new FailUnimplemented ("C_lt_ps", machInst);
									case 0x5:
										return new FailUnimplemented ("C_nge_ps", machInst);
									case 0x6:
										return new FailUnimplemented ("C_le_ps", machInst);
									case 0x7:
										return new FailUnimplemented ("C_ngt_ps", machInst);
									default:
										return new Unknown (machInst);
									}
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						default:
							return new CP1Unimplemented ("unknown", machInst);
						}
					default:
						return new Unknown (machInst);
					}
				case 0x2:
					switch (machInst[BitField.RS_MSB]) {
					case 0x0:
						switch (machInst[BitField.RS_HI]) {
						case 0x0:
							switch (machInst[BitField.RS_LO]) {
							case 0x0:
								return new CP2Unimplemented ("mfc2", machInst);
							case 0x2:
								return new CP2Unimplemented ("cfc2", machInst);
							case 0x3:
								return new CP2Unimplemented ("mfhc2", machInst);
							case 0x4:
								return new CP2Unimplemented ("mtc2", machInst);
							case 0x6:
								return new CP2Unimplemented ("ctc2", machInst);
							case 0x7:
								return new CP2Unimplemented ("mftc2", machInst);
							default:
								return new CP2Unimplemented ("unknown", machInst);
							}
						case 0x1:
							switch (machInst[BitField.ND]) {
							case 0x0:
								switch (machInst[BitField.TF]) {
								case 0x0:
									return new CP2Unimplemented ("bc2f", machInst);
								case 0x1:
									return new CP2Unimplemented ("bc2t", machInst);
								default:
									return new CP2Unimplemented ("unknown", machInst);
								}
							case 0x1:
								switch (machInst[BitField.TF]) {
								case 0x0:
									return new CP2Unimplemented ("bc2fl", machInst);
								case 0x1:
									return new CP2Unimplemented ("bc2tl", machInst);
								default:
									return new CP2Unimplemented ("unknown", machInst);
								}
							default:
								return new CP2Unimplemented ("unknown", machInst);
							}
						default:
							return new CP2Unimplemented ("unknown", machInst);
						}
					default:
						return new CP2Unimplemented ("unknown", machInst);
					}
				case 0x3:
					switch (machInst[BitField.FUNC_HI]) {
					case 0x0:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Lwxc1", machInst);
						case 0x1:
							return new FailUnimplemented ("Ldxc1", machInst);
						case 0x5:
							return new FailUnimplemented ("Luxc1", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x1:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Swxc1", machInst);
						case 0x1:
							return new FailUnimplemented ("Sdxc1", machInst);
						case 0x5:
							return new FailUnimplemented ("Suxc1", machInst);
						case 0x7:
							return new FailUnimplemented ("Prefx", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x3:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x6:
							return new FailUnimplemented ("Alnv_ps", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x4:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Madd_s", machInst);
						case 0x1:
							return new FailUnimplemented ("Madd_d", machInst);
						case 0x6:
							return new FailUnimplemented ("Madd_ps", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x5:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Msub_s", machInst);
						case 0x1:
							return new FailUnimplemented ("Msub_d", machInst);
						case 0x6:
							return new FailUnimplemented ("Msub_ps", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x6:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Nmadd_s", machInst);
						case 0x1:
							return new FailUnimplemented ("Nmadd_d", machInst);
						case 0x6:
							return new FailUnimplemented ("Nmadd_ps", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x7:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Nmsub_s", machInst);
						case 0x1:
							return new FailUnimplemented ("Nmsub_d", machInst);
						case 0x6:
							return new FailUnimplemented ("Nmsub_ps", machInst);
						default:
							return new Unknown (machInst);
						}
					default:
						return new Unknown (machInst);
					}
				case 0x4:
					return new FailUnimplemented ("Beql", machInst);
				case 0x5:
					return new FailUnimplemented ("Bnel", machInst);
				case 0x6:
					return new FailUnimplemented ("Blezl", machInst);
				case 0x7:
					return new FailUnimplemented ("Bgtzl", machInst);
				default:
					return new Unknown (machInst);
				}
			case 0x3:
				switch (machInst[BitField.OPCODE_LO]) {
				case 0x4:
					switch (machInst[BitField.FUNC_HI]) {
					case 0x0:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x2:
							return new FailUnimplemented ("Mul", machInst);
						case 0x0:
							return new FailUnimplemented ("Madd", machInst);
						case 0x1:
							return new FailUnimplemented ("Maddu", machInst);
						case 0x4:
							return new FailUnimplemented ("Msub", machInst);
						case 0x5:
							return new FailUnimplemented ("Msubu", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x4:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Clz", machInst);
						case 0x1:
							return new FailUnimplemented ("Clo", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x7:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x7:
							return new FailUnimplemented ("sdbbp", machInst);
						default:
							return new Unknown (machInst);
						}
					default:
						return new Unknown (machInst);
					}
				case 0x7:
					switch (machInst[BitField.FUNC_HI]) {
					case 0x0:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Ext", machInst);
						case 0x4:
							return new FailUnimplemented ("Ins", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x1:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Fork", machInst);
						case 0x1:
							return new FailUnimplemented ("Yield", machInst);
						case 0x2:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Lwx", machInst);
								case 0x4:
									return new FailUnimplemented ("Lhx", machInst);
								case 0x6:
									return new FailUnimplemented ("Lbux", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0x4:
							return new FailUnimplemented ("Insv", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x2:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addu_qb", machInst);
								case 0x1:
									return new FailUnimplemented ("Subu_qb", machInst);
								case 0x4:
									return new FailUnimplemented ("Addu_s_qb", machInst);
								case 0x5:
									return new FailUnimplemented ("Subu_s_qb", machInst);
								case 0x6:
									return new FailUnimplemented ("Muleu_s_ph_qbl", machInst);
								case 0x7:
									return new FailUnimplemented ("Muleu_s_ph_qbr", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addu_ph", machInst);
								case 0x1:
									return new FailUnimplemented ("Subu_ph", machInst);
								case 0x2:
									return new FailUnimplemented ("Addq_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Subq_ph", machInst);
								case 0x4:
									return new FailUnimplemented ("Addu_s_ph", machInst);
								case 0x5:
									return new FailUnimplemented ("Subu_s_ph", machInst);
								case 0x6:
									return new FailUnimplemented ("Addq_s_ph", machInst);
								case 0x7:
									return new FailUnimplemented ("Subq_s_ph", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x2:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addsc", machInst);
								case 0x1:
									return new FailUnimplemented ("Addwc", machInst);
								case 0x2:
									return new FailUnimplemented ("Modsub", machInst);
								case 0x4:
									return new FailUnimplemented ("Raddu_w_qb", machInst);
								case 0x6:
									return new FailUnimplemented ("Addq_s_w", machInst);
								case 0x7:
									return new FailUnimplemented ("Subq_s_w", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x3:
								switch (machInst[BitField.OP_LO]) {
								case 0x4:
									return new FailUnimplemented ("Muleq_s_w_phl", machInst);
								case 0x5:
									return new FailUnimplemented ("Muleq_s_w_phr", machInst);
								case 0x6:
									return new FailUnimplemented ("Mulq_s_ph", machInst);
								case 0x7:
									return new FailUnimplemented ("Mulq_rs_ph", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0x1:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Cmpu_eq_qb", machInst);
								case 0x1:
									return new FailUnimplemented ("Cmpu_lt_qb", machInst);
								case 0x2:
									return new FailUnimplemented ("Cmpu_le_qb", machInst);
								case 0x3:
									return new FailUnimplemented ("Pick_qb", machInst);
								case 0x4:
									return new FailUnimplemented ("Cmpgu_eq_qb", machInst);
								case 0x5:
									return new FailUnimplemented ("Cmpgu_lt_qb", machInst);
								case 0x6:
									return new FailUnimplemented ("Cmpgu_le_qb", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Cmp_eq_ph", machInst);
								case 0x1:
									return new FailUnimplemented ("Cmp_lt_ph", machInst);
								case 0x2:
									return new FailUnimplemented ("Cmp_le_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Pick_ph", machInst);
								case 0x4:
									return new FailUnimplemented ("Precrq_qb_ph", machInst);
								case 0x5:
									return new FailUnimplemented ("Precr_qb_ph", machInst);
								case 0x6:
									return new FailUnimplemented ("Packrl_ph", machInst);
								case 0x7:
									return new FailUnimplemented ("Precrqu_s_qb_ph", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x2:
								switch (machInst[BitField.OP_LO]) {
								case 0x4:
									return new FailUnimplemented ("Precrq_ph_w", machInst);
								case 0x5:
									return new FailUnimplemented ("Precrq_rs_ph_w", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x3:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Cmpgdu_eq_qb", machInst);
								case 0x1:
									return new FailUnimplemented ("Cmpgdu_lt_qb", machInst);
								case 0x2:
									return new FailUnimplemented ("Cmpgdu_le_qb", machInst);
								case 0x6:
									return new FailUnimplemented ("Precr_sra_ph_w", machInst);
								case 0x7:
									return new FailUnimplemented ("Precr_sra_r_ph_w", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0x2:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x1:
									return new FailUnimplemented ("Absq_s_qb", machInst);
								case 0x2:
									return new FailUnimplemented ("Repl_qb", machInst);
								case 0x3:
									return new FailUnimplemented ("Replv_qb", machInst);
								case 0x4:
									return new FailUnimplemented ("Precequ_ph_qbl", machInst);
								case 0x5:
									return new FailUnimplemented ("Precequ_ph_qbr", machInst);
								case 0x6:
									return new FailUnimplemented ("Precequ_ph_qbla", machInst);
								case 0x7:
									return new FailUnimplemented ("Precequ_ph_qbra", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								switch (machInst[BitField.OP_LO]) {
								case 0x1:
									return new FailUnimplemented ("Absq_s_ph", machInst);
								case 0x2:
									return new FailUnimplemented ("Repl_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Replv_ph", machInst);
								case 0x4:
									return new FailUnimplemented ("Preceq_w_phl", machInst);
								case 0x5:
									return new FailUnimplemented ("Preceq_w_phr", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x2:
								switch (machInst[BitField.OP_LO]) {
								case 0x1:
									return new FailUnimplemented ("Absq_s_w", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x3:
								switch (machInst[BitField.OP_LO]) {
								case 0x3:
									return new FailUnimplemented ("Bitrev", machInst);
								case 0x4:
									return new FailUnimplemented ("Preceu_ph_qbl", machInst);
								case 0x5:
									return new FailUnimplemented ("Preceu_ph_qbr", machInst);
								case 0x6:
									return new FailUnimplemented ("Preceu_ph_qbla", machInst);
								case 0x7:
									return new FailUnimplemented ("Preceu_ph_qbra", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0x3:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Shll_qb", machInst);
								case 0x1:
									return new FailUnimplemented ("Shrl_qb", machInst);
								case 0x2:
									return new FailUnimplemented ("Shllv_qb", machInst);
								case 0x3:
									return new FailUnimplemented ("Shrlv_qb", machInst);
								case 0x4:
									return new FailUnimplemented ("Shra_qb", machInst);
								case 0x5:
									return new FailUnimplemented ("Shra_r_qb", machInst);
								case 0x6:
									return new FailUnimplemented ("Shrav_qb", machInst);
								case 0x7:
									return new FailUnimplemented ("Shrav_r_qb", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Shll_ph", machInst);
								case 0x1:
									return new FailUnimplemented ("Shra_ph", machInst);
								case 0x2:
									return new FailUnimplemented ("Shllv_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Shrav_ph", machInst);
								case 0x4:
									return new FailUnimplemented ("Shll_s_ph", machInst);
								case 0x5:
									return new FailUnimplemented ("Shra_r_ph", machInst);
								case 0x6:
									return new FailUnimplemented ("Shllv_s_ph", machInst);
								case 0x7:
									return new FailUnimplemented ("Shrav_r_ph", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x2:
								switch (machInst[BitField.OP_LO]) {
								case 0x4:
									return new FailUnimplemented ("Shll_s_w", machInst);
								case 0x5:
									return new FailUnimplemented ("Shra_r_w", machInst);
								case 0x6:
									return new FailUnimplemented ("Shllv_s_w", machInst);
								case 0x7:
									return new FailUnimplemented ("Shrav_r_w", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x3:
								switch (machInst[BitField.OP_LO]) {
								case 0x1:
									return new FailUnimplemented ("Shrl_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Shrlv_ph", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						default:
							return new Unknown (machInst);
						}
					case 0x3:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Adduh_qb", machInst);
								case 0x1:
									return new FailUnimplemented ("Subuh_qb", machInst);
								case 0x2:
									return new FailUnimplemented ("Adduh_r_qb", machInst);
								case 0x3:
									return new FailUnimplemented ("Subuh_r_qb", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addqh_ph", machInst);
								case 0x1:
									return new FailUnimplemented ("Subqh_ph", machInst);
								case 0x2:
									return new FailUnimplemented ("Addqh_r_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Subqh_r_ph", machInst);
								case 0x4:
									return new FailUnimplemented ("Mul_ph", machInst);
								case 0x6:
									return new FailUnimplemented ("Mul_s_ph", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x2:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addqh_w", machInst);
								case 0x1:
									return new FailUnimplemented ("Subqh_w", machInst);
								case 0x2:
									return new FailUnimplemented ("Addqh_r_w", machInst);
								case 0x3:
									return new FailUnimplemented ("Subqh_r_w", machInst);
								case 0x6:
									return new FailUnimplemented ("Mulq_s_w", machInst);
								case 0x7:
									return new FailUnimplemented ("Mulq_rs_w", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						default:
							return new Unknown (machInst);
						}
					case 0x4:
						switch (machInst[BitField.SA]) {
						case 0x2:
							return new FailUnimplemented ("Wsbh", machInst);
						case 0x10:
							return new FailUnimplemented ("Seb", machInst);
						case 0x18:
							return new FailUnimplemented ("Seh", machInst);
						default:
							return new Unknown (machInst);
						}
					case 0x6:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Dpa_w_ph", machInst);
								case 0x1:
									return new FailUnimplemented ("Dps_w_ph", machInst);
								case 0x2:
									return new FailUnimplemented ("Mulsa_w_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Dpau_h_qbl", machInst);
								case 0x4:
									return new FailUnimplemented ("Dpaq_s_w_ph", machInst);
								case 0x5:
									return new FailUnimplemented ("Dpsq_s_w_ph", machInst);
								case 0x6:
									return new FailUnimplemented ("Mulsaq_s_w_ph", machInst);
								case 0x7:
									return new FailUnimplemented ("Dpau_h_qbr", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Dpax_w_ph", machInst);
								case 0x1:
									return new FailUnimplemented ("Dpsx_w_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Dpsu_h_qbl", machInst);
								case 0x4:
									return new FailUnimplemented ("Dpaq_sa_l_w", machInst);
								case 0x5:
									return new FailUnimplemented ("Dpsq_sa_l_w", machInst);
								case 0x7:
									return new FailUnimplemented ("Dpsu_h_qbr", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x2:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Maq_sa_w_phl", machInst);
								case 0x2:
									return new FailUnimplemented ("Maq_sa_w_phr", machInst);
								case 0x4:
									return new FailUnimplemented ("Maq_s_w_phl", machInst);
								case 0x6:
									return new FailUnimplemented ("Maq_s_w_phr", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x3:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Dpaqx_s_w_ph", machInst);
								case 0x1:
									return new FailUnimplemented ("Dpsqx_s_w_ph", machInst);
								case 0x2:
									return new FailUnimplemented ("Dpaqx_sa_w_ph", machInst);
								case 0x3:
									return new FailUnimplemented ("Dpsqx_sa_w_ph", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0x1:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Append", machInst);
								case 0x1:
									return new FailUnimplemented ("Prepend", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x2:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Balign", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						default:
							return new Unknown (machInst);
						}
					case 0x7:
						switch (machInst[BitField.FUNC_LO]) {
						case 0x0:
							switch (machInst[BitField.OP_HI]) {
							case 0x0:
								switch (machInst[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Extr_w", machInst);
								case 0x1:
									return new FailUnimplemented ("Extrv_w", machInst);
								case 0x2:
									return new FailUnimplemented ("Extp", machInst);
								case 0x3:
									return new FailUnimplemented ("Extpv", machInst);
								case 0x4:
									return new FailUnimplemented ("Extr_r_w", machInst);
								case 0x5:
									return new FailUnimplemented ("Extrv_r_w", machInst);
								case 0x6:
									return new FailUnimplemented ("Extr_rs_w", machInst);
								case 0x7:
									return new FailUnimplemented ("Extrv_rs_w", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x1:
								switch (machInst[BitField.OP_LO]) {
								case 0x2:
									return new FailUnimplemented ("Extpdp", machInst);
								case 0x3:
									return new FailUnimplemented ("Extpdpv", machInst);
								case 0x6:
									return new FailUnimplemented ("Extr_s_h", machInst);
								case 0x7:
									return new FailUnimplemented ("Extrv_s_h", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x2:
								switch (machInst[BitField.OP_LO]) {
								case 0x2:
									return new FailUnimplemented ("Rddsp", machInst);
								case 0x3:
									return new FailUnimplemented ("Wrdsp", machInst);
								default:
									return new Unknown (machInst);
								}
							case 0x3:
								switch (machInst[BitField.OP_LO]) {
								case 0x2:
									return new FailUnimplemented ("Shilo", machInst);
								case 0x3:
									return new FailUnimplemented ("Shilov", machInst);
								case 0x7:
									return new FailUnimplemented ("Mthlip", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						case 0x3:
							switch (machInst[BitField.OP]) {
							case 0x0:
								switch (machInst[BitField.RD]) {
								case 0x1d:
									return new FailUnimplemented ("Rdhwr", machInst);
								default:
									return new Unknown (machInst);
								}
							default:
								return new Unknown (machInst);
							}
						default:
							return new Unknown (machInst);
						}
					default:
						return new Unknown (machInst);
					}
				default:
					return new Unknown (machInst);
				}
			case 0x4:
				switch (machInst[BitField.OPCODE_LO]) {
				case 0x0:
					return new Lb (machInst);
				case 0x1:
					return new Lh (machInst);
				case 0x3:
					return new Lw (machInst);
				case 0x4:
					return new Lbu (machInst);
				case 0x5:
					return new Lhu (machInst);
				case 0x2:
					return new Lwl (machInst);
				case 0x6:
					return new Lwr (machInst);
				default:
					return new Unknown (machInst);
				}
			case 0x5:
				switch (machInst[BitField.OPCODE_LO]) {
				case 0x0:
					return new Sb (machInst);
				case 0x1:
					return new Sh (machInst);
				case 0x3:
					return new Sw (machInst);
				case 0x2:
					return new Swl (machInst);
				case 0x6:
					return new Swr (machInst);
				case 0x7:
					return new FailUnimplemented ("Cache", machInst);
				default:
					return new Unknown (machInst);
				}
			case 0x6:
				switch (machInst[BitField.OPCODE_LO]) {
				case 0x0:
					return new Ll (machInst);
				case 0x1:
					return new Lwc1 (machInst);
				case 0x5:
					return new Ldc1 (machInst);
				case 0x2:
					return new CP2Unimplemented ("lwc2", machInst);
				case 0x6:
					return new CP2Unimplemented ("ldc2", machInst);
				case 0x3:
					return new FailUnimplemented ("Pref", machInst);
				default:
					return new Unknown (machInst);
				}
			case 0x7:
				switch (machInst[BitField.OPCODE_LO]) {
				case 0x0:
					return new Sc (machInst);
				case 0x1:
					return new Swc1 (machInst);
				case 0x5:
					return new Sdc1 (machInst);
				case 0x2:
					return new CP2Unimplemented ("swc2", machInst);
				case 0x6:
					return new CP2Unimplemented ("sdc2", machInst);
				default:
					return new Unknown (machInst);
				}
			default:
				return new Unknown (machInst);
			}
		}
	}

	public sealed class SyscallInst : StaticInst
	{
		public SyscallInst (MachInst machInst) : base("syscall", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, 2));
		}

		public override void Execute (Thread thread)
		{
			thread.Syscall (thread.Regs.IntRegs[2]);
		}
	}

	public sealed class Sll : StaticInst
	{
		public Sll (MachInst machInst) : base("sll", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RT]] << (int)this[BitField.SA];
		}
	}

	public sealed class Sllv : StaticInst
	{
		public Sllv (MachInst machInst) : base("sllv", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RT]] << (int)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0);
		}
	}

	public sealed class Sra : StaticInst
	{
		public Sra (MachInst machInst) : base("sra", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RT]] >> (int)this[BitField.SA]);
		}
	}

	public sealed class Srav : StaticInst
	{
		public Srav (MachInst machInst) : base("srav", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RT]] >> (int)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0));
		}
	}

	public sealed class Srl : StaticInst
	{
		public Srl (MachInst machInst) : base("srl", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RT]] >> (int)this[BitField.SA];
		}
	}

	public sealed class Srlv : StaticInst
	{
		public Srlv (MachInst machInst) : base("srlv", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RT]] >> (int)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0);
		}
	}

	public abstract class Branch : StaticInst
	{
		public Branch (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
			this.Displacement = BitHelper.Sext (this[BitField.OFFSET] << 2, 16);
		}

		public override uint GetTargetPc (Thread thread)
		{
			return (uint)(thread.Regs.Npc + this.Displacement);
		}

		public void DoBranch (Thread thread)
		{
			thread.Regs.Nnpc = this.GetTargetPc (thread);
		}

		public int Displacement {get; private set;}
	}

	public sealed class B : Branch
	{
		public B (MachInst machInst) : base("b", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Unconditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			this.DoBranch (thread);
		}
	}

	public sealed class Bal : Branch
	{
		public Bal (MachInst machInst) : base("bal", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Unconditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, RegisterConstants.RETURN_ADDRESS_REG));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.RETURN_ADDRESS_REG] = thread.Regs.Nnpc;
			this.DoBranch (thread);
		}
	}

	public sealed class Beq : Branch
	{
		public Beq (MachInst machInst) : base("beq", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] == (int)thread.Regs.IntRegs[this[BitField.RT]]) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Beqz : Branch
	{
		public Beqz (MachInst machInst) : base("beqz", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] == 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bgez : Branch
	{
		public Bgez (MachInst machInst) : base("bgez", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] >= 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bgezal : Branch
	{
		public Bgezal (MachInst machInst) : base("bgezal", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.Call | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, RegisterConstants.RETURN_ADDRESS_REG));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.RETURN_ADDRESS_REG] = thread.Regs.Nnpc;
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] >= 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bgtz : Branch
	{
		public Bgtz (MachInst machInst) : base("bgtz", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] > 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Blez : Branch
	{
		public Blez (MachInst machInst) : base("blez", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] <= 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bltz : Branch
	{
		public Bltz (MachInst machInst) : base("bltz", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] < 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bltzal : Branch
	{
		public Bltzal (MachInst machInst) : base("bltzal", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.Call | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, RegisterConstants.RETURN_ADDRESS_REG));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.RETURN_ADDRESS_REG] = thread.Regs.Nnpc;
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] < 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bne : Branch
	{
		public Bne (MachInst machInst) : base("bne", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] != (int)thread.Regs.IntRegs[this[BitField.RT]]) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bnez : Branch
	{
		public Bnez (MachInst machInst) : base("bnez", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Control | StaticInstFlag.Conditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] != 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bc1f : Branch
	{
		public Bc1f (MachInst machInst) : base("bc1f", machInst, StaticInstFlag.Control | StaticInstFlag.Conditional, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Fcsr));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			bool cond = !BitHelper.GetFCC (fcsr, (int)this[BitField.BRANCH_CC]);
			
			if (cond) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bc1t : Branch
	{
		public Bc1t (MachInst machInst) : base("bc1t", machInst, StaticInstFlag.Control | StaticInstFlag.Conditional, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Fcsr));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			bool cond = BitHelper.GetFCC (fcsr, (int)this[BitField.BRANCH_CC]);
			
			if (cond) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bc1fl : Branch
	{
		public Bc1fl (MachInst machInst) : base("bc1fl", machInst, StaticInstFlag.Control | StaticInstFlag.Conditional, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Fcsr));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			bool cond = !BitHelper.GetFCC (fcsr, (int)this[BitField.BRANCH_CC]);
			
			if (cond) {
				this.DoBranch (thread);
			} else {
				thread.Regs.Npc = thread.Regs.Nnpc;
				thread.Regs.Nnpc = (uint)(thread.Regs.Nnpc + Marshal.SizeOf (typeof(uint)));
			}
		}
	}

	public sealed class Bc1tl : Branch
	{
		public Bc1tl (MachInst machInst) : base("bc1tl", machInst, StaticInstFlag.Control | StaticInstFlag.Conditional, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Fcsr));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			bool cond = BitHelper.GetFCC (fcsr, (int)this[BitField.BRANCH_CC]);
			
			if (cond) {
				this.DoBranch (thread);
			} else {
				thread.Regs.Npc = thread.Regs.Nnpc;
				thread.Regs.Nnpc = (uint)(thread.Regs.Nnpc + Marshal.SizeOf (typeof(uint)));
			}
		}
	}

	public abstract class Jump : StaticInst
	{
		public Jump (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
			this.Target = this[BitField.JMPTARG] << 2;
		}

		public void DoJump (Thread thread)
		{
			thread.Regs.Nnpc = this.GetTargetPc (thread);
		}

		public uint Target {get; private set;}
	}

	public sealed class J : Jump
	{
		public J (MachInst machInst) : base("j", machInst, StaticInstFlag.Control | StaticInstFlag.Unconditional | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override uint GetTargetPc (Thread thread)
		{
			return BitHelper.Mbits (thread.Regs.Npc, 32, 28) | this.Target;
		}

		public override void Execute (Thread thread)
		{
			this.DoJump (thread);
		}
	}

	public sealed class Jal : Jump
	{
		public Jal (MachInst machInst) : base("jal", machInst, StaticInstFlag.Control | StaticInstFlag.Unconditional | StaticInstFlag.Call | StaticInstFlag.DirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, RegisterConstants.RETURN_ADDRESS_REG));
		}

		public override uint GetTargetPc (Thread thread)
		{
			return BitHelper.Mbits (thread.Regs.Npc, 32, 28) | this.Target;
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.RETURN_ADDRESS_REG] = thread.Regs.Nnpc;
			this.DoJump (thread);
		}
	}

	public sealed class Jalr : Jump
	{
		public Jalr (MachInst machInst) : base("jalr", machInst, StaticInstFlag.Control | StaticInstFlag.Unconditional | StaticInstFlag.Call | StaticInstFlag.IndirectJump, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override uint GetTargetPc (Thread thread)
		{
			return thread.Regs.IntRegs[this[BitField.RS]];
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.Nnpc;
			this.DoJump (thread);
		}
	}

	public sealed class Jr : Jump
	{
		public Jr (MachInst machInst) : base("jr", machInst, StaticInstFlag.Control | StaticInstFlag.Unconditional | StaticInstFlag.FunctionReturn | StaticInstFlag.IndirectJump, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		public override uint GetTargetPc (Thread thread)
		{
			return thread.Regs.IntRegs[this[BitField.RS]];
		}

		public override void Execute (Thread thread)
		{
			this.DoJump (thread);
		}
	}

	public abstract class IntOp : StaticInst
	{
		public IntOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
		}
	}

	public abstract class IntImmOp : StaticInst
	{
		public IntImmOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
			this.Imm = (short)machInst[BitField.INTIMM];
			
			this.ZextImm = 0x0000FFFF & machInst[BitField.INTIMM];
			
			this.SextImm = BitHelper.Sext (machInst[BitField.INTIMM], 16);
		}

		public short Imm {get; private set;}
		public int SextImm {get; private set;}
		public uint ZextImm {get; private set;}
	}

	public sealed class Add : IntOp
	{
		public Add (MachInst machInst) : base("add", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + (int)thread.Regs.IntRegs[this[BitField.RT]]);
			Logger.Warn (LogCategory.Instruction, "Add: overflow trap not implemented.");
		}
	}

	public sealed class Addi : IntImmOp
	{
		public Addi (MachInst machInst) : base("addi", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Immediate, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + this.SextImm);
			Logger.Warn (LogCategory.Instruction, "Addi: overflow trap not implemented.");
		}
	}

	public sealed class Addiu : IntImmOp
	{
		public Addiu (MachInst machInst) : base("addiu", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Immediate, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + this.SextImm);
		}
	}

	public sealed class Addu : IntOp
	{
		public Addu (MachInst machInst) : base("addu", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + (int)thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public sealed class Sub : IntOp
	{
		public Sub (MachInst machInst) : base("sub", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] - (int)thread.Regs.IntRegs[this[BitField.RT]]);
			Logger.Warn (LogCategory.Instruction, "Sub: overflow trap not implemented.");
		}
	}

	public sealed class Subu : IntOp
	{
		public Subu (MachInst machInst) : base("subu", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] - (int)thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public sealed class And : IntOp
	{
		public And (MachInst machInst) : base("and", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] & thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public sealed class Andi : IntImmOp
	{
		public Andi (MachInst machInst) : base("andi", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Immediate, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] & this.ZextImm;
		}
	}

	public sealed class Nor : IntOp
	{
		public Nor (MachInst machInst) : base("nor", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = ~(thread.Regs.IntRegs[this[BitField.RS]] | thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public sealed class Or : IntOp
	{
		public Or (MachInst machInst) : base("or", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] | thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public sealed class Ori : IntImmOp
	{
		public Ori (MachInst machInst) : base("ori", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Immediate, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] | this.ZextImm;
		}
	}

	public sealed class Xor : IntOp
	{
		public Xor (MachInst machInst) : base("xor", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] ^ thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public sealed class Xori : IntImmOp
	{
		public Xori (MachInst machInst) : base("xori", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Immediate, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] ^ this.ZextImm;
		}
	}

	public sealed class Slt : IntOp
	{
		public Slt (MachInst machInst) : base("slt", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (int)thread.Regs.IntRegs[this[BitField.RS]] < (int)thread.Regs.IntRegs[this[BitField.RT]] ? 1u : 0;
		}
	}

	public sealed class Slti : IntImmOp
	{
		public Slti (MachInst machInst) : base("slti", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Immediate, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (int)thread.Regs.IntRegs[this[BitField.RS]] < this.SextImm ? 1u : 0;
		}
	}

	public sealed class Sltiu : IntImmOp
	{
		public Sltiu (MachInst machInst) : base("sltiu", machInst, StaticInstFlag.IntegerComputation | StaticInstFlag.Immediate, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)thread.Regs.IntRegs[this[BitField.RS]] < this.ZextImm ? 1u : 0;
		}
	}

	public sealed class Sltu : IntOp
	{
		public Sltu (MachInst machInst) : base("sltu", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RS]] < (uint)thread.Regs.IntRegs[this[BitField.RT]] ? 1u : 0;
		}
	}

	public sealed class Lui : IntImmOp
	{
		public Lui (MachInst machInst) : base("lui", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)(this.Imm << 16);
		}
	}

	public sealed class Divu : StaticInst
	{
		public Divu (MachInst machInst) : base("divu", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntDivide)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Lo));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Hi));
		}

		public override void Execute (Thread thread)
		{
			ulong rs = 0;
			ulong rt = 0;
			
			uint lo = 0;
			uint hi = 0;
			
			rs = thread.Regs.IntRegs[this[BitField.RS]];
			rt = thread.Regs.IntRegs[this[BitField.RT]];
			
			if (rt != 0) {
				lo = (uint)(rs / rt);
				hi = (uint)(rs % rt);
			}
			
			thread.Regs.MiscRegs.Lo = lo;
			thread.Regs.MiscRegs.Hi = hi;
		}
	}

	public sealed class Div : StaticInst
	{
		public Div (MachInst machInst) : base("div", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntDivide)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Lo));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Hi));
		}

		public override void Execute (Thread thread)
		{
			long rs = 0;
			long rt = 0;
			
			uint lo = 0;
			uint hi = 0;
			
			rs = BitHelper.Sext (thread.Regs.IntRegs[this[BitField.RS]], 32);
			rt = BitHelper.Sext (thread.Regs.IntRegs[this[BitField.RT]], 32);
			
			if (rt != 0) {
				lo = (uint)(rs / rt);
				hi = (uint)(rs % rt);
			}
			
			thread.Regs.MiscRegs.Lo = lo;
			thread.Regs.MiscRegs.Hi = hi;
		}
	}

	public sealed class Mflo : StaticInst
	{
		public Mflo (MachInst machInst) : base("mflo", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Lo));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.MiscRegs.Lo;
		}
	}

	public sealed class Mfhi : StaticInst
	{
		public Mfhi (MachInst machInst) : base("mfhi", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Hi));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.MiscRegs.Hi;
		}
	}

	public sealed class Mtlo : StaticInst
	{
		public Mtlo (MachInst machInst) : base("mtlo", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Lo));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.MiscRegs.Lo = thread.Regs.IntRegs[this[BitField.RD]];
		}
	}

	public sealed class Mthi : StaticInst
	{
		public Mthi (MachInst machInst) : base("mthi", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RD]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Hi));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.MiscRegs.Hi = thread.Regs.IntRegs[this[BitField.RD]];
		}
	}

	public sealed class Mult : StaticInst
	{
		public Mult (MachInst machInst) : base("mult", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Lo));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Hi));
		}

		public override void Execute (Thread thread)
		{
			long rs = 0;
			long rt = 0;
			
			rs = BitHelper.Sext (thread.Regs.IntRegs[this[BitField.RS]], 32);
			rt = BitHelper.Sext (thread.Regs.IntRegs[this[BitField.RT]], 32);
			
			long val = rs * rt;
			
			uint lo = (uint)BitHelper.Bits64 ((ulong)val, 31, 0);
			uint hi = (uint)BitHelper.Bits64 ((ulong)val, 63, 32);
			
			thread.Regs.MiscRegs.Lo = lo;
			thread.Regs.MiscRegs.Hi = hi;
		}
	}

	public class Multu : StaticInst
	{
		public Multu (MachInst machInst) : base("multu", machInst, StaticInstFlag.IntegerComputation, FunctionalUnitType.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Lo));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Hi));
		}

		public override void Execute (Thread thread)
		{
			ulong rs = 0;
			ulong rt = 0;
			
			rs = thread.Regs.IntRegs[this[BitField.RS]];
			rt = thread.Regs.IntRegs[this[BitField.RT]];
			
			ulong val = rs * rt;
			
			uint lo = (uint)BitHelper.Bits64 (val, 31, 0);
			uint hi = (uint)BitHelper.Bits64 (val, 63, 32);
			
			thread.Regs.MiscRegs.Lo = lo;
			thread.Regs.MiscRegs.Hi = hi;
		}
	}

	public abstract class FloatOp : StaticInst
	{
		public FloatOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
		}
	}

	public abstract class FloatBinaryOp : FloatOp
	{
		public FloatBinaryOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FD]));
		}
	}

	public abstract class FloatUnaryOp : FloatOp
	{
		public FloatUnaryOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FD]));
		}
	}

	public sealed class Add_d : FloatBinaryOp
	{
		public Add_d (MachInst machInst) : base("add_d", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatAdd)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			
			double fd = fs + ft;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Sub_d : FloatBinaryOp
	{
		public Sub_d (MachInst machInst) : base("sub_d", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatAdd)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			
			double fd = fs - ft;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Mul_d : FloatBinaryOp
	{
		public Mul_d (MachInst machInst) : base("mul_d", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatMultiply)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			
			double fd = fs * ft;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Div_d : FloatBinaryOp
	{
		public Div_d (MachInst machInst) : base("div_d", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatDivide)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			
			double fd = fs / ft;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Sqrt_d : FloatUnaryOp
	{
		public Sqrt_d (MachInst machInst) : base("sqrt_d", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatSquareRoot)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = Math.Sqrt (fs);
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Abs_d : FloatUnaryOp
	{
		public Abs_d (MachInst machInst) : base("abs_d", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatCompare)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = Math.Abs (fs);
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Neg_d : FloatUnaryOp
	{
		public Neg_d (MachInst machInst) : base("neg_d", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatCompare)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = -1 * fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Mov_d : FloatUnaryOp
	{
		public Mov_d (MachInst machInst) : base("mov_d", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double fd = fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Add_s : FloatBinaryOp
	{
		public Add_s (MachInst machInst) : base("add_s", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatAdd)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			
			float fd = fs + ft;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Sub_s : FloatBinaryOp
	{
		public Sub_s (MachInst machInst) : base("sub_s", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatAdd)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			
			float fd = fs - ft;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Mul_s : FloatBinaryOp
	{
		public Mul_s (MachInst machInst) : base("mul_s", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatMultiply)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			
			float fd = fs * ft;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Div_s : FloatBinaryOp
	{
		public Div_s (MachInst machInst) : base("div_s", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatDivide)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			
			float fd = fs / ft;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Sqrt_s : FloatUnaryOp
	{
		public Sqrt_s (MachInst machInst) : base("sqrt_s", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatSquareRoot)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = (float)Math.Sqrt (fs);
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Abs_s : FloatUnaryOp
	{
		public Abs_s (MachInst machInst) : base("abs_s", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatCompare)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = Math.Abs (fs);
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Neg_s : FloatUnaryOp
	{
		public Neg_s (MachInst machInst) : base("neg_s", machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatCompare)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = -fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Mov_s : FloatUnaryOp
	{
		public Mov_s (MachInst machInst) : base("mov_s", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float fd = fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public abstract class FloatConvertOp : FloatOp
	{
		public FloatConvertOp (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatConvert)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FD]));
		}
	}

	public sealed class Cvt_d_s : FloatConvertOp
	{
		public Cvt_d_s (MachInst machInst) : base("cvt_d_s", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			double fd = (double)fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_w_s : FloatConvertOp
	{
		public Cvt_w_s (MachInst machInst) : base("cvt_w_s", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			uint fd = (uint)fs;
			
			thread.Regs.FloatRegs.SetUint (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_l_s : FloatConvertOp
	{
		public Cvt_l_s (MachInst machInst) : base("cvt_l_s", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			ulong fd = (ulong)fs;
			
			thread.Regs.FloatRegs.SetUlong (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_s_d : FloatConvertOp
	{
		public Cvt_s_d (MachInst machInst) : base("cvt_s_d", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			float fd = (float)fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_w_d : FloatConvertOp
	{
		public Cvt_w_d (MachInst machInst) : base("cvt_w_d", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			uint fd = (uint)fs;
			
			thread.Regs.FloatRegs.SetUint (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_l_d : FloatConvertOp
	{
		public Cvt_l_d (MachInst machInst) : base("cvt_l_d", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			ulong fd = (ulong)fs;
			
			thread.Regs.FloatRegs.SetUlong (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_s_w : FloatConvertOp
	{
		public Cvt_s_w (MachInst machInst) : base("cvt_s_w", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			uint fs = thread.Regs.FloatRegs.GetUint (this[BitField.FS]);
			float fd = (float)fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_d_w : FloatConvertOp
	{
		public Cvt_d_w (MachInst machInst) : base("cvt_d_w", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			uint fs = thread.Regs.FloatRegs.GetUint (this[BitField.FS]);
			double fd = (double)fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_s_l : FloatConvertOp
	{
		public Cvt_s_l (MachInst machInst) : base("cvt_s_l", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			ulong fs = thread.Regs.FloatRegs.GetUlong (this[BitField.FS]);
			float fd = (float)fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_d_l : FloatConvertOp
	{
		public Cvt_d_l (MachInst machInst) : base("cvt_d_l", machInst)
		{
		}

		public override void Execute (Thread thread)
		{
			ulong fs = thread.Regs.FloatRegs.GetUlong (this[BitField.FS]);
			double fd = (double)fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public abstract class FloatCompareOp : StaticInst
	{
		public FloatCompareOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FT]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Fcsr));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Fcsr));
		}
	}

	public abstract class C_cond_d : FloatCompareOp
	{
		public C_cond_d (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatCompare)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			
			bool less;
			bool equal;
			
			bool unordered = double.IsNaN (fs) || double.IsNaN (ft);
			if (unordered) {
				equal = false;
				less = false;
			} else {
				equal = fs == ft;
				less = fs < ft;
			}
			
			uint cond = this[BitField.COND];
			
			if ((((cond & 0x4) != 0) && less) || (((cond & 0x2) != 0) && equal) || (((cond & 0x1) != 0) && unordered)) {
				BitHelper.SetFCC (ref fcsr, (int)this[BitField.CC]);
			} else {
				BitHelper.ClearFCC (ref fcsr, (int)this[BitField.CC]);
			}
			
			thread.Regs.MiscRegs.Fcsr = fcsr;
		}
	}

	public abstract class C_cond_s : FloatCompareOp
	{
		public C_cond_s (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.FloatComputation, FunctionalUnitType.FloatCompare)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			
			bool less;
			bool equal;
			
			bool unordered = float.IsNaN (fs) || float.IsNaN (ft);
			if (unordered) {
				equal = false;
				less = false;
			} else {
				equal = fs == ft;
				less = fs < ft;
			}
			
			uint cond = this[BitField.COND];
			
			if ((((cond & 0x4) != 0) && less) || (((cond & 0x2) != 0) && equal) || (((cond & 0x1) != 0) && unordered)) {
				BitHelper.SetFCC (ref fcsr, (int)this[BitField.CC]);
			} else {
				BitHelper.ClearFCC (ref fcsr, (int)this[BitField.CC]);
			}
			
			thread.Regs.MiscRegs.Fcsr = fcsr;
		}
	}

	public sealed class C_f_d : C_cond_d
	{
		public C_f_d (MachInst machInst) : base("c_f_d", machInst)
		{
		}
	}

	public sealed class C_un_d : C_cond_d
	{
		public C_un_d (MachInst machInst) : base("c_un_d", machInst)
		{
		}
	}

	public sealed class C_eq_d : C_cond_d
	{
		public C_eq_d (MachInst machInst) : base("c_eq_d", machInst)
		{
		}
	}

	public sealed class C_ueq_d : C_cond_d
	{
		public C_ueq_d (MachInst machInst) : base("c_ueq_d", machInst)
		{
		}
	}

	public sealed class C_olt_d : C_cond_d
	{
		public C_olt_d (MachInst machInst) : base("c_olt_d", machInst)
		{
		}
	}

	public sealed class C_ult_d : C_cond_d
	{
		public C_ult_d (MachInst machInst) : base("c_ult_d", machInst)
		{
		}
	}

	public sealed class C_ole_d : C_cond_d
	{
		public C_ole_d (MachInst machInst) : base("c_ole_d", machInst)
		{
		}
	}

	public sealed class C_ule_d : C_cond_d
	{
		public C_ule_d (MachInst machInst) : base("c_ule_d", machInst)
		{
		}
	}

	public sealed class C_sf_d : C_cond_d
	{
		public C_sf_d (MachInst machInst) : base("c_sf_d", machInst)
		{
		}
	}

	public sealed class C_ngle_d : C_cond_d
	{
		public C_ngle_d (MachInst machInst) : base("c_ngle_d", machInst)
		{
		}
	}

	public sealed class C_seq_d : C_cond_d
	{
		public C_seq_d (MachInst machInst) : base("c_seq_d", machInst)
		{
		}
	}

	public sealed class C_ngl_d : C_cond_d
	{
		public C_ngl_d (MachInst machInst) : base("c_ngl_d", machInst)
		{
		}
	}

	public sealed class C_lt_d : C_cond_d
	{
		public C_lt_d (MachInst machInst) : base("c_lt_d", machInst)
		{
		}
	}

	public sealed class C_nge_d : C_cond_d
	{
		public C_nge_d (MachInst machInst) : base("c_nge_d", machInst)
		{
		}
	}

	public sealed class C_le_d : C_cond_d
	{
		public C_le_d (MachInst machInst) : base("c_le_d", machInst)
		{
		}
	}

	public sealed class C_ngt_d : C_cond_d
	{
		public C_ngt_d (MachInst machInst) : base("c_ngt_d", machInst)
		{
		}
	}

	public sealed class C_f_s : C_cond_s
	{
		public C_f_s (MachInst machInst) : base("c_f_s", machInst)
		{
		}
	}

	public sealed class C_un_s : C_cond_s
	{
		public C_un_s (MachInst machInst) : base("c_un_s", machInst)
		{
		}
	}

	public sealed class C_eq_s : C_cond_s
	{
		public C_eq_s (MachInst machInst) : base("c_eq_s", machInst)
		{
		}
	}

	public sealed class C_ueq_s : C_cond_s
	{
		public C_ueq_s (MachInst machInst) : base("c_ueq_s", machInst)
		{
		}
	}

	public sealed class C_olt_s : C_cond_s
	{
		public C_olt_s (MachInst machInst) : base("c_olt_s", machInst)
		{
		}
	}

	public sealed class C_ult_s : C_cond_s
	{
		public C_ult_s (MachInst machInst) : base("c_ult_s", machInst)
		{
		}
	}

	public sealed class C_ole_s : C_cond_s
	{
		public C_ole_s (MachInst machInst) : base("c_ole_s", machInst)
		{
		}
	}

	public sealed class C_ule_s : C_cond_s
	{
		public C_ule_s (MachInst machInst) : base("c_ule_s", machInst)
		{
		}
	}

	public sealed class C_sf_s : C_cond_s
	{
		public C_sf_s (MachInst machInst) : base("c_sf_s", machInst)
		{
		}
	}

	public sealed class C_ngle_s : C_cond_s
	{
		public C_ngle_s (MachInst machInst) : base("c_ngle_s", machInst)
		{
		}
	}

	public sealed class C_seq_s : C_cond_s
	{
		public C_seq_s (MachInst machInst) : base("c_seq_s", machInst)
		{
		}
	}

	public sealed class C_ngl_s : C_cond_s
	{
		public C_ngl_s (MachInst machInst) : base("c_ngl_s", machInst)
		{
		}
	}

	public sealed class C_lt_s : C_cond_s
	{
		public C_lt_s (MachInst machInst) : base("c_lt_s", machInst)
		{
		}
	}

	public sealed class C_nge_s : C_cond_s
	{
		public C_nge_s (MachInst machInst) : base("c_nge_s", machInst)
		{
		}
	}

	public sealed class C_le_s : C_cond_s
	{
		public C_le_s (MachInst machInst) : base("c_le_s", machInst)
		{
		}
	}

	public sealed class C_ngt_s : C_cond_s
	{
		public C_ngt_s (MachInst machInst) : base("c_ngt_s", machInst)
		{
		}
	}

	public abstract class MemoryOp : StaticInst
	{
		public MemoryOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
			this.Displacement = BitHelper.Sext (machInst[BitField.OFFSET], 16);
		}

		public virtual uint Ea (Thread thread)
		{
			uint ea = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			return ea;
		}

		protected override void SetupDeps ()
		{
			this.MemIDeps = new List<RegisterDependency> ();
			this.MemODeps = new List<RegisterDependency> ();
			
			this.SetupEaDeps ();
			
			this.EaODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Ea));
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Ea));
			
			this.SetupMemDeps ();
		}

		protected abstract void SetupEaDeps ();
		protected abstract void SetupMemDeps ();

		public List<RegisterDependency> EaIdeps {
			get { return this.IDeps; }
			protected set { this.IDeps = value; }
		}

		public List<RegisterDependency> EaODeps {
			get { return this.ODeps; }
			protected set { this.ODeps = value; }
		}

		public List<RegisterDependency> MemIDeps {get; protected set;}
		public List<RegisterDependency> MemODeps {get; protected set;}

		public int Displacement { get; private set; }
	}

	public sealed class Lb : MemoryOp
	{
		public Lb (MachInst machInst) : base("lb", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			byte mem = 0;
			thread.Mem.ReadByte (this.Ea (thread), (byte*)&mem);
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public sealed class Lbu : MemoryOp
	{
		public Lbu (MachInst machInst) : base("lbu", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			byte mem = 0;
			thread.Mem.ReadByte (this.Ea (thread), &mem);
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public sealed class Lh : MemoryOp
	{
		public Lh (MachInst machInst) : base("lh", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			short mem = 0;
			thread.Mem.ReadHalfWord (this.Ea (thread), (ushort*)&mem);
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)mem;
		}
	}

	public sealed class Lhu : MemoryOp
	{
		public Lhu (MachInst machInst) : base("lhu", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			ushort mem = 0;
			thread.Mem.ReadHalfWord (this.Ea (thread), &mem);
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public sealed class Lw : MemoryOp
	{
		public Lw (MachInst machInst) : base("lw", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			int mem = 0;
			thread.Mem.ReadWord (this.Ea (thread), (uint*)&mem);
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)mem;
		}
	}

	public sealed class Lwl : MemoryOp
	{
		public Lwl (MachInst machInst) : base("lwl", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override uint Ea (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		unsafe public override void Execute (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = 0;
			
			thread.Mem.ReadWord (ea, &mem);
			
			uint mem_shift = 24 - 8 * byte_offset;
			
			uint rt = (mem << (int)mem_shift) | (thread.Regs.IntRegs[this[BitField.RT]] & BitHelper.Mask ((int)mem_shift));
			
			thread.Regs.IntRegs[this[BitField.RT]] = rt;
		}
	}

	public sealed class Lwr : MemoryOp
	{
		public Lwr (MachInst machInst) : base("lwr", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override uint Ea (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		unsafe public override void Execute (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = 0;
			
			thread.Mem.ReadWord (ea, &mem);
			
			uint mem_shift = 8 * byte_offset;
			
			uint rt = (thread.Regs.IntRegs[this[BitField.RT]] & (BitHelper.Mask ((int)mem_shift) << (int)(32 - mem_shift))) | (mem >> (int)mem_shift);
			
			thread.Regs.IntRegs[this[BitField.RT]] = rt;
		}
	}

	public sealed class Ll : MemoryOp
	{
		public Ll (MachInst machInst) : base("ll", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			uint mem = 0;
			thread.Mem.ReadWord (this.Ea (thread), &mem);
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public sealed class Lwc1 : MemoryOp
	{
		public Lwc1 (MachInst machInst) : base("lwc1", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			uint mem = 0;
			thread.Mem.ReadWord (this.Ea (thread), &mem);
			thread.Regs.FloatRegs.SetUint (mem, this[BitField.FT]);
		}
	}

	public sealed class Ldc1 : MemoryOp
	{
		public Ldc1 (MachInst machInst) : base("ldc1", machInst, StaticInstFlag.Memory | StaticInstFlag.Load | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			ulong mem = 0;
			thread.Mem.ReadDoubleWord (this.Ea (thread), &mem);
			thread.Regs.FloatRegs.SetUlong (mem, this[BitField.FT]);
		}
	}

	public sealed class Sb : MemoryOp
	{
		public Sb (MachInst machInst) : base("sb", machInst, StaticInstFlag.Memory | StaticInstFlag.Store | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			byte mem = (byte)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RT]], 7, 0);
			thread.Mem.WriteByte (this.Ea (thread), mem);
		}
	}

	public sealed class Sh : MemoryOp
	{
		public Sh (MachInst machInst) : base("sh", machInst, StaticInstFlag.Memory | StaticInstFlag.Store | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			ushort mem = (ushort)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RT]], 15, 0);
			thread.Mem.WriteHalfWord (this.Ea (thread), mem);
		}
	}

	public sealed class Sw : MemoryOp
	{
		public Sw (MachInst machInst) : base("sw", machInst, StaticInstFlag.Memory | StaticInstFlag.Store | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			uint mem = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Mem.WriteWord (this.Ea (thread), mem);
		}
	}

	public sealed class Swl : MemoryOp
	{
		public Swl (MachInst machInst) : base("swl", machInst, StaticInstFlag.Memory | StaticInstFlag.Store | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override uint Ea (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		unsafe public override void Execute (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = 0;
			
			thread.Mem.ReadWord (ea, &mem);
			
			uint reg_shift = 24 - 8 * byte_offset;
			uint mem_shift = 32 - reg_shift;
			
			mem = (mem & (BitHelper.Mask ((int)reg_shift) << (int)mem_shift)) | (thread.Regs.IntRegs[this[BitField.RT]] >> (int)reg_shift);
			
			thread.Mem.WriteWord (ea, mem);
		}
	}

	public sealed class Swr : MemoryOp
	{
		public Swr (MachInst machInst) : base("swr", machInst, StaticInstFlag.Memory | StaticInstFlag.Store | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override uint Ea (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		unsafe public override void Execute (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = 0;
			
			thread.Mem.ReadWord (ea, &mem);
			
			uint reg_shift = 8 * byte_offset;
			
			mem = thread.Regs.IntRegs[this[BitField.RT]] << (int)reg_shift | (mem & (BitHelper.Mask ((int)reg_shift)));
			
			thread.Mem.WriteWord (ea, mem);
		}
	}

	public sealed class Sc : MemoryOp
	{
		public Sc (MachInst machInst) : base("sc", machInst, StaticInstFlag.Memory | StaticInstFlag.Store | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Mem.WriteWord (this.Ea (thread), rt);
			thread.Regs.IntRegs[this[BitField.RT]] = 1;
		}
	}

	public sealed class Swc1 : MemoryOp
	{
		public Swc1 (MachInst machInst) : base("swc1", machInst, StaticInstFlag.Memory | StaticInstFlag.Store | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FT]));
		}

		public override void Execute (Thread thread)
		{
			uint ft = thread.Regs.FloatRegs.GetUint (this[BitField.FT]);
			thread.Mem.WriteWord (this.Ea (thread), ft);
		}
	}

	public sealed class Sdc1 : MemoryOp
	{
		public Sdc1 (MachInst machInst) : base("sdc1", machInst, StaticInstFlag.Memory | StaticInstFlag.Store | StaticInstFlag.DisplacedAddressing, FunctionalUnitType.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FT]));
		}

		public override void Execute (Thread thread)
		{
			ulong ft = thread.Regs.FloatRegs.GetUlong (this[BitField.FT]);
			thread.Mem.WriteDoubleWord (this.Ea (thread), ft);
		}
	}

	public abstract class CP1Control : StaticInst
	{
		public CP1Control (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
		}
	}

	public sealed class Mfc1 : CP1Control
	{
		public Mfc1 (MachInst machInst) : base("mfc1", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			uint fs = thread.Regs.FloatRegs.GetUint (this[BitField.FS]);
			thread.Regs.IntRegs[this[BitField.RT]] = fs;
		}
	}

	public sealed class Cfc1 : CP1Control
	{
		public Cfc1 (MachInst machInst) : base("cfc1", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Fcsr));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			
			uint rt = 0;
			
			if (this[BitField.FS] == 31) {
				rt = fcsr;
				thread.Regs.IntRegs[this[BitField.RT]] = rt;
			}
		}
	}

	public sealed class Mtc1 : CP1Control
	{
		public Mtc1 (MachInst machInst) : base("mtc1", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Float, this[BitField.FS]));
		}

		public override void Execute (Thread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Regs.FloatRegs.SetUint (rt, this[BitField.FS]);
		}
	}

	public sealed class Ctc1 : CP1Control
	{
		public Ctc1 (MachInst machInst) : base("ctc1", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.Misc, (uint)MiscRegNums.Fcsr));
		}

		public override void Execute (Thread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			
			if (this[BitField.FS] != 0) {
				thread.Regs.MiscRegs.Fcsr = rt;
			}
		}
	}

	public sealed class Nop : StaticInst
	{
		public Nop (MachInst machInst) : base("nop", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
		}
	}

	public sealed class FailUnimplemented : StaticInst
	{
		public FailUnimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.Invoke (thread);
		}
	}

	public sealed class CP0Unimplemented : StaticInst
	{
		public CP0Unimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.Invoke (thread);
		}
	}

	public sealed class CP1Unimplemented : StaticInst
	{
		public CP1Unimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.Invoke (thread);
		}
	}

	public sealed class CP2Unimplemented : StaticInst
	{
		public CP2Unimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.Invoke (thread);
		}
	}

	public sealed class WarnUnimplemented : StaticInst
	{
		public WarnUnimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.Invoke (thread);
		}
	}

	public sealed class Unknown : StaticInst
	{
		public Unknown (MachInst machInst) : base("unknown", machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new ReservedInstructionFault ();
			fault.Invoke(thread);
		}
	}

	public abstract class Trap : StaticInst
	{
		public Trap (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
		}

		protected override void SetupDeps ()
		{
		}
	}

	public abstract class TrapImm : StaticInst
	{
		public TrapImm (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.None, FunctionalUnitType.None)
		{
			this.Imm = (short)machInst[BitField.INTIMM];
		}

		protected override void SetupDeps ()
		{
		}

		protected short Imm {get; set;}
	}

	public abstract class Fault
	{
		public Fault ()
		{
		}

		protected abstract string Name {get;}

		public void Invoke (Thread thread)
		{
			Logger.Panicf (LogCategory.Instruction, "{0:s} detected @ PC 0x{1:x8}", this.Name, thread.Regs.Pc);
		}
	}

	public sealed class UnimplFault : Fault
	{
		public UnimplFault (string text)
		{
			this.Text = text;
		}

		protected override string Name
		{
			get
			{
				return string.Format("UnimplFault ({0:s})\n", this.Text);
			}
		}

		public string Text {get;private set;}
	}

	public sealed class ReservedInstructionFault : Fault
	{
		public ReservedInstructionFault ()
		{
		}

		protected override string Name
		{
			get
			{
				return "ReservedInstructionFault";
			}
		}
	}
}
