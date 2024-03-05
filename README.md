# core-ai-techspike-speech

Example C#/.NET console app demostrating the use of Azure AI Speech with Azure OpenAI integration.

The console app converts speech to text, uses the text to produce a completion from Azure OpenAI and then uses text to speech to add converation to the app.

https://learn.microsoft.com/en-us/azure/ai-services/speech-service/
https://learn.microsoft.com/en-us/azure/ai-services/openai/

### Prerequisites
.NET 7.0 SDK

### Installation

1. Clone the repository:

```
git clone https://github.com/DEFRA/core-ai-techspike-speech.git
```

2. Navigate to the project directory:

```
cd core-ai-techspike-speech
```

### Configuration
Create a appsettings.json file in the root of the project directory with the following structure:

```
{
  "SpeechToText": {
    "Endpoint": "<your_endpoint>",
    "Key": "<your_key>"
  }
}
```

Replace <your_endpoint> and <your_key> with your Azure Speech to Text service endpoint and key respectively.

### Build
To build the project, run the following command in the terminal:

```
dotnet build
```

### Run
To run the project, use the following command:

```
dotnet run
```

The application will start and listen for your voice commands. Say 'Stop' or 'Exit' to stop the application.

### Note
This application uses Azure Speech to Text service and Azure OpenAI. Make sure you have the necessary subscriptions and keys for these services