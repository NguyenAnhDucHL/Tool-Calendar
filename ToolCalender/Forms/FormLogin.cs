using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ToolCalender.Data;
using ToolCalender.Services;
using ToolCalender.Models;

namespace ToolCalender.Forms
{
    public class FormLogin : Form
    {
        // ── Controls ───────────────────────────────────────────
        private TextBox  txtUsername  = new();
        private TextBox  txtPassword  = new();
        private Button   btnLogin     = new();
        private Label    lblEye       = new();   // Tog gle hiện/ẩn mật khẩu
        private Label    lblError     = new();   // Thông báo lỗi inline

        // ── Security: Rate-limit ────────────────────────────────
        private int      _failCount   = 0;
        private DateTime _lockUntil   = DateTime.MinValue;
        private bool     _pwdVisible  = false;

        // ── Colors ──────────────────────────────────────────────
        private static readonly Color C1 = Color.FromArgb(15,  32,  68);   // nền tối
        private static readonly Color C2 = Color.FromArgb(26,  54, 110);   // nền card
        private static readonly Color CA = Color.FromArgb(56, 139, 253);   // accent xanh
        private static readonly Color CT = Color.White;
        private static readonly Color CE = Color.FromArgb(252, 129, 129);  // lỗi

        public FormLogin()
        {
            try { this.Icon = new Icon(@"asset\app_icon.ico"); } catch { }
            BuildUI();
        }

        // ════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.Text            = "Đăng nhập - Hệ thống Quản lý Văn bản";
            this.Size            = new Size(420, 680);
            this.MinimumSize     = new Size(420, 680);
            this.MaximumSize     = new Size(420, 680);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor       = C1;
            this.Region          = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            // ── Gradient background ─────────────────────────────
            this.Paint += (s, e) =>
            {
                using var brush = new LinearGradientBrush(
                    this.ClientRectangle,
                    Color.FromArgb(15, 32, 68),
                    Color.FromArgb(22, 48, 95),
                    LinearGradientMode.ForwardDiagonal);
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            };

