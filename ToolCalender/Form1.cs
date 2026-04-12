using ToolCalender.Data;
using ToolCalender.Forms;
using ToolCalender.Models;
using ToolCalender.Services;

namespace ToolCalender
{
    public partial class Form1 : Form
    {
        // ── Color Palette ────────────────────────────────────────
        private static readonly Color CHeaderBg    = Color.FromArgb(15, 40, 80);
        private static readonly Color CHeaderText  = Color.White;
        private static readonly Color CBg          = Color.FromArgb(236, 241, 248);
        private static readonly Color CAccent      = Color.FromArgb(37, 99, 235);
        private static readonly Color CCard        = Color.White;
        private static readonly Color CText        = Color.FromArgb(30, 41, 59);
        private static readonly Color CMuted       = Color.FromArgb(100, 116, 139);
        private static readonly Color CBorder      = Color.FromArgb(203, 213, 225);
        private static readonly Color CToolbar     = Color.FromArgb(248, 250, 252);

        // Row status colors
        private static readonly Color CRowDanger    = Color.FromArgb(254, 226, 226);
        private static readonly Color CRowDangerTxt = Color.FromArgb(153, 27, 27);
        private static readonly Color CRowWarn      = Color.FromArgb(254, 243, 199);
        private static readonly Color CRowWarnTxt   = Color.FromArgb(120, 53, 15);
        private static readonly Color CRowAlert     = Color.FromArgb(255, 237, 213);
        private static readonly Color CRowAlertTxt  = Color.FromArgb(154, 52, 18);
        private static readonly Color CRowOk        = Color.White;
        private static readonly Color CRowOkTxt     = CText;

        // ── Controls ─────────────────────────────────────────────
        private DataGridView dgv       = new();
        private TextBox      txtSearch = new();
        private Label        lblClock  = new();
        private Label        lblTong   = new();
        private Label        lblSapHan = new();
        private Label        lblQuaHan = new();
        private Label        lblHomNay = new();
        private Panel        pnlStatTong   = new();
        private Panel        pnlStatSap    = new();
        private Panel        pnlStatQua    = new();
        private Panel        pnlStatHomNay = new();

        private readonly NotificationService _notifySvc = new();
        private NotifyIcon _notifyIcon = new();
        private List<DocumentRecord> _allDocs = new();
        private List<DocumentRecord> _filteredDocs = new(); // Danh sách sau khi search
        private int _currentPage = 1;
        private int _pageSize = 20;
        private Label lblPageInfo = new();
        private Button btnPrev = new();
        private Button btnNext = new();

        // ════════════════════════════════════════════════════════════
        public Form1()
        {
            InitializeComponent();
            try { this.Icon = new Icon(@"asset\app_icon.ico"); } catch { }
            BuildUI();
            SetupTrayIcon();
            LoadData();
            _notifySvc.Initialize(_notifyIcon);
        }

        // ════════════════════════════════════════════════════════════
        // UI Construction
        // ════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.Text          = "Quản Lý Văn Bản - Hệ Thống Nhắc Nhở Deadline";
            this.Size          = new Size(1280, 760);
            this.MinimumSize   = new Size(1000, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = CBg;
            this.Font          = new Font("Segoe UI", 9.5f);
            try { this.Icon = new Icon(@"asset\app_icon.ico"); } catch { }
            this.AllowDrop     = true;

            this.DragEnter += (s, e) => {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += async (s, e) => {
                var paths = e.Data?.GetData(DataFormats.FileDrop) as string[];
                if (paths == null || paths.Length == 0) return;

                if (Directory.Exists(paths[0])) {
                    await OpenBatchImport(paths[0]);
                } else {
                    await OpenSingleAdd(paths[0]);
                }
            };

            // ── Status Bar (Bottom) ─────────────────────────────
            var pnlStatus = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 28,
                BackColor = Color.FromArgb(30, 41, 59)
            };
            var lblStatusBar = new Label
            {
                Text      = "  ✅  Hệ thống đang hoạt động  |  Nhắc nhở tự động: 7 ngày • 3 ngày • 1 ngày trước hạn  |  Dữ liệu lưu cục bộ",
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 8.5f)
            };
            pnlStatus.Controls.Add(lblStatusBar);

