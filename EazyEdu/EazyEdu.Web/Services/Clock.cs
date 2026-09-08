using System;
using System.Configuration;
using System.Globalization;

namespace EazyEdu.Web.Services
{
    /// <summary>
    /// Single source of "now". The demo data is dated, so <c>EazyEdu:SimulatedToday</c>
    /// in Web.config can pin the clock; leave it empty to use the real system date.
    /// </summary>
    public static class Clock
    {
        private static readonly DateTime? Pinned = ReadPinnedDate();

        private static DateTime? ReadPinnedDate()
        {
            string raw = ConfigurationManager.AppSettings["EazyEdu:SimulatedToday"];
            if (string.IsNullOrWhiteSpace(raw)) return null;

            DateTime parsed;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed.Date;
            return null;
        }

        public static bool IsPinned { get { return Pinned.HasValue; } }

        public static DateTime Now
        {
            get
            {
                if (!Pinned.HasValue) return DateTime.Now;
                DateTime real = DateTime.Now;
                return Pinned.Value.Add(real.TimeOfDay);
            }
        }

        public static DateTime Today { get { return Now.Date; } }

        public static DateTime UtcNow
        {
            get { return Pinned.HasValue ? Now.ToUniversalTime() : DateTime.UtcNow; }
        }

        /// <summary>Whole days from today until <paramref name="date"/>; negative once it has passed.</summary>
        public static int DaysUntil(DateTime date)
        {
            return (int)Math.Ceiling((date.Date - Today).TotalDays);
        }
    }
}
