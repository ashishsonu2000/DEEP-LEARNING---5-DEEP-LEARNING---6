# EazyEdu Assistant — ASP.NET Web Forms (.NET Framework 4.8)

A school-companion chatbot for students and parents, built as a classic **ASP.NET Web Forms
web application targeting .NET Framework 4.8**, with C# on the server and a dependency-free
ES5 client. It answers questions about fees, exams, attendance, homework, marks, notices,
transport and the library from the school's records, generates practice papers, and hands
over to the school office when a person is needed.

No NuGet packages, no external JS/CSS libraries — it builds and runs on a stock
Visual Studio + IIS install, which is what most school deployments actually have.

---

## Running it

1. Open `EazyEdu.sln` in Visual Studio 2019/2022 (workload: *ASP.NET and web development*).
2. Press **F5** — IIS Express serves it at `http://localhost:50321/`.
3. Sign in with a demo account (password `eazyedu123` for all of them):

| User name        | Role          | Sees                                |
|------------------|---------------|-------------------------------------|
| `aarav`          | Student       | Own records (Class 8-B)             |
| `isha`           | Student       | Own records (Class 5-A)             |
| `parent.sharma`  | Parent        | Both children, with a child switcher|
| `teacher.mehta`  | Class teacher | Every seeded student                |
| `admin`          | School office | Everything + the analytics dashboard|

To deploy to IIS: publish the project to a folder, point an application at it, and give the
app-pool identity **write access to `App_Data`** (chat log and tickets are stored there).
The app runs read-only without it — it just loses history across restarts.

### Configuration (`Web.config` → `appSettings`)

| Key | Meaning |
|-----|---------|
| `EazyEdu:SimulatedToday` | Pin the clock (e.g. `2026-09-08`) so the dated demo data always looks current. Empty = real system date. |
| `EazyEdu:RateLimitPerMinute` | Chat requests allowed per user per minute (default 40). |
| `EazyEdu:AiApiKey` | Optional. Set it to enable the generative fallback for questions the rule engine cannot classify. Empty = fully offline, rule-driven only. |
| `EazyEdu:AiModel` | `claude-sonnet-5` (default) or `claude-opus-5`. |

---

## What it does

**Conversation**
- Intent recognition over 29 intents with confidence scores, typo tolerance
  ("attendence" → attendance), SMS shorthand ("hw", "plz", "ptm") and contraction expansion.
- Entity extraction: subject, difficulty, timeframe/date, child name, language, counts.
- Multi-turn slot filling — "practice test" → "which subject?" → "science" → paper, then
  "answer key" without repeating the subject.
- "Did you mean" chips built from the runner-up intents when confidence is low.
- Sentiment analysis; a complaint or a request for a human raises a support ticket
  automatically and returns the ticket number.
- Loop detection: three unrecognised questions in a row and it offers the school office.
- English/Hindi replies, switchable from the sidebar or by asking ("reply in hindi").

**School records**
- Fees: balance, due date and overdue days, head-wise breakdown, payment history,
  downloadable statement.
- Exams: datesheet with countdown and venue, per-subject syllabus.
- Attendance: percentage, 75%-rule verdict, how many more days can be missed, day-by-day log.
- Homework (with overdue flagging), marks with grades and a strongest/weakest read,
  notices, timetable for today or tomorrow, transport, library loans and fines.
- Practice papers generated per subject and difficulty, date-seeded so a paper is stable
  through the day and new the next, with an answer key and a text download.

**Platform**
- Forms authentication with PBKDF2-SHA256 password hashing, role-aware authorisation, and
  server-side enforcement that an account only ever reads students linked to it.
- Anti-CSRF token on every API call, sliding-window rate limiting, HTML encoding of every
  record value that reaches a bubble, security headers, `App_Data` blocked from being served.
- Chat log and tickets persisted as JSON lines; transcript export; thumbs up/down per answer.
- Admin analytics: top intents, confidence, fallback rate, answer latency, hourly volume,
  unrecognised questions to train on, open tickets, CSV export.
- UI: light/dark theme, voice input (Web Speech API), read-aloud, notice ticker, quick-reply
  chips, draft persistence, offline detection, and an intent-debug panel (🧠 in the header).

---

## Layout

```
EazyEdu.Web/
├── Models/          Domain records — student, fees, exams, chat, NLU, tickets
├── Data/            ISchoolRepository + in-memory implementation, JSON-lines stores
├── Nlu/             Normaliser, string metrics, intent catalog/engine, entities, sentiment
├── Services/        ChatService (answer composition), practice papers, documents,
│                    analytics, localisation, rate limiter, clock, optional AI provider
├── Security/        Password hashing, CSRF, forms-auth service
├── Api/             .ashx JSON endpoints: chat, dashboard, session, feedback, download
├── Admin/           Analytics dashboard (school office only)
├── Content/         Single stylesheet
├── Scripts/         Single ES5 client
├── Default.aspx     Chat UI      Login.aspx / Logout.aspx
└── Global.asax      Startup, culture, auth ticket → principal, security headers
```

### How a question is answered

```
browser → POST /Api/chat.ashx
        → ApiHandlerBase: auth, CSRF, rate limit
        → SessionBootstrap: reconcile requested role/student/language with what is allowed
        → IntentEngine: normalise → spell-correct → score every intent three ways
                        (regex trigger, keyword coverage, example-phrase overlap)
        → SentimentAnalyzer: complaint? → raise a ticket instead
        → ChatService: look the answer up in ISchoolRepository, render HTML + quick replies
        → ChatLogStore: log both turns for transcripts and analytics
        → JSON response
```

### Extending it

- **New intent**: add a definition to `Nlu/IntentCatalog.cs` (keywords, patterns, examples)
  and a `case` in `ChatService.Dispatch`. Nothing else changes.
- **Real database**: implement `ISchoolRepository` against your tables and swap the line in
  `Services/AppServices.Initialize`. The chat layer has no knowledge of the storage.
- **New language**: add the keys to `Services/Localization.cs`; missing keys fall back to
  English, so a partial translation is safe to ship.

The analytics page lists every question the assistant could not place — those phrasings are
exactly what should be added to the intent catalog as examples.
