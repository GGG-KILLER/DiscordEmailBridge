using Microsoft.Extensions.Logging;
using MimeKit;

namespace DiscordEmailBridge
{
    internal static partial class LoggerExtensions
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Receiving message...")]
        public static partial void LogReceivingMessage(
            this ILogger logger);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Error,
            Message = "Received email from more than one sender: {From}")]
        public static partial void LogEmailFromMoreThanOneSender(
            this ILogger logger,
            InternetAddressList from);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Error,
            Message = "Received email from invalid sender: {From}")]
        public static partial void LogEmailFromInvalidSender(
            this ILogger logger,
            InternetAddress from);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Warning,
            Message = "Unconfigured recipient address: {To}")]
        public static partial void LogUnconfiguredRecipient(
            this ILogger logger,
            MailboxAddress to);

        [LoggerMessage(
            EventId = 5,
            Level = LogLevel.Warning,
            Message = "Email contains message parts (email attachments) that aren't supported.")]
        public static partial void LogMessagePartFound(
            this ILogger logger);
    }
}
