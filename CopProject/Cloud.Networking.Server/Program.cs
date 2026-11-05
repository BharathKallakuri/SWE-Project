// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        ConfigureEndpoints(app);
        return app;
    }

    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        return WebApplication.CreateBuilder(args);
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR();
    }

    public static void ConfigureEndpoints(WebApplication app)
    {
        app.MapHub<MessageHub>("/hub");
    }
}
