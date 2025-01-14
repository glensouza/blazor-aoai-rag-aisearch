# Code

Now for the fun part, let's write some code to make the chatbot work. First open the `App.razor` file and replace the bootstrap reference with the following two lines:

```html
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-1BmE4kWBq78iYhFldvKuhfTAU6auU8tT94WrHftjDbrCEXSU1oBoqyl2QvZ6jIW3" crossorigin="anonymous">
<link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css' rel='stylesheet'>

```

## Plugins

First, let's create the plugins that Azure OpenAI can interact with. Create a new folder called `Plugins` under the root folder of the web project then add a file called `TimeInformationPlugin.cs` and add the following:

```csharp
public class TimeInformationPlugin
{
    [KernelFunction]
    [Description("Retrieves the current time in UTC.")]
    public string GetCurrentUtcTime() => DateTime.UtcNow.ToString("R");
}
```

Then add the `SearchPlugin.cs` file and add the following:

```csharp

```

Let's create that component that will be floating in the bottom right corner of the screen. Create a new folder called `Components` under the `Components` folder then add a file called `ChatComponent.razor` and add the following:

```razor
@rendermode InteractiveServer

@inject ILocalStorageService LocalStorage
@inject IJSRuntime JsRuntime
@inject IChatCompletionService ChatCompletionService

@if (!string.IsNullOrEmpty(this.whatAmI))
{
    <input type="checkbox" id="check" @onclick="this.ClearChat" />
    <label class="chat-btn" for="check">
        <i class="fa fa-commenting-o comment"></i>
        <i class="fa fa-close close"></i>
    </label>
    <div class="wrapper">
        <div class="container">
            <div class="d-flex justify-content-center">
                <div class="card" id="chat1" style="border-radius: 15px;">
                    <div class="card-header d-flex justify-content-between align-items-center p-3 bg-info text-white border-bottom-0" style="border-top-left-radius: 15px; border-top-right-radius: 15px;">
                        <p class="mb-0 fw-bold">Chat with Azure OpenAI</p>
                    </div>
                    <div class="card-body" id="messages-container" style="height: 500px; overflow-y: auto; background-color: #eee; border: 2px solid #0CCAF0">
                        @foreach (string chatMessage in this.messages)
                        {
                            @if (chatMessage.Contains("|AI|"))
                            {
                                <div class="d-flex flex-row justify-content-start mb-4">
                                    <div class="avatarSticky">
                                        <img src="openai-chatgpt-logo-icon.webp" alt="avatar 1" style="width: 45px; height: 100%;">
                                    </div>
                                    <div class="p-3 ms-3" style="border-radius: 15px; background-color: rgba(57, 192, 237,.2);">
                                        @((MarkupString)chatMessage.Replace("|AI|", string.Empty))
                                    </div>
                                </div>
                                continue;
                            }

                            <div class="d-flex flex-row justify-content-end mb-4">
                                <div class="p-3 me-3 border" style="border-radius: 15px; background-color: #fbfbfb;">
                                    @((MarkupString)chatMessage)
                                </div>
                                <div class="avatarSticky">
                                    <img src="user-icon.png" alt="avatar 1" style="width: 45px; height: 100%;">
                                </div>
                            </div>
                        }

                        @if (!string.IsNullOrEmpty(this.htmlStreamingResponse))
                        {
                            <div class="d-flex flex-row justify-content-start mb-4">
                                <div class="avatarSticky">
                                    <img src="openai-chatgpt-logo-icon-progress.webp" alt="avatar 1" style="width: 45px; height: 100%;">
                                </div>
                                <div class="p-3 ms-3" style="border-radius: 15px; background-color: #39c8ed; color: #ffff;">
                                    @((MarkupString)this.htmlStreamingResponse)
                                </div>
                            </div>
                        }
                    </div>
                    <div class="card-footer bg-info text-white" style="border-bottom-left-radius: 15px; border-bottom-right-radius: 15px;">
                        <div class="input-group">
                            <input type="text" class="form-control" placeholder="Prompt..." @bind="this.message" id="Message" @onkeydown="this.EnterCheckMessage" disabled=@(!string.IsNullOrEmpty(this.htmlStreamingResponse)) />
                            <span class="input-group-btn">
                                <button class="btn btn-default" @onclick="this.SendChat" disabled=@(!string.IsNullOrEmpty(this.htmlStreamingResponse))>
                                    <i class="fa fa-paper-plane"></i>
                                </button>
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
}

<HeadContent>
    <script>
        function scrollToBottom() {
            var objDiv = document.getElementById("messages-container");
            objDiv.scrollTop = objDiv.scrollHeight;
        }

        function focusOnWhatAmI() {
            var what = document.getElementById("WhatAmI");
            if (what != null) {
                setTimeout(() => what.focus(), 100);
            }
        }

        function focusOnMessage() {
            var message = document.getElementById("Message");
            if (message != null) {
                setTimeout(() => message.focus(), 100);
            }
        }
    </script>
</HeadContent>

@code {
    private string whatAmI = string.Empty;
    private string message = string.Empty;
    private string htmlStreamingResponse = string.Empty;
    private readonly ChatHistory chatHistory = [];
    private readonly List<string> messages = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            this.whatAmI = await this.LocalStorage.GetItemAsync<string>("whatAmI") ?? string.Empty;
            this.StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task EnterCheckMessage(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await this.SendChat();
        }
    }

    private async Task SendChat()
    {
        if (string.IsNullOrEmpty(this.message))
        {
            await this.JsRuntime.InvokeVoidAsync("focusOnMessage");
            return;
        }

        this.htmlStreamingResponse = "<i class=\"fa fa-ellipsis-h\"></i>";
        this.chatHistory.AddUserMessage(this.message);
        this.messages.Add(this.message);
        this.message = string.Empty;
        string streamingResponse = string.Empty;
        await foreach (StreamingChatMessageContent response in this.ChatCompletionService.GetStreamingChatMessageContentsAsync(this.chatHistory))
        {
            streamingResponse += response;
            this.htmlStreamingResponse = Markdig.Markdown.ToHtml($"{streamingResponse} <i class=\"fa fa-ellipsis-h\"></i>");
            this.StateHasChanged();
            await this.JsRuntime.InvokeVoidAsync("scrollToBottom");
            await Task.Delay(100);
        }

        this.chatHistory.AddAssistantMessage(streamingResponse);
        this.messages.Add($"{Markdig.Markdown.ToHtml($"|AI|{streamingResponse}")}");
        this.htmlStreamingResponse = string.Empty;
        await this.JsRuntime.InvokeVoidAsync("focusOnMessage");
    }

    private async Task ClearChat()
    {
        this.messages.Clear();
        this.chatHistory.Clear();
        this.chatHistory.AddSystemMessage(this.whatAmI);
        this.htmlStreamingResponse = string.Empty;
        this.message = string.Empty;
        this.messages.Add("|AI|Welcome to the AI chatbot! I am an AI chatbot trained by OpenAI.");
        this.StateHasChanged();
        await this.JsRuntime.InvokeVoidAsync("focusOnMessage");
    }
}
```

