using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using NoteApp.Entities;
using System.Text;

namespace NoteApp.Services
{
    public class FileTextExtractorService
    {
        public async Task<string> ExtractTextAsync(string filePath, NoteType noteType)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Dosya yolu boş.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Dosya bulunamadı.", filePath);

            return noteType switch
            {
                NoteType.Pdf => await ExtractPdfTextAsync(filePath),

                NoteType.Word => await ExtractWordTextAsync(filePath),

                NoteType.PowerPoint => await ExtractPowerPointTextAsync(filePath),

                NoteType.Excel => await ExtractExcelTextAsync(filePath),

                NoteType.TextFile => await File.ReadAllTextAsync(filePath),

                _ => throw new NotSupportedException(
                    $"'{noteType}' türündeki dosyadan metin çıkarma desteklenmiyor.")
            };
        }


        // ---------------------------------------------------------
        // PDF
        // ---------------------------------------------------------

        private async Task<string> ExtractPdfTextAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath);

                var text = new StringBuilder();

                foreach (var page in pdf.GetPages())
                {
                    text.AppendLine(page.Text);
                }

                return text.ToString();
            });
        }


        // ---------------------------------------------------------
        // WORD (.docx)
        // ---------------------------------------------------------

        private async Task<string> ExtractWordTextAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var document = WordprocessingDocument.Open(filePath, false);

                var body = document.MainDocumentPart?.Document?.Body;

                if (body == null)
                    return string.Empty;

                var text = new StringBuilder();

                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }

                return text.ToString();
            });
        }


        // ---------------------------------------------------------
        // POWERPOINT (.pptx)
        // ---------------------------------------------------------

        private async Task<string> ExtractPowerPointTextAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var presentation =
                    PresentationDocument.Open(filePath, false);

                var text = new StringBuilder();

                var presentationPart =
                    presentation.PresentationPart;

                if (presentationPart == null)
                    return string.Empty;

                foreach (var slidePart in presentationPart.SlideParts)
                {
                    foreach (var textBody in
                             slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                    {
                        text.AppendLine(textBody.Text);
                    }
                }

                return text.ToString();
            });
        }


        // ---------------------------------------------------------
        // EXCEL (.xlsx)
        // ---------------------------------------------------------

        private async Task<string> ExtractExcelTextAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var spreadsheet =
                    SpreadsheetDocument.Open(filePath, false);

                var workbookPart = spreadsheet.WorkbookPart;

                if (workbookPart == null)
                    return string.Empty;

                var sharedStringPart = workbookPart.SharedStringTablePart;

                var text = new StringBuilder();

                foreach (var worksheetPart in workbookPart.WorksheetParts)
                {
                    var sheetData =
                        worksheetPart.Worksheet.GetFirstChild<SheetData>();

                    if (sheetData == null)
                        continue;

                    foreach (var row in sheetData.Elements<Row>())
                    {
                        foreach (var cell in row.Elements<Cell>())
                        {
                            string value = GetExcelCellValue(
                                cell,
                                sharedStringPart
                            );

                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                text.Append(value);
                                text.Append(" ");
                            }
                        }

                        text.AppendLine();
                    }
                }

                return text.ToString();
            });
        }

        private string GetExcelCellValue(
    Cell cell,
    SharedStringTablePart? sharedStringPart)
        {
            if (cell.CellValue == null)
                return string.Empty;

            var value = cell.CellValue.Text;

            // Shared String
            if (cell.DataType != null &&
                cell.DataType.Value == CellValues.SharedString)
            {
                if (sharedStringPart?.SharedStringTable == null)
                    return value;

                if (int.TryParse(value, out int index))
                {
                    var item =
                        sharedStringPart.SharedStringTable
                            .Elements<SharedStringItem>()
                            .ElementAtOrDefault(index);

                    return item?.InnerText ?? value;
                }
            }

            // Normal string / sayı / diğer değerler
            return value;
        }
    }
}