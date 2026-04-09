namespace ToolCalender.Forms
{
    /// <summary>
    /// Dialog xác nhận thao tác nguy hiểm (xóa, v.v.) với thiết kế đẹp thay cho MessageBox mặc định.
    /// </summary>
    public class FormConfirm : Form
    {
        private static readonly Color CHeader    = Color.FromArgb(15, 40, 80);
        private static readonly Color CBg        = Color.FromArgb(248, 250, 252);
        private static readonly Color CBorder    = Color.FromArgb(203, 213, 225);
        private static readonly Color CText      = Color.FromArgb(30, 41, 59);

        public FormConfirm(string title, string message, string confirmText, Color confirmColor)
        {
            this.Text            = title;
            this.Size            = new Size(480, 240);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = CBg;
            this.Font            = new Font("Segoe UI", 9.5f);

            // Header
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = CHeader
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(confirmColor, 3);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 3, pnlHeader.Width, pnlHeader.Height - 3);
            };
            var lblTitle = new Label
            {
                Text      = title,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(18, 13)
            };
            pnlHeader.Controls.Add(lblTitle);

            // Message
            var lblMsg = new Label
            {
                Text      = message,
                ForeColor = CText,
                Font      = new Font("Segoe UI", 10f),
                Location  = new Point(20, 62),
                Size      = new Size(440, 80),
                TextAlign = ContentAlignment.TopLeft
            };

            // Buttons
            var btnConfirm = new Button
            {
                Text       = confirmText,
                BackColor  = confirmColor,
                ForeColor  = Color.White,
                FlatStyle  = FlatStyle.Flat,
                Font       = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size       = new Size(130, 34),
                Location   = new Point(210, 162),
                Cursor     = Cursors.Hand,
                DialogResult = DialogResult.OK,
                FlatAppearance = { BorderSize = 0 }
            };

            var btnCancel = new Button
            {
                Text         = "✖  Hủy",
                BackColor    = Color.FromArgb(100, 116, 139),
                ForeColor    = Color.White,
                FlatStyle    = FlatStyle.Flat,
                Font         = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size         = new Size(100, 34),
                Location     = new Point(350, 162),
                Cursor       = Cursors.Hand,
                DialogResult = DialogResult.Cancel,
                FlatAppearance = { BorderSize = 0 }
            };

            this.Controls.AddRange(new Control[] { pnlHeader, lblMsg, btnConfirm, btnCancel });
            this.AcceptButton = btnConfirm;
            this.CancelButton = btnCancel;
        }
    }
}
