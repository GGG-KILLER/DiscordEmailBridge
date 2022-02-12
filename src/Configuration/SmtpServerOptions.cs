using System.ComponentModel.DataAnnotations;

namespace DiscordEmailBridge.Configuration
{
    internal class SmtpServerOptions
    {
        [RegularExpression(@"[\w\.\-]+")]
        public string? ServerName { get; set; }

        public int[]? Ports { get; set; }
    }
}
