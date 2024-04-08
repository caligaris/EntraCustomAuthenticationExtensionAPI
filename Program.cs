using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Company.Function.Middleware;
using Company.Function;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Core;

var host = new HostBuilder()
    // .ConfigureFunctionsWebApplication(builder => {
    //     builder.UseMiddleware<AuthenticationMiddleware>();
    // })
    .ConfigureFunctionsWorkerDefaults(builder => {
        builder.UseWhen<AuthenticationMiddleware>( 
            (context) => {
                //Check if request has Authorization header
                var req = context.GetHttpRequestDataAsync().AsTask().Result;
                if (req.Headers.TryGetValues("Authorization", out var values))
                {
                    return true;
                }
                return false;
            }
        );
    })
    .ConfigureServices(services => {
        services.AddSingleton<ClaimsCache>(
            s => new ClaimsCache(s.GetRequiredService<ILogger<ClaimsCache>>(), s.GetRequiredService<IConfiguration>()));
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
