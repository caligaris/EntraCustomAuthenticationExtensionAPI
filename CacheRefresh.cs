using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace Company.Function {
    public class CacheRefresh
    {
        private ClaimsCache _claimsCache;

        public CacheRefresh(ClaimsCache claimsCache)
        {
            _claimsCache = claimsCache;
        }

        [Function("StartupInit")]
        public void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            // Your initialization logic here
            _claimsCache.LoadClaims().Wait();
            log.LogInformation("Cache Refreshed Successfully!");

        }
    }
}