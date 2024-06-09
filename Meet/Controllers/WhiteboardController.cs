using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Meet.Controllers
{
    public class WhiteboardController : ControllerBase
    {
        private static readonly ConcurrentBag<WebSocket> _sockets = new ConcurrentBag<WebSocket>();
        private readonly ILogger<WhiteboardController> _logger;

        public WhiteboardController(ILogger<WhiteboardController> logger)
        {
            _logger = logger;
        }

        public async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            _logger.LogInformation("WebSocket connection established");
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = null;

            try
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.CloseStatus.HasValue)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation($"Received message: {message}");
                    await BroadcastMessage(message, webSocket);
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection");
            }
            finally
            {
                _sockets.TryTake(out webSocket);
                if (result != null)
                {
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
                else
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                _logger.LogInformation("WebSocket connection closed");
            }
        }

        private async Task BroadcastMessage(string message, WebSocket sender)
        {
            _logger.LogInformation("Broadcasting message to all clients");
            var tasks = _sockets
                .Where(s => s.State == WebSocketState.Open && s != sender)
                .Select(async s =>
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    _logger.LogInformation($"Sending message: {message} to client");
                    await s.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                });

            await Task.WhenAll(tasks);
        }
    }
}
