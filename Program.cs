using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using Kawa.Json;

namespace kBlorb
{
	class Program
	{
		static JsonObj json = null;
		static string source;
		static string target;
		static Object[] files;

		static UInt32 runningSize = 0;
		static List<List<int>> pics = new List<List<int>>();
		static Dictionary<int, Tuple<string, string>> descs = new Dictionary<int, Tuple<string, string>>();
		static int coverPic = -1;

		static bool longExts = false;
		static bool allowAdriftInInform = false;

		static int autoIndex = 0;
		static byte[] eightBytes = new byte[8];
		static MemoryStream ebStream = new MemoryStream(eightBytes);
		static BinaryWriter ebWriter = new BinaryWriter(ebStream);
		static RiffPadding padding = new RiffPadding();

		static RiffFormChunk form = new RiffFormChunk("IFRS");
		static RiffTrunkChunk rIdx = new RiffTrunkChunk("RIdx");

		static void Main(string[] args)
		{
			var thisAsm = System.Reflection.Assembly.GetCallingAssembly().GetName();
			Console.WriteLine("{0} {1}.{2} -- a blorbing tool by Kawa", thisAsm.Name, thisAsm.Version.Major, thisAsm.Version.Minor);

			if (args.Length == 0)
			{
				DoHelp(false);
				return;
			}
			foreach (var arg in args)
			{
				if (arg == "-h") { DoHelp(true); return; }
				if (arg == "-l") longExts = true;
				if (arg == "-ad") allowAdriftInInform = true;
			}
			source = args.Last();
			if (source.StartsWith("-"))
			{
				DoHelp(false);
				return;
			}

			try
			{
				var theText = File.ReadAllText(source);
				if (theText.StartsWith("{"))
					json = Json5.Parse(theText) as JsonObj;
				else if (theText.Contains("storyfile"))
					json = BlurbToJson(theText);
			}
			catch (FileNotFoundException x)
			{
				Console.WriteLine("Error: file {0} not found.", Path.GetFileName(x.FileName));
				return;
			}
			catch (JsonException x)
			{
				Console.WriteLine("Error: could not read file as JSON object.");
				Console.WriteLine(x.Message);
				return;
			}

			target = json.Path<string>("/target", null);
			if (target == null)
				Console.WriteLine("Warning: no target specified. Will figure it out myself.");

			form.Children.Add(rIdx);

			if (!HandleFiles())
				return;

			if (target == null)
			{
				target = Path.ChangeExtension(source, longExts ? "blorb" : "blb");
				Console.WriteLine("Note: decided on {0}.", target);
			}

			HandleMeta();
			HandleRDesc();
			HandleReso();

			try
			{
				var blorb = new BinaryWriter(File.Open(target, FileMode.Create));
				Console.WriteLine("Writing blorb...");
				form.Write(blorb);
				blorb.Close();
			}
			catch (Exception x)
			{
				Console.WriteLine("Error: could not write blorb.");
				Console.WriteLine(x.Message);
				return;
			}
			Console.WriteLine("Done!");
		}

