/*
 * Interop.IO.cs
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

namespace MinCai.Simulators.Flexim.MemoryHierarchy
{
	public sealed partial class CacheGeometry
	{
		public sealed class Serializer: XmlConfigSerializer<CacheGeometry>
		{
			public Serializer()
			{
			}
			
			public override XmlConfig Save (CacheGeometry cacheGeometry)
			{
				XmlConfig xmlConfig = new XmlConfig("CacheGeometry");
				xmlConfig["size"] = cacheGeometry.Size + "";
				xmlConfig["associativity"] = cacheGeometry.Associativity + "";
				xmlConfig["lineSize"] = cacheGeometry.LineSize + "";
				
				return xmlConfig;
			}
			
			public override CacheGeometry Load (XmlConfig xmlConfig)
			{
				uint size = uint.Parse(xmlConfig["size"]);
				uint associativity = uint.Parse(xmlConfig["associativity"]);
				uint lineSize = uint.Parse(xmlConfig["lineSize"]);
				
				CacheGeometry cacheGeometry = new CacheGeometry(size, associativity, lineSize);
				
				return cacheGeometry;
			}
			
			public static Serializer SingleInstance = new Serializer();
		}
	}
}

namespace MinCai.Simulators.Flexim.Interop
{
	public sealed partial class Workload
	{
		public sealed class Serializer : XmlConfigSerializer<Workload>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (Workload workload)
			{
				XmlConfig xmlConfig = new XmlConfig ("Workload");
				xmlConfig["title"] = workload.Title;
				xmlConfig["cwd"] = workload.Cwd;
				xmlConfig["exe"] = workload.Exe;
				xmlConfig["args"] = workload.Args;
				xmlConfig["stdin"] = workload.Stdin;
				xmlConfig["stdout"] = workload.Stdout;
				xmlConfig["numThreadsNeeded"] = workload.NumThreadsNeeded + "";
				
				return xmlConfig;
			}

			public override Workload Load (XmlConfig xmlConfig)
			{
				string title = xmlConfig["title"];
				string cwd = xmlConfig["cwd"];
				string exe = xmlConfig["exe"];
				string args = xmlConfig["args"];
				string stdin = xmlConfig["stdin"];
				string stdout = xmlConfig["stdout"];
				uint numThreadsNeeded = uint.Parse (xmlConfig["numThreadsNeeded"]);
				
				Workload workload = new Workload (title, cwd, exe, args, stdin, stdout, numThreadsNeeded);
				
				return workload;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class WorkloadSet
	{
		public sealed class Serializer : XmlConfigSerializer<WorkloadSet>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (WorkloadSet workloadSet)
			{
				XmlConfig xmlConfig = new XmlConfig ("WorkloadSet");
				
				xmlConfig["title"] = workloadSet.Title;
				
				foreach (var pair in workloadSet.Workloads) {
					Workload workload = pair.Value;
					
					xmlConfig.Entries.Add (Workload.Serializer.SingleInstance.Save(workload));
				}
				
				return xmlConfig;
			}

			public override WorkloadSet Load (XmlConfig xmlConfig)
			{
				string workloadSetTitle = xmlConfig["title"];
				
				WorkloadSet workloadSet = new WorkloadSet (workloadSetTitle);
				
				foreach (var entry in xmlConfig.Entries) {
					workloadSet.Register (Workload.Serializer.SingleInstance.Load(entry));
				}
				
				return workloadSet;
			}

			public void SaveXML (WorkloadSet workloadSet)
			{
				this.SaveXML (workloadSet, Processor.WorkDirectory + Path.DirectorySeparatorChar + "configs/workloads", workloadSet.Title + ".xml");
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}
	
	public sealed partial class TlbConfig: Config
	{
		public sealed class Serializer: XmlConfigSerializer<TlbConfig>
		{
			public Serializer()
			{
			}
			
			public override XmlConfig Save (TlbConfig tlbConfig)
			{
				XmlConfig xmlConfig = new XmlConfig("TlbConfig");
				xmlConfig["hitLatency"] = tlbConfig.HitLatency + "";
				xmlConfig["missLatency"] = tlbConfig.MissLatency + "";
				
				xmlConfig.Entries.Add(CacheGeometry.Serializer.SingleInstance.Save(tlbConfig.Geometry));
				
				return xmlConfig;
			}
			
			public override TlbConfig Load (XmlConfig xmlConfig)
			{
				uint hitLatency = uint.Parse(xmlConfig["hitLatency"]);
				uint missLatency = uint.Parse(xmlConfig["missLatency"]);
				
				CacheGeometry geometry = CacheGeometry.Serializer.SingleInstance.Load(xmlConfig.Entries[0]);
				
				TlbConfig tlbConfig = new TlbConfig(geometry, hitLatency, missLatency);
				
				return tlbConfig;
			}
			
			public static Serializer SingleInstance = new Serializer();
		}
	}
	
	public sealed partial class CacheConfig : Config
	{
		public sealed class Serializer : XmlConfigSerializer<CacheConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (CacheConfig cacheConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("CacheConfig");
				
				xmlConfig["name"] = cacheConfig.Name;
				xmlConfig["level"] = cacheConfig.Level + "";
				
				xmlConfig["hitLatency"] = cacheConfig.HitLatency + "";
				xmlConfig["policy"] = cacheConfig.Policy + "";
				
				xmlConfig.Entries.Add(CacheGeometry.Serializer.SingleInstance.Save(cacheConfig.Geometry));
				
				return xmlConfig;
			}

			public override CacheConfig Load (XmlConfig xmlConfig)
			{
				string name = xmlConfig["name"];
				uint level = uint.Parse (xmlConfig["level"]);
				
				uint hitLatency = uint.Parse (xmlConfig["hitLatency"]);
				CacheReplacementPolicy policy = (CacheReplacementPolicy)Enum.Parse (typeof(CacheReplacementPolicy), xmlConfig["policy"]);
				
				CacheGeometry geometry = CacheGeometry.Serializer.SingleInstance.Load(xmlConfig.Entries[0]);
				
				CacheConfig cacheConfig = new CacheConfig (name, level, geometry, hitLatency, policy);
				return cacheConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class MainMemoryConfig : Config
	{
		public sealed class Serializer : XmlConfigSerializer<MainMemoryConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (MainMemoryConfig mainMemoryConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("MainMemoryConfig");
				
				xmlConfig["latency"] = mainMemoryConfig.Latency + "";
				
				return xmlConfig;
			}

			public override MainMemoryConfig Load (XmlConfig xmlConfig)
			{
				uint latency = uint.Parse (xmlConfig["latency"]);
				
				MainMemoryConfig mainMemoryConfig = new MainMemoryConfig (latency);
				
				return mainMemoryConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class ContextConfig : Config
	{
		public sealed class Serializer : XmlConfigSerializer<ContextConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (ContextConfig contextConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("ContextConfig");
				
				xmlConfig["workloadSetTitle"] = contextConfig.Workload.WorkloadSet.Title;
				xmlConfig["workloadTitle"] = contextConfig.Workload.Title;
				
				return xmlConfig;
			}

			public override ContextConfig Load (XmlConfig xmlConfig)
			{
				string workloadSetTitle = xmlConfig["workloadSetTitle"];
				string workloadTitle = xmlConfig["workloadTitle"];
				
				Workload workload = WorkloadSet.Serializer.SingleInstance.LoadXML (Processor.WorkDirectory + Path.DirectorySeparatorChar + "configs/workloads", workloadSetTitle + ".xml")[workloadTitle];
				
				ContextConfig contextConfig = new ContextConfig (workload);
				
				return contextConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class CoreConfig : Config
	{
		public sealed class Serializer : XmlConfigSerializer<CoreConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (CoreConfig coreConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("CoreConfig");
				
				xmlConfig.Entries.Add (CacheConfig.Serializer.SingleInstance.Save (coreConfig.ICache));
				xmlConfig.Entries.Add (CacheConfig.Serializer.SingleInstance.Save (coreConfig.DCache));
				
				return xmlConfig;
			}

			public override CoreConfig Load (XmlConfig xmlConfig)
			{
				CacheConfig iCache = CacheConfig.Serializer.SingleInstance.Load (xmlConfig.Entries[0]);
				CacheConfig dCache = CacheConfig.Serializer.SingleInstance.Load (xmlConfig.Entries[1]);
				
				CoreConfig coreConfig = new CoreConfig (iCache, dCache);
				
				return coreConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class ProcessorConfig : Config
	{
		public sealed class Serializer : XmlConfigSerializer<ProcessorConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (ProcessorConfig processorConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("ProcessorConfig");
				
				xmlConfig["maxCycle"] = processorConfig.MaxCycle + "";
				xmlConfig["maxInsts"] = processorConfig.MaxInsts + "";
				xmlConfig["maxTime"] = processorConfig.MaxTime + "";
				xmlConfig["numThreadsPerCore"] = processorConfig.NumThreadsPerCore + "";
				
				xmlConfig["physicalRegisterFileCapacity"] = processorConfig.PhysicalRegisterFileCapacity + "";
				xmlConfig["decodeWidth"] = processorConfig.DecodeWidth + "";
				xmlConfig["issueWidth"] = processorConfig.IssueWidth + "";
				xmlConfig["commitWidth"] = processorConfig.CommitWidth + "";
				xmlConfig["decodeBufferCapacity"] = processorConfig.DecodeBufferCapcity + "";
				xmlConfig["reorderBufferCapacity"] = processorConfig.ReorderBufferCapacity + "";
				xmlConfig["loadStoreQueueCapacity"] = processorConfig.LoadStoreQueueCapacity + "";
				
				ProcessorConfig.Serializer.SaveList("Cores", processorConfig.Cores, coreConfig => CoreConfig.Serializer.SingleInstance.Save(coreConfig));
				xmlConfig.Entries.Add(TlbConfig.Serializer.SingleInstance.Save(processorConfig.Tlb));
				
				return xmlConfig;
			}

			public override ProcessorConfig Load (XmlConfig xmlConfig)
			{
				ulong maxCycle = ulong.Parse (xmlConfig["maxCycle"]);
				ulong maxInsts = ulong.Parse (xmlConfig["maxInsts"]);
				ulong maxTime = ulong.Parse (xmlConfig["maxTime"]);
				uint numThreadsPerCore = uint.Parse (xmlConfig["numThreadsPerCore"]);
				
				uint physicalRegisterFileCapacity = uint.Parse (xmlConfig["physicalRegisterFileCapacity"]);
				uint decodeWidth = uint.Parse (xmlConfig["decodeWidth"]);
				uint issueWidth = uint.Parse (xmlConfig["issueWidth"]);
				uint commitWidth = uint.Parse (xmlConfig["commitWidth"]);
				uint decodeBufferCapacity = uint.Parse (xmlConfig["decodeBufferCapacity"]);
				uint reorderBufferCapacity = uint.Parse (xmlConfig["reorderBufferCapacity"]);
				uint loadStoreQueueCapacity = uint.Parse (xmlConfig["loadStoreQueueCapacity"]);
				
				ProcessorConfig processorConfig = new ProcessorConfig (maxCycle, maxInsts, maxTime, numThreadsPerCore, physicalRegisterFileCapacity, decodeWidth, issueWidth, commitWidth, decodeBufferCapacity, reorderBufferCapacity,
				loadStoreQueueCapacity);
				
				processorConfig.Cores.AddRange(ProcessorConfig.Serializer.LoadList(xmlConfig.Entries[0], entry => CoreConfig.Serializer.SingleInstance.Load(entry)));
				processorConfig.Tlb = TlbConfig.Serializer.SingleInstance.Load(xmlConfig.Entries[1]);				
				
				return processorConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class ArchitectureConfig : Config
	{
		public sealed class Serializer : XmlConfigSerializer<ArchitectureConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (ArchitectureConfig architectureConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("ArchitectureConfig");
				
				xmlConfig["title"] = architectureConfig.Title;
				
				xmlConfig.Entries.Add (ProcessorConfig.Serializer.SingleInstance.Save (architectureConfig.Processor));
				xmlConfig.Entries.Add (CacheConfig.Serializer.SingleInstance.Save (architectureConfig.L2Cache));
				xmlConfig.Entries.Add (MainMemoryConfig.Serializer.SingleInstance.Save (architectureConfig.MainMemory));
				
				return xmlConfig;
			}

			public override ArchitectureConfig Load (XmlConfig xmlConfig)
			{
				string title = xmlConfig["title"];
				
				ProcessorConfig processor = ProcessorConfig.Serializer.SingleInstance.Load (xmlConfig.Entries[0]);
				CacheConfig l2Cache = CacheConfig.Serializer.SingleInstance.Load (xmlConfig.Entries[1]);
				MainMemoryConfig mainMemory = MainMemoryConfig.Serializer.SingleInstance.Load (xmlConfig.Entries[2]);
				
				ArchitectureConfig architectureConfig = new ArchitectureConfig (title, processor, l2Cache, mainMemory);
				
				return architectureConfig;
			}

			public void SaveXML (ArchitectureConfig architectureConfig)
			{
				this.SaveXML (architectureConfig, MinCai.Simulators.Flexim.Microarchitecture.Processor.WorkDirectory + Path.DirectorySeparatorChar + "configs/architectures", architectureConfig.Title + ".xml");
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class SimulationConfig : Config
	{
		public sealed class Serializer : XmlConfigSerializer<SimulationConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (SimulationConfig simulationConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("SimulationConfig");
				
				xmlConfig["architectureConfigTitle"] = simulationConfig.Architecture.Title;
				
				foreach (var context in simulationConfig.Contexts) {
					xmlConfig.Entries.Add (ContextConfig.Serializer.SingleInstance.Save (context));
				}
				
				return xmlConfig;
			}

			public override SimulationConfig Load (XmlConfig xmlConfig)
			{
				string architectureConfigTitle = xmlConfig["architectureConfigTitle"];
				
				ArchitectureConfig architecture = ArchitectureConfig.Serializer.SingleInstance.LoadXML (Processor.WorkDirectory + Path.DirectorySeparatorChar + "configs/architectures", architectureConfigTitle + ".xml");
				
				SimulationConfig simulationConfig = new SimulationConfig (architecture);
				
				foreach (var entry in xmlConfig.Entries) {
					simulationConfig.Contexts.Add (ContextConfig.Serializer.SingleInstance.Load (entry));
				}
				
				return simulationConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}
	
	public sealed partial class TlbStat: Stat
	{
		public sealed class Serializer: XmlConfigSerializer<TlbStat>
		{
			public Serializer()
			{
			}
			
			public override XmlConfig Save (TlbStat tlbStat)
			{
				XmlConfig xmlConfig = new XmlConfig("TlbStat");
				xmlConfig["name"] = tlbStat.Name;
				xmlConfig["accesses"] = tlbStat.Accesses + "";
				xmlConfig["hits"] = tlbStat.Hits + "";
				xmlConfig["evictions"] = tlbStat.Evictions + "";
				
				return xmlConfig;
			}
			
			public override TlbStat Load (XmlConfig xmlConfig)
			{
				string name = xmlConfig["name"];
				uint accesses = uint.Parse(xmlConfig["accesses"]);
				uint hits = uint.Parse(xmlConfig["hits"]);
				uint evictions = uint.Parse(xmlConfig["evictions"]);
				
				TlbStat tlbStat = new TlbStat(name);
				tlbStat.Accesses = accesses;
				tlbStat.Hits = hits;
				tlbStat.Evictions = evictions;
				
				return tlbStat;
			}
			
			public static Serializer SingleInstance = new Serializer();
		}
	}

	public sealed partial class CacheStat : Stat
	{
		public sealed class Serializer : XmlConfigSerializer<CacheStat>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (CacheStat cacheStat)
			{
				XmlConfig xmlConfig = new XmlConfig ("CacheStat");
				
				xmlConfig["accesses"] = cacheStat.Accesses + "";
				xmlConfig["hits"] = cacheStat.Hits + "";
				xmlConfig["evictions"] = cacheStat.Evictions + "";
				xmlConfig["reads"] = cacheStat.Reads + "";
				xmlConfig["blockingReads"] = cacheStat.BlockingReads + "";
				xmlConfig["nonblockingReads"] = cacheStat.NonblockingReads + "";
				xmlConfig["readHits"] = cacheStat.ReadHits + "";
				xmlConfig["writes"] = cacheStat.Writes + "";
				xmlConfig["blockingWrites"] = cacheStat.BlockingWrites + "";
				xmlConfig["nonblockingWrites"] = cacheStat.NonblockingWrites + "";
				xmlConfig["writeHits"] = cacheStat.WriteHits + "";
				
				xmlConfig["readRetries"] = cacheStat.ReadRetries + "";
				xmlConfig["writeRetries"] = cacheStat.WriteRetries + "";
				
				xmlConfig["noRetryAccesses"] = cacheStat.NoRetryAccesses + "";
				xmlConfig["noRetryHits"] = cacheStat.NoRetryHits + "";
				xmlConfig["noRetryReads"] = cacheStat.NoRetryReads + "";
				xmlConfig["noRetryReadHits"] = cacheStat.NoRetryReadHits + "";
				xmlConfig["noRetryWrites"] = cacheStat.NoRetryWrites + "";
				xmlConfig["noRetryWriteHits"] = cacheStat.NoRetryWriteHits + "";
				
				return xmlConfig;
			}

			public override CacheStat Load (XmlConfig xmlConfig)
			{
				ulong accesses = ulong.Parse (xmlConfig["accesses"]);
				ulong hits = ulong.Parse (xmlConfig["hits"]);
				ulong evictions = ulong.Parse (xmlConfig["evictions"]);
				ulong reads = ulong.Parse (xmlConfig["reads"]);
				ulong blockingReads = ulong.Parse (xmlConfig["blockingReads"]);
				ulong nonblockingReads = ulong.Parse (xmlConfig["nonblockingReads"]);
				ulong readHits = ulong.Parse (xmlConfig["readHits"]);
				ulong writes = ulong.Parse (xmlConfig["writes"]);
				ulong blockingWrites = ulong.Parse (xmlConfig["blockingWrites"]);
				ulong nonblockingWrites = ulong.Parse (xmlConfig["nonblockingWrites"]);
				ulong writeHits = ulong.Parse (xmlConfig["writeHits"]);
				
				ulong readRetries = ulong.Parse (xmlConfig["readRetries"]);
				ulong writeRetries = ulong.Parse (xmlConfig["writeRetries"]);
				
				ulong noRetryAccesses = ulong.Parse (xmlConfig["noRetryAccesses"]);
				ulong noRetryHits = ulong.Parse (xmlConfig["noRetryHits"]);
				ulong noRetryReads = ulong.Parse (xmlConfig["noRetryReads"]);
				ulong noRetryReadHits = ulong.Parse (xmlConfig["noRetryReadHits"]);
				ulong noRetryWrites = ulong.Parse (xmlConfig["noRetryWrites"]);
				ulong noRetryWriteHits = ulong.Parse (xmlConfig["noRetryWriteHits"]);
				
				CacheStat cacheStat = new CacheStat ();
				
				cacheStat.Accesses = accesses;
				cacheStat.Hits = hits;
				cacheStat.Evictions = evictions;
				cacheStat.Reads = reads;
				cacheStat.BlockingReads = blockingReads;
				cacheStat.NonblockingReads = nonblockingReads;
				cacheStat.ReadHits = readHits;
				cacheStat.Writes = writes;
				cacheStat.BlockingWrites = blockingWrites;
				cacheStat.NonblockingWrites = nonblockingWrites;
				cacheStat.WriteHits = writeHits;
				
				cacheStat.ReadRetries = readRetries;
				cacheStat.WriteRetries = writeRetries;
				
				cacheStat.NoRetryAccesses = noRetryAccesses;
				cacheStat.NoRetryHits = noRetryHits;
				cacheStat.NoRetryReads = noRetryReads;
				cacheStat.NoRetryReadHits = noRetryReadHits;
				cacheStat.NoRetryWrites = noRetryWrites;
				cacheStat.NoRetryWriteHits = noRetryWriteHits;
				
				return cacheStat;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class MainMemoryStat : Stat
	{
		public sealed class Serializer : XmlConfigSerializer<MainMemoryStat>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (MainMemoryStat mainMemoryStat)
			{
				XmlConfig xmlConfig = new XmlConfig ("MainMemoryStat");
				
				xmlConfig["accesses"] = mainMemoryStat.Accesses + "";
				xmlConfig["reads"] = mainMemoryStat.Reads + "";
				xmlConfig["writes"] = mainMemoryStat.Writes + "";
				
				return xmlConfig;
			}

			public override MainMemoryStat Load (XmlConfig xmlConfig)
			{
				ulong accesses = ulong.Parse (xmlConfig["accesses"]);
				ulong reads = ulong.Parse (xmlConfig["reads"]);
				ulong writes = ulong.Parse (xmlConfig["writes"]);
				
				MainMemoryStat mainMemoryStat = new MainMemoryStat ();
				mainMemoryStat.Accesses = accesses;
				mainMemoryStat.Reads = reads;
				mainMemoryStat.Writes = writes;
				
				return mainMemoryStat;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class ContextStat : Stat
	{
		public sealed class Serializer : XmlConfigSerializer<ContextStat>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (ContextStat contextStat)
			{
				XmlConfig xmlConfig = new XmlConfig ("ContextStat");
				
				xmlConfig["totalInsts"] = contextStat.TotalInsts + "";
				
				xmlConfig.Entries.Add(TlbStat.Serializer.SingleInstance.Save(contextStat.Itlb));
				xmlConfig.Entries.Add(TlbStat.Serializer.SingleInstance.Save(contextStat.Dtlb));
				
				return xmlConfig;
			}

			public override ContextStat Load (XmlConfig xmlConfig)
			{
				ulong totalInsts = ulong.Parse (xmlConfig["totalInsts"]);
				
				TlbStat itlb = TlbStat.Serializer.SingleInstance.Load(xmlConfig.Entries[0]);
				TlbStat dtlb = TlbStat.Serializer.SingleInstance.Load(xmlConfig.Entries[1]);
				
				ContextStat contextStat = new ContextStat ();
				contextStat.TotalInsts = totalInsts;
				contextStat.Itlb = itlb;
				contextStat.Dtlb = dtlb;
				
				return contextStat;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class CoreStat : Stat
	{
		public sealed class Serializer : XmlConfigSerializer<CoreStat>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (CoreStat coreStat)
			{
				XmlConfig xmlConfig = new XmlConfig ("CoreStat");
				
				xmlConfig.Entries.Add (CacheStat.Serializer.SingleInstance.Save (coreStat.ICache));
				xmlConfig.Entries.Add (CacheStat.Serializer.SingleInstance.Save (coreStat.DCache));
				
				return xmlConfig;
			}

			public override CoreStat Load (XmlConfig xmlConfig)
			{
				CacheStat iCache = CacheStat.Serializer.SingleInstance.Load (xmlConfig.Entries[0]);
				CacheStat dCache = CacheStat.Serializer.SingleInstance.Load (xmlConfig.Entries[1]);
				
				CoreStat coreStat = new CoreStat ();
				coreStat.ICache = iCache;
				coreStat.DCache = dCache;
				
				return coreStat;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class ProcessorStat : Stat
	{
		public sealed class Serializer : XmlConfigSerializer<ProcessorStat>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (ProcessorStat processorStat)
			{
				XmlConfig xmlConfig = new XmlConfig ("ProcessorStat");
				
				foreach (var core in processorStat.Cores) {
					xmlConfig.Entries.Add (CoreStat.Serializer.SingleInstance.Save (core));
				}
				
				foreach (var context in processorStat.Contexts) {
					xmlConfig.Entries.Add (ContextStat.Serializer.SingleInstance.Save (context));
				}
				
				return xmlConfig;
			}

			public override ProcessorStat Load (XmlConfig xmlConfig)
			{
				ProcessorStat processorStat = new ProcessorStat ();
				
				foreach (var entry in xmlConfig.Entries) {
					if (entry.TypeName.Equals ("CoreStat")) {
						processorStat.Cores.Add (CoreStat.Serializer.SingleInstance.Load (entry));
					} else if (entry.TypeName.Equals ("ContextStat")) {
						processorStat.Contexts.Add (ContextStat.Serializer.SingleInstance.Load (entry));
					} else {
						throw new Exception ();
					}
				}
				
				return processorStat;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class SimulationStat : Stat
	{
		public sealed class Serializer : XmlConfigSerializer<SimulationStat>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (SimulationStat simulationStat)
			{
				XmlConfig xmlConfig = new XmlConfig ("SimulationStat");
				
				xmlConfig["totalCycles"] = simulationStat.TotalCycles + "";
				xmlConfig["duration"] = simulationStat.Duration + "";
				xmlConfig["totalInsts"] = simulationStat.TotalInsts + "";
				xmlConfig["instsPerCycle"] = simulationStat.InstsPerCycle + "";
				xmlConfig["cyclesPerSecond"] = simulationStat.CyclesPerSecond + "";
				
				xmlConfig.Entries.Add (ProcessorStat.Serializer.SingleInstance.Save (simulationStat.Processor));
				xmlConfig.Entries.Add (CacheStat.Serializer.SingleInstance.Save (simulationStat.L2Cache));
				xmlConfig.Entries.Add (MainMemoryStat.Serializer.SingleInstance.Save (simulationStat.MainMemory));
				
				return xmlConfig;
			}

			public override SimulationStat Load (XmlConfig xmlConfig)
			{
				ulong totalCycles = ulong.Parse (xmlConfig["totalCycles"]);
				ulong duration = ulong.Parse (xmlConfig["duration"]);
				ulong totalInsts = ulong.Parse (xmlConfig["totalInsts"]);
				double instsPerCycle = double.Parse (xmlConfig["instsPerCycle"]);
				double cyclesPerSecond = double.Parse (xmlConfig["cyclesPerSecond"]);
				
				ProcessorStat processor = ProcessorStat.Serializer.SingleInstance.Load (xmlConfig.Entries[0]);
				CacheStat l2Cache = CacheStat.Serializer.SingleInstance.Load (xmlConfig.Entries[1]);
				MainMemoryStat mainMemory = MainMemoryStat.Serializer.SingleInstance.Load (xmlConfig.Entries[2]);
				
				SimulationStat simulationStat = new SimulationStat (processor);
				simulationStat.L2Cache = l2Cache;
				simulationStat.MainMemory = mainMemory;
				
				simulationStat.TotalCycles = totalCycles;
				simulationStat.Duration = duration;
				simulationStat.TotalInsts = totalInsts;
				simulationStat.InstsPerCycle = instsPerCycle;
				simulationStat.CyclesPerSecond = cyclesPerSecond;
				
				return simulationStat;
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}

	public sealed partial class Simulation
	{
		public sealed class Serializer : XmlConfigSerializer<Simulation>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (Simulation simulation)
			{
				XmlConfig xmlConfig = new XmlConfig ("Simulation");
				
				xmlConfig["title"] = simulation.Title;
				xmlConfig["cwd"] = simulation.Cwd;
				
				xmlConfig.Entries.Add (SimulationConfig.Serializer.SingleInstance.Save (simulation.Config));
				xmlConfig.Entries.Add (SimulationStat.Serializer.SingleInstance.Save (simulation.Stat));
				
				return xmlConfig;
			}

			public override Simulation Load (XmlConfig xmlConfig)
			{
				string title = xmlConfig["title"];
				string cwd = xmlConfig["cwd"];
				
				SimulationConfig config = SimulationConfig.Serializer.SingleInstance.Load (xmlConfig.Entries[0]);
				SimulationStat stat = SimulationStat.Serializer.SingleInstance.Load (xmlConfig.Entries[1]);
				
				Simulation simulation = new Simulation (title, cwd, config, stat);
				
				return simulation;
			}

			public void SaveXML (Simulation simulation)
			{
				this.SaveXML (simulation, Processor.WorkDirectory + Path.DirectorySeparatorChar + "simulations", simulation.Title + ".xml");
			}

			public static Serializer SingleInstance = new Serializer ();
		}
	}
}
