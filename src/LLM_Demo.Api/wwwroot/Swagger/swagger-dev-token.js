(function() {
    var USER_INFO = {
        id: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
        username: 'demo_user',
        email: 'demo@example.com'
    };

    function showUserBar() {
        var bar = document.createElement('div');
        bar.id = 'swagger-dev-user-bar';
        bar.style.cssText = 'display:flex;align-items:center;gap:12px;padding:10px 16px;margin-bottom:12px;background:linear-gradient(135deg,#e8f5e9,#c8e6c9);border:1px solid #66bb6a;border-radius:8px;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,sans-serif;font-size:14px;color:#1b5e20;';

        var avatar = document.createElement('div');
        avatar.style.cssText = 'width:36px;height:36px;border-radius:50%;background:#2e7d32;color:#fff;display:flex;align-items:center;justify-content:center;font-weight:700;font-size:16px;flex-shrink:0;';
        avatar.textContent = USER_INFO.username.charAt(0).toUpperCase();

        var info = document.createElement('div');
        info.style.cssText = 'flex:1;line-height:1.4;';
        info.innerHTML = '<strong>\u2713 Dev-\u0442\u043e\u043a\u0435\u043d \u0430\u0432\u0442\u043e\u0437\u0430\u043f\u043e\u043b\u043d\u0435\u043d</strong><br><span style="font-size:13px;opacity:0.85;">\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c: ' + USER_INFO.username + ' | ' + USER_INFO.email + '</span>';

        var badge = document.createElement('span');
        badge.style.cssText = 'background:#2e7d32;color:#fff;border-radius:4px;padding:3px 10px;font-size:12px;font-weight:600;flex-shrink:0;';
        badge.textContent = 'dev';

        bar.appendChild(avatar);
        bar.appendChild(info);
        bar.appendChild(badge);

        var topbar = document.querySelector('.swagger-ui .topbar');
        if (topbar) {
            topbar.after(bar);
        }
    }

    setTimeout(function() {
        var btn = document.querySelector('.swagger-ui .btn.authorize');
        if (btn) btn.click();
        setTimeout(function() {
            var input = document.querySelector('.auth-container input[type="text"]');
            if (input) {
                input.value = '%%DEV_TOKEN%%';
                input.dispatchEvent(new Event('input', { bubbles: true }));
            }
            var authBtn = document.querySelector('.auth-btn-wrapper .btn.modal-btn.auth');
            if (authBtn) authBtn.click();
            showUserBar();
        }, 300);
    }, 1000);
})();
