using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Webhook;
using DiscordEmailBridge.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace DiscordEmailBridge
{
    internal class MessageStore : IMessageStore
    {
        private static readonly ParserOptions s_parseOptions = new()
        {
            AllowAddressesWithoutDomain = true,
            CharsetEncoding = Encoding.UTF8,
            MaxAddressGroupDepth = 0,
            // Arbitrary number I've set without ever reading a MIME
            // message in my entire life.
            MaxMimeDepth = 2
        };
        private readonly IWebhookClientProvider _webhookClientProvider;
        private readonly ILogger<MessageStore> _logger;

        public MessageStore(IWebhookClientProvider webhookClientProvider, ILogger<MessageStore> logger)
        {
            _webhookClientProvider = webhookClientProvider ?? throw new ArgumentNullException(nameof(webhookClientProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SmtpResponse> SaveAsync(
            ISessionContext context,
            IMessageTransaction transaction,
            ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken)
        {
            _logger.LogReceivingMessage();
            await using MemoryStream? stream = new();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
            {
                await stream.WriteAsync(memory, cancellationToken);
            }

            stream.Position = 0;
            var message = await MimeMessage.LoadAsync(s_parseOptions, stream, cancellationToken);

            using var _1 = _logger.BeginScope(
                "Message '{Subject}' from user {User}",
                message.Subject,
                context.Authentication.User ?? "unauthenticated");

            if (message.From.Count != 1)
            {
                _logger.LogEmailFromMoreThanOneSender(message.From);
                return new SmtpResponse(SmtpReplyCode.Error, "Too many senders.");
            }

            if (message.From.Single() is not MailboxAddress senderAddress)
            {
                var address = message.From.Single();
                _logger.LogEmailFromInvalidSender(address);
                return new SmtpResponse(SmtpReplyCode.Error, "Invalid sender.");
            }

            var webhooks = new HashSet<DiscordWebhookClient>();
            foreach (var to in message.To.Concat(message.Cc)
                                         .Concat(message.Bcc)
                                         .SelectMany(GetMailboxAddresses))
            {
                var client = _webhookClientProvider.GetClient(to.Address);
                if (client is null)
                {
                    _logger.LogUnconfiguredRecipient(to);
                    continue;
                }
                webhooks.Add(client);
            }

            if (webhooks.Count > 0)
            {
                var attachments = GetFileAttachments(message.Attachments);
                try
                {
                    foreach (var webhook in webhooks)
                    {
                        if (attachments.IsEmpty)
                        {
                            await webhook.SendMessageAsync(
                                text: message.TextBody,
                                username: senderAddress.Name);
                        }
                        else
                        {
                            await webhook.SendFilesAsync(
                                attachments,
                                message.TextBody,
                                username: senderAddress.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email message to discord.");
                    return new SmtpResponse(SmtpReplyCode.MailboxUnavailable, "Error, maybe being rate-limited?");
                }
            }

            return SmtpResponse.Ok;
        }

        private static IEnumerable<MailboxAddress> GetMailboxAddresses(InternetAddress internetAddress)
        {
            if (internetAddress is MailboxAddress mailboxAddress)
            {
                return ImmutableArray.Create(mailboxAddress);
            }
            else
            {
                return ((GroupAddress) internetAddress).Members.SelectMany(GetMailboxAddresses);
            }
        }

        private ImmutableArray<FileAttachment> GetFileAttachments(
            IEnumerable<MimeEntity> mimeEntities)
        {
            var entities = mimeEntities.ToImmutableArray();
            var attachments = ImmutableArray.CreateBuilder<FileAttachment>(entities.Length);
            var messagePartReported = false;
            foreach (var entity in entities)
            {
                if (entity is MessagePart)
                {
                    if (!messagePartReported)
                    {
                        messagePartReported = true;
                        _logger.LogMessagePartFound();
                    }
                    continue;
                }
                else
                {
                    var mimePart = (MimePart) entity;

                    var memoryStream = new MemoryStream(mimePart.ContentDuration ?? 512 * 1024);
                    mimePart.WriteTo(memoryStream);
                    attachments.Add(new(memoryStream, sanitizeName(mimePart.FileName)));
                }
            }
            return attachments.ToImmutable();

            static string sanitizeName(string name) =>
                Regex.Replace(name, @"[^0-9a-zA-Z]", "");
        }
    }
}
