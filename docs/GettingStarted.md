# Getting Started

We will be learning how to interact with the Azure OpenAI API using the RAG pattern via a chatbot in a Blazor application

The RAG (Retrieval-Augmented Generation) pattern is an AI model architecture that enhances the generation of text by integrating external, relevant information retrieved from a knowledge base or database, improving accuracy and context. It combines retrieval mechanisms with generative models to produce more informed and contextually relevant responses.

## About Products Used

### Azure

There are two resources needed to work with Azure OpenAI with RAG pattern: [Infrastructure](./Infrastructure.md)

### Visual Studio

I'll be using [Visual Studio 2022](https://visualstudio.microsoft.com) for this project. You can use any other IDE of your choice.

### Dotnet 9

Though I won't be teaching how to code in C#, I'll be using [.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) for this project. You can use any version of Dotnet from 6 forward and it should work just fine.

### Semantic Kernel

[Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview) is a library that provides a simple way to interact with the Azure OpenAI API using the RAG pattern. It is a wrapper around the Azure OpenAI API that makes it easier to interact with the API. It is available as a Nuget package and can be installed via the Nuget package manager.

## What's Next

- [Project Setup](./ProjectSetup.md)
- [SeedData](./SeedData.md)
- [Code](./Code.md)
