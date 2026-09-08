<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="EazyEdu.Web.DefaultPage" %>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>EazyEdu — School Assistant</title>
<link rel="stylesheet" href="<%= ResolveUrl("~/Content/eazyedu.css") %>" />
</head>
<body>

<div class="app">

  <!-- ============================ SIDEBAR ============================ -->
  <aside class="sidebar">
    <div class="brand">
      <div class="brand-mark">E</div>
      <div>
        <div class="brand-name">EazyEdu</div>
        <div class="brand-sub">School companion</div>
      </div>
    </div>

    <div class="role-switch" id="roleSwitch">
      <button type="button" data-role="student" class="active">Student</button>
      <button type="button" data-role="parent">Parent</button>
    </div>

    <div class="child-select" id="studentSelectWrap" hidden>
      <label for="studentSelect">Viewing</label>
      <select id="studentSelect"></select>
    </div>

    <div class="card">
      <div class="card-title">Attendance <span id="attendanceCanMiss" class="card-note"></span></div>
      <div class="attendance-row">
        <div class="ring-wrap">
          <svg width="74" height="74" viewBox="0 0 74 74" aria-hidden="true">
            <circle cx="37" cy="37" r="31" fill="none" stroke="rgba(255,255,255,.12)" stroke-width="7" />
            <circle id="attendanceRing" cx="37" cy="37" r="31" fill="none" stroke="var(--success)" stroke-width="7" stroke-linecap="round" />
          </svg>
          <div class="ring-label" id="attendancePct">—</div>
        </div>
        <div class="attendance-meta">
          <div id="attendanceDays">—</div>
          <div>days present this term</div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-title">Fee status <span id="feeYear" class="card-note"></span></div>
      <div class="fee-amounts">
        <span class="paid" id="feePaid">—</span>
        <span class="of" id="feeTotal">of —</span>
      </div>
      <div class="bar-track"><div class="bar-fill" id="feeBar" style="width:0%"></div></div>
      <span class="fee-status-tag" id="feeTag">—</span>
    </div>

    <div class="card">
      <div class="card-title">Next exam</div>
      <div class="exam-row">
        <div class="exam-days" id="examDays">—<sub>days</sub></div>
        <div>
          <div class="exam-name" id="examName">—</div>
          <div class="exam-date" id="examDate">—</div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-title">Notice board</div>
      <div class="ticker-mask"><div class="ticker-track" id="noticeTicker"></div></div>
    </div>

    <div class="sidebar-footer">
      <button type="button" class="pill-btn" id="themeToggle">🌓 <span id="themeLabel">Dark mode</span></button>
      <button type="button" class="pill-btn" id="languageToggle" title="Switch reply language">🌐 <span id="languageLabel">हिंदी</span></button>
    </div>

    <div class="sidebar-account">
      <div>
        <div class="account-name"><%= Server.HtmlEncode(DisplayName) %></div>
        <div class="account-role"><%= Server.HtmlEncode(RoleLabel) %> · <span id="todayLabel"><%= Server.HtmlEncode(TodayLabel) %></span></div>
      </div>
      <div class="account-actions">
        <% if (IsAdmin) { %><a class="link-btn" href="<%= ResolveUrl("~/Admin/Analytics.aspx") %>">Analytics</a><% } %>
        <a class="link-btn" href="<%= ResolveUrl("~/Logout.aspx") %>">Sign out</a>
      </div>
    </div>
  </aside>

  <!-- ============================= CHAT ============================= -->
  <section class="chat-col">
    <header class="chat-header">
      <div class="chat-header-left">
        <div class="avatar">E</div>
        <div>
          <div class="chat-title">EazyEdu Assistant</div>
          <div class="chat-status"><span class="dot" id="statusDot"></span> <span id="statusText">Online — fees, exams, attendance &amp; more</span></div>
        </div>
      </div>
      <div class="header-actions">
        <button type="button" class="icon-btn" id="speakBtn" title="Read answers aloud" aria-pressed="false">🔈</button>
        <button type="button" class="icon-btn" id="debugBtn" title="Show intent debug panel" aria-pressed="false">🧠</button>
        <button type="button" class="icon-btn" id="notifBtn" title="Notices" aria-expanded="false">🔔<span class="badge-dot" id="notifDot"></span></button>
        <button type="button" class="icon-btn" id="clearBtn" title="Clear chat">🗑</button>
      </div>
      <div class="notif-panel" id="notifPanel" role="dialog" aria-label="Notices"></div>
    </header>

    <div class="debug-bar" id="debugBar" hidden></div>

    <div class="messages" id="messages" role="log" aria-live="polite"></div>

    <div class="chips-row" id="chipsRow"></div>

    <div class="composer">
      <div class="composer-inner">
        <textarea id="composerInput" rows="1" maxlength="500" placeholder="Ask about fees, exams, attendance, homework…" aria-label="Message"></textarea>
        <button type="button" class="round-btn mic-btn" id="micBtn" title="Voice input">🎙</button>
        <button type="button" class="round-btn send-btn" id="sendBtn" title="Send">➤</button>
      </div>
      <div class="composer-hint">
        Answers come from your school's live records. Verify critical dates with the school office.
        <span class="hint-sep">·</span> Enter to send, Shift+Enter for a new line.
      </div>
    </div>
  </section>

</div>

<script>
  window.EazyEdu = {
    token: <%= ClientToken %>,
    urls: {
      chat: <%= ClientChatUrl %>,
      dashboard: <%= ClientDashboardUrl %>,
      session: <%= ClientSessionUrl %>,
      feedback: <%= ClientFeedbackUrl %>,
      download: <%= ClientDownloadUrl %>
    },
    role: <%= ClientRole %>,
    studentId: <%= ClientStudentId %>,
    language: <%= ClientLanguage %>
  };
</script>
<script src="<%= ResolveUrl("~/Scripts/eazyedu.js") %>"></script>

</body>
</html>