Next we add some CSS by adding a file `ChatCompnent.razor.css` file:

```css
#chat1 .form-control ~ .form-notch div {
    pointer-events: none;
    border: 1px solid;
    border-color: #eee;
    box-sizing: border-box;
    background: transparent;
}

#chat1 .form-control ~ .form-notch .form-notch-leading {
    left: 0;
    top: 0;
    height: 100%;
    border-right: none;
    border-radius: .65rem 0 0 .65rem;
}

#chat1 .form-control ~ .form-notch .form-notch-middle {
    flex: 0 0 auto;
    max-width: calc(100% - 1rem);
    height: 100%;
    border-right: none;
    border-left: none;
}

#chat1 .form-control ~ .form-notch .form-notch-trailing {
    flex-grow: 1;
    height: 100%;
    border-left: none;
    border-radius: 0 .65rem .65rem 0;
}

#chat1 .form-control:focus ~ .form-notch .form-notch-leading {
    border-top: 0.125rem solid #39c0ed;
    border-bottom: 0.125rem solid #39c0ed;
    border-left: 0.125rem solid #39c0ed;
}

#chat1 .form-control:focus ~ .form-notch .form-notch-leading,
#chat1 .form-control.active ~ .form-notch .form-notch-leading {
    border-right: none;
    transition: all 0.2s linear;
}

#chat1 .form-control:focus ~ .form-notch .form-notch-middle {
    border-bottom: 0.125rem solid;
    border-color: #39c0ed;
}

#chat1 .form-control:focus ~ .form-notch .form-notch-middle,
#chat1 .form-control.active ~ .form-notch .form-notch-middle {
    border-top: none;
    border-right: none;
    border-left: none;
    transition: all 0.2s linear;
}

#chat1 .form-control:focus ~ .form-notch .form-notch-trailing {
    border-top: 0.125rem solid #39c0ed;
    border-bottom: 0.125rem solid #39c0ed;
    border-right: 0.125rem solid #39c0ed;
}

#chat1 .form-control:focus ~ .form-notch .form-notch-trailing,
#chat1 .form-control.active ~ .form-notch .form-notch-trailing {
    border-left: none;
    transition: all 0.2s linear;
}

#chat1 .form-control:focus ~ .form-label {
    color: #39c0ed;
}

#chat1 .form-control ~ .form-label {
    color: #bfbfbf;
}

.avatarSticky {
    position: sticky;
    position: -webkit-sticky;
    width: 45px;
    height: 100%;
    top: 10px;
}

::-webkit-scrollbar {
    width: 5px;
}

/* Track */
::-webkit-scrollbar-track {
    background: #eee;
}

/* Handle */
::-webkit-scrollbar-thumb {
    background: #888;
}

    /* Handle on hover */
    ::-webkit-scrollbar-thumb:hover {
        background: #555;
    }

.chat-btn {
    position: absolute;
    right: 14px;
    bottom: 30px;
    cursor: pointer
}

    .chat-btn .close {
        display: none
    }

    .chat-btn i {
        transition: all 0.9s ease
    }

#check:checked ~ .chat-btn i {
    display: block;
    pointer-events: auto;
    transform: rotate(180deg)
}

#check:checked ~ .chat-btn .comment {
    display: none
}

.chat-btn i {
    font-size: 22px;
    color: #fff !important
}

.chat-btn {
    width: 50px;
    height: 50px;
    display: flex;
    justify-content: center;
    align-items: center;
    border-radius: 50px;
    background-color: #0CCAF0;
    color: #fff;
    font-size: 22px;
    border: none
}

.wrapper {
    position: absolute;
    right: 20px;
    bottom: 100px;
    width: 450px;
    background-color: #fff;
    border-radius: 5px;
    opacity: 0;
    transition: all 0.4s
}

#check:checked ~ .wrapper {
    opacity: 1
}

#check {
    display: none !important
}
```

Let's add the chatbot so that it's available on all pages. Open the `App.razor` file and add the following:

```razor
    <Routes />
    <ChatBotComponent></ChatBotComponent>
```

[<-- Back](./GettingStarted.md)
