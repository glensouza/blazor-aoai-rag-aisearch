# Project Setup

1. Open Visual Studio
1. Click on `File` -> `New` -> `Project`
1. Select `.NET Aspire Starter App` and click `Next`
1. Configure your new project:

    - Enter value for `Solution name`
    - Enter path for `Location`
    - Leave `Create in new folder` selected
    - Click `Next`

1. Additional Information:

    - Select `.NET 9.0 (Standard Term Support)`
    - Leave `Configure for HTTPS` selected
    - Unselect `Use Redis for caching`
    - Under `Create a test project` seelct `None`
    - Click `Create`

1. Let's remove the API project and references to it on the navigation bar

1. Run the project by clicking on the green play button in the top menu bar

1. Let's add another project to the solution, the `SeedData` project

    1. Right-click on the solution and select `Add` -> `New Project`
    1. Select 'Console App' and click `Next`

        1. Configure your new project:

            - Enter value for `Project name`
            - Enter path for `Location

        1. Click `Next`
        1. Select:

            - `.NET 9.0 (Standard Term Support)`
            - unselect `Enable container support`
            - unselect `Do not use top-level statements`
            - unselect `Enable native AOt publish`

        1. Click `Create`

1. Let's now add the secrets we kept from the `Infrastructure` setup

    1. Right-click on the `Web` project and select `Manage User Secrets`
    1. Add the following secrets:

        ```json
        {
            "AzureOpenAI:Endpoint": "https://api.openai.com",
            "AzureOpenAI:Key": "YOUR_API_KEY",
            "AzureOpenAI:ChatDeploymentName": "YOUR_DEPLOYMENT_NAME",
            "AzureOpenAI:ChatModel": "YOUR_MODEL_NAME",
            "AzureOpenAI:EmbeddingDeploymentName": "YOUR_DEPLOYMENT_NAME",
            "AzureOpenAI:EmbeddingModel": "YOUR_EMBEDDING_MODEL_NAME",
            "AzureSearch:Endpoint": "https://YOUR_SEARCH_NAME.search.windows.net",
            "AzureSearch:Key": "YOUR_SEARCH_KEY",
            "AzureSearch:IndexName": "YOUR_INDEX_NAME"
        }
        ```

    1. Save and close the `secrets.json` file
    1. Right-click on the `SeedData` project and select `Manage User Secrets`
    1. Close that file.
    1. Open the `Web` project's '.csproj' file and copy the `<UserSecretsId>` value
    1. Open the `SeedData` project's '.csproj' file and paste the `<UserSecretsId>` value

1. Let's now add the NuGet packages needed for all projects:

    1. Right-click on the solution and select `Manage NuGet Packages for Solution`
    1. Navigate to the `Browse` tab
    1. Search for and install the following packages:

        | Package Name | SeedData Project | Web Project |
        | ------------ | ---------------- | ----------- |
        | Azure.AI.OpenAI | X | |
        | Azure.Search.Documents | | X |
        | Markdig | | X |
        | Markdown.ColorCode | | X |
        | Microsoft.Extensions.Configuration | X | |
        | Microsoft.Extensions.Configuration.UserSecrets | X | |
        | Microsoft.SemanticKernel | | X |
        | Microsoft.SemanticKernel.Connectors.AzureAISearch | | X |
        | Microsoft.SemanticKernel.Connectors.OpenAI | | X |
        | Microsoft.SemanticKernel.Plugins.OpenApi | | X |

[<-- Back](./GettingStarted.md)
