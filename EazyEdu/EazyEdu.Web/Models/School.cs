using System;
using System.Collections.Generic;
using System.Linq;

namespace EazyEdu.Web.Models
{
    /// <summary>Everything the assistant knows about one student.</summary>
    public class Student
    {
        public Student()
        {
            Exams = new List<Exam>();
            Homework = new List<HomeworkItem>();
            Marks = new List<Mark>();
            Notices = new List<Notice>();
            Timetable = new List<TimetableEntry>();
            Library = new List<LibraryLoan>();
            Payments = new List<FeePayment>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public string ClassTeacher { get; set; }
        public string GuardianId { get; set; }

        public Attendance Attendance { get; set; }
        public FeeAccount Fee { get; set; }
        public TransportInfo Transport { get; set; }

        public List<Exam> Exams { get; set; }
        public List<HomeworkItem> Homework { get; set; }
        public List<Mark> Marks { get; set; }
        public List<Notice> Notices { get; set; }
        public List<TimetableEntry> Timetable { get; set; }
        public List<LibraryLoan> Library { get; set; }
        public List<FeePayment> Payments { get; set; }

        public string FirstName
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return string.Empty;
                return Name.Split(' ')[0];
            }
        }

        /// <summary>Subjects the student is examined in, de-duplicated and ordered.</summary>
        public IList<string> Subjects
        {
            get
            {
                return Exams.Select(e => e.Subject)
                            .Concat(Marks.Select(m => m.Subject))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                            .ToList();
            }
        }
    }

    public class Attendance
    {
        public int Present { get; set; }
        public int Total { get; set; }
        /// <summary>Day-by-day log for the running month, newest last.</summary>
        public List<AttendanceDay> RecentDays { get; set; }

        public Attendance()
        {
            RecentDays = new List<AttendanceDay>();
        }

        public int Percent
        {
            get { return Total == 0 ? 0 : (int)Math.Round(Present * 100.0 / Total); }
        }

        /// <summary>How many further days can be missed while staying at or above <paramref name="floor"/>%.</summary>
        public int DaysCanMiss(int floor)
        {
            if (Total == 0) return 0;
            int allowed = 0;
            int present = Present;
            int total = Total;
            while (true)
            {
                int nextTotal = total + 1;
                if (present * 100.0 / nextTotal < floor) break;
                total = nextTotal;
                allowed++;
                if (allowed > 400) break;
            }
            return allowed;
        }
    }

    public class AttendanceDay
    {
        public DateTime Date { get; set; }
        /// <summary>Present, Absent, Late or Holiday.</summary>
        public string Status { get; set; }
        public string Remark { get; set; }
    }

    public class FeeAccount
    {
        public FeeAccount()
        {
            Components = new List<FeeComponent>();
        }

        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public DateTime? DueDate { get; set; }
        public string Installment { get; set; }
        public string AcademicYear { get; set; }
        public List<FeeComponent> Components { get; set; }

        public decimal Balance { get { return Total - Paid; } }
        public bool IsCleared { get { return Balance <= 0m; } }
        public int PercentPaid
        {
            get { return Total == 0m ? 0 : (int)Math.Round(Paid * 100m / Total); }
        }
    }

    public class FeeComponent
    {
        public string Head { get; set; }
        public decimal Amount { get; set; }
    }

    public class FeePayment
    {
        public string ReceiptNo { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Mode { get; set; }
        public string Reference { get; set; }
    }

    public class Exam
    {
        public string Subject { get; set; }
        public DateTime Date { get; set; }
        public string Room { get; set; }
        public string Session { get; set; }
        public int MaxMarks { get; set; }
        public string Syllabus { get; set; }
    }

    public class HomeworkItem
    {
        public string Subject { get; set; }
        public string Task { get; set; }
        public DateTime Due { get; set; }
        public bool Submitted { get; set; }
    }

    public class Mark
    {
        public string Subject { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public string Assessment { get; set; }
        public DateTime RecordedOn { get; set; }

        public double Percent
        {
            get { return MaxScore == 0 ? 0 : Math.Round(Score * 100.0 / MaxScore, 1); }
        }

        public string Grade
        {
            get
            {
                double p = Percent;
                if (p >= 90) return "A1";
                if (p >= 80) return "A2";
                if (p >= 70) return "B1";
                if (p >= 60) return "B2";
                if (p >= 50) return "C1";
                if (p >= 40) return "C2";
                return "D";
            }
        }
    }

    public class Notice
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime PostedOn { get; set; }
        public bool Priority { get; set; }
        public string Audience { get; set; }
    }

    public class TimetableEntry
    {
        public DayOfWeek Day { get; set; }
        public int Period { get; set; }
        public string Subject { get; set; }
        public string Teacher { get; set; }
        public string StartsAt { get; set; }
    }

    public class TransportInfo
    {
        public string Route { get; set; }
        public string PickupTime { get; set; }
        public string DropTime { get; set; }
        public string Stop { get; set; }
        public string DriverContact { get; set; }

        public string Summary
        {
            get { return string.Format("{0} — pickup {1}, drop {2} (stop: {3})", Route, PickupTime, DropTime, Stop); }
        }
    }

    public class LibraryLoan
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime Due { get; set; }
        public decimal FinePerDay { get; set; }
    }
}
