using ModelingEvolution.NetworkManager.App.Components;
using ModelingEvolution.NetworkManager.Contracts;
using ModelingEvolution.NetworkManager.Services;
using ModelingEvolution.NetworkManager.Ui;
using MicroPlumberd.Services;
using MudBlazor.Services;
using System.Diagnostics;
using EventStore.Client;
using MicroPlumberd.Encryption;

namespace ModelingEvolution.NetworkManager.App;

public class Program
{
    private static async Task CheckDebugger(string[] args)
    {
        if (Environment.GetEnvironmentVariable("DEBUG") != null || args.Contains("--debug"))
        {
            if (args.Contains("--disableDebug")) return;

            Console.WriteLine("Waiting for debugger to attach.");
            while (!Debugger.IsAttached)
            {
                await Task.Delay(500);
            }
            Console.WriteLine("Application is attached to debugger.");
        }
    }

    public static async Task Main(string[] args)
    {
        await CheckDebugger(args);
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddEncryption();
        bool disableUi = args.Contains("--disableUi");
        if (disableUi) Console.WriteLine("UI is disabled.");

        services.AddNetworkManagerEnvironment()
            .AddPlumberd(
                sp => EventStoreClientSettings.Create(sp.GetRequiredService<IConfiguration>().GetValue<string>("EventStore")!),
                (sp, c) => c.EnableEncryption());

        if (!disableUi)
        {
            services.AddMudServices()
                .AddNetworkManagerUi()
                .AddRazorComponents()
                .AddInteractiveServerComponents();
        }

        if (Environment.OSVersion.Platform == PlatformID.Unix)
            services.   AddNetworkManagerServices();

        var app = builder.Build();

        if (!disableUi)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<Components.App>()
                .AddInteractiveServerRenderMode();
        }

        await app.RunAsync();
    }
}
