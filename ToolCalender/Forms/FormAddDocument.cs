using ToolCalender.Models;
using ToolCalender.Services;

namespace ToolCalender.Forms
{
    public class FormAddDocument : Form
    {
        // ── Color Palette ────────────────────────────────────────
        private static readonly Color CHeader     = Color.FromArgb(30, 58, 95);
        private static readonly Color CAccent     = Color.FromArgb(37, 99, 235);
        private static readonly Color CBg         = Color.FromArgb(241, 245, 249);
        private static readonly Color CCard       = Color.White;
        private static readonly Color CLabel      = Color.FromArgb(51, 65, 85);
        private static readonly Color CBorder     = Color.FromArgb(203, 213, 225);
        private static readonly Color CSectionBg  = Color.FromArgb(248, 250, 252);
        private static readonly Color CSectionHdr = Color.FromArgb(224, 231, 242);
        private static readonly Color CDanger     = Color.FromArgb(254, 226, 226);
        private static readonly Color CDangerText = Color.FromArgb(153, 27, 27);
        private static readonly Color CWarning    = Color.FromArgb(254, 243, 199);
        private static readonly Color CWarningTxt = Color.FromArgb(120, 53, 15);
        private static readonly Color COk         = Color.FromArgb(209, 250, 229);
        private static readonly Color COkText     = Color.FromArgb(6, 95, 70);
        private static readonly Color CAlert      = Color.FromArgb(255, 237, 213);
        private static readonly Color CAlertText  = Color.FromArgb(154, 52, 18);
        private static readonly Color CMuted      = Color.FromArgb(100, 116, 139);

        // ── Controls ─────────────────────────────────────────────
        private Panel      pnlDropZone   = new();
        private Label      lblDropHint   = new();
        private Label      lblFileName   = new();
        private Label      lblStatus     = new();
        private Panel      grpInfo       = new();
        private Panel      grpDeadline   = new();
        private Panel      pnlDeadline   = new();
        private Label      lblDeadlineTxt = new();

        private TextBox    txtSoVanBan    = new();
        private TextBox    txtTrichYeu    = new();
        private DateTimePicker dtpNgayBanHanh = new();
        private TextBox    txtCoQuanBH    = new();
        private TextBox    txtChuQuan     = new();   // Cơ quan chủ quản tham mưu
        private DateTimePicker dtpThoiHan = new();
        private TextBox    txtDonViChiDao = new();
        private List<DateTime> _extraDeadlines = new();
        private FlowLayoutPanel pnlExtraDeadlines = new();
        private CheckBox   chkKhongThoiHan = new();

        private Button btnSaveCalendar = new();
        private Button btnSaveOnly     = new();
        private Button btnCancel       = new();

        public DocumentRecord? Result { get; private set; }
        private string _filePath = "";

        // ════════════════════════════════════════════════════════
        public FormAddDocument()
        {
            BuildUI();
        }

        public FormAddDocument(DocumentRecord prefilledData)
        {
            BuildUI();
            this.Result = prefilledData;
            _filePath   = prefilledData.FilePath;
            
            txtSoVanBan.Text   = prefilledData.SoVanBan;
            txtTrichYeu.Text   = prefilledData.TrichYeu;
            txtCoQuanBH.Text   = prefilledData.CoQuanBanHanh;
            txtChuQuan.Text    = prefilledData.CoQuanChuQuan;
            txtDonViChiDao.Text = prefilledData.DonViChiDao;
            
            if (prefilledData.NgayBanHanh.HasValue) 
                dtpNgayBanHanh.Value = prefilledData.NgayBanHanh.Value;
            
            if (prefilledData.ThoiHan.HasValue) 
                dtpThoiHan.Value = prefilledData.ThoiHan.Value;

            lblStatus.Text = "Đã bóc tách dữ liệu tự động. Vui lòng kiểm tra lại trước khi lưu.";
            lblStatus.ForeColor = Color.DarkGreen;
        }

        // ════════════════════════════════════════════════════════
        // UI Construction
        // ════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.Text              = "Thêm Văn Bản Mới";
            this.Size              = new Size(1000, 850);
            this.MinimumSize       = new Size(950, 750);
            this.StartPosition     = FormStartPosition.CenterParent;
            this.BackColor         = CBg;
            this.Font              = new Font("Segoe UI", 9.5f);
            this.FormBorderStyle   = FormBorderStyle.Sizable;
            this.MaximizeBox       = true;
            this.AutoScroll        = false;

