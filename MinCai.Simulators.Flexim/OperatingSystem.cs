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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using MinCai.Simulators.Flexim.Architecture;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.Microarchitecture;
using MinCai.Simulators.Flexim.OperatingSystem;
using Mono.Unix.Native;

namespace MinCai.Simulators.Flexim.OperatingSystem
{
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
			ElfFile file = ElfFile.Create (Processor.WorkDirectory, this.Args[0]);
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
//						Logger.Infof (Logger.Categories.PROCESS, "Loading {0:s} ({1:d} bytes) at address 0x{2:x8}", shdr.Name, shdr.Sh_size, shdr.Sh_addr);
						
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
					Logger.Fatal (Logger.Categories.Elf, "dynamic linking is not supported");
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
				Logger.Fatal (Logger.Categories.Process, "Environment overflow. Need to increase MAX_ENVIRON.");
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

	static internal class SyscallEmulation
	{
		[Flags]
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

		[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
		private sealed class SyscallAttribute : Attribute
		{
			public SyscallAttribute (string name, uint num)
			{
				this.Name = name;
				this.Num = num;
			}

			public override string ToString ()
			{
				return string.Format ("[SyscallAttribute: Name={0}, Num={1}]", Name, Num);
			}

			public string Name { get; set; }
			public uint Num { get; set; }
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
		}

		public static void DoSyscall (uint callNum, Thread thread)
		{
			int syscallIndex = (int)(callNum - 4000);
			
			if (!FindAndCallSyscallImpl ((uint)syscallIndex, thread)) {
				Logger.Warnf (Logger.Categories.Syscall, "Syscall {0:d} ({1:d}) not implemented", callNum, syscallIndex);
				thread.SetSyscallReturn ((-(int)Errno.EINVAL));
			}
		}

		private static bool FindAndCallSyscallImpl (uint callNum, Thread thread)
		{
			foreach (var method in typeof(SyscallEmulation).GetMethods (BindingFlags.Static | BindingFlags.NonPublic)) {
				if (method.IsStatic && method.IsDefined (typeof(SyscallAttribute), false)) {
					SyscallAttribute attr = method.GetCustomAttributes (typeof(SyscallAttribute), false).SingleOrDefault () as SyscallAttribute;
					
					if (attr.Num == callNum) {
						int retVal = (int)method.Invoke (null, new object[] { thread });
						thread.SetSyscallReturn (retVal);
						
						return true;
					}
				}
			}
			
			return false;
		}

		[Syscall("exit", 1)]
		private static int ExitImpl (Thread thread)
		{
			Console.WriteLine ("exiting...");
			thread.Halt ((int)(thread.GetSyscallArg (0) & 0xff));
			return 1;
		}

		[Syscall("read", 3)]
		unsafe private static int ReadImpl (Thread thread)
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

		[Syscall("write", 4)]
		unsafe private static int WriteImpl (Thread thread)
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

		[Syscall("open", 5)]
		unsafe private static int OpenImpl (Thread thread)
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
				Logger.Fatalf (Logger.Categories.Syscall, "Syscall: open: cannot decode flags 0x{0:x8}", tgtFlags);
			}
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (path);
			
			int fd = (int)Syscall.open (sb.ToString (), (OpenFlags)hostFlags, (FilePermissions)mode);
			
			return fd;
		}

		[Syscall("close", 6)]
		private static int CloseImpl (Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			int ret = (int)Syscall.close (fd);
			return ret;
		}

		[Syscall("lseek", 19)]
		private static int LseekImpl (Thread thread)
		{
			int fd = (int)thread.GetSyscallArg (0);
			int offset = (int)thread.GetSyscallArg (1);
			int whence = (int)thread.GetSyscallArg (2);
			
			int ret = (int)Syscall.lseek (fd, offset, (SeekFlags)whence);
			return ret;
		}

		[Syscall("getpid", 20)]
		private static int GetpidImpl (Thread thread)
		{
			return (int)thread.Process.Pid;
		}

		[Syscall("getuid", 24)]
		private static int GetuidImpl (Thread thread)
		{
			return (int)thread.Process.Uid;
		}

		[Syscall("brk", 45)]
		private static int BrkImpl (Thread thread)
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

		[Syscall("getgid", 47)]
		private static int GetgidImpl (Thread thread)
		{
			return (int)thread.Process.Gid;
		}

		[Syscall("geteuid", 49)]
		private static int GeteuidImpl (Thread thread)
		{
			return (int)thread.Process.Euid;
		}

		[Syscall("getegid", 50)]
		private static int GetegidImpl (Thread thread)
		{
			return (int)thread.Process.Egid;
		}

		[Syscall("fstat", 108)]
		unsafe private static int FstatImpl (Thread thread)
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

		[Syscall("uname", 122)]
		unsafe private static int UnameImpl (Thread thread)
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

		[Syscall("_llseek", 140)]
		private static int LlseekImpl (Thread thread)
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

		private static List<OpenFlagMapping> OpenFlagMappings;

		private static uint MAX_BUFFER_SIZE = 1024;
	}
}
