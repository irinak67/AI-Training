using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using SemanticKernelPlayground.Models;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace SemanticKernelPlayground.VectorDocs;

public class CodeDocumentationIngestor
{
    private readonly IVectorStore _vectorStore;

    public CodeDocumentationIngestor(IVectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    [KernelFunction]
    [Description("Ingests all code files from a directory into the vector store")]
    public async Task<string> IngestCodeDirectory(
        string directoryPath,
        [Description("File extensions to include, e.g. '.cs,.js'")] string extensions = ".cs",
        [Description("Collection name to use")] string collectionName = "code_documentation")
    {
        if (!Directory.Exists(directoryPath))
        {
            return $"Directory not found: {directoryPath}";
        }

        var extensionArray = extensions.Split(',').Select(e => e.Trim()).ToArray();
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                             .Where(f => extensionArray.Any(e => f.EndsWith(e)));

        Console.WriteLine($"Found {files.Count()} files to process in directory: {directoryPath}");

        List<TextChunk> allChunks = new();

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            var relativeFilePath = file.Substring(directoryPath.Length).TrimStart('\\', '/');
            var content = await File.ReadAllTextAsync(file);

            var chunks = AnalyzeCodeFile(content, relativeFilePath, fileInfo.Extension);
            allChunks.AddRange(chunks);
        }

        Console.WriteLine($"Analysis complete. Generated {allChunks.Count} documentation chunks.");
        Console.WriteLine("Starting vector store ingestion...");

        await IngestDocumentationAsync(collectionName, allChunks);

        return $"Successfully ingested {allChunks.Count} documentation chunks from {files.Count()} files into collection '{collectionName}'.";
    }

    private List<TextChunk> AnalyzeCodeFile(string content, string filePath, string extension)
    {
        var chunks = new List<TextChunk>();
        var lines = content.Split('\n'); 

        if (extension == ".cs")
        {
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

                        chunks.Add(new TextChunk
                        {
                            Id = Guid.NewGuid().ToString(),
                            FileName = filePath,                         
                            Line = i + 1,                                  
                            Content = lineContent,                       
                            Metadata = new DocumentMetadata
                            {
                                Type = "Class",
                                FileName = filePath,
                                ElementName = className,
                                LastModified = DateTime.Now,
                                Tags = new List<string> { "class", className }
                            },
                            Embedding = ReadOnlyMemory<float>.Empty
                        });
                    }
                }
            }
        }

        if (chunks.Count == 0)
        {
            var fileLines = content.Split('\n');
            for (int i = 0; i < fileLines.Length; i++)
            {
                var lineContent = fileLines[i].Trim();
                if (string.IsNullOrWhiteSpace(lineContent)) continue;

                chunks.Add(new TextChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = filePath,
                    Line = i + 1,
                    Content = lineContent,
                    Metadata = new DocumentMetadata
                    {
                        Type = "File",
                        FileName = filePath,
                        ElementName = Path.GetFileName(filePath),
                        LastModified = DateTime.Now,
                        Tags = new List<string> { "file", extension }
                    },
                    Embedding = ReadOnlyMemory<float>.Empty
                });
            }
        }

        return chunks;
    }


    public async Task IngestDocumentationAsync(string collectionName, IEnumerable<TextChunk> documents)
    {
        try
        {
            Console.WriteLine($"Starting batch ingestion of {documents.Count()} documents into collection '{collectionName}'");

            var collection = _vectorStore.GetCollection<string, TextChunk>(collectionName);

            await collection.CreateCollectionIfNotExistsAsync();

            int processedCount = 0;

            foreach (var doc in documents)
            {
                await collection.UpsertAsync(doc);

                processedCount++;
                if (processedCount % 10 == 0)
                {
                    Console.WriteLine($"Ingestion progress: {processedCount}/{documents.Count()} documents processed");
                }
            }

            Console.WriteLine($"Batch ingestion completed. {processedCount} documents loaded into vector store.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during batch ingestion: {ex.Message}");
            throw;
        }
    }

    private IVectorStoreRecordCollection<string, TextChunk> GetCollection(string collectionName)
    {
        var collection = _vectorStore.GetCollection<string, TextChunk>(collectionName);

        return collection;
    }
}
