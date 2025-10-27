using System;
using System.Linq;
using System.Security.Cryptography;

namespace DocumentManagerApp.Helpers
{
    public static class PasswordHelper
    {
        // Tworzy hash i sól z podanego hasła
        public static (string hash, string salt) HashPassword(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(20);

            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        // Sprawdza, czy podane hasło pasuje do zapisanego hasha i soli
        public static bool VerifyPassword(string password, string savedHash, string savedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(savedSalt);
            byte[] hashBytes = Convert.FromBase64String(savedHash);

            var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
            byte[] testHash = pbkdf2.GetBytes(20);

            return hashBytes.SequenceEqual(testHash);
        }
    }
}
