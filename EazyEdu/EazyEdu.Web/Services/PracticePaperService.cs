using System;
using System.Collections.Generic;
using System.Linq;
using EazyEdu.Web.Data;
using EazyEdu.Web.Models;

namespace EazyEdu.Web.Services
{
    /// <summary>
    /// Assembles a practice paper out of the question bank: filters by difficulty,
    /// shuffles with a date-seeded generator (same paper all day, a new one tomorrow)
    /// and balances the marks total.
    /// </summary>
    public class PracticePaperService
    {
        private readonly ISchoolRepository _repository;

        public PracticePaperService(ISchoolRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
        }

        public IList<string> GetSubjects(string studentId)
        {
            return _repository.GetPracticeSubjects(studentId).ToList();
        }

        /// <summary>Resolves a subject the user typed against the bank, tolerating case and partials.</summary>
        public string ResolveSubject(string studentId, string requested)
        {
            if (string.IsNullOrWhiteSpace(requested)) return null;

            IList<string> subjects = GetSubjects(studentId);
            string exact = subjects.FirstOrDefault(s => string.Equals(s, requested, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            return subjects.FirstOrDefault(s => s.IndexOf(requested, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                requested.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public PracticePaper Build(Student student, string subject, string difficulty, int? questionCount)
        {
            if (student == null) return null;

            PracticeSet set = _repository.GetPracticeSet(student.Id, subject);
            if (set == null) return null;

            IEnumerable<PracticeQuestion> pool = set.Questions;
            if (!string.IsNullOrEmpty(difficulty))
            {
                var filtered = set.Questions
                                  .Where(q => string.Equals(q.Difficulty, difficulty, StringComparison.OrdinalIgnoreCase))
                                  .ToList();
                // Fall back to the whole bank rather than handing back a two-question paper.
                if (filtered.Count >= 3) pool = filtered;
            }

            int seed = (Clock.Today.DayOfYear * 397) ^ (subject ?? string.Empty).GetHashCode() ^ student.Id.GetHashCode();
            var random = new Random(seed);

            List<PracticeQuestion> questions = pool.OrderBy(q => random.Next()).ToList();

            int take = questionCount.HasValue
                ? Math.Max(3, Math.Min(questionCount.Value, questions.Count))
                : Math.Min(6, questions.Count);

            questions = questions.Take(take)
                                 .OrderBy(q => DifficultyRank(q.Difficulty))
                                 .ToList();

            var paper = new PracticePaper
            {
                Subject = set.Subject,
                StudentName = student.Name,
                ClassName = student.ClassName,
                Difficulty = string.IsNullOrEmpty(difficulty) ? "mixed" : difficulty,
                DurationMinutes = Math.Max(15, (int)Math.Round(set.DurationMinutes * take / (double)Math.Max(1, set.Questions.Count))),
                TotalMarks = questions.Sum(q => q.Marks)
            };
            paper.Questions.AddRange(questions);
            return paper;
        }

        private static int DifficultyRank(string difficulty)
        {
            if (string.Equals(difficulty, "easy", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(difficulty, "medium", StringComparison.OrdinalIgnoreCase)) return 1;
            return 2;
        }
    }
}
