using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using EazyEdu.Web.Data;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Security
{
    /// <summary>Forms-authentication wrapper plus the per-student access check.</summary>
    public class AuthService
    {
        private readonly ISchoolRepository _repository;

        public AuthService(ISchoolRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
        }

        public UserAccount ValidateCredentials(string userName, string password)
        {
            UserAccount account = _repository.FindUser(userName);
            if (account == null)
            {
                // Hash anyway so a missing user and a wrong password take the same time.
                PasswordHasher.Verify(password ?? string.Empty, PasswordHasher.Hash("dummy"));
                return null;
            }

            return PasswordHasher.Verify(password ?? string.Empty, account.PasswordHash) ? account : null;
        }

        public void SignIn(HttpContext context, UserAccount account, bool persistent)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (account == null) throw new ArgumentNullException("account");

            account.LastLoginUtc = Clock.UtcNow;

            var ticket = new FormsAuthenticationTicket(
                1,
                account.UserName,
                DateTime.Now,
                DateTime.Now.AddMinutes(persistent ? 60 * 24 * 7 : 60),
                persistent,
                account.Role,
                FormsAuthentication.FormsCookiePath);

            string encrypted = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
            {
                HttpOnly = true,
                Path = FormsAuthentication.FormsCookiePath,
                Secure = FormsAuthentication.RequireSSL
            };
            if (persistent) cookie.Expires = ticket.Expiration;

            context.Response.Cookies.Add(cookie);

            // A fresh session id per login keeps the CSRF token and chat context tied to this user.
            context.Session["EazyEdu.UserId"] = account.Id;
            context.Session.Remove("EazyEdu.Csrf");
        }

        public void SignOut(HttpContext context)
        {
            FormsAuthentication.SignOut();
            if (context != null && context.Session != null) context.Session.Clear();
        }

        public UserAccount GetCurrentUser(HttpContext context)
        {
            if (context == null || context.User == null || context.User.Identity == null) return null;
            if (!context.User.Identity.IsAuthenticated) return null;
            return _repository.FindUser(context.User.Identity.Name);
        }

        /// <summary>Admins and teachers see every seeded student; others only their own list.</summary>
        public bool CanAccessStudent(UserAccount account, string studentId)
        {
            if (account == null || string.IsNullOrEmpty(studentId)) return false;
            if (account.Role == UserRoles.Admin || account.Role == UserRoles.Teacher) return true;
            return account.StudentIds.Any(id => string.Equals(id, studentId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The student a request should be answered for, falling back to the first allowed one.</summary>
        public string ResolveStudentId(UserAccount account, string requested)
        {
            if (account == null) return null;
            if (!string.IsNullOrEmpty(requested) && CanAccessStudent(account, requested)) return requested;
            return account.StudentIds.FirstOrDefault();
        }
    }
}
