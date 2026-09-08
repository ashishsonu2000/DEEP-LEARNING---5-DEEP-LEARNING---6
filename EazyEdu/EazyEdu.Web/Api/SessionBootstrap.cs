using System;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Api
{
    /// <summary>
    /// Reconciles what the browser asked for (role, student, language) with what the
    /// signed-in account is actually allowed to see. The server decides, not the client.
    /// </summary>
    internal static class SessionBootstrap
    {
        public static void Apply(ConversationState state, UserAccount user, string requestedRole,
                                 string requestedStudentId, string requestedLanguage)
        {
            if (state == null || user == null) return;

            state.Role = ResolveRole(user, requestedRole);

            string studentId = AppServices.Auth.ResolveStudentId(user, requestedStudentId ?? state.StudentId);
            if (!string.IsNullOrEmpty(studentId)) state.StudentId = studentId;

            if (!string.IsNullOrEmpty(requestedLanguage) && Localization.IsSupported(requestedLanguage))
                state.Language = Localization.Normalize(requestedLanguage);

            if (string.IsNullOrEmpty(state.Language)) state.Language = Localization.English;
        }

        /// <summary>
        /// A parent may look at the assistant in "student" phrasing and vice versa, but
        /// nobody can promote themselves into teacher or admin from the client.
        /// </summary>
        private static string ResolveRole(UserAccount user, string requestedRole)
        {
            if (user.Role == UserRoles.Admin || user.Role == UserRoles.Teacher)
                return user.Role;

            if (string.Equals(requestedRole, UserRoles.Student, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(requestedRole, UserRoles.Parent, StringComparison.OrdinalIgnoreCase))
                return requestedRole.ToLowerInvariant();

            return user.Role;
        }
    }
}
