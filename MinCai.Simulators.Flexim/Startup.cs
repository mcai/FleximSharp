/*
 * Main.cs
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

using System.IO;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using MinCai.Simulators.Flexim.Microarchitecture;

namespace MinCai.Simulators.Flexim.Startup
{
	public class FleximMain
	{
		public static int Main (string[] args)
		{
			Logger.Info (LogCategory.Simulator, "Flexim# - A modular and highly extensible multicore simulator written in C#/Mono.");
			Logger.Info (LogCategory.Simulator, "Copyright © 2010 Min Cai (itecgo@163.com).");
			Logger.Info (LogCategory.Simulator, "");
			
//			string simulationTitle = "WCETBench-fir-1x1";
			//string simulationTitle = "WCETBench-fir-2x1";
			//string simulationTitle = "Olden_Custom1-em3d_original-1x1";
//				string simulationTitle = "Olden_Custom1-mst_original-1x1";
			//string simulationTitle = "Olden_Custom1-mst_original-2x1";
			string simulationTitle = "Olden_Custom1-mst_original-2x2";
			//string simulationTitle = "Olden_Custom1-mst_original-Olden_Custom1_em3d_original-2x1";
			
			Simulation simulation = Simulation.LoadXML (Processor.WorkDirectory + Path.DirectorySeparatorChar + "simulations", simulationTitle + ".xml");
			
			Logger.Infof (LogCategory.Simulator, "run simulation(title={0:s})", simulationTitle);
			
			simulation.Execute (delegate(Processor simulator) { });
			
			Simulation.SaveXML (simulation);
			
			return 0;
		}
	}
}

