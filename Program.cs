 using Azure;
 using Azure.AI.OpenAI;
 using Microsoft.CognitiveServices.Speech;
 using Microsoft.CognitiveServices.Speech.Audio;
 using Microsoft.Extensions.Configuration;

 internal class Program
 {
    private static IConfiguration? _configuration;

    private static void InitializeConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
    }

    private static void ValidateConfiguration()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not initialized.");
        }
    }

    private static void ValidateSpeechConfig(string subscriptionKey, string region)
    {
        if (string.IsNullOrEmpty(subscriptionKey) || string.IsNullOrEmpty(region))
        {
            throw new InvalidOperationException("Subscription key or region is not configured correctly.");
        }
    }

    private static SpeechConfig CreateSpeechConfig()
    {
        ValidateConfiguration();

        var subscriptionKey = _configuration.GetSection("SubscriptionKey").Value;
        var region = _configuration.GetSection("Region").Value;

        ValidateSpeechConfig(subscriptionKey, region);

        var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
        speechConfig.SpeechRecognitionLanguage = "en-GB";
        speechConfig.SpeechSynthesisVoiceName = "en-GB-RyanNeural";

        return speechConfig;
    }

    private static AudioConfig CreateAudioConfig()
    {
        return AudioConfig.FromDefaultMicrophoneInput();
    }

    async static Task FromOpenAI(string prompt, SpeechSynthesizer speechSynthesizer)
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not initialized.");
        }

        var endpoint = _configuration.GetSection("Endpoint").Value;
        var key = _configuration.GetSection("Key").Value;

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("Endpoint or key is not configured correctly.");
        }

        var deploymentName = _configuration.GetSection("DeploymentName").Value;

        OpenAIClient client = new(new Uri(endpoint), new AzureKeyCredential(key));

        var chatCompletionOptions = new ChatCompletionsOptions()
                            {
                                DeploymentName=deploymentName,
                                MaxTokens=500,
                                Temperature=1,
                                FrequencyPenalty=0,
                                PresencePenalty=0,
                            };

        chatCompletionOptions.Messages.Add(new ChatMessage(ChatRole.System, "You farmer."));
        chatCompletionOptions.Messages.Add(new ChatMessage(ChatRole.User, prompt));

        Response<ChatCompletions> response = await client.GetChatCompletionsAsync(chatCompletionOptions);

        Console.WriteLine("Usage:");
        Console.WriteLine($"PromptTokens: {response.Value.Usage.PromptTokens}");
        Console.WriteLine($"TotalTokens: {response.Value.Usage.TotalTokens}");
        Console.WriteLine($"CompletionTokens: {response.Value.Usage.CompletionTokens}");

        Console.WriteLine("Azure OpenAI says:");
        foreach (ChatChoice choice in response.Value.Choices)
        {
            Console.WriteLine(choice.Message.Content);
            await speechSynthesizer.SpeakTextAsync(choice.Message.Content);
        }
    }

    async static Task FromMic(SpeechConfig speechConfig, AudioConfig audioConfig)
    {
        using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
        using var synthesizer = new SpeechSynthesizer(speechConfig);
        using var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

        Console.WriteLine("Speak into your microphone.");

        while (true) {
            var result = await speechRecognizer.RecognizeOnceAsync();

            switch (result.Reason) {
                case ResultReason.RecognizedSpeech:
                    if(result.Text.ToLower().Contains("stop") || 
                        result.Text.ToLower().Contains("exit")) {
                            Console.WriteLine("Azure OpenAI is stopping.");
                            return;
                    }
                    Console.WriteLine($"RECOGNIZED: Text={result.Text}");
                    await FromOpenAI(result.Text, speechSynthesizer);
                    Console.WriteLine("Speak into your microphone.");
                    break;
                case ResultReason.NoMatch:
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    break;
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error) {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    }
                    break;
            }
        }
    }

     async static Task Main(string[] args)
     {
        InitializeConfiguration();
        var speechConfig = CreateSpeechConfig();
        var audioConfig = CreateAudioConfig();

        Console.WriteLine("Azure OpenAI is listening. Say 'Stop' or 'Exit'");
        await FromMic(speechConfig, audioConfig);
     }
 }