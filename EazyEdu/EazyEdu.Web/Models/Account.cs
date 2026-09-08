using System;
using System.Collections.Generic;

namespace EazyEdu.Web.Models
{
    public static class UserRoles
    {
        public const string Student = "student";
        public const string Parent = "parent";
        public const string Teacher = "teacher";
        public const string Admin = "admin";
    }

    public class UserAccount
    {
        public UserAccount()
        {
            StudentIds = new List<string>();
        }

        public string Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        /// <summary>PBKDF2 hash, base64 of salt + subkey.</summary>
        public string PasswordHash { get; set; }
        /// <summary>Students this account may read: itself, its children, or a whole class.</summary>
        public List<string> StudentIds { get; set; }
        public DateTime? LastLoginUtc { get; set; }
    }
}
