using Microsoft.Extensions.VectorData;

namespace SemanticKernelPlayground.Models;

public class TextChunk
{
    [VectorStoreRecordKey]
    public required string Id { get; init; }

    [VectorStoreRecordData]
    public required string FileName { get; init; }

    [VectorStoreRecordData]
    public required int Line { get; init; }

    [VectorStoreRecordData]
    public required string Content { get; init; }

    [VectorStoreRecordData]
    public DocumentMetadata Metadata { get; set; }

    [VectorStoreRecordVector(3072)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}

public class DocumentMetadata
{
    public string Type { get; set; }
    public string FileName { get; set; }
    public string ElementName { get; set; }
    public string Namespace { get; set; }
    public DateTime LastModified { get; set; }
    public List<string> Tags { get; set; }
}

