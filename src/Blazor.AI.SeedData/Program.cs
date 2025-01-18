// ***********************************************************************
// Author           : glensouza
// Created          : 01-14-2025
//
// Last Modified By : glensouza
// Last Modified On : 01-16-2025
// ***********************************************************************
// <summary>This Console App is to seed data on Azure AI Search.</summary>
// ***********************************************************************

using System.ClientModel;
using System.Diagnostics;
using System.Reflection;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Blazor.AI.SeedData;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;

// Create a progress reporter
IProgress<string> progress = new Progress<string>(Console.WriteLine);

// Build configuration from user secrets
progress.Report("Getting values from configuration...");
Stopwatch stopwatch = Stopwatch.StartNew();
IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();
stopwatch.Stop();
progress.Report($"End Getting values from configuration... (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

// Retrieve configuration values from user secrets
string aoaiKey = config["AzureOpenAI:Key"] ?? throw new Exception("AzureOpenAI:Key needs to be set");
string aoaiEndpoint = config["AzureOpenAI:Endpoint"] ?? throw new Exception("AzureOpenAI:Endpoint needs to be set");
string aoaiChatDeploymentName = config["AzureOpenAI:ChatDeploymentName"] ?? throw new Exception("AzureOpenAI:ChatDeploymentName needs to be set");
string aoaiEmbeddingDeploymentName = config["AzureOpenAI:EmbeddingDeploymentName"] ?? throw new Exception("AzureOpenAI:EmbeddingDeploymentName needs to be set");
string aoaiEmbeddingModel = config["AzureOpenAI:EmbeddingModel"] ?? throw new Exception("AzureOpenAI:EmbeddingModel needs to be set");
string searchServiceEndpoint = config["AzureSearch:Endpoint"] ?? throw new Exception("AzureSearch:Endpoint needs to be set");
string searchApiKey = config["AzureSearch:Key"] ?? throw new Exception("AzureSearch:Key needs to be set");
string indexName = config["AzureSearch:IndexName"] ?? throw new Exception("AzureSearch:IndexName needs to be set");
progress.Report("End Getting values from configuration...");

// Initialize Azure OpenAI and Search clients
progress.Report("Initializing Azure OpenAI and Search clients...");
stopwatch.Restart();
AzureOpenAIClient azureClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiKey));
ChatClient chatClient = azureClient.GetChatClient(aoaiChatDeploymentName);
EmbeddingClient embedClient = azureClient.GetEmbeddingClient(aoaiEmbeddingModel);
SearchIndexClient indexClient = new(new Uri(searchServiceEndpoint), new AzureKeyCredential(searchApiKey));
stopwatch.Stop();
progress.Report($"End Initializing Azure OpenAI and Search clients... (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

// Delete existing index if it exists
try
{
    progress.Report("Deleting existing index...");
    stopwatch.Restart();
    indexClient.GetIndex(indexName);
    indexClient.DeleteIndex(indexName);
    stopwatch.Stop();
    progress.Report($"Index deleted successfully. (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");
}
catch (RequestFailedException ex) when (ex.Status == 404)
{
    // If the specified index does not exist, a 404 will be thrown.
    progress.Report("Index does not exist. Skipping deletion.");
}

// Define vector search and semantic search configurations
const string vectorSearchHnswProfile = "my-vector-profile";
const string vectorSearchHnswConfig = "myHnsw";
const string vectorSearchVectorizer = "myOpenAIVectorizer";
const string semanticSearchConfig = "my-semantic-config";

// Create a new search index with vector search and semantic search configurations
SearchIndex searchIndex = new(indexName)
{
    VectorSearch = new VectorSearch
    {
        Profiles =
        {
            new VectorSearchProfile(vectorSearchHnswProfile, vectorSearchHnswConfig) { VectorizerName = vectorSearchVectorizer }
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
                ContentFields = { new SemanticField("content") }
            })
        }
    },
    Fields =
    {
        new SimpleField("id", SearchFieldDataType.String)
        {
            IsKey = true,
            IsFilterable = true,
            IsSortable = true,
            IsFacetable = true
        },
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

// Create the search index
progress.Report("Starting index creation...");
stopwatch.Restart();
await indexClient.CreateOrUpdateIndexAsync(searchIndex);
stopwatch.Stop();
progress.Report($"Index creation completed. (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

// Initialize search client
progress.Report("Initializing search client...");
stopwatch.Restart();
SearchClient searchClient = indexClient.GetSearchClient(indexName);
stopwatch.Stop();
progress.Report($"Search client initialized. (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

// Generate seed data using chat completion feature with Azure OpenAI
List<ChatMessage> chatHistory =
[
    new SystemChatMessage("You are an HR manager at Contoso Hotels, expert at employee handbook creation."),
    new UserChatMessage("Create a privacy policy, vacation policy, and describe a few job roles")
];

// Generate seed data using chat completion feature with Azure OpenAI
progress.Report("Generating seed data using chat completion...");
stopwatch.Restart();
ClientResult<ChatCompletion>? seedData = await chatClient.CompleteChatAsync(chatHistory);
stopwatch.Stop();
progress.Report($"Seed data generation completed. (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

// Collect the seed data from chat completion text
string text = string.Empty;
foreach (ChatMessageContentPart? message in seedData.Value.Content)
{
    // Output the data so we can see it and use it later
    Console.WriteLine($"{message.Kind}: {message.Text}");
    text += message.Text;
}
chatHistory.Add(new AssistantChatMessage(text));

// Output the employee handbook to text file, first checking if it already exists
string filePath = "employee_handbook.txt";
if (File.Exists(filePath))
{
    File.Delete(filePath);
}
File.WriteAllText(filePath, text);

// Open the text file for the user to review
Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
progress.Report("Seed data generation completed.");

// Chunk the text into smaller pieces
progress.Report("Chunking text into smaller pieces...");
stopwatch.Restart();
const int maxChunkSize = 1000;
List<string> chunks = ChunkText(text, maxChunkSize);

// Create documents from the chunks and upload them to the search index
List<RAGSearchDocument> documents = [];
foreach (string chunk in chunks)
{
    documents.Add(new RAGSearchDocument
    {
        Id = Guid.NewGuid().ToString(),
        Title = "Sample Document",
        Content = chunk,
        ContentVector = await GetEmbeddingAsync(chunk)
    });
}
stopwatch.Stop();
progress.Report($"Text chunking completed. (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

// Upload documents to the search index
progress.Report("Uploading documents to the search index...");
stopwatch.Restart();
UploadDocuments(documents);
stopwatch.Stop();
progress.Report($"Document upload completed. (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

// Perform a search query on the index to test that it works
progress.Report("Performing search query...");
stopwatch.Restart();
Response<SearchResults<RAGSearchDocument>>? ss = searchClient.Search<RAGSearchDocument>("employee");
foreach (SearchResult<RAGSearchDocument> result in ss.Value.GetResults())
{
    Console.WriteLine($"Id: {result.Document.Id}, Title: {result.Document.Title}, Content: {result.Document.Content}");
}
stopwatch.Stop();
progress.Report($"Search query completed. (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

// Generate good questions for employee to ask about the employee handbook
progress.Report("Generating good questions for employee to ask...");
stopwatch.Restart();
chatHistory.Add(new UserChatMessage("What are 3 good questions for an employee to ask about information that is in the employee handbook you just wrote? Also what is the right answer that the employee should expect?"));
seedData = await chatClient.CompleteChatAsync(chatHistory);
Console.WriteLine("Ask these questions to the bot later:");
text = string.Empty;
foreach (ChatMessageContentPart? message in seedData.Value.Content)
{
    // Output the data so we can see it and use it later
    Console.WriteLine(message.Text);
    text += message.Text;
}

// Output the good questions to text file, first checking if it already exists
filePath = "good_questions.txt";
if (File.Exists(filePath))
{
    File.Delete(filePath);
}
File.WriteAllText(filePath, text);

// Open the text file for the user to review
Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });

stopwatch.Stop();
progress.Report($"Good questions generation completed. (Elapsed time: {stopwatch.ElapsedMilliseconds} ms)");

return;

// Function to get embedding for a given text
async Task<float[]> GetEmbeddingAsync(string textToEmbed)
{
    if (string.IsNullOrWhiteSpace(textToEmbed))
    {
        // Return an empty array if the text is empty
        return [];
    }

    // Generate the embedding for the text
    ClientResult<OpenAIEmbedding>? response = await embedClient.GenerateEmbeddingAsync(textToEmbed);
    float[] returnFloat = response.Value.ToFloats().ToArray();
    return returnFloat;
}

// Function to upload documents to the search index
void UploadDocuments(List<RAGSearchDocument> documentsToEmbed)
{
    IndexDocumentsBatch<RAGSearchDocument>? batch = IndexDocumentsBatch.Upload(documentsToEmbed);
    searchClient.IndexDocuments(batch);
}

// Function to chunk text into smaller pieces
static List<string> ChunkText(string text, int maxChunkSize)
{
    List<string> chunks = [];
    int start = 0;

    while (start < text.Length)
    {
        // Find the end of the chunk
        int end = Math.Min(start + maxChunkSize, text.Length);

        // Ensure we don't cut off mid-sentence
        if (end < text.Length)
        {
            int lastPeriod = text.LastIndexOf('.', end);
            int lastNewLine = text.LastIndexOf('\n', end);

            // Find the last sentence end within the chunk
            int lastSentenceEnd = Math.Max(lastPeriod, lastNewLine);

            // If we found a sentence end, adjust the end of the chunk
            if (lastSentenceEnd >= start)
            {
                end = lastSentenceEnd + 1;
            }
        }

        // Add the chunk to the list
        chunks.Add(text.Substring(start, end - start).Trim());

        // Move the start index to the end of the current chunk
        start = end;
    }

    return chunks;
}
