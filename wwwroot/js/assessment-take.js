(() => {
    const form = document.getElementById('assessmentForm');
    if (!form) return;
    const limit = Number(form.dataset.limitSeconds || 0);
    const started = Number(document.getElementById('startedAt')?.value || 0);
    const timer = document.getElementById('timer');
    const progress = document.getElementById('progress');
    function sync() {
        document.querySelectorAll('[data-match-question]').forEach(x => {
            const id = x.dataset.matchQuestion, h = document.getElementById('match_' + id), obj = {};
            document.querySelectorAll(`[data-match-question="${id}"]`).forEach(s => obj[s.dataset.matchLeft] = s.value);
            if (h) h.value = JSON.stringify(obj);
        });
        document.querySelectorAll('[data-order-question]').forEach(x => {
            const id = x.dataset.orderQuestion, h = document.getElementById('order_' + id), arr = [];
            document.querySelectorAll(`[data-order-question="${id}"]`).forEach(s => { if (s.value) arr[Number(s.value) - 1] = s.dataset.orderText; });
            if (h) h.value = JSON.stringify(arr.filter(Boolean));
        });
    }
    function tick() {
        const elapsed = Math.max(0, Math.floor((Date.now() - started) / 1000));
        const remain = Math.max(0, limit - elapsed);
        if (timer) timer.textContent = String(Math.floor(remain / 60)).padStart(2, '0') + ':' + String(remain % 60).padStart(2, '0');
        if (progress && limit > 0) progress.style.width = Math.min(100, elapsed * 100 / limit) + '%';
        if (remain <= 0) { sync(); form.submit(); return; }
        setTimeout(tick, 1000);
    }
    form.addEventListener('submit', sync);
    tick();
})();
