/*
 * Interop.cs
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
using MinCai.Simulators.Flexim.Architecture;
using MinCai.Simulators.Flexim.Microarchitecture;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.MemoryHierarchy;

using Process = MinCai.Simulators.Flexim.OperatingSystem.Process;

namespace MinCai.Simulators.Flexim.Interop
{
	public sealed partial class Workload
	{
		public Workload (string title, string cwd, string exe, string args, string stdin, string stdout, uint numThreadsNeeded)
		{
			this.Title = title;
			this.Cwd = cwd;
			this.Exe = exe;
			this.Args = args;
			this.Stdin = stdin;
			this.Stdout = stdout;
			this.NumThreadsNeeded = numThreadsNeeded;
		}

		public string Title { get; set; }
		public string Cwd { get; set; }
		public string Exe { get; set; }
		public string Args { get; set; }
		public string Stdin { get; set; }
		public string Stdout { get; set; }
		public uint NumThreadsNeeded { get; set; }

		public WorkloadSet WorkloadSet { get; set; }
	}

	public sealed partial class WorkloadSet
	{
		public WorkloadSet (string title)
		{
			this.Title = title;
			this.Workloads = new Dictionary<string, Workload> ();
		}

		public void Register (Workload workload)
		{
			Debug.Assert (!this.Workloads.ContainsKey (workload.Title));
			
			workload.WorkloadSet = this;
			this.Workloads[workload.Title] = workload;
		}

		public Workload this[string index] {
			get { return this.Workloads[index]; }
		}

		public string Title { get; set; }
		public Dictionary<string, Workload> Workloads { get; private set; }
	}

	public abstract class Config
	{
	}

	public sealed partial class TlbConfig : Config
	{
		public TlbConfig (CacheGeometry geometry, uint hitLatency, uint missLatency)
		{
			this.Geometry = geometry;
			this.HitLatency = hitLatency;
			this.MissLatency = missLatency;
		}

		public CacheGeometry Geometry { get; set; }
		public uint HitLatency { get; set; }
		public uint MissLatency { get; set; }
	}

	public sealed partial class CacheConfig : Config
	{
		public CacheConfig (string name, CacheGeometry geometry, uint hitLatency, CacheReplacementPolicy policy)
		{
			this.Name = name;
			this.Geometry = geometry;
			this.HitLatency = hitLatency;
			this.Policy = policy;
		}

		public string Name { get; set; }
		public CacheGeometry Geometry { get; set; }
		public uint HitLatency { get; set; }
		public CacheReplacementPolicy Policy { get; set; }
	}

	public sealed partial class MainMemoryConfig : Config
	{
		public MainMemoryConfig (uint latency)
		{
			this.Latency = latency;
		}

		public uint Latency { get; set; }
	}

	public sealed partial class ContextConfig : Config
	{
		public ContextConfig (Workload workload)
		{
			this.Workload = workload;
		}

		public Workload Workload { get; set; }
	}

	public sealed partial class CoreConfig : Config
	{
		public CoreConfig (CacheConfig iCache, CacheConfig dCache)
		{
			this.ICache = iCache;
			this.DCache = dCache;
		}

		public CacheConfig ICache { get; set; }
		public CacheConfig DCache { get; set; }
	}

	public sealed partial class ProcessorConfig : Config
	{
		public ProcessorConfig (ulong maxCycle, ulong maxInsts, ulong maxTime, uint numThreadsPerCore, uint physicalRegisterFileCapacity, uint decodeWidth, uint issueWidth, uint commitWidth, uint decodeBufferCapacity, uint reorderBufferCapacity,
		uint loadStoreQueueCapacity)
		{
			this.MaxCycle = maxCycle;
			this.MaxInsts = maxInsts;
			this.MaxTime = maxTime;
			this.NumThreadsPerCore = numThreadsPerCore;
			this.Cores = new List<CoreConfig> ();
			
			this.PhysicalRegisterFileCapacity = physicalRegisterFileCapacity;
			this.DecodeWidth = decodeWidth;
			this.IssueWidth = issueWidth;
			this.CommitWidth = commitWidth;
			this.DecodeBufferCapcity = decodeBufferCapacity;
			this.ReorderBufferCapacity = reorderBufferCapacity;
			this.LoadStoreQueueCapacity = loadStoreQueueCapacity;
		}

		public ulong MaxCycle { get; set; }
		public ulong MaxInsts { get; set; }
		public ulong MaxTime { get; set; }
		public uint NumThreadsPerCore { get; set; }
		public List<CoreConfig> Cores { get; set; }

		public uint PhysicalRegisterFileCapacity { get; set; }
		public uint DecodeWidth { get; set; }
		public uint IssueWidth { get; set; }
		public uint CommitWidth { get; set; }
		public uint DecodeBufferCapcity { get; set; }
		public uint ReorderBufferCapacity { get; set; }
		public uint LoadStoreQueueCapacity { get; set; }

		public TlbConfig Tlb { get; set; }
	}

	public sealed partial class ArchitectureConfig : Config
	{
		public ArchitectureConfig (string title, ProcessorConfig processor, CacheConfig l2Cache, MainMemoryConfig mainMemory)
		{
			this.Title = title;
			this.Processor = processor;
			this.L2Cache = l2Cache;
			this.MainMemory = mainMemory;
		}

		public string Title { get; set; }
		public ProcessorConfig Processor { get; set; }
		public CacheConfig L2Cache { get; set; }
		public MainMemoryConfig MainMemory { get; set; }
	}

	public sealed partial class SimulationConfig : Config
	{
		public SimulationConfig (ArchitectureConfig architecture)
		{
			this.Architecture = architecture;
			this.Contexts = new List<ContextConfig> ();
		}

		public ArchitectureConfig Architecture { get; set; }
		public List<ContextConfig> Contexts { get; set; }
	}

	public abstract class Stat
	{
		public abstract void Reset ();
	}

	public sealed partial class TlbStat : Stat
	{
		public TlbStat (string name)
		{
			this.Name = name;
			this.Reset ();
		}

		public override void Reset ()
		{
			this.Accesses = 0;
			this.Hits = 0;
			this.Evictions = 0;
		}

		public string Name { get; private set; }
		public ulong Accesses { get; set; }
		public ulong Hits { get; set; }
		public ulong Evictions { get; set; }
	}

	public sealed partial class CacheStat : Stat
	{
		public CacheStat ()
		{
			this.Reset ();
		}

		public override void Reset ()
		{
			this.Accesses = 0;
			this.Hits = 0;
			this.Evictions = 0;
			this.Reads = 0;
			this.BlockingReads = 0;
			this.NonblockingReads = 0;
			this.ReadHits = 0;
			this.Writes = 0;
			this.BlockingWrites = 0;
			this.NonblockingWrites = 0;
			this.WriteHits = 0;
			
			this.ReadRetries = 0;
			this.WriteRetries = 0;
			
			this.NoRetryAccesses = 0;
			this.NoRetryHits = 0;
			this.NoRetryReads = 0;
			this.NoRetryReadHits = 0;
			this.NoRetryWrites = 0;
			this.NoRetryWriteHits = 0;
		}

		public ulong Accesses { get; set; }
		public ulong Hits { get; set; }
		public ulong Evictions { get; set; }
		public ulong Reads { get; set; }
		public ulong BlockingReads { get; set; }
		public ulong NonblockingReads { get; set; }
		public ulong ReadHits { get; set; }
		public ulong Writes { get; set; }
		public ulong BlockingWrites { get; set; }
		public ulong NonblockingWrites { get; set; }
		public ulong WriteHits { get; set; }

		public ulong ReadRetries { get; set; }
		public ulong WriteRetries { get; set; }

		public ulong NoRetryAccesses { get; set; }
		public ulong NoRetryHits { get; set; }
		public ulong NoRetryReads { get; set; }
		public ulong NoRetryReadHits { get; set; }
		public ulong NoRetryWrites { get; set; }
		public ulong NoRetryWriteHits { get; set; }
	}

	public sealed partial class MainMemoryStat : Stat
	{
		public MainMemoryStat ()
		{
			this.Reset ();
		}

		public override void Reset ()
		{
			this.Accesses = 0;
			this.Reads = 0;
			this.Writes = 0;
		}

		public ulong Accesses { get; set; }
		public ulong Reads { get; set; }
		public ulong Writes { get; set; }
	}

	public sealed partial class ContextStat : Stat
	{
		public ContextStat ()
		{
			this.Reset ();
		}

		public override void Reset ()
		{
			this.TotalInsts = 0;
		}

		public ulong TotalInsts { get; set; }

		public TlbStat Itlb { get; set; }
		public TlbStat Dtlb { get; set; }
	}

	public sealed partial class CoreStat : Stat
	{
		public CoreStat ()
		{
			this.ICache = new CacheStat ();
			this.DCache = new CacheStat ();
			
			this.Reset ();
		}

		public override void Reset ()
		{
			this.ICache.Reset ();
			this.DCache.Reset ();
		}

		public CacheStat ICache { get; set; }
		public CacheStat DCache { get; set; }
	}

	public sealed partial class ProcessorStat : Stat
	{
		public ProcessorStat ()
		{
			this.Cores = new List<CoreStat> ();
			this.Contexts = new List<ContextStat> ();
			
			this.Reset ();
		}

		public override void Reset ()
		{
			this.Cores.ForEach (core => core.Reset ());
			this.Contexts.ForEach (context => context.Reset ());
		}

		public List<CoreStat> Cores { get; private set; }
		public List<ContextStat> Contexts { get; private set; }
	}

	public sealed partial class SimulationStat : Stat
	{
		public SimulationStat (int numCores, uint numThreadsPerCore)
		{
			ProcessorStat processor = new ProcessorStat ();
			for (int i = 0; i < numCores; i++) {
				CoreStat core = new CoreStat ();
				
				for (int j = 0; j < numThreadsPerCore; j++) {
					ContextStat context = new ContextStat ();
					processor.Contexts.Add (context);
				}
				
				processor.Cores.Add (core);
			}
			
			this.Processor = processor;
			
			this.L2Cache = new CacheStat ();
			this.MainMemory = new MainMemoryStat ();
			
			this.Reset ();
		}

		public SimulationStat (ProcessorStat processor)
		{
			this.Processor = processor;
			
			this.L2Cache = new CacheStat ();
			this.MainMemory = new MainMemoryStat ();
			
			this.Reset ();
		}

		public override void Reset ()
		{
			this.Processor.Reset ();
			this.L2Cache.Reset ();
			this.MainMemory.Reset ();
			
			this.TotalCycles = 0;
			this.Duration = 0;
			this.TotalInsts = 0;
			this.InstsPerCycle = 0;
			this.CyclesPerSecond = 0;
		}

		public ProcessorStat Processor { get; set; }
		public CacheStat L2Cache { get; set; }
		public MainMemoryStat MainMemory { get; set; }

		public ulong TotalCycles { get; set; }
		public ulong Duration { get; set; }
		public ulong TotalInsts { get; set; }
		public double InstsPerCycle { get; set; }
		public double CyclesPerSecond { get; set; }
	}

	public interface IProcessor
	{
		void Run ();

		MemoryManagementUnit MMU { get; }
		List<ICore> Cores { get; }
		MemorySystem MemorySystem { get; }
		Simulation Simulation { get; }
		ProcessorConfig Config { get; }
		ulong CurrentCycle { get; }
		int ActiveThreadCount { get; set; }
	}

	public interface ICore : ICycleProvider
	{
		void Fetch ();
		void RegisterRename ();
		void Dispatch ();
		void Wakeup ();
		void Selection ();
		void Writeback ();
		void RefreshLoadStoreQueue ();
		void Commit ();

		CoherentCache ICache { get; }
		CoherentCache DCache { get; }

		uint Num { get; }

		IProcessor Processor { get; }
		List<IThread> Threads { get; }

		uint DecodeWidth { get; }
		uint IssueWidth { get; }

		FunctionalUnitPool FuPool { get; }

		PhysicalRegisterFile IntRegFile { get; }
		PhysicalRegisterFile FpRegFile { get; }
		PhysicalRegisterFile MiscRegFile { get; }

		InstructionSetArchitecture Isa { get; }

		List<ReorderBufferEntry> ReadyQueue { get; }
		List<ReorderBufferEntry> WaitingQueue { get; }
		List<ReorderBufferEntry> OoOEventQueue { get; }
	}

	public interface IThread
	{
		void Fetch ();
		void RegisterRenameOne ();
		void RefreshLoadStoreQueue ();
		void Commit ();

		void RecoverReorderBuffer (ReorderBufferEntry branchReorderBufferEntry);

		void IFetch (uint addr, bool isRetry, Action onCompletedCallback);
		void Load (uint addr, bool isRetry, Action onCompletedCallback);
		void Store (uint addr, bool isRetry, Action onCompletedCallback);

		uint GetSyscallArg (int i);
		void SetSyscallArg (int i, uint val);
		void SetSyscallReturn (int returnVal);
		void Syscall (uint callNum);
		void Halt (int exitCode);

		string Name { get; }
		uint Num { get; }

		uint MemoryMapId { get; }

		Thread.ThreadState State { get; }

		ICore Core { get; }

		Process Process { get; }
		Memory Mem { get; }
		CombinedRegisterFile Regs { get; }

		TranslationLookasideBuffer Itlb { get; }
		TranslationLookasideBuffer Dtlb { get; }

		uint FetchPc { get; set; }
		uint FetchNpc { get; set; }
		uint FetchNnpc { get; set; }

		IBranchPredictor Bpred { get; set; }

		RegisterRenameTable RenameTable { get; }

		uint CommitWidth { get; }

		List<DecodeBufferEntry> DecodeBuffer { get; }
		List<ReorderBufferEntry> ReorderBuffer { get; }
		List<ReorderBufferEntry> LoadStoreQueue { get; }

		bool IsSpeculative { get; set; }

		ContextStat Stat { get; }
	}

	public sealed partial class Simulation
	{
		public Simulation (string title, string cwd, SimulationConfig config, SimulationStat stat)
		{
			this.Title = title;
			this.Cwd = cwd;
			this.Config = config;
			this.Stat = stat;
		}

		public Simulation (string title, string cwd, ArchitectureConfig architectureConfig)
		{
			this.Title = title;
			this.Cwd = cwd;
			this.Config = new SimulationConfig (architectureConfig);
			this.Stat = new SimulationStat (architectureConfig.Processor.Cores.Count, architectureConfig.Processor.NumThreadsPerCore);
		}

		public void Execute ()
		{
			this.BeforeRun ();
			this.Run ();
			this.AfterRun ();
		}

		private void BeforeRun ()
		{
			this.IsRunning = true;
			this.Stat.Reset ();
		}

		private void Run ()
		{
			Processor processor = new Processor (this);
			processor.Run ();
		}

		public void Abort ()
		{
			this.IsRunning = false;
		}

		private void AfterRun ()
		{
			this.IsRunning = false;
		}

		public string Title { get; set; }
		public string Cwd { get; set; }
		public SimulationConfig Config { get; private set; }
		public SimulationStat Stat { get; private set; }

		public bool IsRunning { get; set; }
	}
}
