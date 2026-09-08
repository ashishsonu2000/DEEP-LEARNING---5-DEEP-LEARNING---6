using System;
using System.Collections.Generic;
using System.Linq;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Admin
{
    public partial class AnalyticsPage : System.Web.UI.Page
    {
        protected AnalyticsSummary Summary { get; private set; }
        protected IList<SupportTicket> Tickets { get; private set; }
        protected int TicketCount { get { return Tickets.Count; } }

        private int _peakHourVolume = 1;

        protected void Page_Load(object sender, EventArgs e)
        {
            UserAccount user = AppServices.Auth.GetCurrentUser(Context);
            if (user == null || user.Role != UserRoles.Admin)
            {
                Response.Redirect("~/Default.aspx", true);
                return;
            }

            Summary = AppServices.Analytics.Summarize();
            Tickets = AppServices.Tickets.All();
            _peakHourVolume = Math.Max(1, Summary.HourlyVolume.Max());
        }

        /// <summary>Bar height in pixels, scaled against the busiest hour.</summary>
        protected int HourHeight(int hour)
        {
            int value = Summary.HourlyVolume[hour];
            if (value == 0) return 2;
            return (int)Math.Round(6 + (value / (double)_peakHourVolume) * 74);
        }
    }
}
