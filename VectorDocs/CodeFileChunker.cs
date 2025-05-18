using LibGit2Sharp;
using SemanticKernelPlayground.Models;
using System.Text.RegularExpressions;

namespace SemanticKernelPlayground.VectorDocs;

public static class CodeFileChunker
{
    public static IEnumerable<TextChunk> ParseRepository(string repositoryPath, string[] extensions) 
    {
        if (!Repository.IsValid(repositoryPath))
        {
            throw new ArgumentException("Invalid git repository path.", nameof(repositoryPath));
        }

        var files = Directory.GetFiles(repositoryPath, "*.*", SearchOption.AllDirectories)
                             .Where(file => extensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                             .ToList();

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            var relativePath = Path.GetRelativePath(repositoryPath, file);
            var content = string.Join("\n", lines);

            var classMatches = Regex.Matches(content, @"class\s+(\w+)");
            foreach (Match match in classMatches)
            {
                var className = match.Groups[1].Value;
                var startIndex = match.Index;
                var classEndIndex = content.IndexOf("}", startIndex);

                if (classEndIndex > startIndex)
                {
                    var classContent = content.Substring(startIndex, classEndIndex - startIndex + 1);
                    var classLines = classContent.Split('\n');

                    for (int i = 0; i < classLines.Length; i++)
                    {
                        var lineContent = classLines[i].Trim();
                        if (string.IsNullOrWhiteSpace(lineContent)) continue;

                        yield return new TextChunk
                        {
                            Id = Guid.NewGuid().ToString(),
                            FileName = relativePath,    
                            Line = i + 1,               
                            Content = lineContent, 
                            Metadata = new DocumentMetadata
                            {
                                Type = "Class",
                                FileName = relativePath,
                                ElementName = className,
                                Namespace = "",
                                LastModified = File.GetLastWriteTime(file),
                                Tags = new List<string> { "class", className }
                            },
                            Embedding = ReadOnlyMemory<float>.Empty
                        };
                    }
                }
            }
        }
    }
}
