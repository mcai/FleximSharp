/*
 * Architecture.cs
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
using System.Runtime.InteropServices;
using System.Text;
using MinCai.Simulators.Flexim.Architecture;
using MinCai.Simulators.Flexim.Architecture.Instructions;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.Microarchitecture;
using MinCai.Simulators.Flexim.Interop;

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

	public sealed class MachineInstruction
	{
		public enum Types
		{
			R,
			I,
			J,
			F
		}

		public MachineInstruction (uint data)
		{
			this.Data = data;
		}

		public override string ToString ()
		{
			return string.Format ("[MachineInstruction: Data=0x{0:x8}]", this.Data);
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

		public Types MachineInstructionType {
			get {
				uint opcode = this[BitField.OPCODE];
				
				if (opcode == 0)
					return Types.R; else if ((opcode == 0x02) || (opcode == 0x03))
					return Types.J; else if (opcode == 0x11)
					return Types.F;
				else
					return Types.I;
			}
		}

		public uint Data { get; private set; }
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

		public static uint MISC_REG_LO = 0;
		public static uint MISC_REG_HI = 1;
		public static uint MISC_REG_EA = 2;
		public static uint MISC_REG_FCSR = 3;
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
//				Logger.Infof(Logger.Categories.THREAD, "    Reading int reg {0:d} as 0x{1:x8}.", index, val);
				return val;
			}
			set {
				Debug.Assert (index < RegisterConstants.NUM_INT_REGS);
//				Logger.Infof(Logger.Categories.THREAD, "    Setting int reg {0:d} to 0x{1:x8}.", index, value);
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
//			Logger.Infof(Logger.Categories.REGISTER, "    Reading float reg {0:d} as {1:f}.", index, val);
			return val;
		}

		public void SetFloat (float val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			this.Regs.f[index] = val;
//			Logger.Infof(Logger.Categories.REGISTER, "    Setting float reg {0:d} to {1:f}.", index, val);
		}

		public double GetDouble (uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			double val = this.Regs.d[index / 2];
//			Logger.Infof(Logger.Categories.REGISTER, "    Reading double reg {0:d} as {1:f}.", index, val);
			return val;
		}

		public void SetDouble (double val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			this.Regs.d[index / 2] = val;
//			Logger.Infof(Logger.Categories.REGISTER, "    Setting double reg {0:d} to {1:f}.", index, val);
		}

		public uint GetUint (uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			uint val = (uint)(this.Regs.i[index]);
//			Logger.Infof(Logger.Categories.REGISTER, "    Reading float reg {0:d} bits as 0x{1:x8}.", index, val);
			return val;
		}

		public void SetUint (uint val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			this.Regs.i[index] = (int)val;
//			Logger.Infof(Logger.Categories.REGISTER, "    Setting float reg (0:d} bits to 0x{1:x8}.", index, val);
		}

		public ulong GetUlong (uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			ulong val = (ulong)(this.Regs.l[index / 2]);
//			Logger.Infof(Logger.Categories.REGISTER, "    Reading double reg {0:d} bits as 0x{1:x8}.", index, val);
			return val;
		}

		public void SetUlong (ulong val, uint index)
		{
			Debug.Assert (index < RegisterConstants.NUM_FLOAT_REGS);
			
			this.Regs.l[index / 2] = (long)val;
//			Logger.Infof(Logger.Categories.REGISTER, "    Setting double reg {0:d} bits to 0x{1:x8}.", index, val);
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
			this.DecodedStaticInstructions = new Dictionary<uint, StaticInstruction> ();
		}

		public StaticInstruction Decode (uint pc, Memory mem)
		{
			if (this.DecodedStaticInstructions.ContainsKey (pc)) {
				return this.DecodedStaticInstructions[pc];
			} else {
				uint data = mem.ReadWord(pc);
				
				MachineInstruction machineInstruction = new MachineInstruction (data);
				
				StaticInstruction staticInstruction = this.DecodeMachineInstruction (machineInstruction);
				
				this.DecodedStaticInstructions[pc] = staticInstruction;
				
				return staticInstruction;
			}
		}

		public abstract StaticInstruction DecodeMachineInstruction (MachineInstruction machineInstruction);

		private Dictionary<uint, StaticInstruction> DecodedStaticInstructions { get; set; }
	}

	public sealed class RegisterDependency
	{
		public enum Types
		{
			Integer,
			Float,
			Misc
		}

		public RegisterDependency (Types type, uint num)
		{
			this.Type = type;
			this.Num = num;
		}

		public override string ToString ()
		{
			return string.Format ("[RegisterDependency: Type={0}, Num={1}]", this.Type, this.Num);
		}

		public Types Type { get; private set; }
		public uint Num { get; private set; }
	}

	public abstract class StaticInstruction
	{
		[Flags]
		public enum Flag : uint
		{
			None = 0x00000000,
			IntegerComputation = 0x00000001,
			FloatComputation = 0x00000002,
			Control = 0x00000004,
			UnconditionalBranch = 0x00000008,
			ConditionalBranch = 0x00000010,
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
			FunctionCall = 0x00008000,
			FloatConditional = 0x00010000,
			Immediate = 0x00020000,
			FunctionReturn = 0x00040000
		}

		public StaticInstruction (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType)
		{
			this.Mnemonic = mnemonic;
			this.MachineInstruction = machineInstruction;
			this.Flags = flags;
			this.FunctionalUnitType = fuType;
			
			this.IDeps = new List<RegisterDependency> ();
			this.ODeps = new List<RegisterDependency> ();
			
			this.SetupDeps ();
		}

		public virtual uint GetTargetPc (IThread thread)
		{
			throw new NotImplementedException ();
		}

		protected abstract void SetupDeps ();

		public abstract void Execute (IThread thread);

		public override string ToString ()
		{
			return string.Format ("[StaticInstruction: MachineInstruction={0}, Mnemonic={1}, Flags={2}, FunctionalUnitType={3}]", this.MachineInstruction, this.Mnemonic, this.Flags, this.FunctionalUnitType);
		}

		public uint this[BitField field] {
			get { return this.MachineInstruction[field]; }
		}

		public bool IsLongLatency {
			get { return (this.Flags & Flag.LongLatency) == Flag.LongLatency; }
		}

		public bool IsTrap {
			get { return (this.Flags & Flag.Trap) == Flag.Trap; }
		}

		public bool IsMemory {
			get { return (this.Flags & Flag.Memory) == Flag.Memory; }
		}

		public bool IsLoad {
			get { return this.IsMemory && (this.Flags & Flag.Load) == Flag.Load; }
		}

		public bool IsStore {
			get { return this.IsMemory && (this.Flags & Flag.Store) == Flag.Store; }
		}

		public bool IsConditionalBranch {
			get { return (this.Flags & Flag.ConditionalBranch) == Flag.ConditionalBranch; }
		}

		public bool IsUnconditionalBranch {
			get { return (this.Flags & Flag.UnconditionalBranch) == Flag.UnconditionalBranch; }
		}

		public bool IsDirectJump {
			get { return (this.Flags & Flag.DirectJump) != Flag.DirectJump; }
		}

		public bool IsControl {
			get { return (this.Flags & Flag.Control) == Flag.Control; }
		}

		public bool IsFunctionCall {
			get { return (this.Flags & Flag.FunctionCall) == Flag.FunctionCall; }
		}

		public bool IsFunctionReturn {
			get { return (this.Flags & Flag.FunctionReturn) == Flag.FunctionReturn; }
		}

		public bool IsNop {
			get { return (this as Nop) != null; }
		}

		public List<RegisterDependency> IDeps { get; protected set; }
		public List<RegisterDependency> ODeps { get; protected set; }

		public string Mnemonic { get; private set; }
		public MachineInstruction MachineInstruction { get; private set; }
		public Flag Flags { get; private set; }
		public FunctionalUnit.Types FunctionalUnitType { get; private set; }
	}

	public sealed class DynamicInstruction
	{
		public DynamicInstruction (IThread thread, uint pc, StaticInstruction staticInst)
		{
			this.Thread = thread;
			this.Pc = pc;
			this.StaticInstruction = staticInst;
		}

		public void Execute ()
		{
			this.Thread.Regs.IntRegs[RegisterConstants.ZERO_REG] = 0;
			this.StaticInstruction.Execute (this.Thread);
		}

		public override string ToString ()
		{
			return string.Format ("[DynamicInstruction: Dis={0}, Thread.Name={1}]", Disassemble (this.StaticInstruction.MachineInstruction, this.Pc, this.Thread), this.Thread.Name);
		}

		public uint PhysPc {
			get { return this.Thread.Core.Processor.MMU.GetPhysicalAddress (this.Thread.MemoryMapId, this.Pc); }
		}

		public uint Pc { get; set; }
		public StaticInstruction StaticInstruction { get; set; }
		public IThread Thread { get; set; }

		public static string[] MIPS_GPR_NAMES = { "zero", "at", "v0", "v1", "a0", "a1", "a2", "a3", "t0", "t1",
		"t2", "t3", "t4", "t5", "t6", "t7", "s0", "s1", "s2", "s3",
		"s4", "s5", "s6", "s7", "t8", "t9", "k0", "k1", "gp", "sp",
		"s8", "ra" };

		public static string Disassemble (MachineInstruction machineInstruction, uint pc, IThread thread)
		{
			StringBuilder buf = new StringBuilder ();
			
			buf.AppendFormat ("0x{0:x8} : 0x{1:x8} {2} ", pc, machineInstruction.Data, thread.Core.Isa.DecodeMachineInstruction (machineInstruction).Mnemonic);
			
			if (machineInstruction.Data == 0x00000000) {
				return buf.ToString ();
			}
			
			switch (machineInstruction.MachineInstructionType) {
			case MachineInstruction.Types.J:
				buf.AppendFormat ("0x{0:x8}", machineInstruction[BitField.JMPTARG]);
				break;
			case MachineInstruction.Types.I:
				if (machineInstruction.IsOneOpBranch) {
					buf.AppendFormat ("${0}, {1:d}", MIPS_GPR_NAMES[machineInstruction[BitField.RS]], (short)machineInstruction[BitField.INTIMM]);
				} else if (machineInstruction.IsLoadStore) {
					buf.AppendFormat ("${0}, {1:d}(${2})", MIPS_GPR_NAMES[machineInstruction[BitField.RT]], (short)machineInstruction[BitField.INTIMM], MIPS_GPR_NAMES[machineInstruction[BitField.RS]]);
				} else if (machineInstruction.IsFloatLoadStore) {
					buf.AppendFormat ("$f{0}, {1:d}(${2})", machineInstruction[BitField.FT], (short)machineInstruction[BitField.INTIMM], MIPS_GPR_NAMES[machineInstruction[BitField.RS]]);
				} else {
					buf.AppendFormat ("${0}, ${1}, {2:d}", MIPS_GPR_NAMES[machineInstruction[BitField.RT]], MIPS_GPR_NAMES[machineInstruction[BitField.RS]], (short)machineInstruction[BitField.INTIMM]);
				}
				break;
			case MachineInstruction.Types.F:
				if (machineInstruction.IsConvert) {
					buf.AppendFormat ("$f{0:d}, $f{1:d}", machineInstruction[BitField.FD], machineInstruction[BitField.FS]);
				} else if (machineInstruction.IsCompare) {
					buf.AppendFormat ("{0:d}, $f{1:d}, $f{2:d}", machineInstruction[BitField.FD] >> 2, machineInstruction[BitField.FS], machineInstruction[BitField.FT]);
				} else if (machineInstruction.IsFloatBranch) {
					buf.AppendFormat ("{0:d}, {1:d}", machineInstruction[BitField.FD] >> 2, (short)machineInstruction[BitField.INTIMM]);
				} else if (machineInstruction.IsGPRFloatMove) {
					buf.AppendFormat ("${0}, $f{1:d}", MIPS_GPR_NAMES[machineInstruction[BitField.RT]], machineInstruction[BitField.FS]);
				} else if (machineInstruction.IsGPRFCRMove) {
					buf.AppendFormat ("${0}, ${1:d}", MIPS_GPR_NAMES[machineInstruction[BitField.RT]], machineInstruction[BitField.FS]);
				} else {
					buf.AppendFormat ("$f{0:d}, $f{1:d}, $f{2:d}", machineInstruction[BitField.FD], machineInstruction[BitField.FS], machineInstruction[BitField.FT]);
				}
				break;
			case MachineInstruction.Types.R:
				if (machineInstruction.IsSyscall) {
				} else if (machineInstruction.IsShift) {
					buf.AppendFormat ("${0}, ${1}, {2:d}", MIPS_GPR_NAMES[machineInstruction[BitField.RD]], MIPS_GPR_NAMES[machineInstruction[BitField.RT]], machineInstruction[BitField.SA]);
				} else if (machineInstruction.IsROneOp) {
					buf.AppendFormat ("${0}", MIPS_GPR_NAMES[machineInstruction[BitField.RS]]);
				} else if (machineInstruction.IsRTwoOp) {
					buf.AppendFormat ("${0}, ${1}", MIPS_GPR_NAMES[machineInstruction[BitField.RS]], MIPS_GPR_NAMES[machineInstruction[BitField.RT]]);
				} else if (machineInstruction.IsRMt) {
					buf.AppendFormat ("${0}", MIPS_GPR_NAMES[machineInstruction[BitField.RS]]);
				} else if (machineInstruction.IsRMf) {
					buf.AppendFormat ("${0}", MIPS_GPR_NAMES[machineInstruction[BitField.RD]]);
				} else {
					buf.AppendFormat ("${0}, ${1}, ${2}", MIPS_GPR_NAMES[machineInstruction[BitField.RD]], MIPS_GPR_NAMES[machineInstruction[BitField.RS]], MIPS_GPR_NAMES[machineInstruction[BitField.RT]]);
				}
				break;
			default:
				Logger.Fatal (Logger.Categories.Instruction, "you can not reach here");
				break;
			}
			
			return buf.ToString ();
		}
	}

	public sealed partial class Mips32InstructionSetArchitecture : InstructionSetArchitecture
	{
		public Mips32InstructionSetArchitecture ()
		{
		}
	}
}
