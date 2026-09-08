using System;
using System.Configuration;
using System.IO;
using EazyEdu.Web.Data;
using EazyEdu.Web.Nlu;
using EazyEdu.Web.Security;

namespace EazyEdu.Web.Services
{
    /// <summary>
    /// Composition root. Everything is built once in Application_Start and shared;
    /// all of it is stateless or internally synchronised, so it is safe across requests.
    /// </summary>
    public static class AppServices
    {
        private static readonly object Gate = new object();
        private static bool _initialised;

        public static ISchoolRepository Repository { get; private set; }
        public static IntentEngine Engine { get; private set; }
        public static SentimentAnalyzer Sentiment { get; private set; }
        public static PracticePaperService Practice { get; private set; }
        public static ChatLogStore ChatLog { get; private set; }
        public static TicketStore Tickets { get; private set; }
        public static AnalyticsService Analytics { get; private set; }
        public static DocumentService Documents { get; private set; }
        public static AuthService Auth { get; private set; }
        public static ChatService Chat { get; private set; }
        public static RateLimiter Limiter { get; private set; }
        public static IAiCompletionProvider Ai { get; private set; }

        public static void Initialize(string dataDirectory)
        {
            lock (Gate)
            {
                if (_initialised) return;

                Repository = new InMemorySchoolRepository();
                Engine = new IntentEngine();
                Sentiment = new SentimentAnalyzer();
                Practice = new PracticePaperService(Repository);

                ChatLog = new ChatLogStore(Combine(dataDirectory, "chatlog.jsonl"));
                Tickets = new TicketStore(Combine(dataDirectory, "tickets.jsonl"));

                Analytics = new AnalyticsService(ChatLog);
                Documents = new DocumentService();
                Auth = new AuthService(Repository);

                Ai = string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["EazyEdu:AiApiKey"])
                    ? (IAiCompletionProvider)new NullAiCompletionProvider()
                    : new ClaudeCompletionProvider();

                Chat = new ChatService(Repository, Engine, Sentiment, Practice, ChatLog, Tickets, Ai);

                int limit = ReadInt("EazyEdu:RateLimitPerMinute", 40);
                Limiter = new RateLimiter(limit, TimeSpan.FromMinutes(1));

                _initialised = true;
            }
        }

        private static string Combine(string directory, string fileName)
        {
            return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, fileName);
        }

        private static int ReadInt(string key, int fallback)
        {
            int parsed;
            return int.TryParse(ConfigurationManager.AppSettings[key], out parsed) && parsed > 0 ? parsed : fallback;
        }
    }
}
