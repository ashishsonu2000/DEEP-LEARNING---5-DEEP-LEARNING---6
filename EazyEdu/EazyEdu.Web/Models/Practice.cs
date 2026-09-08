using System.Collections.Generic;

namespace EazyEdu.Web.Models
{
    public class PracticeQuestion
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public int Marks { get; set; }
        /// <summary>easy, medium or hard — used when the user asks for a difficulty.</summary>
        public string Difficulty { get; set; }
        public string Topic { get; set; }
    }

    public class PracticeSet
    {
        public PracticeSet()
        {
            Questions = new List<PracticeQuestion>();
        }

        public string Subject { get; set; }
        public string StudentId { get; set; }
        public int DurationMinutes { get; set; }
        public List<PracticeQuestion> Questions { get; set; }
    }

    /// <summary>A paper assembled for one request — a filtered, shuffled view of a PracticeSet.</summary>
    public class PracticePaper
    {
        public PracticePaper()
        {
            Questions = new List<PracticeQuestion>();
        }

        public string Subject { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string Difficulty { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalMarks { get; set; }
        public List<PracticeQuestion> Questions { get; set; }
    }
}
