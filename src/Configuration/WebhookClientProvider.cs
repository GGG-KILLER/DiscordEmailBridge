using System.Collections.Immutable;
using System.Net.Mail;
using Discord.Webhook;
using Microsoft.Extensions.Configuration;

namespace DiscordEmailBridge.Configuration
{
    internal class WebhookClientProvider : IWebhookClientProvider
    {
        private readonly ImmutableDictionary<string, DiscordWebhookClient> _webhooks;

        public WebhookClientProvider(IConfiguration configuration)
        {
            var webhooks = ImmutableDictionary.CreateBuilder<string, DiscordWebhookClient>(StringComparer.OrdinalIgnoreCase);
            foreach (var subSection in configuration.GetRequiredSection(Constants.ConfigurationSections.WebhookOptionsSet)
                .GetChildren())
            {
                if (subSection.Key != "default"
                    && !MailAddress.TryCreate(subSection.Key, out _))
                {
                    throw new InvalidOperationException($"'{subSection.Key}' is not a valid email address.");
                }
                webhooks.Add(subSection.Key, new DiscordWebhookClient(subSection.Value));
            }
            _webhooks = webhooks.ToImmutable();
        }

        public DiscordWebhookClient? GetClient(string address)
        {
            if (!_webhooks.TryGetValue(address, out var client)
                && !_webhooks.TryGetValue("default", out client))
            {
                return null;
            }

            return client;
        }
    }
}
