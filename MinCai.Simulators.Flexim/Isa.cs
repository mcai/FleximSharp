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
using MinCai.Simulators.Flexim.Pipelines;

namespace MinCai.Simulators.Flexim.Architecture
{
	public class BitField
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

	public class MachInst
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
			get { return BitUtils.Bits (this.Data, (int)(field.Hi), (int)(field.Lo)); }
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

		public bool isFPLoadStore {
			get {
				uint opcode = this[BitField.OPCODE];
				return (opcode == 0x31 || opcode == 0x39);
			}
		}

		public bool isOneOpBranch {
			get {
				uint opcode = this[BitField.OPCODE];
				return ((opcode == 0x00) || (opcode == 0x01) || (opcode == 0x06) || (opcode == 0x07));
			}
		}

		public bool isShift {
			get {
				uint func = this[BitField.FUNC];
				return (func == 0x00 || func == 0x01 || func == 0x03);
			}
		}

		public bool isCVT {
			get {
				uint func = this[BitField.FUNC];
				return (func == 32 || func == 33 || func == 36);
			}
		}

		public bool isCompare {
			get {
				uint func = this[BitField.FUNC];
				return (func >= 48);
			}
		}

		public bool isGPRFPMove {
			get {
				uint rs = this[BitField.RS];
				return (rs == 0 || rs == 4);
			}
		}

		public bool isGPRFCRMove {
			get {
				uint rs = this[BitField.RS];
				return (rs == 2 || rs == 6);
			}
		}

		public bool isFPBranch {
			get {
				uint rs = this[BitField.RS];
				return (rs == 8);
			}
		}

		public bool isSyscall {
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

		public uint Data { get; set; }
	}

	/* instruction flags */
	public enum StaticInstFlag : uint
	{
		NONE = 0x00000000,

		/// <summary>
		/// integer computation
		/// </summary>
		ICOMP = 0x00000001,

		/// <summary>
		/// floating-point computation
		/// </summary>
		FCOMP = 0x00000002,

		/// <summary>
		/// control inst
		/// </summary>
		CTRL = 0x00000004,

		/// <summary>
		/// unconditional change
		/// </summary>
		UNCOND = 0x00000008,

		/// <summary>
		/// conditional change
		/// </summary>
		COND = 0x00000010,

		/// <summary>
		/// memory access inst
		/// </summary>
		MEM = 0x00000020,

		/// <summary>
		/// load inst
		/// </summary>
		LOAD = 0x00000040,

		/// <summary>
		/// store inst
		/// </summary>
		STORE = 0x00000080,

		/// <summary>
		/// displaced (R+C) addr mode
		/// </summary>
		DISP = 0x00000100,

		/// <summary>
		/// R+R addr mode
		/// </summary>
		RR = 0x00000200,

		/// <summary>
		/// direct addressing mode
		/// </summary>
		DIRECT = 0x00000400,

		/// <summary>
		/// traping inst
		/// </summary>
		TRAP = 0x00000800,

		/// <summary>
		/// long latency inst (for sched)
		/// </summary>
		LONGLAT = 0x00001000,

		/// <summary>
		/// direct jump
		/// </summary>
		DIRJMP = 0x00002000,

		/// <summary>
		/// indirect jump
		/// </summary>
		INDIRJMP = 0x00004000,

		/// <summary>
		/// function call
		/// </summary>
		CALL = 0x00008000,

		/// <summary>
		/// floating point conditional branch
		/// </summary>
		FPCOND = 0x00010000,

		/// <summary>
		/// instruction has immediate operand
		/// </summary>
		IMM = 0x00020000,

		/// <summary>
		/// function return
		/// </summary>
		RET = 0x00040000
	}

	public static class RegisterConstants
	{
		public static uint NumIntRegs = 32;
		public static uint NumFloatRegs = 32;
		public static uint NumMiscRegs = 4;

		public static uint ZeroReg = 0;
		public static uint AssemblerReg = 1;
		public static uint SyscallSuccessReg = 7;
		public static uint FirstArgumentReg = 4;
		public static uint ReturnValueReg = 2;

		public static uint KernelReg0 = 26;
		public static uint KernelReg1 = 27;
		public static uint GlobalPointerReg = 28;
		public static uint StackPointerReg = 29;
		public static uint FramePointerReg = 30;
		public static uint ReturnAddressReg = 31;

		public static uint SyscallPseudoReturnReg = 3;
	}

	public enum MiscRegNums : int
	{
		LO = 0,
		HI = 1,
		EA = 2,
		FCSR = 3
	}

	public abstract class RegisterFile
	{
		public abstract void Clear ();
	}

	public class IntRegisterFile : RegisterFile
	{
		public IntRegisterFile ()
		{
			this.Regs = new uint[RegisterConstants.NumIntRegs];
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
				Debug.Assert (index < RegisterConstants.NumIntRegs);
				
				uint val = this.Regs[index];
//				Logger.Infof(LogCategory.THREAD, "    Reading int reg {0:d} as 0x{1:x8}.", index, val);
				return val;
			}
			set {
				Debug.Assert (index < RegisterConstants.NumIntRegs);
//				Logger.Infof(LogCategory.THREAD, "    Setting int reg {0:d} to 0x{1:x8}.", index, value);
				this.Regs[index] = value;
			}
		}

		public uint[] Regs { get; private set; }
	}

	public class FloatRegisterFile : RegisterFile
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
			this.Regs.f = new float[RegisterConstants.NumFloatRegs];
			this.Regs.i = new int[RegisterConstants.NumFloatRegs];
			this.Regs.d = new double[RegisterConstants.NumFloatRegs / 2];
			this.Regs.l = new long[RegisterConstants.NumFloatRegs / 2];
		}

		public float GetFloat (uint index)
		{
			Debug.Assert (index < RegisterConstants.NumFloatRegs);
			
			float val = this.Regs.f[index];
//			Logger.Infof(LogCategory.REGISTER, "    Reading float reg {0:d} as {1:f}.", index, val);
			return val;
		}

		public void SetFloat (float val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NumFloatRegs);
			
			this.Regs.f[index] = val;
