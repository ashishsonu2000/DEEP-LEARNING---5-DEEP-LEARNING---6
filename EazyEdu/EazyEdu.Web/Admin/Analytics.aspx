<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Analytics.aspx.cs" Inherits="EazyEdu.Web.Admin.AnalyticsPage" %>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Assistant analytics — EazyEdu</title>
<link rel="stylesheet" href="<%= ResolveUrl("~/Content/eazyedu.css") %>" />
</head>
<body class="admin-body">

<header class="admin-header">
  <div class="brand">
    <div class="brand-mark">E</div>
    <div>
      <div class="brand-name">EazyEdu analytics</div>
      <div class="brand-sub">How the assistant is being used</div>
    </div>
  </div>
  <div class="admin-actions">
    <a class="link-btn" href="<%= ResolveUrl("~/Api/download.ashx?type=analytics") %>">Export CSV</a>
    <a class="link-btn" href="<%= ResolveUrl("~/Default.aspx") %>">Back to chat</a>
    <a class="link-btn" href="<%= ResolveUrl("~/Logout.aspx") %>">Sign out</a>
  </div>
</header>

<main class="admin-main">

  <section class="kpi-grid">
    <div class="kpi"><span class="kpi-value"><%= Summary.UserTurns %></span><span class="kpi-label">questions asked</span></div>
    <div class="kpi"><span class="kpi-value"><%= Summary.Sessions %></span><span class="kpi-label">conversations</span></div>
    <div class="kpi"><span class="kpi-value"><%= Summary.AverageConfidence.ToString("P0") %></span><span class="kpi-label">avg. confidence</span></div>
    <div class="kpi"><span class="kpi-value"><%= Summary.FallbackRate.ToString("0.#") %>%</span><span class="kpi-label">not understood</span></div>
    <div class="kpi"><span class="kpi-value"><%= Summary.AverageResponseMs.ToString("0") %> ms</span><span class="kpi-label">avg. answer time</span></div>
    <div class="kpi"><span class="kpi-value"><%= Summary.ThumbsUp %> / <%= Summary.ThumbsDown %></span><span class="kpi-label">👍 / 👎</span></div>
    <div class="kpi"><span class="kpi-value"><%= Summary.NegativeMessages %></span><span class="kpi-label">unhappy messages</span></div>
    <div class="kpi"><span class="kpi-value"><%= TicketCount %></span><span class="kpi-label">tickets raised</span></div>
  </section>

  <section class="admin-panel">
    <h2>What people ask about</h2>
    <% if (Summary.TopIntents.Count == 0) { %>
      <p class="muted">No conversations recorded yet. Ask the assistant a few questions and refresh.</p>
    <% } else { %>
      <table class="admin-table">
        <thead><tr><th>Topic</th><th>Asked</th><th>Share</th><th>Avg. confidence</th><th></th></tr></thead>
        <tbody>
        <% foreach (var stat in Summary.TopIntents) { %>
          <tr>
            <td><%= Server.HtmlEncode(stat.Label) %><div class="muted small"><%= Server.HtmlEncode(stat.Intent) %></div></td>
            <td><%= stat.Count %></td>
            <td><%= stat.Share.ToString("0.#") %>%</td>
            <td><%= stat.AverageConfidence.ToString("P0") %></td>
            <td class="bar-cell"><span class="admin-bar" style="width:<%= Math.Min(100, stat.Share * 2) %>%"></span></td>
          </tr>
        <% } %>
        </tbody>
      </table>
    <% } %>
  </section>

  <section class="admin-panel">
    <h2>When they ask</h2>
    <div class="hour-chart">
      <% for (int hour = 0; hour < 24; hour++) { %>
        <div class="hour-col" title="<%= hour %>:00 — <%= Summary.HourlyVolume[hour] %> question(s)">
          <span style="height:<%= HourHeight(hour) %>px"></span>
          <em><%= hour % 3 == 0 ? hour.ToString() : "" %></em>
        </div>
      <% } %>
    </div>
  </section>

  <section class="admin-panel">
    <h2>Questions the assistant could not place</h2>
    <% if (Summary.UnrecognizedQueries.Count == 0) { %>
      <p class="muted">Nothing here — every question so far matched an intent.</p>
    <% } else { %>
      <ul class="plain-list">
        <% foreach (string query in Summary.UnrecognizedQueries) { %>
          <li><%= Server.HtmlEncode(query) %></li>
        <% } %>
      </ul>
      <p class="muted small">Add these phrasings to <code>Nlu/IntentCatalog.cs</code> as examples to train them in.</p>
    <% } %>
  </section>

  <section class="admin-panel">
    <h2>Open tickets</h2>
    <% if (Tickets.Count == 0) { %>
      <p class="muted">No escalations yet.</p>
    <% } else { %>
      <table class="admin-table">
        <thead><tr><th>Ticket</th><th>Raised</th><th>By</th><th>Student</th><th>Message</th><th>Status</th></tr></thead>
        <tbody>
        <% foreach (var ticket in Tickets) { %>
          <tr>
            <td><code><%= Server.HtmlEncode(ticket.Id) %></code></td>
            <td><%= ticket.CreatedUtc.ToLocalTime().ToString("dd MMM, HH:mm") %></td>
            <td><%= Server.HtmlEncode(ticket.RaisedBy) %> <span class="muted">(<%= Server.HtmlEncode(ticket.Role) %>)</span></td>
            <td><%= Server.HtmlEncode(ticket.StudentId) %></td>
            <td><%= Server.HtmlEncode(ticket.Message) %></td>
            <td><span class="pill neutral"><%= Server.HtmlEncode(ticket.Status) %></span></td>
          </tr>
        <% } %>
        </tbody>
      </table>
    <% } %>
  </section>

</main>

</body>
</html>
