// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Cloud.Networking.Server.Test;

[TestClass]
public class TestProgram
{
    private static bool HasMessageHubRoute(IEndpointRouteBuilder routes)
    {
        return routes.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Any(endpoint => endpoint.RoutePattern.RawText == "/hub"
                && endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType == typeof(MessageHub));
    }

    [TestMethod]
    public void ConfigureServicesMakesTheHubResolvable()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Program.ConfigureServices(builder.Services);
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        Assert.IsNotNull(provider.GetService<HubLifetimeManager<MessageHub>>());
    }

    [TestMethod]
    public async Task ConfigureEndpointsProperlyMapsHub()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Program.ConfigureServices(builder.Services);
        await using WebApplication app = builder.Build();
        Program.ConfigureEndpoints(app);
        Assert.IsTrue(HasMessageHubRoute(app), "Expected MessageHub to be mapped at /hub.");
    }

    [TestMethod]
    public void CreateBuilderWorksProperly()
    {
        WebApplicationBuilder builder = Program.CreateBuilder(["--environment", "Staging"]);
        Assert.AreEqual("Staging", builder.Environment.EnvironmentName);
    }

    [TestMethod]
    public async Task CreateApplicationWorksProperly()
    {
        await using WebApplication app = Program.CreateApplication(["--environment", "Staging"]);

        Assert.IsNotNull(app.Services.GetService<HubLifetimeManager<MessageHub>>());
        Assert.IsTrue(HasMessageHubRoute(app), "Expected MessageHub to be mapped at /hub.");
        Assert.AreEqual("Staging", app.Environment.EnvironmentName);
    }
}
