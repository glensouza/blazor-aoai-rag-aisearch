# Infrastructure

For this project to work, we will need two resources in Azure. The only resource we need to create before getting starte is a Resource Group.

[Let's go create one now](https://portal.azure.com/#create/Microsoft.ResourceGroup)

## Azure AI Search

[Azure AI Search](https://azure.microsoft.com/en-us/products/ai-services/ai-search) is a search-as-a-service cloud solution that gives developers APIs and tools for adding a rich search experience over your data in web, mobile, and enterprise applications. It is a fully managed cloud search service that provides a better user experience for customers.

This will serve as both our database and our semantic search engine. Information we put in here will be used to add rich context to the LLM when giving answers about our data.

[Let's go create one now](https://portal.azure.com/#create/Microsoft.Search). The **Basic** tier should be sufficient for this project.

Once created open it up and:

1. head over to the `Overview` blade and copy the 'Url' value
1. under `Settings >> Keys` copy the 'Primary admin key' value

## Azure OpenAI

[Azure OpenAI Service](https://azure.microsoft.com/en-us/products/ai-services/openai-service) is a cloud-based API that provides advanced natural language processing capabilities. It enables developers to build AI-powered applications that can understand and generate human-like text.

It is currently still limited access to certain models and you need to request access through the [Azure AI Foundry](https://ai.azure.com/).

[Let's create one now](https://portal.azure.com/#create/Microsoft.CognitiveServicesOpenAI)

Once created open it up and:

1. navigate to `Resource Managment >> Keys and Endpoint` and copy the 'Key 1' and the 'Endpoint' values
1. head over to the `Overview` and click [Go to Azure AI Foundry portal](https://ai.azure.com/) which should open the **Azure AI Foundry** in a new tab
1. navigate to `Shared resources >> Deployments`
1. click on `+ Deploy model` button then select `Deploy base model`
1. select one of the **Chat completion** models available to you, I'll be using '**gpt-4o**' but any model will do, even older ones like '**gpt-35-turbo**'
1. note the '**Deployment Name**' you assigned it, you'll need it later
1. click `Deploy`
1. click on `+ Deploy model` button again then select `Deploy base model`
1. select one of the **Embeddings** models available to you, I'll be using '**text-embedding-ada-002**' but any model will do, even newer ones like '**text-embedding-3-small**', note the model you've chosen, you'll need it later
1. click `Deploy`

[<-- Back](./GettingStarted.md)