		public static bool HandleFiles()
		{
			files = json.Path<Object[]>("/files", null);
			if (files == null)
			{
				Console.WriteLine("Error: must have a \"files\" element.");
				return false;
			}
			runningSize = (UInt32)(24 + (files.Length * 12));

			var fileToType = new Dictionary<string, string>()
			{
				{ ".z3", "Exec" },
				{ ".z4", "Exec" },
				{ ".z5", "Exec" },
				{ ".z6", "Exec" },
				{ ".z7", "Exec" },
				{ ".z8", "Exec" },
				{ ".ulx", "Exec" },
				{ ".taf", "Exec" },
				{ ".png", "Pict" },
				{ ".jpg", "Pict" },
				{ ".gif", "Pict" },
				{ ".ogg", "Snd " },
				{ ".mod", "Snd " },
				{ ".s3m", "Snd " },
				{ ".xm", "Snd " },
				{ ".it", "Snd " },
				{ ".wav", "Snd " },
				{ ".mid", "Snd " },
				{ ".mp3", "Snd " },
			};
			var fileToChunk = new Dictionary<string, string>()
			{
				{ ".z3", "ZCOD" },
				{ ".z4", "ZCOD" },
				{ ".z5", "ZCOD" },
				{ ".z6", "ZCOD" },
				{ ".z7", "ZCOD" },
				{ ".z8", "ZCOD" },
				{ ".ulx", "GLUL" },
				{ ".taf", "ADRI" },
				{ ".png", "PNG " },
				{ ".jpg", "JPEG" },
				{ ".gif", "GIF " },
				{ ".ogg", "OGGV" },
				{ ".mod", "MOD " },
				{ ".s3m", "MOD " },
				{ ".xm", "MOD " },
				{ ".it", "MOD " },
				{ ".wav", "WAV " },
				{ ".mid", "MIDI" },
				{ ".mp3", "MP3 " },
			};

			var chunkToBlorb = new Dictionary<string, string>()
			{
				{ "ZCOD", longExts ? "zblorb" : "zlb" },
				{ "GLUL", longExts ? "gblorb" : "glb" },
				{ "ADRI", "adriftblorb" }, //yecch
			};

			bool isAdrift = false;

			foreach (var file in files)
			{
				var type = "????";
				var index = autoIndex++;
				var source = "????";
				RiffDataChunk newBlob = new RiffDataChunk("????", new byte[] { 0 });
				var thisFile = file;
				if (thisFile is string)
				{
					source = thisFile as string;
					var ext = Path.GetExtension(source).ToLowerInvariant();
					if (fileToType.ContainsKey(ext))
						type = fileToType[ext];

					var newFile = new JsonObj();
					newFile["type"] = type;
					newFile["src"] = source;
					thisFile = newFile;
				}
				if (thisFile is List<object>)
				{
					var l = thisFile as List<object>;
					if (l.Count == 2)
					{
						var newFile = new JsonObj();
						newFile["type"] = "Rect";
						newFile["size"] = l;
						thisFile = newFile;
					}
				}
				if (thisFile is JsonObj)
				{
					var fob = thisFile as JsonObj;
					index = fob.Path<int>("/index", index);
					type = fob.Path<string>("/type", type);
					var desc = fob.Path<string>("/alt", null);
					if (type == "????")
					{
						if (fob.ContainsKey("src"))
						{
							var ext = Path.GetExtension(fob.Path<string>("/src")).ToLowerInvariant();
							if (fileToType.ContainsKey(ext))
								type = fileToType[ext];
						}
					}
					if (type == "????") continue;
					if (type == "picture") type = "Pict";
					if (type == "rectangle") type = "Rect";
					if (type == "rect") type = "Rect";
					if (type == "sound") type = "Snd ";
					if (type == "Snd ")
					{
						if (index < 3)
						{
							Console.WriteLine("Note: skipping ahead to #3 for a sound.");
							index++;
							autoIndex++;
						}
						if (desc != null)
							descs.Add(index, Tuple.Create("Snd ", desc));
					}
					if (type == "Exec")
					{
						if (index != 0)
							Console.WriteLine("Warning: Exec chunk is not #0.");
						var ext = Path.GetExtension(source).ToLowerInvariant();
						isAdrift = ext == ".taf";
					}
					if (type == "Pict")
					{
						type = "Pict";
						var ratios = fob.Path<List<int>>("/ratios", new List<int>() { 0 });
						if (ratios.Count == 1)
							ratios = new List<int>() { ratios[0], ratios[0], ratios[0] };
						if (ratios.Count == 3)
							ratios = new List<int>() { ratios[0], 1, ratios[1], 1, ratios[2], 1 };
						ratios.Insert(0, index);
						pics.Add(ratios);
						if (fob.Path<bool>("/cover", false))
							coverPic = index;
						if (desc != null)
							descs.Add(index, Tuple.Create("Pict", desc));
					}
					if (type == "Rect")
					{
						var rect = fob.Path<List<object>>("/size").Select(x => (int)x).ToArray();
						ebWriter.Seek(0, SeekOrigin.Begin);
						ebWriter.WriteMoto((UInt32)rect[0]);
						ebWriter.WriteMoto((UInt32)rect[1]);
						newBlob = new RiffDataChunk("Rect", eightBytes);
					}
					if (fob.ContainsKey("src"))
					{
						source = fob.Path<string>("/src");
						var chunkName = "????";
						var ext = Path.GetExtension(source).ToLowerInvariant();
						if (fileToChunk.ContainsKey(ext))
							chunkName = fileToChunk[ext];
						if (!isAdrift &&
							(ext == ".gif") ||
							(ext == ".mid") ||
							(ext == ".wav") ||
							(ext == ".mp3"))
						{
							if (allowAdriftInInform)
								Console.WriteLine("Warning: tried to use Adrift-only media ({0}) in non-Adrift game.", ext);
							else
							{
								Console.WriteLine("Error: tried to use Adrift-only media ({0}) in non-Adrift game.", ext);
								Console.WriteLine("(if you insist, use -ad to demote this to a warning.)");
								return false;
							}
						}

						try
							{
							newBlob = new RiffDataChunk(chunkName, File.ReadAllBytes(source));
						}
						catch (FileNotFoundException x)
						{
							Console.WriteLine("Error: could not find {0} for inclusion.", Path.GetFileName(x.FileName));
							return false;
						}
						if (type == "Exec" && target == null)
						{
							if (chunkToBlorb.ContainsKey(chunkName))
							{
								target = Path.ChangeExtension(source, chunkToBlorb[chunkName]);
								Console.WriteLine("Note: decided on {0}.", target);
							}
						}
						Console.WriteLine("Chunk #{0}, type {1}", index, type);
					}
				}
				ebWriter.Seek(0, SeekOrigin.Begin);
				ebWriter.WriteMoto((UInt32)index);
				ebWriter.WriteMoto((UInt32)runningSize);
				var chunk = new RiffIndexChunk(type, eightBytes);
				rIdx.Children.Add(chunk);
				runningSize += newBlob.Size() + 8;
				form.Children.Add(newBlob);
				if (runningSize % 2 == 1)
				{
					form.Children.Add(padding);
					runningSize++;
				}
			}
			return true;
		}

