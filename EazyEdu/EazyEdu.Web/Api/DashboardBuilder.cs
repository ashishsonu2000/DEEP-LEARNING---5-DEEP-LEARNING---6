using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Api
{
    /// <summary>Shapes the sidebar payload the browser renders: attendance, fee, next exam, notices.</summary>
    internal static class DashboardBuilder
    {
        private static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");

        public static object Build(UserAccount user, ConversationState state)
        {
            Student student = AppServices.Repository.GetStudent(state.StudentId);
            if (student == null) return new { ok = false, error = "No student record available for this account." };

            Exam nextExam = student.Exams
                                   .Where(e => e.Date >= Clock.Today)
                                   .OrderBy(e => e.Date)
                                   .FirstOrDefault()
                            ?? student.Exams.OrderBy(e => e.Date).FirstOrDefault();

            var students = user.StudentIds
                               .Select(id => AppServices.Repository.GetStudent(id))
                               .Where(s => s != null)
                               .Select(s => new { id = s.Id, name = s.Name, className = s.ClassName })
                               .ToList();

            if (user.Role == UserRoles.Admin || user.Role == UserRoles.Teacher)
            {
                students = AppServices.Repository.GetStudents()
                                      .Select(s => new { id = s.Id, name = s.Name, className = s.ClassName })
                                      .ToList();
            }

            return new
            {
                ok = true,
                user = new
                {
                    name = user.DisplayName,
                    userName = user.UserName,
                    role = user.Role,
                    isStaff = user.Role == UserRoles.Admin || user.Role == UserRoles.Teacher
                },
                role = state.Role,
                language = state.Language,
                today = Clock.Today.ToString("dd MMM yyyy", India),
                simulatedClock = Clock.IsPinned,
                student = new
                {
                    id = student.Id,
                    name = student.Name,
                    firstName = student.FirstName,
                    className = student.ClassName,
                    classTeacher = student.ClassTeacher
                },
                students,
                attendance = new
                {
                    present = student.Attendance.Present,
                    total = student.Attendance.Total,
                    percent = student.Attendance.Percent,
                    canMiss = student.Attendance.DaysCanMiss(75)
                },
                fee = new
                {
                    total = student.Fee.Total,
                    paid = student.Fee.Paid,
                    balance = student.Fee.Balance,
                    percent = student.Fee.PercentPaid,
                    cleared = student.Fee.IsCleared,
                    academicYear = student.Fee.AcademicYear,
                    installment = student.Fee.Installment,
                    dueDate = student.Fee.DueDate.HasValue
                        ? student.Fee.DueDate.Value.ToString("d MMM yyyy", India)
                        : null,
                    totalText = Money(student.Fee.Total),
                    paidText = Money(student.Fee.Paid),
                    balanceText = Money(student.Fee.Balance)
                },
                nextExam = nextExam == null ? null : new
                {
                    subject = nextExam.Subject,
                    date = nextExam.Date.ToString("d MMM yyyy", India),
                    room = nextExam.Room,
                    days = Clock.DaysUntil(nextExam.Date)
                },
                homeworkPending = student.Homework.Count(h => !h.Submitted),
                notices = AppServices.Repository.GetNotices(student.Id)
                    .Select(n => new
                    {
                        title = n.Title,
                        body = n.Body,
                        priority = n.Priority,
                        postedOn = n.PostedOn.ToString("d MMM", India)
                    })
                    .ToList(),
                chips = AppServices.Chat.DefaultChips(state.Language)
                    .Select(c => new { label = c.Label, query = c.Query })
                    .ToList(),
                greeting = AppServices.Chat.Greeting(state, user)
            };
        }

        private static string Money(decimal amount)
        {
            return "₹" + amount.ToString("N0", India);
        }
    }
}
