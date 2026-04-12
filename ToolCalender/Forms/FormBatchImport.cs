using System.Collections.Concurrent;
using ToolCalender.Models;
using ToolCalender.Services;

namespace ToolCalender.Forms
{
    public class FormBatchImport : Form
    {
        // ── Design System ────────────────────────────────────────
        private static readonly Color CHeader     = Color.FromArgb(15, 40, 80);
        private static readonly Color CAccent     = Color.FromArgb(37, 99, 235);
        private static readonly Color CBg         = Color.FromArgb(241, 245, 249);
        private static readonly Color CCard       = Color.White;
        private static readonly Color CText       = Color.FromArgb(30, 41, 59);
        private static readonly Color CBorder     = Color.FromArgb(203, 213, 225);

        // ── Controls ─────────────────────────────────────────────
        private DataGridView dgv = new();
        private Label lblTotal = new();
        private Label lblProcessing = new();
        private ProgressBar pbProgress = new();
        private Button btnSelectFolder = new();
        private Button btnSaveAll = new();
        private Panel pnlPagination = new();
        private Label lblPageInfo = new();
        
        private List<DocumentRecord> _tempDocs = new();
        private int _currentPage = 1;
        private int _pageSize = 15;
        public List<DocumentRecord> Results { get; private set; } = new();

        // ════════════════════════════════════════════════════════
        public FormBatchImport()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text          = "Nhập Văn Bản Hàng Loạt Từ Thư Mục";
            this.Size          = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor     = CBg;
            this.Font          = new Font("Segoe UI", 9.5f);
            this.AllowDrop     = true;

