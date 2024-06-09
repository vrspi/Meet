using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public class WebSocketManager
{
    private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

    public async Task HandleConnection(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            string connId = Guid.NewGuid().ToString();
            _sockets.TryAdd(connId, webSocket);
            Console.WriteLine($"WebSocket connected: {connId}");

            await ProcessMessages(connId, webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }

    public async Task ProcessMessages(string connId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            foreach (var pair in _sockets)
            {
                if (pair.Key != connId && pair.Value.State == WebSocketState.Open)
                {
                    Console.WriteLine($"Sending data to {pair.Key}");
                    await pair.Value.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                }
            }
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Console.WriteLine($"Subsequent data received from {connId}, MessageType: {result.MessageType}, Count: {result.Count}");
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        _sockets.TryRemove(connId, out _); // Clean up
    }
}

public static class WebSocketExtensions
{
    public static void UseWebSocketManager(this IApplicationBuilder app)
    {
        var webSocketOptions = new WebSocketOptions()
        {
            KeepAliveInterval = TimeSpan.FromSeconds(120),
            ReceiveBufferSize = 4 * 1024
        };

        app.UseWebSockets(webSocketOptions);
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/ws")
            {
                var manager = new WebSocketManager();
                await manager.HandleConnection(context);
            }
            else
            {
                await next();
            }
        });
    }
}
