using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RemoteShellServer
{
    /// <summary>
    /// Provides security-related functionality for the RemoteShellServer
    /// </summary>
    public static class SecurityUtility
    {
        // Use AES-256 for encryption
        private const int AesKeySize = 256;
        private const int AesBlockSize = 128;

        /// <summary>
        /// Encrypts a string using AES-256
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <param name="key">The encryption key</param>
        /// <returns>Base64-encoded encrypted string</returns>
        public static string EncryptString(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                byte[] keyBytes = DeriveKeyBytes(key);
                byte[] iv = GenerateRandomBytes(16);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = AesKeySize;
                    aes.BlockSize = AesBlockSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = keyBytes;
                    aes.IV = iv;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        // Write IV to the beginning of the stream
                        ms.Write(iv, 0, iv.Length);

                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }

                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Decrypts an AES-256 encrypted string
        /// </summary>
        /// <param name="cipherText">The encrypted text (Base64-encoded)</param>
        /// <param name="key">The decryption key</param>
        /// <returns>Decrypted string</returns>
        public static string DecryptString(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] keyBytes = DeriveKeyBytes(key);

                // First 16 bytes are the IV
                byte[] iv = new byte[16];
                Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = AesKeySize;
                    aes.BlockSize = AesBlockSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = keyBytes;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(
                            new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length),
                            decryptor,
                            CryptoStreamMode.Read))
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random string
        /// </summary>
        /// <param name="length">The desired length of the string</param>
        /// <returns>Random string</returns>
        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            byte[] randomBytes = GenerateRandomBytes(length);
            
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[randomBytes[i] % chars.Length];
            }
            
            return new string(result);
        }

        /// <summary>
        /// Validates that a command is allowed to be executed
        /// </summary>
        /// <param name="command">The command to validate</param>
        /// <param name="shellType">The shell type</param>
        /// <returns>True if the command is allowed, otherwise false</returns>
        public static bool ValidateCommand(string command, ShellType shellType)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            // List of potentially dangerous commands to block
            // This is just a basic implementation - a production version would be more comprehensive
            var blockedCommands = new[] 
            { 
                "rm -rf /", "format", "deltree", "fdisk", "format c:", "del /f /s /q", 
                ":(){:|:&};:", "dd if=/dev/zero", "shutdown", "reboot"
            };

            foreach (var blockedCmd in blockedCommands)
            {
                if (command.ToLower().Contains(blockedCmd.ToLower()))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sanitizes a command input
        /// </summary>
        /// <param name="command">The command to sanitize</param>
        /// <returns>Sanitized command</returns>
        public static string SanitizeCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return string.Empty;

            // This is a very basic sanitization - a production version would be more comprehensive
            return command
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        /// <summary>
        /// Generates cryptographically secure random bytes
        /// </summary>
        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// Derives key bytes from a string using PBKDF2
        /// </summary>
        private static byte[] DeriveKeyBytes(string key)
        {
            const int iterations = 10000;
            byte[] salt = Encoding.UTF8.GetBytes("SysGuardRemoteShellSalt"); // In production, use a configurable salt

            using (var deriveBytes = new Rfc2898DeriveBytes(key, salt, iterations))
            {
                return deriveBytes.GetBytes(AesKeySize / 8);
            }
        }

        /// <summary>
        /// Calculate SHA-256 hash of a string
        /// </summary>
        public static string CalculateSha256Hash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                
                return builder.ToString();
            }
        }
    }
}