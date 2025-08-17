using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using DocxTemplate.CLI.Commands;
using DocxTemplate.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// Setup dependency injection
var services = new ServiceCollection();
services.AddInfrastructure(configuration);
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

var serviceProvider = services.BuildServiceProvider();

// Create root command
var rootCommand = new RootCommand("DocxTemplate CLI - Process Word document templates with placeholder replacement");

// Create middleware to inject service provider
var middleware = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .AddMiddleware(async (context, next) =>
    {
        context.BindingContext.AddService(typeof(IServiceProvider), _ => serviceProvider);
        await next(context);
    })
    .Build();

// Add commands
rootCommand.AddCommand(new ListSetsCommand());
rootCommand.AddCommand(new DiscoverCommand());
rootCommand.AddCommand(new ScanCommand());
rootCommand.AddCommand(new CopyCommand());

// Execute
return await middleware.InvokeAsync(args);