		public static bool HandleRDesc()
		{
			if (descs.Count == 0)
				return false;

			var rDescData = new List<byte>();
			ebWriter.Seek(0, SeekOrigin.Begin);
			ebWriter.WriteMoto((UInt32)descs.Count);
			rDescData.AddRange(eightBytes.Take(4));
			foreach (var desc in descs)
			{
				ebWriter.Seek(0, SeekOrigin.Begin);
				ebWriter.Write(desc.Value.Item1.ToCharArray());
				ebWriter.WriteMoto((UInt32)desc.Key);
				rDescData.AddRange(eightBytes);
				ebWriter.Seek(0, SeekOrigin.Begin);
				ebWriter.WriteMoto((UInt32)desc.Value.Item2.Length);
				rDescData.AddRange(eightBytes.Take(4));
				rDescData.AddRange(Encoding.UTF8.GetBytes(desc.Value.Item2));
				if (desc.Value.Item2.Length % 2 == 1)
					rDescData.Add(0);
			}

			var chunk = new RiffDataChunk("RDesc", rDescData.ToArray());
			form.Children.Add(chunk);
			return true;
		}

		public static bool HandleReso()
		{
			var reso = json.Path<JsonObj>("/reso", json.Path<JsonObj>("/resolution", null));
			if (reso == null)
				return false;
			var zero = new[] { 0, 0 };
			var standard = reso.Path<int[]>("/standard", zero);
			var minimum = reso.Path<int[]>("/minimum", standard);
			var maximum = reso.Path<int[]>("/maximum", standard);
			var resoData = new List<byte>();

			ebWriter.Seek(0, SeekOrigin.Begin);
			ebWriter.WriteMoto((UInt32)standard[0]);
			ebWriter.WriteMoto((UInt32)standard[1]);
			resoData.AddRange(eightBytes);
			ebWriter.Seek(0, SeekOrigin.Begin);
			ebWriter.WriteMoto((UInt32)minimum[0]);
			ebWriter.WriteMoto((UInt32)minimum[1]);
			resoData.AddRange(eightBytes);
			ebWriter.Seek(0, SeekOrigin.Begin);
			ebWriter.WriteMoto((UInt32)maximum[0]);
			ebWriter.WriteMoto((UInt32)maximum[1]);
			resoData.AddRange(eightBytes);

			foreach (var entry in pics)
			{
				ebWriter.Seek(0, SeekOrigin.Begin);
				ebWriter.WriteMoto((UInt32)entry[0]);
				resoData.AddRange(eightBytes.Take(4));
				ebWriter.Seek(0, SeekOrigin.Begin);
				ebWriter.WriteMoto((UInt32)entry[1]);
				ebWriter.WriteMoto((UInt32)entry[2]);
				resoData.AddRange(eightBytes);
				ebWriter.Seek(0, SeekOrigin.Begin);
				ebWriter.WriteMoto((UInt32)entry[3]);
				ebWriter.WriteMoto((UInt32)entry[4]);
				resoData.AddRange(eightBytes);
				ebWriter.Seek(0, SeekOrigin.Begin);
				ebWriter.WriteMoto((UInt32)entry[5]);
				ebWriter.WriteMoto((UInt32)entry[6]);
				resoData.AddRange(eightBytes);
			}

			var chunk = new RiffDataChunk("Reso", resoData.ToArray());
			form.Children.Add(chunk);

			return true;
		}

