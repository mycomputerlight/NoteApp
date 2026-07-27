namespace NoteApp.Entities.Dtos
{
    public class CreateNoteDto
    {
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public Guid? CategoryId { get; set; }

        public IFormFile? File { get; set; }

    }
}
