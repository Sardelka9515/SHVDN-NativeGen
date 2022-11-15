using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
namespace NativeGen;

internal static class Program
{
    public static GenOptions Options = GenOptions.All;

    private static void Main(string[] args)
    {
        Console.WriteLine("SHVDN source generator by Sardelka9515");
        Console.WriteLine("Available options:");
        Console.WriteLine("\t" + string.Join(", ", Enum.GetValues<GenOptions>()));

        if (args.Length > 0)
        {
            Options = GenOptions.None;
            foreach (var a in args) Options |= Enum.Parse<GenOptions>(a, true);
        }

        Console.WriteLine("Generating with configuration: " + Options);

        Console.WriteLine("Downloading natives...");
        string nativeData;
        using (var wc = new WebClient())
        {
            nativeData =
                wc.DownloadString("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json");
        }


        Console.WriteLine("Parsing data...");
        var namespaces = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, NativeInfo>>>(nativeData);
        GenEnum(namespaces);
        if (Options.HasFlag(GenOptions.GenInvoker)) GenInvoker(namespaces);

    }

    private static void GenEnum(Dictionary<string, Dictionary<string, NativeInfo>> namespaces,
        string output = "NativeHashes.cs")
    {
        Console.WriteLine($"Generating {output}...");
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
                n.Value.WriteHashEnum(sb, n.Key, names, Options);
            }

            sb.AppendLine();
            sb.AppendLine("\t\t#endregion");
            sb.AppendLine();
        }

        sb.AppendLine(footer);
        File.WriteAllText(output, sb.ToString().Replace("\r\n", "\n"));
        Console.WriteLine("Success!");
    }

    private static void GenInvoker(Dictionary<string, Dictionary<string, NativeInfo>> namespaces,
        string output = "NativeInvoker.cs")
    {
        Console.WriteLine($"Generating {output}...");
        var header =
            "using System;\nusing GTA;\nusing GTA.Math;\n\nnamespace GTA.Native\r\n{\r\n\tpublic static unsafe class NativeInvoker\r\n\t{";
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
                n.Value.WriteInvoker(sb, n.Key, names, Options);
            }

            sb.AppendLine();
            sb.AppendLine("\t\t#endregion");
            sb.AppendLine();
        }

        sb.AppendLine(footer);
        File.WriteAllText(output, sb.ToString().Replace("\r\n", "\n"));
        Console.WriteLine("Success!");
    }

    public static string ToSharpType(this string name)
    {
        return name switch
        {
            "Any" or "ScrHandle" or "SrcHandle" or "FireId" or "Interior" or "ScrHandle*" or "SrcHandle*"
                or "Any*" => "IntPtr",
            "BOOL*" => "bool*",
            "Ped*" or "Entity*" or "Vehicle*" or "Object*" or "Blip*" => "int*",
            "Cam" => "Camera",
            "Object" => "int",
            "Hash" => "uint",
            "const char*" => "string",
            "BOOL" => "bool",
            _ => name
        };
    }
}