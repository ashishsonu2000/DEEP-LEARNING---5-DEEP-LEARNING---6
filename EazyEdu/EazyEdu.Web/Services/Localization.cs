using System;
using System.Collections.Generic;
using System.Globalization;

namespace EazyEdu.Web.Services
{
    /// <summary>
    /// Two-language phrase book. Missing Hindi keys fall back to English rather than
    /// showing a raw key, so a partial translation is always safe to ship.
    /// </summary>
    public static class Localization
    {
        public const string English = "en";
        public const string Hindi = "hi";

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "greeting.student", "Hi {0}! I'm your EazyEdu assistant. Ask me about fees, exams, attendance, homework, notices, transport or the library." },
            { "greeting.parent", "Good {0}. Here's the latest for <b>{1}</b> ({2}). Ask me about fees, exams, attendance, homework or notices any time." },
            { "identity", "I'm the EazyEdu assistant — the school's own helper. I read your live school records and answer questions about fees, exams, attendance, homework, marks, notices, transport and the library. I never guess: if I don't have a record, I say so." },
            { "thanks", "Happy to help! Anything else about fees, exams, attendance or homework?" },
            { "goodbye", "Goodbye — study well, and come back any time. 👋" },
            { "help.intro", "Here's what I can do for <b>{0}</b> ({1}):" },
            { "help.footer", "You can also type in plain English or Hindi — I'll work out what you mean." },

            { "fee.header", "<b>{0} fee</b> for {1}" },
            { "fee.paid", "Paid {0} of {1} ({2}%)." },
            { "fee.remaining", "Remaining <b>{0}</b>, due by <b>{1}</b> ({2})." },
            { "fee.overdue", "Remaining <b>{0}</b>, which was due on <b>{1}</b> — {2} day(s) overdue." },
            { "fee.cleared", "No dues pending — the account is fully paid. 🎉" },
            { "fee.breakdown.title", "Fee breakdown — {0} ({1})" },
            { "fee.history.title", "Payments received" },
            { "fee.history.empty", "No payments recorded against this account yet." },

            { "exam.title", "Upcoming exams — {0}" },
            { "exam.next", "The next paper is <b>{0}</b> in <b>{1}</b> day(s), on {2} in {3}. All the best! 📘" },
            { "exam.none", "No exams are scheduled for {0} right now." },
            { "syllabus.title", "Syllabus — {0}" },
            { "syllabus.ask", "Which subject's syllabus would you like?" },

            { "practice.ask", "I can generate a practice paper from {0}'s syllabus. Which subject?" },
            { "practice.header", "📝 {0} practice paper — {1}" },
            { "practice.meta", "Time: {0} min · Total marks: {1} · Difficulty: {2}" },
            { "practice.footer", "Attempt all questions, then check yourself against the answer key." },
            { "practice.none", "I don't have a question bank for {0} yet. Try {1}." },
            { "answerkey.title", "✅ {0} — answer key" },
            { "answerkey.ask", "Which subject's answer key do you need?" },

            { "attendance.summary", "<b>{0}</b> has attended {1} of {2} school days this term — <b>{3}%</b>." },
            { "attendance.good", "That comfortably clears the 75% requirement." },
            { "attendance.ok", "That is above the 75% minimum, but worth keeping an eye on." },
            { "attendance.low", "That is below the 75% minimum — please check in with the class teacher." },
            { "attendance.canmiss", "At this rate {0} more day(s) can be missed while staying above 75%." },
            { "attendance.log.title", "Attendance — last two weeks" },

            { "homework.title", "Pending homework for {0}" },
            { "homework.none", "Nothing pending — all homework is submitted. 🎉" },
            { "homework.overdue", "⚠️ {0} item(s) are past their due date." },

            { "marks.title", "Latest assessment results" },
            { "marks.average", "Average across subjects: <b>{0}%</b> ({1})." },
            { "marks.best", "Strongest subject: <b>{0}</b>. Needs the most work: <b>{1}</b>." },

            { "notice.title", "Notice board" },
            { "notice.none", "There are no notices right now." },

            { "timetable.title", "Timetable — {0}" },
            { "timetable.none", "No periods scheduled for {0} — it's a holiday." },

            { "transport.line", "🚌 {0}'s bus: <b>{1}</b>. Driver: {2}. Call the transport desk if the pickup time needs to change." },
            { "library.title", "📖 Library loans" },
            { "library.none", "No books are on loan right now." },
            { "library.due", "<b>{0}</b> by {1} — due {2} ({3}). A fine of {4}/day applies after the due date." },

            { "teacher.line", "{0}'s class teacher is <b>{1}</b> ({2}). Messages sent through EazyEdu reach them within a working day." },

            { "studytips", "A few things that genuinely help: revise one subject a day instead of cramming, redo old homework problems rather than re-reading notes, and sleep properly the night before the paper. Want a practice paper to test yourself?" },
            { "transcript", "Your chat transcript is ready — it includes every question and answer from this session." },

            { "escalate.created", "I've raised ticket <b>{0}</b> with the school office and attached this conversation. Someone will get back to you within one working day." },
            { "escalate.offer", "I don't seem to be getting this right. Would you like me to pass it to the school office?" },