            // ── Header ──────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 84,
                BackColor = CHeaderBg
            };
            pnlHeader.Paint += (s, e) =>
            {
                // Accent line at bottom
                using var pen = new Pen(CAccent, 3);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 3, pnlHeader.Width, pnlHeader.Height - 3);
            };

            // --- Layout chính cho Header (Sử dụng TableLayoutPanel để chia 2 cột Trái/Phải) ---
            var tblHeader = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(20, 0, 0, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f)); // Cột trái cho Tiêu đề
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // Cột phải cho Account/Đồng hồ

            // --- Nhóm Tiêu đề (Bên trái) ---
            var pnlLeftHeader = new FlowLayoutPanel {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 10, 0, 0)
            };

            var picLogo = new PictureBox {
                Image = Image.FromFile(@"asset\app_logo.png"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(48, 48),
                Margin = new Padding(0, 0, 15, 0)
            };

            var pnlTitles = new FlowLayoutPanel {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            var lblTitle = new Label {
                Text = "HỆ THỐNG QUẢN LÝ VĂN BẢN HÀNH CHÍNH",
                ForeColor = CHeaderText,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblSub = new Label {
                Text = "Theo dõi thời hạn • Nhắc nhở tự động • Quản lý tập trung",
                ForeColor = Color.FromArgb(147, 197, 253),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                AutoSize = true,
                Margin = new Padding(4, 0, 0, 0)
            };
            pnlTitles.Controls.Add(lblTitle);
            pnlTitles.Controls.Add(lblSub);
            
            pnlLeftHeader.Controls.Add(picLogo);
            pnlLeftHeader.Controls.Add(pnlTitles);

            // --- Nhóm Thông tin (Bên phải) ---
            var pnlRightHeader = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            pnlRightHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlRightHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Thẻ Account (FlowLayoutPanel tự động dãn)
            var pnlUserPill = new FlowLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 10, 20, 2),
                Padding = new Padding(8, 6, 8, 8)
            };
            pnlUserPill.Paint += (s, e) => {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(Color.FromArgb(50, 255, 255, 255));
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                int r = 16;
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(pnlUserPill.Width - r - 1, 0, r, r, 270, 90);
                path.AddArc(pnlUserPill.Width - r - 1, pnlUserPill.Height - r - 1, r, r, 0, 90);
                path.AddArc(0, pnlUserPill.Height - r - 1, r, r, 90, 90);
                path.CloseFigure();
                e.Graphics.FillPath(brush, path);
            };

            var lblUser = new Label {
                Text = $"👤 {SessionService.CurrentUser?.Username} ({SessionService.CurrentUser?.Role})",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(2, 2, 8, 0)
            };

            var btnLogout = new Label {
                Text = "🚪 Đăng xuất",
                ForeColor = Color.FromArgb(254, 202, 202),
                Font = new Font("Segoe UI", 9f, FontStyle.Underline),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 2, 0)
            };
            btnLogout.Click += (s, e) => {
                SessionService.Logout();
                Application.Restart();
            };

            pnlUserPill.Controls.Add(lblUser);
            pnlUserPill.Controls.Add(btnLogout);
            pnlRightHeader.Controls.Add(pnlUserPill, 0, 0);

            // Đồng hồ bên dưới Thẻ Account
            lblClock = new Label {
                ForeColor = Color.FromArgb(147, 197, 253),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 0, 24, 0),
                BackColor = Color.Transparent
            };
            pnlRightHeader.Controls.Add(lblClock, 0, 1);

            // Chạy đồng hồ
            UpdateClock();
            var clockTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            clockTimer.Tick += (s, e) => UpdateClock();
            clockTimer.Start();

            // Ráp nối vào Header
            tblHeader.Controls.Add(pnlLeftHeader, 0, 0);
            tblHeader.Controls.Add(pnlRightHeader, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Stats Bar ────────────────────────────────────────
            var pnlStats = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 125,
                BackColor = Color.FromArgb(20, 52, 100),
                Padding   = new Padding(16, 12, 16, 12)
            };

            var statFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding       = new Padding(0),
                WrapContents  = false
            };

            (pnlStatTong, lblTong)     = CreateStatCard("📄  Tổng Văn Bản",        "0", Color.FromArgb(59, 130, 246));
            (pnlStatSap, lblSapHan)    = CreateStatCard("⏰  Sắp Hết Hạn (≤7 ngày)", "0", Color.FromArgb(245, 158, 11));
            (pnlStatQua, lblQuaHan)    = CreateStatCard("🚨  Quá Hạn",              "0", Color.FromArgb(239, 68, 68));
            (pnlStatHomNay, lblHomNay) = CreateStatCard("📅  Hôm Nay", DateTime.Today.ToString("dd/MM/yyyy"), Color.FromArgb(16, 185, 129));

            statFlow.Controls.AddRange(new Control[] { pnlStatTong, pnlStatSap, pnlStatQua, pnlStatHomNay });
            pnlStats.Controls.Add(statFlow);

            // ── Toolbar ──────────────────────────────────────────
            var pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 54,
                BackColor = CToolbar,
                Padding   = new Padding(14, 0, 14, 0)
            };
            pnlToolbar.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(CBorder, 1), 0, 0, pnlToolbar.Width, 0);
                e.Graphics.DrawLine(new Pen(CBorder, 1), 0, pnlToolbar.Height - 1, pnlToolbar.Width, pnlToolbar.Height - 1);
            };

            var btnAdd = MakeToolButton("➕  Thêm Văn Bản", Color.FromArgb(21, 128, 61));
            btnAdd.Click += BtnAdd_Click;

            var btnEdit = MakeToolButton("✏️  Xem / Sửa", Color.FromArgb(37, 99, 235));
            btnEdit.Click += BtnEdit_Click;

            var btnDelete = MakeToolButton("🗑  Xóa", Color.FromArgb(185, 28, 28));
            btnDelete.Click += BtnDelete_Click;

            var btnCalendar = MakeToolButton("📅  Tạo Lịch Nhắc", Color.FromArgb(124, 58, 237));
            btnCalendar.Click += BtnCalendar_Click;

            var btnOpenFile = MakeToolButton("📂  Mở File Gốc", Color.FromArgb(71, 85, 105));
            btnOpenFile.Click += BtnOpenFile_Click;

            var btnRefresh = MakeToolButton("🔄  Làm Mới", Color.FromArgb(71, 85, 105));
            btnRefresh.Click += (s, e) => LoadData();

            var btnImport = MakeToolButton("📥  Nhập Dữ Liệu", Color.FromArgb(71, 85, 105));
            btnImport.Click += BtnImport_Click;

            // Search box — dùng TextBox đơn giản với icon trong Paint để tránh bị che chữ
            var pnlSearch = new Panel
            {
                Width     = 240,
                Height    = 32,
                BackColor = CCard,
                Margin    = new Padding(12, 0, 0, 0)
            };
            pnlSearch.Paint += (s, e) =>
            {
                using var borderPen = new Pen(CBorder);
                e.Graphics.DrawRectangle(borderPen, new Rectangle(0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1));
                // Vẽ icon 🔍 như text để không tạo control chồng lên TextBox
                using var iconBrush = new SolidBrush(CMuted);
                using var iconFont  = new Font("Segoe UI", 10f);
                e.Graphics.DrawString("🔍", iconFont, iconBrush, new PointF(5, 6));
            };

            txtSearch = new TextBox
            {
                BorderStyle     = BorderStyle.None,
                BackColor       = CCard,
                ForeColor       = CText,
                Font            = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Tìm kiếm văn bản...",
                Location        = new Point(28, 7),
                Width           = 206
            };
            txtSearch.TextChanged += (s, e) => FilterData();
            pnlSearch.Controls.Add(txtSearch);

            var toolFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding       = new Padding(0, 11, 0, 0),
                WrapContents  = false
            };
            toolFlow.Controls.AddRange(new Control[]
            {
                btnAdd, btnEdit, btnDelete, btnCalendar, btnOpenFile, btnImport, btnRefresh, pnlSearch
            });
            pnlToolbar.Controls.Add(toolFlow);

            // Áp dụng phân quyền Main
            if (!SessionService.IsAdmin)
            {
                btnAdd.Visible = false;
                btnDelete.Visible = false;
                btnImport.Visible = false;
                btnEdit.Text = "👁  Xem Chi Tiết";
            }

            // ── DataGridView ─────────────────────────────────────
            var pnlGrid = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 10)
            };

            dgv = new DataGridView
            {
                Dock                           = DockStyle.Fill,
                BackgroundColor                = CBg,
                BorderStyle                    = BorderStyle.None,
                GridColor                      = CBorder,
                RowHeadersVisible              = false,
                AllowUserToAddRows             = false,
                AllowUserToDeleteRows          = false,
                AllowUserToResizeColumns       = true,
                AllowUserToResizeRows          = false,
                ReadOnly                       = true,
                SelectionMode                  = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                    = true,
                AutoSizeRowsMode               = DataGridViewAutoSizeRowsMode.AllCells,
                AutoSizeColumnsMode            = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode        = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowTemplate                    = { Height = 38 },
                Font                           = new Font("Segoe UI", 9.5f),
                CellBorderStyle                = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles      = false
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor  = Color.FromArgb(30, 41, 59),
                ForeColor  = Color.White,
                Font       = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Alignment  = DataGridViewContentAlignment.MiddleLeft,
                Padding    = new Padding(10, 8, 10, 8),
                SelectionBackColor = Color.FromArgb(30, 41, 59),
                SelectionForeColor = Color.White,
                WrapMode   = DataGridViewTriState.True
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = CText,
                Padding            = new Padding(8, 2, 8, 2),
                Font               = new Font("Segoe UI", 9.5f)
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.FromArgb(248, 250, 252),
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = CText
            };

            SetupGridColumns();

            dgv.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenDetail(e.RowIndex);
            };
            dgv.DataBindingComplete += DgvColorRows;
            dgv.SelectionChanged   += DgvColorRows;

            // Xử lý tự động dãn rộng cột Trích Yếu khi Form/Grid thay đổi kích thước
            dgv.Resize += (s, e) => AutoSizeTrichYeuColumn();
            dgv.ColumnWidthChanged += (s, e) => {
                if (e.Column.Name != "colTrichYeu") AutoSizeTrichYeuColumn();
            };

            pnlGrid.Controls.Add(dgv);

            // ── Pagination Panel (Bottom of Grid) ────────────────
            var pnlPagination = new Panel {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(0, 5, 20, 5)
            };
            pnlPagination.Paint += (s, e) => e.Graphics.DrawLine(new Pen(CBorder), 0, 0, pnlPagination.Width, 0);

            btnPrev = new Button { Text = "◀  Trang Trước", Width = 120, Height = 32, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.White };
            btnPrev.FlatAppearance.BorderColor = CBorder;
            btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; DisplayPage(); } };

            lblPageInfo = new Label { Text = "Trang 1 / 1", Width = 120, Height = 32, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

            btnNext = new Button { Text = "Trang Sau  ▶", Width = 120, Height = 32, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.White };
            btnNext.FlatAppearance.BorderColor = CBorder;
            btnNext.Click += (s, e) => { if (_currentPage < Math.Ceiling((double)_filteredDocs.Count / _pageSize)) { _currentPage++; DisplayPage(); } };

            pnlPagination.Controls.AddRange(new Control[] { btnNext, lblPageInfo, btnPrev });
            pnlGrid.Controls.Add(pnlPagination);

            // ── Assembly ─────────────────────────────────────────
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlStats);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlStatus);
        }

        // ════════════════════════════════════════════════════════════
        // Grid Columns
        // ════════════════════════════════════════════════════════════
        private void SetupGridColumns()
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "colStt",
                HeaderText = "STT",
                Width      = 48,
                SortMode   = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9f)
                }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name         = "colSoVb",
                HeaderText   = "Số Văn Bản",
                Width        = 180, // Có thể kéo dãn bằng tay
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name         = "colTrichYeu",
                HeaderText   = "Trích Yếu / Nội Dung",
                Width        = 350, // Khởi tạo, sẽ được tự động tính lại ở dưới
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "colNgayBH",
                HeaderText = "Ngày Ban Hành",
                Width      = 125,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "colChuQuan",
                HeaderText = "Cơ Quan Chủ Quản Tham Mưu",
                Width      = 210
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "colThoiHan",
                HeaderText = "Thời Hạn",
                Width      = 115,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "colTrangThai",
                HeaderText = "Trạng Thái",
                Width      = 145,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "colLich",
                HeaderText = "Lịch",
                Width      = 55,
                SortMode   = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font      = new Font("Segoe UI", 11f)
                }
            });
        }

        // ════════════════════════════════════════════════════════════
        // Data
        // ════════════════════════════════════════════════════════════
        private void LoadData()
        {
            _allDocs = DatabaseService.GetAll();
            FilterData();
            UpdateStats();
        }

        private void FilterData()
        {
            string q = txtSearch.Text.Trim().ToLower();
            _filteredDocs = string.IsNullOrEmpty(q)
                ? _allDocs
                : _allDocs.Where(d =>
                    (d.SoVanBan       ?? "").ToLower().Contains(q) ||
                    (d.TrichYeu       ?? "").ToLower().Contains(q) ||
                    (d.CoQuanBanHanh  ?? "").ToLower().Contains(q) ||
                    (d.CoQuanChuQuan  ?? "").ToLower().Contains(q) ||
                    (d.DonViChiDao    ?? "").ToLower().Contains(q)).ToList();

            _currentPage = 1; // Reset về trang đầu khi tìm kiếm
            DisplayPage();
        }

        private void DisplayPage()
        {
            dgv.Rows.Clear();
            
            int totalItems = _filteredDocs.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / _pageSize);
            if (totalPages == 0) totalPages = 1;
            if (_currentPage > totalPages) _currentPage = totalPages;

            lblPageInfo.Text = $"Trang {_currentPage} / {totalPages}";
            btnPrev.Enabled = (_currentPage > 1);
            btnNext.Enabled = (_currentPage < totalPages);

            var pageItems = _filteredDocs.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();

            int stt = ((_currentPage - 1) * _pageSize) + 1;
            foreach (var doc in pageItems)
            {
                int idx = dgv.Rows.Add(
                    stt++,
                    doc.SoVanBan,
                    doc.TrichYeu,
                    doc.NgayBanHanh?.ToString("dd/MM/yyyy") ?? "—",
                    doc.CoQuanChuQuan,
                    (doc.ThoiHan?.ToString("dd/MM/yyyy") ?? "Chưa có") + 
                    (doc.AdditionalDeadlines?.Count > 0 ? $" (+{doc.AdditionalDeadlines.Count})" : ""),
                    GetTrangThaiText(doc),
                    doc.DaTaoLich ? "✅" : "—"
                );
                dgv.Rows[idx].Tag = doc;
            }
            DgvColorRows(null, null!);
        }

        private string GetTrangThaiText(DocumentRecord doc)
        {
            if (doc.ThoiHan == null) return "Chưa xác định";
            int d = doc.SoNgayConLai;
            if (d < 0)   return $"🚨 Quá hạn {Math.Abs(d)} ngày";
            if (d == 0)  return "⚡ Hết hạn hôm nay!";
            if (d <= 3)  return $"⚠️ Còn {d} ngày";
            if (d <= 7)  return $"⏰ Còn {d} ngày";
            return $"✅ Còn {d} ngày";
        }

        private void UpdateStats()
        {
            int tong   = _allDocs.Count;
            int sap    = _allDocs.Count(d => d.SoNgayConLai is >= 1 and <= 7);
            int qua    = _allDocs.Count(d => d.SoNgayConLai < 0);
            int homNay = _allDocs.Count(d => d.SoNgayConLai == 0);

            lblTong.Text   = tong.ToString();
            lblSapHan.Text = sap.ToString();
            lblQuaHan.Text = qua.ToString();
            lblHomNay.Text = homNay.ToString();

            // Highlight if urgent
            pnlStatQua.BackColor  = qua > 0
                ? Color.FromArgb(239, 68, 68)
                : Color.FromArgb(50, 70, 100);
            pnlStatSap.BackColor  = sap > 0
                ? Color.FromArgb(217, 119, 6)
                : Color.FromArgb(50, 70, 100);
            pnlStatHomNay.BackColor = homNay > 0
                ? Color.FromArgb(239, 68, 68)
                : Color.FromArgb(50, 70, 100);
        }

        private void DgvColorRows(object? sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Tag is not DocumentRecord doc) continue;
                int days = doc.SoNgayConLai;

                Color bg, fg;
                if      (days < 0)  { bg = CRowDanger; fg = CRowDangerTxt; }
                else if (days == 0) { bg = Color.FromArgb(252, 165, 165); fg = Color.FromArgb(127, 29, 29); }
                else if (days <= 3) { bg = CRowAlert;  fg = CRowAlertTxt; }
                else if (days <= 7) { bg = CRowWarn;   fg = CRowWarnTxt; }
                else                { bg = CRowOk;     fg = CRowOkTxt; }

                row.DefaultCellStyle.BackColor      = bg;
                row.DefaultCellStyle.ForeColor      = fg;
                
                // Giữ nguyên màu gốc khi được chọn, chỉ làm đậm chữ
                row.DefaultCellStyle.SelectionBackColor = ControlPaint.Dark(bg, 0.05f); // Chỉ hơi sậm lại 5% để phân biệt
                row.DefaultCellStyle.SelectionForeColor = fg;

                // Tạo hiệu ứng đậm chữ khi dòng được chọn (Focus)
                if (row.Selected)
                {
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else
                {
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        // Actions
        // ════════════════════════════════════════════════════════════
        private async void BtnAdd_Click(object? sender, EventArgs e)
        {
            await OpenSingleAdd();
        }

        private async Task OpenSingleAdd(string initialPath = null)
        {
            using var form = new FormAddDocument();
            if (!string.IsNullOrEmpty(initialPath)) {
                // Manually trigger load if path provided
                // This logic is mostly handled inside FormAddDocument.LoadFile
                // but we call it here for drag-drop flow
            }

            var res = form.ShowDialog(this);
            if (res == DialogResult.OK && form.Result != null)
            {
                int id = DatabaseService.Insert(form.Result);
                form.Result.Id = id;
                LoadData();

                // Highlight newly added
                foreach (DataGridViewRow row in dgv.Rows)
                    if (row.Tag is DocumentRecord d && d.SoVanBan == form.Result.SoVanBan)
                        row.Selected = true;

                ShowSuccessToast($"Đã lưu văn bản «{form.Result.SoVanBan}» thành công!");
            }
            else if (res == DialogResult.Retry && form.Tag is string folderPath)
            {
                // Pivot to batch!
                await OpenBatchImport(folderPath);
            }
        }

        private void BtnImport_Click(object? sender, EventArgs e)
        {
            _ = OpenBatchImport();
        }

        private async Task OpenBatchImport(string initialFolder = null)
        {
            using var form = new FormBatchImport();
            if (!string.IsNullOrEmpty(initialFolder))
            {
                // Biến form.ShowDialog() là chặn (blocking), nên ta cần chạy scan sau khi form load
                form.Load += async (s, e) => await form.ScanFolderAsync(initialFolder);
            }

            if (form.ShowDialog(this) == DialogResult.OK && form.Results != null && form.Results.Count > 0)
            {
                int successCount = 0;
                foreach (var doc in form.Results)
                {
                    if (doc.SoVanBan == "LỖI") continue;
                    try {
                        DatabaseService.Insert(doc);
                        successCount++;
                    } catch { }
                }

                LoadData();
                ShowSuccessToast($"Đã nhập thành công {successCount} văn bản vào hệ thống!");
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var (doc, _) = GetSelectedDoc();
            if (doc == null)
            {
                ShowInfo("Vui lòng chọn một văn bản để xem / chỉnh sửa.");
                return;
            }
            OpenDetail(-1, doc);
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                ShowInfo("Vui lòng chọn ít nhất một văn bản để xóa.\n(Gợi ý: Nhấn Ctrl+A để chọn tất cả hoặc giữ phím Ctrl/Shift để chọn nhiều file giống như Excel).");
                return;
            }

            var selectedDocs = new List<DocumentRecord>();
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                if (row.Tag is DocumentRecord rec)
                {
                    selectedDocs.Add(rec);
                }
            }

            if (selectedDocs.Count == 0) return;

            string title = "Xác nhận xóa văn bản";
            string msg = selectedDocs.Count == 1 
                ? $"Bạn có chắc chắn muốn xóa văn bản:\n\n  «{selectedDocs[0].SoVanBan}»\n\nThao tác này không thể hoàn tác!"
                : $"Bạn có chắc chắn muốn xóa {selectedDocs.Count} văn bản đã chọn?\n\nThao tác này không thể hoàn tác!";

            using var confirm = new FormConfirm(
                title,
                msg,
                "🗑  Xóa", Color.FromArgb(185, 28, 28));

            if (confirm.ShowDialog(this) == DialogResult.OK)
            {
                foreach (var doc in selectedDocs)
                {
                    DatabaseService.Delete(doc.Id);
                }
                LoadData();
                ShowSuccessToast($"Đã xóa thành công {selectedDocs.Count} văn bản.");
            }
        }

        private void BtnCalendar_Click(object? sender, EventArgs e)
        {
            var (doc, _) = GetSelectedDoc();
            if (doc == null)
            {
                ShowInfo("Vui lòng chọn một văn bản để tạo lịch nhắc.");
                return;
            }
            if (doc.ThoiHan == null)
            {
                MessageBox.Show("Văn bản này không có thời hạn. Không thể tạo lịch.",
                    "Không có thời hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                CalendarService.CreateCalendarEvents(doc);
                doc.DaTaoLich = true;
                DatabaseService.Update(doc);
                LoadData();
                ShowSuccessToast($"Đã tạo lịch nhắc nhở cho «{doc.SoVanBan}» (7, 3, 1 ngày trước hạn)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo lịch:\n{ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOpenFile_Click(object? sender, EventArgs e)
        {
            var (doc, _) = GetSelectedDoc();
            if (doc == null) { ShowInfo("Vui lòng chọn một văn bản."); return; }

            if (string.IsNullOrEmpty(doc.FilePath) || !File.Exists(doc.FilePath))
            {
                MessageBox.Show("Không tìm thấy file gốc.\nFile có thể đã bị di chuyển hoặc xóa.",
                    "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName       = doc.FilePath,
                UseShellExecute = true
            });
        }

        private void OpenDetail(int rowIndex, DocumentRecord? doc = null)
        {
            if (doc == null)
            {
                var (d, _) = GetSelectedDoc();
                doc = d;
            }
            if (doc == null) return;

            using var detail = new FormDetail(doc);
            if (detail.ShowDialog(this) == DialogResult.OK && detail.UpdatedRecord != null)
            {
                DatabaseService.Update(detail.UpdatedRecord);
                LoadData();
                ShowSuccessToast("Đã cập nhật thông tin văn bản.");
            }
        }

        // ════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════
        private (DocumentRecord? doc, int rowIndex) GetSelectedDoc()
        {
            if (dgv.SelectedRows.Count == 0) return (null, -1);
            var row = dgv.SelectedRows[0];
            return (row.Tag as DocumentRecord, row.Index);
        }

        private void UpdateClock()
        {
            lblClock.Text = $"🕐  {DateTime.Now:HH:mm}  |  {DateTime.Now:dddd, dd/MM/yyyy}";
            // Reposition
            var pnlH = lblClock.Parent;
            if (pnlH != null)
                lblClock.Location = new Point(pnlH.Width - lblClock.Width - 24, 26);
        }

        private void ShowInfo(string msg) =>
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private void ShowSuccessToast(string msg)
        {
            _notifyIcon.ShowBalloonTip(3000, "✅ Thành công", msg, ToolTipIcon.Info);
        }

        // ── Stat Card ────────────────────────────────────────────
        // Vẽ trực tiếp qua Paint để tránh label bị clip hoặc chồng lên nhau
        private (Panel panel, Label valueLabel) CreateStatCard(string caption, string value, Color accent)
        {
            var pnl = new Panel
            {
                Width     = 280,
                Height    = 95,
                BackColor = Color.FromArgb(50, 70, 100),
                Margin    = new Padding(0, 0, 10, 0),
                Cursor    = Cursors.Default
            };

            var lblCaption = new Label
            {
                Text      = caption,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font      = new Font("Segoe UI", 10f),
                AutoSize  = false,
                UseCompatibleTextRendering = true,
                Width     = 265,
                Height    = 28,
                Location  = new Point(14, 12),
                TextAlign = ContentAlignment.TopLeft
            };

            // Label giá trị lớn nằm ở NỬA MẶT DƯỚI BÊN PHẢI (Top/Bottom layout)
            var lblValue = new Label
            {
                Text      = value,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize  = false,
                UseCompatibleTextRendering = true,
                Width     = 260,
                Height    = 46,
                Location  = new Point(14, 40),
                TextAlign = ContentAlignment.TopRight
            };

            pnl.Controls.Add(lblCaption);
            pnl.Controls.Add(lblValue);

            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Accent border trái
                using var accentPen = new Pen(accent, 4);
                g.DrawLine(accentPen, 2, 4, 2, pnl.Height - 4);

                // Glow nhẹ phía trên
                using var glowBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, pnl.Width, 4),
                    Color.FromArgb(50, accent.R, accent.G, accent.B),
                    Color.Transparent, 90f);
                g.FillRectangle(glowBrush, 0, 0, pnl.Width, 4);
            };

            return (pnl, lblValue);
        }

        // ── Tool Button ──────────────────────────────────────────
        private Button MakeToolButton(string text, Color color)
        {
            var btn = new Button
            {
                Text       = text,
                BackColor  = color,
                ForeColor  = Color.White,
                FlatStyle  = FlatStyle.Flat,
                Font       = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height     = 32,
                AutoSize   = true,
                Cursor     = Cursors.Hand,
                Margin     = new Padding(0, 0, 6, 0),
                FlatAppearance =
                {
                    BorderSize           = 0,
                    MouseOverBackColor   = ControlPaint.Light(color, 0.15f),
                    MouseDownBackColor   = ControlPaint.Dark(color, 0.1f)
                }
            };
            return btn;
        }

        // ════════════════════════════════════════════════════════════
        // System Tray
        // ════════════════════════════════════════════════════════════
        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Text    = "Quản Lý Văn Bản - Nhắc Nhở Deadline",
                Icon    = this.Icon,
                Visible = true
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("📋  Mở cửa sổ chính", null, (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            });
            menu.Items.Add("➕  Thêm văn bản mới", null, BtnAdd_Click);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("❌  Thoát", null, (s, e) =>
            {
                _notifyIcon.Visible = false;
                Application.Exit();
            });

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            };

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                    _notifyIcon.ShowBalloonTip(3000, "Quản Lý Văn Bản",
                        "Ứng dụng vẫn chạy nền. Double-click icon để mở lại.",
                        ToolTipIcon.Info);
                }
            };
        }

        private void AutoSizeTrichYeuColumn()
        {
            if (dgv.Columns.Count == 0 || !dgv.Columns.Contains("colTrichYeu")) return;

            // Tính tổng độ rộng của tất cả các cột NGOẠI TRỪ cột Trích yếu
            int otherColumnsWidth = 0;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Name != "colTrichYeu" && col.Visible)
                {
                    otherColumnsWidth += col.Width;
                }
            }

            // Độ rộng khả dụng của Grid (trừ đi khoảng cho scrollbar nếu có)
            int availableWidth = dgv.ClientSize.Width - 2; 
            
            // Cập nhật độ rộng cho cột Trích yếu
            int newWidth = availableWidth - otherColumnsWidth;
            if (newWidth > 200) // Đảm bảo không quá nhỏ
            {
                var col = dgv.Columns["colTrichYeu"];
                if (col != null)
                {
                    col.Width = newWidth;
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _notifySvc.Dispose();
            _notifyIcon.Dispose();
            base.OnFormClosed(e);
        }
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
    }
}
