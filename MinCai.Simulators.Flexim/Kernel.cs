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
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.OperatingSystem;
using MinCai.Simulators.Flexim.Pipelines;
using Mono.Unix.Native;

namespace MinCai.Simulators.Flexim.OperatingSystem
{
	public class ElfReaderException : Exception
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

	public class ElfHeader : ElfFormatEntity
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

	public class ElfIdentification
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

	public class ElfProgramHeader : ElfFormatEntity
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

	public class ElfSectionHeader : ElfFormatEntity
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

	public class ElfStringTable : ElfFormatEntity
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
		public byte[] RawData { get; private set; }
	}

	public class ElfSymbolTable : ElfFormatEntity
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

	public class ElfSymbolTableEntry : ElfFormatEntity
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
			return string.Format ("[ElfSymbolTableEntry: Name={0}, Binding={1}, Type={2}, St_value={3}, St_size={4}, St_info={5}, St_other={6}, St_shndx={7}]",
				this.Name, this.Binding, this.Type, this.St_value, this.St_size, this.St_info, this.St_other, this.St_shndx);
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

	public class ElfFile
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
			
			foreach (ElfSectionHeader sectionHeader in this.SectionHeaders) {
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
		public ElfStringTable StringTable { get; set; }
		public ElfStringTable SymbolStringTable { get; set; }
		public ElfSymbolTable SymbolTable { get; set; }

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
	
	public static class LinkerConstants
	{
		public static uint LD_STACK_BASE = 0xc0000000;
		public static uint LD_MAX_ENVIRON = 0x40000;
		public static uint LD_STACK_SIZE = 0x100000;
	}

	public class Process
	{
		public Process (string cwd, List<string> args)
		{
			this.Cwd = cwd;
			this.Args = args;
			
			this.Env = new List<string> ();
			
			this.Uid = 100;
			this.Euid = 100;
			this.Gid = 100;
			this.Egid = 100;
			
			this.Pid = 100;
			this.Ppid = 99;
		}

		public bool Load (Thread thread)
		{
			ElfFile file = ElfFile.OpenAndProcessFile (this.Args[0]);
			this.LoadInternal (thread, file);
			
			return true;
		}

		unsafe private void LoadInternal (Thread thread, ElfFile file)
		{
			uint dataBase = 0;
			uint dataSize = 0;
			uint envAddr, argAddr;
			uint stackPtr;
			
			foreach (ElfProgramHeader phdr in file.ProgramHeaders) {
				if (phdr.P_type == ElfProgramHeader.P_Type.PT_LOAD && phdr.P_vaddr > dataBase) {
					dataBase = phdr.P_vaddr;
					dataSize = phdr.P_memsz;
				}
			}
			
			foreach (ElfSectionHeader shdr in file.SectionHeaders) {
				if (shdr.Sh_type == ElfSectionHeader.Sh_Type.SHT_PROGBITS || shdr.Sh_type == ElfSectionHeader.Sh_Type.SHT_NOBITS) {
					if (shdr.Sh_size > 0 && ((shdr.Sh_flags & ElfSectionHeader.Sh_Flags.SHF_ALLOC) != 0)) {
//						Logger.Infof (LogCategory.PROCESS, "Loading {0:s} ({1:d} bytes) at address 0x{2:x8}", shdr.Name, shdr.Sh_size, shdr.Sh_addr);
						
						MemoryAccessType perm = MemoryAccessType.INIT | MemoryAccessType.READ;
						
						if ((shdr.Sh_flags & ElfSectionHeader.Sh_Flags.SHF_WRITE) != 0) {
							perm |= MemoryAccessType.WRITE;
						}
						
						if ((shdr.Sh_flags & ElfSectionHeader.Sh_Flags.SHF_EXECINSTR) != 0) {
							perm |= MemoryAccessType.EXEC;
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
					Logger.Fatal (LogCategory.ELF, "dynamic linking is not supported");
				}
			}
			
			this.ProgEntry = file.Header.E_entry;
			
			const uint STACK_BASE = 0xc0000000;
			const uint MMAP_BASE = 0xd4000000;
			const uint MAX_ENVIRON = (16 * 1024);
			const uint STACK_SIZE = (1024 * 1024);
			
			thread.Mem.Map (STACK_BASE - STACK_SIZE, (int)STACK_SIZE, MemoryAccessType.READ | MemoryAccessType.WRITE);
			thread.Mem.Zero (STACK_BASE - STACK_SIZE, (int)STACK_SIZE);
			
			stackPtr = STACK_BASE - MAX_ENVIRON;
			
			thread.Regs.IntRegs[RegisterConstants.StackPointerReg] = stackPtr;
			
			thread.Mem.WriteWord (stackPtr, this.Argc);
			thread.SetSyscallArg (0, this.Argc);
			stackPtr += (uint)Marshal.SizeOf (typeof(uint));
			
			argAddr = stackPtr;
			thread.SetSyscallArg (1, argAddr);
			stackPtr += (uint)((this.Argc + 1) * Marshal.SizeOf (typeof(uint)));
			
			envAddr = stackPtr;
			stackPtr += (uint)(this.Env.Count * Marshal.SizeOf (typeof(uint)) + Marshal.SizeOf (typeof(uint)));
			
			for (int i = 0; i < this.Argc; i++) {
				thread.Mem.WriteWord ((uint)(argAddr + i * Marshal.SizeOf (typeof(uint))), stackPtr);
				
				char* arg = (char*)Marshal.StringToHGlobalAnsi (this.Args[i]);
				thread.Mem.WriteString (stackPtr, arg);
				Marshal.FreeHGlobal ((IntPtr)arg);
				
				stackPtr += (uint)(PtrUtils.Strlen (arg) + 1);
			}
			
			for (int i = 0; i < this.Env.Count; i++) {
				thread.Mem.WriteWord ((uint)(envAddr + i * Marshal.SizeOf (typeof(uint))), stackPtr);
				
				char* e = (char*)Marshal.StringToHGlobalAnsi (this.Env[i]);
				thread.Mem.WriteString (stackPtr, e);
				Marshal.FreeHGlobal ((IntPtr)e);
				
				stackPtr += (uint)(PtrUtils.Strlen (e) + 1);
			}
			
			if (stackPtr + Marshal.SizeOf (typeof(uint)) >= STACK_BASE) {
				Logger.Fatal (LogCategory.PROCESS, "Environment overflow. Need to increase MAX_ENVIRON.");
			}
			
			uint abrk = dataBase + dataSize + MemoryConstants.MEM_PAGESIZE;
			abrk -= abrk % MemoryConstants.MEM_PAGESIZE;
			
			this.Brk = abrk;
			
			this.MmapBrk = MMAP_BASE;
			
			thread.Regs.Npc = this.ProgEntry;
			thread.Regs.Nnpc = (uint)(thread.Regs.Npc + Marshal.SizeOf (typeof(uint)));
		}

		public string Cwd { get; private set; }
		public List<string> Args { get; private set; }

		public uint Argc {
			get { return (uint)this.Args.Count; }
		}

		unsafe public List<string> Env { get; private set; }

		public uint Brk { get; set; }
		public uint MmapBrk { get; set; }
		public uint ProgEntry { get; set; }

		public uint Uid { get; set; }
		public uint Euid { get; set; }
		public uint Gid { get; set; }
		public uint Egid { get; set; }
		public uint Pid { get; set; }
		public uint Ppid { get; set; }
	}

	public delegate int SyscallAction (SyscallDesc desc, Thread thread);

	public static class SyscallConstants
	{
		public static uint MAX_BUFFER_SIZE = 1024;

		public static int SIM_O_RDONLY = 0;
		public static int SIM_O_WRONLY = 1;
		public static int SIM_O_RDWR = 2;
		public static int SIM_O_CREAT = 0x100;
		public static int SIM_O_EXCL = 0x400;
		public static int SIM_O_NOCTTY = 0x800;
		public static int SIM_O_TRUNC = 0x200;
		public static int SIM_O_APPEND = 8;
		public static int SIM_O_NONBLOCK = 0x80;
		public static int SIM_O_SYNC = 0x10;
	}

	public struct OpenFlagTransTable
	{
		public OpenFlagTransTable (int tgtFlag, int hostFlag)
		{
			this.TgtFlag = tgtFlag;
			this.HostFlag = hostFlag;
		}

		public int TgtFlag;
		public int HostFlag;

		static OpenFlagTransTable ()
		{
			OpenFlagTable = new List<OpenFlagTransTable> ();
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_RDONLY, (int)OpenFlags.O_RDONLY));
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_WRONLY, (int)OpenFlags.O_WRONLY));
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_RDWR, (int)OpenFlags.O_RDWR));
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_APPEND, (int)OpenFlags.O_APPEND));
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_SYNC, (int)OpenFlags.O_SYNC));
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_CREAT, (int)OpenFlags.O_CREAT));
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_TRUNC, (int)OpenFlags.O_TRUNC));
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_EXCL, (int)OpenFlags.O_EXCL));
			OpenFlagTable.Add (new OpenFlagTransTable (SyscallConstants.SIM_O_NOCTTY, (int)OpenFlags.O_NOCTTY));
			OpenFlagTable.Add (new OpenFlagTransTable (0x2000, 0));
		}

		public static List<OpenFlagTransTable> OpenFlagTable;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct utsname
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

	public class SyscallEmul
	{
		public SyscallEmul ()
		{
			this.SyscallDescs = new Dictionary<uint, SyscallDesc> ();
			
			this.InitSyscallDescs ();
		}

		public void InitSyscallDescs ()
		{
			uint index = 0;
			
						/* 0 */this.Register ("syscall", index++);
			/* 1 */			this.Register ("exit", index++, ExitImpl);
			/* 2 */			this.Register ("fork", index++, InvalidArgImpl);
			/* 3 */			this.Register ("read", index++, ReadImpl);
			/* 4 */			this.Register ("write", index++, WriteImpl);
			/* 5 */			this.Register ("open", index++, OpenImpl);
			/* 6 */			this.Register ("close", index++, CloseImpl);
			/* 7 */			this.Register ("waitpid", index++, InvalidArgImpl);
			/* 8 */			this.Register ("creat", index++, InvalidArgImpl);
			/* 8 */			this.Register ("link", index++, InvalidArgImpl);
			/* 10 */			this.Register ("unlink", index++, InvalidArgImpl);
			/* 11 */			this.Register ("execve", index++, InvalidArgImpl);
			/* 12 */			this.Register ("chdir", index++, InvalidArgImpl);
			/* 13 */			this.Register ("time", index++, InvalidArgImpl);
			/* 14 */			this.Register ("mknod", index++, InvalidArgImpl);
			/* 15 */			this.Register ("chmod", index++, InvalidArgImpl);
			/* 16 */			this.Register ("lchown", index++, InvalidArgImpl);
			/* 17 */			this.Register ("break", index++, InvalidArgImpl);
			/* 18 */			this.Register ("oldstat", index++, InvalidArgImpl);
			/* 19 */			this.Register ("lseek", index++, LseekImpl);
			/* 20 */			this.Register ("getpid", index++, GetpidImpl);
			/* 21 */			this.Register ("mount", index++, InvalidArgImpl);
			/* 22 */			this.Register ("umount", index++, InvalidArgImpl);
			/* 23 */			this.Register ("setuid", index++, InvalidArgImpl);
			/* 24 */			this.Register ("getuid", index++, GetuidImpl);
			/* 25 */			this.Register ("stime", index++, InvalidArgImpl);
			/* 26 */			this.Register ("ptrace", index++, InvalidArgImpl);
			/* 27 */			this.Register ("alarm", index++, InvalidArgImpl);
			/* 28 */			this.Register ("oldfstat", index++, InvalidArgImpl);
			/* 29 */			this.Register ("pause", index++, InvalidArgImpl);
			/* 30 */			this.Register ("utime", index++, InvalidArgImpl);
			/* 31 */			this.Register ("stty", index++, InvalidArgImpl);
			/* 32 */			this.Register ("gtty", index++, InvalidArgImpl);
			/* 33 */			this.Register ("access", index++, InvalidArgImpl);
			/* 34 */			this.Register ("nice", index++, InvalidArgImpl);
			/* 35 */			this.Register ("ftime", index++, InvalidArgImpl);
			/* 36 */			this.Register ("sync", index++, InvalidArgImpl);
			/* 37 */			this.Register ("kill", index++, InvalidArgImpl);
			/* 38 */			this.Register ("rename", index++, InvalidArgImpl);
			/* 39 */			this.Register ("mkdir", index++, InvalidArgImpl);
			/* 40 */			this.Register ("rmdir", index++, InvalidArgImpl);
			/* 41 */			this.Register ("dup", index++, InvalidArgImpl);
			/* 42 */			this.Register ("pipe", index++, InvalidArgImpl);
			/* 43 */			this.Register ("times", index++, InvalidArgImpl);
			/* 44 */			this.Register ("prof", index++, InvalidArgImpl);
			/* 45 */			this.Register ("brk", index++, BrkImpl);
			/* 46 */			this.Register ("setgid", index++, InvalidArgImpl);
			/* 47 */			this.Register ("getgid", index++, GetgidImpl);
			/* 48 */			this.Register ("signal", index++, InvalidArgImpl);
			/* 49 */			this.Register ("geteuid", index++, GeteuidImpl);
			/* 50 */			this.Register ("getegid", index++, GetegidImpl);
			/* 51 */			this.Register ("acct", index++, InvalidArgImpl);
			/* 52 */			this.Register ("umount2", index++, InvalidArgImpl);
			/* 53 */			this.Register ("lock", index++, InvalidArgImpl);
			/* 54 */			this.Register ("ioctl", index++, InvalidArgImpl);
			/* 55 */			this.Register ("fcntl", index++, InvalidArgImpl);
			/* 56 */			this.Register ("mpx", index++, InvalidArgImpl);
			/* 57 */			this.Register ("setpgid", index++, InvalidArgImpl);
			/* 58 */			this.Register ("ulimit", index++, InvalidArgImpl);
			/* 59 */			this.Register ("oldolduname", index++, InvalidArgImpl);
			/* 60 */			this.Register ("umask", index++, InvalidArgImpl);
			/* 61 */			this.Register ("chroot", index++, InvalidArgImpl);
			/* 62 */			this.Register ("ustat", index++, InvalidArgImpl);
			/* 63 */			this.Register ("dup2", index++, InvalidArgImpl);
			/* 64 */			this.Register ("getppid", index++, InvalidArgImpl);
			/* 65 */			this.Register ("getpgrp", index++, InvalidArgImpl);
			/* 66 */			this.Register ("setsid", index++, InvalidArgImpl);
			/* 67 */			this.Register ("sigaction", index++, InvalidArgImpl);
			/* 68 */			this.Register ("sgetmask", index++, InvalidArgImpl);
			/* 69 */			this.Register ("ssetmask", index++, InvalidArgImpl);
			/* 70 */			this.Register ("setreuid", index++, InvalidArgImpl);
			/* 71 */			this.Register ("setregid", index++, InvalidArgImpl);
			/* 72 */			this.Register ("sigsuspend", index++, InvalidArgImpl);
			/* 73 */			this.Register ("sigpending", index++, InvalidArgImpl);
			/* 74 */			this.Register ("sethostname", index++, InvalidArgImpl);
			/* 75 */			this.Register ("setrlimit", index++, InvalidArgImpl);
			/* 76 */			this.Register ("getrlimit", index++, InvalidArgImpl);
			/* 77 */			this.Register ("getrusage", index++, InvalidArgImpl);
			/* 78 */			this.Register ("gettimeofday", index++, InvalidArgImpl);
			/* 79 */			this.Register ("settimeofday", index++, InvalidArgImpl);
			/* 80 */			this.Register ("getgroups", index++, InvalidArgImpl);
			/* 81 */			this.Register ("setgroups", index++, InvalidArgImpl);
			/* 82 */			this.Register ("select", index++, InvalidArgImpl);
			/* 83 */			this.Register ("symlink", index++, InvalidArgImpl);
			/* 84 */			this.Register ("oldlstat", index++, InvalidArgImpl);
			/* 85 */			this.Register ("readlink", index++, InvalidArgImpl);
			/* 86 */			this.Register ("uselib", index++, InvalidArgImpl);
			/* 87 */			this.Register ("swapon", index++, InvalidArgImpl);
			/* 88 */			this.Register ("reboot", index++, InvalidArgImpl);
			/* 89 */			this.Register ("readdir", index++, InvalidArgImpl);
			/* 90 */			this.Register ("mmap", index++, InvalidArgImpl);
			/* 91 */			this.Register ("munmap", index++, InvalidArgImpl);
			/* 92 */			this.Register ("truncate", index++, InvalidArgImpl);
			/* 93 */			this.Register ("ftruncate", index++, InvalidArgImpl);
			/* 94 */			this.Register ("fchmod", index++, InvalidArgImpl);
			/* 95 */			this.Register ("fchown", index++, InvalidArgImpl);
			/* 96 */			this.Register ("getpriority", index++, InvalidArgImpl);
			/* 97 */			this.Register ("setpriority", index++, InvalidArgImpl);
			/* 98 */			this.Register ("profil", index++, InvalidArgImpl);
			/* 99 */			this.Register ("statfs", index++, InvalidArgImpl);
			/* 100 */			this.Register ("fstatfs", index++, InvalidArgImpl);
			/* 101 */			this.Register ("ioperm", index++, InvalidArgImpl);
			/* 102 */			this.Register ("socketcall", index++, InvalidArgImpl);
			/* 103 */			this.Register ("syslog", index++, InvalidArgImpl);
			/* 104 */			this.Register ("setitimer", index++, InvalidArgImpl);
			/* 105 */			this.Register ("getitimer", index++, InvalidArgImpl);
			/* 106 */			this.Register ("stat", index++, InvalidArgImpl);
			/* 107 */			this.Register ("lstat", index++, InvalidArgImpl);
			/* 108 */			this.Register ("fstat", index++, FstatImpl);
			/* 109 */			this.Register ("olduname", index++, InvalidArgImpl);
			/* 110 */			this.Register ("iopl", index++, InvalidArgImpl);
			/* 111 */			this.Register ("vhangup", index++, InvalidArgImpl);
			/* 112 */			this.Register ("idle", index++, InvalidArgImpl);
			/* 113 */			this.Register ("vm86old", index++, InvalidArgImpl);
			/* 114 */			this.Register ("wait4", index++, InvalidArgImpl);
			/* 115 */			this.Register ("swapoff", index++, InvalidArgImpl);
			/* 116 */			this.Register ("sysinfo", index++, InvalidArgImpl);
			/* 117 */			this.Register ("ipc", index++, InvalidArgImpl);
			/* 118 */			this.Register ("fsync", index++, InvalidArgImpl);
			/* 119 */			this.Register ("sigreturn", index++, InvalidArgImpl);
			/* 120 */			this.Register ("clone", index++, InvalidArgImpl);
			/* 121 */			this.Register ("setdomainname", index++, InvalidArgImpl);
			/* 122 */			this.Register ("uname", index++, UnameImpl);
			/* 123 */			this.Register ("modify_ldt", index++, InvalidArgImpl);
			/* 124 */			this.Register ("adjtimex", index++, InvalidArgImpl);
			/* 125 */			this.Register ("mprotect", index++, InvalidArgImpl);
			/* 126 */			this.Register ("sigprocmask", index++, InvalidArgImpl);
			/* 127 */			this.Register ("create_module", index++, InvalidArgImpl);
			/* 128 */			this.Register ("init_module", index++, InvalidArgImpl);
			/* 129 */			this.Register ("delete_module", index++, InvalidArgImpl);
			/* 130 */			this.Register ("get_kernel_syms", index++, InvalidArgImpl);
			/* 131 */			this.Register ("quotactl", index++, InvalidArgImpl);
			/* 132 */			this.Register ("getpgid", index++, InvalidArgImpl);
			/* 133 */			this.Register ("fchdir", index++, InvalidArgImpl);
			/* 134 */			this.Register ("bdflush", index++, InvalidArgImpl);
			/* 135 */			this.Register ("sysfs", index++, InvalidArgImpl);
			/* 136 */			this.Register ("personality", index++, InvalidArgImpl);
			/* 137 */			this.Register ("afs_syscall", index++, InvalidArgImpl);
			/* 138 */			this.Register ("setfsuid", index++, InvalidArgImpl);
			/* 139 */			this.Register ("setfsgid", index++, InvalidArgImpl);
			/* 140 */			this.Register ("_llseek", index++, LlseekImpl);
			/* 141 */			this.Register ("getdents", index++, InvalidArgImpl);
			/* 142 */			this.Register ("_newselect", index++, InvalidArgImpl);
			/* 143 */			this.Register ("flock", index++, InvalidArgImpl);
			/* 144 */			this.Register ("msync", index++, InvalidArgImpl);
			/* 145 */			this.Register ("readv", index++, InvalidArgImpl);
			/* 146 */			this.Register ("writev", index++, InvalidArgImpl);
			/* 147 */			this.Register ("getsid", index++, InvalidArgImpl);
			/* 148 */			this.Register ("fdatasync", index++, InvalidArgImpl);
			/* 149 */			this.Register ("_sysctl", index++, InvalidArgImpl);
			/* 150 */			this.Register ("mlock", index++, InvalidArgImpl);
			/* 151 */			this.Register ("munlock", index++, InvalidArgImpl);
			/* 152 */			this.Register ("mlockall", index++, InvalidArgImpl);
			/* 153 */			this.Register ("munlockall", index++, InvalidArgImpl);
			/* 154 */			this.Register ("sched_setparam", index++, InvalidArgImpl);
			/* 155 */			this.Register ("sched_getparam", index++, InvalidArgImpl);
			/* 156 */			this.Register ("sched_setscheduler", index++, InvalidArgImpl);
			/* 157 */			this.Register ("sched_getscheduler", index++, InvalidArgImpl);
			/* 158 */			this.Register ("sched_yield", index++, InvalidArgImpl);
			/* 159 */			this.Register ("sched_get_priority_max", index++, InvalidArgImpl);
			/* 160 */			this.Register ("sched_get_priority_min", index++, InvalidArgImpl);
			/* 161 */			this.Register ("sched_rr_get_interval", index++, InvalidArgImpl);
			/* 162 */			this.Register ("nanosleep", index++, InvalidArgImpl);
			/* 163 */			this.Register ("mremap", index++, InvalidArgImpl);
			/* 164 */			this.Register ("setresuid", index++, InvalidArgImpl);
			/* 165 */			this.Register ("getresuid", index++, InvalidArgImpl);
			/* 166 */			this.Register ("vm86", index++, InvalidArgImpl);
			/* 167 */			this.Register ("query_module", index++, InvalidArgImpl);
			/* 168 */			this.Register ("poll", index++, InvalidArgImpl);
			/* 169 */			this.Register ("nfsservctl", index++, InvalidArgImpl);
			/* 170 */			this.Register ("setresgid", index++, InvalidArgImpl);
			/* 171 */			this.Register ("getresgid", index++, InvalidArgImpl);
			/* 172 */			this.Register ("prctl", index++, InvalidArgImpl);
			/* 173 */			this.Register ("rt_sigreturn", index++, InvalidArgImpl);
			/* 174 */			this.Register ("rt_sigaction", index++, InvalidArgImpl);
			/* 175 */			this.Register ("rt_sigprocmask", index++, InvalidArgImpl);
			/* 176 */			this.Register ("rt_sigpending", index++, InvalidArgImpl);
			/* 177 */			this.Register ("rt_sigtimedwait", index++, InvalidArgImpl);
			/* 178 */			this.Register ("rt_sigqueueinfo", index++, InvalidArgImpl);
			/* 179 */			this.Register ("rt_sigsuspend", index++, InvalidArgImpl);
			/* 180 */			this.Register ("pread", index++, InvalidArgImpl);
			/* 181 */			this.Register ("pwrite", index++, InvalidArgImpl);
			/* 182 */			this.Register ("chown", index++, InvalidArgImpl);
			/* 183 */			this.Register ("getcwd", index++, InvalidArgImpl);
			/* 184 */			this.Register ("capget", index++, InvalidArgImpl);
			/* 185 */			this.Register ("capset", index++, InvalidArgImpl);
			/* 186 */			this.Register ("sigalstack", index++, InvalidArgImpl);
			/* 187 */			this.Register ("sendfile", index++, InvalidArgImpl);
			/* 188 */			this.Register ("getpmsg", index++, InvalidArgImpl);
			/* 189 */			this.Register ("putpmsg", index++, InvalidArgImpl);
			/* 190 */			this.Register ("vfork", index++, InvalidArgImpl);
			/* 191 */			this.Register ("ugetrlimit", index++, InvalidArgImpl);
			/* 192 */			this.Register ("mmap2", index++, InvalidArgImpl);
			/* 193 */			this.Register ("truncate64", index++, InvalidArgImpl);
			/* 194 */			this.Register ("ftruncate64", index++, InvalidArgImpl);
			/* 195 */			this.Register ("stat64", index++, InvalidArgImpl);
			/* 196 */			this.Register ("lstat64", index++, InvalidArgImpl);
			/* 197 */			this.Register ("fstat64", index++, InvalidArgImpl);
			/* 198 */			this.Register ("lchown32", index++, InvalidArgImpl);
			/* 199 */			this.Register ("getuid32", index++, InvalidArgImpl);
			/* 200 */			this.Register ("getgid32", index++, InvalidArgImpl);
			/* 201 */			this.Register ("geteuid32", index++, InvalidArgImpl);
			/* 202 */			this.Register ("getegid32", index++, InvalidArgImpl);
			/* 203 */			this.Register ("setreuid32", index++, InvalidArgImpl);
			/* 204 */			this.Register ("setregid32", index++, InvalidArgImpl);
			/* 205 */			this.Register ("getgroups32", index++, InvalidArgImpl);
			/* 206 */			this.Register ("setgroups32", index++, InvalidArgImpl);
			/* 207 */			this.Register ("fchown32", index++, InvalidArgImpl);
			/* 208 */			this.Register ("setresuid32", index++, InvalidArgImpl);
			/* 209 */			this.Register ("getresuid32", index++, InvalidArgImpl);
			/* 210 */			this.Register ("setresgid32", index++, InvalidArgImpl);
			/* 211 */			this.Register ("getresgid32", index++, InvalidArgImpl);
			/* 212 */			this.Register ("chown32", index++, InvalidArgImpl);
			/* 213 */			this.Register ("setuid32", index++, InvalidArgImpl);
			/* 214 */			this.Register ("setgid32", index++, InvalidArgImpl);
			/* 215 */			this.Register ("setfsuid32", index++, InvalidArgImpl);
			/* 216 */			this.Register ("setfsgid32", index++, InvalidArgImpl);
			/* 217 */			this.Register ("pivot_root", index++, InvalidArgImpl);
			/* 218 */			this.Register ("mincore", index++, InvalidArgImpl);
			/* 219 */			this.Register ("madvise", index++, InvalidArgImpl);
			/* 220 */			this.Register ("getdents64", index++, InvalidArgImpl);
			/* 221 */			this.Register ("fcntl64", index++, InvalidArgImpl);
		}

		public void Register (string name, uint num)
		{
			this.Register (new SyscallDesc (name, num));
		}

		public void Register (string name, uint num, SyscallAction action)
		{
			this.Register (new SyscallDesc (name, num, action));
		}

		public void Register (SyscallDesc desc)
		{
			this.SyscallDescs[desc.Num] = desc;
		}

		public void DoSyscall (uint callNum, Thread thread)
		{
			int syscallIndex = (int)(callNum - 4000);
			
			if (syscallIndex >= 0 && syscallIndex < this.SyscallDescs.Count && this.SyscallDescs.ContainsKey ((uint)syscallIndex)) {
				this.SyscallDescs[(uint)syscallIndex].DoSyscall (thread);
			} else {
				Logger.Warnf (LogCategory.SYSCALL, "Syscall {0:d} ({1:d}) out of range", callNum, syscallIndex);
				thread.SetSyscallReturn ((-(int)Errno.EINVAL));
			}
		}

		public Dictionary<uint, SyscallDesc> SyscallDescs { get; private set; }

		public static int ExitImpl (SyscallDesc desc, Thread thread)
		{
			Console.WriteLine ("exiting...");
			thread.Halt ((int)(thread.GetSyscallArg (0) & 0xff));
			return 1;
		}

		unsafe public static int ReadImpl (SyscallDesc desc, Thread thread)
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

		unsafe public static int WriteImpl (SyscallDesc desc, Thread thread)
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

		unsafe public static int OpenImpl (SyscallDesc desc, Thread thread)
		{
			char[] path = new char[SyscallConstants.MAX_BUFFER_SIZE];
			
			uint addr = thread.GetSyscallArg (0);
			uint tgtFlags = thread.GetSyscallArg (1);
			uint mode = thread.GetSyscallArg (2);
			
			fixed (char* pathPtr = &path[0]) {
				thread.Mem.ReadString (addr, (int)SyscallConstants.MAX_BUFFER_SIZE, pathPtr);
			}
			
			int hostFlags = 0;
			foreach (OpenFlagTransTable t in OpenFlagTransTable.OpenFlagTable) {
				if ((tgtFlags & t.TgtFlag) != 0) {
					tgtFlags &= (uint)(~t.TgtFlag);
					hostFlags |= t.HostFlag;
				}
			}
			
			if (tgtFlags != 0) {
				Logger.Fatalf (LogCategory.SYSCALL, "Syscall: open: cannot decode flags 0x{0:x8}", tgtFlags);
			}
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (path);
			
			int fd = (int)Syscall.open (sb.ToString (), (OpenFlags)hostFlags, (FilePermissions)mode);
			
			return fd;
		}

		public static int CloseImpl (SyscallDesc desc, Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			int ret = (int)Syscall.close (fd);
			return ret;
		}

		public static int LseekImpl (SyscallDesc desc, Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			int offset = (int)thread.GetSyscallArg (1);
			int whence = (int)thread.GetSyscallArg (2);
			
			int ret = (int)Syscall.lseek (fd, offset, (SeekFlags)whence);
			return ret;
		}

		public static int GetpidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Pid;
		}

		public static int GetuidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Uid;
		}

		public static int BrkImpl (SyscallDesc desc, Thread thread)
		{
			uint newbrk = thread.GetSyscallArg (0);
			uint oldbrk = thread.Process.Brk;
			
			if (newbrk == 0) {
				return (int)thread.Process.Brk;
			}
			
			uint newbrkRnd = BitUtils.RoundUp (newbrk, MemoryConstants.MEM_PAGESIZE);
			uint oldbrkRnd = BitUtils.RoundUp (oldbrk, MemoryConstants.MEM_PAGESIZE);
			
			if (newbrk > oldbrk) {
				thread.Mem.Map (oldbrkRnd, (int)(newbrkRnd - oldbrkRnd), MemoryAccessType.READ | MemoryAccessType.WRITE);
			} else if (newbrk < oldbrk) {
				thread.Mem.Unmap (newbrkRnd, (int)(oldbrkRnd - newbrkRnd));
			}
			
			thread.Process.Brk = newbrk;
			
			return (int)thread.Process.Brk;
		}

		public static int GetgidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Gid;
		}

		public static int GeteuidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Euid;
		}

		public static int GetegidImpl (SyscallDesc desc, Thread thread)
		{
			return (int)thread.Process.Egid;
		}

		unsafe public static int FstatImpl (SyscallDesc desc, Thread thread)
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

		unsafe public static int UnameImpl (SyscallDesc desc, Thread thread)
		{
			utsname un = new utsname ();
			un.sysname = "Linux";
			un.nodename = "sim";
			un.release = "2.6";
			un.version = "Tue Apr 5 12:21:57 UTC 2005";
			un.machine = "mips";
			
			IntPtr unPtr = Marshal.AllocHGlobal (Marshal.SizeOf (typeof(utsname)));
			Marshal.StructureToPtr (un, unPtr, false);
			
			thread.Mem.WriteBlock (thread.GetSyscallArg (0), (uint)Marshal.SizeOf (typeof(utsname)), (byte*)unPtr);
			
			Marshal.FreeHGlobal (unPtr);
			
			return 0;
		}

		public static int LlseekImpl (SyscallDesc desc, Thread thread)
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

		public static int InvalidArgImpl (SyscallDesc desc, Thread thread)
		{
			Logger.Warnf (LogCategory.SYSCALL, "syscall {0:s} is ignored.", desc.Name);
			return -(int)Errno.EINVAL;
		}
	}

	public class SyscallDesc
	{
		public SyscallDesc (string name, uint num) : this(name, num, null)
		{
		}

		public SyscallDesc (string name, uint num, SyscallAction action)
		{
			this.Name = name;
			this.Num = num;
			this.Action = action;
		}

		public void DoSyscall (Thread thread)
		{
			if (this.Action == null) {
				Logger.Fatalf (LogCategory.SYSCALL, "syscall {0:s} has not been implemented yet.", this.Name);
			}
			
			int retVal = this.Action (this, thread);
			thread.SetSyscallReturn (retVal);
		}

		public string Name { get; set; }
		public uint Num { get; set; }
		public SyscallAction Action { get; set; }
	}
}
