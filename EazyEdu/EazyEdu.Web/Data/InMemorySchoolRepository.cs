using System;
using System.Collections.Generic;
using System.Linq;
using EazyEdu.Web.Models;
using EazyEdu.Web.Security;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Data
{
    /// <summary>
    /// Demo data store. Everything is dated relative to <see cref="Clock.Today"/> so the
    /// sample school always looks current, whatever day the app is opened.
    /// </summary>
    public class InMemorySchoolRepository : ISchoolRepository
    {
        private readonly Dictionary<string, Student> _students =
            new Dictionary<string, Student>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Dictionary<string, PracticeSet>> _practice =
            new Dictionary<string, Dictionary<string, PracticeSet>>(StringComparer.OrdinalIgnoreCase);

        private readonly List<UserAccount> _users = new List<UserAccount>();

        public InMemorySchoolRepository()
        {
            SeedStudents();
            SeedPracticeBank();
            SeedUsers();
        }

        public IEnumerable<Student> GetStudents()
        {
            return _students.Values.OrderBy(s => s.Name).ToList();
        }

        public Student GetStudent(string studentId)
        {
            if (string.IsNullOrEmpty(studentId)) return null;
            Student student;
            return _students.TryGetValue(studentId, out student) ? student : null;
        }

        public IEnumerable<Notice> GetNotices(string studentId)
        {
            Student student = GetStudent(studentId);
            if (student == null) return new List<Notice>();
            return student.Notices
                          .OrderByDescending(n => n.Priority)
                          .ThenByDescending(n => n.PostedOn)
                          .ToList();
        }

        public IEnumerable<string> GetPracticeSubjects(string studentId)
        {
            Dictionary<string, PracticeSet> bank;
            if (studentId == null || !_practice.TryGetValue(studentId, out bank))
                return new List<string>();
            return bank.Values.Select(p => p.Subject).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public PracticeSet GetPracticeSet(string studentId, string subject)
        {
            Dictionary<string, PracticeSet> bank;
            if (studentId == null || subject == null) return null;
            if (!_practice.TryGetValue(studentId, out bank)) return null;
            PracticeSet set;
            return bank.TryGetValue(subject, out set) ? set : null;
        }

        public UserAccount FindUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;
            return _users.FirstOrDefault(u => string.Equals(u.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public UserAccount GetUserById(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return _users.FirstOrDefault(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<UserAccount> GetUsers()
        {
            return _users.ToList();
        }

        // ------------------------------------------------------------------ seed

        private void SeedStudents()
        {
            DateTime today = Clock.Today;

            var aarav = new Student
            {
                Id = "aarav",
                Name = "Aarav Sharma",
                ClassName = "Class 8-B",
                Section = "B",
                ClassTeacher = "Mrs. Kavita Mehta",
                GuardianId = "parent.sharma",
                Attendance = new Attendance { Present = 165, Total = 180 },
                Fee = new FeeAccount
                {
                    Total = 45000m,
                    Paid = 30000m,
                    DueDate = today.AddDays(12),
                    Installment = "Term 2",
                    AcademicYear = "2026-27",
                    Components = new List<FeeComponent>
                    {
                        new FeeComponent { Head = "Tuition", Amount = 31500m },
                        new FeeComponent { Head = "Transport", Amount = 6750m },
                        new FeeComponent { Head = "Activities & library", Amount = 6750m }
                    }
                },
                Transport = new TransportInfo
                {
                    Route = "Route 4B",
                    PickupTime = "7:45 AM",
                    DropTime = "2:40 PM",
                    Stop = "Sector 21 Market",
                    DriverContact = "+91 98xxx 41120"
                }
            };

            aarav.Payments.AddRange(new[]
            {
                new FeePayment { ReceiptNo = "EE-26-004182", Date = today.AddDays(-124), Amount = 15000m, Mode = "Net banking", Reference = "TXN8841203" },
                new FeePayment { ReceiptNo = "EE-26-006540", Date = today.AddDays(-41),  Amount = 15000m, Mode = "UPI",          Reference = "UPI/774120933" }
            });

            aarav.Exams.AddRange(new[]
            {
                new Exam { Subject = "Mathematics",    Date = today.AddDays(7),  Room = "Room 12", Session = "Morning", MaxMarks = 80, Syllabus = "Ch. 1-6 (Rational numbers to Squares)" },
                new Exam { Subject = "Science",        Date = today.AddDays(9),  Room = "Room 12", Session = "Morning", MaxMarks = 80, Syllabus = "Ch. 1-5 (Crop production to Microorganisms)" },
                new Exam { Subject = "English",        Date = today.AddDays(11), Room = "Room 08", Session = "Morning", MaxMarks = 80, Syllabus = "Honeydew Ch. 1-4 + Writing" },
                new Exam { Subject = "Social Studies", Date = today.AddDays(14), Room = "Room 12", Session = "Morning", MaxMarks = 80, Syllabus = "History Ch. 1-3, Civics Ch. 1-2" }
            });

            aarav.Homework.AddRange(new[]
            {
                new HomeworkItem { Subject = "Mathematics", Task = "Exercise 6.3, Q1-10",                    Due = today.AddDays(2), Submitted = false },
                new HomeworkItem { Subject = "Science",     Task = "Diagram: human respiratory system",      Due = today.AddDays(3), Submitted = false },
                new HomeworkItem { Subject = "English",     Task = "Essay - \"My favourite festival\"",      Due = today.AddDays(4), Submitted = false },
                new HomeworkItem { Subject = "Science",     Task = "Worksheet 4 - synthetic fibres",         Due = today.AddDays(-2), Submitted = true }
            });

            aarav.Marks.AddRange(new[]
            {
                new Mark { Subject = "Mathematics",    Score = 88, MaxScore = 100, Assessment = "Unit test 2", RecordedOn = today.AddDays(-21) },
                new Mark { Subject = "Science",        Score = 91, MaxScore = 100, Assessment = "Unit test 2", RecordedOn = today.AddDays(-19) },
                new Mark { Subject = "English",        Score = 79, MaxScore = 100, Assessment = "Unit test 2", RecordedOn = today.AddDays(-18) },
                new Mark { Subject = "Social Studies", Score = 83, MaxScore = 100, Assessment = "Unit test 2", RecordedOn = today.AddDays(-16) },
                new Mark { Subject = "Mathematics",    Score = 81, MaxScore = 100, Assessment = "Unit test 1", RecordedOn = today.AddDays(-70) },
                new Mark { Subject = "Science",        Score = 84, MaxScore = 100, Assessment = "Unit test 1", RecordedOn = today.AddDays(-68) }
            });

            aarav.Notices.AddRange(new[]
            {
                new Notice { Title = "Parent-teacher meeting", Body = "PTM scheduled for " + Fmt(today.AddDays(18)) + ", 9 AM onward in the main hall.", PostedOn = today.AddDays(-1), Priority = true,  Audience = "All" },
                new Notice { Title = "Sports day practice",    Body = "Annual sports day practice begins " + Fmt(today.AddDays(6)) + ", report in PT kit.",  PostedOn = today.AddDays(-2), Priority = false, Audience = "Class 8" },
                new Notice { Title = "Library returns",        Body = "Borrowed books are due back by " + Fmt(today.AddDays(4)) + ".",                        PostedOn = today.AddDays(-3), Priority = false, Audience = "All" }
            });

            aarav.Library.Add(new LibraryLoan { Title = "Percy Jackson: The Lightning Thief", Author = "Rick Riordan", Due = today.AddDays(4), FinePerDay = 5m });

            aarav.Timetable.AddRange(BuildTimetable(new[]
            {
                "Mathematics", "Science", "English", "Social Studies", "Hindi", "Computer Science"
            }, "Mrs. Kavita Mehta"));

            aarav.Attendance.RecentDays.AddRange(BuildAttendanceLog(today, new[] { -9, -4 }));

            var isha = new Student
            {
                Id = "isha",
                Name = "Isha Sharma",
                ClassName = "Class 5-A",
                Section = "A",
                ClassTeacher = "Mr. Rahul Verma",
                GuardianId = "parent.sharma",
                Attendance = new Attendance { Present = 141, Total = 150 },
                Fee = new FeeAccount
                {
                    Total = 32000m,
                    Paid = 32000m,
                    DueDate = null,
                    Installment = "Fully paid",
                    AcademicYear = "2026-27",
                    Components = new List<FeeComponent>
                    {
                        new FeeComponent { Head = "Tuition", Amount = 22400m },
                        new FeeComponent { Head = "Transport", Amount = 4800m },
                        new FeeComponent { Head = "Activities & library", Amount = 4800m }
                    }
                },
                Transport = new TransportInfo
                {
                    Route = "Route 2A",
                    PickupTime = "7:30 AM",
                    DropTime = "2:15 PM",
                    Stop = "Green Park Gate 2",
                    DriverContact = "+91 98xxx 55031"
                }
            };

            isha.Payments.AddRange(new[]
            {
                new FeePayment { ReceiptNo = "EE-26-004199", Date = today.AddDays(-124), Amount = 16000m, Mode = "Cheque", Reference = "CHQ 220417" },
                new FeePayment { ReceiptNo = "EE-26-006611", Date = today.AddDays(-38),  Amount = 16000m, Mode = "UPI",    Reference = "UPI/882301145" }
            });

            isha.Exams.AddRange(new[]
            {
                new Exam { Subject = "Hindi",       Date = today.AddDays(6),  Room = "Room 3", Session = "Morning", MaxMarks = 50, Syllabus = "पाठ 1-4 एवं व्याकरण" },
                new Exam { Subject = "Mathematics", Date = today.AddDays(8),  Room = "Room 3", Session = "Morning", MaxMarks = 50, Syllabus = "Ch. 1-5 (Numbers to Fractions)" },
                new Exam { Subject = "EVS",         Date = today.AddDays(10), Room = "Room 3", Session = "Morning", MaxMarks = 50, Syllabus = "Ch. 1-6" }
            });

            isha.Homework.AddRange(new[]
            {
                new HomeworkItem { Subject = "Hindi",       Task = "लेख लेखन - \"मेरा विद्यालय\"", Due = today.AddDays(2), Submitted = false },
                new HomeworkItem { Subject = "Mathematics", Task = "Worksheet - fractions",        Due = today.AddDays(3), Submitted = false }
            });

            isha.Marks.AddRange(new[]
            {
                new Mark { Subject = "Hindi",       Score = 84, MaxScore = 100, Assessment = "Unit test 2", RecordedOn = today.AddDays(-20) },
                new Mark { Subject = "Mathematics", Score = 95, MaxScore = 100, Assessment = "Unit test 2", RecordedOn = today.AddDays(-18) },
                new Mark { Subject = "EVS",         Score = 89, MaxScore = 100, Assessment = "Unit test 2", RecordedOn = today.AddDays(-17) }
            });

            isha.Notices.AddRange(new[]
            {
                new Notice { Title = "Parent-teacher meeting", Body = "PTM scheduled for " + Fmt(today.AddDays(18)) + ", 9 AM onward in the main hall.", PostedOn = today.AddDays(-1), Priority = true,  Audience = "All" },
                new Notice { Title = "Field trip slips",       Body = "Class 5 field trip permission slips due " + Fmt(today.AddDays(5)) + ".",          PostedOn = today.AddDays(-2), Priority = false, Audience = "Class 5" },
                new Notice { Title = "New EVS textbook",       Body = "New EVS textbook to be collected from the office.",                                PostedOn = today.AddDays(-4), Priority = false, Audience = "Class 5" }
            });

            isha.Library.Add(new LibraryLoan { Title = "Panchatantra Tales", Author = "Vishnu Sharma", Due = today.AddDays(7), FinePerDay = 5m });

            isha.Timetable.AddRange(BuildTimetable(new[]
            {
                "Hindi", "Mathematics", "EVS", "English", "Art", "Physical Education"
            }, "Mr. Rahul Verma"));

            isha.Attendance.RecentDays.AddRange(BuildAttendanceLog(today, new[] { -6 }));

            _students[aarav.Id] = aarav;
            _students[isha.Id] = isha;
        }

        private static string Fmt(DateTime date)
        {
            return date.ToString("d MMM yyyy");
        }

        private static IEnumerable<TimetableEntry> BuildTimetable(string[] subjects, string teacher)
        {
            var entries = new List<TimetableEntry>();
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            var starts = new[] { "8:00 AM", "8:45 AM", "9:30 AM", "10:30 AM", "11:15 AM", "12:00 PM" };

            for (int d = 0; d < days.Length; d++)
            {
                for (int p = 0; p < starts.Length; p++)
                {
                    entries.Add(new TimetableEntry
                    {
                        Day = days[d],
                        Period = p + 1,
                        Subject = subjects[(d + p) % subjects.Length],
                        Teacher = teacher,
                        StartsAt = starts[p]
                    });
                }
            }
            return entries;
        }

        private static IEnumerable<AttendanceDay> BuildAttendanceLog(DateTime today, int[] absentOffsets)
        {
            var log = new List<AttendanceDay>();
            for (int offset = -13; offset <= 0; offset++)
            {
                DateTime day = today.AddDays(offset);
                if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                {
                    log.Add(new AttendanceDay { Date = day, Status = "Holiday", Remark = "Weekend" });
                    continue;
                }

                bool absent = Array.IndexOf(absentOffsets, offset) >= 0;
                log.Add(new AttendanceDay
                {
                    Date = day,
                    Status = absent ? "Absent" : "Present",
                    Remark = absent ? "Leave applied by guardian" : string.Empty
                });
            }
            return log;
        }

        private void SeedPracticeBank()
        {
            AddPractice("aarav", "Mathematics", 40, new[]
            {
                Q("Solve for x: 3x + 7 = 22", "x = 5", 2, "easy", "Linear equations"),
                Q("Find the HCF of 36 and 60.", "12", 2, "easy", "Number theory"),
                Q("Simplify: (2/3) + (5/6)", "3/2", 3, "easy", "Fractions"),
                Q("A rectangle has length 12 cm and breadth 7 cm. Find its area and perimeter.", "Area = 84 cm², Perimeter = 38 cm", 3, "medium", "Mensuration"),
                Q("If a number is increased by 20% it becomes 120. Find the original number.", "100", 3, "medium", "Percentage"),
                Q("Factorise: x² - 9x + 20", "(x - 4)(x - 5)", 4, "medium", "Algebra"),
                Q("The angles of a triangle are in the ratio 2:3:4. Find each angle.", "40°, 60°, 80°", 4, "hard", "Geometry"),
                Q("A sum doubles in 8 years at simple interest. Find the rate per annum.", "12.5% p.a.", 4, "hard", "Simple interest")
            });

            AddPractice("aarav", "Science", 35, new[]
            {
                Q("Name the process by which plants prepare their own food.", "Photosynthesis", 1, "easy", "Plant life"),
                Q("List two functions of the human respiratory system.", "Intake of oxygen and removal of carbon dioxide", 2, "easy", "Human body"),
                Q("What is the chemical formula of common salt?", "NaCl", 1, "easy", "Chemistry"),
                Q("Differentiate between reversible and irreversible changes with one example each.", "Reversible: melting of ice; Irreversible: burning of paper", 3, "medium", "Changes around us"),
                Q("Why does a compass needle point towards the north?", "It aligns itself with the Earth's magnetic field", 3, "medium", "Magnetism"),
                Q("Explain why crop rotation improves soil fertility.", "Different crops use different nutrients and legumes fix nitrogen, so the soil is not exhausted", 4, "hard", "Crop production"),
                Q("Describe one method to separate a mixture of sand and salt.", "Dissolve in water, filter out the sand, then evaporate the filtrate to recover the salt", 4, "hard", "Separation of substances")
            });

            AddPractice("aarav", "English", 35, new[]
            {
                Q("Identify the tense: \"She has finished her homework.\"", "Present perfect tense", 1, "easy", "Grammar"),
                Q("Write one sentence using the word \"meticulous\".", "Example: She is meticulous about her handwriting.", 2, "easy", "Vocabulary"),
                Q("Give a synonym of \"abundant\".", "Plentiful", 1, "easy", "Vocabulary"),
                Q("Convert to passive voice: \"The teacher praised the students.\"", "The students were praised by the teacher.", 3, "medium", "Voice"),
                Q("Join using a relative pronoun: \"This is the book. I bought it yesterday.\"", "This is the book that I bought yesterday.", 3, "medium", "Grammar"),
                Q("Write a short paragraph (40-50 words) on \"The importance of reading\".", "Open ended - check structure, spelling and relevance.", 5, "hard", "Writing")
            });

            AddPractice("aarav", "Social Studies", 35, new[]
            {
                Q("Name the three organs of the Indian government.", "Legislature, Executive, Judiciary", 3, "easy", "Civics"),
                Q("What is the Tropic of Cancer?", "A line of latitude at 23.5°N that passes through India", 2, "easy", "Geography"),
                Q("Define \"democracy\" in one sentence.", "A system of government in which power rests with the people through elected representatives", 2, "easy", "Civics"),
                Q("Name any two natural resources found in India.", "Coal and iron ore (other valid resources accepted)", 2, "medium", "Geography"),
                Q("What was the main aim of the Non-Cooperation Movement?", "To resist British rule by refusing to cooperate with colonial institutions", 4, "medium", "History"),
                Q("Explain two reasons for the growth of towns in medieval India.", "Trade routes and temple/administrative centres attracted artisans and merchants", 5, "hard", "History")
            });

            AddPractice("isha", "Hindi", 30, new[]
            {
                Q("\"विद्यालय\" शब्द का पर्यायवाची लिखिए।", "पाठशाला / स्कूल", 2, "easy", "शब्द भंडार"),
                Q("निम्न वाक्य में क्रिया छाँटिए: \"राम पढ़ता है।\"", "पढ़ता है", 2, "easy", "व्याकरण"),
                Q("\"परिश्रम\" शब्द का विलोम शब्द लिखिए।", "आलस्य", 2, "easy", "शब्द भंडार"),
                Q("\"कमल\" के दो पर्यायवाची शब्द लिखिए।", "जलज, पंकज", 3, "medium", "शब्द भंडार"),
                Q("पाँच पंक्तियों में \"मेरा प्रिय त्योहार\" पर निबंध लिखिए।", "खुला उत्तर - विषयवस्तु व वर्तनी जाँचें", 5, "hard", "लेखन")
            });

            AddPractice("isha", "Mathematics", 30, new[]
            {
                Q("Add: 3/5 + 1/5", "4/5", 2, "easy", "Fractions"),
                Q("What is the perimeter of a square with side 9 cm?", "36 cm", 2, "easy", "Mensuration"),
                Q("Round off 4,678 to the nearest hundred.", "4,700", 2, "easy", "Numbers"),
                Q("Find the value: 12 × 8", "96", 2, "easy", "Multiplication"),
                Q("Convert 3 kg into grams.", "3000 g", 3, "medium", "Measurement"),
                Q("A pencil costs ₹8. How much do 15 pencils cost?", "₹120", 3, "medium", "Word problems"),
                Q("Find the missing number: 7 × ___ = 84", "12", 3, "hard", "Multiplication")
            });

            AddPractice("isha", "EVS", 30, new[]
            {
                Q("Name any two sources of water.", "Rivers and lakes (wells, rain also accepted)", 2, "easy", "Water"),
                Q("Why should we plant more trees?", "They give oxygen, prevent soil erosion and provide shade and habitat", 3, "easy", "Environment"),
                Q("Name one migratory bird.", "Siberian crane (other valid answers accepted)", 2, "medium", "Animals"),
                Q("What is the main function of the heart?", "To pump blood throughout the body", 3, "medium", "Human body"),
                Q("Give two ways to save electricity at home.", "Switch off unused lights and fans; use LED bulbs", 4, "hard", "Environment")
            });
        }

        private static PracticeQuestion Q(string question, string answer, int marks, string difficulty, string topic)
        {
            return new PracticeQuestion
            {
                Question = question,
                Answer = answer,
                Marks = marks,
                Difficulty = difficulty,
                Topic = topic
            };
        }

        private void AddPractice(string studentId, string subject, int duration, PracticeQuestion[] questions)
        {
            Dictionary<string, PracticeSet> bank;
            if (!_practice.TryGetValue(studentId, out bank))
            {
                bank = new Dictionary<string, PracticeSet>(StringComparer.OrdinalIgnoreCase);
                _practice[studentId] = bank;
            }

            var set = new PracticeSet
            {
                Subject = subject,
                StudentId = studentId,
                DurationMinutes = duration
            };
            set.Questions.AddRange(questions);
            bank[subject] = set;
        }

        private void SeedUsers()
        {
            _users.Add(NewUser("u-aarav", "aarav", "Aarav Sharma", UserRoles.Student, "eazyedu123", "aarav"));
            _users.Add(NewUser("u-isha", "isha", "Isha Sharma", UserRoles.Student, "eazyedu123", "isha"));
            _users.Add(NewUser("u-parent", "parent.sharma", "Meera Sharma", UserRoles.Parent, "eazyedu123", "aarav", "isha"));
            _users.Add(NewUser("u-teacher", "teacher.mehta", "Kavita Mehta", UserRoles.Teacher, "eazyedu123", "aarav", "isha"));
            _users.Add(NewUser("u-admin", "admin", "School Office", UserRoles.Admin, "eazyedu123", "aarav", "isha"));
        }

        private static UserAccount NewUser(string id, string userName, string displayName, string role, string password, params string[] studentIds)
        {
            var account = new UserAccount
            {
                Id = id,
                UserName = userName,
                DisplayName = displayName,
                Role = role,
                PasswordHash = PasswordHasher.Hash(password)
            };
            account.StudentIds.AddRange(studentIds);
            return account;
        }
    }
}
