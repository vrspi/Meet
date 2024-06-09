using System.Net.Sockets;
using System.Net;
using System.Text;
using Meet.Models;
using System.Collections.Concurrent;

namespace Meet.services
{
    public class ChatService
    {
        private const int Port = 15000;
        private TcpListener _server;
        private readonly ConcurrentBag<TcpClient> _clients = new ConcurrentBag<TcpClient>();
        private readonly ConcurrentDictionary<string, TcpClient> _onlineUsers = new ConcurrentDictionary<string, TcpClient>();
        private readonly List<ChatMessage> _messages = new List<ChatMessage>();
        private readonly object _lock = new object();
        private bool _serverStarted = false;

        public void StartServer()
        {
            if (_serverStarted) return;

            lock (_lock)
            {
                if (_serverStarted) return;

                _server = new TcpListener(IPAddress.Loopback, Port);
                _server.Start();
                _serverStarted = true;
            }

            Task.Run(async () => await AcceptClientsAsync());
        }

        public bool IsServerStarted()
        {
            lock (_lock)
            {
                return _serverStarted;
            }
        }

        public async Task ConnectClientAsync(string username)
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, Port);
            _clients.Add(client);
            _onlineUsers[username] = client;

            _ = HandleClientAsync(client, username);
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                _clients.Add(client);
                // You need to add some logic here to get the username of the connecting client
                // _onlineUsers[username] = client;
                // _ = HandleClientAsync(client, username);
            }
        }

        private async Task HandleClientAsync(TcpClient client, string username)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    _onlineUsers.TryRemove(username, out _);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var chatMessage = System.Text.Json.JsonSerializer.Deserialize<ChatMessage>(message);

                lock (_lock)
                {
                    _messages.Add(chatMessage);
                }

                await BroadcastMessageAsync(chatMessage);
            }
        }

        private async Task BroadcastMessageAsync(ChatMessage message)
        {
            var messageJson = System.Text.Json.JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(messageJson);

            foreach (var client in _clients)
            {
                if (client.Connected)
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

        public List<ChatMessage> GetMessages()
        {
            lock (_lock)
            {
                return new List<ChatMessage>(_messages);
            }
        }

        public List<string> GetOnlineUsers()
        {
            return new List<string>(_onlineUsers.Keys);
        }

        public async Task SendMessageAsync(ChatMessage message)
        {
            lock (_lock)
            {
                _messages.Add(message);
            }

            var messageJson = System.Text.Json.JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(messageJson);

            foreach (var client in _clients)
            {
                if (client.Connected)
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

    }
}
