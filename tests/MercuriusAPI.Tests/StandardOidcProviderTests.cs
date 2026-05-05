using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Auth.Module.Models;
using Auth.Module.Services.External;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Mercurius.LAN.API.Tests;

public class StandardOidcProviderTests
{
    [Fact]
    public async Task CreateAuthorizationResponseAsync_UsesDiscoveryAndConfiguredParameters()
    {
        var provider = CreateProvider();
        var state = new OidcStateEntry
        {
            Provider = "google",
            State = "state-1",
            Nonce = "nonce-1",
            CodeVerifier = "verifier-1",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        };

        var response = await provider.CreateAuthorizationResponseAsync(state);
        var query = QueryHelpers.ParseQuery(new Uri(response.AuthorizationUrl).Query);

        Assert.StartsWith("https://issuer.example.com/authorize", response.AuthorizationUrl, StringComparison.Ordinal);
        Assert.Equal("client-id", query["client_id"]);
        Assert.Equal("https://localhost/callback", query["redirect_uri"]);
        Assert.Equal("openid email profile", query["scope"]);
        Assert.Equal("state-1", query["state"]);
        Assert.Equal("nonce-1", query["nonce"]);
        Assert.Equal("offline", query["access_type"]);
        Assert.Equal("consent", query["prompt"]);
        Assert.True(query.ContainsKey("code_challenge"));
        Assert.Equal("S256", query["code_challenge_method"]);
    }

    [Fact]
    public async Task ExchangeCodeAsync_PostsAuthorizationCodeToTokenEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        var provider = CreateProvider(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new OidcTokenResponse { IdToken = "id-token" })
            };
        }));

        var response = await provider.ExchangeCodeAsync("auth-code", "code-verifier");

        Assert.Equal("id-token", response.IdToken);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("https://issuer.example.com/token", capturedRequest.RequestUri!.ToString());

        var formBody = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("code=auth-code", formBody, StringComparison.Ordinal);
        Assert.Contains("code_verifier=code-verifier", formBody, StringComparison.Ordinal);
        Assert.Contains("client_id=client-id", formBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateAndExtractClaimsAsync_ReturnsExpectedClaims()
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-signing-key-1234567890"));
        var provider = CreateProvider(signingKey: signingKey);
        var nonce = "nonce-123";

        var token = CreateIdToken(signingKey, nonce);

        var userInfo = await provider.ValidateAndExtractClaimsAsync(token, nonce);

        Assert.Equal("subject-1", userInfo.Subject);
        Assert.Equal("user@example.com", userInfo.Email);
        Assert.True(userInfo.EmailVerified);
        Assert.Equal("Jane", userInfo.GivenName);
        Assert.Equal("Doe", userInfo.FamilyName);
    }

    private static StandardOidcProvider CreateProvider(StubHttpMessageHandler? handler = null, SecurityKey? signingKey = null)
    {
        var key = signingKey ?? new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-signing-key-1234567890"));
        var configuration = new OpenIdConnectConfiguration
        {
            Issuer = "https://issuer.example.com",
            AuthorizationEndpoint = "https://issuer.example.com/authorize",
            TokenEndpoint = "https://issuer.example.com/token"
        };
        configuration.SigningKeys.Add(key);

        var httpClient = new HttpClient(handler ?? new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
        {
            DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
        };

        return new StandardOidcProvider(
            "google",
            new OidcProviderOptions
            {
                ClientId = "client-id",
                ClientSecret = "client-secret",
                RedirectUri = "https://localhost/callback",
                MetadataAddress = "https://issuer.example.com/.well-known/openid-configuration",
                ValidIssuers = ["https://issuer.example.com"],
                AdditionalAuthorizationParameters = new Dictionary<string, string>
                {
                    ["access_type"] = "offline",
                    ["prompt"] = "consent"
                }
            },
            httpClient,
            new StaticConfigurationManager(configuration));
    }

    private static string CreateIdToken(SecurityKey signingKey, string nonce)
    {
        var token = new JwtSecurityToken(
            issuer: "https://issuer.example.com",
            audience: "client-id",
            claims:
            [
                new Claim("sub", "subject-1"),
                new Claim("email", "user@example.com"),
                new Claim("email_verified", "true"),
                new Claim("given_name", "Jane"),
                new Claim("family_name", "Doe"),
                new Claim("nonce", nonce)
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class StaticConfigurationManager(OpenIdConnectConfiguration configuration) : IConfigurationManager<OpenIdConnectConfiguration>
    {
        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) => Task.FromResult(configuration);

        public void RequestRefresh()
        {
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
