// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Components;
using Cloud.Networking.Server.Models;
using Cloud.Networking.Server.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace Cloud.Networking.Server;

public class Program
{
    /* Testing a Main function is quite difficult unless you seperate
    * business logic into something that can be tested (like a function or a class)*/

    public static void Main(string[] args)
    {
        WebApplication app = CreateApplication(args);
        app.Run();
    }

    public static WebApplication CreateApplication(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services);
        WebApplication app = builder.Build();
        ConfigureMiddlewares(app);
        ConfigureEndpoints(app);
        return app;
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ServerMonitor>(); /*only one instance of ServerMonitor will exist*/
        services.AddScoped<DashboardViewModel>(); /*one instance per user circuit (per browser tab)*/
        services.AddSignalR().AddHubOptions<MessageHub>(options => options.AddFilter<MonitorHubFilter>()); /* this filter connects ServerMonitor and MessageHub */
        services.AddRazorComponents().AddInteractiveServerComponents(); /* needed for interactivity in server components */
    }

    public static void ConfigureMiddlewares(WebApplication app)
    {
        app.UseHttpsRedirection(); /* http to https */
        app.UseStaticFiles(); /* serves from wwwroot */
        app.UseRouting(); /* matches request to an endpoint (point of knowledge) */
        app.UseAntiforgery(); /* protection from cross-site request forgery */
    }

    public static void ConfigureEndpoints(WebApplication app)
    {
        app.MapGet("/health", () => "ok");
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.MapHub<MessageHub>("/hub"); /* maps MessageHub to /hub */
    }
}
