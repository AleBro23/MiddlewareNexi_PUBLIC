using System.Security.Cryptography;
using System.Text;

namespace MiddlewareNexi.Filters
{
    public static class SecuritySha1
    {
        public static string CalcolaSha1(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            using var sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