		public static bool HandleMeta()
		{
			var meta = json.Path<Object>("/meta", json.Path<Object>("/metadata", null));
			if (coverPic != 0)
			{
				if (meta == null)
					Console.WriteLine("Warning: cover art, but no metadata.");
				ebWriter.Seek(0, SeekOrigin.Begin);
				ebWriter.WriteMoto((UInt32)coverPic);
				var chunk = new RiffDataChunk("Fspc", eightBytes.Take(4).ToArray());
				form.Children.Add(chunk);
			}
			if (meta == null)
				return false;
			if (meta is string && (meta as string).EndsWith(".xml"))
				meta = File.ReadAllText(meta as string);
			if (meta is JsonObj)
			{
				var mj = meta as JsonObj;
				var xml = new XmlDocument();
				var xmlDec = xml.CreateXmlDeclaration("1.0", "UTF-8", "");
				var ifIndex = xml.CreateElement("ifindex");
				var ifIdent = xml.CreateElement("identification");
				var ifId = xml.CreateElement("ifid");
				var ifFormat = xml.CreateElement("format");
				var ifStory = xml.CreateElement("story");
				var ifBiblio = xml.CreateElement("bibliographic");
				var ifTitle = xml.CreateElement("title");
				var ifAuthor = xml.CreateElement("author");
				var ifHeadline = xml.CreateElement("headline");
				var ifGenre = xml.CreateElement("genre");
				var ifDescription = xml.CreateElement("description");
				ifIndex.SetAttribute("version", "1.0");
				ifIndex.SetAttribute("xmlns", "http://babel.ifarchive.org/protocol/iFiction/");
				var guid = mj.Path<string>("/id", null);
				if (guid == null)
				{
					guid = Guid.NewGuid().ToString().ToUpperInvariant();
					Console.WriteLine("Note: no ID specified, decided on {0} for you.", guid);
				}
				ifId.InnerText = guid;
				ifFormat.InnerText = "zcode";
				ifTitle.InnerText = mj.Path<string>("/title", Path.GetFileNameWithoutExtension(target));
				ifAuthor.InnerText = mj.Path<string>("/author", Environment.UserName);
				ifHeadline.InnerText = mj.Path<string>("/headline", null);
				ifGenre.InnerText = mj.Path<string>("/genre", null);
				ifDescription.InnerText = mj.Path<string>("/description", null);
				xml.AppendChild(xmlDec);
				xml.AppendChild(ifIndex);
				ifIndex.AppendChild(ifStory);
				ifStory.AppendChild(ifIdent);
				ifIdent.AppendChild(ifId);
				ifIdent.AppendChild(ifFormat);
				ifStory.AppendChild(ifBiblio);
				if (ifTitle != null) ifBiblio.AppendChild(ifTitle);
				if (ifAuthor != null) ifBiblio.AppendChild(ifAuthor);
				if (ifHeadline != null) ifBiblio.AppendChild(ifHeadline);
				if (ifGenre != null) ifBiblio.AppendChild(ifGenre);
				if (ifDescription != null) ifBiblio.AppendChild(ifDescription);
				meta = xml.OuterXml;
			}
			if (meta is string)
			{
				//it's a block of XML?
				var chunk = new RiffDataChunk("IFmd", Encoding.UTF8.GetBytes(meta as string));
				form.Children.Add(chunk);
				if ((meta as string).Length % 2 == 1)
					form.Children.Add(padding);
			}
			return true;
		}

		public static JsonObj BlurbToJson(string text)
		{
			var ret = new JsonObj();
			var lines = text.Split('\n').Select(x => x.Trim().Replace("  ", " ").Replace("\t", " ")).Where(x => !x.StartsWith("!") && x.Length > 0);
			Console.WriteLine("Note: trying to convert a blurb file...");
			var files = new List<object>();
			ret["files"] = files;
			foreach (var line in lines)
			{
				if (line.StartsWith("storyfile leafname"))
					ret["target"] = Regex.Match(line, @"storyfile leafname ""(.*)""").Groups[1].Value;
				else if (line.StartsWith("storyfile") && line.EndsWith("include"))
					files.Add(Regex.Match(line, @"storyfile ""(.*)"" include").Groups[1].Value);
				else if (line.StartsWith("cover"))
				{
					var m = Regex.Match(line, @"cover ""(.*)""");
					var f = new JsonObj();
					f["cover"] = true;
					f["src"] = m.Groups[1].Value;
					files.Add(f);
				}
				else if (line.StartsWith("picture"))
				{
					if (Regex.IsMatch(line, @"picture (\d*) ""(.*)"""))
					{ 
						var m = Regex.Match(line, @"picture (\d*) ""(.*)""");
						var f = new JsonObj();
						f["index"] = int.Parse(m.Groups[1].Value);
						f["src"] = m.Groups[2].Value;
						files.Add(f);
					}
					else if (Regex.IsMatch(line, @"picture ""(.*)"""))
					{
						var m = Regex.Match(line, @"picture ""(.*)""");
						var f = new JsonObj();
						f["src"] = m.Groups[1].Value;
						files.Add(f);
					}
				}
				else if (line.StartsWith("sound"))
				{
					if (Regex.IsMatch(line, @"sound (\d*) ""(.*)"""))
					{
						var m = Regex.Match(line, @"sound (\d*) ""(.*)""");
						var f = new JsonObj();
						f["index"] = int.Parse(m.Groups[1].Value);
						f["src"] = m.Groups[2].Value;
						files.Add(f);
					}
					else if (Regex.IsMatch(line, @"sound ""(.*)"""))
					{
						var m = Regex.Match(line, @"sound ""(.*)""");
						var f = new JsonObj();
						f["src"] = m.Groups[1].Value;
						files.Add(f);
					}
					}
				}
			Console.WriteLine("Debug: blorb translated as follows.");
			Console.WriteLine(ret.Stringify());
			return ret;
		}

