/*
 * Kernel.cs
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
using System.Runtime.InteropServices;
using System.Text;
using MinCai.Simulators.Flexim.Architecture;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.OperatingSystem;
using MinCai.Simulators.Flexim.Microarchitecture;
using Mono.Unix.Native;

namespace MinCai.Simulators.Flexim.OperatingSystem
{
	public sealed class ElfReaderException : Exception
	{
		public ElfReaderException (string message) : base(message)
		{
		}
	}

	public abstract class ElfFormatEntity
	{
		public ElfFormatEntity (ElfFile elfFile)
		{
			this.ElfFile = elfFile;
		}

		public ElfFile ElfFile { get; private set; }

		protected byte[] ReadBytes (BinaryReader reader, int count)
		{
			return reader.ReadBytes (count);
		}

		protected string ReadString (BinaryReader reader, int count)
		{
			StringBuilder str = new StringBuilder ();
			
			byte[] bytes = reader.ReadBytes (count);
			
			for (int i = 0; i < count; i++)
				str.Append ((char)bytes[i]);
			
			return str.ToString ();
		}

		protected ushort ReadElf32Half (BinaryReader reader)
		{
			byte b1 = reader.ReadByte ();
			byte b2 = reader.ReadByte ();
			
			ushort result = 0;
			
			if (ElfFile.Identification.Ei_data == ElfIdentification.Ei_Data.ElfData2Msb) {
				result = (ushort)((b1 << 8) | b2);
			} else {
				result = (ushort)((b2 << 8) | b1);
			}
			
			return result;
		}

		protected byte ReadByte (BinaryReader reader)
		{
			return reader.ReadByte ();
		}

		protected uint ReadElf32Word (BinaryReader reader)
		{
			byte b1 = reader.ReadByte ();
			byte b2 = reader.ReadByte ();
			byte b3 = reader.ReadByte ();
			byte b4 = reader.ReadByte ();
			
			uint result = 0;
			
			if (ElfFile.Identification.Ei_data == ElfIdentification.Ei_Data.ElfData2Msb) {
				result = (uint)(b1 << 24) | (uint)(b2 << 16) | (uint)(b3 << 8) | (uint)(b4);
			} else {
				result = (uint)(b4 << 24) | (uint)(b3 << 16) | (uint)(b2 << 8) | (uint)(b1);
			}
			
			return result;
		}

		protected uint ReadElf32Off (BinaryReader reader)
		{
			return this.ReadElf32Word (reader);
		}

		protected uint ReadElf32Addr (BinaryReader reader)
		{
			return this.ReadElf32Word (reader);
		}
	}

	public sealed class ElfHeader : ElfFormatEntity
	{
		public enum E_Type : ushort
		{
			ET_NONE = 0,
			ET_REL = 1,
			ET_EXEC = 2,
			ET_DYN = 3,
			ET_CORE = 4,
			ET_LOPROC = 0xff00,
			ET_HIPROC = 0xffff
		}

		public enum E_Machine : ushort
		{
			EM_NONE = 0,
			EM_M32 = 1,
			EM_SPARC = 2,
			EM_386 = 3,
			EM_68K = 4,
			EM_88K = 5,
			EM_486 = 6,
			EM_860 = 7,
			EM_MIPS = 8
		}

		public enum E_Version : uint
		{
			EV_NONE = 0,
			EV_CURRENT = 1
		}

		public ElfHeader (ElfFile elfFile) : base(elfFile)
		{
		}

		public void Read ()
		{
			this.E_type = (E_Type)this.ReadElf32Half (this.ElfFile.Reader);
			
			this.E_machine = (E_Machine)this.ReadElf32Half (this.ElfFile.Reader);
			this.E_version = (E_Version)this.ReadElf32Word (this.ElfFile.Reader);
			this.E_entry = this.ReadElf32Addr (this.ElfFile.Reader);
			this.E_phoff = this.ReadElf32Off (this.ElfFile.Reader);
			this.E_shoff = this.ReadElf32Off (this.ElfFile.Reader);
			this.E_flags = this.ReadElf32Word (this.ElfFile.Reader);
			
			this.E_ehsize = this.ReadElf32Half (this.ElfFile.Reader);
			this.E_phentsize = this.ReadElf32Half (this.ElfFile.Reader);
			this.E_phnum = this.ReadElf32Half (this.ElfFile.Reader);
			this.E_shentsize = this.ReadElf32Half (this.ElfFile.Reader);
			this.E_shnum = this.ReadElf32Half (this.ElfFile.Reader);
			this.E_shstrndx = this.ReadElf32Half (this.ElfFile.Reader);
		}

		public override string ToString ()
		{
			return string.Format ("[ElfHeader: E_type={0}, E_machine=0x{1:x8}, E_version={2}, E_entry=0x{3:x8}, E_phoff=0x{4:x8}, E_shoff=0x{5:x8}, E_flags=0x{6:x8}, E_ehsize=0x{7:x8}, E_phentsize=0x{8:x8}, E_phnum={9}, E_shentsize=0x{10:x8}, E_shnum={11}, E_shstrndx=0x{12:x8}]", this.E_type, this.E_machine, this.E_version, this.E_entry, this.E_phoff, this.E_shoff, this.E_flags, this.E_ehsize, this.E_phentsize,
			this.E_phnum, this.E_shentsize, this.E_shnum, this.E_shstrndx);
		}

		public E_Type E_type { get; private set; }
		public E_Machine E_machine { get; private set; }
		public E_Version E_version { get; private set; }
		public uint E_entry { get; private set; }
		public uint E_phoff { get; private set; }
		public uint E_shoff { get; private set; }
		public uint E_flags { get; private set; }
		public ushort E_ehsize { get; private set; }
		public ushort E_phentsize { get; private set; }
		public ushort E_phnum { get; private set; }
		public ushort E_shentsize { get; private set; }
		public ushort E_shnum { get; private set; }
		public ushort E_shstrndx { get; private set; }
	}

	public sealed class ElfIdentification
	{
		public enum Ei_Class : uint
		{
			ElfClassNone,
			ElfClass32,
			ElfClass64
		}

		public enum Ei_Data : uint
		{
			ElfDataNone,
			ElfData2Lsb,
			ElfData2Msb
		}

		public void Read (BinaryReader reader)
		{
			byte[] e_ident = reader.ReadBytes (16);
			
			bool isElfFile = e_ident[0] == 0x7f && e_ident[1] == (byte)'E' && e_ident[2] == (byte)'L' && e_ident[3] == (byte)'F';
			
			if (!isElfFile)
				throw new Exception ();
			
			this.Ei_class = e_ident[4] == 1 ? Ei_Class.ElfClass32 : e_ident[4] == 2 ? Ei_Class.ElfClass64 : Ei_Class.ElfClassNone;
			
			this.Ei_data = e_ident[5] == 1 ? Ei_Data.ElfData2Lsb : e_ident[5] == 2 ? Ei_Data.ElfData2Msb : Ei_Data.ElfDataNone;
			
			this.Ei_version = (int)e_ident[6];
		}

		public override string ToString ()
		{
			return string.Format ("[ElfIdentification: Ei_class={0}, Ei_data={1}, Ei_version={2}]", this.Ei_class, this.Ei_data, this.Ei_version);
		}

		public Ei_Class Ei_class { get; private set; }
		public Ei_Data Ei_data { get; private set; }
		public int Ei_version { get; private set; }
	}

	public sealed class ElfProgramHeader : ElfFormatEntity
	{
		public enum P_Type : uint
		{
			PT_NULL = 0,
			PT_LOAD = 1,
			PT_DYNAMIC = 2,
			PT_INTERP = 3,
			PT_NOTE = 4,
			PT_SHLIB = 5,
			PT_PHDR = 6,
			PT_LOPROC = 0x70000000,
			PT_HIPROC = 0x7fffffff
		}

		public ElfProgramHeader (ElfFile elfFile) : base(elfFile)
		{
		}

		public void Read ()
		{
			this.P_type = (P_Type)this.ReadElf32Word (ElfFile.Reader);
			this.P_offset = this.ReadElf32Off (ElfFile.Reader);
			this.P_vaddr = this.ReadElf32Addr (ElfFile.Reader);
			this.P_paddr = this.ReadElf32Addr (ElfFile.Reader);
			this.P_filesz = this.ReadElf32Word (ElfFile.Reader);
			this.P_memsz = this.ReadElf32Word (ElfFile.Reader);
			this.P_flags = this.ReadElf32Word (ElfFile.Reader);
			this.P_align = this.ReadElf32Word (ElfFile.Reader);
			
			long position = this.ElfFile.Reader.BaseStream.Position;
			
			this.ElfFile.Reader.BaseStream.Seek (P_offset, 0);
			
			this.Content = this.ElfFile.Reader.ReadBytes ((int)P_filesz);
			
			this.ElfFile.Reader.BaseStream.Seek (position, 0);
		}

		public override string ToString ()
		{
			return string.Format ("[ElfProgramHeader: P_type={0}, P_offset=0x{1:x8}, P_vaddr=0x{2:x8}, P_paddr=0x{3:x8}, P_filesz=0x{4:x8}, P_memsz=0x{5:x8}, P_flags=0x{6:x8}, P_align=0x{7:x8}, Content=0x{8:x8}]", this.P_type, this.P_offset, this.P_vaddr, this.P_paddr, this.P_filesz, this.P_memsz, this.P_flags, this.P_align, this.Content);
		}

		public P_Type P_type { get; private set; }
		public uint P_offset { get; private set; }
		public uint P_vaddr { get; private set; }
		public uint P_paddr { get; private set; }
		public uint P_filesz { get; private set; }
		public uint P_memsz { get; private set; }
		public uint P_flags { get; private set; }
		public uint P_align { get; private set; }
		public byte[] Content { get; private set; }
	}

	public sealed class ElfSectionHeader : ElfFormatEntity
	{
		public enum Sh_Type : uint
		{
			SHT_NULL = 0,
			SHT_PROGBITS = 1,
			SHT_SYMTAB = 2,
			SHT_STRTAB = 3,
			SHT_RELA = 4,
			SHT_HASH = 5,
			SHT_DYNAMIC = 6,
			SHT_NOTE = 7,
			SHT_NOBITS = 8,
			SHT_REL = 9,
			SHT_SHLIB = 10,
			SHT_DYNSYM = 11,
			SHT_LOPROC = 0x70000000,
			SHT_HIGPROC = 0x7fffffff,
			SHT_LOUSER = 0x80000000,
			SHT_HIUSER = 0xffffffff
		}

		public enum Sh_Flags : uint
		{
			SHF_WRITE = 0x1,
			SHF_ALLOC = 0x2,
			SHF_EXECINSTR = 0x4
		}

		public ElfSectionHeader (ElfFile elfFile) : base(elfFile)
		{
		}

		public void Read ()
		{
			this.Sh_name = this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_type = (Sh_Type)this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_flags = (Sh_Flags)this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_addr = this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_offset = this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_size = this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_link = this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_info = this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_addralign = this.ReadElf32Word (this.ElfFile.Reader);
			this.Sh_entsize = this.ReadElf32Word (this.ElfFile.Reader);
			
			long position = this.ElfFile.Reader.BaseStream.Position;
			
			this.ElfFile.Reader.BaseStream.Seek (this.Sh_offset, 0);
			
			this.Content = this.ElfFile.Reader.ReadBytes ((int)this.Sh_size);
			
			this.ElfFile.Reader.BaseStream.Seek (position, 0);
		}

		public override string ToString ()
		{
			return string.Format ("[ElfSectionHeader: Name={0}, Sh_type={1}, Sh_flags={2}, Sh_addr=0x{3:x8}, Sh_offset=0x{4:x8}, Sh_size={5}, Sh_link={6}, Sh_info={7}, Sh_addralign={8}, Sh_entsize={9}}]", this.Name, this.Sh_type, this.Sh_flags, this.Sh_addr, this.Sh_offset, this.Sh_size, this.Sh_link, this.Sh_info, this.Sh_addralign,
			this.Sh_entsize);
		}

		public string Name {
			get { return this.ElfFile.GetNameFromMainStringTable (this.Sh_name); }
		}

		public ElfFormatEntity AssociatedEntity { get; set; }

		public uint Sh_name { get; private set; }
		public Sh_Type Sh_type { get; private set; }
		public Sh_Flags Sh_flags { get; private set; }
		public uint Sh_addr { get; private set; }
		public uint Sh_offset { get; private set; }
		public uint Sh_size { get; private set; }
		public uint Sh_link { get; private set; }
		public uint Sh_info { get; private set; }
		public uint Sh_addralign { get; private set; }
		public uint Sh_entsize { get; private set; }

		public byte[] Content { get; private set; }
	}

	public sealed class ElfStringTable : ElfFormatEntity
	{
		public ElfStringTable (ElfSectionHeader section) : base(section.ElfFile)
		{
			this.SectionHeader = section;
			
			if (section.Sh_type != ElfSectionHeader.Sh_Type.SHT_STRTAB)
				throw new ElfReaderException ("Section is not a string table");
		}

		public String GetString (uint index)
		{
			StringBuilder str = new StringBuilder ();
			for (uint i = index; this.RawData[i] != '\0'; i++)
				str.Append ((char)this.RawData[i]);
			
			return str.ToString ();
		}

		public void Read ()
		{
			long position = this.ElfFile.Reader.BaseStream.Position;
			
			this.ElfFile.Reader.BaseStream.Seek (this.SectionHeader.Sh_offset, 0);
			
			this.RawData = this.ElfFile.Reader.ReadBytes ((int)this.SectionHeader.Sh_size);
			
			this.ElfFile.Reader.BaseStream.Seek (position, 0);
		}

		private ElfSectionHeader SectionHeader { get; set; }
		private byte[] RawData { get; set; }
	}

	public sealed class ElfSymbolTable : ElfFormatEntity
	{
		public ElfSymbolTable (ElfSectionHeader section) : base(section.ElfFile)
		{
			this.SectionHeader = section;
			this.Entries = new List<ElfSymbolTableEntry> ();
		}

		public void Read ()
		{
			long position = this.ElfFile.Reader.BaseStream.Position;
			
			this.ElfFile.Reader.BaseStream.Seek (this.SectionHeader.Sh_offset, 0);
			
			uint entryCount = this.SectionHeader.Sh_size / this.SectionHeader.Sh_entsize;
			
			for (int i = 0; i < entryCount; i++) {
				ElfSymbolTableEntry entry = new ElfSymbolTableEntry (this.ElfFile);
				
				entry.Read ();
				
				this.Entries.Add (entry);
			}
			
			this.ElfFile.Reader.BaseStream.Seek (position, 0);
		}

		public ElfSectionHeader SectionHeader { get; private set; }
		public List<ElfSymbolTableEntry> Entries { get; private set; }
	}

	public sealed class ElfSymbolTableEntry : ElfFormatEntity
	{
		public enum SymbolBinding : uint
		{
			STB_LOCAL = 0,
			STB_GLOBAL = 1,
			STB_WEAK = 2,
			STB_LOPROC = 13,
			STB_HIPROC = 15
		}

		public enum SymbolType : uint
		{
			STT_NOTYPE = 0,
			STT_OBJECT = 1,
			STT_FUNC = 2,
			STT_SECTION = 3,
			STT_FILE = 4,
			STT_LOPROC = 13,
			STT_HIPROC = 15
		}

		public ElfSymbolTableEntry (ElfFile elfFile) : base(elfFile)
		{
		}

		public void Read ()
		{
			this.St_name = this.ReadElf32Word (this.ElfFile.Reader);
			this.St_value = this.ReadElf32Addr (this.ElfFile.Reader);
			this.St_size = this.ReadElf32Word (this.ElfFile.Reader);
			this.St_info = this.ReadByte (this.ElfFile.Reader);
			this.St_other = this.ReadByte (this.ElfFile.Reader);
			this.St_shndx = this.ReadElf32Half (this.ElfFile.Reader);
		}

		public override string ToString ()
		{
			return string.Format ("[ElfSymbolTableEntry: Name={0}, Binding={1}, Type={2}, St_value={3}, St_size={4}, St_info={5}, St_other={6}, St_shndx={7}]", this.Name, this.Binding, this.Type, this.St_value, this.St_size, this.St_info, this.St_other, this.St_shndx);
		}

		public string Name {
			get { return this.ElfFile.GetNameFromSymbolStringTable (this.St_name); }
		}

		public SymbolBinding Binding {
			get { return (SymbolBinding)Elf32_St_Bind (this.St_info); }
		}

		public SymbolType Type {
			get { return (SymbolType)Elf32_St_Type (this.St_info); }
		}

		public uint St_name { get; private set; }
		public uint St_value { get; private set; }
		public uint St_size { get; private set; }
		public byte St_info { get; private set; }
		public byte St_other { get; private set; }
		public ushort St_shndx { get; private set; }

		public static uint Elf32_St_Bind (uint i)
		{
			return (i >> 4);
		}

		public static uint Elf32_St_Type (uint i)
		{
			return (i & 0xF);
		}

		public static uint Elf32_St_Info (uint b, uint t)
		{
			return ((b << 4) + (t & 0xF));
		}
	}

	public sealed class ElfFile
	{
		public ElfFile (BinaryReader reader)
		{
			this.Reader = reader;
			this.SectionHeaders = new List<ElfSectionHeader> ();
			this.ProgramHeaders = new List<ElfProgramHeader> ();
		}

		public string GetNameFromMainStringTable (uint index)
		{
			if (this.StringTable == null)
				return "table not set";
			else
				return this.StringTable.GetString (index);
		}

		public String GetNameFromSymbolStringTable (uint index)
		{
			if (this.StringTable == null)
				return "table not set";
			else
				return this.SymbolStringTable.GetString (index);
		}

		public void Read ()
		{
			this.Identification = new ElfIdentification ();
			this.Identification.Read (Reader);
			
			this.Header = new ElfHeader (this);
			this.Header.Read ();
			
			Debug.Assert (this.Identification.Ei_class == ElfIdentification.Ei_Class.ElfClass32, "Only 32 bit binary is supported.");
			Debug.Assert (this.Identification.Ei_data == ElfIdentification.Ei_Data.ElfData2Lsb, "Only little-endian binary is supported..");
			Debug.Assert (this.Header.E_machine == ElfHeader.E_Machine.EM_MIPS, "Only MIPS binary is supported.");
			
			this.Reader.BaseStream.Seek ((long)this.Header.E_shoff, 0);
			
			for (int i = 0; i < this.Header.E_shnum; i++) {
				ElfSectionHeader sectionHeader = new ElfSectionHeader (this);
				sectionHeader.Read ();
				
				this.SectionHeaders.Add (sectionHeader);
				
				if (sectionHeader.Sh_type == ElfSectionHeader.Sh_Type.SHT_SYMTAB) {
					this.SymbolTable = new ElfSymbolTable (sectionHeader);
					this.SymbolTable.Read ();
					
					sectionHeader.AssociatedEntity = this.SymbolTable;
				} else if (sectionHeader.Sh_type == ElfSectionHeader.Sh_Type.SHT_STRTAB) {
					ElfStringTable stringTable = new ElfStringTable (sectionHeader);
					stringTable.Read ();
					
					sectionHeader.AssociatedEntity = stringTable;
				}
			}
			
			this.StringTable = this.SectionHeaders[this.Header.E_shstrndx].AssociatedEntity as ElfStringTable;
			
			foreach (var sectionHeader in this.SectionHeaders) {
				if (sectionHeader.Name == ".strtab") {
					this.SymbolStringTable = sectionHeader.AssociatedEntity as ElfStringTable;
				}
			}
			
			this.Reader.BaseStream.Seek ((long)this.Header.E_phoff, 0);
			
			for (int i = 0; i < this.Header.E_phnum; i++) {
				ElfProgramHeader programHeader = new ElfProgramHeader (this);
				programHeader.Read ();
				
				this.ProgramHeaders.Add (programHeader);
			}
		}

		public BinaryReader Reader { get; private set; }
		public ElfIdentification Identification { get; private set; }
		public ElfHeader Header { get; private set; }
		public List<ElfSectionHeader> SectionHeaders { get; private set; }
		public List<ElfProgramHeader> ProgramHeaders { get; private set; }
		public ElfStringTable StringTable { get; private set; }
		public ElfStringTable SymbolStringTable { get; private set; }
		public ElfSymbolTable SymbolTable { get; private set; }

		public static ElfFile OpenAndProcessFile (string filename)
		{
			FileStream stream = null;
			BinaryReader reader = null;
			ElfFile elfFile = null;
			
			try {
				stream = File.Open (Processor.WorkDirectory + Path.DirectorySeparatorChar + filename, FileMode.Open);
				reader = new BinaryReader (stream);
				
				elfFile = ProcessFile (reader);
			} finally {
				if (reader != null)
					reader.Close ();
				
				if (stream != null)
					stream.Close ();
			}
			
			return elfFile;
		}

		private static ElfFile ProcessFile (BinaryReader reader)
		{
			ElfFile file = new ElfFile (reader);
			file.Read ();
			
			return file;
		}
	}

	public sealed class Process
	{
		public Process (string cwd, List<string> args)
		{
			this.Cwd = cwd;
			this.Args = args;
			
			this.Envs = new List<string> ();
			
			this.Uid = 100;
			this.Euid = 100;
			this.Gid = 100;
			this.Egid = 100;
			
			this.Pid = 100;
			this.Ppid = 99;
		}

		public void Load (Thread thread)
		{
			ElfFile file = ElfFile.OpenAndProcessFile (this.Args[0]);
			this.LoadInternal (thread, file);
		}

		unsafe private void LoadInternal (Thread thread, ElfFile file)
		{
			uint dataBase = 0;
			uint dataSize = 0;
			uint envAddr, argAddr;
			uint stackPtr;
			
			foreach (var phdr in file.ProgramHeaders) {
				if (phdr.P_type == ElfProgramHeader.P_Type.PT_LOAD && phdr.P_vaddr > dataBase) {
					dataBase = phdr.P_vaddr;
					dataSize = phdr.P_memsz;
				}
			}
			
			foreach (var shdr in file.SectionHeaders) {
				if (shdr.Sh_type == ElfSectionHeader.Sh_Type.SHT_PROGBITS || shdr.Sh_type == ElfSectionHeader.Sh_Type.SHT_NOBITS) {
					if (shdr.Sh_size > 0 && ((shdr.Sh_flags & ElfSectionHeader.Sh_Flags.SHF_ALLOC) != 0)) {
//						Logger.Infof (LogCategory.PROCESS, "Loading {0:s} ({1:d} bytes) at address 0x{2:x8}", shdr.Name, shdr.Sh_size, shdr.Sh_addr);
						
						MemoryAccessType perm = MemoryAccessType.Init | MemoryAccessType.Read;
						
						if ((shdr.Sh_flags & ElfSectionHeader.Sh_Flags.SHF_WRITE) != 0) {
							perm |= MemoryAccessType.Write;
						}
						
						if ((shdr.Sh_flags & ElfSectionHeader.Sh_Flags.SHF_EXECINSTR) != 0) {
							perm |= MemoryAccessType.Execute;
						}
						
						thread.Mem.Map (shdr.Sh_addr, (int)shdr.Sh_size, perm);
						
						if (shdr.Sh_type == ElfSectionHeader.Sh_Type.SHT_NOBITS) {
							thread.Mem.Zero (shdr.Sh_addr, (int)shdr.Sh_size);
						} else {
							fixed (byte* buf = &shdr.Content[0]) {
								thread.Mem.InitBlock (shdr.Sh_addr, shdr.Sh_size, buf);
							}
						}
					}
				} else if (shdr.Sh_type == ElfSectionHeader.Sh_Type.SHT_DYNAMIC || shdr.Sh_type == ElfSectionHeader.Sh_Type.SHT_DYNSYM) {
					Logger.Fatal (LogCategory.Elf, "dynamic linking is not supported");
				}
			}
			
			this.ProgramEntry = file.Header.E_entry;
			
			const uint STACK_BASE = 0xc0000000;
			const uint MMAP_BASE = 0xd4000000;
			const uint MAX_ENVIRON = (16 * 1024);
			const uint STACK_SIZE = (1024 * 1024);
			
			thread.Mem.Map (STACK_BASE - STACK_SIZE, (int)STACK_SIZE, MemoryAccessType.Read | MemoryAccessType.Write);
			thread.Mem.Zero (STACK_BASE - STACK_SIZE, (int)STACK_SIZE);
			
			stackPtr = STACK_BASE - MAX_ENVIRON;
			
			thread.Regs.IntRegs[RegisterConstants.STACK_POINTER_REG] = stackPtr;
			
			thread.Mem.WriteWord (stackPtr, this.Argc);
			thread.SetSyscallArg (0, this.Argc);
			stackPtr += (uint)Marshal.SizeOf (typeof(uint));
			
			argAddr = stackPtr;
			thread.SetSyscallArg (1, argAddr);
			stackPtr += (uint)((this.Argc + 1) * Marshal.SizeOf (typeof(uint)));
			
			envAddr = stackPtr;
			stackPtr += (uint)(this.Envs.Count * Marshal.SizeOf (typeof(uint)) + Marshal.SizeOf (typeof(uint)));
			
			for (int i = 0; i < this.Argc; i++) {
				thread.Mem.WriteWord ((uint)(argAddr + i * Marshal.SizeOf (typeof(uint))), stackPtr);
				
				char* arg = (char*)Marshal.StringToHGlobalAnsi (this.Args[i]);
				thread.Mem.WriteString (stackPtr, arg);
				Marshal.FreeHGlobal ((IntPtr)arg);
				
				stackPtr += (uint)(PtrHelper.Strlen (arg) + 1);
			}
			
			for (int i = 0; i < this.Envs.Count; i++) {
				thread.Mem.WriteWord ((uint)(envAddr + i * Marshal.SizeOf (typeof(uint))), stackPtr);
				
				char* e = (char*)Marshal.StringToHGlobalAnsi (this.Envs[i]);
				thread.Mem.WriteString (stackPtr, e);
				Marshal.FreeHGlobal ((IntPtr)e);
				
				stackPtr += (uint)(PtrHelper.Strlen (e) + 1);
			}
			
			if (stackPtr + Marshal.SizeOf (typeof(uint)) >= STACK_BASE) {
				Logger.Fatal (LogCategory.Process, "Environment overflow. Need to increase MAX_ENVIRON.");
			}
			
			uint abrk = dataBase + dataSize + MemoryConstants.PAGE_SIZE;
			abrk -= abrk % MemoryConstants.PAGE_SIZE;
			
			this.Brk = abrk;
			
			this.MmapBrk = MMAP_BASE;
			
			thread.Regs.Npc = this.ProgramEntry;
			thread.Regs.Nnpc = (uint)(thread.Regs.Npc + Marshal.SizeOf (typeof(uint)));
		}

		public string Cwd { get; private set; }
		public List<string> Args { get; private set; }

		private uint Argc {
			get { return (uint)this.Args.Count; }
		}

		unsafe public List<string> Envs { get; private set; }

		public uint Brk { get; set; }
		public uint MmapBrk { get; private set; }
		public uint ProgramEntry { get; private set; }

		public uint Uid { get; private set; }
		public uint Euid { get; private set; }
		public uint Gid { get; private set; }
		public uint Egid { get; private set; }
		public uint Pid { get; private set; }
		public uint Ppid { get; private set; }
	}

	public static class SyscallEmulation
	{
		private enum TargetOpenFlags : int
		{
			O_RDONLY = 0,
			O_WRONLY = 1,
			O_RDWR = 2,
			O_CREAT = 0x100,
			O_EXCL = 0x400,
			O_NOCTTY = 0x800,
			O_TRUNC = 0x200,
			O_APPEND = 8,
			O_NONBLOCK = 0x80,
			O_SYNC = 0x10
		}

		private sealed class OpenFlagMapping
		{
			public OpenFlagMapping (int targetFlag, int hostFlag)
			{
				this.TargetFlag = targetFlag;
				this.HostFlag = hostFlag;
			}

			public int TargetFlag { get; private set; }
			public int HostFlag { get; private set; }
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private struct Utsname
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string sysname;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string nodename;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string release;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string version;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string machine;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
			public string extraJustInCase;
		}

		private sealed class SyscallDesc
		{
			public SyscallDesc (string name, uint num) : this(name, num, null)
			{
			}

			public SyscallDesc (string name, uint num, Func<SyscallDesc, Thread, int> action)
			{
				this.Name = name;
				this.Num = num;
				this.Action = action;
			}

			public void DoSyscall (Thread thread)
			{
				if (this.Action == null) {
					Logger.Fatalf (LogCategory.Syscall, "syscall {0:s} has not been implemented yet.", this.Name);
				}
				
				int retVal = this.Action (this, thread);
				thread.SetSyscallReturn (retVal);
			}

			public string Name { get; set; }
			public uint Num { get; set; }
			public Func<SyscallDesc, Thread, int> Action { get; set; }
		}

		static SyscallEmulation ()
		{
			OpenFlagMappings = new List<OpenFlagMapping> ();
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_RDONLY, (int)OpenFlags.O_RDONLY));
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_WRONLY, (int)OpenFlags.O_WRONLY));
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_RDWR, (int)OpenFlags.O_RDWR));
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_APPEND, (int)OpenFlags.O_APPEND));
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_SYNC, (int)OpenFlags.O_SYNC));
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_CREAT, (int)OpenFlags.O_CREAT));
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_TRUNC, (int)OpenFlags.O_TRUNC));
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_EXCL, (int)OpenFlags.O_EXCL));
			OpenFlagMappings.Add (new OpenFlagMapping ((int)TargetOpenFlags.O_NOCTTY, (int)OpenFlags.O_NOCTTY));
			OpenFlagMappings.Add (new OpenFlagMapping (0x2000, 0));
			
			SyscallDescs = new Dictionary<uint, SyscallDesc> ();
			
			InitSyscallDescs ();
		}

		private static void InitSyscallDescs ()
		{
			uint index = 0;
			
			Register ("syscall", index++);
			Register ("exit", index++, ExitImpl);
			Register ("fork", index++, InvalidArgImpl);
			Register ("read", index++, ReadImpl);
			Register ("write", index++, WriteImpl);
			Register ("open", index++, OpenImpl);
			Register ("close", index++, CloseImpl);
			Register ("waitpid", index++, InvalidArgImpl);
			Register ("creat", index++, InvalidArgImpl);
			Register ("link", index++, InvalidArgImpl);
			Register ("unlink", index++, InvalidArgImpl);
			Register ("execve", index++, InvalidArgImpl);
			Register ("chdir", index++, InvalidArgImpl);
			Register ("time", index++, InvalidArgImpl);
			Register ("mknod", index++, InvalidArgImpl);
			Register ("chmod", index++, InvalidArgImpl);
			Register ("lchown", index++, InvalidArgImpl);
			Register ("break", index++, InvalidArgImpl);
			Register ("oldstat", index++, InvalidArgImpl);
			Register ("lseek", index++, LseekImpl);
			Register ("getpid", index++, GetpidImpl);
			Register ("mount", index++, InvalidArgImpl);
			Register ("umount", index++, InvalidArgImpl);
			Register ("setuid", index++, InvalidArgImpl);
			Register ("getuid", index++, GetuidImpl);
			Register ("stime", index++, InvalidArgImpl);
			Register ("ptrace", index++, InvalidArgImpl);
			Register ("alarm", index++, InvalidArgImpl);
			Register ("oldfstat", index++, InvalidArgImpl);
			Register ("pause", index++, InvalidArgImpl);
			Register ("utime", index++, InvalidArgImpl);
			Register ("stty", index++, InvalidArgImpl);
			Register ("gtty", index++, InvalidArgImpl);
			Register ("access", index++, InvalidArgImpl);
			Register ("nice", index++, InvalidArgImpl);
			Register ("ftime", index++, InvalidArgImpl);
			Register ("sync", index++, InvalidArgImpl);
			Register ("kill", index++, InvalidArgImpl);
			Register ("rename", index++, InvalidArgImpl);
			Register ("mkdir", index++, InvalidArgImpl);
			Register ("rmdir", index++, InvalidArgImpl);
			Register ("dup", index++, InvalidArgImpl);
			Register ("pipe", index++, InvalidArgImpl);
			Register ("times", index++, InvalidArgImpl);
			Register ("prof", index++, InvalidArgImpl);
			Register ("brk", index++, BrkImpl);
			Register ("setgid", index++, InvalidArgImpl);
			Register ("getgid", index++, GetgidImpl);
			Register ("signal", index++, InvalidArgImpl);
			Register ("geteuid", index++, GeteuidImpl);
			Register ("getegid", index++, GetegidImpl);
			Register ("acct", index++, InvalidArgImpl);
			Register ("umount2", index++, InvalidArgImpl);
			Register ("lock", index++, InvalidArgImpl);
			Register ("ioctl", index++, InvalidArgImpl);
			Register ("fcntl", index++, InvalidArgImpl);
			Register ("mpx", index++, InvalidArgImpl);
			Register ("setpgid", index++, InvalidArgImpl);
			Register ("ulimit", index++, InvalidArgImpl);
			Register ("oldolduname", index++, InvalidArgImpl);
			Register ("umask", index++, InvalidArgImpl);
			Register ("chroot", index++, InvalidArgImpl);
			Register ("ustat", index++, InvalidArgImpl);
			Register ("dup2", index++, InvalidArgImpl);
			Register ("getppid", index++, InvalidArgImpl);
			Register ("getpgrp", index++, InvalidArgImpl);
			Register ("setsid", index++, InvalidArgImpl);
			Register ("sigaction", index++, InvalidArgImpl);
			Register ("sgetmask", index++, InvalidArgImpl);
			Register ("ssetmask", index++, InvalidArgImpl);
			Register ("setreuid", index++, InvalidArgImpl);
			Register ("setregid", index++, InvalidArgImpl);
			Register ("sigsuspend", index++, InvalidArgImpl);
			Register ("sigpending", index++, InvalidArgImpl);
			Register ("sethostname", index++, InvalidArgImpl);
			Register ("setrlimit", index++, InvalidArgImpl);
			Register ("getrlimit", index++, InvalidArgImpl);
			Register ("getrusage", index++, InvalidArgImpl);
			Register ("gettimeofday", index++, InvalidArgImpl);
			Register ("settimeofday", index++, InvalidArgImpl);
			Register ("getgroups", index++, InvalidArgImpl);
			Register ("setgroups", index++, InvalidArgImpl);
			Register ("select", index++, InvalidArgImpl);
			Register ("symlink", index++, InvalidArgImpl);
			Register ("oldlstat", index++, InvalidArgImpl);
			Register ("readlink", index++, InvalidArgImpl);
			Register ("uselib", index++, InvalidArgImpl);
			Register ("swapon", index++, InvalidArgImpl);
			Register ("reboot", index++, InvalidArgImpl);
			Register ("readdir", index++, InvalidArgImpl);
			Register ("mmap", index++, InvalidArgImpl);
			Register ("munmap", index++, InvalidArgImpl);
			Register ("truncate", index++, InvalidArgImpl);
			Register ("ftruncate", index++, InvalidArgImpl);
			Register ("fchmod", index++, InvalidArgImpl);
			Register ("fchown", index++, InvalidArgImpl);
			Register ("getpriority", index++, InvalidArgImpl);
			Register ("setpriority", index++, InvalidArgImpl);
			Register ("profil", index++, InvalidArgImpl);
			Register ("statfs", index++, InvalidArgImpl);
			Register ("fstatfs", index++, InvalidArgImpl);
			Register ("ioperm", index++, InvalidArgImpl);
			Register ("socketcall", index++, InvalidArgImpl);
			Register ("syslog", index++, InvalidArgImpl);
			Register ("setitimer", index++, InvalidArgImpl);
			Register ("getitimer", index++, InvalidArgImpl);
			Register ("stat", index++, InvalidArgImpl);
			Register ("lstat", index++, InvalidArgImpl);
			Register ("fstat", index++, FstatImpl);
			Register ("olduname", index++, InvalidArgImpl);
			Register ("iopl", index++, InvalidArgImpl);
			Register ("vhangup", index++, InvalidArgImpl);
			Register ("idle", index++, InvalidArgImpl);
			Register ("vm86old", index++, InvalidArgImpl);
			Register ("wait4", index++, InvalidArgImpl);
			Register ("swapoff", index++, InvalidArgImpl);
			Register ("sysinfo", index++, InvalidArgImpl);
			Register ("ipc", index++, InvalidArgImpl);
			Register ("fsync", index++, InvalidArgImpl);
			Register ("sigreturn", index++, InvalidArgImpl);
			Register ("clone", index++, InvalidArgImpl);
			Register ("setdomainname", index++, InvalidArgImpl);
			Register ("uname", index++, UnameImpl);
			Register ("modify_ldt", index++, InvalidArgImpl);
			Register ("adjtimex", index++, InvalidArgImpl);
			Register ("mprotect", index++, InvalidArgImpl);
			Register ("sigprocmask", index++, InvalidArgImpl);
			Register ("create_module", index++, InvalidArgImpl);
			Register ("init_module", index++, InvalidArgImpl);
			Register ("delete_module", index++, InvalidArgImpl);
			Register ("get_kernel_syms", index++, InvalidArgImpl);
			Register ("quotactl", index++, InvalidArgImpl);
			Register ("getpgid", index++, InvalidArgImpl);
			Register ("fchdir", index++, InvalidArgImpl);
			Register ("bdflush", index++, InvalidArgImpl);
			Register ("sysfs", index++, InvalidArgImpl);
			Register ("personality", index++, InvalidArgImpl);
			Register ("afs_syscall", index++, InvalidArgImpl);
			Register ("setfsuid", index++, InvalidArgImpl);
			Register ("setfsgid", index++, InvalidArgImpl);
			Register ("_llseek", index++, LlseekImpl);
			Register ("getdents", index++, InvalidArgImpl);
			Register ("_newselect", index++, InvalidArgImpl);
			Register ("flock", index++, InvalidArgImpl);
			Register ("msync", index++, InvalidArgImpl);
			Register ("readv", index++, InvalidArgImpl);
			Register ("writev", index++, InvalidArgImpl);
			Register ("getsid", index++, InvalidArgImpl);
			Register ("fdatasync", index++, InvalidArgImpl);
			Register ("_sysctl", index++, InvalidArgImpl);
			Register ("mlock", index++, InvalidArgImpl);
			Register ("munlock", index++, InvalidArgImpl);
			Register ("mlockall", index++, InvalidArgImpl);
			Register ("munlockall", index++, InvalidArgImpl);
			Register ("sched_setparam", index++, InvalidArgImpl);
			Register ("sched_getparam", index++, InvalidArgImpl);
			Register ("sched_setscheduler", index++, InvalidArgImpl);
			Register ("sched_getscheduler", index++, InvalidArgImpl);
			Register ("sched_yield", index++, InvalidArgImpl);
			Register ("sched_get_priority_max", index++, InvalidArgImpl);
			Register ("sched_get_priority_min", index++, InvalidArgImpl);
			Register ("sched_rr_get_interval", index++, InvalidArgImpl);
			Register ("nanosleep", index++, InvalidArgImpl);
			Register ("mremap", index++, InvalidArgImpl);
			Register ("setresuid", index++, InvalidArgImpl);
			Register ("getresuid", index++, InvalidArgImpl);
			Register ("vm86", index++, InvalidArgImpl);
			Register ("query_module", index++, InvalidArgImpl);
			Register ("poll", index++, InvalidArgImpl);
			Register ("nfsservctl", index++, InvalidArgImpl);
			Register ("setresgid", index++, InvalidArgImpl);
			Register ("getresgid", index++, InvalidArgImpl);
			Register ("prctl", index++, InvalidArgImpl);
			Register ("rt_sigreturn", index++, InvalidArgImpl);
			Register ("rt_sigaction", index++, InvalidArgImpl);
			Register ("rt_sigprocmask", index++, InvalidArgImpl);
			Register ("rt_sigpending", index++, InvalidArgImpl);
			Register ("rt_sigtimedwait", index++, InvalidArgImpl);
			Register ("rt_sigqueueinfo", index++, InvalidArgImpl);
			Register ("rt_sigsuspend", index++, InvalidArgImpl);
			Register ("pread", index++, InvalidArgImpl);
			Register ("pwrite", index++, InvalidArgImpl);
			Register ("chown", index++, InvalidArgImpl);
			Register ("getcwd", index++, InvalidArgImpl);
			Register ("capget", index++, InvalidArgImpl);
			Register ("capset", index++, InvalidArgImpl);
			Register ("sigalstack", index++, InvalidArgImpl);
			Register ("sendfile", index++, InvalidArgImpl);
			Register ("getpmsg", index++, InvalidArgImpl);
			Register ("putpmsg", index++, InvalidArgImpl);
			Register ("vfork", index++, InvalidArgImpl);
			Register ("ugetrlimit", index++, InvalidArgImpl);
			Register ("mmap2", index++, InvalidArgImpl);
			Register ("truncate64", index++, InvalidArgImpl);
			Register ("ftruncate64", index++, InvalidArgImpl);
			Register ("stat64", index++, InvalidArgImpl);
			Register ("lstat64", index++, InvalidArgImpl);
			Register ("fstat64", index++, InvalidArgImpl);
			Register ("lchown32", index++, InvalidArgImpl);
			Register ("getuid32", index++, InvalidArgImpl);
			Register ("getgid32", index++, InvalidArgImpl);
			Register ("geteuid32", index++, InvalidArgImpl);
			Register ("getegid32", index++, InvalidArgImpl);
			Register ("setreuid32", index++, InvalidArgImpl);
			Register ("setregid32", index++, InvalidArgImpl);
			Register ("getgroups32", index++, InvalidArgImpl);
			Register ("setgroups32", index++, InvalidArgImpl);
			Register ("fchown32", index++, InvalidArgImpl);
			Register ("setresuid32", index++, InvalidArgImpl);
			Register ("getresuid32", index++, InvalidArgImpl);
			Register ("setresgid32", index++, InvalidArgImpl);
			Register ("getresgid32", index++, InvalidArgImpl);
			Register ("chown32", index++, InvalidArgImpl);
			Register ("setuid32", index++, InvalidArgImpl);
			Register ("setgid32", index++, InvalidArgImpl);
			Register ("setfsuid32", index++, InvalidArgImpl);
			Register ("setfsgid32", index++, InvalidArgImpl);
			Register ("pivot_root", index++, InvalidArgImpl);
			Register ("mincore", index++, InvalidArgImpl);
			Register ("madvise", index++, InvalidArgImpl);
			Register ("getdents64", index++, InvalidArgImpl);
			Register ("fcntl64", index++, InvalidArgImpl);
		}

		private static void Register (string name, uint num)
		{
			Register (new SyscallDesc (name, num));
		}

		private static void Register (string name, uint num, Func<SyscallDesc, Thread, int> action)
		{
			Register (new SyscallDesc (name, num, action));
		}

		private static void Register (SyscallDesc desc)
		{
			SyscallDescs[desc.Num] = desc;
		}

		public static void DoSyscall (uint callNum, Thread thread)
		{
			int syscallIndex = (int)(callNum - 4000);
			
			if (syscallIndex >= 0 && syscallIndex < SyscallDescs.Count && SyscallDescs.ContainsKey ((uint)syscallIndex)) {
				SyscallDescs[(uint)syscallIndex].DoSyscall (thread);
			} else {
				Logger.Warnf (LogCategory.Syscall, "Syscall {0:d} ({1:d}) out of range", callNum, syscallIndex);
				thread.SetSyscallReturn ((-(int)Errno.EINVAL));
			}
		}

		private static int ExitImpl (SyscallDesc desc, Thread thread)
		{
			Console.WriteLine ("exiting...");
			thread.Halt ((int)(thread.GetSyscallArg (0) & 0xff));
			return 1;
		}

		unsafe private static int ReadImpl (SyscallDesc desc, Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			uint bufAddr = thread.GetSyscallArg (1);
			uint count = thread.GetSyscallArg (2);
			
			IntPtr buf = Syscall.malloc (count);
			
			int ret = (int)Syscall.read (fd, buf, count);
			if (ret > 0) {
				thread.Mem.WriteBlock (bufAddr, (uint)ret, (byte*)buf);
			}
			
			Syscall.free (buf);
			
			return ret;
		}

		unsafe private static int WriteImpl (SyscallDesc desc, Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			uint bufAddr = thread.GetSyscallArg (1);
			uint count = thread.GetSyscallArg (2);
			
			IntPtr buf = Syscall.malloc (count);
			
			thread.Mem.ReadBlock (bufAddr, (int)count, (byte*)buf);
			int ret = (int)Syscall.write (fd, buf, count);
			
			Syscall.free (buf);
			
			return ret;
		}

		unsafe private static int OpenImpl (SyscallDesc desc, Thread thread)
		{
			char[] path = new char[MAX_BUFFER_SIZE];
			
			uint addr = thread.GetSyscallArg (0);
			uint tgtFlags = thread.GetSyscallArg (1);
			uint mode = thread.GetSyscallArg (2);
			
			fixed (char* pathPtr = &path[0]) {
				thread.Mem.ReadString (addr, (int)MAX_BUFFER_SIZE, pathPtr);
			}
			
			int hostFlags = 0;
			foreach (var mapping in OpenFlagMappings) {
				if ((tgtFlags & mapping.TargetFlag) != 0) {
					tgtFlags &= (uint)(~mapping.TargetFlag);
					hostFlags |= mapping.HostFlag;
				}
			}
			
			if (tgtFlags != 0) {
				Logger.Fatalf (LogCategory.Syscall, "Syscall: open: cannot decode flags 0x{0:x8}", tgtFlags);
			}
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (path);
			
			int fd = (int)Syscall.open (sb.ToString (), (OpenFlags)hostFlags, (FilePermissions)mode);
			
			return fd;
		}

		private static int CloseImpl (SyscallDesc desc, Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			int ret = (int)Syscall.close (fd);
			return ret;
		}

		private static int LseekImpl (SyscallDesc desc, Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			int offset = (int)thread.GetSyscallArg (1);
			int whence = (int)thread.GetSyscallArg (2);
			
			int ret = (int)Syscall.lseek (fd, offset, (SeekFlags)whence);
			return ret;
		}

		private static int GetpidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Pid;
		}

		private static int GetuidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Uid;
		}

		private static int BrkImpl (SyscallDesc desc, Thread thread)
		{
			uint newbrk = thread.GetSyscallArg (0);
			uint oldbrk = thread.Process.Brk;
			
			if (newbrk == 0) {
				return (int)thread.Process.Brk;
			}
			
			uint newbrkRnd = BitHelper.RoundUp (newbrk, MemoryConstants.PAGE_SIZE);
			uint oldbrkRnd = BitHelper.RoundUp (oldbrk, MemoryConstants.PAGE_SIZE);
			
			if (newbrk > oldbrk) {
				thread.Mem.Map (oldbrkRnd, (int)(newbrkRnd - oldbrkRnd), MemoryAccessType.Read | MemoryAccessType.Write);
			} else if (newbrk < oldbrk) {
				thread.Mem.Unmap (newbrkRnd, (int)(oldbrkRnd - newbrkRnd));
			}
			
			thread.Process.Brk = newbrk;
			
			return (int)thread.Process.Brk;
		}

		private static int GetgidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Gid;
		}

		private static int GeteuidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Euid;
		}

		private static int GetegidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Egid;
		}

		unsafe private static int FstatImpl (SyscallDesc desc, Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			uint bufAddr = thread.GetSyscallArg (1);
			Mono.Unix.Native.Stat buf = new Mono.Unix.Native.Stat ();
			int ret = Syscall.fstat (fd, out buf);
			if (ret >= 0) {
				thread.Mem.WriteBlock (bufAddr, (uint)Marshal.SizeOf (typeof(Mono.Unix.Native.Stat)), (byte*)&buf);
			}
			return ret;
		}

		unsafe private static int UnameImpl (SyscallDesc desc, Thread thread)
		{
			Utsname un = new Utsname ();
			un.sysname = "Linux";
			un.nodename = "sim";
			un.release = "2.6";
			un.version = "Tue Apr 5 12:21:57 UTC 2005";
			un.machine = "mips";
			
			IntPtr unPtr = Marshal.AllocHGlobal (Marshal.SizeOf (typeof(Utsname)));
			Marshal.StructureToPtr (un, unPtr, false);
			
			thread.Mem.WriteBlock (thread.GetSyscallArg (0), (uint)Marshal.SizeOf (typeof(Utsname)), (byte*)unPtr);
			
			Marshal.FreeHGlobal (unPtr);
			
			return 0;
		}

		private static int LlseekImpl (SyscallDesc desc, Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			uint offsetHigh = thread.GetSyscallArg (1);
			uint offsetLow = thread.GetSyscallArg (2);
			uint resultAddr = thread.GetSyscallArg (3);
			int whence = (int)thread.GetSyscallArg (4);
			
			int ret;
			
			if (offsetHigh == 0) {
				long lseekRet = Syscall.lseek (fd, (long)offsetLow, (SeekFlags)whence);
				if (lseekRet >= 0) {
					thread.Mem.WriteDoubleWord (resultAddr, (ulong)lseekRet);
					ret = 0;
				} else {
					ret = -1;
				}
			} else {
				ret = -1;
			}
			
			return ret;
		}

		private static int InvalidArgImpl (SyscallDesc desc, Thread thread)
		{
			Logger.Warnf (LogCategory.Syscall, "syscall {0:s} is ignored.", desc.Name);
			return -(int)Errno.EINVAL;
		}

		private static List<OpenFlagMapping> OpenFlagMappings;
		private static Dictionary<uint, SyscallDesc> SyscallDescs { get; set; }

		private static uint MAX_BUFFER_SIZE = 1024;
	}
}
