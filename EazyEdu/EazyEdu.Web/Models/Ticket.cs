using System;

namespace EazyEdu.Web.Models
{
    /// <summary>Raised when the assistant hands a conversation to the school office.</summary>
    public class SupportTicket
    {
        public string Id { get; set; }
        public string StudentId { get; set; }
        public string RaisedBy { get; set; }
        public string Role { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
