using System.ClientModel;

using Azure;
using Azure.AI.OpenAI;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

using OpenAI;

var config = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddUserSecrets<Program>()
                 .Build();

var builder = Kernel.CreateBuilder();
if (string.IsNullOrWhiteSpace(config["Azure:OpenAI:Endpoint"]!) == false)
{
    var client = new AzureOpenAIClient(
        new Uri(config["Azure:OpenAI:Endpoint"]!),
        new AzureKeyCredential(config["Azure:OpenAI:ApiKey"]!));

    builder.AddAzureOpenAIChatCompletion(
                deploymentName: config["Azure:OpenAI:DeploymentName"]!,
                azureOpenAIClient: client);
}
else
{
    var client = new OpenAIClient(
        credential: new ApiKeyCredential(config["GitHub:Models:AccessToken"]!),
        options: new OpenAIClientOptions { Endpoint = new Uri(config["GitHub:Models:Endpoint"]!) });

    builder.AddOpenAIChatCompletion(
                modelId: config["GitHub:Models:ModelId"]!,
                openAIClient: client);
}
var kernel = builder.Build();

var background = "나는 맛있는 음식과 야외활동을 좋아해.";
var plugins = kernel.CreatePluginFromPromptDirectory(Path.Combine(AppContext.BaseDirectory, "../../..", "Plugins"));

var input = default(string);
var message = default(string);
while (true)
{
    Console.WriteLine("여러분이 방문 중인 도시를 알려주세요.");
    Console.Write("유저: ");
    input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    Console.Write("어시스턴트: ");

    var arguments = new KernelArguments()
    {
        { "city", input },
        { "background", background }
    };


    var response = kernel.InvokeStreamingAsync(plugins["TravelAgent"], arguments);
    await foreach (var content in response)
    {
        await Task.Delay(20);
        message += content;
        Console.Write(content);
    }
    Console.WriteLine();

    Console.WriteLine();
}
