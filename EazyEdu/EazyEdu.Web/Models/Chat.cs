using System;
using System.Collections.Generic;

namespace EazyEdu.Web.Models
{
    /// <summary>Payload posted by the browser for every chat turn.</summary>
    public class ChatRequest
    {
        public string Message { get; set; }
        public string Role { get; set; }
        public string StudentId { get; set; }
        public string Language { get; set; }
        public string ClientMessageId { get; set; }
    }

    public class QuickReply
    {
        public QuickReply() { }

        public QuickReply(string label, string query)
        {
            Label = label;
            Query = query;
        }

        public string Label { get; set; }
        /// <summary>Text sent back as the next user message when the chip is tapped.</summary>
        public string Query { get; set; }
        /// <summary>Client-side action instead of a query: download, answerKey, escalate…</summary>
        public string Action { get; set; }
        /// <summary>Optional argument carried by the action (e.g. a subject name).</summary>
        public string Argument { get; set; }
    }

    /// <summary>A single card rendered under a bot bubble (fee summary, exam list…).</summary>
    public class ChatCard
    {
        public string Kind { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<string[]> Rows { get; set; }
        public string[] Columns { get; set; }

        public ChatCard()
        {
            Rows = new List<string[]>();
        }
    }

    public class ChatResponse
    {
        public ChatResponse()
        {
            Replies = new List<QuickReply>();
            Cards = new List<ChatCard>();
        }

        public bool Ok { get; set; }
        public string Html { get; set; }
        public string PlainText { get; set; }
        public List<QuickReply> Replies { get; set; }
        public List<ChatCard> Cards { get; set; }

        public string Intent { get; set; }
        public double Confidence { get; set; }
        public string Sentiment { get; set; }
        public bool Escalated { get; set; }
        public string TicketId { get; set; }
        public string Source { get; set; }
        public string[] Suggestions { get; set; }
        /// <summary>Id of the logged bot turn, echoed back when the user rates the answer.</summary>
        public string TurnId { get; set; }
        /// <summary>Set when the answer changed the active student (parent switching child).</summary>
        public string StudentId { get; set; }
        /// <summary>Set when the answer changed the reply language.</summary>
        public string Language { get; set; }
        public string Error { get; set; }
        /// <summary>Milliseconds spent building the answer, surfaced in the debug pane.</summary>
        public long ElapsedMs { get; set; }
        public string ServerTime { get; set; }
    }

    public class ChatTurn
    {
        public string Id { get; set; }
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public string StudentId { get; set; }
        public string Role { get; set; }
        public string Author { get; set; }
        public string Text { get; set; }
        public string Intent { get; set; }
        public double Confidence { get; set; }
        public string Sentiment { get; set; }
        public DateTime TimestampUtc { get; set; }
        public long ElapsedMs { get; set; }
        /// <summary>up / down / null — set by the thumb buttons under a bot bubble.</summary>
        public string Feedback { get; set; }
    }
}
