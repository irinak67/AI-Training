using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelPlayground.Plugins;
using SemanticKernelPlayground.VectorDocs;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

const string ExitCommand = "exit";

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var model = configuration["ModelName"] ?? throw new Exception("ModelName missing");
var embedding = configuration["EmbeddingModel"] ?? throw new Exception("EmbeddingModel missing");
var endpoint = configuration["Endpoint"] ?? throw new Exception("Endpoint missing");
var apiKey = configuration["ApiKey"] ?? throw new Exception("ApiKey missing");


var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(model, endpoint, apiKey)
    .AddAzureOpenAITextEmbeddingGeneration(embedding, endpoint, apiKey)
    .AddInMemoryVectorStore();

var vectorStore = builder.Services.BuildServiceProvider().GetRequiredService<IVectorStore>();

builder.Plugins.AddFromType<GitPlugin>("Git");
builder.Plugins.AddFromPromptDirectory("Prompts/ReleaseNotesPlugin", "ReleaseNotes");
builder.Plugins.AddFromObject(new CodeDocumentationIngestor(vectorStore), "CodeDocumentation");

var kernel = builder.Build();

var chatService = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();

await RunIngestCommand.IngestDirectoryAsync(kernel);

var searchPlugin = CodeSearchPluginBuilder.Create(kernel);
kernel.Plugins.Add(searchPlugin);

Console.ForegroundColor = ConsoleColor.DarkCyan;
Console.WriteLine("┌────────────────────────────────────────────┐");
Console.WriteLine("│       Welcome to Semantic Kernel AI!       │");
Console.WriteLine("└────────────────────────────────────────────┘");
Console.ResetColor();

Console.WriteLine();
Console.WriteLine("I can help you with two main things:");

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Git & Release Notes");
Console.ResetColor();
Console.WriteLine("   • Generate release notes from commits");
Console.WriteLine("   • View recent repository activity");

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("Code Knowledge Chat");
Console.ResetColor();
Console.WriteLine("   • Ask me about your code");
Console.WriteLine("   • I’ll answer based on indexed project files");

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("-> To exit at any time, type 'exit' and press Enter.");
Console.ResetColor();

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.Write("Assistant > ");
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("How can I help you today?");
Console.ResetColor();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.Write("Me > ");
Console.ResetColor();

string? input = Console.ReadLine()?.Trim();
if (string.Equals(input, ExitCommand, StringComparison.OrdinalIgnoreCase)) return;

await RunChatLoop(kernel, chatService, history, input!);

static async Task RunChatLoop(Kernel kernel, IChatCompletionService chatService, ChatHistory history, string input)
{
    var settings = new OpenAIPromptExecutionSettings
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    history.AddUserMessage(input);

    while (true)
    {
        string response = "";
        bool assistantHeaderPrinted = false;

        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(history, settings, kernel))
        {
            if (!string.IsNullOrWhiteSpace(chunk.Content))
            {
                if (!assistantHeaderPrinted)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Assistant > ");
                    assistantHeaderPrinted = true;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(chunk.Content);
                Console.ResetColor();

                response += chunk.Content;
            }
        }

        Console.WriteLine();
        history.AddAssistantMessage(response);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Me > ");
        Console.ResetColor();

        input = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.Equals(input, ExitCommand, StringComparison.OrdinalIgnoreCase)) break;

        if (string.IsNullOrWhiteSpace(input)) continue;

        history.AddUserMessage(input!);
    }
}
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
