using System;
using System.Security.Cryptography;
using System.Web;

namespace EazyEdu.Web.Security
{
    /// <summary>
    /// Double-submit CSRF protection: a random token lives in the session and is echoed
    /// back by the page in the <c>X-EazyEdu-Token</c> header on every API call.
    /// </summary>
    public static class AntiForgery
    {
        public const string HeaderName = "X-EazyEdu-Token";
        private const string SessionKey = "EazyEdu.Csrf";

        public static string GetToken(HttpContext context)
        {
            if (context == null || context.Session == null) return string.Empty;

            var existing = context.Session[SessionKey] as string;
            if (!string.IsNullOrEmpty(existing)) return existing;

            byte[] bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            string token = Convert.ToBase64String(bytes);
            context.Session[SessionKey] = token;
            return token;
        }

        public static bool Validate(HttpContext context)
        {
            if (context == null || context.Session == null) return false;

            var expected = context.Session[SessionKey] as string;
            if (string.IsNullOrEmpty(expected)) return false;

            string supplied = context.Request.Headers[HeaderName];
            if (string.IsNullOrEmpty(supplied)) supplied = context.Request.Form[HeaderName];
            if (string.IsNullOrEmpty(supplied)) return false;

            return FixedTimeEquals(expected, supplied);
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
