using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

string root = args.Length > 0 ? args[0] : "Assets/OneMoreFloor";
int count = 0, errors = 0;
var configurations=new[]{Array.Empty<string>(),new[]{"UNITY_EDITOR"},new[]{"OMF_ADMOB","UNITY_ANDROID"},new[]{"OMF_ADMOB","UNITY_IOS"},new[]{"OMF_ADMOB","UNITY_EDITOR"}};
foreach (var path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
{
    foreach(var symbols in configurations)
    {
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.CSharp9,preprocessorSymbols:symbols), path);
    foreach (var diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
    {
        Console.Error.WriteLine(diagnostic); errors++;
    }
    }
    count++;
}
Console.WriteLine($"C# 9 syntax: {count} files × {configurations.Length} conditional configurations, {errors} errors. This does not check Unity API references or Play Mode.");
return count > 0 && errors == 0 ? 0 : 1;