            // ── Drag to move (chỉ khi click vào nền Form, không phải control con) ──
            bool dragging = false; Point dragStart = Point.Empty;
            this.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && s == this)
                { dragging = true; dragStart = e.Location; }
            };
            this.MouseMove += (s, e) =>
            {
                if (dragging) { var p = PointToScreen(e.Location); Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y); }
            };
            this.MouseUp += (s, e) => dragging = false;

            // ── ✕ Close button (góc phải trên) ────────────────────────
            var btnClose = new Button   // Dùng Button thay Label — nhận click chắc hơn
            {
                Text      = "✕",
                Size      = new Size(36, 36),
                Location  = new Point(Width - 48, 10),  // dời vào trong tránh góc bo
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 180, 220),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnClose.FlatAppearance.BorderSize      = 0;
            btnClose.FlatAppearance.MouseOverBackColor  = Color.FromArgb(60, 220, 80, 80);
            btnClose.FlatAppearance.MouseDownBackColor  = Color.FromArgb(120, 220, 60, 60);
            btnClose.Click      += (s, e) => { this.DialogResult = DialogResult.Cancel; Environment.Exit(0); };
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = CE;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(150, 180, 220);

            // ── Logo / Icon Container ────────────────────────────
            var pnlIcon = new Panel
            {
                Size      = new Size(80, 80),
                BackColor = Color.Transparent
            };
            // Căn giữa sau khi form size đã xác định
            pnlIcon.Location = new Point((this.ClientSize.Width - 80) / 2, 50);
            pnlIcon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(Color.FromArgb(45, CA));
                e.Graphics.FillEllipse(brush, 0, 0, pnlIcon.Width - 1, pnlIcon.Height - 1);
            };

            var lblIcon = new Label
            {
                Text      = "📋",
                Size      = new Size(80, 80),
                Location  = new Point(0, 0),
                Font      = new Font("Segoe UI Emoji", 32f),
                ForeColor = CT,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = true
            };
            pnlIcon.Controls.Add(lblIcon);

            // ── Tiêu đề ──────────────────────────────────────────
            var lblTitle = new Label
            {
                Text      = "XIN CHÀO!",
                Size      = new Size(Width, 60),
                Location  = new Point(0, 140),
                Font      = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = CT,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = true
            };

            var lblSub = new Label
            {
                Text      = "Vui lòng đăng nhập để tiếp tục",
                Size      = new Size(Width, 40),
                Location  = new Point(0, 195),
                Font      = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(140, 170, 215),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = true
            };

            // ── Card panel ───────────────────────────────────────
            var card = new Panel
            {
                Size      = new Size(360, 240),
                Location  = new Point(30, 240),
                BackColor = C2
            };
            card.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, 360, 240, 14, 14));

            // ── Username field ───────────────────────────────────
            var lblUName = new Label
            {
                Text      = "TÊN ĐĂNG NHẬP",
                Location  = new Point(20, 20),
                Size      = new Size(320, 18),
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 150, 220),
                BackColor = Color.Transparent
            };

            var pnlUser = MakeInputPanel(20, 42, 320, out txtUsername, false, "admin");

            // ── Password field ───────────────────────────────────
            var lblPwd = new Label
            {
                Text      = "MẬT KHẨU",
                Location  = new Point(20, 112),
                Size      = new Size(320, 18),
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 150, 220),
                BackColor = Color.Transparent
            };

            var pnlPwd = MakeInputPanel(20, 134, 320, out txtPassword, true, "••••••••");

            // ── Eye toggle ───────────────────────────────────────
            lblEye = new Label
            {
                Text      = "👁",
                Size      = new Size(30, 30),
                Location  = new Point(310, 140),   // sẽ tính lại dưới
                Font      = new Font("Segoe UI Emoji", 14f),
                ForeColor = Color.FromArgb(100, 140, 200),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblEye.Location = new Point(
                pnlPwd.Left + pnlPwd.Width - 34,
                pnlPwd.Top + (pnlPwd.Height - 30) / 2);
            lblEye.Click += TogglePassword;
            lblEye.MouseEnter += (s, e) => lblEye.ForeColor = CT;
            lblEye.MouseLeave += (s, e) => lblEye.ForeColor = Color.FromArgb(100, 140, 200);

            card.Controls.AddRange(new Control[] { lblUName, pnlUser, lblPwd, pnlPwd, lblEye });

            // ── Thông báo lỗi inline ─────────────────────────────
            lblError = new Label
            {
                Text      = "",
                Size      = new Size(360, 26),
                Location  = new Point(30, 490),
                Font      = new Font("Segoe UI", 9f),
                ForeColor = CE,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Login buttons ─────────────────────────────────────
            btnLogin = new Button
            {
                Text      = "ĐĂNG NHẬP (CHO ADMIN)",
                Size      = new Size(360, 45),
                Location  = new Point(30, 520),
                BackColor = CA,
                ForeColor = CT,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click      += BtnLogin_Click;
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(80, 160, 255);
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = CA;
            // Không bo góc bằng Region để tránh bị cắt lẹm 

            var btnGuest = new Button
            {
                Text      = "ĐĂNG NHẬP CHO KHÁCH (CHỈ XEM)",
                Size      = new Size(360, 45),
                Location  = new Point(30, 575),
                BackColor = Color.FromArgb(40, 70, 120),
                ForeColor = Color.FromArgb(200, 220, 255),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnGuest.FlatAppearance.BorderSize = 0;
            btnGuest.Click += (s, e) => 
            {
                string guestName = PromptForGuestName();
                if (string.IsNullOrWhiteSpace(guestName))
                {
                    MessageBox.Show("Bạn phải nhập tên để có thể đăng nhập chức năng Khách!", "Yêu cầu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SessionService.CurrentUser = new User { Id = 0, Username = guestName + " (Khách)", Role = "Guest" };
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            btnGuest.MouseEnter += (s, e) => btnGuest.BackColor = Color.FromArgb(50, 85, 140);
            btnGuest.MouseLeave += (s, e) => btnGuest.BackColor = Color.FromArgb(40, 70, 120);

            // ── Enter key support ────────────────────────────────
            txtUsername.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtPassword.Focus(); }};
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BtnLogin_Click(s, e); }};

            // ── Footer ───────────────────────────────────────────
            var lblFooter = new Label
            {
                Text      = "© 2026 Hệ thống Quản lý Văn bản Hành chính",
                Size      = new Size(Width, 22),
                Location  = new Point(0, 640),
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(70, 100, 140),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.AddRange(new Control[]
            {
                btnClose, pnlIcon, lblTitle, lblSub, card,
                lblError, btnLogin, btnGuest, lblFooter
            });
            pnlIcon.BringToFront();
        }

        // ════════════════════════════════════════════════════════
        // Input Panel Factory (box tối + viền accent khi focus)
        // ════════════════════════════════════════════════════════
        private Panel MakeInputPanel(int x, int y, int w, out TextBox txt, bool isPassword, string placeholder)
        {
            var pnl = new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(w, 46),
                BackColor = Color.FromArgb(12, 28, 62),
                Padding   = new Padding(12, 11, isPassword ? 38 : 12, 8) // Căn giữa dọc tốt hơn
            };
            pnl.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, w, 46, 9, 9));
            pnl.Paint += (s, e) =>
            {
                bool focused = pnl.ContainsFocus;
                using var pen = new Pen(focused ? CA : Color.FromArgb(45, 70, 110), focused ? 2 : 1);
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1));
            };

            var innerTxt = new TextBox
            {
                Text        = placeholder,
                Dock        = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor   = Color.FromArgb(12, 28, 62),
                ForeColor   = Color.FromArgb(100, 130, 180), // Màu placeholder
                Font        = new Font("Segoe UI", 11f),
                PasswordChar = '\0'
            };

            innerTxt.Enter += (s, e) =>
            {
                if (innerTxt.Text == placeholder)
                {
                    innerTxt.Text = "";
                    innerTxt.ForeColor = CT;
                    if (isPassword) innerTxt.PasswordChar = _pwdVisible ? '\0' : '●';
                }
                pnl.Invalidate();
            };

            innerTxt.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(innerTxt.Text))
                {
                    innerTxt.Text = placeholder;
                    innerTxt.ForeColor = Color.FromArgb(100, 130, 180);
                    innerTxt.PasswordChar = '\0';
                }
                pnl.Invalidate();
            };

            txt = innerTxt;
            pnl.Controls.Add(txt);
            return pnl;
        }

        // ════════════════════════════════════════════════════════
        // Toggle hiện / ẩn mật khẩu
        // ════════════════════════════════════════════════════════
        private void TogglePassword(object? sender, EventArgs e)
        {
            _pwdVisible = !_pwdVisible;
            txtPassword.PasswordChar = _pwdVisible ? '\0' : '●';
            lblEye.Text = _pwdVisible ? "🙈" : "👁";
            txtPassword.Focus();
        }

        // ════════════════════════════════════════════════════════
        // Login Logic + Rate-Limit (chống brute-force)
        // ════════════════════════════════════════════════════════
        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            // ── Kiểm tra khóa ─────────────────────────────────
            if (DateTime.Now < _lockUntil)
            {
                int secs = (int)(_lockUntil - DateTime.Now).TotalSeconds;
                ShowError($"⛔ Quá nhiều lần thử sai. Vui lòng chờ {secs}s.");
                return;
            }

            // ── Validate input ─────────────────────────────────
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;   // Không Trim mật khẩu

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("⚠ Vui lòng nhập tên đăng nhập."); txtUsername.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("⚠ Vui lòng nhập mật khẩu."); txtPassword.Focus(); return;
            }
            // Giới hạn độ dài tránh DoS
            if (username.Length > 50 || password.Length > 200)
            {
                ShowError("⚠ Thông tin đăng nhập không hợp lệ."); return;
            }

            // ── Gọi DB (đã dùng parameterized query) ──────────
            var user = DatabaseService.Login(username, password);

            if (user != null)
            {
                _failCount = 0;
                SessionService.CurrentUser = user;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _failCount++;
                if (_failCount >= 3)
                {
                    _lockUntil = DateTime.Now.AddSeconds(30);
                    _failCount = 0;
                    ShowError("⛔ Sai 3 lần liên tiếp. Hệ thống khóa 30 giây.");
                }
                else
                {
                    ShowError($"❌ Sai tài khoản hoặc mật khẩu! (Lần {_failCount}/3)");
                }
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        // ════════════════════════════════════════════════════════
        private void ShowError(string msg)
        {
            lblError.Text = msg;
            // Hiệu ứng rung nhẹ
            var pos = btnLogin.Location;
            for (int i = 0; i < 3; i++)
            {
                btnLogin.Left = pos.X + 5; Application.DoEvents(); System.Threading.Thread.Sleep(30);
                btnLogin.Left = pos.X - 5; Application.DoEvents(); System.Threading.Thread.Sleep(30);
            }
            btnLogin.Left = pos.X;
        }

        // ════════════════════════════════════════════════════════
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        private string PromptForGuestName()
        {
            using Form prompt = new Form()
            {
                Width = 400,
                Height = 220,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Thông tin Khách truy cập",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Width = 340, Height = 45, Text = "Vui lòng nhập tên của bạn để hiển thị trong các bình luận:" };
            textLabel.Font = new Font("Segoe UI", 9.5f);
            TextBox textBox = new TextBox() { Left = 20, Top = 70, Width = 340 };
            textBox.Font = new Font("Segoe UI", 10f);
            Button confirmation = new Button() { Text = "Xác nhận", Left = 240, Width = 120, Height = 35, Top = 120, DialogResult = DialogResult.OK, Cursor = Cursors.Hand };
            confirmation.BackColor = CA;
            confirmation.ForeColor = Color.White;
            confirmation.FlatStyle = FlatStyle.Flat;
            confirmation.FlatAppearance.BorderSize = 0;
            confirmation.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : "";
        }
    }
}
