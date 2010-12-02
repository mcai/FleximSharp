/*
 * Misc.cs
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
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using MinCai.Simulators.Flexim.Common;
using MinCai.Simulators.Flexim.Interop;
using Mono.Unix.Native;

namespace MinCai.Simulators.Flexim.Common
{
	public static class BitUtils
	{
		/// Generate a 32-bit mask of 'nbits' 1s, right justified.
		public static uint Mask (int nbits)
		{
			return (nbits == 32) ? uint.MaxValue : (1u << nbits) - 1;
		}

		/// Generate a 64-bit mask of 'nbits' 1s, right justified.
		public static ulong Mask64 (int nbits)
		{
			return (nbits == 64) ? ulong.MaxValue : (1uL << nbits) - 1;
		}

		/// Extract the bitfield from position 'first' to 'last' (inclusive)
		/// from 'val' and right justify it.  MSB is numbered 31, LSB is 0.
		public static uint Bits (uint val, int first, int last)
		{
			int nbits = first - last + 1;
			return (val >> last) & Mask (nbits);
		}

		/// Extract the bitfield from position 'first' to 'last' (inclusive)
		/// from 'val' and right justify it.  MSB is numbered 63, LSB is 0.
		public static ulong Bits64 (ulong val, int first, int last)
		{
			int nbits = first - last + 1;
			return (val >> last) & Mask (nbits);
		}

		/// Extract the bit from this position from 'val' and right justify it.
		public static uint Bits (uint val, int bit)
		{
			return Bits (val, bit, bit);
		}

		/// Extract the bit from this position from 'val' and right justify it.
		public static ulong Bits64 (ulong val, int bit)
		{
			return Bits64 (val, bit, bit);
		}

		/// Mask off the given bits in place like bits() but without shifting.
		/// MSB is numbered 31, LSB is 0.
		public static uint Mbits (uint val, int first, int last)
		{
			return val & (Mask (first + 1) & ~Mask (last));
		}

		/// Mask off the given bits in place like bits() but without shifting.
		/// MSB is numbered 63, LSB is 0.
		public static ulong Mbits64 (ulong val, int first, int last)
		{
			return val & (Mask64 (first + 1) & ~Mask (last));
		}

		public static uint Mask (int first, int last)
		{
			return Mbits (uint.MaxValue, first, last);
		}

		public static ulong Mask64 (int first, int last)
		{
			return Mbits64 (ulong.MaxValue, first, last);
		}

		/// Sign-extend an N-bit value to 32 bits.
		public static int Sext (uint val, int n)
		{
			int sign_bit = (int)(Bits (val, n - 1, n - 1));
			return (int)(sign_bit != 0 ? (val | ~Mask (n)) : val);
		}

		/// Sign-extend an N-bit value to 32 bits.
		public static long Sext64 (ulong val, int n)
		{
			long sign_bit = (long)(Bits64 (val, n - 1, n - 1));
			return (long)(sign_bit != 0 ? (val | ~Mask64 (n)) : val);
		}

		public static uint RoundUp (uint n, uint alignment)
		{
			return (n + (uint)(alignment - 1)) & ~(uint)(alignment - 1);
		}

		public static uint RoundDown (uint n, uint alignment)
		{
			return n & ~(alignment - 1);
		}

		/// 32 bit is assumed.
		public static uint Aligned (uint n, uint i)
		{
			return RoundDown (n, i);
		}

		/// 32 bit is assumed.
		public static uint Aligned (uint n)
		{
			return RoundDown (n, 4);
		}

		/// 32 bit is assumed.
		public static uint GetBit (uint x, uint b)
		{
			return x & (1u << (int)b);
		}

		/// 32 bit is assumed.
		public static uint SetBit (uint x, uint b)
		{
			return x | (1u << (int)b);
		}

		/// 32 bit is assumed.
		public static uint ClearBit (uint x, uint b)
		{
			return x & ~(1u << (int)b);
		}

		/// 32 bit is assumed.
		public static uint SetBitValue (uint x, uint b, bool v)
		{
			return v ? SetBit (x, b) : ClearBit (x, b);
		}

		public static uint Mod (uint x, uint y)
		{
			return (x + y) % y;
		}

		public static bool GetFCC1 (uint fcsr, int cc)
		{
			if (cc == 0)
				return (fcsr & 0x800000) != 0;
			else
				return (fcsr & (0x1000000 << (int)cc)) != 0;
		}

		public static bool GetFCC (uint fcsr, int cc_idx)
		{
			int shift = (cc_idx == 0) ? 23 : cc_idx + 24;
			bool cc_val = ((fcsr >> shift) & 0x00000001) != 0;
			return cc_val;
		}

		public static void SetFCC (ref uint fcsr, int cc)
		{
			if (cc == 0)
				fcsr = (fcsr | 0x800000);
			else
				fcsr = (fcsr | (0x1000000u << cc));
		}

		public static void ClearFCC (ref uint fcsr, int cc)
		{
			if (cc == 0)
				fcsr = (fcsr & 0xFF7FFFFF);
			else
				fcsr = (fcsr & (0xFEFFFFFF << cc));
		}

		public static uint GenCCVector (uint fcsr, int cc_num, uint cc_val)
		{
			int cc_idx = (cc_num == 0) ? 23 : cc_num + 24;
			
			fcsr = Bits (fcsr, 31, cc_idx + 1) << (cc_idx + 1) | cc_val << cc_idx | Bits (fcsr, cc_idx - 1, 0);
			
			return fcsr;
		}
	}

	public static class PtrUtils
	{
		unsafe public static ulong Strlen (char* s)
		{
			ulong cnt = 0;
			byte* b = (byte*)s;
			while (*b != 0) {
				b++;
				cnt++;
			}
			return cnt;
		}

		unsafe public static void memcpy (byte* pDest, byte* pSource, int Count)
		{
			for (uint i = 0; i < Count; i++) {
				*pDest++ = *pSource++;
			}
		}

		unsafe public static void memset (byte* pDest, byte byteVal, int Count)
		{
			for (uint i = 0; i < Count; i++) {
				*pDest++ = byteVal;
			}
		}
	}

	public static class ListUtils
	{
		public static IEnumerable<T> AsReverseEnumerable<T> (this IEnumerable<T> items)
		{
			IList<T> list = items as IList<T>;
			if (list == null)
				list = new List<T> (items);
			for (int i = list.Count - 1; i >= 0; i--) {
				yield return list[i];
			}
		}
	}

	public class PipelineList<EntryT> where EntryT : class
	{
		public PipelineList (string name)
		{
			this.Name = name;
			this.Entries = new List<EntryT> ();
		}

		public bool Empty {
			get { return this.Entries.Count == 0; }
		}

		public uint Size {
			get { return (uint)this.Entries.Count; }
		}

		public void TakeFront ()
		{
			this.Entries.RemoveAt (0);
		}

		public void TakeBack ()
		{
			this.Entries.RemoveAt (this.Entries.Count - 1);
		}

		public EntryT Front {
			get {
				if (!this.Empty) {
					return this.Entries[0];
				}
				
				return null;
			}
		}

		public EntryT Back {
			get {
				if (!this.Empty) {
					return this.Entries[this.Entries.Count - 1];
				}
				
				return null;
			}
		}

		public void Remove (EntryT val)
		{
			this.Entries.Remove (val);
		}

		public virtual void Add (EntryT val)
		{
			this.Entries.Add (val);
		}

		public void Clear ()
		{
			this.Entries.Clear ();
		}

		public List<EntryT>.Enumerator GetEnumerator ()
		{
			return this.Entries.GetEnumerator ();
		}
		
		public override string ToString ()
		{
			return string.Format ("[PipelineList: Name={0}, Size={1}]", this.Name, this.Size);
		}

		public string Name { get; set; }
		public List<EntryT> Entries { get; private set; }
	}

	public class PipelineQueue<EntryT> : PipelineList<EntryT> where EntryT : class
	{
		public PipelineQueue (string name, uint capacity) : base(name)
		{
			this.Capacity = capacity;
		}

		public bool Full {
			get { return this.Size >= this.Capacity; }
		}

		public override void Add (EntryT val)
		{
			if (this.Full) {
				Logger.Fatalf (LogCategory.MISC, "%s", this);
			}
			
			base.Add (val);
		}
		
		public override string ToString ()
		{
			return string.Format ("[PipelineQueue: Name={0}, Capacity={1}, Size={2}, Full={3}]", this.Name, this.Capacity, this.Size, this.Full);
		}

		public uint Capacity { get; private set; }
	}

	public class Event<EventTypeT, EventContextT>
	{
		public Event (EventTypeT eventType, EventContextT context, ulong scheduled, ulong when)
		{
			this.EventType = eventType;
			this.Context = context;
			this.Scheduled = scheduled;
			this.When = when;
		}
		
		public override string ToString ()
		{
			return string.Format ("[Event: EventType={0}, Context={1}, Scheduled={2}, When={3}]", this.EventType, this.Context, this.Scheduled, this.When);
		}

		public EventTypeT EventType { get; set; }
		public EventContextT Context { get; set; }
		public ulong Scheduled { get; set; }
		public ulong When { get; set; }
	}

	public interface EventProcessor
	{
		void ProcessEvents ();
	}

	public delegate void VoidDelegate ();

	public delegate void HasErrorDelegate (bool hasError);

	public delegate void HasErrorAndIsSharedDelegate (bool hasError, bool isShared);

	public class DelegateEventQueue : EventProcessor
	{
		public class EventT
		{
			public EventT (VoidDelegate del, ulong when)
			{
				this.Del = del;
				this.When = when;
			}

			public VoidDelegate Del { get; set; }
			public ulong When { get; set; }
		}

		public DelegateEventQueue ()
		{
			this.Events = new Dictionary<ulong, List<EventT>> ();
		}

		public void ProcessEvents ()
		{
			if (this.Events.ContainsKey (Simulator.CurrentCycle)) {
				foreach (EventT evt in this.Events[Simulator.CurrentCycle]) {
					evt.Del ();
				}
				
				this.Events.Remove (Simulator.CurrentCycle);
			}
		}

		public void Schedule (VoidDelegate del, ulong delay)
		{
			ulong when = Simulator.CurrentCycle + delay;
			
			if (!this.Events.ContainsKey (when)) {
				this.Events[when] = new List<EventT> ();
			}
			
			this.Events[when].Add (new EventT (del, when));
		}

		public Dictionary<ulong, List<EventT>> Events { get; private set; }
	}


	public class StringValue : Attribute
	{
		public StringValue (string value)
		{
			this.Value = value;
		}

		public string Value { get; private set; }
	}

	public static class EnumUtils
	{
		public static string ToStringValue (Enum e)
		{
			FieldInfo fi = e.GetType ().GetField (e.ToString ());
			StringValue[] attributes = (StringValue[])fi.GetCustomAttributes (typeof(StringValue), false);
			if (attributes.Length > 0) {
				return attributes[0].Value;
			} else {
				return e.ToString ();
			}
		}

		public static T Parse<T> (string value)
		{
			Type enumType = typeof(T);
			string[] names = Enum.GetNames (enumType);
			foreach (string name in names) {
				if ((ToStringValue ((Enum)Enum.Parse (enumType, name))).Equals (value)) {
					return (T)(Enum.Parse (enumType, name));
				}
			}
			
			throw new ArgumentException ("The string is not a description or value of the specified enum.");
		}
	}

	public sealed class IniFile
	{
		public sealed class Section
		{
			public Section (string name)
			{
				this.Name = name;
				this.Properties = new Dictionary<string, Property> ();
			}

			public void Register (Property property)
			{
				this[property.Name] = property;
			}

			public Property this[string name] {
				get { return this.Properties[name]; }
				set { this.Properties[name] = value; }
			}

			public string Name { get; set; }

			public Dictionary<string, Property> Properties { get; private set; }
		}

		public sealed class Property
		{
			public Property (string name, string val)
			{
				this.Name = name;
				this.Value = val;
			}

			public string Name { get; set; }
			public string Value { get; set; }
		}

		public IniFile ()
		{
			this.Sections = new Dictionary<string, Section> ();
		}

		public void Load (string fileName)
		{
			string sectionName = "";
			Section section = null;
			
			StreamReader sr = new StreamReader (fileName);
			
			string line;
			while ((line = sr.ReadLine ()) != null) {
				line = line.Trim ();
				
				if (line.Length == 0)
					continue;
				
				if (line[0] == ';' || line[0] == '#')
					continue;
				
				if (line[0] == '[') {
					sectionName = line.Substring (1, line.Length - 2);
					
					section = new Section (sectionName);
					this[section.Name] = section;
					
					continue;
				}
				
				int pos;
				if ((pos = line.IndexOf ('=')) == -1)
					continue;
				
				string name = line.Substring (0, pos - 1).Trim ();
				string val = line.Substring (pos + 1).Trim ();
				
				if (val.Length > 0) {
					if (val[0] == '"')
						val = val.Substring (1);
					if (val[val.Length - 1] == '"')
						val = val.Substring (0, val.Length - 1);
				}
				
				section[name] = new IniFile.Property (name, val);
			}
			
			sr.Close ();
		}

		public void save (string fileName)
		{
			StreamWriter sw = new StreamWriter (fileName);
			
			foreach (KeyValuePair<string, Section> sectionPair in this.Sections) {
				string sectionName = sectionPair.Key;
				Section section = sectionPair.Value;
				
				sw.WriteLine ("[ " + sectionName + " ]");
				foreach (KeyValuePair<string, Property> propertyPair in section.Properties) {
					Property property = propertyPair.Value;
					sw.WriteLine (property.Name + " = " + property.Value);
				}
				sw.WriteLine ();
			}
			
			sw.Close ();
		}

		public void Register (Section section)
		{
			this[section.Name] = section;
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			
			foreach (KeyValuePair<string, Section> sectionPair in this.Sections) {
				string sectionName = sectionPair.Key;
				Section section = sectionPair.Value;
				
				sb.Append ("[" + sectionName.Trim () + "]\n");
				
				foreach (KeyValuePair<string, Property> propertyPair in section.Properties) {
					Property property = propertyPair.Value;
					
					sb.Append (string.Format ("{0:s} = {1:s}\n", property.Name, property.Value));
				}
			}
			
			return sb.ToString ();
		}

		public Section this[string name] {
			get { return this.Sections[name]; }
			set {
				value.Name = value.Name.Trim ();
				this.Sections[value.Name] = value;
			}
		}

		public Dictionary<string, Section> Sections { get; private set; }
	}

	public class XmlConfig
	{
		public XmlConfig (string typeName)
		{
			this.TypeName = typeName;
			this.Attributes = new SortedDictionary<string, string> ();
			this.Entries = new List<XmlConfig> ();
		}

		public bool IsNull {
			get { return this.Attributes.ContainsKey (IS_NULL) && bool.Parse (this[IS_NULL]) == true; }
		}
		
		public override string ToString ()
		{
			return string.Format ("[XmlConfig: TypeName={0}, Attributes.Count={1}]", this.TypeName, this.Attributes.Count);
		}

		public string this[string index] {
			get { return this.Attributes[index]; }
			set { this.Attributes[index] = value; }
		}

		public string TypeName { get; set; }
		public SortedDictionary<string, string> Attributes { get; set; }
		public List<XmlConfig> Entries { get; set; }

		public static XmlConfig Null (string typeName)
		{
			XmlConfig xmlConfig = new XmlConfig (typeName);
			xmlConfig[IS_NULL] = true + "";
			return xmlConfig;
		}

		private static string IS_NULL = "IsNull";
	}

	public sealed class XmlConfigFile : XmlConfig
	{
		public XmlConfigFile (string typeName) : base(typeName)
		{
		}
	}

	public delegate XmlConfig SaveEntryDelegate<T> (T entry);
	public delegate T LoadEntryDelegate<T> (XmlConfig xmlConfig);

	public delegate KeyT KeyOf<KeyT, EntryT> (EntryT entry);

	public abstract class XmlConfigSerializer<T>
	{
		public abstract XmlConfig Save (T config);
		public abstract T Load (XmlConfig xmlConfig);

		public static XmlConfig SaveList<K> (string name, List<K> entries, SaveEntryDelegate<K> saveEntry)
		{
			XmlConfig xmlConfig = new XmlConfig (name);
			
			foreach (K entry in entries) {
				xmlConfig.Entries.Add (saveEntry (entry));
			}
			
			return xmlConfig;
		}

		public static List<K> LoadList<K> (XmlConfig xmlConfig, LoadEntryDelegate<K> loadEntry)
		{
			List<K> entries = new List<K> ();
			
			foreach (XmlConfig child in xmlConfig.Entries) {
				entries.Add (loadEntry (child));
			}
			
			return entries;
		}

		public static XmlConfig SaveUintDictionary<K> (string name, SortedDictionary<uint, K> entries, SaveEntryDelegate<K> saveEntry)
		{
			XmlConfig xmlConfig = new XmlConfig (name);
			
			foreach (KeyValuePair<uint, K> pair in entries) {
				XmlConfig child = saveEntry (pair.Value);
				
				xmlConfig.Entries.Add (child);
			}
			
			return xmlConfig;
		}

		public static SortedDictionary<uint, K> LoadUintDictionary<K> (XmlConfig xmlConfig, LoadEntryDelegate<K> loadEntry, KeyOf<uint, K> keyOf)
		{
			SortedDictionary<uint, K> entries = new SortedDictionary<uint, K> ();
			
			foreach (XmlConfig child in xmlConfig.Entries) {
				K entry = loadEntry (child);
				entries[keyOf (entry)] = entry;
			}
			
			return entries;
		}

		public static XmlConfig SaveStringDictionary<ValueT> (string name, SortedDictionary<string, ValueT> entries, SaveEntryDelegate<ValueT> saveEntry)
		{
			XmlConfig xmlConfig = new XmlConfig (name);
			
			foreach (KeyValuePair<string, ValueT> pair in entries) {
				XmlConfig child = saveEntry (pair.Value);
				
				xmlConfig.Entries.Add (child);
			}
			
			return xmlConfig;
		}

		public static SortedDictionary<string, K> LoadStringDictionary<K> (XmlConfig xmlConfig, LoadEntryDelegate<K> loadEntry, KeyOf<string, K> keyOf)
		{
			SortedDictionary<string, K> entries = new SortedDictionary<string, K> ();
			
			foreach (XmlConfig child in xmlConfig.Entries) {
				K entry = loadEntry (child);
				entries[keyOf (entry)] = entry;
			}
			
			return entries;
		}
	}

	public abstract class XmlConfigFileSerializer<T>
	{
		public abstract XmlConfigFile Save (T config);
		public abstract T Load (XmlConfigFile xmlConfigFile);

		public void SaveXML (T config, string cwd, string fileName)
		{			
			SaveXML (config, cwd + Path.DirectorySeparatorChar + fileName);
		}

		public void SaveXML (T config, string xmlFileName)
		{
//			Logger.Infof(LogCategory.XML, "{0:s}.SaveXML({1:s})", "XMLConfigFileSerializer", xmlFileName);
			
			XmlConfigFile xmlConfigFile = this.Save (config);
			Serialize (xmlConfigFile, xmlFileName);
		}

		public T LoadXML (string cwd, string fileName)
		{
			return LoadXML (cwd + Path.DirectorySeparatorChar + fileName);
		}

		public T LoadXML (string xmlFileName)
		{
//			Logger.Infof(LogCategory.XML, "{0:s}.LoadXML({1:s})", "XMLConfigFileSerializer", xmlFileName);
			
			XmlConfigFile xmlConfigFile = Deserialize (xmlFileName);
			return this.Load (xmlConfigFile);
		}

		public static void Serialize (XmlConfig xmlConfig, XmlElement rootElement)
		{
			XmlElement element = rootElement.OwnerDocument.CreateElement (xmlConfig.TypeName);
			rootElement.AppendChild (element);
			
			Serialize (xmlConfig, rootElement, element);
		}

		public static void Serialize (XmlConfig xmlConfig, XmlElement rootElement, XmlElement element)
		{
			foreach (KeyValuePair<string, string> pair in xmlConfig.Attributes) {
				element.SetAttribute (pair.Key, pair.Value);
			}
			
			foreach (XmlConfig child in xmlConfig.Entries) {
				Serialize (child, element);
			}
		}

		public static void Serialize (XmlConfigFile xmlConfigFile, string xmlFileName)
		{
			XmlDocument doc = new XmlDocument ();
			
			XmlElement rootElement = doc.CreateElement (xmlConfigFile.TypeName);
			doc.AppendChild (rootElement);
			
			foreach (KeyValuePair<string, string> pair in xmlConfigFile.Attributes) {
				rootElement.SetAttribute (pair.Key, pair.Value);
			}
			
			foreach (XmlConfig child in xmlConfigFile.Entries) {
				Serialize (child, rootElement);
			}
			
			doc.Save (xmlFileName);
		}

		public static void Deserialize (XmlConfig rootEntry, XmlElement rootElement)
		{
			XmlConfig entry = new XmlConfig (rootElement.Name);
			
			foreach (XmlAttribute attribute in rootElement.Attributes) {
				entry[attribute.Name] = attribute.Value;
			}
			
			foreach (XmlNode node in rootElement.ChildNodes) {
				if (node is XmlElement) {
					XmlElement childElement = (XmlElement)node;
					Deserialize (entry, childElement);
				}
			}
			
			rootEntry.Entries.Add (entry);
		}

		public static XmlConfigFile Deserialize (string xmlFileName)
		{
			XmlTextReader reader = new XmlTextReader (xmlFileName);
			XmlDocument doc = new XmlDocument ();
			doc.Load (reader);
			reader.Close ();
			
			XmlConfigFile xmlConfigFile = new XmlConfigFile (doc.DocumentElement.Name);
			
			foreach (XmlAttribute attribute in doc.DocumentElement.Attributes) {
				xmlConfigFile[attribute.Name] = attribute.Value;
			}
			
			foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
				if (node is XmlElement) {
					XmlElement childElement = (XmlElement)node;
					Deserialize (xmlConfigFile, childElement);
				}
			}
			
			return xmlConfigFile;
		}
	}

	public enum LogCategory
	{
		EVENT_QUEUE,
		SIMULATOR,
		CORE,
		THREAD,
		PROCESS,
		REGISTER,
		REQUEST,
		CACHE,
		COHERENCE,
		MEMORY,
		NET,
		INSTRUCTION,
		SYSCALL,
		ELF,
		CONFIG,
		STAT,
		MISC,
		OOO,
		TEST,
		DEBUG,
		XML
	}

	public static class Logger
	{
		static Logger ()
		{
			LogSwitches = new Dictionary<LogCategory, bool> ();
			
			Enable (LogCategory.SIMULATOR);
			
			//		Enable(LogCategory.EVENT_QUEUE);
			//		Enable(LogCategory.PROCESSOR);
			//		Enable(LogCategory.REGISTER);
			//		Enable(LogCategory.THREAD);
//			Enable(LogCategory.PROCESS);
			Enable (LogCategory.REQUEST);
			//		Enable(LogCategory.CACHE);
			Enable (LogCategory.COHERENCE);
			//		Enable(LogCategory.MEMORY);
			//		Enable(LogCategory.NET);
			Enable (LogCategory.CONFIG);
			Enable (LogCategory.STAT);
			//		Enable(LogCategory.MISC);
			//		Enable(LogCategory.OOO);
			//		Enable(LogCategory.TEST);
			//		Enable(LogCategory.XML);
			Enable (LogCategory.DEBUG);
		}

		public static void Enable (LogCategory category)
		{
			LogSwitches[category] = true;
		}

		public static void Disable (LogCategory category)
		{
			LogSwitches[category] = false;
		}

		public static bool Enabled (LogCategory category)
		{
			return true;
//			return LogSwitches.ContainsKey (category) && LogSwitches[category];
		}

		public static string Message (string caption, string text)
		{
			return string.Format ("[{0:d}] \t{1:s}{2:s}", Simulator.CurrentCycle, caption.EndsWith ("info") ? "" : "[" + caption + "]", text);
		}

		public static void Infof (LogCategory category, string format, params object[] args)
		{
			Info (category, string.Format (format, args));
		}

		public static void Info (LogCategory category, string text)
		{
			if (Enabled (category)) {
				Console.WriteLine (Message (category + "|" + "info", text));
			}
		}

		public static void Warnf (LogCategory category, string format, params object[] args)
		{
			Warn (category, string.Format (format, args));
		}

		public static void Warn (LogCategory category, string text)
		{
			Console.Error.WriteLine (Message (category + "|" + "warn", text));
		}

		public static void Fatalf (LogCategory category, string format, params object[] args)
		{
			Fatal (category, string.Format (format, args));
		}

		public static void Fatal (LogCategory category, string text)
		{
			Console.Error.WriteLine (Message (category + "|" + "fatal", text));
			Syscall.exit (1);
		}

		public static void Panicf (LogCategory category, string format, params object[] args)
		{
			Panic (category, string.Format (format, args));
		}

		public static void Panic (LogCategory category, string text)
		{
			Console.Error.WriteLine (Message (category + "|" + "panic", text));
			Syscall.exit (-1);
		}

		public static void Halt (LogCategory category, string text)
		{
			Console.Error.WriteLine (Message (category + "|" + "halt", text));
			Simulator.SingleInstance.Halted = true;
		}

		public static Dictionary<LogCategory, bool> LogSwitches { get; private set; }
	}
}
