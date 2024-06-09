using Meet.Models;
using Meet.services;
using Microsoft.AspNetCore.Mvc;

namespace Meet.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(string username)
        {
            HttpContext.Session.SetString("Username", username);

            // Start the server if it hasn't been started yet
            if (!_chatService.IsServerStarted())
            {
                _chatService.StartServer();
            }

            // Connect the client
            await _chatService.ConnectClientAsync(username);

            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("SignIn");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string message)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("SignIn");
            }

            var chatMessage = new ChatMessage
            {
                Username = username,
                Message = message
            };

            await _chatService.SendMessageAsync(chatMessage);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SendAudioMessage(IFormFile audio)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("SignIn");
            }

            if (audio != null && audio.Length > 0)
            {
                var audioFileName = $"{Guid.NewGuid()}.wav";
                var audioPath = Path.Combine("wwwroot", "uploads", audioFileName);
                using (var stream = new FileStream(audioPath, FileMode.Create))
                {
                    await audio.CopyToAsync(stream);
                }

                var chatMessage = new ChatMessage
                {
                    Username = username,
                    AudioUrl = $"/uploads/{audioFileName}",
                    Timestamp = DateTime.UtcNow
                };

                await _chatService.SendMessageAsync(chatMessage);
            }

            return Ok();
        }


        [HttpGet]
        public JsonResult GetMessages()
        {
            var messages = _chatService.GetMessages();
            return Json(messages);
        }

        [HttpGet]
        public JsonResult GetOnlineUsers()
        {
            var users = _chatService.GetOnlineUsers();
            return Json(users);
        }
    }
}
