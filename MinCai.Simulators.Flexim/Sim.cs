/*
 * Sim.cs
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
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.MemoryHierarchy;
using MinCai.Simulators.Flexim.Pipelines;

namespace MinCai.Simulators.Flexim.Interop
{
	public class Workload
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

		public override string ToString ()
		{
			return string.Format ("[Workload: Title={0}, Parent.Title={1}, Cwd={2}, Exe={3}, Args={4}, Stdin={5}, Stdout={6}, NumThreadsNeeded={7}]", this.Title, this.Parent.Title, this.Cwd, this.Exe, this.Args, this.Stdin, this.Stdout, this.NumThreadsNeeded);
		}

		public string Title { get; set; }
		public string Cwd { get; set; }
		public string Exe { get; set; }
		public string Args { get; set; }
		public string Stdin { get; set; }
		public string Stdout { get; set; }

		public uint NumThreadsNeeded { get; set; }
		public WorkloadSet Parent { get; set; }
	}

	public class WorkloadSet
	{
		public class Serializer : XmlConfigFileSerializer<WorkloadSet>
		{
			public Serializer ()
			{
			}

			public override XmlConfigFile Save (WorkloadSet workloadSet)
			{
				XmlConfigFile xmlConfigFile = new XmlConfigFile ("WorkloadSet");
				
				xmlConfigFile["title"] = workloadSet.Title;
				
				foreach (KeyValuePair<string, Workload> pair in workloadSet.Workloads) {
					Workload workload = pair.Value;
					
					XmlConfig xmlConfig = new XmlConfig ("Workload");
					xmlConfig["title"] = workload.Title;
					xmlConfig["cwd"] = workload.Cwd;
					xmlConfig["exe"] = workload.Exe;
					xmlConfig["args"] = workload.Args;
					xmlConfig["stdin"] = workload.Stdin;
					xmlConfig["stdout"] = workload.Stdout;
					xmlConfig["numThreadsNeeded"] = workload.NumThreadsNeeded + "";
					
					xmlConfigFile.Entries.Add (xmlConfig);
				}
				
				return xmlConfigFile;
			}

			public override WorkloadSet Load (XmlConfigFile xmlConfigFile)
			{
				string workloadSetTitle = xmlConfigFile["title"];
				
				WorkloadSet workloadSet = new WorkloadSet (workloadSetTitle);
				
				foreach (XmlConfig entry in xmlConfigFile.Entries) {
					string title = entry["title"];
					string cwd = entry["cwd"];
					string exe = entry["exe"];
					string args = entry["args"];
					string stdin = entry["stdin"];
					string stdout = entry["stdout"];
					uint numThreadsNeeded = uint.Parse (entry["numThreadsNeeded"]);
					
					Workload workload = new Workload (title, cwd, exe, args, stdin, stdout, numThreadsNeeded);
					workloadSet.Register (workload);
				}
				
				return workloadSet;
			}

			public static Serializer SingleInstance = new Serializer ();
		}

		public WorkloadSet (string title)
		{
			this.Title = title;
			this.Workloads = new Dictionary<string, Workload> ();
		}

		public void Register (Workload workload)
		{
			Debug.Assert (!this.Workloads.ContainsKey (workload.Title));
			
			workload.Parent = this;
			this.Workloads[workload.Title] = workload;
		}

		public override string ToString ()
		{
			return string.Format ("[WorkloadSet: Title={0}, Workloads.Count={1}]", this.Title, this.Workloads.Count);
		}

		public Workload this[string index] {
			get { return this.Workloads[index]; }
		}

		public string Title { get; set; }
		public Dictionary<string, Workload> Workloads { get; private set; }

		public static WorkloadSet LoadXML (string cwd, string fileName)
		{
			return Serializer.SingleInstance.LoadXML (cwd, fileName);
		}

		public void SaveXML (WorkloadSet workloadSet, string cwd, string fileName)
		{
			Serializer.SingleInstance.SaveXML (workloadSet, cwd, fileName);
		}

		public void SaveXML (WorkloadSet workloadSet)
		{
			SaveXML (workloadSet, Simulator.WorkDirectory + Path.DirectorySeparatorChar + "configs/workloads", workloadSet.Title + ".xml");
		}
	}

	public abstract class Config
	{
	}

	public class CacheConfig : Config
	{
		public class Serializer : XmlConfigSerializer<CacheConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (CacheConfig cacheConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("CacheConfig");
				
				xmlConfig["name"] = cacheConfig.Name;
				xmlConfig["level"] = cacheConfig.Level + "";
				xmlConfig["numSets"] = cacheConfig.NumSets + "";
				xmlConfig["assoc"] = cacheConfig.Assoc + "";
				xmlConfig["blockSize"] = cacheConfig.BlockSize + "";
				xmlConfig["hitLatency"] = cacheConfig.HitLatency + "";
				xmlConfig["missLatency"] = cacheConfig.MissLatency + "";
				xmlConfig["policy"] = cacheConfig.Policy + "";
				
				return xmlConfig;
			}

			public override CacheConfig Load (XmlConfig xmlConfig)
			{
				string name = xmlConfig["name"];
				uint level = uint.Parse (xmlConfig["level"]);
				uint numSets = uint.Parse (xmlConfig["numSets"]);
				uint assoc = uint.Parse (xmlConfig["assoc"]);
				uint blockSize = uint.Parse (xmlConfig["blockSize"]);
				uint hitLatency = uint.Parse (xmlConfig["hitLatency"]);
				uint missLatency = uint.Parse (xmlConfig["missLatency"]);
				CacheReplacementPolicy policy = (CacheReplacementPolicy)Enum.Parse (typeof(CacheReplacementPolicy), xmlConfig["policy"]);
				
				CacheConfig cacheConfig = new CacheConfig (name, level, numSets, assoc, blockSize, hitLatency, missLatency, policy);
				return cacheConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}

		public CacheConfig (string name, uint level, uint numSets, uint assoc, uint blockSize, uint hitLatency, uint missLatency, CacheReplacementPolicy policy)
		{
			this.Name = name;
			this.Level = level;
			this.NumSets = numSets;
			this.Assoc = assoc;
			this.BlockSize = blockSize;
			this.HitLatency = hitLatency;
			this.MissLatency = missLatency;
			this.Policy = policy;
		}

		public override string ToString ()
		{
			return string.Format ("[CacheConfig: Name={0}, Level={1}, NumSets={2}, Assoc={3}, BlockSize={4}, HitLatency={5}, MissLatency={6}, Policy={7}]", this.Name, this.Level, this.NumSets, this.Assoc, this.BlockSize, this.HitLatency, this.MissLatency, this.Policy);
		}

		public string Name { get; set; }
		public uint Level { get; set; }
		public uint NumSets { get; set; }
		public uint Assoc { get; set; }
		public uint BlockSize { get; set; }
		public uint HitLatency { get; set; }
		public uint MissLatency { get; set; }
		public CacheReplacementPolicy Policy { get; set; }
	}

	public class MainMemoryConfig : Config
	{
		public class Serializer : XmlConfigSerializer<MainMemoryConfig>
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

		public MainMemoryConfig (uint latency)
		{
			this.Latency = latency;
		}

		public override string ToString ()
		{
			return string.Format ("[MainMemoryConfig: Latency={0}]", this.Latency);
		}

		public uint Latency { get; set; }
	}

	public class ContextConfig : Config
	{
		public class Serializer : XmlConfigSerializer<ContextConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (ContextConfig contextConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("ContextConfig");
				
				xmlConfig["workloadSetTitle"] = contextConfig.Workload.Parent.Title;
				xmlConfig["workloadTitle"] = contextConfig.Workload.Title;
				
				return xmlConfig;
			}

			public override ContextConfig Load (XmlConfig xmlConfig)
			{
				string workloadSetTitle = xmlConfig["workloadSetTitle"];
				string workloadTitle = xmlConfig["workloadTitle"];
				
				Workload workload = WorkloadSet.LoadXML (Simulator.WorkDirectory + Path.DirectorySeparatorChar + "configs/workloads", workloadSetTitle + ".xml")[workloadTitle];
				
				ContextConfig contextConfig = new ContextConfig (workload);
				
				return contextConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}

		public ContextConfig (Workload workload)
		{
			this.Workload = workload;
		}

		public override string ToString ()
		{
			return string.Format ("[ContextConfig: Workload={0}]", this.Workload);
		}

		public Workload Workload { get; set; }
	}

	public class CoreConfig : Config
	{
		public class Serializer : XmlConfigSerializer<CoreConfig>
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

		public CoreConfig (CacheConfig iCache, CacheConfig dCache)
		{
			this.ICache = iCache;
			this.DCache = dCache;
		}

		public override string ToString ()
		{
			return string.Format ("[CoreConfig: ICache={0}, DCache={1}]", this.ICache, this.DCache);
		}

		public CacheConfig ICache { get; set; }
		public CacheConfig DCache { get; set; }
	}

	public class ProcessorConfig : Config
	{
		public class Serializer : XmlConfigSerializer<ProcessorConfig>
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
				
				foreach (CoreConfig core in processorConfig.Cores) {
					xmlConfig.Entries.Add (CoreConfig.Serializer.SingleInstance.Save (core));
				}
				
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
				
				foreach (XmlConfig entry in xmlConfig.Entries) {
					processorConfig.Cores.Add (CoreConfig.Serializer.SingleInstance.Load (entry));
				}
				
				return processorConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}

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

		public override string ToString ()
		{
			return string.Format ("[ProcessorConfig: MaxCycle={0}, MaxInsts={1}, MaxTime={2}, NumThreadsPerCore={3}, Cores.Count={4}, PhysicalRegisterFileCapacity={5}, DecodeWidth={6}, IssueWidth={7}, CommitWidth={8}, DecodeBufferCapcity={9}, ReorderBufferCapacity={10}, LoadStoreQueueCapacity={11}]", this.MaxCycle, this.MaxInsts, this.MaxTime, this.NumThreadsPerCore, this.Cores.Count, this.PhysicalRegisterFileCapacity, this.DecodeWidth, this.IssueWidth, this.CommitWidth,
			this.DecodeBufferCapcity, this.ReorderBufferCapacity, this.LoadStoreQueueCapacity);
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
	}

	public class ArchitectureConfig : Config
	{
		public class Serializer : XmlConfigFileSerializer<ArchitectureConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfigFile Save (ArchitectureConfig architectureConfig)
			{
				XmlConfigFile xmlConfigFile = new XmlConfigFile ("ArchitectureConfig");
				
				xmlConfigFile["title"] = architectureConfig.Title;
				
				xmlConfigFile.Entries.Add (ProcessorConfig.Serializer.SingleInstance.Save (architectureConfig.Processor));
				xmlConfigFile.Entries.Add (CacheConfig.Serializer.SingleInstance.Save (architectureConfig.L2Cache));
				xmlConfigFile.Entries.Add (MainMemoryConfig.Serializer.SingleInstance.Save (architectureConfig.MainMemory));
				
				return xmlConfigFile;
			}

			public override ArchitectureConfig Load (XmlConfigFile xmlConfigFile)
			{
				string title = xmlConfigFile["title"];
				
				ProcessorConfig processor = ProcessorConfig.Serializer.SingleInstance.Load (xmlConfigFile.Entries[0]);
				CacheConfig l2Cache = CacheConfig.Serializer.SingleInstance.Load (xmlConfigFile.Entries[1]);
				MainMemoryConfig mainMemory = MainMemoryConfig.Serializer.SingleInstance.Load (xmlConfigFile.Entries[2]);
				
				ArchitectureConfig architectureConfig = new ArchitectureConfig (title, processor, l2Cache, mainMemory);
				
				return architectureConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}

		public ArchitectureConfig (string title, ProcessorConfig processor, CacheConfig l2Cache, MainMemoryConfig mainMemory)
		{
			this.Title = title;
			this.Processor = processor;
			this.L2Cache = l2Cache;
			this.MainMemory = mainMemory;
		}

		public override string ToString ()
		{
			return string.Format ("[ArchitectureConfig: Title={0}, Processor={1}, L2Cache={2}, MainMemory={3}]", this.Title, this.Processor, this.L2Cache, this.MainMemory);
		}

		public Dictionary<string, CacheConfig> Caches {
			get {
				Dictionary<string, CacheConfig> caches = new Dictionary<string, CacheConfig> ();
				
				for (int i = 0; i < this.Processor.Cores.Count; i++) {
					CoreConfig core = this.Processor.Cores[i];
					
					caches["l1I-" + i] = core.ICache;
					caches["l1D-" + i] = core.DCache;
				}
				
				caches["l2"] = this.L2Cache;
				
				return caches;
			}
		}

		public string Title { get; set; }
		public ProcessorConfig Processor { get; set; }
		public CacheConfig L2Cache { get; set; }
		public MainMemoryConfig MainMemory { get; set; }

		public static ArchitectureConfig LoadXML (string cwd, string fileName)
		{
			return Serializer.SingleInstance.LoadXML (cwd, fileName);
		}

		public static void SaveXML (ArchitectureConfig architectureConfig)
		{
			SaveXML (architectureConfig, Simulator.WorkDirectory + Path.DirectorySeparatorChar + "configs/architectures", architectureConfig.Title + ".xml");
		}

		public static void SaveXML (ArchitectureConfig architectureConfig, string cwd, string fileName)
		{
			Serializer.SingleInstance.SaveXML (architectureConfig, cwd, fileName);
		}
	}

	public class SimulationConfig : Config
	{
		public class Serializer : XmlConfigSerializer<SimulationConfig>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (SimulationConfig simulationConfig)
			{
				XmlConfig xmlConfig = new XmlConfig ("SimulationConfig");
				
				xmlConfig["architectureConfigTitle"] = simulationConfig.Architecture.Title;
				
				foreach (ContextConfig context in simulationConfig.Contexts) {
					xmlConfig.Entries.Add (ContextConfig.Serializer.SingleInstance.Save (context));
				}
				
				return xmlConfig;
			}

			public override SimulationConfig Load (XmlConfig xmlConfig)
			{
				string architectureConfigTitle = xmlConfig["architectureConfigTitle"];
				
				ArchitectureConfig architecture = ArchitectureConfig.LoadXML (Simulator.WorkDirectory + Path.DirectorySeparatorChar + "configs/architectures", architectureConfigTitle + ".xml");
				
				SimulationConfig simulationConfig = new SimulationConfig (architecture);
				
				foreach (XmlConfig entry in xmlConfig.Entries) {
					simulationConfig.Contexts.Add (ContextConfig.Serializer.SingleInstance.Load (entry));
				}
				
				return simulationConfig;
			}

			public static Serializer SingleInstance = new Serializer ();
		}

		public SimulationConfig (ArchitectureConfig architecture)
		{
			this.Architecture = architecture;
			this.Contexts = new List<ContextConfig> ();
		}

		public override string ToString ()
		{
			return string.Format ("[SimulationConfig: Architecture={0}, Contexts.Count={1}]", this.Architecture, this.Contexts.Count);
		}

		public ArchitectureConfig Architecture { get; set; }
		public List<ContextConfig> Contexts { get; set; }
	}

	public abstract class Stat
	{
		public abstract void Reset ();
	}

	public class CacheStat : Stat
	{
		public class Serializer : XmlConfigSerializer<CacheStat>
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

		public override string ToString ()
		{
			return string.Format ("[CacheStat: Accesses={0}, Hits={1}, Evictions={2}, Reads={3}, BlockingReads={4}, NonblockingReads={5}, ReadHits={6}, Writes={7}, BlockingWrites={8}, NonblockingWrites={9}, WriteHits={10}, ReadRetries={11}, WriteRetries={12}, NoRetryAccesses={13}, NoRetryHits={14}, NoRetryReads={15}, NoRetryReadHits={16}, NoRetryWrites={17}, NoRetryWriteHits={18}]", this.Accesses, this.Hits, this.Evictions, this.Reads, this.BlockingReads, this.NonblockingReads, this.ReadHits, this.Writes, this.BlockingWrites,
			this.NonblockingWrites, this.WriteHits, this.ReadRetries, this.WriteRetries, this.NoRetryAccesses, this.NoRetryHits, this.NoRetryReads, this.NoRetryReadHits, this.NoRetryWrites, this.NoRetryWriteHits);
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

	public class MainMemoryStat : Stat
	{
		public class Serializer : XmlConfigSerializer<MainMemoryStat>
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

		public MainMemoryStat ()
		{
			this.Reset ();
		}

		public override string ToString ()
		{
			return string.Format ("[MainMemoryStat: Accesses={0}, Reads={1}, Writes={2}]", this.Accesses, this.Reads, this.Writes);
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

	public class ContextStat : Stat
	{
		public class Serializer : XmlConfigSerializer<ContextStat>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (ContextStat contextStat)
			{
				XmlConfig xmlConfig = new XmlConfig ("ContextStat");
				
				xmlConfig["totalInsts"] = contextStat.TotalInsts + "";
				
				return xmlConfig;
			}

			public override ContextStat Load (XmlConfig xmlConfig)
			{
				ulong totalInsts = ulong.Parse (xmlConfig["totalInsts"]);
				
				ContextStat contextStat = new ContextStat ();
				contextStat.TotalInsts = totalInsts;
				
				return contextStat;
			}

			public static Serializer SingleInstance = new Serializer ();
		}

		public ContextStat ()
		{
			this.Reset ();
		}

		public override void Reset ()
		{
			this.TotalInsts = 0;
		}

		public override string ToString ()
		{
			return string.Format ("[ContextStat: TotalInsts={0}]", this.TotalInsts);
		}

		public ulong TotalInsts { get; set; }
	}

	public class CoreStat : Stat
	{
		public class Serializer : XmlConfigSerializer<CoreStat>
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

		public override string ToString ()
		{
			return string.Format ("[CoreStat: ICache={0}, DCache={1}]", this.ICache, this.DCache);
		}

		public CacheStat ICache { get; set; }
		public CacheStat DCache { get; set; }
	}

	public class ProcessorStat : Stat
	{
		public class Serializer : XmlConfigSerializer<ProcessorStat>
		{
			public Serializer ()
			{
			}

			public override XmlConfig Save (ProcessorStat processorStat)
			{
				XmlConfig xmlConfig = new XmlConfig ("ProcessorStat");
				
				foreach (CoreStat core in processorStat.Cores) {
					xmlConfig.Entries.Add (CoreStat.Serializer.SingleInstance.Save (core));
				}
				
				foreach (ContextStat context in processorStat.Contexts) {
					xmlConfig.Entries.Add (ContextStat.Serializer.SingleInstance.Save (context));
				}
				
				return xmlConfig;
			}

			public override ProcessorStat Load (XmlConfig xmlConfig)
			{
				ProcessorStat processorStat = new ProcessorStat ();
				
				foreach (XmlConfig entry in xmlConfig.Entries) {
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

		public ProcessorStat ()
		{
			this.Cores = new List<CoreStat> ();
			this.Contexts = new List<ContextStat> ();
			
			this.Reset ();
		}

		public override void Reset ()
		{
			foreach (CoreStat core in this.Cores) {
				core.Reset ();
			}
			
			foreach (ContextStat context in this.Contexts) {
				context.Reset ();
			}
		}

		public override string ToString ()
		{
			return string.Format ("[ProcessorStat: Cores.Count={0}, Contexts.Count={1}]", this.Cores.Count, this.Contexts.Count);
		}

		public List<CoreStat> Cores { get; private set; }
		public List<ContextStat> Contexts { get; private set; }
	}

	public class SimulationStat : Stat
	{
		public class Serializer : XmlConfigSerializer<SimulationStat>
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

		public override string ToString ()
		{
			return string.Format ("[SimulationStat: Processor={0}, L2Cache={1}, MainMemory={2}, TotalCycles={3}, Duration={4}]", this.Processor, this.L2Cache, this.MainMemory, this.TotalCycles, this.Duration);
		}

		public ProcessorStat Processor { get; set; }
		public CacheStat L2Cache { get; set; }
		public MainMemoryStat MainMemory { get; set; }

		public ulong TotalCycles { get; set; }
		public ulong Duration { get; set; }
		public ulong TotalInsts { get; set; }
		public double InstsPerCycle { get; set; }
		public double CyclesPerSecond {get;set;}
	}

	public class Simulation
	{
		public class Serializer : XmlConfigFileSerializer<Simulation>
		{
			public Serializer ()
			{
			}

			public override XmlConfigFile Save (Simulation simulation)
			{
				XmlConfigFile xmlConfigFile = new XmlConfigFile ("Simulation");
				
				xmlConfigFile["title"] = simulation.Title;
				xmlConfigFile["cwd"] = simulation.Cwd;
				
				xmlConfigFile.Entries.Add (SimulationConfig.Serializer.SingleInstance.Save (simulation.Config));
				xmlConfigFile.Entries.Add (SimulationStat.Serializer.SingleInstance.Save (simulation.Stat));
				
				return xmlConfigFile;
			}

			public override Simulation Load (XmlConfigFile xmlConfigFile)
			{
				string title = xmlConfigFile["title"];
				string cwd = xmlConfigFile["cwd"];
				
				SimulationConfig config = SimulationConfig.Serializer.SingleInstance.Load (xmlConfigFile.Entries[0]);
				SimulationStat stat = SimulationStat.Serializer.SingleInstance.Load (xmlConfigFile.Entries[1]);
				
				Simulation simulation = new Simulation (title, cwd, config, stat);
				
				return simulation;
			}

			public static Serializer SingleInstance = new Serializer ();
		}

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

		public delegate void SimulatorInitDelegate (CPUSimulator simulator);

		public void Execute (SimulatorInitDelegate del)
		{
			this.BeforeRun (del);
			this.Run ();
			this.AfterRun ();
		}

		public void BeforeRun (SimulatorInitDelegate del)
		{
			this.SimulatorInitDel = del;
			this.Running = true;
			this.Stat.Reset ();
		}

		public void Run ()
		{
			CPUSimulator simulator = new CPUSimulator (this);
			
			if (this.SimulatorInitDel != null) {
				this.SimulatorInitDel (simulator);
			}
			
			simulator.Run ();
		}

		public void Abort ()
		{
			this.Running = false;
		}

		public void AfterRun ()
		{
			this.Running = false;
		}

		public override string ToString ()
		{
			return string.Format ("[Simulation: Title={0}, Cwd={1}, Config={2}, Stat={3}, Running={4}]", this.Title, this.Cwd, this.Config, this.Stat, this.Running);
		}

		public string Title { get; set; }
		public string Cwd { get; set; }
		public SimulationConfig Config { get; set; }
		public SimulationStat Stat { get; set; }

		public bool Running { get; set; }

		public SimulatorInitDelegate SimulatorInitDel { get; set; }

		public static Simulation LoadXML (string cwd, string fileName)
		{
			return Serializer.SingleInstance.LoadXML (cwd, fileName);
		}

		public static void SaveXML (Simulation simulation, string cwd, string fileName)
		{
			Serializer.SingleInstance.SaveXML (simulation, cwd, fileName);
		}

		public static void SaveXML (Simulation simulation)
		{
			SaveXML (simulation, Simulator.WorkDirectory + Path.DirectorySeparatorChar + "simulations", simulation.Title + ".xml");
		}
	}

	public abstract class Simulator
	{
		public Simulator ()
		{
			this.EventQueue = new DelegateEventQueue ();
			this.EventProcessors = new List<EventProcessor> ();
			this.AddEventProcessor (this.EventQueue);
			
			Simulator.SingleInstance = this;
			this.Halted = false;
		}

		public abstract void Run ();

		public void AddEventProcessor (EventProcessor eventProcessor)
		{
			this.EventProcessors.Add (eventProcessor);
		}

		public EventProcessor EventQueue { get; set; }
		public List<EventProcessor> EventProcessors { get; set; }
		public bool Halted { get; set; }

		static Simulator ()
		{
			CurrentCycle = 0;
			
			WorkDirectory = "../../../";
			//TODO: not hardcode!
		}

		public static Simulator SingleInstance { get; set; }
		public static ulong CurrentCycle { get; set; }

		public static string WorkDirectory { get; set; }
	}
}
