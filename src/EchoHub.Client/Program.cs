using EchoHub.Client;
using EchoHub.Client.Config;
using EchoHub.Client.Themes;
using Microsoft.Extensions.Configuration;
using Serilog;
using Terminal.Gui.App;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

Log.Information("EchoHub client starting");

try
{
    var config = ConfigManager.Load();
    Log.Information("Configuration loaded, active theme: {Theme}", config.ActiveTheme);

    var app = Application.Create().Init();

    var theme = ThemeManager.GetTheme(config.ActiveTheme);
    ThemeManager.ApplyTheme(theme);

    using var orchestrator = new AppOrchestrator(app, config);

    app.Run(orchestrator.MainWindow);
    app.Dispose();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EchoHub client crashed");
    throw;
}
finally
{
    Log.Information("EchoHub client shutting down");
    Log.CloseAndFlush();
}
