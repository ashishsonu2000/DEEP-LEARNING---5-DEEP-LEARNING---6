using System;
using System.Collections.Generic;
using System.Web;

namespace EazyEdu.Web.Services
{
    /// <summary>
    /// Short-term memory for one browser session: who we are talking about, which
    /// question the assistant is waiting on, and what was last discussed. This is what
    /// makes "practice test" → "science" → "answer key" work as one conversation.
    /// </summary>
    [Serializable]
    public class ConversationState
    {
        private const string SessionKey = "EazyEdu.Conversation";

        public ConversationState()
        {
            RecentIntents = new List<string>();
            Language = "en";
        }

        public string StudentId { get; set; }
        public string Role { get; set; }
        public string Language { get; set; }

        /// <summary>Intent the assistant asked a follow-up question for, if any.</summary>
        public string PendingIntent { get; set; }
        /// <summary>The slot that follow-up is trying to fill (currently only "subject").</summary>
        public string PendingSlot { get; set; }

        public string LastSubject { get; set; }
        public string LastPaperSubject { get; set; }
        public string LastIntent { get; set; }
        public int TurnCount { get; set; }
        public List<string> RecentIntents { get; private set; }
        public DateTime? GreetedAtUtc { get; set; }

        public void Remember(string intent)
        {
            LastIntent = intent;
            TurnCount++;
            RecentIntents.Add(intent);
            if (RecentIntents.Count > 12) RecentIntents.RemoveAt(0);
        }

        public void ClearPending()
        {
            PendingIntent = null;
            PendingSlot = null;
        }

        public void AwaitSlot(string intent, string slot)
        {
            PendingIntent = intent;
            PendingSlot = slot;
        }

        /// <summary>True when the same intent has been asked three times in a row — time to offer a human.</summary>
        public bool IsLooping(string intent)
        {
            int count = 0;
            for (int i = RecentIntents.Count - 1; i >= 0 && count < 3; i--)
            {
                if (RecentIntents[i] != intent) break;
                count++;
            }
            return count >= 3;
        }

        public static ConversationState FromSession(HttpContext context)
        {
            if (context == null || context.Session == null) return new ConversationState();

            var state = context.Session[SessionKey] as ConversationState;
            if (state == null)
            {
                state = new ConversationState();
                context.Session[SessionKey] = state;
            }
            return state;
        }

        public static void Save(HttpContext context, ConversationState state)
        {
            if (context == null || context.Session == null) return;
            context.Session[SessionKey] = state;
        }

        public static void Reset(HttpContext context)
        {
            if (context == null || context.Session == null) return;
            context.Session[SessionKey] = new ConversationState();
        }
    }
}
