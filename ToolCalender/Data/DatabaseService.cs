using Microsoft.Data.Sqlite;
using ToolCalender.Models;

namespace ToolCalender.Data
{
    public static class DatabaseService
    {
        private static string _connectionString = "";

        public static void Initialize()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ToolCalender"
            );
            Directory.CreateDirectory(appData);
            string dbPath = Path.Combine(appData, "documents.db");
            _connectionString = $"Data Source={dbPath}";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string createDocumentsTable = @"
                CREATE TABLE IF NOT EXISTS Documents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SoVanBan TEXT,
                    TrichYeu TEXT,
                    NgayBanHanh TEXT,
                    CoQuanBanHanh TEXT,
                    CoQuanChuQuan TEXT,
                    ThoiHan TEXT,
                    DonViChiDao TEXT,
                    FilePath TEXT,
                    NgayThem TEXT,
                    DaTaoLich INTEGER DEFAULT 0,
                    UploadedByUserId INTEGER DEFAULT 1,
                    AllDeadlines TEXT
                )";

            string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE,
                    PasswordHash TEXT,
                    Role TEXT,
                    CreatedAt TEXT
                )";

            string createCommentsTable = @"
                CREATE TABLE IF NOT EXISTS Comments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DocumentId INTEGER,
                    UserId INTEGER,
                    Username TEXT,
                    Content TEXT,
                    CreatedAt TEXT,
                    FOREIGN KEY(DocumentId) REFERENCES Documents(Id)
                )";

            using var cmd = new SqliteCommand(createDocumentsTable, connection);
            cmd.ExecuteNonQuery();

            // Migration: Thêm cột AllDeadlines nếu chưa có (Dành cho DB cũ)
            try {
                cmd.CommandText = "ALTER TABLE Documents ADD COLUMN AllDeadlines TEXT";
                cmd.ExecuteNonQuery();
            } catch { /* Cột đã tồn tại */ }

            cmd.CommandText = createUsersTable;
            cmd.ExecuteNonQuery();

            cmd.CommandText = createCommentsTable;
            cmd.ExecuteNonQuery();

            // Đảm bảo tài khoản admin luôn tồn tại và đúng mật khẩu mặc định (đã băm an toàn)
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Username='admin'";
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            {
                string adminHash = BCrypt.Net.BCrypt.HashPassword("admin@123456");
                cmd.CommandText = $"INSERT INTO Users (Username, PasswordHash, Role, CreatedAt) VALUES ('admin', '{adminHash}', 'Admin', datetime('now'))";
                cmd.ExecuteNonQuery();
            }
            else
            {
                // Để đảm bảo đăng nhập được sau bản cập nhật này, nếu đã có user admin thì ép reset lại password hash này
                string adminHash = BCrypt.Net.BCrypt.HashPassword("admin@123456");
                cmd.CommandText = $"UPDATE Users SET PasswordHash='{adminHash}' WHERE Username='admin'";
                cmd.ExecuteNonQuery();
            }
        }

        // --- USER MANAGEMENT ---
        /// <summary>
        /// Đăng nhập an toàn.
        /// ✅ Chống SQL Injection: Dùng parameterized query (@u).
        /// ✅ Chống DoS: Giới hạn độ dài input theo chuẩn BCrypt (tối đa 72 byte cho mật khẩu).
        /// ✅ Mã hóa: Đã áp dụng BCrypt.Net-Next khớp chuỗi Hash (chuẩn bảo mật doanh nghiệp).
        /// </summary>
        public static User? Login(string username, string password)
        {
            // Sanitize tại tầng dữ liệu (defense-in-depth)
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            // Giới hạn độ dài tránh bị lợi dụng
            if (username.Length > 50 || password.Length > 72)
                return null;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = "SELECT Id, Username, Role, PasswordHash FROM Users WHERE Username=@u LIMIT 1";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@u", username.Trim());

            int userId = -1;
            string dbUser = "";
            string dbRole = "";
            string storedHash = "";
            bool found = false;

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    userId = Convert.ToInt32(reader["Id"]);
                    dbUser = reader["Username"].ToString() ?? "";
                    dbRole = reader["Role"].ToString() ?? "Guest";
                    storedHash = reader["PasswordHash"]?.ToString() ?? "";
                    found = true;
                }
            }

            if (!found) return null;

            bool isValid = false;

            // Kiểm tra xem Hash trong DB có định dạng chuẩn BCrypt không (bắt đầu bằng $2a$, $2b$, $2y$, $2x$)
            if (storedHash.StartsWith("$2"))
            {
                try 
                { 
                    isValid = BCrypt.Net.BCrypt.Verify(password, storedHash); 
                } 
                catch { /* Lỗi định dạng hash */ }
            }
            else
            {
                // Fallback: Mật khẩu tại DB đang là plain-text chưa được mã hóa trước đó
                if (password == storedHash)
                {
                    isValid = true;
                    // 👉 Tự động cập nhật (Migrate) mật khẩu lên chuẩn BCrypt ngay lập tức
                    string newHash = BCrypt.Net.BCrypt.HashPassword(password);
                    string upSql = "UPDATE Users SET PasswordHash=@ph WHERE Id=@id";
                    using var upCmd = new SqliteCommand(upSql, connection);
                    upCmd.Parameters.AddWithValue("@ph", newHash);
                    upCmd.Parameters.AddWithValue("@id", userId);
                    upCmd.ExecuteNonQuery();
                }
            }

            if (isValid)
            {
                return new User
                {
                    Id       = userId,
                    Username = dbUser,
                    Role     = dbRole
                };
            }
            return null;
        }

        public static bool Register(string username, string password, string role = "Guest")
        {
            try {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                string sql = "INSERT INTO Users (Username, PasswordHash, Role, CreatedAt) VALUES (@u, @p, @r, datetime('now'))";
                using var cmd = new SqliteCommand(sql, connection);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(password));
                cmd.Parameters.AddWithValue("@r", role);
                cmd.ExecuteNonQuery();
                return true;
            } catch { return false; }
        }

        // --- COMMENT MANAGEMENT ---
        public static List<Comment> GetComments(int docId)
        {
            var list = new List<Comment>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string sql = "SELECT * FROM Comments WHERE DocumentId=@id ORDER BY CreatedAt ASC";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", docId);
            using var reader = cmd.ExecuteReader();
            while(reader.Read())
            {
                list.Add(new Comment {
                    Id = Convert.ToInt32(reader["Id"]),
                    DocumentId = Convert.ToInt32(reader["DocumentId"]),
                    UserId = Convert.ToInt32(reader["UserId"]),
                    Username = reader["Username"].ToString() ?? "",
                    Content = reader["Content"].ToString() ?? "",
                    CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString() ?? DateTime.Now.ToString())
                });
            }
            return list;
        }

        public static void InsertComment(Comment c)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string sql = "INSERT INTO Comments (DocumentId, UserId, Username, Content, CreatedAt) VALUES (@docId, @uId, @uName, @c, datetime('now'))";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@docId", c.DocumentId);
            cmd.Parameters.AddWithValue("@uId", c.UserId);
            cmd.Parameters.AddWithValue("@uName", c.Username);
            cmd.Parameters.AddWithValue("@c", c.Content);
            cmd.ExecuteNonQuery();
        }

        public static List<DocumentRecord> GetAll()
        {
            var records = new List<DocumentRecord>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = "SELECT * FROM Documents ORDER BY ThoiHan ASC NULLS LAST";
            using var cmd = new SqliteCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                records.Add(MapRecord(reader));

            return records;
        }

        public static int Insert(DocumentRecord record)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = @"
                INSERT INTO Documents (SoVanBan, TrichYeu, NgayBanHanh, CoQuanBanHanh, CoQuanChuQuan, ThoiHan, DonViChiDao, FilePath, NgayThem, DaTaoLich, AllDeadlines)
                VALUES (@SoVanBan, @TrichYeu, @NgayBanHanh, @CoQuanBanHanh, @CoQuanChuQuan, @ThoiHan, @DonViChiDao, @FilePath, @NgayThem, @DaTaoLich, @AllDeadlines);
                SELECT last_insert_rowid();";

            using var cmd = new SqliteCommand(sql, connection);
            AddParams(cmd, record);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void Update(DocumentRecord record)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = @"
                UPDATE Documents SET
                    SoVanBan=@SoVanBan, TrichYeu=@TrichYeu, NgayBanHanh=@NgayBanHanh,
                    CoQuanBanHanh=@CoQuanBanHanh, CoQuanChuQuan=@CoQuanChuQuan,
                    ThoiHan=@ThoiHan, DonViChiDao=@DonViChiDao,
                    FilePath=@FilePath, DaTaoLich=@DaTaoLich, AllDeadlines=@AllDeadlines
                WHERE Id=@Id";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Id", record.Id);
            AddParams(cmd, record);
            cmd.ExecuteNonQuery();
        }

        public static void Delete(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Xóa dữ liệu liên kết trước (bình luận) để tránh lỗi Foreign Key Constraint
            using var cmd1 = new SqliteCommand("DELETE FROM Comments WHERE DocumentId=@Id", connection);
            cmd1.Parameters.AddWithValue("@Id", id);
            cmd1.ExecuteNonQuery();

            using var cmd2 = new SqliteCommand("DELETE FROM Documents WHERE Id=@Id", connection);
            cmd2.Parameters.AddWithValue("@Id", id);
            cmd2.ExecuteNonQuery();
        }

        private static void AddParams(SqliteCommand cmd, DocumentRecord r)
        {
            cmd.Parameters.AddWithValue("@SoVanBan", (object?)r.SoVanBan ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TrichYeu", (object?)r.TrichYeu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NgayBanHanh", r.NgayBanHanh.HasValue ? (object)r.NgayBanHanh.Value.ToString("yyyy-MM-dd") : DBNull.Value);
            cmd.Parameters.AddWithValue("@CoQuanBanHanh", (object?)r.CoQuanBanHanh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CoQuanChuQuan", (object?)r.CoQuanChuQuan ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ThoiHan", r.ThoiHan.HasValue ? (object)r.ThoiHan.Value.ToString("yyyy-MM-dd") : DBNull.Value);
            cmd.Parameters.AddWithValue("@DonViChiDao", (object?)r.DonViChiDao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", (object?)r.FilePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NgayThem", r.NgayThem.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@DaTaoLich", r.DaTaoLich ? 1 : 0);
            
            // Serialize danh sách ngày phụ thành chuỗi (Phân cách bằng dấu phẩy)
            string ad = r.AdditionalDeadlines != null && r.AdditionalDeadlines.Count > 0
                ? string.Join(",", r.AdditionalDeadlines.Select(x => x.ToString("yyyy-MM-dd HH:mm:ss")))
                : "";
            cmd.Parameters.AddWithValue("@AllDeadlines", ad);
        }

        private static DocumentRecord MapRecord(SqliteDataReader r)
        {
            return new DocumentRecord
            {
                Id = Convert.ToInt32(r["Id"]),
                SoVanBan = r["SoVanBan"]?.ToString() ?? "",
                TrichYeu = r["TrichYeu"]?.ToString() ?? "",
                NgayBanHanh = TryParseDate(r["NgayBanHanh"]?.ToString()),
                CoQuanBanHanh = r["CoQuanBanHanh"]?.ToString() ?? "",
                CoQuanChuQuan = r["CoQuanChuQuan"]?.ToString() ?? "",
                ThoiHan = TryParseDate(r["ThoiHan"]?.ToString()),
                DonViChiDao = r["DonViChiDao"]?.ToString() ?? "",
                FilePath = r["FilePath"]?.ToString() ?? "",
                NgayThem = TryParseDate(r["NgayThem"]?.ToString()) ?? DateTime.Now,
                DaTaoLich = Convert.ToInt32(r["DaTaoLich"]) == 1,
                AdditionalDeadlines = ParseDeadlines(r["AllDeadlines"]?.ToString())
            };
        }

        private static List<DateTime> ParseDeadlines(string? value)
        {
            var list = new List<DateTime>();
            if (string.IsNullOrEmpty(value)) return list;
            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (DateTime.TryParse(p, out DateTime dt)) list.Add(dt);
            }
            return list;
        }

        private static DateTime? TryParseDate(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (DateTime.TryParse(value, out DateTime dt)) return dt;
            return null;
        }
    }
}
