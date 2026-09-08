using System;
using System.Collections.Generic;
using System.Linq;
using EazyEdu.Web.Models;

namespace EazyEdu.Web.Data
{
    /// <summary>Every turn of every conversation, used for transcripts and the analytics page.</summary>
    public class ChatLogStore
    {
        private readonly JsonLineStore<ChatTurn> _store;

        public ChatLogStore(string path)
        {
            _store = new JsonLineStore<ChatTurn>(path);
        }

        public void Add(ChatTurn turn)
        {
            if (turn == null) return;
            if (string.IsNullOrEmpty(turn.Id)) turn.Id = Guid.NewGuid().ToString("N");
            if (turn.TimestampUtc == default(DateTime)) turn.TimestampUtc = DateTime.UtcNow;
            _store.Add(turn);
        }

        public IList<ChatTurn> GetSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return new List<ChatTurn>();
            return _store.Snapshot()
                         .Where(t => string.Equals(t.SessionId, sessionId, StringComparison.Ordinal))
                         .OrderBy(t => t.TimestampUtc)
                         .ToList();
        }

        public IList<ChatTurn> GetRecent(int count)
        {
            return _store.Snapshot()
                         .OrderByDescending(t => t.TimestampUtc)
                         .Take(count)
                         .ToList();
        }

        public IList<ChatTurn> All()
        {
            return _store.Snapshot();
        }

        public bool SetFeedback(string turnId, string feedback)
        {
            if (string.IsNullOrEmpty(turnId)) return false;

            bool found = false;
            _store.Mutate(items =>
            {
                foreach (ChatTurn turn in items)
                {
                    if (!string.Equals(turn.Id, turnId, StringComparison.Ordinal)) continue;
                    turn.Feedback = feedback;
                    found = true;
                    break;
                }
            });
            return found;
        }
    }
}
