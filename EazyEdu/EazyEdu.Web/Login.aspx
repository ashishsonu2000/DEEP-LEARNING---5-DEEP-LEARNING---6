<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="EazyEdu.Web.LoginPage" %>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Sign in — EazyEdu Assistant</title>
<link rel="stylesheet" href="<%= ResolveUrl("~/Content/eazyedu.css") %>" />
</head>
<body class="login-body">

<main class="login-card">
  <div class="login-brand">
    <div class="brand-mark">E</div>
    <div>
      <div class="brand-name">EazyEdu</div>
      <div class="brand-sub">School companion</div>
    </div>
  </div>

  <h1 class="login-title">Sign in</h1>
  <p class="login-sub">Use your school account to open the assistant.</p>

  <% if (!string.IsNullOrEmpty(ErrorMessage)) { %>
    <div class="login-error" role="alert"><%= Server.HtmlEncode(ErrorMessage) %></div>
  <% } %>

  <form method="post" action="<%= ResolveUrl("~/Login.aspx") %>" autocomplete="on">
    <input type="hidden" name="csrf" value="<%= Server.HtmlEncode(FormToken) %>" />

    <label class="field">
      <span>User name</span>
      <input type="text" name="userName" value="<%= Server.HtmlEncode(SubmittedUserName) %>" required autofocus />
    </label>

    <label class="field">
      <span>Password</span>
      <input type="password" name="password" required />
    </label>

    <label class="check">
      <input type="checkbox" name="remember" value="1" /> Keep me signed in on this device
    </label>

    <button type="submit" class="login-btn">Sign in</button>
  </form>

  <div class="login-demo">
    <b>Demo accounts</b> — password <code>eazyedu123</code>
    <ul>
      <li><code>aarav</code> — student, Class 8-B</li>
      <li><code>parent.sharma</code> — parent of Aarav and Isha</li>
      <li><code>teacher.mehta</code> — class teacher</li>
      <li><code>admin</code> — school office, sees the analytics dashboard</li>
    </ul>
  </div>
</main>

</body>
</html>
