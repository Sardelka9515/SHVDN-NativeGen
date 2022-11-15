using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GTA.Native;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;

namespace NativeGen
{
    class NativeParameter
    {
        string _name;
        public string name
        {
            get => _name;
            set
            {
                _name = value;
                _name = _name switch
                {
                    "event" or "override" or "base" or "object" or "string" or "out" => "@" + _name,
                    _ => _name,
                };
            }
        }
        public string type;

        public override string ToString()
        {
            return $"{type.ToSharpType()} {name}";
        }
    }

    class NativeInfo
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("jhash")]
        public string Hash;

        [JsonProperty("comment")]
        public string Comment;

        [JsonProperty("params")]
        public NativeParameter[] Parameters;

        [JsonProperty("return_type")]
        public string ReturnType;

        [JsonProperty("build")]
        public string Build;


        [JsonProperty("old_names")]
        public string[] OldNames;

        StringBuilder _builder;

        void Add(string line = null)
        {
            if (string.IsNullOrEmpty(line))
            {
                _builder.AppendLine();
                return;
            }
            _builder.AppendLine($"\t\t{line}");
        }

        public void AddComment()
        {

            Add($"/// <remarks>");

            foreach (var s in Comment.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                // Escape xml special characters
                var escaped = s.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&#38;");
                // Add url link
                var result = Regex.Replace(escaped,
                @"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)",
                "<see href='$1'>$1</see>");
                Add($"/// {result}<br/>");
            }

            Add("/// </remarks>");
        }

        private void AddObsolete(string name = null)
        {
            Add($"///<remarks>This function has been replaced by <see cref=\"{name ?? Name}\"/></remarks>");
            Add($"[Obsolete]");
        }

        public void WriteInvoker(StringBuilder sb,  string hash,HashSet<string> added, GenOptions o)
        {
            if (added.Contains(Name))
            {
                return;
            }

            _builder = sb;

            if (o.HasFlag(GenOptions.Parameters) && Parameters is { Length: > 0 })
            {
                foreach (var p in Parameters)
                {
                    Add($"/// <param name={p.name}></param>");
                }
            }
            if (o.HasFlag(GenOptions.Comments) && !string.IsNullOrEmpty(Comment))
            {
                AddComment();
            }

            WriteMethod(hash);
            added.Add(Name);

            if (OldNames != null && o.HasFlag(GenOptions.OldNames))
            {
                foreach (var old in OldNames)
                {
                    if (added.Contains(old)) return;
                    Add();
                    if (o.HasFlag(GenOptions.MarkObsolete))
                    {
                        AddObsolete();
                    }
                    WriteMethod(hash,old);
                    added.Add(old);
                }
            }
        }
        void WriteMethod(string hash,string name = null)
        {
            name ??= Name;
            Add($"public static {ReturnType.ToSharpType()} {name}({string.Join(", ", (object[])Parameters)})");

            string paras = "";
            foreach (var p in Parameters)
            {
                paras += $", {p.name}";
            }
            var ret = ReturnType != "void" ? $"<{ReturnType.ToSharpType()}>" : "";
            Add($"\t=> Function.Call{ret}((Hash){hash}{paras});");
        }
        public void WiteHashEnum(StringBuilder sb, string hash, HashSet<string> added, GenOptions o)
        {
            if (added.Contains(Name))
            {
                return;
            }
            _builder = sb;
            if (o.HasFlag(GenOptions.Parameters) && Parameters is { Length: > 0 })
            {
                Add($"/// <summary>");
                Add($"/// Parameters: " + string.Join(", ", (object[])Parameters));
                Add($"/// </summary>");
            }

            if (o.HasFlag(GenOptions.Comments) && !string.IsNullOrEmpty(Comment))
            {
                AddComment();
            }

            if (o.HasFlag(GenOptions.Returns))
            {
                Add($"/// <returns>{ReturnType}</returns>");
            }
            var suffix = string.IsNullOrEmpty(Hash) ? "" : $" // {Hash}";
            Add($"{Name} = {hash},{suffix}");
            added.Add(Name);
            if (OldNames != null && o.HasFlag(GenOptions.OldNames))
            {
                foreach (var old in OldNames)
                {
                    if (added.Contains(old)) { continue; }
                    Add();
                    if (o.HasFlag(GenOptions.MarkObsolete))
                    {
                        AddObsolete();
                    }
                    Add($"{old} = {hash},{suffix}");
                    added.Add(old);
                }
            }

        }

    }

    [Flags]
    enum GenOptions
    {
        None = 0,
        Parameters = 1,
        Returns = 2,
        Comments = 4,
        OldNames = 8,
        MarkObsolete = 16,
        All = ~0,
    }
    internal static class Program
    {
        public static GenOptions Options = GenOptions.All;
        static void Main(string[] args)
        {
            Console.WriteLine("SHVDN hash generator by Sardelka9515");
            Console.WriteLine("Available options:");
            Console.WriteLine("\t" + string.Join(", ", Enum.GetValues<GenOptions>()));

            if (args.Length > 0)
            {
                Options = GenOptions.None;
                foreach (var a in args)
                {
                    Options |= Enum.Parse<GenOptions>(a, true);
                }
            }

            Console.WriteLine("Generating with configuration: " + Options);

            Console.WriteLine("Downloading natives...");
            string nativeData;
            using (var wc = new System.Net.WebClient())
                nativeData = wc.DownloadString("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json");



            Console.WriteLine("Parsing data...");
            var namespaces = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, NativeInfo>>>(nativeData);

            // GenEnum(namespaces);
            GenInvoker(namespaces);

        }

        static void GenEnum(Dictionary<string, Dictionary<string, NativeInfo>> namespaces)
        {

            Console.WriteLine("Generating NativeHashes.cs...");
            var header = "using System;\n\nnamespace GTA.Native\r\n{\r\n\tpublic enum Hash : ulong\r\n\t{";
            var footer = "\t}\r\n}";
            var names = new HashSet<string>();
            var sb = new StringBuilder();
            sb.AppendLine(header);
            foreach (var ns in namespaces)
            {
                sb.AppendLine($"\t\t#region {ns.Key}");
                foreach (var n in ns.Value)
                {
                    sb.AppendLine();
                    n.Value.WiteHashEnum(sb, n.Key, names, Options);
                }
                sb.AppendLine();
                sb.AppendLine("\t\t#endregion");
                sb.AppendLine();
            }
            sb.AppendLine(footer);
            File.WriteAllText("NativeHashes.cs", sb.ToString().Replace("\r\n", "\n"));
            Console.WriteLine("Success!");
        }
        static void GenInvoker(Dictionary<string, Dictionary<string, NativeInfo>> namespaces)
        {

            Console.WriteLine("Generating NativInvoker.cs...");
            var header = "using System;\nusing GTA;\nusing GTA.Math;\n\nnamespace GTA.Native\r\n{\r\n\tpublic static unsafe class NativeInvoker\r\n\t{";
            var footer = "\t}\r\n}";
            var names = new HashSet<string>();
            var sb = new StringBuilder();
            sb.AppendLine(header);
            foreach (var ns in namespaces)
            {
                sb.AppendLine($"\t\t#region {ns.Key}");
                foreach (var n in ns.Value)
                {
                    sb.AppendLine();
                    n.Value.WriteInvoker(sb,n.Key, names, Options);
                }
                sb.AppendLine();
                sb.AppendLine("\t\t#endregion");
                sb.AppendLine();
            }
            sb.AppendLine(footer);
            File.WriteAllText("NativeInvoker.cs", sb.ToString().Replace("\r\n", "\n"));
            Console.WriteLine("Success!");
        }

        public static string ToSharpType(this string name)
        => name switch
        {
            "Any" or "ScrHandle" or "SrcHandle" or "FireId" or "Interior" => "IntPtr",
            "ScrHandle*" or "SrcHandle*" or "Any*" => "IntPtr*",
            "BOOL*" =>"bool*",
            "Ped*" or "Entity*" or "Vehicle*" or "Object*"=>"int*",
            "Cam"=>"Camera",
            "Object" => "int",
            "Hash" => "uint",
            "const char*" => "string",
            "BOOL" => "bool",
            _ => name,
        };
    }
}
