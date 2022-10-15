using HealthChecks.UI.Client;
using HelthCheckApp1;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddSqlServer(configuration["Data:ConnectionStrings:Sql"], name: "Sql Server",
        healthQuery: "SELECT 1;", failureStatus: HealthStatus.Degraded,
        tags: new string[] { "searchengine", "sql", "sqlserver" })
    .AddRedis(configuration["Data:ConnectionStrings:Redis"], name: "Redis")
    .AddMongoDb(configuration["Data:ConnectionStrings:Mongo"], name: "Mongo")
    .AddNpgSql(configuration["Data:ConnectionStrings:Postgres"], name: "Postgres")
    .AddElasticsearch(configuration["Data:ConnectionStrings:Elastic"], name: "Elastic",
        failureStatus: HealthStatus.Unhealthy,
        tags: new string[] { "searchengine", "nosql", "elasticsearch" })
    .AddCheck<RandomHealthCheckSample>("random", tags: new string[] { "sample" });

builder.Services
    .AddHealthChecksUI(c =>
    {
        c.AddHealthCheckEndpoint("Data Bases", "/healthz");
        // Set configuration property programatically. Also them can be specified in the appsettings.json
        c.AddHealthCheckEndpoint("HTTP Api 1", "https://localhost:7014/healthz");

        // Sample endpoint to demo the use of "Predicate"
        c.AddHealthCheckEndpoint("No SQL", "/nosql");
        //
        // Set configuration property programatically. Also them can be specified in the appsettings.json
        //
        //// Configures the UI to poll for healthchecks updates every 5 seconds
        //c.SetEvaluationTimeInSeconds(5);
        ////  Only one active request will be executed at a time. Excedent requests will result in 429 (Too many requests)
        //c.SetApiMaxActiveRequests(1);
        //// Set the maximum history entries by endpoint that will be served by the UI api middleware
        //c.MaximumHistoryEntriesPerEndpoint(50);
    })
    .AddInMemoryStorage();


var app = builder.Build();

app
    // Standard AspNet Healthcheck
    .UseHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true
    })
    .UseHealthChecks("/healthz", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    })
    // Sample endpoint to demo the use of "Predicate"
    .UseHealthChecks("/nosql", new HealthCheckOptions
    {
        Predicate = p => p.Tags.Contains("nosql"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    })

    // Use of Prometheus
    // .UseHealthChecksPrometheusExporter("/metrics")
    .UseRouting()
    .UseEndpoints(config =>
    {
        config.MapHealthChecksUI();
        config.MapDefaultControllerRoute();
        // Change the default theme
        // config.MapHealthChecksUI(c => { c.AddCustomStylesheet("dotnet.css"); });
    });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