            this.DragEnter += (s, e) => {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) {
                    var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (paths != null && paths.Any(p => Directory.Exists(p))) e.Effect = DragDropEffects.Copy;
                }
            };
            this.DragDrop += async (s, e) => {
                var paths = e.Data?.GetData(DataFormats.FileDrop) as string[];
                if (paths != null) {
                    var dir = paths.FirstOrDefault(p => Directory.Exists(p));
                    if (dir != null) await ScanFolderAsync(dir);
                }
            };
// ... (rest of old BuildUI code adapted below)
            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = CHeader };
            var lblTitle = new Label {
                Text = "📥  NHẬP VĂN BẢN HÀNG LOẠT",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };
            var lblSub = new Label {
                Text = "Chọn thư mục hoặc kéo thả thư mục vào đây để tự động bóc tách dữ liệu.",
                ForeColor = Color.FromArgb(147, 197, 253),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                Location = new Point(22, 50),
                AutoSize = true
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // Toolbar
            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 75, BackColor = Color.White, Padding = new Padding(15, 12, 15, 10) };
            pnlToolbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(CBorder), 0, 74, pnlToolbar.Width, 74);

            btnSelectFolder = MakeBtn("📂  Chọn Thư Mục", CAccent, Color.White);
            btnSelectFolder.Click += BtnSelectFolder_Click;
            
            btnSaveAll = MakeBtn("💾  Lưu Tất Cả Vào Hệ Thống", Color.FromArgb(21, 128, 61), Color.White);
            btnSaveAll.Enabled = false;
            btnSaveAll.Click += (s, e) => { Results = _tempDocs; this.DialogResult = DialogResult.OK; this.Close(); };

            lblProcessing = new Label { Text = "Sẵn sàng...", ForeColor = CText, AutoSize = true, Location = new Point(540, 20), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pbProgress = new ProgressBar { Width = 300, Height = 18, Location = new Point(540, 45), Visible = false };

            pnlToolbar.Controls.Add(btnSelectFolder);
            pnlToolbar.Controls.Add(btnSaveAll);
            pnlToolbar.Controls.Add(lblProcessing);
            pnlToolbar.Controls.Add(pbProgress);
            
            btnSelectFolder.Width = 180;
            btnSelectFolder.Top = 15;

            btnSaveAll.Left = 210;
            btnSaveAll.Width = 300;
            btnSaveAll.Top = 15;

            // Simple Pagination
            pnlPagination = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.White };
            pnlPagination.Paint += (s, e) => e.Graphics.DrawLine(new Pen(CBorder), 0, 0, pnlPagination.Width, 0);

            var btnNext = MakeBtn("Trang Sau  ▶", Color.White, CText);
            btnNext.Width = 110; btnNext.Dock = DockStyle.Right;
            btnNext.Click += (s, e) => { if (_currentPage * _pageSize < _tempDocs.Count) { _currentPage++; DisplayPage(); } };

            lblPageInfo = new Label { Text = "Trang 1 / 1", Width = 100, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

            var btnPrev = MakeBtn("◀  Trang Trước", Color.White, CText);
            btnPrev.Width = 110; btnPrev.Dock = DockStyle.Right;
            btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; DisplayPage(); } };

            pnlPagination.Controls.AddRange(new Control[] { btnNext, lblPageInfo, btnPrev });

            // Grid
            dgv = new DataGridView {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 35 },
                AllowDrop = true // Allow drop on grid too
            };
            dgv.DragEnter += (s, e) => e.Effect = DragDropEffects.Copy;
            dgv.DragDrop += async (s, e) => {
                var paths = e.Data?.GetData(DataFormats.FileDrop) as string[];
                if (paths != null) {
                    var dir = paths.FirstOrDefault(p => Directory.Exists(p));
                    if (dir != null) await ScanFolderAsync(dir);
                }
            };
            dgv.Columns.Add("colFile", "Tên File");
            dgv.Columns.Add("colSo", "Số Văn Bản");
            dgv.Columns.Add("colTrichYeu", "Trích Yếu");
            dgv.Columns.Add("colHạn", "Hạn Cuối");
            dgv.Columns.Add("colStatus", "Trạng Thái");
            dgv.Columns["colTrichYeu"].FillWeight = 200;

            this.Controls.Add(dgv);
            this.Controls.Add(pnlPagination);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);
        }

        public async Task ScanFolderAsync(string path)
        {
            if (!Directory.Exists(path)) return;

            string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (files.Length == 0) { MessageBox.Show("Không tìm thấy file hợp lệ trong thư mục này."); return; }

            _tempDocs.Clear();
            dgv.Rows.Clear();
            _currentPage = 1;
            btnSaveAll.Enabled = false;
            pbProgress.Visible = true;
            pbProgress.Maximum = files.Length;
            pbProgress.Value = 0;

            int count = 0;
            foreach (var file in files)
            {
                count++;
                lblProcessing.Text = $"⏳ Đang xử lý: {count}/{files.Length} - {Path.GetFileName(file)}";
                pbProgress.Value = count;

                try {
                    var record = await DocumentExtractorService.ExtractFromFileAsync(file);
                    _tempDocs.Add(record);
                    if (count % 3 == 0 || count == files.Length) DisplayPage();
                } catch {
                    _tempDocs.Add(new DocumentRecord { FilePath = file, SoVanBan = "LỖI", TrichYeu = "Không thể đọc file này" });
                }
            }

            lblProcessing.Text = $"✅ Hoàn thành! Đã bóc tách {files.Length} file.";
            btnSaveAll.Enabled = true;
        }

        private async void BtnSelectFolder_Click(object? sender, EventArgs e)
        {
            using var fbg = new FolderBrowserDialog { Description = "Chọn thư mục chứa văn bản" };
            if (fbg.ShowDialog() == DialogResult.OK)
            {
                await ScanFolderAsync(fbg.SelectedPath);
            }
        }

        private void DisplayPage()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(DisplayPage)); return; }

            dgv.Rows.Clear();
            int totalPages = (int)Math.Ceiling((double)_tempDocs.Count / _pageSize);
            if (totalPages == 0) totalPages = 1;
            lblPageInfo.Text = $"Trang {_currentPage} / {totalPages}";

            var items = _tempDocs.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
            foreach (var doc in items)
            {
                dgv.Rows.Add(
                    Path.GetFileName(doc.FilePath),
                    doc.SoVanBan,
                    doc.TrichYeu,
                    doc.ThoiHan?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có",
                    string.IsNullOrEmpty(doc.SoVanBan) || doc.SoVanBan == "LỖI" ? "⚠️ Check lại" : "✅ OK"
                );
            }
        }

        private Button MakeBtn(string text, Color bg, Color fg) => new Button {
            Text = text, BackColor = bg, ForeColor = fg, FlatStyle = FlatStyle.Flat, 
            Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand, Height = 36,
            FlatAppearance = { BorderSize = 1, BorderColor = CBorder }
        };
    }
}
