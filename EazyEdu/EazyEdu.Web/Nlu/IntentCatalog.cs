using System.Collections.Generic;

namespace EazyEdu.Web.Nlu
{
    /// <summary>The trained intent set. Add a definition here and the engine picks it up.</summary>
    public static class IntentCatalog
    {
        public static IList<IntentDefinition> Build()
        {
            var list = new List<IntentDefinition>();

            list.Add(new IntentDefinition(Intents.Greeting)
                .WithPattern(@"^(hi|hey|hello|yo|namaste|namaskar|good (morning|afternoon|evening))\b")
                .WithKeywords("hi", "hello", "hey", "namaste", "greetings")
                .WithExamples("hi", "hello there", "good morning", "namaste"));

            list.Add(new IntentDefinition(Intents.Identity)
                .WithPattern(@"\b(who are you|what are you|your name|what can you do about yourself)\b")
                .WithKeywords("who", "you", "name", "bot", "assistant")
                .WithExamples("who are you", "what is your name", "are you a bot"));

            list.Add(new IntentDefinition(Intents.Thanks)
                .WithPattern(@"\b(thanks|thank you|thankyou|dhanyavad|shukriya)\b")
                .WithKeywords("thanks", "thank", "appreciate", "dhanyavad")
                .WithExamples("thanks a lot", "thank you", "that was helpful"));

            list.Add(new IntentDefinition(Intents.Goodbye)
                .WithPattern(@"\b(bye|goodbye|see you|good night|alvida)\b")
                .WithKeywords("bye", "goodbye", "later", "night")
                .WithExamples("bye", "see you later", "good night"));

            list.Add(new IntentDefinition(Intents.Help)
                .WithPattern(@"\b(help|what can you do|options|menu|features|capabilities)\b")
                .WithKeywords("help", "can", "do", "option", "menu", "feature", "support")
                .Without("homework", "fee", "exam")
                .WithExamples("what can you do", "help me", "show me the menu", "list your features"));

            list.Add(new IntentDefinition(Intents.FeeStatus)
                .WithPattern(@"\b(fee|fees|tuition)\b.*\b(status|due|pending|balance|paid|left|remaining|outstanding)\b")
                .WithPattern(@"^\s*(fee|fees)\s*$")
                .WithKeywords("fee", "due", "payment", "pay", "balance", "outstanding", "pending", "tuition")
                .Without("breakdown", "receipt", "history", "split")
                .WithWeight(1.05)
                .WithExamples("what is my fee status", "how much fee is pending", "do i have any dues",
                              "fee balance", "how much do i need to pay"));

            list.Add(new IntentDefinition(Intents.FeeBreakdown)
                .WithPattern(@"\b(break\s?down|split|component|head wise|itemis|itemiz|what am i paying for)\b")
                .WithKeywords("breakdown", "split", "component", "head", "itemised", "detail", "fee")
                .WithWeight(1.15)
                .WithExamples("show fee breakdown", "what is the fee split", "fee components",
                              "what am i paying for"));

            list.Add(new IntentDefinition(Intents.FeeReceipt)
                .WithPattern(@"\b(receipt|invoice|bill)\b")
                .WithKeywords("receipt", "invoice", "bill", "download", "print")
                .WithWeight(1.15)
                .WithExamples("download the fee receipt", "i need an invoice", "send me the bill"));

            list.Add(new IntentDefinition(Intents.FeeHistory)
                .WithPattern(@"\b(payment|transaction)s?\b.*\b(history|list|made|earlier|previous)\b")
                .WithKeywords("history", "transaction", "previous", "earlier", "payment", "installment")
                .WithWeight(1.1)
                .WithExamples("show payment history", "which payments have i made", "past transactions"));

            list.Add(new IntentDefinition(Intents.ExamSchedule)
                .WithPattern(@"\b(exam|exams|datesheet|date sheet|test schedule)\b")
                .WithKeywords("exam", "datesheet", "schedule", "when", "paper", "date", "room", "venue")
                .Without("practice", "sample", "mock", "syllabus", "timetable")
                .WithWeight(1.05)
                .WithExamples("when are the exams", "show the exam schedule", "exam datesheet",
                              "which room is my maths exam in", "when is my next exam"));

            list.Add(new IntentDefinition(Intents.ExamSyllabus)
                .WithPattern(@"\b(syllabus|portion|chapters? covered|what to study)\b")
                .WithKeywords("syllabus", "portion", "chapter", "cover", "study", "topic")
                .WithWeight(1.15)
                .WithExamples("what is the syllabus for science", "which chapters are in the maths exam",
                              "exam portion"));

            list.Add(new IntentDefinition(Intents.PracticeRequest)
                .WithPattern(@"\b(practice|sample|mock|model)\s*(test|paper|question|exam)?\b")
                .WithPattern(@"\b(question paper|test paper|give me questions?|quiz me)\b")
                .WithKeywords("practice", "sample", "mock", "quiz", "worksheet", "question", "paper", "revision")
                .WithWeight(1.2)
                .WithExamples("give me a practice test", "generate a sample paper", "i want a mock test",
                              "quiz me on science", "practice questions for mathematics"));

            list.Add(new IntentDefinition(Intents.PracticeAnswerKey)
                .WithPattern(@"\b(answer key|answers|solution|solutions)\b")
                .WithKeywords("answer", "key", "solution", "check", "correct")
                .WithWeight(1.25)
                .WithExamples("show the answer key", "give me the solutions", "answers for the practice paper"));

            list.Add(new IntentDefinition(Intents.StudyTips)
                .WithPattern(@"\b(study tips?|how (should|do) i (study|prepare)|revision plan|prepare for)\b")
                .WithKeywords("tip", "study", "prepare", "revision", "plan", "advice")
                .WithWeight(1.1)
                .WithExamples("give me study tips", "how should i prepare for the exam", "revision plan"));

            list.Add(new IntentDefinition(Intents.AttendanceStatus)
                .WithPattern(@"\b(attendance|present|absent|percentage of days)\b")
                .WithKeywords("attendance", "present", "absent", "percentage", "day")
                .Without("log", "record", "which day", "miss")
                .WithWeight(1.05)
                .WithExamples("what is my attendance", "how many days was i present", "attendance percentage"));

            list.Add(new IntentDefinition(Intents.AttendanceDetail)
                .WithPattern(@"\b(which days?|attendance (log|record|history)|how many (more )?(days? )?can i miss|leave balance)\b")
                .WithKeywords("log", "record", "history", "miss", "leave", "which", "daywise")
                .WithWeight(1.2)
                .WithExamples("show my attendance log", "which days was i absent", "how many days can i miss",
                              "attendance record for this month"));

            list.Add(new IntentDefinition(Intents.HomeworkList)
                .WithPattern(@"\b(homework|assignment|hw due|what to submit)\b")
                .WithKeywords("homework", "assignment", "due", "submit", "pending", "work")
                .WithWeight(1.05)
                .WithExamples("what homework is due", "show my assignments", "pending homework",
                              "what do i have to submit"));

            list.Add(new IntentDefinition(Intents.MarksReport)
                .WithPattern(@"\b(mark|marks|grade|grades|result|results|score|report card|performance)\b")
                .WithKeywords("mark", "grade", "result", "score", "report", "card", "performance", "progress")
                .Without("practice", "answer")
                .WithWeight(1.05)
                .WithExamples("show my marks", "what were my results", "report card", "how did i perform"));

            list.Add(new IntentDefinition(Intents.NoticeList)
                .WithPattern(@"\b(notice|announcement|circular|news|noticeboard|notice board)\b")
                .WithKeywords("notice", "announcement", "circular", "news", "board", "update")
                .WithExamples("any notices", "show announcements", "what is new on the notice board"));

            list.Add(new IntentDefinition(Intents.Timetable)
                .WithPattern(@"\b(time\s?table|periods?|class schedule|which classes|what classes)\b")
                .WithKeywords("timetable", "period", "class", "schedule", "today", "tomorrow", "lecture")
                .Without("exam", "datesheet")
                .WithWeight(1.1)
                .WithExamples("what is my timetable", "which classes do i have today", "period schedule for tomorrow"));

            list.Add(new IntentDefinition(Intents.Transport)
                .WithPattern(@"\b(bus|transport|route|pickup|pick up|drop|van)\b")
                .WithKeywords("bus", "transport", "route", "pickup", "drop", "driver", "stop")
                .WithExamples("what is my bus route", "when is the pickup", "transport details"));

            list.Add(new IntentDefinition(Intents.Library)
                .WithPattern(@"\b(library|borrowed|issued book|book due|fine)\b")
                .WithKeywords("library", "book", "borrow", "issue", "return", "fine", "due")
                .Without("fee", "homework")
                .WithWeight(1.05)
                .WithExamples("which library book do i have", "when is my book due", "library fine"));

            list.Add(new IntentDefinition(Intents.TeacherContact)
                .WithPattern(@"\b(class teacher|teacher (contact|name|email)|who is my teacher|contact the teacher)\b")
                .WithKeywords("teacher", "contact", "class", "who", "email", "phone")
                .WithWeight(1.15)
                .WithExamples("who is my class teacher", "how do i contact the teacher", "teacher details"));

            list.Add(new IntentDefinition(Intents.Meeting)
                .WithPattern(@"\b(ptm|parent teacher meeting|parents meeting|open house)\b")
                .WithKeywords("ptm", "parent", "teacher", "meeting", "open", "house")
                .WithWeight(1.2)
                .WithExamples("when is the ptm", "parent teacher meeting date", "open house schedule"));

            list.Add(new IntentDefinition(Intents.SwitchStudent)
                .WithPattern(@"\b(switch to|show me|check) (aarav|isha)\b")
                .WithPattern(@"\b(other child|second child|my (son|daughter))\b")
                .WithKeywords("switch", "child", "son", "daughter", "aarav", "isha", "change")
                .WithWeight(1.25)
                .WithExamples("switch to isha", "show me aarav's details", "check my other child"));

            list.Add(new IntentDefinition(Intents.SwitchLanguage)
                .WithPattern(@"\b(hindi me|in hindi|hindi mein|speak hindi|switch to (hindi|english)|in english)\b")
                .WithKeywords("hindi", "english", "language", "translate")
                .WithWeight(1.25)
                .WithExamples("reply in hindi", "switch to english", "can you speak hindi"));

            list.Add(new IntentDefinition(Intents.Transcript)
                .WithPattern(@"\b(transcript|chat (history|log)|save (this )?(chat|conversation)|export)\b")
                .WithKeywords("transcript", "chat", "history", "export", "save", "conversation", "download")
                .WithWeight(1.2)
                .WithExamples("download the chat transcript", "save this conversation", "export chat history"));

            list.Add(new IntentDefinition(Intents.Escalate)
                .WithPattern(@"\b(talk to (a )?(human|person|teacher|principal|office)|raise a? ?(complaint|ticket)|customer care|speak to someone)\b")
                .WithKeywords("human", "complaint", "ticket", "principal", "office", "escalate", "someone", "staff")
                .WithWeight(1.3)
                .WithExamples("i want to talk to a human", "raise a complaint", "connect me to the office",
                              "this is wrong, let me speak to someone"));

            return list;
        }
    }
}
