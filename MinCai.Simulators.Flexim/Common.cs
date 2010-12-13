/*
 * Common.cs
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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace MinCai.Simulators.Flexim.Common
{
	static internal class BitHelper
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

	static internal class StringHelper
	{
		public static byte[] StringToBytes (string str, out int bytesCount)
		{
			bytesCount = ASCIIEncoding.ASCII.GetByteCount (str) + 1;
			return ASCIIEncoding.ASCII.GetBytes (str + char.MinValue);
		}

		public static string BytesToString (byte[] bytes)
		{
			return ASCIIEncoding.ASCII.GetString (bytes.TakeWhile (b => !b.Equals (char.MinValue)).ToArray ());
		}
	}

	static internal class ListExtensions
	{
		public static bool IsFull<T> (this List<T> list, uint capacity)
		{
			return list.Count >= (int)capacity;
		}

		public static void RemoveFirst<T> (this List<T> list)
		{
			list.RemoveAt (0);
		}

		public static void RemoveLast<T> (this List<T> list)
		{
			list.RemoveAt (list.Count - 1);
		}
	}

	internal sealed class EnumHelper
	{
		public sealed class StringValue : Attribute
		{
			public StringValue (string value)
			{
				this.Value = value;
			}

			public string Value { get; private set; }
		}

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
			foreach (var name in names) {
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
			
			using (StreamReader sr = new StreamReader (fileName)) {
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
			}
		}

		public void Save (string fileName)
		{
			using (StreamWriter sw = new StreamWriter (fileName)) {
				foreach (var sectionPair in this.Sections) {
					string sectionName = sectionPair.Key;
					Section section = sectionPair.Value;
					
					sw.WriteLine ("[ " + sectionName + " ]");
					foreach (var propertyPair in section.Properties) {
						Property property = propertyPair.Value;
						sw.WriteLine (property.Name + " = " + property.Value);
					}
					sw.WriteLine ();
				}
			}
		}

		public void Register (Section section)
		{
			this[section.Name] = section;
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			
			foreach (var sectionPair in this.Sections) {
				string sectionName = sectionPair.Key;
				Section section = sectionPair.Value;
				
				sb.Append ("[" + sectionName.Trim () + "]\n");
				
				foreach (var propertyPair in section.Properties) {
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

		public bool IsPlaceHolder {
			get { return this.Attributes.ContainsKey (IS_PLACEHOLDER) && bool.Parse (this[IS_PLACEHOLDER]) == true; }
		}

		public string this[string index] {
			get { return this.Attributes[index]; }
			set { this.Attributes[index] = value; }
		}

		public string TypeName { get; set; }
		public SortedDictionary<string, string> Attributes { get; set; }
		public List<XmlConfig> Entries { get; set; }

		public static XmlConfig GetPlaceHolder (string typeName)
		{
			XmlConfig xmlConfig = new XmlConfig (typeName);
			xmlConfig[IS_PLACEHOLDER] = true + "";
			return xmlConfig;
		}

		private static string IS_PLACEHOLDER = "IsNull";
	}

	public abstract class XmlConfigSerializer<T>
	{
		public abstract XmlConfig Save (T config);
		public abstract T Load (XmlConfig xmlConfig);

		protected static XmlConfig SaveList<K> (string name, List<K> entries, Func<K, XmlConfig> saveEntry)
		{
			XmlConfig xmlConfig = new XmlConfig (name);
			
			entries.ForEach (entry => xmlConfig.Entries.Add (saveEntry (entry)));
			
			return xmlConfig;
		}

		protected static List<K> LoadList<K> (XmlConfig xmlConfig, Func<XmlConfig, K> loadEntry)
		{
			List<K> entries = new List<K> ();
			
			xmlConfig.Entries.ForEach (child => entries.Add (loadEntry (child)));
			
			return entries;
		}

		protected static XmlConfig SaveDictionary<KeyT, K> (string name, SortedDictionary<KeyT, K> entries, Func<K, XmlConfig> saveEntry)
		{
			XmlConfig xmlConfig = new XmlConfig (name);
			
			foreach (var pair in entries) {
				XmlConfig child = saveEntry (pair.Value);
				xmlConfig.Entries.Add (child);
			}
			
			return xmlConfig;
		}

		protected static SortedDictionary<KeyT, K> LoadDictionary<KeyT, K> (XmlConfig xmlConfig, Func<XmlConfig, K> loadEntry, Func<K, KeyT> keyOf)
		{
			SortedDictionary<KeyT, K> entries = new SortedDictionary<KeyT, K> ();
			
			foreach (var child in xmlConfig.Entries) {
				K entry = loadEntry (child);
				entries[keyOf (entry)] = entry;
			}
			
			return entries;
		}

		public void SaveXML (T config, string cwd, string fileName)
		{
			SaveXML (config, cwd + Path.DirectorySeparatorChar + fileName);
		}

		public void SaveXML (T config, string xmlFileName)
		{
			XmlConfig xmlConfig = this.Save (config);
			Serialize (xmlConfig, xmlFileName);
		}

		public T LoadXML (string cwd, string fileName)
		{
			return LoadXML (cwd + Path.DirectorySeparatorChar + fileName);
		}

		public T LoadXML (string xmlFileName)
		{
			XmlConfig xmlConfig = Deserialize (xmlFileName);
			return this.Load (xmlConfig);
		}

		private static void Serialize (XmlConfig xmlConfig, XmlElement rootElement)
		{
			XmlElement element = rootElement.OwnerDocument.CreateElement (xmlConfig.TypeName);
			rootElement.AppendChild (element);
			
			Serialize (xmlConfig, rootElement, element);
		}

		private static void Serialize (XmlConfig xmlConfig, XmlElement rootElement, XmlElement element)
		{
			foreach (var pair in xmlConfig.Attributes) {
				element.SetAttribute (pair.Key, pair.Value);
			}
			
			xmlConfig.Entries.ForEach (child => Serialize (child, element));
		}

		private static void Serialize (XmlConfig xmlConfig, string xmlFileName)
		{
			XmlDocument doc = new XmlDocument ();
			
			XmlElement rootElement = doc.CreateElement (xmlConfig.TypeName);
			doc.AppendChild (rootElement);
			
			foreach (var pair in xmlConfig.Attributes) {
				rootElement.SetAttribute (pair.Key, pair.Value);
			}
			
			xmlConfig.Entries.ForEach (child => Serialize (child, rootElement));
			
			doc.Save (xmlFileName);
		}

		private static void Deserialize (XmlConfig rootEntry, XmlElement rootElement)
		{
			XmlConfig entry = new XmlConfig (rootElement.Name);
			
			foreach (XmlAttribute attribute in rootElement.Attributes) {
				entry[attribute.Name] = attribute.Value;
			}
			
			foreach (var node in rootElement.ChildNodes) {
				if (node is XmlElement) {
					XmlElement childElement = (XmlElement)node;
					Deserialize (entry, childElement);
				}
			}
			
			rootEntry.Entries.Add (entry);
		}

		private static XmlConfig Deserialize (string xmlFileName)
		{
			XmlTextReader reader = new XmlTextReader (xmlFileName);
			XmlDocument doc = new XmlDocument ();
			doc.Load (reader);
			reader.Close ();
			
			XmlConfig xmlConfig = new XmlConfig (doc.DocumentElement.Name);
			
			foreach (XmlAttribute attribute in doc.DocumentElement.Attributes) {
				xmlConfig[attribute.Name] = attribute.Value;
			}
			
			foreach (var node in doc.DocumentElement.ChildNodes) {
				if (node is XmlElement) {
					XmlElement childElement = (XmlElement)node;
					Deserialize (xmlConfig, childElement);
				}
			}
			
			return xmlConfig;
		}
	}

	public static class Logger
	{
		public enum Categories
		{
			EventQueue,
			Simulator,
			Core,
			Thread,
			Process,
			Register,
			Request,
			Cache,
			Coherence,
			Memory,
			Net,
			Instruction,
			Syscall,
			Elf,
			Config,
			Stat,
			Misc,
			OoO,
			Test,
			Debug,
			Xml
		}

		static Logger ()
		{
			LogSwitches = new Dictionary<Logger.Categories, bool> ();
			
			Enable (Categories.Simulator);
			
			//		Enable(Categories.EventQueue);
			//		Enable(Categories.Processor);
			//		Enable(Categories.Register);
			//		Enable(Categories.Thread);
//			Enable(Categories.Process);
			Enable (Categories.Request);
			//		Enable(Categories.Cache);
			Enable (Categories.Coherence);
			//		Enable(Categories.Memory);
			//		Enable(Categories.Net);
			Enable (Categories.Config);
			Enable (Categories.Stat);
			//		Enable(Categories.Misc);
			//		Enable(Categories.OoO);
			//		Enable(Categories.Test);
			//		Enable(Categories.Xml);
			Enable (Categories.Debug);
		}

		public static void Enable (Categories category)
		{
			LogSwitches[category] = true;
		}

		public static void Disable (Categories category)
		{
			LogSwitches[category] = false;
		}

		public static bool IsEnabled (Categories category)
		{
			return true;
//			return LogSwitches.ContainsKey (category) && LogSwitches[category];
		}

		public static string Message (string caption, string text)
		{
			return string.Format ("{0:s} {1:s}", caption.EndsWith ("info") ? "" : "[" + caption + "]", text);
		}

		public static void Infof (Categories category, string format, params object[] args)
		{
			Info (category, string.Format (format, args));
		}

		public static void Info (Categories category, string text)
		{
			if (IsEnabled (category)) {
				Console.WriteLine (Message (category + "|" + "info", text));
			}
		}

		public static void Warnf (Categories category, string format, params object[] args)
		{
			Warn (category, string.Format (format, args));
		}

		public static void Warn (Categories category, string text)
		{
			Console.Error.WriteLine (Message (category + "|" + "warn", text));
		}

		public static void Fatalf (Categories category, string format, params object[] args)
		{
			Fatal (category, string.Format (format, args));
		}

		public static void Fatal (Categories category, string text)
		{
			throw new Exception (Message (category + "|" + "fatal", text));
		}

		public static void Panicf (Categories category, string format, params object[] args)
		{
			Panic (category, string.Format (format, args));
		}

		public static void Panic (Categories category, string text)
		{
			throw new Exception (Message (category + "|" + "panic", text));
		}

		public static Dictionary<Categories, bool> LogSwitches { get; private set; }
	}

	public sealed class Event<EventTypeT, EventContextT>
	{
		public Event (EventTypeT eventType, EventContextT context, ulong scheduledCycle, ulong when)
		{
			this.EventType = eventType;
			this.Context = context;
			this.ScheduledCycle = scheduledCycle;
			this.When = when;
		}

		public EventTypeT EventType { get; private set; }
		public EventContextT Context { get; private set; }
		public ulong ScheduledCycle { get; private set; }
		public ulong When { get; private set; }
	}

	public sealed class ActionEventQueue : EventProcessor
	{
		public ActionEventQueue ()
		{
			this.Actions = new Dictionary<ulong, List<Action>> ();
		}

		public void AdvanceOneCycle ()
		{
			if (this.Actions.ContainsKey (this.CurrentCycle)) {
				this.Actions[this.CurrentCycle].ForEach (action => action ());
				this.Actions.Remove (this.CurrentCycle);
			}
			
			this.CurrentCycle++;
		}

		public void Schedule (Action action, ulong delay)
		{
			ulong when = this.CurrentCycle + delay;
			
			if (!this.Actions.ContainsKey (when)) {
				this.Actions[when] = new List<Action> ();
			}
			
			this.Actions[when].Add (action);
		}

		public ulong CurrentCycle { get; private set; }
		public Dictionary<ulong, List<Action>> Actions { get; private set; }
	}

	public interface EventProcessor
	{
		void AdvanceOneCycle ();
	}

	public interface ICycleProvider : EventProcessor
	{
		ulong CurrentCycle { get; }
		List<EventProcessor> EventProcessors { get; }
	}
}

namespace MinCai.Simulators.Flexim.Common
{
	internal sealed class ElfReaderException : Exception
	{
		public ElfReaderException (string message) : base(message)
		{
		}
	}

	internal abstract class ElfFormatEntity
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

	internal sealed class ElfHeader : ElfFormatEntity
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

	internal sealed class ElfIdentification
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

		public Ei_Class Ei_class { get; private set; }
		public Ei_Data Ei_data { get; private set; }
		public int Ei_version { get; private set; }
	}

	internal sealed class ElfProgramHeader : ElfFormatEntity
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

	internal sealed class ElfSectionHeader : ElfFormatEntity
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

	internal sealed class ElfStringTable : ElfFormatEntity
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

	internal sealed class ElfSymbolTable : ElfFormatEntity
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

	internal sealed class ElfSymbolTableEntry : ElfFormatEntity
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

	internal sealed class ElfFile
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
			
			this.SymbolStringTable = this.SectionHeaders.Find (sectionHeader => sectionHeader.Name == ".strtab").AssociatedEntity as ElfStringTable;
			
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

		public static ElfFile Create (string workDirectory, string fileName)
		{
			using (FileStream fs = File.Open (workDirectory + Path.DirectorySeparatorChar + fileName, FileMode.Open)) {
				return ProcessFile (new BinaryReader (fs));
			}
		}

		private static ElfFile ProcessFile (BinaryReader reader)
		{
			ElfFile file = new ElfFile (reader);
			file.Read ();
			
			return file;
		}
	}

	public class RoundRobinScheduler<T>
	{
		public RoundRobinScheduler (List<T> resources, Predicate<T> pred, Action<T> consumeAction, int quant)
		{
			this.Resources = resources;
			this.Pred = pred;
			this.ConsumeAction = consumeAction;
			this.Quant = quant;
			
			this.CurrentResourceId = 0;
		}

		public void ConsumeNext ()
		{
			Dictionary<int, bool> stalled = new Dictionary<int, bool> ();
			
			for (int i = 0; i < this.Resources.Count; i++) {
				stalled[i] = false;
			}
			
			this.CurrentResourceId = (this.CurrentResourceId + 1) % this.Resources.Count;
			
			int consumedCount = 0;
			
			while (consumedCount < this.Quant) {
				if (!this.Pred (this.Resources[this.CurrentResourceId]) || stalled[this.CurrentResourceId]) {
					this.CurrentResourceId = this.Resources.FindIndex (this.Pred);
				}
				
				if (this.CurrentResourceId == -1) {
					break;
				}
				
				try {
					this.ConsumeAction (this.Resources[this.CurrentResourceId]);
				} catch (Exception) {
					stalled[this.CurrentResourceId] = true;
					continue;
				}
				
				consumedCount++;
			}
		}

		public int CurrentResourceId { get; private set; }

		public List<T> Resources { get; private set; }
		public Predicate<T> Pred { get; private set; }
		public Action<T> ConsumeAction { get; private set; }
		public int Quant { get; private set; }
	}
}