//			Logger.Infof(LogCategory.REGISTER, "    Setting float reg {0:d} to {1:f}.", index, val);
		}

		public double GetDouble (uint index)
		{
			Debug.Assert (index < RegisterConstants.NumFloatRegs);
			
			double val = this.Regs.d[index / 2];
//			Logger.Infof(LogCategory.REGISTER, "    Reading double reg {0:d} as {1:f}.", index, val);
			return val;
		}

		public void SetDouble (double val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NumFloatRegs);
			
			this.Regs.d[index / 2] = val;
//			Logger.Infof(LogCategory.REGISTER, "    Setting double reg {0:d} to {1:f}.", index, val);
		}

		public uint GetUint (uint index)
		{
			Debug.Assert (index < RegisterConstants.NumFloatRegs);
			
			uint val = (uint)(this.Regs.i[index]);
//			Logger.Infof(LogCategory.REGISTER, "    Reading float reg {0:d} bits as 0x{1:x8}.", index, val);
			return val;
		}

		public void SetUint (uint val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NumFloatRegs);
			
			this.Regs.i[index] = (int)val;
//			Logger.Infof(LogCategory.REGISTER, "    Setting float reg (0:d} bits to 0x{1:x8}.", index, val);
		}

		public ulong GetUlong (uint index)
		{
			Debug.Assert (index < RegisterConstants.NumFloatRegs);
			
			ulong val = (ulong)(this.Regs.l[index / 2]);
//			Logger.Infof(LogCategory.REGISTER, "    Reading double reg {0:d} bits as 0x{1:x8}.", index, val);
			return val;
		}

		public void SetUlong (ulong val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NumFloatRegs);
			
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

	public class MiscRegisterFile : RegisterFile
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

	public class CombinedRegisterFile : RegisterFile
	{
		public CombinedRegisterFile ()
		{
			this.speculative = false;
			
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

		public bool Speculative {
			get { return this.speculative; }
			set {
				if (this.speculative != value) {
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
					
					this.speculative = value;
				}
			}
		}

		private bool speculative;
	}

	public abstract class ISA
	{
		public ISA ()
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

		public Dictionary<uint, StaticInst> DecodedInsts { get; set; }
	}

	public enum RegisterDependencyType
	{
		INT,
		FP,
		MISC
	}

	public class RegisterDependency
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

		public RegisterDependencyType Type { get; set; }
		public uint Num { get; set; }
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

		public virtual uint TargetPc (Thread thread)
		{
			return 0;
		}

		public abstract void SetupDeps ();

		public abstract void Execute (Thread thread);

		public override string ToString ()
		{
			return string.Format ("[StaticInst: MachInst={0}, Mnemonic={1}, Flags={2}, FuType={3}]", this.MachInst, this.Mnemonic, this.Flags, this.FuType);
		}

		public uint this[BitField field] {
			get { return this.MachInst[field]; }
		}

		public bool IsLongLat {
			get { return (this.Flags & StaticInstFlag.LONGLAT) == StaticInstFlag.LONGLAT; }
		}

		public bool IsTrap {
			get { return (this.Flags & StaticInstFlag.TRAP) == StaticInstFlag.TRAP; }
		}

		public bool IsMem {
			get { return (this.Flags & StaticInstFlag.MEM) == StaticInstFlag.MEM; }
		}

		public bool IsLoad {
			get { return this.IsMem && (this.Flags & StaticInstFlag.LOAD) == StaticInstFlag.LOAD; }
		}

		public bool IsStore {
			get { return this.IsMem && (this.Flags & StaticInstFlag.STORE) == StaticInstFlag.STORE; }
		}

		public bool IsConditional {
			get { return (this.Flags & StaticInstFlag.COND) == StaticInstFlag.COND; }
		}

		public bool IsUnconditional {
			get { return (this.Flags & StaticInstFlag.UNCOND) == StaticInstFlag.UNCOND; }
		}

		public bool IsDirectJump {
			get { return (this.Flags & StaticInstFlag.DIRJMP) != StaticInstFlag.DIRJMP; }
		}

		public bool IsControl {
			get { return (this.Flags & StaticInstFlag.CTRL) == StaticInstFlag.CTRL; }
		}

		public bool IsCall {
			get { return (this.Flags & StaticInstFlag.CALL) == StaticInstFlag.CALL; }
		}

		public bool IsReturn {
			get { return (this.Flags & StaticInstFlag.RET) == StaticInstFlag.RET; }
		}

		public bool IsNop {
			get { return (this as Nop) != null; }
		}

		public List<RegisterDependency> IDeps { get; set; }
		public List<RegisterDependency> ODeps { get; set; }

		public MachInst MachInst { get; set; }
		public string Mnemonic { get; set; }
		public StaticInstFlag Flags { get; set; }
		public FunctionalUnitType FuType { get; set; }
	}

	public class DynamicInst
	{
		public DynamicInst (Thread thread, uint pc, StaticInst staticInst)
		{
			this.Thread = thread;
			this.Pc = pc;
			this.StaticInst = staticInst;
		}

		public void Execute ()
		{
			this.Thread.Regs.IntRegs[RegisterConstants.ZeroReg] = 0;
			this.StaticInst.Execute (this.Thread);
		}

		public override string ToString ()
		{
			return string.Format ("[DynamicInst: Dis={0}, Thread.Name={1}]", Disassemble(this.StaticInst.MachInst, this.Pc, this.Thread), this.Thread.Name);
		}

		public uint PhysPc {
			get { return this.Thread.Core.MMU.Translate (this.Pc); }
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
				if (machInst.isOneOpBranch) {
					buf.AppendFormat ("${0}, {1:d}", MIPS_GPR_NAMES[machInst[BitField.RS]], (short)machInst[BitField.INTIMM]);
				} else if (machInst.IsLoadStore) {
					buf.AppendFormat ("${0}, {1:d}(${2})", MIPS_GPR_NAMES[machInst[BitField.RT]], (short)machInst[BitField.INTIMM], MIPS_GPR_NAMES[machInst[BitField.RS]]);
				} else if (machInst.isFPLoadStore) {
					buf.AppendFormat ("$f{0}, {1:d}(${2})", machInst[BitField.FT], (short)machInst[BitField.INTIMM], MIPS_GPR_NAMES[machInst[BitField.RS]]);
				} else {
					buf.AppendFormat ("${0}, ${1}, {2:d}", MIPS_GPR_NAMES[machInst[BitField.RT]], MIPS_GPR_NAMES[machInst[BitField.RS]], (short)machInst[BitField.INTIMM]);
				}
				break;
			case MachInstType.F:
				if (machInst.isCVT) {
					buf.AppendFormat ("$f{0:d}, $f{1:d}", machInst[BitField.FD], machInst[BitField.FS]);
				} else if (machInst.isCompare) {
					buf.AppendFormat ("{0:d}, $f{1:d}, $f{2:d}", machInst[BitField.FD] >> 2, machInst[BitField.FS], machInst[BitField.FT]);
				} else if (machInst.isFPBranch) {
					buf.AppendFormat ("{0:d}, {1:d}", machInst[BitField.FD] >> 2, (short)machInst[BitField.INTIMM]);
				} else if (machInst.isGPRFPMove) {
					buf.AppendFormat ("${0}, $f{1:d}", MIPS_GPR_NAMES[machInst[BitField.RT]], machInst[BitField.FS]);
				} else if (machInst.isGPRFCRMove) {
					buf.AppendFormat ("${0}, ${1:d}", MIPS_GPR_NAMES[machInst[BitField.RT]], machInst[BitField.FS]);
				} else {
					buf.AppendFormat ("$f{0:d}, $f{1:d}, $f{2:d}", machInst[BitField.FD], machInst[BitField.FS], machInst[BitField.FT]);
				}
				break;
			case MachInstType.R:
				if (machInst.isSyscall) {
				} else if (machInst.isShift) {
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
				Logger.Fatal (LogCategory.INSTRUCTION, "you can not reach here");
				break;
			}
			
			return buf.ToString ();
		}
	}

	public class MipsISA : ISA
	{
		public MipsISA ()
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

	public class SyscallInst : StaticInst
	{
		public SyscallInst (MachInst machInst) : base("syscall", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, 2));
		}

		public override void Execute (Thread thread)
		{
			thread.Syscall (thread.Regs.IntRegs[2]);
		}
	}

	public class Sll : StaticInst
	{
		public Sll (MachInst machInst) : base("sll", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RT]] << (int)this[BitField.SA];
		}
	}

	public class Sllv : StaticInst
	{
		public Sllv (MachInst machInst) : base("sllv", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RT]] << (int)BitUtils.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0);
		}
	}

	public class Sra : StaticInst
	{
		public Sra (MachInst machInst) : base("sra", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RT]] >> (int)this[BitField.SA]);
		}
	}

	public class Srav : StaticInst
	{
		public Srav (MachInst machInst) : base("srav", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RT]] >> (int)BitUtils.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0));
		}
	}

	public class Srl : StaticInst
	{
		public Srl (MachInst machInst) : base("srl", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RT]] >> (int)this[BitField.SA];
		}
	}

	public class Srlv : StaticInst
	{
		public Srlv (MachInst machInst) : base("srlv", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RT]] >> (int)BitUtils.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0);
		}
	}

	public abstract class Branch : StaticInst
	{
		public Branch (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
			this.displacement = BitUtils.Sext (this[BitField.OFFSET] << 2, 16);
		}

		public override uint TargetPc (Thread thread)
		{
			return (uint)(thread.Regs.Npc + this.displacement);
		}

		public void branch (Thread thread)
		{
			thread.Regs.Nnpc = this.TargetPc (thread);
		}

		private int displacement;
	}

	public class B : Branch
	{
		public B (MachInst machInst) : base("b", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.UNCOND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			this.branch (thread);
		}
	}

	public class Bal : Branch
	{
		public Bal (MachInst machInst) : base("bal", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.UNCOND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, RegisterConstants.ReturnAddressReg));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.ReturnAddressReg] = thread.Regs.Nnpc;
			this.branch (thread);
		}
	}

	public class Beq : Branch
	{
		public Beq (MachInst machInst) : base("beq", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] == (int)thread.Regs.IntRegs[this[BitField.RT]]) {
				this.branch (thread);
			}
		}
	}

	public class Beqz : Branch
	{
		public Beqz (MachInst machInst) : base("beqz", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] == 0) {
				this.branch (thread);
			}
		}
	}

	public class Bgez : Branch
	{
		public Bgez (MachInst machInst) : base("bgez", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] >= 0) {
				this.branch (thread);
			}
		}
	}

	public class Bgezal : Branch
	{
		public Bgezal (MachInst machInst) : base("bgezal", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.CALL | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, RegisterConstants.ReturnAddressReg));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.ReturnAddressReg] = thread.Regs.Nnpc;
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] >= 0) {
				this.branch (thread);
			}
		}
	}

	public class Bgtz : Branch
	{
		public Bgtz (MachInst machInst) : base("bgtz", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] > 0) {
				this.branch (thread);
			}
		}
	}

	public class Blez : Branch
	{
		public Blez (MachInst machInst) : base("blez", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] <= 0) {
				this.branch (thread);
			}
		}
	}

	public class Bltz : Branch
	{
		public Bltz (MachInst machInst) : base("bltz", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] < 0) {
				this.branch (thread);
			}
		}
	}

	public class Bltzal : Branch
	{
		public Bltzal (MachInst machInst) : base("bltzal", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.CALL | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, RegisterConstants.ReturnAddressReg));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.ReturnAddressReg] = thread.Regs.Nnpc;
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] < 0) {
				this.branch (thread);
			}
		}
	}

	public class Bne : Branch
	{
		public Bne (MachInst machInst) : base("bne", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] != (int)thread.Regs.IntRegs[this[BitField.RT]]) {
				this.branch (thread);
			}
		}
	}

	public class Bnez : Branch
	{
		public Bnez (MachInst machInst) : base("bnez", machInst, StaticInstFlag.ICOMP | StaticInstFlag.CTRL | StaticInstFlag.COND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void Execute (Thread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] != 0) {
				this.branch (thread);
			}
		}
	}

	public class Bc1f : Branch
	{
		public Bc1f (MachInst machInst) : base("bc1f", machInst, StaticInstFlag.CTRL | StaticInstFlag.COND, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.FCSR));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			bool cond = !BitUtils.GetFCC (fcsr, (int)this[BitField.BRANCH_CC]);
			
			if (cond) {
				this.branch (thread);
			}
		}
	}

	public class Bc1t : Branch
	{
		public Bc1t (MachInst machInst) : base("bc1t", machInst, StaticInstFlag.CTRL | StaticInstFlag.COND, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.FCSR));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			bool cond = BitUtils.GetFCC (fcsr, (int)this[BitField.BRANCH_CC]);
			
			if (cond) {
				this.branch (thread);
			}
		}
	}

	public class Bc1fl : Branch
	{
		public Bc1fl (MachInst machInst) : base("bc1fl", machInst, StaticInstFlag.CTRL | StaticInstFlag.COND, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.FCSR));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			bool cond = !BitUtils.GetFCC (fcsr, (int)this[BitField.BRANCH_CC]);
			
			if (cond) {
				this.branch (thread);
			} else {
				thread.Regs.Npc = thread.Regs.Nnpc;
				thread.Regs.Nnpc = (uint)(thread.Regs.Nnpc + Marshal.SizeOf (typeof(uint)));
			}
		}
	}

	public class Bc1tl : Branch
	{
		public Bc1tl (MachInst machInst) : base("bc1tl", machInst, StaticInstFlag.CTRL | StaticInstFlag.COND, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.FCSR));
		}

		public override void Execute (Thread thread)
		{
			uint fcsr = thread.Regs.MiscRegs.Fcsr;
			bool cond = BitUtils.GetFCC (fcsr, (int)this[BitField.BRANCH_CC]);
			
			if (cond) {
				this.branch (thread);
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
			this.target = this[BitField.JMPTARG] << 2;
		}

		public void jump (Thread thread)
		{
			thread.Regs.Nnpc = this.TargetPc (thread);
		}

		public uint target;
	}

	public class J : Jump
	{
		public J (MachInst machInst) : base("j", machInst, StaticInstFlag.CTRL | StaticInstFlag.UNCOND | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override uint TargetPc (Thread thread)
		{
			return BitUtils.Mbits (thread.Regs.Npc, 32, 28) | this.target;
		}

		public override void Execute (Thread thread)
		{
			this.jump (thread);
		}
	}

	public class Jal : Jump
	{
		public Jal (MachInst machInst) : base("jal", machInst, StaticInstFlag.CTRL | StaticInstFlag.UNCOND | StaticInstFlag.CALL | StaticInstFlag.DIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, RegisterConstants.ReturnAddressReg));
		}

		public override uint TargetPc (Thread thread)
		{
			return BitUtils.Mbits (thread.Regs.Npc, 32, 28) | this.target;
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.ReturnAddressReg] = thread.Regs.Nnpc;
			this.jump (thread);
		}
	}

	public class Jalr : Jump
	{
		public Jalr (MachInst machInst) : base("jalr", machInst, StaticInstFlag.CTRL | StaticInstFlag.UNCOND | StaticInstFlag.CALL | StaticInstFlag.INDIRJMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override uint TargetPc (Thread thread)
		{
			return thread.Regs.IntRegs[this[BitField.RS]];
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.Nnpc;
			this.jump (thread);
		}
	}

	public class Jr : Jump
	{
		public Jr (MachInst machInst) : base("jr", machInst, StaticInstFlag.CTRL | StaticInstFlag.UNCOND | StaticInstFlag.RET | StaticInstFlag.INDIRJMP, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override uint TargetPc (Thread thread)
		{
			return thread.Regs.IntRegs[this[BitField.RS]];
		}

		public override void Execute (Thread thread)
		{
			this.jump (thread);
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
			this.imm = (short)machInst[BitField.INTIMM];
			
			this.zextImm = 0x0000FFFF & machInst[BitField.INTIMM];
			
			this.sextImm = BitUtils.Sext (machInst[BitField.INTIMM], 16);
		}

		public short imm;
		public int sextImm;
		public uint zextImm;
	}

	public class Add : IntOp
	{
		public Add (MachInst machInst) : base("add", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + (int)thread.Regs.IntRegs[this[BitField.RT]]);
			Logger.Warn (LogCategory.INSTRUCTION, "Add: overflow trap not implemented.");
		}
	}

	public class Addi : IntImmOp
	{
		public Addi (MachInst machInst) : base("addi", machInst, StaticInstFlag.ICOMP | StaticInstFlag.IMM, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + this.sextImm);
			Logger.Warn (LogCategory.INSTRUCTION, "Addi: overflow trap not implemented.");
		}
	}

	public class Addiu : IntImmOp
	{
		public Addiu (MachInst machInst) : base("addiu", machInst, StaticInstFlag.ICOMP | StaticInstFlag.IMM, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + this.sextImm);
		}
	}

	public class Addu : IntOp
	{
		public Addu (MachInst machInst) : base("addu", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + (int)thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public class Sub : IntOp
	{
		public Sub (MachInst machInst) : base("sub", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] - (int)thread.Regs.IntRegs[this[BitField.RT]]);
			Logger.Warn (LogCategory.INSTRUCTION, "Sub: overflow trap not implemented.");
		}
	}

	public class Subu : IntOp
	{
		public Subu (MachInst machInst) : base("subu", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] - (int)thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public class And : IntOp
	{
		public And (MachInst machInst) : base("and", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] & thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public class Andi : IntImmOp
	{
		public Andi (MachInst machInst) : base("andi", machInst, StaticInstFlag.ICOMP | StaticInstFlag.IMM, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] & this.zextImm;
		}
	}

	public class Nor : IntOp
	{
		public Nor (MachInst machInst) : base("nor", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = ~(thread.Regs.IntRegs[this[BitField.RS]] | thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public class Or : IntOp
	{
		public Or (MachInst machInst) : base("or", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] | thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public class Ori : IntImmOp
	{
		public Ori (MachInst machInst) : base("ori", machInst, StaticInstFlag.ICOMP | StaticInstFlag.IMM, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] | this.zextImm;
		}
	}

	public class Xor : IntOp
	{
		public Xor (MachInst machInst) : base("xor", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] ^ thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public class Xori : IntImmOp
	{
		public Xori (MachInst machInst) : base("xori", machInst, StaticInstFlag.ICOMP | StaticInstFlag.IMM, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] ^ this.zextImm;
		}
	}

	public class Slt : IntOp
	{
		public Slt (MachInst machInst) : base("slt", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (int)thread.Regs.IntRegs[this[BitField.RS]] < (int)thread.Regs.IntRegs[this[BitField.RT]] ? 1u : 0;
		}
	}

	public class Slti : IntImmOp
	{
		public Slti (MachInst machInst) : base("slti", machInst, StaticInstFlag.ICOMP | StaticInstFlag.IMM, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (int)thread.Regs.IntRegs[this[BitField.RS]] < this.sextImm ? 1u : 0;
		}
	}

	public class Sltiu : IntImmOp
	{
		public Sltiu (MachInst machInst) : base("sltiu", machInst, StaticInstFlag.ICOMP | StaticInstFlag.IMM, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)thread.Regs.IntRegs[this[BitField.RS]] < this.zextImm ? 1u : 0;
		}
	}

	public class Sltu : IntOp
	{
		public Sltu (MachInst machInst) : base("sltu", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RS]] < (uint)thread.Regs.IntRegs[this[BitField.RT]] ? 1u : 0;
		}
	}

	public class Lui : IntImmOp
	{
		public Lui (MachInst machInst) : base("lui", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)(this.imm << 16);
		}
	}

	public class Divu : StaticInst
	{
		public Divu (MachInst machInst) : base("divu", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntDIV)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.HI));
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

	public class Div : StaticInst
	{
		public Div (MachInst machInst) : base("div", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntDIV)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.HI));
		}

		public override void Execute (Thread thread)
		{
			long rs = 0;
			long rt = 0;
			
			uint lo = 0;
			uint hi = 0;
			
			rs = BitUtils.Sext (thread.Regs.IntRegs[this[BitField.RS]], 32);
			rt = BitUtils.Sext (thread.Regs.IntRegs[this[BitField.RT]], 32);
			
			if (rt != 0) {
				lo = (uint)(rs / rt);
				hi = (uint)(rs % rt);
			}
			
			thread.Regs.MiscRegs.Lo = lo;
			thread.Regs.MiscRegs.Hi = hi;
		}
	}

	public class Mflo : StaticInst
	{
		public Mflo (MachInst machInst) : base("mflo", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.MiscRegs.Lo;
		}
	}

	public class Mfhi : StaticInst
	{
		public Mfhi (MachInst machInst) : base("mfhi", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.HI));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.MiscRegs.Hi;
		}
	}

	public class Mtlo : StaticInst
	{
		public Mtlo (MachInst machInst) : base("mtlo", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.LO));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.MiscRegs.Lo = thread.Regs.IntRegs[this[BitField.RD]];
		}
	}

	public class Mthi : StaticInst
	{
		public Mthi (MachInst machInst) : base("mthi", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RD]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.HI));
		}

		public override void Execute (Thread thread)
		{
			thread.Regs.MiscRegs.Hi = thread.Regs.IntRegs[this[BitField.RD]];
		}
	}

	public class Mult : StaticInst
	{
		public Mult (MachInst machInst) : base("mult", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.HI));
		}

		public override void Execute (Thread thread)
		{
			long rs = 0;
			long rt = 0;
			
			rs = BitUtils.Sext (thread.Regs.IntRegs[this[BitField.RS]], 32);
			rt = BitUtils.Sext (thread.Regs.IntRegs[this[BitField.RT]], 32);
			
			long val = rs * rt;
			
			uint lo = (uint)BitUtils.Bits64 ((ulong)val, 31, 0);
			uint hi = (uint)BitUtils.Bits64 ((ulong)val, 63, 32);
			
			thread.Regs.MiscRegs.Lo = lo;
			thread.Regs.MiscRegs.Hi = hi;
		}
	}

	public class Multu : StaticInst
	{
		public Multu (MachInst machInst) : base("multu", machInst, StaticInstFlag.ICOMP, FunctionalUnitType.IntALU)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.HI));
		}

		public override void Execute (Thread thread)
		{
			ulong rs = 0;
			ulong rt = 0;
			
			rs = thread.Regs.IntRegs[this[BitField.RS]];
			rt = thread.Regs.IntRegs[this[BitField.RT]];
			
			ulong val = rs * rt;
			
			uint lo = (uint)BitUtils.Bits64 (val, 31, 0);
			uint hi = (uint)BitUtils.Bits64 (val, 63, 32);
			
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

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FD]));
		}
	}

	public abstract class FloatUnaryOp : FloatOp
	{
		public FloatUnaryOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FD]));
		}
	}

	public class Add_d : FloatBinaryOp
	{
		public Add_d (MachInst machInst) : base("add_d", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatADD)
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

	public class Sub_d : FloatBinaryOp
	{
		public Sub_d (MachInst machInst) : base("sub_d", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatADD)
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

	public class Mul_d : FloatBinaryOp
	{
		public Mul_d (MachInst machInst) : base("mul_d", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatMULT)
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

	public class Div_d : FloatBinaryOp
	{
		public Div_d (MachInst machInst) : base("div_d", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatDIV)
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

	public class Sqrt_d : FloatUnaryOp
	{
		public Sqrt_d (MachInst machInst) : base("sqrt_d", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatSQRT)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = Math.Sqrt (fs);
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public class Abs_d : FloatUnaryOp
	{
		public Abs_d (MachInst machInst) : base("abs_d", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatCMP)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = Math.Abs (fs);
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public class Neg_d : FloatUnaryOp
	{
		public Neg_d (MachInst machInst) : base("neg_d", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatCMP)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = -1 * fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public class Mov_d : FloatUnaryOp
	{
		public Mov_d (MachInst machInst) : base("mov_d", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void Execute (Thread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double fd = fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public class Add_s : FloatBinaryOp
	{
		public Add_s (MachInst machInst) : base("add_s", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatADD)
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

	public class Sub_s : FloatBinaryOp
	{
		public Sub_s (MachInst machInst) : base("sub_s", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatADD)
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

	public class Mul_s : FloatBinaryOp
	{
		public Mul_s (MachInst machInst) : base("mul_s", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatMULT)
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

	public class Div_s : FloatBinaryOp
	{
		public Div_s (MachInst machInst) : base("div_s", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatDIV)
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

	public class Sqrt_s : FloatUnaryOp
	{
		public Sqrt_s (MachInst machInst) : base("sqrt_s", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatSQRT)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = (float)Math.Sqrt (fs);
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public class Abs_s : FloatUnaryOp
	{
		public Abs_s (MachInst machInst) : base("abs_s", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatCMP)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = Math.Abs (fs);
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public class Neg_s : FloatUnaryOp
	{
		public Neg_s (MachInst machInst) : base("neg_s", machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatCMP)
		{
		}

		public override void Execute (Thread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = -fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public class Mov_s : FloatUnaryOp
	{
		public Mov_s (MachInst machInst) : base("mov_s", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
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
		public FloatConvertOp (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatCVT)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FD]));
		}
	}

	public class Cvt_d_s : FloatConvertOp
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

	public class Cvt_w_s : FloatConvertOp
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

	public class Cvt_l_s : FloatConvertOp
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

	public class Cvt_s_d : FloatConvertOp
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

	public class Cvt_w_d : FloatConvertOp
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

	public class Cvt_l_d : FloatConvertOp
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

	public class Cvt_s_w : FloatConvertOp
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

	public class Cvt_d_w : FloatConvertOp
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

	public class Cvt_s_l : FloatConvertOp
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

	public class Cvt_d_l : FloatConvertOp
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

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FT]));
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.FCSR));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.FCSR));
		}
	}

	public abstract class C_cond_d : FloatCompareOp
	{
		public C_cond_d (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatCMP)
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
				BitUtils.SetFCC (ref fcsr, (int)this[BitField.CC]);
			} else {
				BitUtils.ClearFCC (ref fcsr, (int)this[BitField.CC]);
			}
			
			thread.Regs.MiscRegs.Fcsr = fcsr;
		}
	}

	public class C_cond_s : FloatCompareOp
	{
		public C_cond_s (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.FCOMP, FunctionalUnitType.FloatCMP)
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
				BitUtils.SetFCC (ref fcsr, (int)this[BitField.CC]);
			} else {
				BitUtils.ClearFCC (ref fcsr, (int)this[BitField.CC]);
			}
			
			thread.Regs.MiscRegs.Fcsr = fcsr;
		}
	}

	public class C_f_d : C_cond_d
	{
		public C_f_d (MachInst machInst) : base("c_f_d", machInst)
		{
		}
	}

	public class C_un_d : C_cond_d
	{
		public C_un_d (MachInst machInst) : base("c_un_d", machInst)
		{
		}
	}

	public class C_eq_d : C_cond_d
	{
		public C_eq_d (MachInst machInst) : base("c_eq_d", machInst)
		{
		}
	}

	public class C_ueq_d : C_cond_d
	{
		public C_ueq_d (MachInst machInst) : base("c_ueq_d", machInst)
		{
		}
	}

	public class C_olt_d : C_cond_d
	{
		public C_olt_d (MachInst machInst) : base("c_olt_d", machInst)
		{
		}
	}

	public class C_ult_d : C_cond_d
	{
		public C_ult_d (MachInst machInst) : base("c_ult_d", machInst)
		{
		}
	}

	public class C_ole_d : C_cond_d
	{
		public C_ole_d (MachInst machInst) : base("c_ole_d", machInst)
		{
		}
	}

	public class C_ule_d : C_cond_d
	{
		public C_ule_d (MachInst machInst) : base("c_ule_d", machInst)
		{
		}
	}

	public class C_sf_d : C_cond_d
	{
		public C_sf_d (MachInst machInst) : base("c_sf_d", machInst)
		{
		}
	}

	public class C_ngle_d : C_cond_d
	{
		public C_ngle_d (MachInst machInst) : base("c_ngle_d", machInst)
		{
		}
	}

	public class C_seq_d : C_cond_d
	{
		public C_seq_d (MachInst machInst) : base("c_seq_d", machInst)
		{
		}
	}

	public class C_ngl_d : C_cond_d
	{
		public C_ngl_d (MachInst machInst) : base("c_ngl_d", machInst)
		{
		}
	}

	public class C_lt_d : C_cond_d
	{
		public C_lt_d (MachInst machInst) : base("c_lt_d", machInst)
		{
		}
	}

	public class C_nge_d : C_cond_d
	{
		public C_nge_d (MachInst machInst) : base("c_nge_d", machInst)
		{
		}
	}

	public class C_le_d : C_cond_d
	{
		public C_le_d (MachInst machInst) : base("c_le_d", machInst)
		{
		}
	}

	public class C_ngt_d : C_cond_d
	{
		public C_ngt_d (MachInst machInst) : base("c_ngt_d", machInst)
		{
		}
	}

	public class C_f_s : C_cond_s
	{
		public C_f_s (MachInst machInst) : base("c_f_s", machInst)
		{
		}
	}

	public class C_un_s : C_cond_s
	{
		public C_un_s (MachInst machInst) : base("c_un_s", machInst)
		{
		}
	}

	public class C_eq_s : C_cond_s
	{
		public C_eq_s (MachInst machInst) : base("c_eq_s", machInst)
		{
		}
	}

	public class C_ueq_s : C_cond_s
	{
		public C_ueq_s (MachInst machInst) : base("c_ueq_s", machInst)
		{
		}
	}

	public class C_olt_s : C_cond_s
	{
		public C_olt_s (MachInst machInst) : base("c_olt_s", machInst)
		{
		}
	}

	public class C_ult_s : C_cond_s
	{
		public C_ult_s (MachInst machInst) : base("c_ult_s", machInst)
		{
		}
	}

	public class C_ole_s : C_cond_s
	{
		public C_ole_s (MachInst machInst) : base("c_ole_s", machInst)
		{
		}
	}

	public class C_ule_s : C_cond_s
	{
		public C_ule_s (MachInst machInst) : base("c_ule_s", machInst)
		{
		}
	}

	public class C_sf_s : C_cond_s
	{
		public C_sf_s (MachInst machInst) : base("c_sf_s", machInst)
		{
		}
	}

	public class C_ngle_s : C_cond_s
	{
		public C_ngle_s (MachInst machInst) : base("c_ngle_s", machInst)
		{
		}
	}

	public class C_seq_s : C_cond_s
	{
		public C_seq_s (MachInst machInst) : base("c_seq_s", machInst)
		{
		}
	}

	public class C_ngl_s : C_cond_s
	{
		public C_ngl_s (MachInst machInst) : base("c_ngl_s", machInst)
		{
		}
	}

	public class C_lt_s : C_cond_s
	{
		public C_lt_s (MachInst machInst) : base("c_lt_s", machInst)
		{
		}
	}

	public class C_nge_s : C_cond_s
	{
		public C_nge_s (MachInst machInst) : base("c_nge_s", machInst)
		{
		}
	}

	public class C_le_s : C_cond_s
	{
		public C_le_s (MachInst machInst) : base("c_le_s", machInst)
		{
		}
	}

	public class C_ngt_s : C_cond_s
	{
		public C_ngt_s (MachInst machInst) : base("c_ngt_s", machInst)
		{
		}
	}

	public abstract class MemoryOp : StaticInst
	{
		public MemoryOp (string mnemonic, MachInst machInst, StaticInstFlag flags, FunctionalUnitType fuType) : base(mnemonic, machInst, flags, fuType)
		{
			this.displacement = BitUtils.Sext (machInst[BitField.OFFSET], 16);
		}

		public virtual uint Ea (Thread thread)
		{
			uint ea = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			return ea;
		}

		public override void SetupDeps ()
		{
			this.MemIDeps = new List<RegisterDependency> ();
			this.MemODeps = new List<RegisterDependency> ();
			
			this.setupEaDeps ();
			
			this.eaODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.EA));
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.EA));
			
			this.setupMemDeps ();
		}

		public abstract void setupEaDeps ();
		public abstract void setupMemDeps ();

		public List<RegisterDependency> EaIdeps {
			get { return this.IDeps; }
			set { this.IDeps = value; }
		}

		public List<RegisterDependency> eaODeps {
			get { return this.ODeps; }
			set { this.ODeps = value; }
		}

		public List<RegisterDependency> MemIDeps;
		public List<RegisterDependency> MemODeps;

		public int displacement { get; private set; }
	}

	public class Lb : MemoryOp
	{
		public Lb (MachInst machInst) : base("lb", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			byte mem = 0;
			thread.Mem.ReadByte (this.Ea (thread), (byte*)&mem);
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public class Lbu : MemoryOp
	{
		public Lbu (MachInst machInst) : base("lbu", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			byte mem = 0;
			thread.Mem.ReadByte (this.Ea (thread), &mem);
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public class Lh : MemoryOp
	{
		public Lh (MachInst machInst) : base("lh", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			short mem = 0;
			thread.Mem.ReadHalfWord (this.Ea (thread), (ushort*)&mem);
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)mem;
		}
	}

	public class Lhu : MemoryOp
	{
		public Lhu (MachInst machInst) : base("lhu", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			ushort mem = 0;
			thread.Mem.ReadHalfWord (this.Ea (thread), &mem);
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public class Lw : MemoryOp
	{
		public Lw (MachInst machInst) : base("lw", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			int mem = 0;
			thread.Mem.ReadWord (this.Ea (thread), (uint*)&mem);
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)mem;
		}
	}

	public class Lwl : MemoryOp
	{
		public Lwl (MachInst machInst) : base("lwl", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override uint Ea (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		unsafe public override void Execute (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = 0;
			
			thread.Mem.ReadWord (ea, &mem);
			
			uint mem_shift = 24 - 8 * byte_offset;
			
			uint rt = (mem << (int)mem_shift) | (thread.Regs.IntRegs[this[BitField.RT]] & BitUtils.Mask ((int)mem_shift));
			
			thread.Regs.IntRegs[this[BitField.RT]] = rt;
		}
	}

	public class Lwr : MemoryOp
	{
		public Lwr (MachInst machInst) : base("lwr", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override uint Ea (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		unsafe public override void Execute (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = 0;
			
			thread.Mem.ReadWord (ea, &mem);
			
			uint mem_shift = 8 * byte_offset;
			
			uint rt = (thread.Regs.IntRegs[this[BitField.RT]] & (BitUtils.Mask ((int)mem_shift) << (int)(32 - mem_shift))) | (mem >> (int)mem_shift);
			
			thread.Regs.IntRegs[this[BitField.RT]] = rt;
		}
	}

	public class Ll : MemoryOp
	{
		public Ll (MachInst machInst) : base("ll", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			uint mem = 0;
			thread.Mem.ReadWord (this.Ea (thread), &mem);
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public class Lwc1 : MemoryOp
	{
		public Lwc1 (MachInst machInst) : base("lwc1", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			uint mem = 0;
			thread.Mem.ReadWord (this.Ea (thread), &mem);
			thread.Regs.FloatRegs.SetUint (mem, this[BitField.FT]);
		}
	}

	public class Ldc1 : MemoryOp
	{
		public Ldc1 (MachInst machInst) : base("ldc1", machInst, StaticInstFlag.MEM | StaticInstFlag.LOAD | StaticInstFlag.DISP, FunctionalUnitType.RdPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FT]));
		}

		unsafe public override void Execute (Thread thread)
		{
			ulong mem = 0;
			thread.Mem.ReadDoubleWord (this.Ea (thread), &mem);
			thread.Regs.FloatRegs.SetUlong (mem, this[BitField.FT]);
		}
	}

	public class Sb : MemoryOp
	{
		public Sb (MachInst machInst) : base("sb", machInst, StaticInstFlag.MEM | StaticInstFlag.STORE | StaticInstFlag.DISP, FunctionalUnitType.WrPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			byte mem = (byte)BitUtils.Bits (thread.Regs.IntRegs[this[BitField.RT]], 7, 0);
			thread.Mem.WriteByte (this.Ea (thread), mem);
		}
	}

	public class Sh : MemoryOp
	{
		public Sh (MachInst machInst) : base("sh", machInst, StaticInstFlag.MEM | StaticInstFlag.STORE | StaticInstFlag.DISP, FunctionalUnitType.WrPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			ushort mem = (ushort)BitUtils.Bits (thread.Regs.IntRegs[this[BitField.RT]], 15, 0);
			thread.Mem.WriteHalfWord (this.Ea (thread), mem);
		}
	}

	public class Sw : MemoryOp
	{
		public Sw (MachInst machInst) : base("sw", machInst, StaticInstFlag.MEM | StaticInstFlag.STORE | StaticInstFlag.DISP, FunctionalUnitType.WrPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			uint mem = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Mem.WriteWord (this.Ea (thread), mem);
		}
	}

	public class Swl : MemoryOp
	{
		public Swl (MachInst machInst) : base("swl", machInst, StaticInstFlag.MEM | StaticInstFlag.STORE | StaticInstFlag.DISP, FunctionalUnitType.WrPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override uint Ea (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		unsafe public override void Execute (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = 0;
			
			thread.Mem.ReadWord (ea, &mem);
			
			uint reg_shift = 24 - 8 * byte_offset;
			uint mem_shift = 32 - reg_shift;
			
			mem = (mem & (BitUtils.Mask ((int)reg_shift) << (int)mem_shift)) | (thread.Regs.IntRegs[this[BitField.RT]] >> (int)reg_shift);
			
			thread.Mem.WriteWord (ea, mem);
		}
	}

	public class Swr : MemoryOp
	{
		public Swr (MachInst machInst) : base("swr", machInst, StaticInstFlag.MEM | StaticInstFlag.STORE | StaticInstFlag.DISP, FunctionalUnitType.WrPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override uint Ea (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		unsafe public override void Execute (Thread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = 0;
			
			thread.Mem.ReadWord (ea, &mem);
			
			uint reg_shift = 8 * byte_offset;
			
			mem = thread.Regs.IntRegs[this[BitField.RT]] << (int)reg_shift | (mem & (BitUtils.Mask ((int)reg_shift)));
			
			thread.Mem.WriteWord (ea, mem);
		}
	}

	public class Sc : MemoryOp
	{
		public Sc (MachInst machInst) : base("sc", machInst, StaticInstFlag.MEM | StaticInstFlag.STORE | StaticInstFlag.DISP, FunctionalUnitType.WrPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void setupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Mem.WriteWord (this.Ea (thread), rt);
			thread.Regs.IntRegs[this[BitField.RT]] = 1;
		}
	}

	public class Swc1 : MemoryOp
	{
		public Swc1 (MachInst machInst) : base("swc1", machInst, StaticInstFlag.MEM | StaticInstFlag.STORE | StaticInstFlag.DISP, FunctionalUnitType.WrPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FT]));
		}

		public override void Execute (Thread thread)
		{
			uint ft = thread.Regs.FloatRegs.GetUint (this[BitField.FT]);
			thread.Mem.WriteWord (this.Ea (thread), ft);
		}
	}

	public class Sdc1 : MemoryOp
	{
		public Sdc1 (MachInst machInst) : base("sdc1", machInst, StaticInstFlag.MEM | StaticInstFlag.STORE | StaticInstFlag.DISP, FunctionalUnitType.WrPort)
		{
		}

		public override void setupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RS]));
		}

		public override void setupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FT]));
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

	public class Mfc1 : CP1Control
	{
		public Mfc1 (MachInst machInst) : base("mfc1", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
		}

		public override void Execute (Thread thread)
		{
			uint fs = thread.Regs.FloatRegs.GetUint (this[BitField.FS]);
			thread.Regs.IntRegs[this[BitField.RT]] = fs;
		}
	}

	public class Cfc1 : CP1Control
	{
		public Cfc1 (MachInst machInst) : base("cfc1", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.FCSR));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
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

	public class Mtc1 : CP1Control
	{
		public Mtc1 (MachInst machInst) : base("mtc1", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.FP, this[BitField.FS]));
		}

		public override void Execute (Thread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Regs.FloatRegs.SetUint (rt, this[BitField.FS]);
		}
	}

	public class Ctc1 : CP1Control
	{
		public Ctc1 (MachInst machInst) : base("ctc1", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependencyType.INT, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependencyType.MISC, (uint)MiscRegNums.FCSR));
		}

		public override void Execute (Thread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			
			if (this[BitField.FS] != 0) {
				thread.Regs.MiscRegs.Fcsr = rt;
			}
		}
	}

	public class Nop : StaticInst
	{
		public Nop (MachInst machInst) : base("nop", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
		}
	}

	public class FailUnimplemented : StaticInst
	{
		public FailUnimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.invoke (thread);
		}
	}

	public class CP0Unimplemented : StaticInst
	{
		public CP0Unimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.invoke (thread);
		}
	}

	public class CP1Unimplemented : StaticInst
	{
		public CP1Unimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.invoke (thread);
		}
	}

	public class CP2Unimplemented : StaticInst
	{
		public CP2Unimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.invoke (thread);
		}
	}

	public class WarnUnimplemented : StaticInst
	{
		public WarnUnimplemented (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			Fault fault = new UnimplFault (string.Format ("[{0:s}] machInst: 0x{1:x8}, mnemonic: \"{2:s}\"", this, this.MachInst.Data, this.Mnemonic));
			fault.invoke (thread);
		}
	}

	public class Unknown : StaticInst
	{
		public Unknown (MachInst machInst) : base("unknown", machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
		}

		public override void Execute (Thread thread)
		{
			new ReservedInstructionFault ();
		}
	}

	public abstract class Trap : StaticInst
	{
		public Trap (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
		}

		public override void SetupDeps ()
		{
		}
	}

	public abstract class TrapImm : StaticInst
	{
		public TrapImm (string mnemonic, MachInst machInst) : base(mnemonic, machInst, StaticInstFlag.NONE, FunctionalUnitType.NONE)
		{
			this.imm = (short)machInst[BitField.INTIMM];
		}

		public override void SetupDeps ()
		{
		}

		protected short imm;
	}

	public abstract class Fault
	{
		public Fault ()
		{
		}

		public abstract string getName ();

		public virtual void invoke (Thread thread)
		{
			Logger.Panicf (LogCategory.INSTRUCTION, "fault ({0:s}) detected @ PC 0x{1:x8}", this.getName (), thread.Regs.Pc);
		}
	}

	public class UnimplFault : Fault
	{
		public UnimplFault (string text)
		{
			this.text = text;
		}

		public override string getName ()
		{
			return "Unimplemented simulator feature";
		}

		public override void invoke (Thread thread)
		{
			Logger.Panicf (LogCategory.INSTRUCTION, "UnimplFault ({0:s})\n", this.text);
		}

		private string text;
	}

	public class ReservedInstructionFault : Fault
	{
		public ReservedInstructionFault ()
		{
		}

		public override string getName ()
		{
			return "Reserved Instruction Fault";
		}

		public override void invoke (Thread thread)
		{
			Logger.Panicf (LogCategory.INSTRUCTION, "ReservedInstructionFault ({0:s})\n", this.getName ());
		}
	}
}
