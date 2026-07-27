using Microsoft.AspNetCore.Mvc;
using NoteApp.Data;
using NoteApp.Entities;
using NoteApp.Entities.Dtos;

namespace NoteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NoteController : ControllerBase
    {
        AppDbContext _context;
    

        public NoteController(AppDbContext context)
        {
            _context = context;
        }   

        [HttpPost("Create")]
        public IActionResult CreateNote([FromForm] CreateNoteDto request)
        {
            //jwt
            if (request.File != null)
            {
                var uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads");

             
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }
                var extension = Path.GetExtension(request.File.FileName).ToLower();

                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);

                request.File.CopyTo(stream);
                NoteType noteType;

                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                        noteType = NoteType.Image;
                        break;

                    case ".pdf":
                        noteType = NoteType.Pdf;
                        break;

                    case ".docx":
                        noteType = NoteType.Word;
                        break;

                    case ".pptx":
                        noteType = NoteType.PowerPoint;
                        break;

                    case ".xlsx":
                        noteType = NoteType.Excel;
                        break;

                    case ".txt":
                        noteType = NoteType.TextFile;
                        break;

                    case ".mp3":
                    case ".wav":
                        noteType = NoteType.Audio;
                        break;

                    case ".mp4":
                        noteType = NoteType.Video;
                        break;

                    default:
                        noteType = NoteType.Text;
                        break;
                }

            

                var note = new Note()
                {
                    Title = request.Title,
                    Content = request.Content,
                    FileName = fileName,
                    FilePath = filePath,
                    NoteType = noteType,
                    CategoryId = request.CategoryId,

                };
                _context.Notes.Add(note);
                _context.SaveChanges();
                return Ok(new { Message = "Not başarıyla oluşturuldu" });
            }

            return Ok("Dosya gelmedi");


           
        }
    }
}
