// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using Company.Function.Models;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Configuration;

namespace Company.Function
{
    public class CustomAuthenticationAPI
    {
        private readonly ILogger<CustomAuthenticationAPI> _logger;
        private readonly ClaimsCache _claimsCache;
        private readonly IConfiguration _configuration;

        public CustomAuthenticationAPI(
            ILogger<CustomAuthenticationAPI> logger,
            ClaimsCache claimsCache,
            IConfiguration configuration)
        {
            _logger = logger;
            _claimsCache = claimsCache;
            _configuration = configuration;
        }

        [Function("CustomAuthenticationAPI")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // deserialize the request body using system.text.json
            var data = JsonSerializer.Deserialize<JsonNode>(requestBody);

            string correlationId = data?["data"]?["authenticationContext"]?["correlationId"]?.ToString() ?? string.Empty;
            string userPrincipalName = data?["data"]?["authenticationContext"]?["user"]?["userPrincipalName"]?.ToString() ?? string.Empty;

            //var appClaims = await _claimsCache.GetClaim(_configuration["AuthenticationClientId"]);
            var userClaims = await _claimsCache.GetClaim(userPrincipalName);

            var jsonResponse = new JsonObject
            {
                ["@odata.type"] = "microsoft.graph.onTokenIssuanceStartResponseData",
                ["actions"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["@odata.type"] = "microsoft.graph.tokenIssuanceStart.provideClaimsForToken",
                        ["claims"] = new JsonObject
                        {
                            ["CorrelationId"] = correlationId,
                            // ["ApiVersion"] = "1.0.0",
                            // ["CustomRoles"] = new JsonArray { "Admin", "User" }
                        }
                    }
                }
            };

            userClaims.ForEach(c => jsonResponse["actions"][0]["claims"][c.ClaimName.Trim()] = c.ClaimValue.Trim());
            var root = new JsonObject { ["data"] = jsonResponse };


            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.Body = new MemoryStream(Encoding.UTF8.GetBytes(root.ToJsonString()));

            _logger.LogInformation($" -- response: {root.ToJsonString()}");
            return response;
        }
    }
}
