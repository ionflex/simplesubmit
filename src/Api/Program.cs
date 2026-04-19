using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
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
builder.Services.AddSingleton<IAdminAuthorization, SwaAdminAuthorization>();

var storageConnection = builder.Configuration["SUGGESTIONS_STORAGE"];
if (!string.IsNullOrWhiteSpace(storageConnection))
{
    builder.Services.AddSingleton(_ => new TableServiceClient(storageConnection));
    builder.Services.AddSingleton<ISuggestionStore, TableSuggestionStore>();
}
else
{
    builder.Services.AddSingleton<ISuggestionStore, InMemorySuggestionStore>();
}

builder.Build().Run();
