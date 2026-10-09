using System.Text;
using Mercurius.Modules.Identity;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Identity.Services;
using Mercurius.Modules.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Platform.Extensions;

namespace Mercurius.Api.Tests;

public class AdminUserPickerPagingEndpointTests
{
    private const string AdminPickerRoute = "v{version:apiVersion}/lan/users/admins";

    [Theory]
    [InlineData("", 1, 20, "")]
    [InlineData("?page=2&pageSize=51", 2, 50, "")]
    [InlineData("?query=%20ALPHA%20&page=3", 3, 20, "alpha")]
    public async Task AdminPicker_NormalizesPagingAndPreservesQueryFiltering(
        string queryString,
        int expectedPage,
        int expectedPageSize,
        string expectedQuery)
    {
        var identityModule = new RecordingIdentityModule();
        await using var app = CreateApp(identityModule);

        var response = await InvokeGetAsync(app, queryString);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal((expectedPage, expectedPageSize), identityModule.LastPaging);
        Assert.Equal(expectedQuery, identityModule.LastQuery);
        Assert.Equal(1, identityModule.CallCount);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=-1")]
    public async Task AdminPicker_RejectsInvalidPageBeforeIdentityInvocation(string queryString)
    {
        var identityModule = new RecordingIdentityModule();
        await using var app = CreateApp(identityModule);

        var response = await InvokeGetAsync(app, queryString);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal(0, identityModule.CallCount);
    }

    private static WebApplication CreateApp(IIdentityModule identityModule)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddHttpConventions();
        builder.Services.AddSingleton<IIdentityModule>(identityModule);
        builder.Services.AddScoped<IUserService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.MapIdentityModule();
        return app;
    }

    private static async Task<(int StatusCode, string Body)> InvokeGetAsync(WebApplication app, string queryString)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == AdminPickerRoute);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString(queryString);
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return (context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private sealed class RecordingIdentityModule : IIdentityModule
    {
        public int CallCount { get; private set; }
        public string? LastQuery { get; private set; }
        public (int Page, int PageSize) LastPaging { get; private set; }

        public Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PublicUserProfileSummary?> GetPublicProfileByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PublicUserProfileSummary?> GetPublicProfileByIdAsync(UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<UserId, string>> GetPublicUsernamesByIdsAsync(IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PublicUserSearchDocument>> GetPublicUserSearchDocumentsPageAsync(UserId? afterId, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<UserProfileSummary>> GetAdminUsersAsync(
            string normalizedQuery,
            int pageSize,
            CancellationToken cancellationToken = default,
            int page = 1)
        {
            return RecordAdminLookup(normalizedQuery, pageSize, page);
        }

        private Task<IReadOnlyList<UserProfileSummary>> RecordAdminLookup(string normalizedQuery, int pageSize, int page)
        {
            CallCount++;
            LastQuery = normalizedQuery;
            LastPaging = (page, pageSize);
            return Task.FromResult<IReadOnlyList<UserProfileSummary>>([]);
        }
    }
}
