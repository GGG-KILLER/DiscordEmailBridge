
using Microsoft.Extensions.Hosting;

namespace DiscordEmailBridge.Hosting
{
    internal class SmtpServerService : IHostedService
    {
        private readonly SmtpServer.SmtpServer _server;

        public SmtpServerService(SmtpServer.SmtpServer server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public async Task StartAsync(CancellationToken cancellationToken) =>
            await _server.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.Shutdown();
            return _server.ShutdownTask;
        }
    }
}
