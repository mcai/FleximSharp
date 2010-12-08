/*
 * Instructions.cs
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
	public sealed partial class Mips32InstructionSetArchitecture : InstructionSetArchitecture
	{
		public override StaticInstruction DecodeMachineInstruction (MachineInstruction machineInstruction)
		{
			switch (machineInstruction[BitField.OPCODE_HI]) {
			case 0x0:
				switch (machineInstruction[BitField.OPCODE_LO]) {
				case 0x0:
					switch (machineInstruction[BitField.FUNC_HI]) {
					case 0x0:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x1:
							switch (machineInstruction[BitField.MOVCI]) {
							case 0x0:
								return new FailUnimplemented ("Movf", machineInstruction);
							case 0x1:
								return new FailUnimplemented ("Movt", machineInstruction);
							default:
								return new Unknown (machineInstruction);
							}
						case 0x0:
							switch (machineInstruction[BitField.RS]) {
							case 0x0:
								switch (machineInstruction[BitField.RT_RD]) {
								case 0x0:
									switch (machineInstruction[BitField.SA]) {
									case 0x1:
										return new FailUnimplemented ("Ssnop", machineInstruction);
									case 0x3:
										return new FailUnimplemented ("Ehb", machineInstruction);
									default:
										return new Nop (machineInstruction);
									}
								default:
									return new Sll (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0x2:
							switch (machineInstruction[BitField.RS_SRL]) {
							case 0x0:
								switch (machineInstruction[BitField.SRL]) {
								case 0x0:
									return new Srl (machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Rotr", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0x3:
							switch (machineInstruction[BitField.RS]) {
							case 0x0:
								return new Sra (machineInstruction);
							default:
								return new Unknown (machineInstruction);
							}
						case 0x4:
							return new Sllv (machineInstruction);
						case 0x6:
							switch (machineInstruction[BitField.SRLV]) {
							case 0x0:
								return new Srlv (machineInstruction);
							case 0x1:
								return new FailUnimplemented ("Rotrv", machineInstruction);
							default:
								return new Unknown (machineInstruction);
							}
						case 0x7:
							return new Srav (machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x1:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							switch (machineInstruction[BitField.HINT]) {
							case 0x1:
								return new FailUnimplemented ("Jr_hb", machineInstruction);
							default:
								return new Jr (machineInstruction);
							}
						case 0x1:
							switch (machineInstruction[BitField.HINT]) {
							case 0x1:
								return new FailUnimplemented ("Jalr_hb", machineInstruction);
							default:
								return new Jalr (machineInstruction);
							}
						case 0x2:
							return new FailUnimplemented ("Movz", machineInstruction);
						case 0x3:
							return new FailUnimplemented ("Movn", machineInstruction);
						case 0x4:
							return new Syscall (machineInstruction);
						case 0x7:
							return new FailUnimplemented ("Sync", machineInstruction);
						case 0x5:
							return new FailUnimplemented ("Break", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x2:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new Mfhi (machineInstruction);
						case 0x1:
							return new Mthi (machineInstruction);
						case 0x2:
							return new Mflo (machineInstruction);
						case 0x3:
							return new Mtlo (machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x3:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new Mult (machineInstruction);
						case 0x1:
							return new Multu (machineInstruction);
						case 0x2:
							return new Div (machineInstruction);
						case 0x3:
							return new Divu (machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x4:
						switch (machineInstruction[BitField.HINT]) {
						case 0x0:
							switch (machineInstruction[BitField.FUNC_LO]) {
							case 0x0:
								return new Add (machineInstruction);
							case 0x1:
								return new Addu (machineInstruction);
							case 0x2:
								return new Sub (machineInstruction);
							case 0x3:
								return new Subu (machineInstruction);
							case 0x4:
								return new And (machineInstruction);
							case 0x5:
								return new Or (machineInstruction);
							case 0x6:
								return new Xor (machineInstruction);
							case 0x7:
								return new Nor (machineInstruction);
							default:
								return new Unknown (machineInstruction);
							}
						default:
							return new Unknown (machineInstruction);
						}
					case 0x5:
						switch (machineInstruction[BitField.HINT]) {
						case 0x0:
							switch (machineInstruction[BitField.FUNC_LO]) {
							case 0x2:
								return new Slt (machineInstruction);
							case 0x3:
								return new Sltu (machineInstruction);
							default:
								return new Unknown (machineInstruction);
							}
						default:
							return new Unknown (machineInstruction);
						}
					case 0x6:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Tge", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Tgeu", machineInstruction);
						case 0x2:
							return new FailUnimplemented ("Tlt", machineInstruction);
						case 0x3:
							return new FailUnimplemented ("Tltu", machineInstruction);
						case 0x4:
							return new FailUnimplemented ("Teq", machineInstruction);
						case 0x6:
							return new FailUnimplemented ("Tne", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					default:
						return new Unknown (machineInstruction);
					}
				case 0x1:
					switch (machineInstruction[BitField.REGIMM_HI]) {
					case 0x0:
						switch (machineInstruction[BitField.REGIMM_LO]) {
						case 0x0:
							return new Bltz (machineInstruction);
						case 0x1:
							return new Bgez (machineInstruction);
						case 0x2:
							return new FailUnimplemented ("Bltzl", machineInstruction);
						case 0x3:
							return new FailUnimplemented ("Bgezl", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x1:
						switch (machineInstruction[BitField.REGIMM_LO]) {
						case 0x0:
							return new FailUnimplemented ("Tgei", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Tgeiu", machineInstruction);
						case 0x2:
							return new FailUnimplemented ("Tlti", machineInstruction);
						case 0x3:
							return new FailUnimplemented ("Tltiu", machineInstruction);
						case 0x4:
							return new FailUnimplemented ("Teqi", machineInstruction);
						case 0x6:
							return new FailUnimplemented ("Tnei", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x2:
						switch (machineInstruction[BitField.REGIMM_LO]) {
						case 0x0:
							return new Bltzal (machineInstruction);
						case 0x1:
							switch (machineInstruction[BitField.RS]) {
							case 0x0:
								return new Bal (machineInstruction);
							default:
								return new Bgezal (machineInstruction);
							}
						case 0x2:
							return new FailUnimplemented ("Bltzall", machineInstruction);
						case 0x3:
							return new FailUnimplemented ("Bgezall", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x3:
						switch (machineInstruction[BitField.REGIMM_LO]) {
						case 0x4:
							return new FailUnimplemented ("Bposge32", machineInstruction);
						case 0x7:
							return new FailUnimplemented ("WarnUnimplemented.synci", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					default:
						return new Unknown (machineInstruction);
					}
				case 0x2:
					return new J (machineInstruction);
				case 0x3:
					return new Jal (machineInstruction);
				case 0x4:
					switch (machineInstruction[BitField.RS_RT]) {
					case 0x0:
						return new B (machineInstruction);
					default:
						return new Beq (machineInstruction);
					}
				case 0x5:
					return new Bne (machineInstruction);
				case 0x6:
					return new Blez (machineInstruction);
				case 0x7:
					return new Bgtz (machineInstruction);
				default:
					return new Unknown (machineInstruction);
				}
			case 0x1:
				switch (machineInstruction[BitField.OPCODE_LO]) {
				case 0x0:
					return new Addi (machineInstruction);
				case 0x1:
					return new Addiu (machineInstruction);
				case 0x2:
					return new Slti (machineInstruction);
				case 0x3:
					switch (machineInstruction[BitField.RS_RT_INTIMM]) {
					case 0xabc1:
						return new FailUnimplemented ("Fail", machineInstruction);
					case 0xabc2:
						return new FailUnimplemented ("Pass", machineInstruction);
					default:
						return new Sltiu (machineInstruction);
					}
				case 0x4:
					return new Andi (machineInstruction);
				case 0x5:
					return new Ori (machineInstruction);
				case 0x6:
					return new Xori (machineInstruction);
				case 0x7:
					switch (machineInstruction[BitField.RS]) {
					case 0x0:
						return new Lui (machineInstruction);
					default:
						return new Unknown (machineInstruction);
					}
				default:
					return new Unknown (machineInstruction);
				}
			case 0x2:
				switch (machineInstruction[BitField.OPCODE_LO]) {
				case 0x0:
					switch (machineInstruction[BitField.RS_MSB]) {
					case 0x0:
						switch (machineInstruction[BitField.RS]) {
						case 0x0:
							return new FailUnimplemented ("Mfc0", machineInstruction);
						case 0x4:
							return new FailUnimplemented ("Mtc0", machineInstruction);
						case 0x1:
							return new CP0Unimplemented ("dmfc0", machineInstruction);
						case 0x5:
							return new CP0Unimplemented ("dmtc0", machineInstruction);
						default:
							return new CP0Unimplemented ("unknown", machineInstruction);
						case 0x8:
							switch (machineInstruction[BitField.MT_U]) {
							case 0x0:
								return new FailUnimplemented ("Mftc0", machineInstruction);
							case 0x1:
								switch (machineInstruction[BitField.SEL]) {
								case 0x0:
									return new FailUnimplemented ("Mftgpr", machineInstruction);
								case 0x1:
									switch (machineInstruction[BitField.RT]) {
									case 0x0:
										return new FailUnimplemented ("Mftlo_dsp0", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("Mfthi_dsp0", machineInstruction);
									case 0x2:
										return new FailUnimplemented ("Mftacx_dsp0", machineInstruction);
									case 0x4:
										return new FailUnimplemented ("Mftlo_dsp1", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("Mfthi_dsp1", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Mftacx_dsp1", machineInstruction);
									case 0x8:
										return new FailUnimplemented ("Mftlo_dsp2", machineInstruction);
									case 0x9:
										return new FailUnimplemented ("Mfthi_dsp2", machineInstruction);
									case 0x10:
										return new FailUnimplemented ("Mftacx_dsp2", machineInstruction);
									case 0x12:
										return new FailUnimplemented ("Mftlo_dsp3", machineInstruction);
									case 0x13:
										return new FailUnimplemented ("Mfthi_dsp3", machineInstruction);
									case 0x14:
										return new FailUnimplemented ("Mftacx_dsp3", machineInstruction);
									case 0x16:
										return new FailUnimplemented ("Mftdsp", machineInstruction);
									default:
										return new CP0Unimplemented ("unknown", machineInstruction);
									}
								case 0x2:
									switch (machineInstruction[BitField.MT_H]) {
									case 0x0:
										return new FailUnimplemented ("Mftc1", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("Mfthc1", machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x3:
									return new FailUnimplemented ("Cftc1", machineInstruction);
								default:
									return new CP0Unimplemented ("unknown", machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0xc:
							switch (machineInstruction[BitField.MT_U]) {
							case 0x0:
								return new FailUnimplemented ("Mttc0", machineInstruction);
							case 0x1:
								switch (machineInstruction[BitField.SEL]) {
								case 0x0:
									return new FailUnimplemented ("Mttgpr", machineInstruction);
								case 0x1:
									switch (machineInstruction[BitField.RT]) {
									case 0x0:
										return new FailUnimplemented ("Mttlo_dsp0", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("Mtthi_dsp0", machineInstruction);
									case 0x2:
										return new FailUnimplemented ("Mttacx_dsp0", machineInstruction);
									case 0x4:
										return new FailUnimplemented ("Mttlo_dsp1", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("Mtthi_dsp1", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Mttacx_dsp1", machineInstruction);
									case 0x8:
										return new FailUnimplemented ("Mttlo_dsp2", machineInstruction);
									case 0x9:
										return new FailUnimplemented ("Mtthi_dsp2", machineInstruction);
									case 0x10:
										return new FailUnimplemented ("Mttacx_dsp2", machineInstruction);
									case 0x12:
										return new FailUnimplemented ("Mttlo_dsp3", machineInstruction);
									case 0x13:
										return new FailUnimplemented ("Mtthi_dsp3", machineInstruction);
									case 0x14:
										return new FailUnimplemented ("Mttacx_dsp3", machineInstruction);
									case 0x16:
										return new FailUnimplemented ("Mttdsp", machineInstruction);
									default:
										return new CP0Unimplemented ("unknown", machineInstruction);
									}
								case 0x2:
									return new FailUnimplemented ("Mttc1", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Cttc1", machineInstruction);
								default:
									return new CP0Unimplemented ("unknown", machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0xb:
							switch (machineInstruction[BitField.RD]) {
							case 0x0:
								switch (machineInstruction[BitField.POS]) {
								case 0x0:
									switch (machineInstruction[BitField.SEL]) {
									case 0x1:
										switch (machineInstruction[BitField.SC]) {
										case 0x0:
											return new FailUnimplemented ("Dvpe", machineInstruction);
										case 0x1:
											return new FailUnimplemented ("Evpe", machineInstruction);
										default:
											return new CP0Unimplemented ("unknown", machineInstruction);
										}
									default:
										return new CP0Unimplemented ("unknown", machineInstruction);
									}
								default:
									return new CP0Unimplemented ("unknown", machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.POS]) {
								case 0xf:
									switch (machineInstruction[BitField.SEL]) {
									case 0x1:
										switch (machineInstruction[BitField.SC]) {
										case 0x0:
											return new FailUnimplemented ("Dmt", machineInstruction);
										case 0x1:
											return new FailUnimplemented ("Emt", machineInstruction);
										default:
											return new CP0Unimplemented ("unknown", machineInstruction);
										}
									default:
										return new CP0Unimplemented ("unknown", machineInstruction);
									}
								default:
									return new CP0Unimplemented ("unknown", machineInstruction);
								}
							case 0xc:
								switch (machineInstruction[BitField.POS]) {
								case 0x0:
									switch (machineInstruction[BitField.SC]) {
									case 0x0:
										return new FailUnimplemented ("Di", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("Ei", machineInstruction);
									default:
										return new CP0Unimplemented ("unknown", machineInstruction);
									}
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new CP0Unimplemented ("unknown", machineInstruction);
							}
						case 0xa:
							return new FailUnimplemented ("Rdpgpr", machineInstruction);
						case 0xe:
							return new FailUnimplemented ("Wrpgpr", machineInstruction);
						}
					case 0x1:
						switch (machineInstruction[BitField.FUNC]) {
						case 0x18:
							return new FailUnimplemented ("Eret", machineInstruction);
						case 0x1f:
							return new FailUnimplemented ("Deret", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Tlbr", machineInstruction);
						case 0x2:
							return new FailUnimplemented ("Tlbwi", machineInstruction);
						case 0x6:
							return new FailUnimplemented ("Tlbwr", machineInstruction);
						case 0x8:
							return new FailUnimplemented ("Tlbp", machineInstruction);
						case 0x20:
							return new CP0Unimplemented ("wait", machineInstruction);
						default:
							return new CP0Unimplemented ("unknown", machineInstruction);
						}
					default:
						return new Unknown (machineInstruction);
					}
				case 0x1:
					switch (machineInstruction[BitField.RS_MSB]) {
					case 0x0:
						switch (machineInstruction[BitField.RS_HI]) {
						case 0x0:
							switch (machineInstruction[BitField.RS_LO]) {
							case 0x0:
								return new Mfc1 (machineInstruction);
							case 0x2:
								return new Cfc1 (machineInstruction);
							case 0x3:
								return new FailUnimplemented ("Mfhc1", machineInstruction);
							case 0x4:
								return new Mtc1 (machineInstruction);
							case 0x6:
								return new Ctc1 (machineInstruction);
							case 0x7:
								return new FailUnimplemented ("Mthc1", machineInstruction);
							case 0x1:
								return new CP1Unimplemented ("dmfc1", machineInstruction);
							case 0x5:
								return new CP1Unimplemented ("dmtc1", machineInstruction);
							default:
								return new Unknown (machineInstruction);
							}
						case 0x1:
							switch (machineInstruction[BitField.RS_LO]) {
							case 0x0:
								switch (machineInstruction[BitField.ND]) {
								case 0x0:
									switch (machineInstruction[BitField.TF]) {
									case 0x0:
										return new Bc1f (machineInstruction);
									case 0x1:
										return new Bc1t (machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x1:
									switch (machineInstruction[BitField.TF]) {
									case 0x0:
										return new Bc1fl (machineInstruction);
									case 0x1:
										return new Bc1tl (machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								return new CP1Unimplemented ("bc1any2", machineInstruction);
							case 0x2:
								return new CP1Unimplemented ("bc1any4", machineInstruction);
							default:
								return new CP1Unimplemented ("unknown", machineInstruction);
							}
						default:
							return new Unknown (machineInstruction);
						}
					case 0x1:
						switch (machineInstruction[BitField.RS_HI]) {
						case 0x2:
							switch (machineInstruction[BitField.RS_LO]) {
							case 0x0:
								switch (machineInstruction[BitField.FUNC_HI]) {
								case 0x0:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new Add_s (machineInstruction);
									case 0x1:
										return new Sub_s (machineInstruction);
									case 0x2:
										return new Mul_s (machineInstruction);
									case 0x3:
										return new Div_s (machineInstruction);
									case 0x4:
										return new Sqrt_s (machineInstruction);
									case 0x5:
										return new Abs_s (machineInstruction);
									case 0x7:
										return new Neg_s (machineInstruction);
									case 0x6:
										return new Mov_s (machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x1:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Round_l_s", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("Trunc_l_s", machineInstruction);
									case 0x2:
										return new FailUnimplemented ("Ceil_l_s", machineInstruction);
									case 0x3:
										return new FailUnimplemented ("Floor_l_s", machineInstruction);
									case 0x4:
										return new FailUnimplemented ("Round_w_s", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("Trunc_w_s", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Ceil_w_s", machineInstruction);
									case 0x7:
										return new FailUnimplemented ("Floor_w_s", machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x2:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x1:
										switch (machineInstruction[BitField.MOVCF]) {
										case 0x0:
											return new FailUnimplemented ("Movf_s", machineInstruction);
										case 0x1:
											return new FailUnimplemented ("Movt_s", machineInstruction);
										default:
											return new Unknown (machineInstruction);
										}
									case 0x2:
										return new FailUnimplemented ("Movz_s", machineInstruction);
									case 0x3:
										return new FailUnimplemented ("Movn_s", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("Recip_s", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Rsqrt_s", machineInstruction);
									default:
										return new CP1Unimplemented ("unknown", machineInstruction);
									}
								case 0x3:
									return new CP1Unimplemented ("unknown", machineInstruction);
								case 0x4:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x1:
										return new Cvt_d_s (machineInstruction);
									case 0x4:
										return new Cvt_w_s (machineInstruction);
									case 0x5:
										return new Cvt_l_s (machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Cvt_ps_s", machineInstruction);
									default:
										return new CP1Unimplemented ("unknown", machineInstruction);
									}
								case 0x5:
									return new CP1Unimplemented ("unknown", machineInstruction);
								case 0x6:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new C_f_s (machineInstruction);
									case 0x1:
										return new C_un_s (machineInstruction);
									case 0x2:
										return new C_eq_s (machineInstruction);
									case 0x3:
										return new C_ueq_s (machineInstruction);
									case 0x4:
										return new C_olt_s (machineInstruction);
									case 0x5:
										return new C_ult_s (machineInstruction);
									case 0x6:
										return new C_ole_s (machineInstruction);
									case 0x7:
										return new C_ule_s (machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x7:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new C_sf_s (machineInstruction);
									case 0x1:
										return new C_ngle_s (machineInstruction);
									case 0x2:
										return new C_seq_s (machineInstruction);
									case 0x3:
										return new C_ngl_s (machineInstruction);
									case 0x4:
										return new C_lt_s (machineInstruction);
									case 0x5:
										return new C_nge_s (machineInstruction);
									case 0x6:
										return new C_le_s (machineInstruction);
									case 0x7:
										return new C_ngt_s (machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.FUNC_HI]) {
								case 0x0:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new Add_d (machineInstruction);
									case 0x1:
										return new Sub_d (machineInstruction);
									case 0x2:
										return new Mul_d (machineInstruction);
									case 0x3:
										return new Div_d (machineInstruction);
									case 0x4:
										return new Sqrt_d (machineInstruction);
									case 0x5:
										return new Abs_d (machineInstruction);
									case 0x7:
										return new Neg_d (machineInstruction);
									case 0x6:
										return new Mov_d (machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x1:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Round_l_d", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("Trunc_l_d", machineInstruction);
									case 0x2:
										return new FailUnimplemented ("Ceil_l_d", machineInstruction);
									case 0x3:
										return new FailUnimplemented ("Floor_l_d", machineInstruction);
									case 0x4:
										return new FailUnimplemented ("Round_w_d", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("Trunc_w_d", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Ceil_w_d", machineInstruction);
									case 0x7:
										return new FailUnimplemented ("Floor_w_d", machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x2:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x1:
										switch (machineInstruction[BitField.MOVCF]) {
										case 0x0:
											return new FailUnimplemented ("Movf_d", machineInstruction);
										case 0x1:
											return new FailUnimplemented ("Movt_d", machineInstruction);
										default:
											return new Unknown (machineInstruction);
										}
									case 0x2:
										return new FailUnimplemented ("Movz_d", machineInstruction);
									case 0x3:
										return new FailUnimplemented ("Movn_d", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("Recip_d", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Rsqrt_d", machineInstruction);
									default:
										return new CP1Unimplemented ("unknown", machineInstruction);
									}
								case 0x4:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new Cvt_s_d (machineInstruction);
									case 0x4:
										return new Cvt_w_d (machineInstruction);
									case 0x5:
										return new Cvt_l_d (machineInstruction);
									default:
										return new CP1Unimplemented ("unknown", machineInstruction);
									}
								case 0x6:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new C_f_d (machineInstruction);
									case 0x1:
										return new C_un_d (machineInstruction);
									case 0x2:
										return new C_eq_d (machineInstruction);
									case 0x3:
										return new C_ueq_d (machineInstruction);
									case 0x4:
										return new C_olt_d (machineInstruction);
									case 0x5:
										return new C_ult_d (machineInstruction);
									case 0x6:
										return new C_ole_d (machineInstruction);
									case 0x7:
										return new C_ule_d (machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x7:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new C_sf_d (machineInstruction);
									case 0x1:
										return new C_ngle_d (machineInstruction);
									case 0x2:
										return new C_seq_d (machineInstruction);
									case 0x3:
										return new C_ngl_d (machineInstruction);
									case 0x4:
										return new C_lt_d (machineInstruction);
									case 0x5:
										return new C_nge_d (machineInstruction);
									case 0x6:
										return new C_le_d (machineInstruction);
									case 0x7:
										return new C_ngt_d (machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								default:
									return new CP1Unimplemented ("unknown", machineInstruction);
								}
							case 0x2:
								return new CP1Unimplemented ("unknown", machineInstruction);
							case 0x3:
								return new CP1Unimplemented ("unknown", machineInstruction);
							case 0x7:
								return new CP1Unimplemented ("unknown", machineInstruction);
							case 0x4:
								switch (machineInstruction[BitField.FUNC]) {
								case 0x20:
									return new Cvt_s_w (machineInstruction);
								case 0x21:
									return new Cvt_d_w (machineInstruction);
								case 0x26:
									return new CP1Unimplemented ("cvt_ps_w", machineInstruction);
								default:
									return new CP1Unimplemented ("unknown", machineInstruction);
								}
							case 0x5:
								switch (machineInstruction[BitField.FUNC_HI]) {
								case 0x20:
									return new Cvt_s_l (machineInstruction);
								case 0x21:
									return new Cvt_d_l (machineInstruction);
								case 0x26:
									return new CP1Unimplemented ("cvt_ps_l", machineInstruction);
								default:
									return new CP1Unimplemented ("unknown", machineInstruction);
								}
							case 0x6:
								switch (machineInstruction[BitField.FUNC_HI]) {
								case 0x0:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Add_ps", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("Sub_ps", machineInstruction);
									case 0x2:
										return new FailUnimplemented ("Mul_ps", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("Abs_ps", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Mov_ps", machineInstruction);
									case 0x7:
										return new FailUnimplemented ("Neg_ps", machineInstruction);
									default:
										return new CP1Unimplemented ("unknown", machineInstruction);
									}
								case 0x1:
									return new CP1Unimplemented ("unknown", machineInstruction);
								case 0x2:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x1:
										switch (machineInstruction[BitField.MOVCF]) {
										case 0x0:
											return new FailUnimplemented ("Movf_ps", machineInstruction);
										case 0x1:
											return new FailUnimplemented ("Movt_ps", machineInstruction);
										default:
											return new Unknown (machineInstruction);
										}
									case 0x2:
										return new FailUnimplemented ("Movz_ps", machineInstruction);
									case 0x3:
										return new FailUnimplemented ("Movn_ps", machineInstruction);
									default:
										return new CP1Unimplemented ("unknown", machineInstruction);
									}
								case 0x3:
									return new CP1Unimplemented ("unknown", machineInstruction);
								case 0x4:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Cvt_s_pu", machineInstruction);
									default:
										return new CP1Unimplemented ("unknown", machineInstruction);
									}
								case 0x5:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("Cvt_s_pl", machineInstruction);
									case 0x4:
										return new FailUnimplemented ("Pll", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("Plu", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("Pul", machineInstruction);
									case 0x7:
										return new FailUnimplemented ("Puu", machineInstruction);
									default:
										return new CP1Unimplemented ("unknown", machineInstruction);
									}
								case 0x6:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("C_f_ps", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("C_un_ps", machineInstruction);
									case 0x2:
										return new FailUnimplemented ("C_eq_ps", machineInstruction);
									case 0x3:
										return new FailUnimplemented ("C_ueq_ps", machineInstruction);
									case 0x4:
										return new FailUnimplemented ("C_olt_ps", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("C_ult_ps", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("C_ole_ps", machineInstruction);
									case 0x7:
										return new FailUnimplemented ("C_ule_ps", machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								case 0x7:
									switch (machineInstruction[BitField.FUNC_LO]) {
									case 0x0:
										return new FailUnimplemented ("C_sf_ps", machineInstruction);
									case 0x1:
										return new FailUnimplemented ("C_ngle_ps", machineInstruction);
									case 0x2:
										return new FailUnimplemented ("C_seq_ps", machineInstruction);
									case 0x3:
										return new FailUnimplemented ("C_ngl_ps", machineInstruction);
									case 0x4:
										return new FailUnimplemented ("C_lt_ps", machineInstruction);
									case 0x5:
										return new FailUnimplemented ("C_nge_ps", machineInstruction);
									case 0x6:
										return new FailUnimplemented ("C_le_ps", machineInstruction);
									case 0x7:
										return new FailUnimplemented ("C_ngt_ps", machineInstruction);
									default:
										return new Unknown (machineInstruction);
									}
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						default:
							return new CP1Unimplemented ("unknown", machineInstruction);
						}
					default:
						return new Unknown (machineInstruction);
					}
				case 0x2:
					switch (machineInstruction[BitField.RS_MSB]) {
					case 0x0:
						switch (machineInstruction[BitField.RS_HI]) {
						case 0x0:
							switch (machineInstruction[BitField.RS_LO]) {
							case 0x0:
								return new CP2Unimplemented ("mfc2", machineInstruction);
							case 0x2:
								return new CP2Unimplemented ("cfc2", machineInstruction);
							case 0x3:
								return new CP2Unimplemented ("mfhc2", machineInstruction);
							case 0x4:
								return new CP2Unimplemented ("mtc2", machineInstruction);
							case 0x6:
								return new CP2Unimplemented ("ctc2", machineInstruction);
							case 0x7:
								return new CP2Unimplemented ("mftc2", machineInstruction);
							default:
								return new CP2Unimplemented ("unknown", machineInstruction);
							}
						case 0x1:
							switch (machineInstruction[BitField.ND]) {
							case 0x0:
								switch (machineInstruction[BitField.TF]) {
								case 0x0:
									return new CP2Unimplemented ("bc2f", machineInstruction);
								case 0x1:
									return new CP2Unimplemented ("bc2t", machineInstruction);
								default:
									return new CP2Unimplemented ("unknown", machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.TF]) {
								case 0x0:
									return new CP2Unimplemented ("bc2fl", machineInstruction);
								case 0x1:
									return new CP2Unimplemented ("bc2tl", machineInstruction);
								default:
									return new CP2Unimplemented ("unknown", machineInstruction);
								}
							default:
								return new CP2Unimplemented ("unknown", machineInstruction);
							}
						default:
							return new CP2Unimplemented ("unknown", machineInstruction);
						}
					default:
						return new CP2Unimplemented ("unknown", machineInstruction);
					}
				case 0x3:
					switch (machineInstruction[BitField.FUNC_HI]) {
					case 0x0:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Lwxc1", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Ldxc1", machineInstruction);
						case 0x5:
							return new FailUnimplemented ("Luxc1", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x1:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Swxc1", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Sdxc1", machineInstruction);
						case 0x5:
							return new FailUnimplemented ("Suxc1", machineInstruction);
						case 0x7:
							return new FailUnimplemented ("Prefx", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x3:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x6:
							return new FailUnimplemented ("Alnv_ps", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x4:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Madd_s", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Madd_d", machineInstruction);
						case 0x6:
							return new FailUnimplemented ("Madd_ps", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x5:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Msub_s", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Msub_d", machineInstruction);
						case 0x6:
							return new FailUnimplemented ("Msub_ps", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x6:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Nmadd_s", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Nmadd_d", machineInstruction);
						case 0x6:
							return new FailUnimplemented ("Nmadd_ps", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x7:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Nmsub_s", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Nmsub_d", machineInstruction);
						case 0x6:
							return new FailUnimplemented ("Nmsub_ps", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					default:
						return new Unknown (machineInstruction);
					}
				case 0x4:
					return new FailUnimplemented ("Beql", machineInstruction);
				case 0x5:
					return new FailUnimplemented ("Bnel", machineInstruction);
				case 0x6:
					return new FailUnimplemented ("Blezl", machineInstruction);
				case 0x7:
					return new FailUnimplemented ("Bgtzl", machineInstruction);
				default:
					return new Unknown (machineInstruction);
				}
			case 0x3:
				switch (machineInstruction[BitField.OPCODE_LO]) {
				case 0x4:
					switch (machineInstruction[BitField.FUNC_HI]) {
					case 0x0:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x2:
							return new FailUnimplemented ("Mul", machineInstruction);
						case 0x0:
							return new FailUnimplemented ("Madd", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Maddu", machineInstruction);
						case 0x4:
							return new FailUnimplemented ("Msub", machineInstruction);
						case 0x5:
							return new FailUnimplemented ("Msubu", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x4:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Clz", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Clo", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x7:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x7:
							return new FailUnimplemented ("sdbbp", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					default:
						return new Unknown (machineInstruction);
					}
				case 0x7:
					switch (machineInstruction[BitField.FUNC_HI]) {
					case 0x0:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Ext", machineInstruction);
						case 0x4:
							return new FailUnimplemented ("Ins", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x1:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							return new FailUnimplemented ("Fork", machineInstruction);
						case 0x1:
							return new FailUnimplemented ("Yield", machineInstruction);
						case 0x2:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Lwx", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Lhx", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Lbux", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0x4:
							return new FailUnimplemented ("Insv", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x2:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addu_qb", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Subu_qb", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Addu_s_qb", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Subu_s_qb", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Muleu_s_ph_qbl", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Muleu_s_ph_qbr", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addu_ph", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Subu_ph", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Addq_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Subq_ph", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Addu_s_ph", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Subu_s_ph", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Addq_s_ph", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Subq_s_ph", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x2:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addsc", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Addwc", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Modsub", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Raddu_w_qb", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Addq_s_w", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Subq_s_w", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x3:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x4:
									return new FailUnimplemented ("Muleq_s_w_phl", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Muleq_s_w_phr", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Mulq_s_ph", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Mulq_rs_ph", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0x1:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Cmpu_eq_qb", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Cmpu_lt_qb", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Cmpu_le_qb", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Pick_qb", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Cmpgu_eq_qb", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Cmpgu_lt_qb", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Cmpgu_le_qb", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Cmp_eq_ph", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Cmp_lt_ph", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Cmp_le_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Pick_ph", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Precrq_qb_ph", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Precr_qb_ph", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Packrl_ph", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Precrqu_s_qb_ph", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x2:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x4:
									return new FailUnimplemented ("Precrq_ph_w", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Precrq_rs_ph_w", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x3:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Cmpgdu_eq_qb", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Cmpgdu_lt_qb", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Cmpgdu_le_qb", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Precr_sra_ph_w", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Precr_sra_r_ph_w", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0x2:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x1:
									return new FailUnimplemented ("Absq_s_qb", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Repl_qb", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Replv_qb", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Precequ_ph_qbl", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Precequ_ph_qbr", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Precequ_ph_qbla", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Precequ_ph_qbra", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x1:
									return new FailUnimplemented ("Absq_s_ph", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Repl_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Replv_ph", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Preceq_w_phl", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Preceq_w_phr", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x2:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x1:
									return new FailUnimplemented ("Absq_s_w", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x3:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x3:
									return new FailUnimplemented ("Bitrev", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Preceu_ph_qbl", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Preceu_ph_qbr", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Preceu_ph_qbla", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Preceu_ph_qbra", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0x3:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Shll_qb", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Shrl_qb", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Shllv_qb", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Shrlv_qb", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Shra_qb", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Shra_r_qb", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Shrav_qb", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Shrav_r_qb", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Shll_ph", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Shra_ph", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Shllv_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Shrav_ph", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Shll_s_ph", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Shra_r_ph", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Shllv_s_ph", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Shrav_r_ph", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x2:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x4:
									return new FailUnimplemented ("Shll_s_w", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Shra_r_w", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Shllv_s_w", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Shrav_r_w", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x3:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x1:
									return new FailUnimplemented ("Shrl_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Shrlv_ph", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						default:
							return new Unknown (machineInstruction);
						}
					case 0x3:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Adduh_qb", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Subuh_qb", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Adduh_r_qb", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Subuh_r_qb", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addqh_ph", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Subqh_ph", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Addqh_r_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Subqh_r_ph", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Mul_ph", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Mul_s_ph", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x2:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Addqh_w", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Subqh_w", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Addqh_r_w", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Subqh_r_w", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Mulq_s_w", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Mulq_rs_w", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						default:
							return new Unknown (machineInstruction);
						}
					case 0x4:
						switch (machineInstruction[BitField.SA]) {
						case 0x2:
							return new FailUnimplemented ("Wsbh", machineInstruction);
						case 0x10:
							return new FailUnimplemented ("Seb", machineInstruction);
						case 0x18:
							return new FailUnimplemented ("Seh", machineInstruction);
						default:
							return new Unknown (machineInstruction);
						}
					case 0x6:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Dpa_w_ph", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Dps_w_ph", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Mulsa_w_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Dpau_h_qbl", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Dpaq_s_w_ph", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Dpsq_s_w_ph", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Mulsaq_s_w_ph", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Dpau_h_qbr", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Dpax_w_ph", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Dpsx_w_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Dpsu_h_qbl", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Dpaq_sa_l_w", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Dpsq_sa_l_w", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Dpsu_h_qbr", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x2:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Maq_sa_w_phl", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Maq_sa_w_phr", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Maq_s_w_phl", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Maq_s_w_phr", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x3:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Dpaqx_s_w_ph", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Dpsqx_s_w_ph", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Dpaqx_sa_w_ph", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Dpsqx_sa_w_ph", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0x1:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Append", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Prepend", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x2:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Balign", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						default:
							return new Unknown (machineInstruction);
						}
					case 0x7:
						switch (machineInstruction[BitField.FUNC_LO]) {
						case 0x0:
							switch (machineInstruction[BitField.OP_HI]) {
							case 0x0:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x0:
									return new FailUnimplemented ("Extr_w", machineInstruction);
								case 0x1:
									return new FailUnimplemented ("Extrv_w", machineInstruction);
								case 0x2:
									return new FailUnimplemented ("Extp", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Extpv", machineInstruction);
								case 0x4:
									return new FailUnimplemented ("Extr_r_w", machineInstruction);
								case 0x5:
									return new FailUnimplemented ("Extrv_r_w", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Extr_rs_w", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Extrv_rs_w", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x1:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x2:
									return new FailUnimplemented ("Extpdp", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Extpdpv", machineInstruction);
								case 0x6:
									return new FailUnimplemented ("Extr_s_h", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Extrv_s_h", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x2:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x2:
									return new FailUnimplemented ("Rddsp", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Wrdsp", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							case 0x3:
								switch (machineInstruction[BitField.OP_LO]) {
								case 0x2:
									return new FailUnimplemented ("Shilo", machineInstruction);
								case 0x3:
									return new FailUnimplemented ("Shilov", machineInstruction);
								case 0x7:
									return new FailUnimplemented ("Mthlip", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						case 0x3:
							switch (machineInstruction[BitField.OP]) {
							case 0x0:
								switch (machineInstruction[BitField.RD]) {
								case 0x1d:
									return new FailUnimplemented ("Rdhwr", machineInstruction);
								default:
									return new Unknown (machineInstruction);
								}
							default:
								return new Unknown (machineInstruction);
							}
						default:
							return new Unknown (machineInstruction);
						}
					default:
						return new Unknown (machineInstruction);
					}
				default:
					return new Unknown (machineInstruction);
				}
			case 0x4:
				switch (machineInstruction[BitField.OPCODE_LO]) {
				case 0x0:
					return new Lb (machineInstruction);
				case 0x1:
					return new Lh (machineInstruction);
				case 0x3:
					return new Lw (machineInstruction);
				case 0x4:
					return new Lbu (machineInstruction);
				case 0x5:
					return new Lhu (machineInstruction);
				case 0x2:
					return new Lwl (machineInstruction);
				case 0x6:
					return new Lwr (machineInstruction);
				default:
					return new Unknown (machineInstruction);
				}
			case 0x5:
				switch (machineInstruction[BitField.OPCODE_LO]) {
				case 0x0:
					return new Sb (machineInstruction);
				case 0x1:
					return new Sh (machineInstruction);
				case 0x3:
					return new Sw (machineInstruction);
				case 0x2:
					return new Swl (machineInstruction);
				case 0x6:
					return new Swr (machineInstruction);
				case 0x7:
					return new FailUnimplemented ("Cache", machineInstruction);
				default:
					return new Unknown (machineInstruction);
				}
			case 0x6:
				switch (machineInstruction[BitField.OPCODE_LO]) {
				case 0x0:
					return new Ll (machineInstruction);
				case 0x1:
					return new Lwc1 (machineInstruction);
				case 0x5:
					return new Ldc1 (machineInstruction);
				case 0x2:
					return new CP2Unimplemented ("lwc2", machineInstruction);
				case 0x6:
					return new CP2Unimplemented ("ldc2", machineInstruction);
				case 0x3:
					return new FailUnimplemented ("Pref", machineInstruction);
				default:
					return new Unknown (machineInstruction);
				}
			case 0x7:
				switch (machineInstruction[BitField.OPCODE_LO]) {
				case 0x0:
					return new Sc (machineInstruction);
				case 0x1:
					return new Swc1 (machineInstruction);
				case 0x5:
					return new Sdc1 (machineInstruction);
				case 0x2:
					return new CP2Unimplemented ("swc2", machineInstruction);
				case 0x6:
					return new CP2Unimplemented ("sdc2", machineInstruction);
				default:
					return new Unknown (machineInstruction);
				}
			default:
				return new Unknown (machineInstruction);
			}
		}
	}
}

namespace MinCai.Simulators.Flexim.Architecture.Instructions
{
	public sealed class Syscall : StaticInstruction
	{
		public Syscall (MachineInstruction machineInstruction) : base("syscall", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, 2));
		}

		public override void Execute (IThread thread)
		{
			thread.Syscall (thread.Regs.IntRegs[2]);
		}
	}

	public sealed class Sll : StaticInstruction
	{
		public Sll (MachineInstruction machineInstruction) : base("sll", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RT]] << (int)this[BitField.SA];
		}
	}

	public sealed class Sllv : StaticInstruction
	{
		public Sllv (MachineInstruction machineInstruction) : base("sllv", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RT]] << (int)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0);
		}
	}

	public sealed class Sra : StaticInstruction
	{
		public Sra (MachineInstruction machineInstruction) : base("sra", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RT]] >> (int)this[BitField.SA]);
		}
	}

	public sealed class Srav : StaticInstruction
	{
		public Srav (MachineInstruction machineInstruction) : base("srav", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RT]] >> (int)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0));
		}
	}

	public sealed class Srl : StaticInstruction
	{
		public Srl (MachineInstruction machineInstruction) : base("srl", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RT]] >> (int)this[BitField.SA];
		}
	}

	public sealed class Srlv : StaticInstruction
	{
		public Srlv (MachineInstruction machineInstruction) : base("srlv", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RT]] >> (int)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RS]], 4, 0);
		}
	}

	public abstract class Branch : StaticInstruction
	{
		public Branch (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
			this.Displacement = BitHelper.Sext (this[BitField.OFFSET] << 2, 16);
		}

		public override uint GetTargetPc (IThread thread)
		{
			return (uint)(thread.Regs.Npc + this.Displacement);
		}

		public void DoBranch (IThread thread)
		{
			thread.Regs.Nnpc = this.GetTargetPc (thread);
		}

		public int Displacement { get; private set; }
	}

	public sealed class B : Branch
	{
		public B (MachineInstruction machineInstruction) : base("b", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.UnconditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (IThread thread)
		{
			this.DoBranch (thread);
		}
	}

	public sealed class Bal : Branch
	{
		public Bal (MachineInstruction machineInstruction) : base("bal", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.UnconditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, RegisterConstants.RETURN_ADDRESS_REG));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.RETURN_ADDRESS_REG] = thread.Regs.Nnpc;
			this.DoBranch (thread);
		}
	}

	public sealed class Beq : Branch
	{
		public Beq (MachineInstruction machineInstruction) : base("beq", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] == (int)thread.Regs.IntRegs[this[BitField.RT]]) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Beqz : Branch
	{
		public Beqz (MachineInstruction machineInstruction) : base("beqz", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		public override void Execute (IThread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] == 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bgez : Branch
	{
		public Bgez (MachineInstruction machineInstruction) : base("bgez", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		public override void Execute (IThread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] >= 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bgezal : Branch
	{
		public Bgezal (MachineInstruction machineInstruction) : base("bgezal", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.FunctionCall | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, RegisterConstants.RETURN_ADDRESS_REG));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.RETURN_ADDRESS_REG] = thread.Regs.Nnpc;
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] >= 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bgtz : Branch
	{
		public Bgtz (MachineInstruction machineInstruction) : base("bgtz", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		public override void Execute (IThread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] > 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Blez : Branch
	{
		public Blez (MachineInstruction machineInstruction) : base("blez", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		public override void Execute (IThread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] <= 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bltz : Branch
	{
		public Bltz (MachineInstruction machineInstruction) : base("bltz", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		public override void Execute (IThread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] < 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bltzal : Branch
	{
		public Bltzal (MachineInstruction machineInstruction) : base("bltzal", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.FunctionCall | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, RegisterConstants.RETURN_ADDRESS_REG));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.RETURN_ADDRESS_REG] = thread.Regs.Nnpc;
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] < 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bne : Branch
	{
		public Bne (MachineInstruction machineInstruction) : base("bne", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] != (int)thread.Regs.IntRegs[this[BitField.RT]]) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bnez : Branch
	{
		public Bnez (MachineInstruction machineInstruction) : base("bnez", machineInstruction, Flag.IntegerComputation | Flag.Control | Flag.ConditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		public override void Execute (IThread thread)
		{
			if ((int)thread.Regs.IntRegs[this[BitField.RS]] != 0) {
				this.DoBranch (thread);
			}
		}
	}

	public sealed class Bc1f : Branch
	{
		public Bc1f (MachineInstruction machineInstruction) : base("bc1f", machineInstruction, Flag.Control | Flag.ConditionalBranch, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_FCSR));
		}

		public override void Execute (IThread thread)
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
		public Bc1t (MachineInstruction machineInstruction) : base("bc1t", machineInstruction, Flag.Control | Flag.ConditionalBranch, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_FCSR));
		}

		public override void Execute (IThread thread)
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
		public Bc1fl (MachineInstruction machineInstruction) : base("bc1fl", machineInstruction, Flag.Control | Flag.ConditionalBranch, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_FCSR));
		}

		public override void Execute (IThread thread)
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
		public Bc1tl (MachineInstruction machineInstruction) : base("bc1tl", machineInstruction, Flag.Control | Flag.ConditionalBranch, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_FCSR));
		}

		public override void Execute (IThread thread)
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

	public abstract class Jump : StaticInstruction
	{
		public Jump (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
			this.Target = this[BitField.JMPTARG] << 2;
		}

		public void DoJump (IThread thread)
		{
			thread.Regs.Nnpc = this.GetTargetPc (thread);
		}

		public uint Target { get; private set; }
	}

	public sealed class J : Jump
	{
		public J (MachineInstruction machineInstruction) : base("j", machineInstruction, Flag.Control | Flag.UnconditionalBranch | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override uint GetTargetPc (IThread thread)
		{
			return BitHelper.Mbits (thread.Regs.Npc, 32, 28) | this.Target;
		}

		public override void Execute (IThread thread)
		{
			this.DoJump (thread);
		}
	}

	public sealed class Jal : Jump
	{
		public Jal (MachineInstruction machineInstruction) : base("jal", machineInstruction, Flag.Control | Flag.UnconditionalBranch | Flag.FunctionCall | Flag.DirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, RegisterConstants.RETURN_ADDRESS_REG));
		}

		public override uint GetTargetPc (IThread thread)
		{
			return BitHelper.Mbits (thread.Regs.Npc, 32, 28) | this.Target;
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[RegisterConstants.RETURN_ADDRESS_REG] = thread.Regs.Nnpc;
			this.DoJump (thread);
		}
	}

	public sealed class Jalr : Jump
	{
		public Jalr (MachineInstruction machineInstruction) : base("jalr", machineInstruction, Flag.Control | Flag.UnconditionalBranch | Flag.FunctionCall | Flag.IndirectJump, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override uint GetTargetPc (IThread thread)
		{
			return thread.Regs.IntRegs[this[BitField.RS]];
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.Nnpc;
			this.DoJump (thread);
		}
	}

	public sealed class Jr : Jump
	{
		public Jr (MachineInstruction machineInstruction) : base("jr", machineInstruction, Flag.Control | Flag.UnconditionalBranch | Flag.FunctionReturn | Flag.IndirectJump, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		public override uint GetTargetPc (IThread thread)
		{
			return thread.Regs.IntRegs[this[BitField.RS]];
		}

		public override void Execute (IThread thread)
		{
			this.DoJump (thread);
		}
	}

	public abstract class IntOp : StaticInstruction
	{
		public IntOp (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
		}
	}

	public abstract class IntImmOp : StaticInstruction
	{
		public IntImmOp (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
			this.Imm = (short)machineInstruction[BitField.INTIMM];
			
			this.ZextImm = 0x0000FFFF & machineInstruction[BitField.INTIMM];
			
			this.SextImm = BitHelper.Sext (machineInstruction[BitField.INTIMM], 16);
		}

		public short Imm { get; private set; }
		public int SextImm { get; private set; }
		public uint ZextImm { get; private set; }
	}

	public sealed class Add : IntOp
	{
		public Add (MachineInstruction machineInstruction) : base("add", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + (int)thread.Regs.IntRegs[this[BitField.RT]]);
			Logger.Warn (Logger.Categories.Instruction, "Add: overflow trap not implemented.");
		}
	}

	public sealed class Addi : IntImmOp
	{
		public Addi (MachineInstruction machineInstruction) : base("addi", machineInstruction, Flag.IntegerComputation | Flag.Immediate, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + this.SextImm);
			Logger.Warn (Logger.Categories.Instruction, "Addi: overflow trap not implemented.");
		}
	}

	public sealed class Addiu : IntImmOp
	{
		public Addiu (MachineInstruction machineInstruction) : base("addiu", machineInstruction, Flag.IntegerComputation | Flag.Immediate, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + this.SextImm);
		}
	}

	public sealed class Addu : IntOp
	{
		public Addu (MachineInstruction machineInstruction) : base("addu", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] + (int)thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public sealed class Sub : IntOp
	{
		public Sub (MachineInstruction machineInstruction) : base("sub", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] - (int)thread.Regs.IntRegs[this[BitField.RT]]);
			Logger.Warn (Logger.Categories.Instruction, "Sub: overflow trap not implemented.");
		}
	}

	public sealed class Subu : IntOp
	{
		public Subu (MachineInstruction machineInstruction) : base("subu", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)((int)thread.Regs.IntRegs[this[BitField.RS]] - (int)thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public sealed class And : IntOp
	{
		public And (MachineInstruction machineInstruction) : base("and", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] & thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public sealed class Andi : IntImmOp
	{
		public Andi (MachineInstruction machineInstruction) : base("andi", machineInstruction, Flag.IntegerComputation | Flag.Immediate, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] & this.ZextImm;
		}
	}

	public sealed class Nor : IntOp
	{
		public Nor (MachineInstruction machineInstruction) : base("nor", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = ~(thread.Regs.IntRegs[this[BitField.RS]] | thread.Regs.IntRegs[this[BitField.RT]]);
		}
	}

	public sealed class Or : IntOp
	{
		public Or (MachineInstruction machineInstruction) : base("or", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] | thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public sealed class Ori : IntImmOp
	{
		public Ori (MachineInstruction machineInstruction) : base("ori", machineInstruction, Flag.IntegerComputation | Flag.Immediate, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] | this.ZextImm;
		}
	}

	public sealed class Xor : IntOp
	{
		public Xor (MachineInstruction machineInstruction) : base("xor", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.IntRegs[this[BitField.RS]] ^ thread.Regs.IntRegs[this[BitField.RT]];
		}
	}

	public sealed class Xori : IntImmOp
	{
		public Xori (MachineInstruction machineInstruction) : base("xori", machineInstruction, Flag.IntegerComputation | Flag.Immediate, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = thread.Regs.IntRegs[this[BitField.RS]] ^ this.ZextImm;
		}
	}

	public sealed class Slt : IntOp
	{
		public Slt (MachineInstruction machineInstruction) : base("slt", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (int)thread.Regs.IntRegs[this[BitField.RS]] < (int)thread.Regs.IntRegs[this[BitField.RT]] ? 1u : 0;
		}
	}

	public sealed class Slti : IntImmOp
	{
		public Slti (MachineInstruction machineInstruction) : base("slti", machineInstruction, Flag.IntegerComputation | Flag.Immediate, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (int)thread.Regs.IntRegs[this[BitField.RS]] < this.SextImm ? 1u : 0;
		}
	}

	public sealed class Sltiu : IntImmOp
	{
		public Sltiu (MachineInstruction machineInstruction) : base("sltiu", machineInstruction, Flag.IntegerComputation | Flag.Immediate, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)thread.Regs.IntRegs[this[BitField.RS]] < this.ZextImm ? 1u : 0;
		}
	}

	public sealed class Sltu : IntOp
	{
		public Sltu (MachineInstruction machineInstruction) : base("sltu", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = (uint)thread.Regs.IntRegs[this[BitField.RS]] < (uint)thread.Regs.IntRegs[this[BitField.RT]] ? 1u : 0;
		}
	}

	public sealed class Lui : IntImmOp
	{
		public Lui (MachineInstruction machineInstruction) : base("lui", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)(this.Imm << 16);
		}
	}

	public sealed class Divu : StaticInstruction
	{
		public Divu (MachineInstruction machineInstruction) : base("divu", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntDivide)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_HI));
		}

		public override void Execute (IThread thread)
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

	public sealed class Div : StaticInstruction
	{
		public Div (MachineInstruction machineInstruction) : base("div", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntDivide)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_HI));
		}

		public override void Execute (IThread thread)
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

	public sealed class Mflo : StaticInstruction
	{
		public Mflo (MachineInstruction machineInstruction) : base("mflo", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.MiscRegs.Lo;
		}
	}

	public sealed class Mfhi : StaticInstruction
	{
		public Mfhi (MachineInstruction machineInstruction) : base("mfhi", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_HI));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.IntRegs[this[BitField.RD]] = thread.Regs.MiscRegs.Hi;
		}
	}

	public sealed class Mtlo : StaticInstruction
	{
		public Mtlo (MachineInstruction machineInstruction) : base("mtlo", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_LO));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.MiscRegs.Lo = thread.Regs.IntRegs[this[BitField.RD]];
		}
	}

	public sealed class Mthi : StaticInstruction
	{
		public Mthi (MachineInstruction machineInstruction) : base("mthi", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RD]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_HI));
		}

		public override void Execute (IThread thread)
		{
			thread.Regs.MiscRegs.Hi = thread.Regs.IntRegs[this[BitField.RD]];
		}
	}

	public sealed class Mult : StaticInstruction
	{
		public Mult (MachineInstruction machineInstruction) : base("mult", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_HI));
		}

		public override void Execute (IThread thread)
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

	public class Multu : StaticInstruction
	{
		public Multu (MachineInstruction machineInstruction) : base("multu", machineInstruction, Flag.IntegerComputation, FunctionalUnit.Types.IntALU)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_LO));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_HI));
		}

		public override void Execute (IThread thread)
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

	public abstract class FloatOp : StaticInstruction
	{
		public FloatOp (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
		}
	}

	public abstract class FloatBinaryOp : FloatOp
	{
		public FloatBinaryOp (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FD]));
		}
	}

	public abstract class FloatUnaryOp : FloatOp
	{
		public FloatUnaryOp (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FD]));
		}
	}

	public sealed class Add_d : FloatBinaryOp
	{
		public Add_d (MachineInstruction machineInstruction) : base("add_d", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatAdd)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			
			double fd = fs + ft;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Sub_d : FloatBinaryOp
	{
		public Sub_d (MachineInstruction machineInstruction) : base("sub_d", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatAdd)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			
			double fd = fs - ft;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Mul_d : FloatBinaryOp
	{
		public Mul_d (MachineInstruction machineInstruction) : base("mul_d", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatMultiply)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			
			double fd = fs * ft;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Div_d : FloatBinaryOp
	{
		public Div_d (MachineInstruction machineInstruction) : base("div_d", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatDivide)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double ft = thread.Regs.FloatRegs.GetDouble (this[BitField.FT]);
			
			double fd = fs / ft;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Sqrt_d : FloatUnaryOp
	{
		public Sqrt_d (MachineInstruction machineInstruction) : base("sqrt_d", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatSquareRoot)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = Math.Sqrt (fs);
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Abs_d : FloatUnaryOp
	{
		public Abs_d (MachineInstruction machineInstruction) : base("abs_d", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatCompare)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = Math.Abs (fs);
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Neg_d : FloatUnaryOp
	{
		public Neg_d (MachineInstruction machineInstruction) : base("neg_d", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatCompare)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			
			double fd = -1 * fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Mov_d : FloatUnaryOp
	{
		public Mov_d (MachineInstruction machineInstruction) : base("mov_d", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			double fd = fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Add_s : FloatBinaryOp
	{
		public Add_s (MachineInstruction machineInstruction) : base("add_s", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatAdd)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			
			float fd = fs + ft;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Sub_s : FloatBinaryOp
	{
		public Sub_s (MachineInstruction machineInstruction) : base("sub_s", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatAdd)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			
			float fd = fs - ft;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Mul_s : FloatBinaryOp
	{
		public Mul_s (MachineInstruction machineInstruction) : base("mul_s", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatMultiply)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			
			float fd = fs * ft;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Div_s : FloatBinaryOp
	{
		public Div_s (MachineInstruction machineInstruction) : base("div_s", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatDivide)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float ft = thread.Regs.FloatRegs.GetFloat (this[BitField.FT]);
			
			float fd = fs / ft;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Sqrt_s : FloatUnaryOp
	{
		public Sqrt_s (MachineInstruction machineInstruction) : base("sqrt_s", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatSquareRoot)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = (float)Math.Sqrt (fs);
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Abs_s : FloatUnaryOp
	{
		public Abs_s (MachineInstruction machineInstruction) : base("abs_s", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatCompare)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = Math.Abs (fs);
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Neg_s : FloatUnaryOp
	{
		public Neg_s (MachineInstruction machineInstruction) : base("neg_s", machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatCompare)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			
			float fd = -fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Mov_s : FloatUnaryOp
	{
		public Mov_s (MachineInstruction machineInstruction) : base("mov_s", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			float fd = fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public abstract class FloatConvertOp : FloatOp
	{
		public FloatConvertOp (string mnemonic, MachineInstruction machineInstruction) : base(mnemonic, machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatConvert)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FD]));
		}
	}

	public sealed class Cvt_d_s : FloatConvertOp
	{
		public Cvt_d_s (MachineInstruction machineInstruction) : base("cvt_d_s", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			double fd = (double)fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_w_s : FloatConvertOp
	{
		public Cvt_w_s (MachineInstruction machineInstruction) : base("cvt_w_s", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			uint fd = (uint)fs;
			
			thread.Regs.FloatRegs.SetUint (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_l_s : FloatConvertOp
	{
		public Cvt_l_s (MachineInstruction machineInstruction) : base("cvt_l_s", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			float fs = thread.Regs.FloatRegs.GetFloat (this[BitField.FS]);
			ulong fd = (ulong)fs;
			
			thread.Regs.FloatRegs.SetUlong (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_s_d : FloatConvertOp
	{
		public Cvt_s_d (MachineInstruction machineInstruction) : base("cvt_s_d", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			float fd = (float)fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_w_d : FloatConvertOp
	{
		public Cvt_w_d (MachineInstruction machineInstruction) : base("cvt_w_d", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			uint fd = (uint)fs;
			
			thread.Regs.FloatRegs.SetUint (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_l_d : FloatConvertOp
	{
		public Cvt_l_d (MachineInstruction machineInstruction) : base("cvt_l_d", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			double fs = thread.Regs.FloatRegs.GetDouble (this[BitField.FS]);
			ulong fd = (ulong)fs;
			
			thread.Regs.FloatRegs.SetUlong (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_s_w : FloatConvertOp
	{
		public Cvt_s_w (MachineInstruction machineInstruction) : base("cvt_s_w", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			uint fs = thread.Regs.FloatRegs.GetUint (this[BitField.FS]);
			float fd = (float)fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_d_w : FloatConvertOp
	{
		public Cvt_d_w (MachineInstruction machineInstruction) : base("cvt_d_w", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			uint fs = thread.Regs.FloatRegs.GetUint (this[BitField.FS]);
			double fd = (double)fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_s_l : FloatConvertOp
	{
		public Cvt_s_l (MachineInstruction machineInstruction) : base("cvt_s_l", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			ulong fs = thread.Regs.FloatRegs.GetUlong (this[BitField.FS]);
			float fd = (float)fs;
			
			thread.Regs.FloatRegs.SetFloat (fd, this[BitField.FD]);
		}
	}

	public sealed class Cvt_d_l : FloatConvertOp
	{
		public Cvt_d_l (MachineInstruction machineInstruction) : base("cvt_d_l", machineInstruction)
		{
		}

		public override void Execute (IThread thread)
		{
			ulong fs = thread.Regs.FloatRegs.GetUlong (this[BitField.FS]);
			double fd = (double)fs;
			
			thread.Regs.FloatRegs.SetDouble (fd, this[BitField.FD]);
		}
	}

	public abstract class FloatCompareOp : StaticInstruction
	{
		public FloatCompareOp (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FS]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FT]));
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_FCSR));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_FCSR));
		}
	}

	public abstract class C_cond_d : FloatCompareOp
	{
		public C_cond_d (string mnemonic, MachineInstruction machineInstruction) : base(mnemonic, machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatCompare)
		{
		}

		public override void Execute (IThread thread)
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
		public C_cond_s (string mnemonic, MachineInstruction machineInstruction) : base(mnemonic, machineInstruction, Flag.FloatComputation, FunctionalUnit.Types.FloatCompare)
		{
		}

		public override void Execute (IThread thread)
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
		public C_f_d (MachineInstruction machineInstruction) : base("c_f_d", machineInstruction)
		{
		}
	}

	public sealed class C_un_d : C_cond_d
	{
		public C_un_d (MachineInstruction machineInstruction) : base("c_un_d", machineInstruction)
		{
		}
	}

	public sealed class C_eq_d : C_cond_d
	{
		public C_eq_d (MachineInstruction machineInstruction) : base("c_eq_d", machineInstruction)
		{
		}
	}

	public sealed class C_ueq_d : C_cond_d
	{
		public C_ueq_d (MachineInstruction machineInstruction) : base("c_ueq_d", machineInstruction)
		{
		}
	}

	public sealed class C_olt_d : C_cond_d
	{
		public C_olt_d (MachineInstruction machineInstruction) : base("c_olt_d", machineInstruction)
		{
		}
	}

	public sealed class C_ult_d : C_cond_d
	{
		public C_ult_d (MachineInstruction machineInstruction) : base("c_ult_d", machineInstruction)
		{
		}
	}

	public sealed class C_ole_d : C_cond_d
	{
		public C_ole_d (MachineInstruction machineInstruction) : base("c_ole_d", machineInstruction)
		{
		}
	}

	public sealed class C_ule_d : C_cond_d
	{
		public C_ule_d (MachineInstruction machineInstruction) : base("c_ule_d", machineInstruction)
		{
		}
	}

	public sealed class C_sf_d : C_cond_d
	{
		public C_sf_d (MachineInstruction machineInstruction) : base("c_sf_d", machineInstruction)
		{
		}
	}

	public sealed class C_ngle_d : C_cond_d
	{
		public C_ngle_d (MachineInstruction machineInstruction) : base("c_ngle_d", machineInstruction)
		{
		}
	}

	public sealed class C_seq_d : C_cond_d
	{
		public C_seq_d (MachineInstruction machineInstruction) : base("c_seq_d", machineInstruction)
		{
		}
	}

	public sealed class C_ngl_d : C_cond_d
	{
		public C_ngl_d (MachineInstruction machineInstruction) : base("c_ngl_d", machineInstruction)
		{
		}
	}

	public sealed class C_lt_d : C_cond_d
	{
		public C_lt_d (MachineInstruction machineInstruction) : base("c_lt_d", machineInstruction)
		{
		}
	}

	public sealed class C_nge_d : C_cond_d
	{
		public C_nge_d (MachineInstruction machineInstruction) : base("c_nge_d", machineInstruction)
		{
		}
	}

	public sealed class C_le_d : C_cond_d
	{
		public C_le_d (MachineInstruction machineInstruction) : base("c_le_d", machineInstruction)
		{
		}
	}

	public sealed class C_ngt_d : C_cond_d
	{
		public C_ngt_d (MachineInstruction machineInstruction) : base("c_ngt_d", machineInstruction)
		{
		}
	}

	public sealed class C_f_s : C_cond_s
	{
		public C_f_s (MachineInstruction machineInstruction) : base("c_f_s", machineInstruction)
		{
		}
	}

	public sealed class C_un_s : C_cond_s
	{
		public C_un_s (MachineInstruction machineInstruction) : base("c_un_s", machineInstruction)
		{
		}
	}

	public sealed class C_eq_s : C_cond_s
	{
		public C_eq_s (MachineInstruction machineInstruction) : base("c_eq_s", machineInstruction)
		{
		}
	}

	public sealed class C_ueq_s : C_cond_s
	{
		public C_ueq_s (MachineInstruction machineInstruction) : base("c_ueq_s", machineInstruction)
		{
		}
	}

	public sealed class C_olt_s : C_cond_s
	{
		public C_olt_s (MachineInstruction machineInstruction) : base("c_olt_s", machineInstruction)
		{
		}
	}

	public sealed class C_ult_s : C_cond_s
	{
		public C_ult_s (MachineInstruction machineInstruction) : base("c_ult_s", machineInstruction)
		{
		}
	}

	public sealed class C_ole_s : C_cond_s
	{
		public C_ole_s (MachineInstruction machineInstruction) : base("c_ole_s", machineInstruction)
		{
		}
	}

	public sealed class C_ule_s : C_cond_s
	{
		public C_ule_s (MachineInstruction machineInstruction) : base("c_ule_s", machineInstruction)
		{
		}
	}

	public sealed class C_sf_s : C_cond_s
	{
		public C_sf_s (MachineInstruction machineInstruction) : base("c_sf_s", machineInstruction)
		{
		}
	}

	public sealed class C_ngle_s : C_cond_s
	{
		public C_ngle_s (MachineInstruction machineInstruction) : base("c_ngle_s", machineInstruction)
		{
		}
	}

	public sealed class C_seq_s : C_cond_s
	{
		public C_seq_s (MachineInstruction machineInstruction) : base("c_seq_s", machineInstruction)
		{
		}
	}

	public sealed class C_ngl_s : C_cond_s
	{
		public C_ngl_s (MachineInstruction machineInstruction) : base("c_ngl_s", machineInstruction)
		{
		}
	}

	public sealed class C_lt_s : C_cond_s
	{
		public C_lt_s (MachineInstruction machineInstruction) : base("c_lt_s", machineInstruction)
		{
		}
	}

	public sealed class C_nge_s : C_cond_s
	{
		public C_nge_s (MachineInstruction machineInstruction) : base("c_nge_s", machineInstruction)
		{
		}
	}

	public sealed class C_le_s : C_cond_s
	{
		public C_le_s (MachineInstruction machineInstruction) : base("c_le_s", machineInstruction)
		{
		}
	}

	public sealed class C_ngt_s : C_cond_s
	{
		public C_ngt_s (MachineInstruction machineInstruction) : base("c_ngt_s", machineInstruction)
		{
		}
	}

	public abstract class MemoryOp : StaticInstruction
	{
		public MemoryOp (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
			this.Displacement = BitHelper.Sext (machineInstruction[BitField.OFFSET], 16);
		}

		public virtual uint Ea (IThread thread)
		{
			uint ea = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			return ea;
		}

		protected override void SetupDeps ()
		{
			this.MemIDeps = new List<RegisterDependency> ();
			this.MemODeps = new List<RegisterDependency> ();
			
			this.SetupEaDeps ();
			
			this.EaODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_EA));
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_EA));
			
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

		public List<RegisterDependency> MemIDeps { get; protected set; }
		public List<RegisterDependency> MemODeps { get; protected set; }

		public int Displacement { get; private set; }
	}

	public sealed class Lb : MemoryOp
	{
		public Lb (MachineInstruction machineInstruction) : base("lb", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			sbyte mem = (sbyte)thread.Mem.ReadByte(this.Ea(thread));
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)mem;
		}
	}

	public sealed class Lbu : MemoryOp
	{
		public Lbu (MachineInstruction machineInstruction) : base("lbu", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			byte mem = thread.Mem.ReadByte(this.Ea(thread));
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public sealed class Lh : MemoryOp
	{
		public Lh (MachineInstruction machineInstruction) : base("lh", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			short mem = (short)thread.Mem.ReadHalfWord(this.Ea(thread));
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)mem;
		}
	}

	public sealed class Lhu : MemoryOp
	{
		public Lhu (MachineInstruction machineInstruction) : base("lhu", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			ushort mem = thread.Mem.ReadHalfWord(this.Ea(thread));
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public sealed class Lw : MemoryOp
	{
		public Lw (MachineInstruction machineInstruction) : base("lw", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			int mem = (int)thread.Mem.ReadWord(this.Ea(thread));
			thread.Regs.IntRegs[this[BitField.RT]] = (uint)mem;
		}
	}

	public sealed class Lwl : MemoryOp
	{
		public Lwl (MachineInstruction machineInstruction) : base("lwl", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override uint Ea (IThread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		public override void Execute (IThread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = thread.Mem.ReadWord(ea);
			uint mem_shift = 24 - 8 * byte_offset;
			
			uint rt = (mem << (int)mem_shift) | (thread.Regs.IntRegs[this[BitField.RT]] & BitHelper.Mask ((int)mem_shift));
			
			thread.Regs.IntRegs[this[BitField.RT]] = rt;
		}
	}

	public sealed class Lwr : MemoryOp
	{
		public Lwr (MachineInstruction machineInstruction) : base("lwr", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override uint Ea (IThread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		public override void Execute (IThread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = thread.Mem.ReadWord(ea);
			uint mem_shift = 8 * byte_offset;
			
			uint rt = (thread.Regs.IntRegs[this[BitField.RT]] & (BitHelper.Mask ((int)mem_shift) << (int)(32 - mem_shift))) | (mem >> (int)mem_shift);
			
			thread.Regs.IntRegs[this[BitField.RT]] = rt;
		}
	}

	public sealed class Ll : MemoryOp
	{
		public Ll (MachineInstruction machineInstruction) : base("ll", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			uint mem = thread.Mem.ReadWord(this.Ea(thread));
			thread.Regs.IntRegs[this[BitField.RT]] = mem;
		}
	}

	public sealed class Lwc1 : MemoryOp
	{
		public Lwc1 (MachineInstruction machineInstruction) : base("lwc1", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FT]));
		}

		public override void Execute (IThread thread)
		{
			uint mem = thread.Mem.ReadWord(this.Ea(thread));
			thread.Regs.FloatRegs.SetUint (mem, this[BitField.FT]);
		}
	}

	public sealed class Ldc1 : MemoryOp
	{
		public Ldc1 (MachineInstruction machineInstruction) : base("ldc1", machineInstruction, Flag.Memory | Flag.Load | Flag.DisplacedAddressing, FunctionalUnit.Types.ReadPort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FT]));
		}

		public override void Execute (IThread thread)
		{
			ulong mem = thread.Mem.ReadDoubleWord(this.Ea(thread));
			thread.Regs.FloatRegs.SetUlong (mem, this[BitField.FT]);
		}
	}

	public sealed class Sb : MemoryOp
	{
		public Sb (MachineInstruction machineInstruction) : base("sb", machineInstruction, Flag.Memory | Flag.Store | Flag.DisplacedAddressing, FunctionalUnit.Types.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			byte mem = (byte)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RT]], 7, 0);
			thread.Mem.WriteByte (this.Ea (thread), mem);
		}
	}

	public sealed class Sh : MemoryOp
	{
		public Sh (MachineInstruction machineInstruction) : base("sh", machineInstruction, Flag.Memory | Flag.Store | Flag.DisplacedAddressing, FunctionalUnit.Types.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			ushort mem = (ushort)BitHelper.Bits (thread.Regs.IntRegs[this[BitField.RT]], 15, 0);
			thread.Mem.WriteHalfWord (this.Ea (thread), mem);
		}
	}

	public sealed class Sw : MemoryOp
	{
		public Sw (MachineInstruction machineInstruction) : base("sw", machineInstruction, Flag.Memory | Flag.Store | Flag.DisplacedAddressing, FunctionalUnit.Types.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			uint mem = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Mem.WriteWord (this.Ea (thread), mem);
		}
	}

	public sealed class Swl : MemoryOp
	{
		public Swl (MachineInstruction machineInstruction) : base("swl", machineInstruction, Flag.Memory | Flag.Store | Flag.DisplacedAddressing, FunctionalUnit.Types.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override uint Ea (IThread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		public override void Execute (IThread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = thread.Mem.ReadWord(ea);
			uint reg_shift = 24 - 8 * byte_offset;
			uint mem_shift = 32 - reg_shift;
			
			mem = (mem & (BitHelper.Mask ((int)reg_shift) << (int)mem_shift)) | (thread.Regs.IntRegs[this[BitField.RT]] >> (int)reg_shift);
			
			thread.Mem.WriteWord (ea, mem);
		}
	}

	public sealed class Swr : MemoryOp
	{
		public Swr (MachineInstruction machineInstruction) : base("swr", machineInstruction, Flag.Memory | Flag.Store | Flag.DisplacedAddressing, FunctionalUnit.Types.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override uint Ea (IThread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			uint ea = addr & ~3u;
			return ea;
		}

		public override void Execute (IThread thread)
		{
			uint addr = (uint)(thread.Regs.IntRegs[this[BitField.RS]] + this.Displacement);
			
			uint ea = addr & ~3u;
			uint byte_offset = addr & 3;
			
			uint mem = thread.Mem.ReadWord(ea);
			uint reg_shift = 8 * byte_offset;
			
			mem = thread.Regs.IntRegs[this[BitField.RT]] << (int)reg_shift | (mem & (BitHelper.Mask ((int)reg_shift)));
			
			thread.Mem.WriteWord (ea, mem);
		}
	}

	public sealed class Sc : MemoryOp
	{
		public Sc (MachineInstruction machineInstruction) : base("sc", machineInstruction, Flag.Memory | Flag.Store | Flag.DisplacedAddressing, FunctionalUnit.Types.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Mem.WriteWord (this.Ea (thread), rt);
			thread.Regs.IntRegs[this[BitField.RT]] = 1;
		}
	}

	public sealed class Swc1 : MemoryOp
	{
		public Swc1 (MachineInstruction machineInstruction) : base("swc1", machineInstruction, Flag.Memory | Flag.Store | Flag.DisplacedAddressing, FunctionalUnit.Types.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FT]));
		}

		public override void Execute (IThread thread)
		{
			uint ft = thread.Regs.FloatRegs.GetUint (this[BitField.FT]);
			thread.Mem.WriteWord (this.Ea (thread), ft);
		}
	}

	public sealed class Sdc1 : MemoryOp
	{
		public Sdc1 (MachineInstruction machineInstruction) : base("sdc1", machineInstruction, Flag.Memory | Flag.Store | Flag.DisplacedAddressing, FunctionalUnit.Types.WritePort)
		{
		}

		protected override void SetupEaDeps ()
		{
			this.EaIdeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RS]));
		}

		protected override void SetupMemDeps ()
		{
			this.MemIDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FT]));
		}

		public override void Execute (IThread thread)
		{
			ulong ft = thread.Regs.FloatRegs.GetUlong (this[BitField.FT]);
			thread.Mem.WriteDoubleWord (this.Ea (thread), ft);
		}
	}

	public abstract class CP1Control : StaticInstruction
	{
		public CP1Control (string mnemonic, MachineInstruction machineInstruction, Flag flags, FunctionalUnit.Types fuType) : base(mnemonic, machineInstruction, flags, fuType)
		{
		}
	}

	public sealed class Mfc1 : CP1Control
	{
		public Mfc1 (MachineInstruction machineInstruction) : base("mfc1", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FS]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
		{
			uint fs = thread.Regs.FloatRegs.GetUint (this[BitField.FS]);
			thread.Regs.IntRegs[this[BitField.RT]] = fs;
		}
	}

	public sealed class Cfc1 : CP1Control
	{
		public Cfc1 (MachineInstruction machineInstruction) : base("cfc1", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_FCSR));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
		}

		public override void Execute (IThread thread)
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
		public Mtc1 (MachineInstruction machineInstruction) : base("mtc1", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Float, this[BitField.FS]));
		}

		public override void Execute (IThread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			thread.Regs.FloatRegs.SetUint (rt, this[BitField.FS]);
		}
	}

	public sealed class Ctc1 : CP1Control
	{
		public Ctc1 (MachineInstruction machineInstruction) : base("ctc1", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
			this.IDeps.Add (new RegisterDependency (RegisterDependency.Types.Integer, this[BitField.RT]));
			this.ODeps.Add (new RegisterDependency (RegisterDependency.Types.Misc, RegisterConstants.MISC_REG_FCSR));
		}

		public override void Execute (IThread thread)
		{
			uint rt = thread.Regs.IntRegs[this[BitField.RT]];
			
			if (this[BitField.FS] != 0) {
				thread.Regs.MiscRegs.Fcsr = rt;
			}
		}
	}

	public sealed class Nop : StaticInstruction
	{
		public Nop (MachineInstruction machineInstruction) : base("nop", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (IThread thread)
		{
		}
	}

	public class Unimplemented : StaticInstruction
	{
		public Unimplemented (string mnemonic, MachineInstruction machineInstruction) : base(mnemonic, machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (IThread thread)
		{
			Logger.Panicf (Logger.Categories.Instruction, "Unimplemented instruction [machineInstruction: 0x{0:x8}, mnemonic: \"{1:s}] detected @ PC 0x{2:x8}", this.MachineInstruction.Data, this.Mnemonic, thread.Regs.Pc);
		}
	}

	public sealed class FailUnimplemented : Unimplemented
	{
		public FailUnimplemented (string mnemonic, MachineInstruction machineInstruction) : base(mnemonic, machineInstruction)
		{
		}
	}

	public sealed class CP0Unimplemented : Unimplemented
	{
		public CP0Unimplemented (string mnemonic, MachineInstruction machineInstruction) : base(mnemonic, machineInstruction)
		{
		}
	}

	public sealed class CP1Unimplemented : Unimplemented
	{
		public CP1Unimplemented (string mnemonic, MachineInstruction machineInstruction) : base(mnemonic, machineInstruction)
		{
		}
	}

	public sealed class CP2Unimplemented : Unimplemented
	{
		public CP2Unimplemented (string mnemonic, MachineInstruction machineInstruction) : base(mnemonic, machineInstruction)
		{
		}
	}

	public sealed class Unknown : StaticInstruction
	{
		public Unknown (MachineInstruction machineInstruction) : base("unknown", machineInstruction, Flag.None, FunctionalUnit.Types.None)
		{
		}

		protected override void SetupDeps ()
		{
		}

		public override void Execute (IThread thread)
		{
			Logger.Panicf (Logger.Categories.Instruction, "{0:s} detected @ PC 0x{1:x8}", "Unknown instruction", thread.Regs.Pc);
		}
	}
}
