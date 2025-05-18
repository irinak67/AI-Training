using Microsoft.SemanticKernel;

namespace SemanticKernelPlayground.VectorDocs
{
    public static class RunIngestCommand
    {
        public static async Task IngestDirectoryAsync(Kernel kernel)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== Start Ingesting Code Directory ===");
            Console.ResetColor();

            Console.Write("Enter directory path (or leave empty for current folder): ");
            var directory = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(directory))
                directory = Environment.CurrentDirectory;

            Console.Write("Enter file extensions (e.g. .cs,.js), default is .cs: ");
            var extensions = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(extensions))
                extensions = ".cs";

            var arguments = new KernelArguments
            {
                ["directoryPath"] = directory,
                ["extensions"] = extensions,
                ["collectionName"] = "code_documentation"
            };

            try
            {
                var result = await kernel.InvokeAsync("CodeDocumentation", "IngestCodeDirectory", arguments);

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("\n[Ingest Completed]");
                Console.ResetColor();
                Console.WriteLine(result?.ToString() ?? "No result returned.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error during ingestion: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
