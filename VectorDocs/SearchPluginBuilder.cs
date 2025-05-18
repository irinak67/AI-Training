using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernelPlayground.Models;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace SemanticKernelPlayground.VectorDocs;

public static class CodeSearchPluginBuilder
{
    public static KernelPlugin Create(Kernel kernel)
    {
        var collection = kernel.GetRequiredService<IVectorStore>().GetCollection<string, TextChunk>("projectDocs");
        var embedder = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        var searchPlugin = new VectorStoreTextSearch<TextChunk>(
            collection,
            embedder,
            new CodeTextStringMapper(),
            new CodeTextResultMapper());

        return searchPlugin.CreateWithGetSearchResults("CodeDocSearch");
    }

    private sealed class CodeTextStringMapper : ITextSearchStringMapper
    {
        public string MapFromResultToString(object result) =>
            result is TextChunk chunk ? chunk.Content : throw new ArgumentException();
    }

    private sealed class CodeTextResultMapper : ITextSearchResultMapper
    {
        public TextSearchResult MapFromResultToTextSearchResult(object result)
        {
            var chunk = result as TextChunk ?? throw new ArgumentException();
            return new TextSearchResult(chunk.Content)
            {
                Name = chunk.FileName,
                Link = $"Line {chunk.Line}"
            };
        }
    }
}
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.