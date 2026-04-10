using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using ToolCalender.Models;

namespace ToolCalender.Services
{
    public static partial class DocumentExtractorService
    {
        public static DocumentRecord ExtractFromFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            string text = ext switch
            {
                ".pdf" => ExtractFromPdf(filePath),
                ".docx" or ".doc" => ExtractFromWord(filePath),
                _ => throw new NotSupportedException($"Định dạng '{ext}' không hỗ trợ. Chỉ hỗ trợ PDF và DOCX.")
            };

            return ParseText(text, filePath);
        }

        // ------- Đọc PDF -------
        private static string ExtractFromPdf(string filePath)
        {
            var sb = new StringBuilder();
            using var reader = new PdfReader(filePath);
            using var pdf = new PdfDocument(reader);

            for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
            {
                var page = pdf.GetPage(i);
                var strategy = new LocationTextExtractionStrategy();
                string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                sb.AppendLine(pageText);
            }
            return sb.ToString();
        }

        // ------- Đọc Word -------
        private static string ExtractFromWord(string filePath)
        {
            var sb = new StringBuilder();
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body != null)
                foreach (var para in body.Descendants<Paragraph>())
                    sb.AppendLine(para.InnerText);
            return sb.ToString();
        }

        // ------- Generated Regex — compile-time, zero runtime recompilation -------

        [GeneratedRegex(@"[Ss]ố[:\s]*(\d+[/\-][A-ZĐÀÁẢÃẠĂẮẶẰẲẴÂẤẬẦẨẪ&\.\-/]+(?:[/\-][A-ZĐÀÁẢÃẠĂẮẶẰẲẴÂẤẬẦẨẪ]+)*)")]
        private static partial Regex RxSoVanBan();

        [GeneratedRegex(@"ngày\s+(\d{1,2})\s+tháng\s+(\d{1,2})\s+năm\s+(\d{4})", RegexOptions.IgnoreCase)]
        private static partial Regex RxNgayBanHanh();

        // ThoiHan: trước ngày DD/MM/YYYY
        [GeneratedRegex(@"trước ngày\s+(\d{1,2})[/\-](\d{1,2})[/\-](\d{4})", RegexOptions.IgnoreCase)]
        private static partial Regex RxThoiHan1();

        // ThoiHan: trước ngày NN tháng MM năm YYYY
        [GeneratedRegex(@"trước ngày\s+(\d{1,2})\s+tháng\s+(\d{1,2})\s+năm\s+(\d{4})", RegexOptions.IgnoreCase)]
        private static partial Regex RxThoiHan2();

        // ThoiHan: chậm nhất (vào|ngày)? DD/MM/YYYY
        [GeneratedRegex(@"chậm nhất(?:\s+(?:vào|ngày))?\s+(\d{1,2})[/\-](\d{1,2})[/\-](\d{4})", RegexOptions.IgnoreCase)]
        private static partial Regex RxThoiHan3();

        // ThoiHan: chậm nhất (vào|ngày)? NN tháng MM năm YYYY
        [GeneratedRegex(@"chậm nhất(?:\s+(?:vào|ngày))?\s+(\d{1,2})\s+tháng\s+(\d{1,2})\s+năm\s+(\d{4})", RegexOptions.IgnoreCase)]
        private static partial Regex RxThoiHan4();

        // ThoiHan: hạn (đến|chót|nộp) (ngày)? DD/MM/YYYY
        [GeneratedRegex(@"hạn\s+(?:đến|chót|nộp)(?:\s+ngày)?\s+(\d{1,2})[/\-](\d{1,2})[/\-](\d{4})", RegexOptions.IgnoreCase)]
        private static partial Regex RxThoiHan5();

        // ThoiHan: hạn (đến|chót|nộp) (ngày)? NN tháng MM năm YYYY
        [GeneratedRegex(@"hạn\s+(?:đến|chót|nộp)(?:\s+ngày)?\s+(\d{1,2})\s+tháng\s+(\d{1,2})\s+năm\s+(\d{4})", RegexOptions.IgnoreCase)]
        private static partial Regex RxThoiHan6();

        [GeneratedRegex(@"Sở|UBND|Ủy ban|Phòng|Ban|Cục|Chi cục|Tổng cục", RegexOptions.IgnoreCase)]
        private static partial Regex RxCoQuan();

        [GeneratedRegex(@"\(qua\s+([^\)]{5,100})\)", RegexOptions.IgnoreCase)]
        private static partial Regex RxChuQuan();

        [GeneratedRegex(@"Chi cục[^\n,;\.]{3,60}|Phòng [^\n,;\.]{3,50}", RegexOptions.IgnoreCase)]
        private static partial Regex RxChuQuanFallback();

        // 7 pattern gộp thành 1 alternation — 1 lần scan thay vì 7
        [GeneratedRegex(
            @"Kinh tế[/\s]*Kinh tế|Hạ tầng và Đô thị|Trung tâm Cung ứng[^\n,;\.]{0,50}|Văn phòng[^\n,;\.]{0,30}|Nội vụ|Tài chính[^\n,;\.]{0,30}|Tư pháp",
            RegexOptions.IgnoreCase)]
        private static partial Regex RxDonVi();

        [GeneratedRegex(@"[Vv]/[vV]\s*[:\.]?\s*(.{10,200})")]
        private static partial Regex RxTrichYeu1();

        [GeneratedRegex(@"[Vv]ề\s+việc\s+(.{10,200})")]
        private static partial Regex RxTrichYeu2();

        // ------- Helper parse date từ match groups (1=DD, 2=MM, 3=YYYY) -------
        private static DateTime? TryParseDate(Match m)
        {
            if (!m.Success) return null;
            try
            {
                return new DateTime(
                    int.Parse(m.Groups[3].Value),
                    int.Parse(m.Groups[2].Value),
                    int.Parse(m.Groups[1].Value));
            }
            catch { return null; }
        }

        // ------- Phân tích văn bản -------
        private static DocumentRecord ParseText(string text, string filePath)
        {
            var record = new DocumentRecord
            {
                FilePath = filePath,
                NgayThem = DateTime.Now
            };

            string t = text.Replace("\r\n", "\n").Replace("\r", "\n").Normalize(NormalizationForm.FormC);

            // ── 1. Số văn bản
            var mSoVb = RxSoVanBan().Match(t);
            if (mSoVb.Success) record.SoVanBan = mSoVb.Groups[1].Value.Trim();

            // ── 2. Ngày ban hành
            record.NgayBanHanh = TryParseDate(RxNgayBanHanh().Match(t));

            // ── 3. Thời hạn — thử lần lượt 6 dạng diễn đạt
            record.ThoiHan = TryParseDate(RxThoiHan1().Match(t))
                ?? TryParseDate(RxThoiHan2().Match(t))
                ?? TryParseDate(RxThoiHan3().Match(t))
                ?? TryParseDate(RxThoiHan4().Match(t))
                ?? TryParseDate(RxThoiHan5().Match(t))
                ?? TryParseDate(RxThoiHan6().Match(t));

            // ── 4. Cơ quan ban hành (dòng đầu chứa từ khóa tổ chức)
            var rxCoQuan = RxCoQuan();
            var coQuanLine = t.Split('\n').Take(30).FirstOrDefault(l => rxCoQuan.IsMatch(l));
            if (!string.IsNullOrWhiteSpace(coQuanLine))
                record.CoQuanBanHanh = coQuanLine.Trim();

            // ── 5. Cơ quan chủ quản tham mưu
            var mChuQuan = RxChuQuan().Match(t);
            if (mChuQuan.Success)
                record.CoQuanChuQuan = mChuQuan.Groups[1].Value.Trim();
            else
            {
                var mCQ = RxChuQuanFallback().Match(t);
                if (mCQ.Success) record.CoQuanChuQuan = mCQ.Groups[1].Value.Trim();
            }

            // ── 6. Đơn vị bị chỉ đạo — 1 lần scan với alternation
            var donViList = new List<string>();
            foreach (Match m in RxDonVi().Matches(t))
            {
                string val = Regex.Replace(m.Value.Trim(), @"\s+", " ");
                if (!donViList.Any(x => x.Contains(val, StringComparison.OrdinalIgnoreCase)))
                    donViList.Add(val);
            }
            if (donViList.Count > 0)
                record.DonViChiDao = string.Join("; ", donViList.Distinct());

            // ── 7. Trích yếu
            var mTrichYeu = RxTrichYeu1().Match(t);
            if (mTrichYeu.Success)
                record.TrichYeu = mTrichYeu.Groups[1].Value.Trim();
            else
            {
                var mVV = RxTrichYeu2().Match(t);
                if (mVV.Success) record.TrichYeu = mVV.Groups[1].Value.Trim();
            }

            return record;
        }
    }
}