            { "switch.done", "Now showing records for <b>{0}</b> ({1})." },
            { "switch.denied", "That student isn't linked to your account, so I can't open their records." },
            { "language.switched", "ठीक है — अब मैं हिंदी में जवाब दूँगा। फ़ीस, परीक्षा, उपस्थिति या गृहकार्य के बारे में पूछिए।" },
            { "language.switched.en", "Switched back to English." },

            { "fallback", "I can help with fee status, exam schedules, attendance, homework, marks, notices, transport and library books. Could you rephrase that, or pick one of these?" },
            { "fallback.didyoumean", "I'm not certain I understood. Did you mean:" },
            { "error.nostudent", "I couldn't work out whose records to open. Pick a student from the sidebar and ask again." },

            { "chip.fee", "💰 Fee status" },
            { "chip.exam", "📅 Exam schedule" },
            { "chip.attendance", "📊 Attendance" },
            { "chip.practice", "📝 Practice test" },
            { "chip.homework", "📚 Homework" },
            { "chip.notice", "📢 Notices" },
            { "chip.bus", "🚌 Bus route" },
            { "chip.marks", "🏅 Marks" },
            { "chip.timetable", "🗓 Timetable" }
        };

        private static readonly Dictionary<string, string> Hi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "greeting.student", "नमस्ते {0}! मैं आपका EazyEdu सहायक हूँ। फ़ीस, परीक्षा, उपस्थिति, गृहकार्य, सूचनाएँ या बस मार्ग के बारे में पूछिए।" },
            { "greeting.parent", "नमस्ते! <b>{1}</b> ({2}) की ताज़ा जानकारी यहाँ है। फ़ीस, परीक्षा, उपस्थिति या गृहकार्य के बारे में कभी भी पूछें।" },
            { "identity", "मैं EazyEdu सहायक हूँ। मैं विद्यालय के रिकॉर्ड से फ़ीस, परीक्षा, उपस्थिति, गृहकार्य, अंक, सूचनाएँ, परिवहन और पुस्तकालय की जानकारी देता हूँ।" },
            { "thanks", "आपकी सहायता करके अच्छा लगा! और कुछ पूछना चाहेंगे?" },
            { "goodbye", "अलविदा — मन लगाकर पढ़िए, फिर मिलते हैं। 👋" },

            { "fee.header", "<b>{0} फ़ीस</b> — {1}" },
            { "fee.paid", "{1} में से {0} जमा ({2}%)।" },
            { "fee.remaining", "शेष <b>{0}</b>, अंतिम तिथि <b>{1}</b> ({2})।" },
            { "fee.cleared", "कोई बकाया नहीं — पूरी फ़ीस जमा हो चुकी है। 🎉" },
            { "fee.breakdown.title", "फ़ीस विवरण — {0} ({1})" },

            { "exam.title", "आगामी परीक्षाएँ — {0}" },
            { "exam.next", "अगला पेपर <b>{0}</b> है, <b>{1}</b> दिन बाद, {2} को {3} में। शुभकामनाएँ! 📘" },

            { "attendance.summary", "<b>{0}</b> ने इस सत्र में {2} में से {1} दिन उपस्थिति दर्ज की — <b>{3}%</b>।" },
            { "attendance.good", "यह 75% की अनिवार्यता से काफ़ी ऊपर है।" },
            { "attendance.ok", "यह न्यूनतम 75% से ऊपर है, पर ध्यान रखने योग्य है।" },
            { "attendance.low", "यह 75% से कम है — कृपया कक्षा अध्यापक से संपर्क करें।" },

            { "homework.title", "{0} का शेष गृहकार्य" },
            { "homework.none", "कोई गृहकार्य शेष नहीं है। 🎉" },
            { "notice.title", "सूचना पट" },
            { "marks.title", "नवीनतम परीक्षा परिणाम" },
            { "transport.line", "🚌 {0} की बस: <b>{1}</b>. चालक: {2}." },

            { "fallback", "मैं फ़ीस, परीक्षा, उपस्थिति, गृहकार्य, अंक, सूचनाएँ, परिवहन और पुस्तकालय में सहायता कर सकता हूँ। कृपया दोबारा पूछें या नीचे से चुनें।" },
            { "language.switched.en", "Switched back to English." },

            { "chip.fee", "💰 फ़ीस" },
            { "chip.exam", "📅 परीक्षा" },
            { "chip.attendance", "📊 उपस्थिति" },
            { "chip.practice", "📝 अभ्यास प्रश्नपत्र" },
            { "chip.homework", "📚 गृहकार्य" },
            { "chip.notice", "📢 सूचनाएँ" },
            { "chip.bus", "🚌 बस मार्ग" },
            { "chip.marks", "🏅 अंक" },
            { "chip.timetable", "🗓 समय-सारणी" }
        };

        public static bool IsSupported(string language)
        {
            return string.Equals(language, English, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(language, Hindi, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string language)
        {
            return string.Equals(language, Hindi, StringComparison.OrdinalIgnoreCase) ? Hindi : English;
        }

        public static string Get(string language, string key, params object[] args)
        {
            string template;

            if (string.Equals(language, Hindi, StringComparison.OrdinalIgnoreCase) && Hi.TryGetValue(key, out template))
                return Format(template, args);

            if (En.TryGetValue(key, out template))
                return Format(template, args);

            return key;
        }

        private static string Format(string template, object[] args)
        {
            if (args == null || args.Length == 0) return template;

            try
            {
                return string.Format(CultureInfo.CurrentCulture, template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }
    }
}
