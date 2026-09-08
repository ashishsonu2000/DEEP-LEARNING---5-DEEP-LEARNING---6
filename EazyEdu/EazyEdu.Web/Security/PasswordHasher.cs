using System;
using System.Security.Cryptography;

namespace EazyEdu.Web.Security
{
    /// <summary>PBKDF2-SHA256 password hashing with a per-password random salt.</summary>
    public static class PasswordHasher
    {
        private const int SaltBytes = 16;
        private const int SubkeyBytes = 32;
        private const int Iterations = 50000;

        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException("password");

            byte[] salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] subkey = DeriveKey(password, salt);
            byte[] output = new byte[SaltBytes + SubkeyBytes];
            Buffer.BlockCopy(salt, 0, output, 0, SaltBytes);
            Buffer.BlockCopy(subkey, 0, output, SaltBytes, SubkeyBytes);
            return Convert.ToBase64String(output);
        }

        public static bool Verify(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) return false;

            byte[] stored;
            try
            {
                stored = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                return false;
            }

            if (stored.Length != SaltBytes + SubkeyBytes) return false;

            byte[] salt = new byte[SaltBytes];
            Buffer.BlockCopy(stored, 0, salt, 0, SaltBytes);

            byte[] expected = new byte[SubkeyBytes];
            Buffer.BlockCopy(stored, SaltBytes, expected, 0, SubkeyBytes);

            byte[] actual = DeriveKey(password, salt);
            return FixedTimeEquals(expected, actual);
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(SubkeyBytes);
            }
        }

        /// <summary>Comparison whose running time does not depend on where the bytes differ.</summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
