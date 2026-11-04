using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

// DocFX は目次にないページでもビルドするため、公開チュートリアルの孤立を別途検出する。
// Architecture/ は非公開の設計資料なので、Docs/ 直下の Markdown だけを対象にする。
var docsDirectory = Path.GetFullPath(Path.Combine(SourceDirectory(), "../../Docs"));
var tutorials = Directory.EnumerateFiles(docsDirectory)
    .Where(path => path.EndsWith(".md", StringComparison.Ordinal))
    .Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray();
var toc = File.ReadAllText(Path.Combine(docsDirectory, "toc.yml"));
var referenced = Regex.Matches(toc, @"href:\s*([^\s]+\.md)")
    .Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
var valid = true;

foreach (var tutorial in tutorials)
{
    if (!referenced.Contains(tutorial!))
    {
        Console.Error.WriteLine($"::error::Docs/{tutorial} is not listed in Docs/toc.yml");
        valid = false;
    }
}

foreach (var reference in referenced.Order(StringComparer.Ordinal))
{
    if (!File.Exists(Path.Combine(docsDirectory, reference)))
    {
        Console.Error.WriteLine($"::error::Docs/toc.yml references Docs/{reference} which does not exist");
        valid = false;
    }
}

if (!valid)
{
    Console.Error.WriteLine("\nUpdate Docs/toc.yml: add entries for new tutorials (or remove stale ones).");
    return 1;
}

Console.WriteLine($"All {tutorials.Length} tutorials in Docs/ are referenced by Docs/toc.yml.");
return 0;

static string SourceDirectory([CallerFilePath] string source = "") => Path.GetDirectoryName(source)!;
