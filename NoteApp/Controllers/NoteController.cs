using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteApp.Data;
using NoteApp.Entities;
using NoteApp.Entities.Dtos;
using System.Text;
using UglyToad.PdfPig;
using NoteApp.Services;

namespace NoteApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NoteController : ControllerBase
    {
        AppDbContext _context;
        readonly ChatService _chatService;
        private readonly FileTextExtractorService _fileTextExtractor;

        public NoteController(AppDbContext context, ChatService chatService, FileTextExtractorService fileTextExtractor)
        {
            _context = context;
            _chatService = chatService;
            _fileTextExtractor = fileTextExtractor;
        }

        [HttpPost("Create")]
        public IActionResult CreateNote([FromForm] CreateNoteDto request)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId == null)
                return Unauthorized("Lutfen tekrar giris yapiniz.");

            var user = _context.Users
                .FirstOrDefault(u => u.Active
                                    && u.Id == Guid.Parse(userId));
            if (user == null)
                return Unauthorized();

            if (request.File == null && string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("İçerik veya dosya ekleyiniz.");
            }

            string? fileName = null;
            string? realFileName = null;
            string? filePath = null;
            NoteType noteType = NoteType.Text;

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

                fileName = Guid.NewGuid().ToString() + extension;
                filePath = Path.Combine(uploadFolder, fileName);
                realFileName = request.File.FileName;


                using var stream = new FileStream(filePath, FileMode.Create);
                request.File.CopyTo(stream);

                noteType = extension switch
                {
                    ".jpg" or ".jpeg" or ".png" => NoteType.Image,
                    ".pdf" => NoteType.Pdf,
                    ".docx" => NoteType.Word,
                    ".pptx" => NoteType.PowerPoint,
                    ".xlsx" => NoteType.Excel,
                    ".txt" => NoteType.TextFile,
                    ".mp3" or ".wav" => NoteType.Audio,
                    ".mp4" => NoteType.Video,
                    _ => NoteType.Text
                };

            }

            Guid? categoryId = request.CategoryId; //kategori belittilmemeişse

            if (categoryId == null || !_context.Categories.Any(c => c.Id == categoryId && c.UserId == user.Id && c.Active))
            {
                categoryId = GetOrCreateDefaultCategoryId(user.Id);
            }

            var note = new Note
            {
                Title = request.Title,
                Content = request.Content,
                FileName = fileName,
                FilePath = filePath,
                RealFileName = realFileName,
                NoteType = noteType,
                CategoryId = categoryId,
                UserId = user.Id,
                User = user,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.Notes.Add(note);
            _context.SaveChanges();

            return Ok(new
            {
                Message = "Not başarıyla oluşturuldu."
            });

        }
        private Guid GetOrCreateDefaultCategoryId(Guid userId)
        {
            var defaultCategory = _context.Categories
                .FirstOrDefault(c => c.UserId == userId && c.Name == "Diğer" && c.Active);

            if (defaultCategory == null)
            {
                defaultCategory = new Category
                {
                    Name = "Diğer",
                    UserId = userId,
                    Active = true
                };
                _context.Categories.Add(defaultCategory);
                _context.SaveChanges();
            }

            return defaultCategory.Id;
        }


        [HttpPost("CreateCategory")]
        public IActionResult CreateCategory([FromBody] CreateCategoryDto request)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId == null)
                return Unauthorized("Lutfen tekrar giris yapiniz.");
            var user = _context.Users
                .FirstOrDefault(u => u.Active
                                    && u.Id == Guid.Parse(userId));
            if (user == null)
                return Unauthorized();

            var categoryExists = _context.Categories
                .Any(c => c.Active &&
                          c.UserId == user.Id &&
                          c.Name.ToLower() == request.Name.ToLower());

            if (categoryExists)
                return BadRequest("Bu kategori zaten mevcut.");

            var category = new Category
            {
                Name = request.Name,
                UserId = user.Id
            };

            _context.Categories.Add(category);
            _context.SaveChanges();

            return Ok(new
            {
                Message = "Kategori başarıyla oluşturuldu.",
                CategoryId = category.Id
            });
        }

        [HttpGet("GetCategories")]
        public IActionResult GetCategories()
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized();

            var categories = _context.Categories
                .Where(c => c.Active &&
                            c.UserId == Guid.Parse(userId))
                .Select(c => new
                {
                    c.Id,
                    c.Name
                })
                .ToList();

            return Ok(categories);

        }

        [HttpGet("GetCategoryById")]
        public IActionResult GetCategoryById(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized();

            var category = _context.Categories
                .FirstOrDefault(c => c.Active &&
                            c.UserId == Guid.Parse(userId) && c.Id == id);
                

            return Ok(category.Name);

        }

        [HttpGet("GetNotes")]
        public IActionResult GetNotes()
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized("Lütfen tekrar giriş yapınız.");

            var notes = _context.Notes
                .Where(n => !n.IsDeleted && n.UserId == Guid.Parse(userId))
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Content,
                    n.FileName,
                    n.FilePath,
                    n.NoteType,
                    n.CategoryId,
                    n.IsPinned,
                    n.IsFavorite,
                    n.IsArchived,
                    n.UpdatedAt
                })
                .ToList();

            return Ok(notes);
        }

        [HttpGet("GetById/{id}")]
        public IActionResult GetById(Guid id)
        {
            var UsertId = User.FindFirst("userId")?.Value;

            if (UsertId == null)
            {
                return Unauthorized("Lütfen tekrar giriş yapınız.");
            }

            var note = _context.Notes
                .Where(n => n.Id == id && n.UserId == Guid.Parse(UsertId) && !n.IsDeleted).Select(n => new //notun id si mi kullanıcınn notu mu ve silinmiş mi
                {
                    n.Id,
                    n.Title,
                    n.Content,
                    n.FileName,
                    n.FilePath,
                    n.RealFileName,
                    n.NoteType,
                    n.CategoryId,
                    n.IsPinned,
                    n.IsFavorite,
                    n.IsArchived,
                    n.UpdatedAt
                })
                .FirstOrDefault();

            if (note == null)
                return NotFound("Not bulunamadı.");

            return Ok(note);
        }

        [HttpGet("GetFile/{id}")]
        public IActionResult GetFile(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId == null)
                return Unauthorized("Lütfen tekrar giriş yapınız.");

            var note = _context.Notes.FirstOrDefault(n =>
                n.Id == id && n.UserId == Guid.Parse(userId) && !n.IsDeleted);

            if (note == null || string.IsNullOrEmpty(note.FilePath))
                return NotFound("Dosya bulunamadı.");

            if (!System.IO.File.Exists(note.FilePath))
                return NotFound("Dosya sunucuda bulunamadı.");

            var contentType = note.FileName.Substring(note.FileName.LastIndexOf('.') + 1).ToLower() switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "pdf" => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(note.FilePath);
            return File(fileBytes, contentType, note.FileName);
        }

        [HttpGet("Search")]
        public IActionResult SearchNotes([FromQuery] string query)
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized("Lütfen tekrar giriş yapınız.");

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Arama metni boş olamaz.");

            var notes = _context.Notes
                .Where(n =>
                    n.UserId == Guid.Parse(userId) &&
                    !n.IsDeleted &&
                    (
                        n.Title.Contains(query) ||
                        n.Content.Contains(query)
                    ))
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Content,
                    n.FileName,
                    n.FilePath,
                    n.NoteType,
                    n.CategoryId,
                    n.IsPinned,
                    n.IsFavorite,
                    n.IsArchived,
                    n.UpdatedAt
                })
                .ToList();

            return Ok(notes);
        }


        [HttpPut("UpdateNote/{id}")]
        public IActionResult UpdateNote(Guid id, [FromForm] CreateNoteDto request)
        {
            var UsertId = User.FindFirst("userId")?.Value; //kullanıcıyı kontrol et

            if (UsertId == null)
            {
                return Unauthorized("Lütfen tekrar giriş yapınız.");
            }

            var note = _context.Notes.FirstOrDefault(n => n.Id == id && n.UserId == Guid.Parse(UsertId) && !n.IsDeleted); //notu bul

            if (note == null)
            {
                return NotFound("Not bulunamadı");
            }

            note.Title = request.Title; //başlık ve içerik güncelleme
            note.Content = request.Content;

            if (request.CategoryId != null &&  //kaegori kontroli
                _context.Categories.Any(c =>
                c.Id == request.CategoryId &&
                c.UserId == note.UserId &&
                c.Active))
            {
                note.CategoryId = request.CategoryId;
            }
            else
            {
                note.CategoryId = GetOrCreateDefaultCategoryId(note.UserId);
            }


            if (request.File != null) //dosya eklenecekse
            {
                var uploadFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Uploads");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var extension = Path
                    .GetExtension(request.File.FileName)
                    .ToLower();

                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadFolder, fileName);

                using var stream = new FileStream(
                    filePath,
                    FileMode.Create);

                request.File.CopyTo(stream);

                note.FileName = fileName;
                note.FilePath = filePath;
                note.RealFileName = request.File.FileName;

                note.NoteType = extension switch
                {
                    ".jpg" or ".jpeg" or ".png" => NoteType.Image,
                    ".pdf" => NoteType.Pdf,
                    ".docx" => NoteType.Word,
                    ".pptx" => NoteType.PowerPoint,
                    ".xlsx" => NoteType.Excel,
                    ".txt" => NoteType.TextFile,
                    ".mp3" or ".wav" => NoteType.Audio,
                    ".mp4" => NoteType.Video,
                    _ => NoteType.Text
                };
            }

            note.UpdatedAt = DateTime.UtcNow;
            _context.Update(note);
            _context.SaveChanges();
            return Ok(new
            {
                Message = "Not başarıyla güncellendi."
            });
        }

        [HttpDelete("DeleteNote/{id}")]
        public IActionResult DeleteNote(Guid id)
        {
            var UsertId = User.FindFirst("userId")?.Value;
            if (UsertId == null)
            {
                return Unauthorized("Lütfen tekrar giriş yapınız.");
            }

            var note = _context.Notes.FirstOrDefault(n => n.Id == id && n.UserId == Guid.Parse(UsertId) && !n.IsDeleted);
            if (note == null)
            {
                return NotFound("Not bulunamadı");
            }

            note.IsDeleted = true;
            note.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();
            return Ok(new
            {
                Message = "Not başarıyla silindi."
            });
        }

        [HttpGet("GetDeletedNotes")]
        public IActionResult GetDeletedNotes()
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized("Lütfen tekrar giriş yapınız.");

            var deletedNotes = _context.Notes //silinmiş notları listele
                .Where(n => n.UserId == Guid.Parse(userId) && n.IsDeleted)
                .ToList();

            return Ok(deletedNotes);
        }

        [HttpPatch("RestoreNote/{id}")] 
        public IActionResult Restored(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId == null)
            {
                return Unauthorized("Lütfen tekrar giriş yapınız.");
            }

            var note = _context.Notes.FirstOrDefault(n=>
                n.Id == id && n.UserId == Guid.Parse(userId) && n.IsDeleted); 

            if(note==null)
            {
                return NotFound("Not bulunamadı");
            };

            note.IsDeleted = false;
            note.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return Ok(new {message="Not başarıyla geri yüklendi."});
        }

        [HttpDelete("PermanentDelete/{id}")] 
        public IActionResult PermanentDelete(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value;
            if(userId == null)
            {
                return Unauthorized("Lütfen tekrar giriş yapınız.");
            }

            var note = _context.Notes.FirstOrDefault(n=>
                n.Id== id && n.UserId == Guid.Parse(userId) && n.IsDeleted);

            if (note == null)
            {
                return NotFound("Not bulunamadı");
            }
            
            if(!string.IsNullOrEmpty(note.FilePath) && System.IO.File.Exists(note.FilePath))
            {
                System.IO.File.Delete(note.FilePath);
            }

            _context.Notes.Remove(note);
            _context.SaveChanges();
            return Ok(new { Message = "Not kalıcı olarak silindi." });
        }



        [HttpPatch("Pin/{id}")]
        public IActionResult PinNote(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized("Lütfen tekrar giriş yapınız.");

            var note = _context.Notes
                .FirstOrDefault(n =>
                    n.Id == id &&
                    n.UserId == Guid.Parse(userId) &&
                    !n.IsDeleted);

            if (note == null)
                return NotFound("Not bulunamadı.");

            
            note.IsPinned = !note.IsPinned;
            note.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return Ok(new
            {
                Message = note.IsPinned
                    ? "Not sabitlendi."
                    : "Not sabitlemeden çıkarıldı.",
                IsPinned = note.IsPinned
            });
        }

        [HttpPatch("Favorite/{id}")]
        public IActionResult FavoriteNote(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized("Lütfen tekrar giriş yapınız.");

            var note = _context.Notes
                .FirstOrDefault(n =>
                    n.Id == id &&
                    n.UserId == Guid.Parse(userId) &&
                    !n.IsDeleted);

            if (note == null)
                return NotFound("Not bulunamadı.");


            note.IsFavorite = !note.IsFavorite;
            note.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return Ok(new
            {
                Message = note.IsFavorite
                    ? "Not favorilendi."
                    : "Not favorilerden çıkarıldı.",
                IsFavorite = note.IsFavorite
            });
        }

        [HttpPatch("Archive/{id}")]
        public IActionResult ArchiveNote(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized("Lütfen tekrar giriş yapınız.");

            var note = _context.Notes
                .FirstOrDefault(n =>
                    n.Id == id &&
                    n.UserId == Guid.Parse(userId) &&
                    !n.IsDeleted);

            if (note == null)
                return NotFound("Not bulunamadı.");

           
            note.IsArchived = !note.IsArchived;
            note.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return Ok(new
            {
                Message = note.IsArchived
                    ? "Not arşivlendi."
                    : "Not arşivden çıkarıldı.",
                IsArchived = note.IsArchived
            });
        }

        [HttpPost("Summarize/{id}")]
        public async Task<IActionResult> Summarize(Guid id)
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized("Lütfen tekrar giriş yapınız.");

            var note = _context.Notes.FirstOrDefault(n =>
                n.Id == id &&
                n.UserId == Guid.Parse(userId) &&
                !n.IsDeleted);

            if (note == null)
                return NotFound("Not bulunamadı.");

            

            // PDF dosya yolu var mı?
            if (string.IsNullOrWhiteSpace(note.FilePath))
                return BadRequest("Dosyasının yolu bulunamadı.");

            // Dosya gerçekten var mı?
            if (!System.IO.File.Exists(note.FilePath))
                return NotFound("Dosya bulunamadı.");

            try
            {
                var extractedText = await _fileTextExtractor.ExtractTextAsync(
                    note.FilePath,
                    note.NoteType
                );

                Console.WriteLine("EXCEL'DEN ÇIKAN METİN:");
                Console.WriteLine(extractedText);

                if (string.IsNullOrWhiteSpace(extractedText))
                    return BadRequest("Dosyadan metin çıkarılamadı.");

                var prompt = $"""
                       Aşağıda bir dosyadan çıkarılmış metin bulunmaktadır.

                   Bu metni Türkçe olarak, ders çalışmaya uygun ve anlaşılır bir şekilde özetle.

                   Özetleme sırasında şu kurallara kesinlikle uy:

                   - Metnin ana konusunu belirle.
                   - Önemli başlıkları ve alt başlıkları koru.
                   - Önemli tanımları, kavramları, yöntemleri, formülleri, sınıflandırmaları ve sonuçları atlama.
                   - Gereksiz tekrarları ve konu dışı ayrıntıları çıkar.
                   - Metinde bulunmayan hiçbir bilgiyi ekleme.
                   - Kendi yorumunu veya kişisel görüşünü ekleme.
                   - Teknik terimleri mümkün olduğunca orijinal anlamını koruyarak kullan.
                   - Önemli bilgileri kısa ve anlaşılır cümlelerle açıkla.
                   - Uygun yerlerde madde işaretleri ve numaralandırma kullan.
                   - Eğer metinde karşılaştırmalar varsa bunları açıkça karşılaştır.
                   - Eğer metinde bir süreç veya aşamalar anlatılıyorsa adım adım sırala.
                   - Eğer metinde soru-cevap yapısı varsa, soruları mümkün olduğunca koru ve her sorunun altında metindeki bilgilere dayanarak kısa ve açıklayıcı cevabını ver.
                   - Eğer metinde sınavda sorulabilecek tanımlar veya önemli bilgiler varsa bunları özellikle belirgin hale getir.
                   - Eğer metin soru içermiyorsa, zorla soru-cevap formatına dönüştürme; normal konu başlıkları altında özetle.
                   - Dosyanın yapısını mümkün olduğunca koru.
                   - Özet, orijinal metinden daha kısa olmalı ancak önemli bilgileri kaybetmemelidir.
                   - Sadece hazırladığın özeti döndür. Özet hakkında ayrıca açıklama yapma.

                   DOSYADAN ÇIKARILAN METİN:
                {extractedText}
                """;

                var summary = await _chatService.SendMessageAsync(prompt);

                return Ok(new
                {
                    message = "Dosya başarıyla özetlendi.",
                    textLength = extractedText.Length,
                    summary = summary
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Dosya okunurken hata oluştu: {ex.Message}");
            }
        }
    }
}
