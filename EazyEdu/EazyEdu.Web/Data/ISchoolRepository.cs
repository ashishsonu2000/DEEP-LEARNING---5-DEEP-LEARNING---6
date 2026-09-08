using System.Collections.Generic;
using EazyEdu.Web.Models;

namespace EazyEdu.Web.Data
{
    /// <summary>
    /// Read model the assistant answers from. The demo ships an in-memory
    /// implementation; swap in an ADO.NET/EF one without touching the chat layer.
    /// </summary>
    public interface ISchoolRepository
    {
        IEnumerable<Student> GetStudents();
        Student GetStudent(string studentId);
        IEnumerable<Notice> GetNotices(string studentId);
        IEnumerable<string> GetPracticeSubjects(string studentId);
        PracticeSet GetPracticeSet(string studentId, string subject);

        UserAccount FindUser(string userName);
        UserAccount GetUserById(string userId);
        IEnumerable<UserAccount> GetUsers();
    }
}
