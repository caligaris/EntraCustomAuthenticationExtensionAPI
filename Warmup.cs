using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Company.Function
{
    public class Warmup
    {
        private ClaimsCache _claimsCache;

        public Warmup(ClaimsCache claimsCache)
        {
            _claimsCache = claimsCache;
        }

        [Function(nameof(Warmup))]
        public void Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
           _claimsCache.LoadClaims().Wait();
        }
    }
}