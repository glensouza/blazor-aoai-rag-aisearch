using Azure.Search.Documents;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Blazor.AI.Web.Plugins;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class SearchPlugin(ITextEmbeddingGenerationService textEmbeddingGenerationService, SearchIndexClient indexClient)
{
    [KernelFunction("contoso_search")]
    [Description("Search documents for employer Contoso")]
    public async Task<string> SearchAsync([Description("The users optimized semantic search query")] string query)
    {
        // Convert string query to vector
        ReadOnlyMemory<float> embedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(query);

        // Set the index to use in AI Search
        SearchClient searchClient = indexClient.GetSearchClient("demo");

        // Configure request parameters
        VectorizedQuery vectorQuery = new(embedding);
        vectorQuery.Fields.Add("contentVector"); // name of the vector field from index schema

        SearchOptions searchOptions = new() { VectorSearch = new() { Queries = { vectorQuery } } };

        //var response = await searchClient.SearchAsync<SearchDocument>(searchOptions);

        // Perform search request
        Response<SearchResults<IndexSchema>> response = await searchClient.SearchAsync<IndexSchema>(searchOptions);

        //// Collect search results
        await foreach (SearchResult<IndexSchema> result in response.Value.GetResultsAsync())
        {
            return result.Document.Content; // Return text from first result
        }

        return string.Empty;
    }

    //This schema comes from the index schema in Azure AI Search
    private sealed class IndexSchema
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

}