using Discord.Webhook;

namespace DiscordEmailBridge.Configuration
{
    internal interface IWebhookClientProvider
    {
        DiscordWebhookClient? GetClient(string address);
    }
}