using Microsoft.AspNetCore.Mvc;
using NoteApp.Entities;

namespace NoteApp.Controllers
{
    [Route("api/[controller]")]
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] string Message)
        {
            var response = await _chatService.SendMessageAsync(Message);
            return Ok(response);
        }
    }
}
