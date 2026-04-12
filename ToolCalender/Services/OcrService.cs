using System.Text;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ToolCalender.Services
{
    public class OcrService
    {
        public static async Task<string> ExtractTextFromPdfOcrAsync(string filePath)
        {
            var sb = new StringBuilder();

            try
            {
                // 1. Load file PDF thông qua Windows Storage
                StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                
                PdfDocument pdfDoc = await PdfDocument.LoadFromFileAsync(file);
                if (pdfDoc.PageCount == 0) return string.Empty;

                // Khởi tạo bộ máy OCR (Ưu tiên tiếng Việt)
                var language = new Windows.Globalization.Language("vi-VN");
                OcrEngine ocrEngine = OcrEngine.IsLanguageSupported(language) 
                    ? OcrEngine.TryCreateFromLanguage(language) 
                    : OcrEngine.TryCreateFromUserProfileLanguages();

                if (ocrEngine == null) return "[OCR Error]: No OCR Engine available.";

                for (uint i = 0; i < pdfDoc.PageCount; i++)
                {
                    using (PdfPage page = pdfDoc.GetPage(i))
                    using (var stream = new InMemoryRandomAccessStream())
                    {
                        // Render trang PDF thành hình ảnh (DPI cao để OCR chuẩn)
                        var options = new PdfPageRenderOptions { DestinationWidth = 2048 }; 
                        await page.RenderToStreamAsync(stream, options);

                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                        using (SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync())
                        {
                            var result = await ocrEngine.RecognizeAsync(softwareBitmap);
                            if (result != null)
                            {
                                sb.AppendLine(result.Text);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[OCR Error]: {ex.Message}");
            }

            return sb.ToString();
        }
    }
}
