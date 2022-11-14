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
        public string type;
        public string name;

        public override string ToString()
        {
            return $"{type} {name}";
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
        public void Format(StringBuilder sb, string hash, HashSet<string> added, GenOptions o)
        {
            if (added.Contains(Name))
            {
                return;
            }

            if (o.HasFlag(GenOptions.Parameters) && Parameters is { Length: > 0 })
            {
                Add($"/// <summary>");
                Add($"/// Parameters: " + string.Join(", ", (object[])Parameters));
                Add($"/// </summary>");
            }

            if (o.HasFlag(GenOptions.Comments) && !string.IsNullOrEmpty(Comment))
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

            if (o.HasFlag(GenOptions.Returns))
            {
                Add($"/// <returns>{ReturnType}</returns>");
            }
            Add($"{Name} = {hash}, // {Hash}");
            added.Add(Name);
            if (OldNames != null && o.HasFlag(GenOptions.OldNames))
            {
                foreach (var old in OldNames)
                {
                    if (added.Contains(old)) { continue; }
                    Add();
                    if (o.HasFlag(GenOptions.MarkObsolete))
                    {
                        Add($"///<remarks>This function has been replaced by <see cref=\"{Name}\"/></remarks>");
                        Add($"[Obsolete]");
                    }
                    Add($"{old} = {hash}, // {Hash}");
                    added.Add(old);
                }
            }

            void Add(string line = null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    sb.AppendLine();
                    return;
                }
                sb.AppendLine($"\t\t\t{line}");
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
    internal class Program
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



            Console.WriteLine("Generating...");
            var header = "using System;\n\nnamespace GTA\r\n{\r\n\tnamespace Native\r\n\t{\r\n\t\tpublic enum Hash : ulong\r\n\t\t{";
            var footer = "}\r\n\t}\r\n}";
            var names = new HashSet<string>();
            var sb = new StringBuilder();
            sb.AppendLine(header);
            foreach (var ns in namespaces)
            {
                sb.AppendLine($"\t\t\t// {ns.Key}");
                foreach (var n in ns.Value)
                {
                    sb.AppendLine();
                    n.Value.Format(sb, n.Key, names, Options);
                }
            }
            sb.AppendLine(footer);

            Console.WriteLine("Writing to output: NativeHashes.cs");
            File.WriteAllText("NativeHashes.cs", sb.ToString().Replace("\r\n", "\n"));
            Console.WriteLine("Success!");
        }

    }
}
