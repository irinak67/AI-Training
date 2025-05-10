using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelPlayground.Plugins;
using System.Text.RegularExpressions;

const string ExitCommand = "exit";

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var model = configuration["ModelName"] ?? throw new Exception("ModelName missing");
var endpoint = configuration["Endpoint"] ?? throw new Exception("Endpoint missing");
var apiKey = configuration["ApiKey"] ?? throw new Exception("ApiKey missing");

var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(model, endpoint, apiKey);

builder.Plugins.AddFromObject(new GitPlugin(), "Git");
builder.Plugins.AddFromPromptDirectory("Prompts/ReleaseNotesPlugin", "ReleaseNotes");

var kernel = builder.Build();

var chatService = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();

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
Console.WriteLine("   • Notes are structured into: Commits Overview and Highlights");
Console.WriteLine("   • Formatted in Markdown — ready for GitHub releases");

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Chat with AI");
Console.ResetColor();
Console.WriteLine("   • Ask me any question");
Console.WriteLine("   • Get help with coding, learning, or ideas");
Console.WriteLine("   • Just type your message to start chatting");

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Magenta;
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
        if (input.ToLower().Contains("commit"))
        {
            var match = Regex.Match(input, @"\b(\d+)\b");
            int count = 5;

            if (match.Success)
            {
                count = int.Parse(match.Groups[1].Value); 
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Assistant > ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("How many commits would you like to see?");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Me > ");
                Console.ResetColor();

                var countInput = Console.ReadLine();
                if (!int.TryParse(countInput, out count)) count = 5;
            }

            var result = await kernel.InvokeAsync("Git", "GetLatestCommits", new() { ["count"] = count });
            var commitText = result.GetValue<string>() ?? "(No commits)";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nAssistant > Here's the latest commits:\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(commitText);
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Me > ");
            Console.ResetColor();

            input = Console.ReadLine()?.Trim() ?? string.Empty;
            if (input.ToLower() == ExitCommand) break;

            history.AddUserMessage(input);
            continue;
        }

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
