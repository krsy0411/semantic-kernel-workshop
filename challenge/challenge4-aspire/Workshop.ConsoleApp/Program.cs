using System.ClientModel;

using Azure;
using Azure.AI.OpenAI;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

using OpenAI;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Workshop.ConsoleApp;

var config = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddUserSecrets<Program>()
                 .Build();

var dashboardEndpoint = config["Aspire:Dashboard:Endpoint"]!;
var resourceBuilder = ResourceBuilder.CreateDefault()
                                     .AddService("SKOpenTelemetry");

// Enable model diagnostics with sensitive data.
AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

using var traceProvider = Sdk.CreateTracerProviderBuilder()
                             .SetResourceBuilder(resourceBuilder)
                             .AddSource("Microsoft.SemanticKernel*")
                             .AddConsoleExporter()
                             .AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint))
                             .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
                             .SetResourceBuilder(resourceBuilder)
                             .AddMeter("Microsoft.SemanticKernel*")
                             .AddConsoleExporter()
                             .AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint))
                             .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add OpenTelemetry as a logging provider
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddConsoleExporter();
        options.AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint));
        // Format log messages. This is default to false.
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var builder = Kernel.CreateBuilder();

builder.Services.AddSingleton(loggerFactory);

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

// 👇👇👇 스토리텔러 에이전트 실행하고 싶으면 주석 제거
await AgentActions.InvokeStoryTellerAgentAsync(kernel);
// 👆👆👆 스토리텔러 에이전트 실행하고 싶으면 주석 제거

// 👇👇👇 식당 호스트 에이전트 실행하고 싶으면 주석 제거
// await AgentActions.InvokeRestaurantAgentAsync(kernel);
// 👆👆👆 식당 호스트 에이전트 실행하고 싶으면 주석 제거

// 👇👇👇 에이전트간 협업 과정 실행하고 싶으면 주석 제거
// await AgentActions.InvokeAgentCollaborationsAsync(kernel);
// 👆👆👆 에이전트간 협업 과정 실행하고 싶으면 주석 제거