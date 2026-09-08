using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Data
{
    /// <summary>Support tickets raised when the assistant hands over to the school office.</summary>
    public class TicketStore
    {
        private readonly JsonLineStore<SupportTicket> _store;
        private int _sequence;

        public TicketStore(string path)
        {
            _store = new JsonLineStore<SupportTicket>(path);
            _sequence = _store.Count;
        }

        public SupportTicket Create(string studentId, string raisedBy, string role, string category, string message)
        {
            int number = Interlocked.Increment(ref _sequence);

            var ticket = new SupportTicket
            {
                Id = string.Format(CultureInfo.InvariantCulture, "EE-{0:yyMMdd}-{1:D4}", Clock.Today, number),
                StudentId = studentId,
                RaisedBy = raisedBy,
                Role = role,
                Category = category,
                Message = message,
                Status = "Open",
                CreatedUtc = DateTime.UtcNow
            };

            _store.Add(ticket);
            return ticket;
        }

        public IList<SupportTicket> All()
        {
            return _store.Snapshot().OrderByDescending(t => t.CreatedUtc).ToList();
        }
    }
}
