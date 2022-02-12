using DiscordEmailBridge;
using DiscordEmailBridge.Configuration;
using DiscordEmailBridge.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Storage;

var host = Host.CreateDefaultBuilder(args)
                 .ConfigureHostConfiguration(ConfigureHost)
                 .ConfigureServices(ConfigureServices)
                 .Build();

await host.StartAsync();

static void ConfigureHost(IConfigurationBuilder obj)
{
    obj.AddEnvironmentVariables(prefix: "DEB_");
}

static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
{
    services.AddLogging();
    services.AddOptions<SmtpServerOptions>()
            .Bind(context.Configuration.GetSection(Constants.ConfigurationSections.SmtpServerOptions))
            .ValidateOnStart();

    services.AddSingleton(static provider =>
    {
        var options = provider.GetRequiredService<IOptions<SmtpServerOptions>>().Value;
        return new SmtpServerOptionsBuilder()
            .ServerName(options.ServerName)
            .Port(options.Ports)
            .Build();
    });
    services.AddSingleton<SmtpServer.SmtpServer>();
    services.AddSingleton<IMessageStore, DiscordEmailBridge.MessageStore>();
    services.AddSingleton(MailboxFilter.Default);
    services.AddSingleton(UserAuthenticator.Default);
    services.AddSingleton<IWebhookClientProvider, WebhookClientProvider>();

    services.AddHostedService<SmtpServerService>();
}