		public static void DoHelp(bool full)
		{
			Console.WriteLine(@"Usage: kBlorb.exe [-h] [-l] [-ad] blurbFile");
			if (full)
				Console.WriteLine(@"
Safely protects a small object as though in a strong box. (what?)

Arguments:
  -h	show this help message and exit
  -l	use long file extensions over short (.blorb over .blb)
  -ad	allow adrift-only files even in inform mode (no effect yet)

blurbFile should be the name of a JSON object. cBlorb blurbfile support is
a work in progress.
");
		}
	}

	abstract class RiffChunk
	{
		public string Name;
		public RiffChunk(string name)
		{
			Name = name;
		}
		public abstract void Write(BinaryWriter b);
		public abstract UInt32 Size();
	}

	class RiffDataChunk : RiffChunk
	{
		public byte[] Data;
		public RiffDataChunk(string name, byte[] data) : base(name)
		{
			Name = name;
			Data = new byte[data.Length];
			data.CopyTo(Data, 0);
		}
		public override void Write(BinaryWriter b)
		{
			b.Write((Name + "    ").Substring(0, 4).ToCharArray());
			b.WriteMoto((UInt32)Data.Length);
			b.Write(Data);
		}
		public override uint Size()
		{
			return (UInt32)Data.Length;
		}
	}

	class RiffIndexChunk : RiffChunk
	{
		public byte[] Data;
		public RiffIndexChunk(string name, byte[] data) : base(name)
		{
			Name = name;
			Data = new byte[data.Length];
			data.CopyTo(Data, 0);
		}
		public override void Write(BinaryWriter b)
		{
			b.Write((Name + "    ").Substring(0, 4).ToCharArray());
			b.Write(Data);
		}
		public override uint Size()
		{
			return (UInt32)Data.Length;
		}
	}

	class RiffTrunkChunk : RiffChunk
	{
		public List<RiffChunk> Children;
		public RiffTrunkChunk(string name) : base(name)
		{
			Name = name;
			Children = new List<RiffChunk>();
		}
		public override void Write(BinaryWriter b)
		{
			b.Write((Name + "    ").Substring(0, 4).ToCharArray());
			b.WriteMoto(Size());
			b.WriteMoto(Children.Count());
			foreach (var child in Children)
				child.Write(b);
		}
		public override uint Size()
		{
			UInt32 size = 4;
			foreach (var child in Children)
			{
				if (child is RiffPadding)
					continue;
				size += child.Size() + 4;
			}
			return size;
		}
	}

	class RiffFormChunk : RiffTrunkChunk
	{
		public RiffFormChunk(string name) : base(name)
		{
			Children = new List<RiffChunk>();
		}
		public override void Write(BinaryWriter b)
		{
			b.Write("FORM".ToCharArray());
			b.WriteMoto(Size() + 4);
			b.Write((Name + "    ").Substring(0, 4).ToCharArray());
			foreach (var child in Children)
				child.Write(b);
		}
		public override uint Size()
		{
			UInt32 size = 0;
			foreach (var child in Children)
			{
				if (child is RiffPadding)
					size += 1;
				else
					size += child.Size() + 8;
			}
			return size;
		}
	}

	class RiffPadding : RiffChunk
	{
		public RiffPadding() : base("")
		{
		}
		public override void Write(BinaryWriter b)
		{
			b.Write((byte)0);
		}
		public override uint Size()
		{
			return 1;
		}
	}
}
