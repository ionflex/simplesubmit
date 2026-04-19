using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleSubmit.Api.Identity;
using SimpleSubmit.Api.Storage;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<ISubmitterIdentity, CookieSubmitterIdentity>();
builder.Services.AddSingleton<ISuggestionStore, InMemorySuggestionStore>();

builder.Build().Run();
