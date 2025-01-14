# Seed Data

There are a couple of ways to seed some data you'd like to chat with in the chatbot using RAG pattern. [This](https://github.com/glensouza/dotnet-semantic-kernel-rag-demo) sample repository will show you the more UI friendly way to seed data using blob storage where it will crawl through your documents and add them to the Azure AI Search service.

Since we're all enterprise Dotnet developers here I'd like to show you how to seed data using a console application for ultimate flexibility and control of your data sources. This is a more programmatic way to seed data and is more suited for enterprise applications, especially those that consume third-party apis and want to include the results as part of the search without saving files.

## Create a Console Application

Create a file `RAGSearchDocument.cs` then open it and replace it with the  following code:

```csharp
public class RAGSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("contentVector")]
    public float[] ContentVector { get; set; }
}
```

Let's open up the `program.cs` file in the SeedData project. Here is the final code:

```csharp
using System.ClientModel;
using System.Reflection;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();

string aoaiEndpoint = config["AzureOpenAI:Endpoint"] ?? throw new Exception("AzureOpenAI:Endpoint needs to be set");
string aoaiChatDeploymentName = config["AzureOpenAI:ChatDeploymentName"] ?? throw new Exception("AzureOpenAI:ChatDeploymentName needs to be set");
string aoaiChatModel = config["AzureOpenAI:ChatModel"] ?? throw new Exception("AzureOpenAI:ChatModel needs to be set");
string aoaiEmbeddingDeploymentName = config["AzureOpenAI:EmbeddingDeploymentName"] ?? throw new Exception("AzureOpenAI:EmbeddingDeploymentName needs to be set");
string aoaiEmbeddingModel = config["AzureOpenAI:EmbeddingModel"] ?? throw new Exception("AzureOpenAI:EmbeddingModel needs to be set");
string searchServiceEndpoint = config["AzureSearch:Endpoint"] ?? throw new Exception("AzureSearch:Endpoint needs to be set");
string searchApiKey = config["AzureSearch:Key"] ?? throw new Exception("AzureSearch:Key needs to be set");
string indexName = config["AzureSearch:IndexName"] ?? throw new Exception("AzureSearch:IndexName needs to be set");

AzureOpenAIClient azureClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiKey));
ChatClient chatClient = azureClient.GetChatClient(aoaiChatDeploymentName);
EmbeddingClient embedClient = azureClient.GetEmbeddingClient(aoaiEmbeddingModel);

SearchIndexClient indexClient = new(new Uri(searchServiceEndpoint), new AzureKeyCredential(searchApiKey));
try
{
    indexClient.GetIndex(indexName);
    indexClient.DeleteIndex(indexName);
}
catch (RequestFailedException ex) when (ex.Status == 404)
{
    //if the specified index not exist, 404 will be thrown.
}

const string vectorSearchHnswProfile = "my-vector-profile";
const string vectorSearchHnswConfig = "myHnsw";
const string vectorSearchVectorizer = "myOpenAIVectorizer";
const string semanticSearchConfig = "my-semantic-config";
SearchIndex searchIndex = new(indexName)
{
    VectorSearch = new VectorSearch
    {
        Profiles =
        {
            new VectorSearchProfile(vectorSearchHnswProfile, vectorSearchHnswConfig)
                { VectorizerName = vectorSearchVectorizer }
        },
        Algorithms = { new HnswAlgorithmConfiguration(vectorSearchHnswConfig) },
        Vectorizers =
        {
            new AzureOpenAIVectorizer(vectorSearchVectorizer)
            {
                Parameters = new AzureOpenAIVectorizerParameters
                {
                    ResourceUri = new Uri(aoaiEndpoint),
                    ModelName = aoaiEmbeddingModel,
                    DeploymentName = aoaiEmbeddingDeploymentName
                }
            }
        }
    },
    SemanticSearch = new SemanticSearch
    {
        Configurations =
        {
            new SemanticConfiguration(semanticSearchConfig, new SemanticPrioritizedFields
            {
                TitleField = new SemanticField("title"),
                ContentFields =
                {
                    new SemanticField("content")
                }
            })
        }
    },
    Fields =
    {
        new SimpleField("id", SearchFieldDataType.String)
            { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
        new SearchableField("title") { IsFilterable = true },
        new SearchableField("content") { IsFilterable = true },
        new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
        {
            IsSearchable = true,
            VectorSearchDimensions = 1536,
            VectorSearchProfileName = vectorSearchHnswProfile
        }
    }
};
await indexClient.CreateOrUpdateIndexAsync(searchIndex);

ClientResult<ChatCompletion>? cc = await chatClient.CompleteChatAsync(
    new SystemChatMessage("You are an HR manager at Contoso Hotels, expert at employee handbook creation."),
    new UserChatMessage("Create a privacy policy, vacation policy, and describe a few job roles")
);
string text = string.Empty;
foreach (ChatMessageContentPart? message in cc.Value.Content)
{
    Console.WriteLine($"{message.Kind}: {message.Text}");
    text += message.Text;
}

SearchClient searchClient = indexClient.GetSearchClient(indexName);
const int maxChunkSize = 100;
List<string> chunks = ChunkText(text, maxChunkSize);
List<RAGSearchDocument> documents = [];
foreach (string chunk in chunks)
{
    Console.WriteLine($"Chunk: {chunk}");
    documents.Add(new RAGSearchDocument
    {
        Id = Guid.NewGuid().ToString(),
        Title = "Sample Document",
        Content = chunk,
        ContentVector = await GetEmbeddingAsync(chunk)
    });
}

UploadDocuments(documents);

Response<SearchResults<RAGSearchDocument>>? ss = searchClient.Search<RAGSearchDocument>("employee");
foreach (SearchResult<RAGSearchDocument> result in ss.Value.GetResults())
{
    Console.WriteLine($"Id: {result.Document.Id}, Title: {result.Document.Title}, Content: {result.Document.Content}");
}

return;

async Task<float[]> GetEmbeddingAsync(string text)
{
    ClientResult<OpenAIEmbedding>? response = await embedClient.GenerateEmbeddingAsync(text);
    float[] returnFloat = response.Value.ToFloats().ToArray();
    return returnFloat;
}

void UploadDocuments(List<RAGSearchDocument> documents)
{
    IndexDocumentsBatch<RAGSearchDocument>? batch = IndexDocumentsBatch.Upload(documents);
    searchClient.IndexDocuments(batch);
}

static List<string> ChunkText(string text, int maxChunkSize)
{
    List<string> chunks = new();
    int start = 0;

    while (start < text.Length)
    {
        int end = Math.Min(start + maxChunkSize, text.Length);

        // Ensure we don't cut off mid-sentence
        if (end < text.Length)
        {
            int lastPeriod = text.LastIndexOf('.', end);
            int lastNewLine = text.LastIndexOf('\n', end);

            // Find the last sentence end within the chunk
            int lastSentenceEnd = Math.Max(lastPeriod, lastNewLine);

            if (lastSentenceEnd >= start) end = lastSentenceEnd + 1;
        }

        chunks.Add(text.Substring(start, end - start).Trim());
        start = end;
    }

    return chunks;
}
```

Now let's run this application to seed some data into the Azure Search service. Log in to Azure and verify that the index was created properly.

[<-- Back](./GettingStarted.md)
