using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Company.Function.Middleware
{
    public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly JwtSecurityTokenHandler _tokenValidator;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
        private readonly ILogger _logger;

        public AuthenticationMiddleware(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AuthenticationMiddleware>();
            var tenantId = configuration["AuthenticationTenantId"];
            var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
            var audience = configuration["AuthenticationClientId"];
            _tokenValidator = new JwtSecurityTokenHandler();
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = audience

            };
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{authority}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());
        }

        public async Task Invoke(
            FunctionContext context,
            FunctionExecutionDelegate next)
        {
            if (!TryGetTokenFromHeaders(context, out var token))
            {
                // Unable to get token from headers
                await context.SetHttpResponseStatusCode(HttpStatusCode.Unauthorized);
                return;
            }

            if (!_tokenValidator.CanReadToken(token))
            {
                // Token is malformed
                await context.SetHttpResponseStatusCode(HttpStatusCode.Unauthorized);
                return;
            }

            // Validate Claim named "azp" is equals to a string constant
            var jwtToken = _tokenValidator.ReadJwtToken(token);
            // Check if the token is coming from Entra Custom Authentication Extension
            // as stated in https://learn.microsoft.com/en-us/entra/identity-platform/custom-extension-overview#protect-your-rest-api
            if (jwtToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value != "99045fe1-7639-4a75-9d4a-577b6ca3810f")
            {
                // Token was not requested by Entra Custom Authentication Extension
                await context.SetHttpResponseStatusCode(HttpStatusCode.Unauthorized);
                return;
            }

            // Get OpenID Connect metadata
            var validationParameters = _tokenValidationParameters.Clone();
            var openIdConfig = await _configurationManager.GetConfigurationAsync(default);
            validationParameters.ValidIssuer = openIdConfig.Issuer;
            validationParameters.IssuerSigningKeys = openIdConfig.SigningKeys;

            try
            {
                // Validate token
                var principal = _tokenValidator.ValidateToken(
                        token, validationParameters, out _);

                // Set principal + token in Features collection
                // They can be accessed from here later in the call chain
                context.Features.Set(new JwtPrincipalFeature(principal, token));

                await next(context);
            }
            catch (SecurityTokenException ex)
            {
                // Token is not valid (expired etc.)
                _logger.LogWarning(ex, "Token validation failed");
                await context.SetHttpResponseStatusCode(HttpStatusCode.Unauthorized);
                return;
            }
        }

        private static bool TryGetTokenFromHeaders(FunctionContext context, out string token)
        {
            token = "";
            // HTTP headers are in the binding context as a JSON object
            // The first checks ensure that we have the JSON string
            if (!context.BindingContext.BindingData.TryGetValue("Headers", out var headersObj))
            {
                return false;
            }

            if (headersObj is not string headersStr)
            {
                return false;
            }

            // Deserialize headers from JSON
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersStr);
            var normalizedKeyHeaders = headers?.ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value);

            if (normalizedKeyHeaders == null)
                // No headers present
                return false;
            
            if (!normalizedKeyHeaders.TryGetValue("authorization", out var authHeaderValue))
                // No Authorization header present
                return false;

            if (!authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                // Scheme is not Bearer
                return false;

            token = authHeaderValue.Substring("Bearer ".Length).Trim();
            return true;
        }
    }
}