            // ── Header ──────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 75,
                BackColor = CHeader
            };
            pnlHeader.Paint += (s, e) =>
            {
                // Bottom gradient line
                using var pen = new Pen(Color.FromArgb(37, 99, 235), 3);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 3, pnlHeader.Width, pnlHeader.Height - 3);
            };

            var lblTitle = new Label
            {
                Text      = "📄  THÊM VĂN BẢN MỚI",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                AutoSize  = false,
                UseCompatibleTextRendering = true,
                Width     = 400,
                Height    = 35,
                Location  = new Point(20, 10)
            };
            var lblSubTitle = new Label
            {
                Text      = "Tải lên và nhập thông tin hành chính - Hệ thống sẽ tự động nhắc nhở khi đến hạn",
                ForeColor = Color.FromArgb(147, 197, 253),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                AutoSize  = false,
                UseCompatibleTextRendering = true,
                Width     = 800,
                Height    = 25,
                Location  = new Point(22, 45)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubTitle });

            // ── Buttons ──────────────────────────────────────────
            var pnlButtons = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 80,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding   = new Padding(20, 12, 20, 0)
            };
            pnlButtons.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(CBorder), 0, 0, pnlButtons.Width, 0);

            btnSaveCalendar = MakeButton("💾  Lưu & Tạo Lịch Nhắc", Color.FromArgb(21, 128, 61), Color.White);
            btnSaveCalendar.Size   = new Size(240, 42);
            btnSaveCalendar.Left   = 20;
            btnSaveCalendar.Top    = 15;
            btnSaveCalendar.Click += BtnSaveCalendar_Click;

            btnSaveOnly = MakeButton("📋  Chỉ Lưu", CAccent, Color.White);
            btnSaveOnly.Size   = new Size(140, 42);
            btnSaveOnly.Left   = 275;
            btnSaveOnly.Top    = 15;
            btnSaveOnly.Click += BtnSaveOnly_Click;

            btnCancel = MakeButton("✖  Hủy", Color.FromArgb(100, 116, 139), Color.White);
            btnCancel.Size   = new Size(120, 42);
            btnCancel.Left   = 430;
            btnCancel.Top    = 15;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Tooltip hint
            var lblBtnHint = new Label
            {
                Text      = "💡 \"Lưu & Tạo Lịch\" sẽ thêm sự kiện nhắc nhở vào Windows Calendar (7, 3, 1 ngày trước hạn)",
                ForeColor = Color.FromArgb(100, 116, 139),
                Font      = new Font("Segoe UI", 8f, FontStyle.Italic),
                AutoSize  = true,
                Location  = new Point(20, 60)
            };

            pnlButtons.Controls.AddRange(new Control[] { btnSaveCalendar, btnSaveOnly, btnCancel, lblBtnHint });

            // ── Scroll panel (main content) ──────────────────────
            var pnlScroll = new Panel
            {
                Dock      = DockStyle.Fill,
                AutoScroll = true,
                Padding   = new Padding(20, 12, 20, 10)
            };

            // ── 1. Upload Zone ───────────────────────────────────
            var grpUpload = MakeSectionPanel("📂  BƯỚC 1: TẢI FILE VĂN BẢN");

            pnlDropZone = new Panel
            {
                Height      = 140,
                Dock        = DockStyle.Top,
                BackColor   = Color.FromArgb(239, 246, 255),
                Cursor      = Cursors.Hand,
                Margin      = new Padding(0, 6, 0, 4)
            };
            pnlDropZone.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, pnlDropZone.Width - 1, pnlDropZone.Height - 1);
                using var pen = new Pen(Color.FromArgb(147, 196, 250), 2);
                // Draw dashed border
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.DrawRectangle(pen, rect);
            };
            pnlDropZone.AllowDrop = true;
            pnlDropZone.DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                {
                    e.Effect = DragDropEffects.Copy;
                    pnlDropZone.BackColor = Color.FromArgb(219, 234, 254);
                }
            };
            pnlDropZone.DragLeave += (s, e) => pnlDropZone.BackColor = Color.FromArgb(239, 246, 255);
            pnlDropZone.Click += (s, e) => { /* Click handled by buttons inside */ };
            
            lblDropHint = new Label
            {
                Text      = "📁  Kéo & thả File / Thư mục vào đây  hoặc",
                TextAlign = ContentAlignment.TopCenter,
                Dock      = DockStyle.Top,
                Height    = 35,
                ForeColor = Color.FromArgb(71, 116, 185),
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 25, 0, 0)
            };
            
            var btnBrowseFile = new Button {
                Text = "📂  Chọn File",
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                MinimumSize = new Size(160, 42),
                BackColor = Color.White,
                ForeColor = CAccent,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatAppearance = { BorderColor = CAccent },
                Padding = new Padding(15, 0, 15, 0)
            };
            btnBrowseFile.Click += (s, e) => BrowseFile();

            var lblOr = new Label { Text = "hoặc", AutoSize = true, Font = new Font("Segoe UI", 10f), ForeColor = CMuted, Margin = new Padding(12, 10, 12, 0) };

            var btnBrowseFolder = new Button {
                Text = "📁  Chọn Thư Mục",
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                MinimumSize = new Size(180, 42),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(21, 128, 61),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatAppearance = { BorderColor = Color.FromArgb(21, 128, 61) },
                Padding = new Padding(15, 0, 15, 0)
            };
            btnBrowseFolder.Click += (s, e) => BrowseFolder();

            var pnlBtnContainer = new FlowLayoutPanel {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.Top,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 0),
                WrapContents = false
            };
            
            // Center the buttons
            pnlDropZone.Resize += (s, e) => {
                pnlBtnContainer.Left = (pnlDropZone.Width - pnlBtnContainer.Width) / 2;
                if (pnlBtnContainer.Left < 0) pnlBtnContainer.Left = 0;
            };

            pnlBtnContainer.Controls.AddRange(new Control[] { btnBrowseFile, lblOr, btnBrowseFolder });
            
            pnlDropZone.Controls.Add(pnlBtnContainer);
            pnlDropZone.Controls.Add(lblDropHint);
            
            var lblFormatHint = new Label {
                Text = "Định dạng hỗ trợ: PDF (.pdf)  |  Word (.docx, .doc)",
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = CMuted,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };
            pnlDropZone.Controls.Add(lblFormatHint);

            pnlDropZone.DragDrop  += async (s, e) =>
            {
                pnlDropZone.BackColor = Color.FromArgb(239, 246, 255);
                var paths = e.Data?.GetData(DataFormats.FileDrop) as string[];
                if (paths == null || paths.Length == 0) return;

                string firstPath = paths[0];
                if (Directory.Exists(firstPath))
                {
                    // Detect directory, pivot to batch import
                    this.Tag = firstPath; // Marker to signal batch import to parent
                    this.DialogResult = DialogResult.Retry; // Custom result to trigger batch in Form1
                    this.Close();
                }
                else
                {
                    LoadFile(firstPath);
                }
            };

            lblFileName = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(21, 128, 61),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold | FontStyle.Italic),
                Height    = 22,
                Dock      = DockStyle.Top,
                Margin    = new Padding(0, 2, 0, 0)
            };

            var uploadFlow = new Panel
            {
                AutoSize      = true,
                Dock          = DockStyle.Top,
                Padding       = new Padding(0)
            };
            uploadFlow.Controls.Add(lblFileName);
            uploadFlow.Controls.Add(pnlDropZone);
            
            // Re-order Z-index to dock correctly top-down: pnlDropZone THEN lblFileName
            pnlDropZone.BringToFront();
            lblFileName.BringToFront();

            grpUpload.Controls.Add(uploadFlow);
            uploadFlow.BringToFront();

            // ── 2. Thông tin văn bản ─────────────────────────────
            grpInfo = MakeSectionPanel("📋  BƯỚC 2: THÔNG TIN VĂN BẢN");
            grpInfo.Visible = false;

            var tbl = new TableLayoutPanel
            {
                ColumnCount = 4,
                AutoSize    = true,
                Dock        = DockStyle.Top,
                Padding     = new Padding(0, 6, 0, 10)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            int r = 0;

            // Row 0: Số VB | Ngày ban hành
            AddLabelCtrl(tbl, r, 0, "Số văn bản  (*)", txtSoVanBan = MakeTxt());
            AddLabelCtrl(tbl, r, 2, "Ngày ban hành  (*)", dtpNgayBanHanh = MakeDtp());
            r++;

            // Row 1: Cơ quan ban hành | Cơ quan chủ quản tham mưu
            AddLabelCtrl(tbl, r, 0, "Cơ quan ban hành", txtCoQuanBH = MakeTxt());
            AddLabelCtrl(tbl, r, 2, "Cơ quan chủ quản tham mưu  (*)", txtChuQuan = MakeTxt());
            r++;

            // Row 2: Thời hạn | Đơn vị được chỉ đạo
            AddLabelCtrl(tbl, r, 0, "Thời hạn / Ngày đến hạn  (*)", dtpThoiHan = MakeDtp());
            dtpThoiHan.ValueChanged += (s, e) => UpdateDeadlinePreview();
            AddLabelCtrl(tbl, r, 2, "Đơn vị được chỉ đạo", txtDonViChiDao = MakeTxt());
            r++;

            // Row 3: Không có thời hạn checkbox
            chkKhongThoiHan = new CheckBox
            {
                Text      = "Văn bản không có thời hạn cụ thể",
                ForeColor = Color.FromArgb(100, 116, 139),
                Font      = new Font("Segoe UI", 9f, FontStyle.Italic),
                Margin    = new Padding(0, 2, 0, 4),
                AutoSize  = true
            };
            chkKhongThoiHan.CheckedChanged += (s, e) =>
            {
                dtpThoiHan.Enabled = !chkKhongThoiHan.Checked;
                pnlDeadline.Visible = !chkKhongThoiHan.Checked;
                if (!chkKhongThoiHan.Checked) UpdateDeadlinePreview();
            };
            tbl.Controls.Add(chkKhongThoiHan, 1, r);
            tbl.SetColumnSpan(chkKhongThoiHan, 3);
            r++;

            // Row 4: Empty row for spacing
            r++;

            grpInfo.Controls.Add(tbl);
            tbl.BringToFront();

            // ── Trích yếu (Full Width outside table) ────────────
            var pnlTrichYeu = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 160,
                Padding = new Padding(10, 5, 5, 0)
            };
            var lblTrichYeuTitle = new Label
            {
                Text      = "Trích yếu / Nội dung chính của văn bản:",
                ForeColor = CLabel,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock      = DockStyle.Top,
                Height    = 22
            };
            txtTrichYeu = new TextBox
            {
                Multiline     = true,
                AcceptsReturn = true,
                Dock          = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = CCard,
                ForeColor   = CLabel,
                Font        = new Font("Segoe UI", 10f),
                ScrollBars  = ScrollBars.Vertical
            };
            pnlTrichYeu.Controls.Add(txtTrichYeu);
            pnlTrichYeu.Controls.Add(lblTrichYeuTitle);
            
            grpInfo.Controls.Add(pnlTrichYeu);
            pnlTrichYeu.BringToFront();

            // ── 3. Các mốc thời hạn bổ sung (NEW) ────────────────
            var grpExtra = MakeSectionPanel("⏰  CÁC MỐC THỜI HẠN BỔ SUNG (Milestones)");
            var pnlExtraTop = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(0, 5, 0, 5) };
            
            var btnAddDate = MakeButton("➕  Thêm mốc thời gian", Color.FromArgb(51, 65, 85), Color.White);
            btnAddDate.Size = new Size(180, 32);
            btnAddDate.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnAddDate.Location = new Point(10, 6);
            btnAddDate.Click += (s, e) => {
                using var dtpDlg = new Form();
                dtpDlg.Text = "Chọn mốc thời gian bổ sung";
                dtpDlg.Size = new Size(300, 150);
                dtpDlg.StartPosition = FormStartPosition.CenterParent;
                var dtp = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Location = new Point(20, 20), Width = 240 };
                var btnOk = new Button { Text = "Thêm", Location = new Point(180, 60), DialogResult = DialogResult.OK };
                dtpDlg.Controls.AddRange(new Control[] { dtp, btnOk });
                if (dtpDlg.ShowDialog() == DialogResult.OK) {
                    AddDeadlinePill(dtp.Value);
                }
            };
            pnlExtraTop.Controls.Add(btnAddDate);
            grpExtra.Controls.Add(pnlExtraTop);

            pnlExtraDeadlines = new FlowLayoutPanel {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10, 0, 10, 10),
                AutoScroll = false
            };
            grpExtra.Controls.Add(pnlExtraDeadlines);
            pnlExtraDeadlines.BringToFront();
            pnlExtraTop.BringToFront();

            // ── 4. Preview thông báo deadline ────────────────────
            grpDeadline = MakeSectionPanel("🔔  BƯỚC 4: THÔNG BÁO ĐẾN HẠN");
            grpDeadline.Visible = false;

            pnlDeadline = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 95,
                BackColor = COk,
                Margin    = new Padding(0, 6, 0, 4),
                Padding   = new Padding(16, 10, 16, 10)
            };
            pnlDeadline.Paint += (s, e) =>
            {
                using var pen = new Pen(CBorder);
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, pnlDeadline.Width - 1, pnlDeadline.Height - 1));
            };

            lblDeadlineTxt = new Label
            {
                Dock      = DockStyle.Fill,
                ForeColor = COkText,
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text      = "⏳  Chọn thời hạn để xem trạng thái..."
            };
            pnlDeadline.Controls.Add(lblDeadlineTxt);

            var pnlNotifyInfo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 34,
                BackColor = Color.FromArgb(239, 246, 255),
                Margin    = new Padding(0, 2, 0, 0)
            };
            pnlNotifyInfo.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(186, 214, 250));
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, pnlNotifyInfo.Width - 1, pnlNotifyInfo.Height - 1));
            };
            var lblNotifyInfo = new Label
            {
                Text      = "🔔  Khi lưu & tạo lịch, hệ thống sẽ tự động nhắc nhở: " +
                            "7 ngày trước hạn  •  3 ngày trước hạn  •  1 ngày trước hạn",
                ForeColor = Color.FromArgb(37, 99, 235),
                Font      = new Font("Segoe UI", 8.5f),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlNotifyInfo.Controls.Add(lblNotifyInfo);

            var deadlineFlow = new Panel
            {
                AutoSize      = true,
                Dock          = DockStyle.Top,
                Padding       = new Padding(0)
            };
            deadlineFlow.Controls.Add(pnlNotifyInfo);
            deadlineFlow.Controls.Add(pnlDeadline);

            pnlDeadline.BringToFront();
            pnlNotifyInfo.BringToFront();

            grpDeadline.Controls.Add(deadlineFlow);
            deadlineFlow.BringToFront();

            // ── Status bar ───────────────────────────────────────
            lblStatus = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(21, 128, 61),
                Font      = new Font("Segoe UI", 9f),
                Height    = 40,
                Dock      = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            };

            // ── Stack all sections ───────────────────────────────
            var mainStack = new Panel
            {
                Dock      = DockStyle.Top,
                AutoSize  = true,
                Padding   = new Padding(0)
            };

            grpUpload.Dock   = DockStyle.Top;
            grpInfo.Dock     = DockStyle.Top;
            grpExtra.Dock    = DockStyle.Top;
            grpDeadline.Dock = DockStyle.Top;

            mainStack.Controls.Add(lblStatus);
            mainStack.Controls.Add(grpDeadline);
            mainStack.Controls.Add(grpExtra);
            mainStack.Controls.Add(grpInfo);
            mainStack.Controls.Add(grpUpload);
            
            grpUpload.BringToFront();
            grpInfo.BringToFront();
            grpExtra.BringToFront();
            grpDeadline.BringToFront();
            lblStatus.BringToFront();

            pnlScroll.Controls.Add(mainStack);

            this.Controls.Add(pnlScroll);
            this.Controls.Add(pnlButtons);
            this.Controls.Add(pnlHeader);

            // Initial deadline preview
            UpdateDeadlinePreview();
        }

        // ════════════════════════════════════════════════════════
        // Section Panel Builder
        // ════════════════════════════════════════════════════════
        private Panel MakeSectionPanel(string title)
        {
            var pnl = new Panel
            {
                AutoSize    = true,
                BackColor   = CCard,
                Margin      = new Padding(0, 0, 0, 12),
                Padding     = new Padding(14, 0, 14, 12) // Removed top padding internally, lblTitle will provide it naturally
            };
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(CBorder);
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1));

                using var hdrBrush = new SolidBrush(CSectionHdr);
                e.Graphics.FillRectangle(hdrBrush, 0, 0, pnl.Width, 34);

                using var borderPen = new Pen(Color.FromArgb(37, 99, 235), 3);
                e.Graphics.DrawLine(borderPen, 0, 0, 0, 34);
            };

            var lblTitle = new Label
            {
                Text = title,
                ForeColor = CHeader,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = false,
                UseCompatibleTextRendering = true,
                Height = 34,
                Dock = DockStyle.Top,
                BackColor = CSectionHdr,
                Padding = new Padding(2, 9, 0, 0)
            };
            pnl.Controls.Add(lblTitle);

            // Add an empty space below title
            var spacer = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent };
            pnl.Controls.Add(spacer);

            lblTitle.BringToFront();
            spacer.BringToFront();

            return pnl;
        }

        // ════════════════════════════════════════════════════════
        // Deadline Preview
        // ════════════════════════════════════════════════════════
        private void UpdateDeadlinePreview()
        {
            if (chkKhongThoiHan.Checked) return;

            DateTime thoiHan = dtpThoiHan.Value.Date;
            int daysLeft = (int)(thoiHan - DateTime.Today).TotalDays;

            Color bg, fg, borderC;
            string icon, msg;

            if (daysLeft < 0)
            {
                bg = CDanger; fg = CDangerText;
                borderC = Color.FromArgb(239, 68, 68);
                icon = "🚨";
                msg = $"QUÁ HẠN {Math.Abs(daysLeft)} NGÀY\n" +
                      $"     Văn bản đã quá hạn từ ngày {thoiHan:dd/MM/yyyy}. " +
                      $"Cần xử lý ngay!";
            }
            else if (daysLeft == 0)
            {
                bg = Color.FromArgb(252, 165, 165);
                fg = Color.FromArgb(127, 29, 29);
                borderC = Color.FromArgb(239, 68, 68);
                icon = "⚠️";
                msg = $"HẾT HẠN HÔM NAY — {thoiHan:dd/MM/yyyy}\n" +
                      $"     Văn bản đến hạn xử lý ngay hôm nay!";
            }
            else if (daysLeft <= 3)
            {
                bg = CAlert; fg = CAlertText;
                borderC = Color.FromArgb(251, 146, 60);
                icon = "⚠️";
                msg = $"CÒN {daysLeft} NGÀY — Đến hạn: {thoiHan:dd/MM/yyyy}\n" +
                      $"     Rất cấp bách! Cần hoàn thành trước ngày {thoiHan:dd/MM/yyyy}.";
            }
            else if (daysLeft <= 7)
            {
                bg = CWarning; fg = CWarningTxt;
                borderC = Color.FromArgb(245, 158, 11);
                icon = "⏰";
                msg = $"CÒN {daysLeft} NGÀY — Đến hạn: {thoiHan:dd/MM/yyyy}\n" +
                      $"     Sắp đến hạn. Hệ thống sẽ nhắc nhở 7, 3, 1 ngày trước hạn.";
            }
            else if (daysLeft <= 30)
            {
                bg = Color.FromArgb(224, 242, 254);
                fg = Color.FromArgb(7, 89, 133);
                borderC = Color.FromArgb(56, 189, 248);
                icon = "📅";
                msg = $"CÒN {daysLeft} NGÀY — Đến hạn: {thoiHan:dd/MM/yyyy}\n" +
                      $"     Trong vòng 1 tháng. Hệ thống sẽ tự động nhắc nhở khi gần đến hạn.";
            }
            else
            {
                bg = COk; fg = COkText;
                borderC = Color.FromArgb(52, 211, 153);
                icon = "✅";
                msg = $"CÒN {daysLeft} NGÀY — Đến hạn: {thoiHan:dd/MM/yyyy}\n" +
                      $"     Còn nhiều thời gian. Hệ thống sẽ nhắc nhở khi đến gần thời hạn.";
            }

            pnlDeadline.BackColor    = bg;
            lblDeadlineTxt.ForeColor = fg;
            lblDeadlineTxt.Text      = $"{icon}  {msg}";
            pnlDeadline.Invalidate();
        }

        // ════════════════════════════════════════════════════════
        // Helpers — TableLayout
        // ════════════════════════════════════════════════════════
        private void AddLabelCtrl(TableLayoutPanel tbl, int row, int col, string labelText, Control ctrl)
        {
            AddLabel(tbl, row, col, labelText);
            ctrl.Margin = new Padding(0, 4, 12, 6);
            tbl.Controls.Add(ctrl, col + 1, row);
        }

        private void AddLabel(TableLayoutPanel tbl, int row, int col, string text)
        {
            var lbl = new Label
            {
                Text      = text,
                ForeColor = CLabel,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                AutoSize  = true,
                TextAlign = ContentAlignment.MiddleRight,
                Margin    = new Padding(0, 6, 8, 0)
            };
            tbl.Controls.Add(lbl, col, row);
        }

        // ════════════════════════════════════════════════════════
        // Control Factories
        // ════════════════════════════════════════════════════════
        private TextBox MakeTxt() => new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = CCard,
            ForeColor   = CLabel,
            Font        = new Font("Segoe UI", 9.5f),
            Height      = 27,
            Dock        = DockStyle.Top
        };

        private DateTimePicker MakeDtp() => new DateTimePicker
        {
            Format       = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Height       = 27,
            Dock         = DockStyle.Top
        };

        private Button MakeButton(string text, Color bg, Color fg) => new Button
        {
            Text       = text,
            BackColor  = bg,
            ForeColor  = fg,
            FlatStyle  = FlatStyle.Flat,
            Font       = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor     = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };

        // ════════════════════════════════════════════════════════
        // File Handling
        // ════════════════════════════════════════════════════════
        private void BrowseFile()
        {
            using var dlg = new OpenFileDialog
            {
                Title     = "Chọn văn bản hành chính",
                Filter    = "Văn bản (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|PDF|*.pdf|Word|*.docx;*.doc",
                Multiselect = false
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                LoadFile(dlg.FileName);
        }

        private void BrowseFolder()
        {
            using var fbg = new FolderBrowserDialog { Description = "Chọn thư mục chứa văn bản hành chính để nhập hàng loạt" };
            if (fbg.ShowDialog() == DialogResult.OK)
            {
                // Detect directory, pivot to batch import
                this.Tag = fbg.SelectedPath; 
                this.DialogResult = DialogResult.Retry; 
                this.Close();
            }
        }

        private async void LoadFile(string filePath)
        {
            _filePath = filePath;
            string name = Path.GetFileName(filePath);
            string ext  = Path.GetExtension(filePath).ToLower();
            string icon = ext == ".pdf" ? "📕" : "📘";

            lblDropHint.Text      = $"✅  Đã tải: {name}";
            lblDropHint.ForeColor = Color.FromArgb(21, 128, 61);
            lblDropHint.Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblFileName.Text      = $"{icon}  {filePath}";

            lblStatus.Text      = "⏳  Đang phân tích văn bản (OCR), vui lòng chờ...";
            lblStatus.ForeColor = CAccent;

            // Reveal sections
            grpInfo.Visible     = true;
            grpDeadline.Visible = true;

            try
            {
                var record = await DocumentExtractorService.ExtractFromFileAsync(filePath);
                PopulateFields(record);
                lblStatus.Text      = "✅  Trích xuất thành công! Kiểm tra thông tin bên dưới và điều chỉnh nếu cần.";
                lblStatus.ForeColor = Color.FromArgb(21, 128, 61);
            }
            catch (Exception ex)
            {
                lblStatus.Text      = $"⚠  Không trích xuất tự động được: {ex.Message}. Vui lòng nhập thủ công.";
                lblStatus.ForeColor = Color.FromArgb(180, 38, 0);
            }
        }

        private void PopulateFields(DocumentRecord r)
        {
            if (!string.IsNullOrEmpty(r.SoVanBan))   txtSoVanBan.Text    = r.SoVanBan;
            if (!string.IsNullOrEmpty(r.TrichYeu))   txtTrichYeu.Text    = r.TrichYeu;
            if (r.NgayBanHanh.HasValue)               dtpNgayBanHanh.Value = r.NgayBanHanh.Value;
            if (!string.IsNullOrEmpty(r.CoQuanBanHanh)) txtCoQuanBH.Text = r.CoQuanBanHanh;
            if (!string.IsNullOrEmpty(r.CoQuanChuQuan)) txtChuQuan.Text  = r.CoQuanChuQuan;
            if (r.ThoiHan.HasValue)
            {
                dtpThoiHan.Value = r.ThoiHan.Value;
                UpdateDeadlinePreview();
            }
            if (!string.IsNullOrEmpty(r.DonViChiDao)) txtDonViChiDao.Text = r.DonViChiDao;
            
            // Thêm các hạn bổ sung
            pnlExtraDeadlines.Controls.Clear();
            _extraDeadlines.Clear();
            if (r.AdditionalDeadlines != null)
            {
                foreach (var dt in r.AdditionalDeadlines)
                {
                    AddDeadlinePill(dt);
                }
            }
        }

        private void AddDeadlinePill(DateTime dt)
        {
            if (_extraDeadlines.Contains(dt)) return;
            _extraDeadlines.Add(dt);

            var pill = new Panel {
                Size = new Size(200, 32),
                BackColor = Color.FromArgb(224, 231, 242),
                Margin = new Padding(0, 0, 8, 8)
            };
            pill.Paint += (s, e) => {
                using var pen = new Pen(CBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, pill.Width - 1, pill.Height - 1);
            };

            var lbl = new Label {
                Text = dt.ToString("dd/MM/yy HH:mm"),
                AutoSize = false,
                Width = 150,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Location = new Point(0, 0)
            };
            
            var btnRem = new Label {
                Text = "✕",
                AutoSize = false,
                Size = new Size(24, 24),
                Location = new Point(170, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnRem.Click += (s, e) => {
                _extraDeadlines.Remove(dt);
                pnlExtraDeadlines.Controls.Remove(pill);
            };

            pill.Controls.AddRange(new Control[] { lbl, btnRem });
            pnlExtraDeadlines.Controls.Add(pill);
        }

        // ════════════════════════════════════════════════════════
        // Validation & Build Record
        // ════════════════════════════════════════════════════════
        private DocumentRecord? BuildRecord()
        {
            if (string.IsNullOrWhiteSpace(txtSoVanBan.Text))
            {
                MessageBox.Show("Vui lòng nhập Số văn bản.", "Thiếu thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSoVanBan.Focus();
                return null;
            }

            if (string.IsNullOrWhiteSpace(txtChuQuan.Text))
            {
                var res = MessageBox.Show(
                    "Bạn chưa nhập Cơ quan chủ quản tham mưu.\nXác nhận tiếp tục không?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.No) { txtChuQuan.Focus(); return null; }
            }

            return new DocumentRecord
            {
                FilePath       = _filePath,
                SoVanBan       = txtSoVanBan.Text.Trim(),
                TrichYeu       = txtTrichYeu.Text.Trim(),
                NgayBanHanh    = dtpNgayBanHanh.Value.Date,
                CoQuanBanHanh  = txtCoQuanBH.Text.Trim(),
                CoQuanChuQuan  = txtChuQuan.Text.Trim(),
                ThoiHan        = chkKhongThoiHan.Checked ? (DateTime?)null : dtpThoiHan.Value.Date,
                DonViChiDao    = txtDonViChiDao.Text.Trim(),
                NgayThem       = DateTime.Now,
                DaTaoLich      = false,
                AdditionalDeadlines = new List<DateTime>(_extraDeadlines)
            };
        }

        // ════════════════════════════════════════════════════════
        // Button Handlers
        // ════════════════════════════════════════════════════════
        private void BtnSaveCalendar_Click(object? sender, EventArgs e)
        {
            var record = BuildRecord();
            if (record == null) return;

            if (record.ThoiHan == null)
            {
                MessageBox.Show("Không thể tạo lịch cho văn bản không có thời hạn.\nVui lòng bỏ chọn 'Không có thời hạn' hoặc chọn 'Chỉ Lưu'.",
                    "Không có thời hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                CalendarService.CreateCalendarEvents(record);
                record.DaTaoLich = true;
                Result = record;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                var r2 = MessageBox.Show(
                    $"Lỗi khi tạo lịch Calendar:\n{ex.Message}\n\nVăn bản vẫn sẽ được lưu. Tiếp tục?",
                    "Lỗi Calendar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r2 == DialogResult.Yes)
                {
                    Result = record;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void BtnSaveOnly_Click(object? sender, EventArgs e)
        {
            var record = BuildRecord();
            if (record == null) return;
            Result = record;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
