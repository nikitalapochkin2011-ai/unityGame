using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

string root = args.Length > 0 ? args[0] : "Assets/OneMoreFloor";
int count = 0, errors = 0;
foreach (var path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
{
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.CSharp9), path);
    foreach (var diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
    {
        Console.Error.WriteLine(diagnostic); errors++;
    }
    count++;
}
Console.WriteLine($"C# 9 syntax: {count} files, {errors} errors. This does not check Unity API references or Play Mode.");
return count > 0 && errors == 0 ? 0 : 1;
