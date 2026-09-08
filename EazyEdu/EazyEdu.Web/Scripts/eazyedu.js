/* =============================================================
   EazyEdu Assistant — browser client
   Talks to the .ashx endpoints, renders the chat and the sidebar.
   Written against ES5 + XMLHttpRequest so it also runs on the
   older browsers still found in school computer labs.
   ============================================================= */
(function () {
    'use strict';

    var cfg = window.EazyEdu || {};
    var state = {
        role: cfg.role || 'student',
        studentId: cfg.studentId || '',
        language: cfg.language || 'en',
        studentName: '',
        speaking: false,
        listening: false,
        debug: false,
        busy: false,
        dashboard: null
    };

    var el = {};
    var STORAGE_THEME = 'eazyedu.theme';
    var STORAGE_DRAFT = 'eazyedu.draft';

    // ---------------------------------------------------------- utilities

    function $(id) { return document.getElementById(id); }

    function escapeHtml(value) {
        return String(value === null || value === undefined ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function store(key, value) {
        try { window.localStorage.setItem(key, value); } catch (e) { /* private mode */ }
    }

    function read(key) {
        try { return window.localStorage.getItem(key); } catch (e) { return null; }
    }

    /**
     * Minimal JSON transport. Every call carries the anti-forgery token; a non-2xx
     * response is surfaced with the server's own message so the user sees something useful.
     */
    function request(method, url, body, onSuccess, onError) {
        var xhr = new XMLHttpRequest();
        xhr.open(method, url, true);
        xhr.setRequestHeader('Accept', 'application/json');
        xhr.setRequestHeader('X-EazyEdu-Token', cfg.token || '');
        if (body) xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');

        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) return;

            var payload = null;
            try { payload = JSON.parse(xhr.responseText); } catch (e) { payload = null; }

            if (xhr.status >= 200 && xhr.status < 300) {
                onSuccess(payload || {});
                return;
            }

            var message = (payload && payload.error) || 'The assistant is not reachable right now.';
            if (xhr.status === 401) message = 'Your session expired. Reloading the sign-in page…';
            if (onError) onError(message, xhr.status);
            if (xhr.status === 401) window.setTimeout(function () { window.location.reload(); }, 1500);
        };

        xhr.send(body ? JSON.stringify(body) : null);
    }

    // ---------------------------------------------------------- sidebar

    function renderDashboard(data) {
        if (!data || data.ok === false) {
            setStatus(false, (data && data.error) || 'No records available.');
            return;
        }

        state.dashboard = data;
        state.role = data.role;
        state.studentId = data.student.id;
        state.studentName = data.student.name;
        state.language = data.language;

        el.languageLabel.textContent = data.language === 'hi' ? 'English' : 'हिंदी';
        el.todayLabel.textContent = data.today + (data.simulatedClock ? ' (demo clock)' : '');

        // Role switch
        var buttons = el.roleSwitch.getElementsByTagName('button');
        for (var i = 0; i < buttons.length; i++) {
            var isActive = buttons[i].getAttribute('data-role') === data.role;
            buttons[i].className = isActive ? 'active' : '';
        }

        // Student picker — shown whenever the account can see more than one child
        var showPicker = data.students.length > 1;
        el.studentSelectWrap.hidden = !showPicker;
        if (showPicker) {
            el.studentSelect.innerHTML = '';
            for (var s = 0; s < data.students.length; s++) {
                var option = document.createElement('option');
                option.value = data.students[s].id;
                option.textContent = data.students[s].name + ' — ' + data.students[s].className;
                if (data.students[s].id === data.student.id) option.selected = true;
                el.studentSelect.appendChild(option);
            }
        }

        // Attendance ring
        var circumference = 2 * Math.PI * 31;
        var percent = data.attendance.percent;
        el.attendanceRing.setAttribute('stroke-dasharray', circumference);
        el.attendanceRing.setAttribute('stroke-dashoffset', circumference * (1 - percent / 100));
        el.attendanceRing.style.stroke = percent >= 85 ? 'var(--success)' : (percent >= 75 ? 'var(--gold)' : 'var(--danger)');
        el.attendancePct.textContent = percent + '%';
        el.attendanceDays.textContent = data.attendance.present + ' / ' + data.attendance.total;
        el.attendanceCanMiss.textContent = percent >= 75 ? data.attendance.canMiss + ' day(s) spare' : 'below 75%';

        // Fee
        el.feeYear.textContent = data.fee.academicYear;
        el.feePaid.textContent = data.fee.paidText;
        el.feeTotal.textContent = 'of ' + data.fee.totalText;
        el.feeBar.style.width = data.fee.percent + '%';
        if (data.fee.cleared) {
            el.feeTag.textContent = 'Fully paid';
            el.feeTag.className = 'fee-status-tag ok';
        } else {
            el.feeTag.textContent = data.fee.balanceText + ' due ' + (data.fee.dueDate || 'soon');
            el.feeTag.className = 'fee-status-tag due';
        }

        // Next exam
        if (data.nextExam) {
            el.examDays.innerHTML = data.nextExam.days + '<sub>days</sub>';
            el.examName.textContent = data.nextExam.subject;
            el.examDate.textContent = data.nextExam.date + ' · ' + data.nextExam.room;
        } else {
            el.examDays.innerHTML = '—<sub>days</sub>';
            el.examName.textContent = 'No exam scheduled';
            el.examDate.textContent = '';
        }

        renderNotices(data.notices);
        renderChips(data.chips);
        setStatus(true);
    }

    function renderNotices(notices) {
        var ticker = '';
        var panel = '';

        for (var i = 0; i < notices.length; i++) {
            ticker += '<div>• ' + escapeHtml(notices[i].body) + '</div>';
            panel += '<div class="notif-item"><b>' + (notices[i].priority ? '📌 ' : '📢 ') +
                     escapeHtml(notices[i].title) + '</b><span>' + escapeHtml(notices[i].body) +
                     ' · ' + escapeHtml(notices[i].postedOn) + '</span></div>';
        }

        // Repeat the first line so the ticker loop has no visible seam.
        if (notices.length > 0) ticker += '<div>• ' + escapeHtml(notices[0].body) + '</div>';

        el.noticeTicker.innerHTML = ticker;
        el.notifPanel.innerHTML = panel || '<div class="notif-item"><span>No notices right now.</span></div>';
        el.notifDot.style.display = notices.length ? 'block' : 'none';
    }

    function renderChips(chips) {
        el.chipsRow.innerHTML = '';
        for (var i = 0; i < chips.length; i++) {
            (function (chip) {
                var button = document.createElement('button');
                button.type = 'button';
                button.className = 'chip';
                button.textContent = chip.label;
                button.onclick = function () { send(chip.query); };
                el.chipsRow.appendChild(button);
            })(chips[i]);
        }
    }

    function setStatus(online, message) {
        el.chatStatus.className = 'chat-status' + (online ? '' : ' offline');
        el.statusText.textContent = message || (online
            ? 'Online — fees, exams, attendance & more'
            : 'Offline — reconnecting…');
    }

    // ---------------------------------------------------------- messages

    function timeNow() {
        var now = new Date();
        var hours = now.getHours();
        var minutes = now.getMinutes();
        return (hours < 10 ? '0' : '') + hours + ':' + (minutes < 10 ? '0' : '') + minutes;
    }

    function addMessage(author, html, options) {
        options = options || {};

        var row = document.createElement('div');
        row.className = 'msg-row ' + author;

        var initial = author === 'bot' ? 'E' : (state.studentName ? state.studentName.charAt(0) : 'Y');
        var avatar = document.createElement('div');
        avatar.className = 'msg-avatar';
        avatar.textContent = initial;

        var column = document.createElement('div');

        var bubble = document.createElement('div');
        bubble.className = 'msg-bubble';
        bubble.innerHTML = html;
        column.appendChild(bubble);

        if (options.replies && options.replies.length) {
            column.appendChild(buildReplies(options.replies));
        }

        var meta = document.createElement('div');
        meta.className = 'msg-meta';

        var time = document.createElement('span');
        time.className = 'msg-time';
        time.textContent = options.time || timeNow();
        meta.appendChild(time);

        if (author === 'bot' && options.turnId) {
            meta.appendChild(buildRating(options.turnId));
        }

        column.appendChild(meta);
        row.appendChild(avatar);
        row.appendChild(column);
        el.messages.appendChild(row);
        el.messages.scrollTop = el.messages.scrollHeight;

        if (author === 'bot' && state.speaking && options.plainText) speak(options.plainText);
        return row;
    }

    function buildReplies(replies) {
        var wrap = document.createElement('div');
        wrap.className = 'quick-replies';

        for (var i = 0; i < replies.length; i++) {
            (function (reply) {
                var button = document.createElement('button');
                button.type = 'button';
                button.textContent = reply.Label || reply.label;
                button.onclick = function () {
                    var action = reply.Action || reply.action;
                    if (action === 'download') {
                        download(reply.Argument || reply.argument);
                    } else {
                        send(reply.Query || reply.query);
                    }
                };
                wrap.appendChild(button);
            })(replies[i]);
        }
        return wrap;
    }

    function buildRating(turnId) {
        var wrap = document.createElement('span');

        function make(value, glyph, title) {
            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'rate-btn';
            button.title = title;
            button.innerHTML = glyph;
            button.onclick = function () {
                request('POST', cfg.urls.feedback, { turnId: turnId, value: value }, function () {
                    var siblings = wrap.getElementsByTagName('button');
                    for (var i = 0; i < siblings.length; i++) siblings[i].className = 'rate-btn';
                    button.className = 'rate-btn chosen';
                });
            };
            return button;
        }

        wrap.appendChild(make('up', '👍', 'This answer helped'));
        wrap.appendChild(make('down', '👎', 'This answer missed'));
        return wrap;
    }

    function showTyping() {
        var row = document.createElement('div');
        row.className = 'typing-row';
        row.id = 'typingRow';
        row.innerHTML = '<div class="msg-avatar" style="background:var(--ink);color:var(--gold);">E</div>' +
                        '<div class="typing-bubble"><span></span><span></span><span></span></div>';
        el.messages.appendChild(row);
        el.messages.scrollTop = el.messages.scrollHeight;
    }

    function hideTyping() {
        var row = $('typingRow');
        if (row && row.parentNode) row.parentNode.removeChild(row);
    }

    function renderDebug(response) {
        if (!state.debug) return;
        el.debugBar.innerHTML =
            'intent <b>' + escapeHtml(response.Intent || '—') + '</b>' +
            ' · confidence <b>' + Math.round((response.Confidence || 0) * 100) + '%</b>' +
            ' · sentiment <b>' + escapeHtml(response.Sentiment || '—') + '</b>' +
            ' · source <b>' + escapeHtml(response.Source || 'rules') + '</b>' +
            ' · ' + (response.ElapsedMs || 0) + ' ms' +
            (response.TicketId ? ' · ticket <b>' + escapeHtml(response.TicketId) + '</b>' : '');
    }

    // ---------------------------------------------------------- actions

    function send(text) {
        var message = (text || '').replace(/^\s+|\s+$/g, '');
        if (!message || state.busy) return;

        state.busy = true;
        addMessage('user', escapeHtml(message));
        el.composerInput.value = '';
        store(STORAGE_DRAFT, '');
        autoGrow();
        showTyping();

        request('POST', cfg.urls.chat, {
            message: message,
            role: state.role,
            studentId: state.studentId,
            language: state.language
        }, function (response) {
            hideTyping();
            state.busy = false;
            setStatus(true);

            addMessage('bot', response.Html || '', {
                replies: response.Replies,
                turnId: response.TurnId,
                plainText: response.PlainText,
                time: response.ServerTime
            });

            renderDebug(response);

            // The answer may have switched child or language server-side.
            if (response.StudentId && response.StudentId !== state.studentId) {
                state.studentId = response.StudentId;
                loadDashboard();
            }
            if (response.Language && response.Language !== state.language) {
                state.language = response.Language;
                loadDashboard();
            }
        }, function (message) {
            hideTyping();
            state.busy = false;
            setStatus(false, message);
            addMessage('bot', escapeHtml(message) + '<br><span class="muted small">Your question was not lost — try sending it again.</span>');
        });
    }

    function download(argument) {
        var url = cfg.urls.download + '?type=';
        if (!argument) return;

        if (argument.indexOf('paper:') === 0) {
            url += 'paper&subject=' + encodeURIComponent(argument.substring(6));
        } else {
            url += encodeURIComponent(argument);
        }

        url += '&studentId=' + encodeURIComponent(state.studentId);
        window.location.href = url;
    }

    function loadDashboard(then) {
        var url = cfg.urls.dashboard +
                  '?role=' + encodeURIComponent(state.role) +
                  '&studentId=' + encodeURIComponent(state.studentId) +
                  '&lang=' + encodeURIComponent(state.language);

        request('GET', url, null, function (data) {
            renderDashboard(data);
            if (then) then(data);
        }, function (message) {
            setStatus(false, message);
        });
    }

    function sessionAction(payload, resetChat) {
        request('POST', cfg.urls.session, payload, function (data) {
            renderDashboard(data);
            if (resetChat) {
                el.messages.innerHTML = '';
                addMessage('bot', data.greeting, { replies: data.chips.map(toReply) });
            }
        }, function (message) {
            setStatus(false, message);
        });
    }

    function toReply(chip) {
        return { Label: chip.label, Query: chip.query };
    }

    // ---------------------------------------------------------- speech

    function speak(text) {
        if (!window.speechSynthesis) return;
        try {
            window.speechSynthesis.cancel();
            var utterance = new window.SpeechSynthesisUtterance(text);
            utterance.lang = state.language === 'hi' ? 'hi-IN' : 'en-IN';
            utterance.rate = 1;
            window.speechSynthesis.speak(utterance);
        } catch (e) { /* speech unavailable */ }
    }

    function setupMicrophone() {
        var Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!Recognition) {
            el.micBtn.disabled = true;
            el.micBtn.title = 'Voice input is not supported in this browser';
            return;
        }

        var recognition = new Recognition();
        recognition.continuous = false;
        recognition.interimResults = false;

        recognition.onresult = function (event) {
            var transcript = event.results[0][0].transcript;
            el.composerInput.value = transcript;
            autoGrow();
            send(transcript);
        };

        recognition.onerror = function () { stopListening(); };
        recognition.onend = function () { stopListening(); };

        function stopListening() {
            state.listening = false;
            el.micBtn.className = 'round-btn mic-btn';
            el.composerInput.placeholder = 'Ask about fees, exams, attendance, homework…';
        }

        el.micBtn.onclick = function () {
            if (state.listening) {
                recognition.stop();
                stopListening();
                return;
            }

            try {
                recognition.lang = state.language === 'hi' ? 'hi-IN' : 'en-IN';
                recognition.start();
                state.listening = true;
                el.micBtn.className = 'round-btn mic-btn listening';
                el.composerInput.placeholder = 'Listening…';
            } catch (e) {
                stopListening();
            }
        };
    }

    // ---------------------------------------------------------- composer

    function autoGrow() {
        el.composerInput.style.height = 'auto';
        el.composerInput.style.height = Math.min(110, el.composerInput.scrollHeight) + 'px';
    }

    function applyTheme(dark) {
        document.body.className = dark ? 'dark' : '';
        el.themeLabel.textContent = dark ? 'Light mode' : 'Dark mode';
        store(STORAGE_THEME, dark ? 'dark' : 'light');
    }

    // ---------------------------------------------------------- wiring

    function bind() {
        el.roleSwitch.onclick = function (event) {
            var target = event.target;
            if (!target || target.tagName !== 'BUTTON') return;

            state.role = target.getAttribute('data-role');
            sessionAction({ action: 'reset', role: state.role, studentId: state.studentId, language: state.language }, true);
        };

        el.studentSelect.onchange = function () {
            state.studentId = el.studentSelect.value;
            sessionAction({ action: 'reset', role: state.role, studentId: state.studentId, language: state.language }, true);
        };

        el.clearBtn.onclick = function () {
            sessionAction({ action: 'reset', role: state.role, studentId: state.studentId, language: state.language }, true);
        };

        el.languageToggle.onclick = function () {
            state.language = state.language === 'hi' ? 'en' : 'hi';
            sessionAction({ action: 'language', role: state.role, studentId: state.studentId, language: state.language }, true);
        };

        el.themeToggle.onclick = function () {
            applyTheme(document.body.className.indexOf('dark') === -1);
        };

        el.speakBtn.onclick = function () {
            state.speaking = !state.speaking;
            el.speakBtn.setAttribute('aria-pressed', state.speaking ? 'true' : 'false');
            if (!state.speaking && window.speechSynthesis) window.speechSynthesis.cancel();
        };

        el.debugBtn.onclick = function () {
            state.debug = !state.debug;
            el.debugBtn.setAttribute('aria-pressed', state.debug ? 'true' : 'false');
            el.debugBar.hidden = !state.debug;
            if (state.debug && !el.debugBar.innerHTML) {
                el.debugBar.textContent = 'Ask something — the recognised intent, confidence and timing appear here.';
            }
        };

        el.notifBtn.onclick = function (event) {
            event.stopPropagation();
            var open = el.notifPanel.className.indexOf('open') === -1;
            el.notifPanel.className = open ? 'notif-panel open' : 'notif-panel';
            el.notifBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
            el.notifDot.style.display = 'none';
        };

        document.addEventListener('click', function (event) {
            if (el.notifPanel.contains(event.target) || event.target === el.notifBtn) return;
            el.notifPanel.className = 'notif-panel';
            el.notifBtn.setAttribute('aria-expanded', 'false');
        });

        el.sendBtn.onclick = function () { send(el.composerInput.value); };

        el.composerInput.addEventListener('keydown', function (event) {
            if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault();
                send(el.composerInput.value);
            }
        });

        el.composerInput.addEventListener('input', function () {
            autoGrow();
            store(STORAGE_DRAFT, el.composerInput.value);
        });

        // "/" focuses the composer, Escape clears it — small touches that add up.
        document.addEventListener('keydown', function (event) {
            if (event.key === '/' && document.activeElement !== el.composerInput) {
                event.preventDefault();
                el.composerInput.focus();
            } else if (event.key === 'Escape' && document.activeElement === el.composerInput) {
                el.composerInput.value = '';
                autoGrow();
            }
        });

        window.addEventListener('online', function () { setStatus(true); });
        window.addEventListener('offline', function () { setStatus(false); });
    }

    function init() {
        el = {
            roleSwitch: $('roleSwitch'),
            studentSelectWrap: $('studentSelectWrap'),
            studentSelect: $('studentSelect'),
            attendanceRing: $('attendanceRing'),
            attendancePct: $('attendancePct'),
            attendanceDays: $('attendanceDays'),
            attendanceCanMiss: $('attendanceCanMiss'),
            feeYear: $('feeYear'),
            feePaid: $('feePaid'),
            feeTotal: $('feeTotal'),
            feeBar: $('feeBar'),
            feeTag: $('feeTag'),
            examDays: $('examDays'),
            examName: $('examName'),
            examDate: $('examDate'),
            noticeTicker: $('noticeTicker'),
            notifPanel: $('notifPanel'),
            notifBtn: $('notifBtn'),
            notifDot: $('notifDot'),
            messages: $('messages'),
            chipsRow: $('chipsRow'),
            composerInput: $('composerInput'),
            sendBtn: $('sendBtn'),
            micBtn: $('micBtn'),
            clearBtn: $('clearBtn'),
            themeToggle: $('themeToggle'),
            themeLabel: $('themeLabel'),
            languageToggle: $('languageToggle'),
            languageLabel: $('languageLabel'),
            speakBtn: $('speakBtn'),
            debugBtn: $('debugBtn'),
            debugBar: $('debugBar'),
            statusText: $('statusText'),
            chatStatus: document.querySelector('.chat-status'),
            todayLabel: $('todayLabel')
        };

        applyTheme(read(STORAGE_THEME) === 'dark');

        var draft = read(STORAGE_DRAFT);
        if (draft) { el.composerInput.value = draft; autoGrow(); }

        bind();
        setupMicrophone();

        loadDashboard(function (data) {
            addMessage('bot', data.greeting, { replies: data.chips.slice(0, 6).map(toReply) });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
