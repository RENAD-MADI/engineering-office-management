using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RenadWebApp.Data;
using RenadWebApp.Models.DataModel;

namespace RenadWebApp.Services
{
    /// <summary>Password hashing and the initial admin account.</summary>
    public class UserService
    {
        public const string AdminRole = "Admin";

        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<LoginModel> _hasher = new();

        public UserService(ApplicationDbContext db)
        {
            _db = db;
        }

        public string Hash(LoginModel user, string password) => _hasher.HashPassword(user, password);

        public async Task<LoginModel?> ValidateAsync(string username, string password)
        {
            var user = await _db.Login.SingleOrDefaultAsync(x => x.username == username);
            if (user == null) return null;

            PasswordVerificationResult result;
            try
            {
                result = _hasher.VerifyHashedPassword(user, user.password, password);
            }
            catch (FormatException)
            {
                result = PasswordVerificationResult.Failed;
            }
            if (result == PasswordVerificationResult.Failed)
            {
                // Accounts created before hashing was introduced still hold plain text; upgrade them on login.
                if (user.password != password) return null;
                result = PasswordVerificationResult.SuccessRehashNeeded;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.password = Hash(user, password);
                await _db.SaveChangesAsync();
            }
            return user;
        }

        public static bool IsAdmin(LoginModel user) =>
            string.Equals(user.username, AdminRole, StringComparison.OrdinalIgnoreCase);

        public async Task SeedAdminAsync(IConfiguration config)
        {
            var username = config["SeedAdmin:Username"] ?? AdminRole;
            var password = config["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(password) || await _db.Login.AnyAsync()) return;

            var admin = new LoginModel { username = username, password = "" };
            admin.password = Hash(admin, password);
            _db.Login.Add(admin);
            await _db.SaveChangesAsync();
        }
    }
}
