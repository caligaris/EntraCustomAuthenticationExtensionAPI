// class that queries an Azure SQL database and stores the resoults in a dictionary for use in the CustomAuthenticationAPI
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Company.Function.Models;

namespace Company.Function
{
    public class ClaimsCache
    {
        private readonly ILogger<ClaimsCache> _logger;
        private readonly string _connectionString;
        private readonly Dictionary<string, List<CustomUserClaims>> _claims;

        public ClaimsCache(ILogger<ClaimsCache> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration["SqlConnectionString"];
            _claims = [];

        }

        public async Task<List<CustomUserClaims>> GetClaim(string UserPrincipalName)
        {
            if (_claims.TryGetValue(UserPrincipalName, out var value))
            {
                return value;
            }

            value = await QueryClaim(UserPrincipalName);
            _claims[UserPrincipalName] = value;
            return value;
        }

        private async Task<List<CustomUserClaims>> QueryClaim(string UserPrincipalName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT UserPrincipalName, ClaimName, ClaimValue FROM CustomUserClaims WHERE UserPrincipalName = @UserPrincipalName";
            command.Parameters.AddWithValue("@UserPrincipalName", UserPrincipalName);

            var results = new List<CustomUserClaims>();
            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new CustomUserClaims() {
                    UserPrincipalName = reader.GetString(0),
                    ClaimName = reader.GetString(1),
                    ClaimValue = reader.GetString(2)
                });
            }

            return results;
        }

        // Get all claims and store them in _claims object
        public async Task LoadClaims()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT UserPrincipalName, ClaimName, ClaimValue FROM CustomUserClaims";

            var reader = await command.ExecuteReaderAsync();
            _claims.Clear();
            while (await reader.ReadAsync())
            {
                var UserPrincipalName = reader.GetString(0);
                if (!_claims.TryGetValue(UserPrincipalName, out var value))
                {
                    value = new List<CustomUserClaims>();
                    _claims[UserPrincipalName] = value;
                }

                value.Add(new CustomUserClaims() {
                    UserPrincipalName = UserPrincipalName,
                    ClaimName = reader.GetString(1),
                    ClaimValue = reader.GetString(2)
                });
            }
        }
    }
}