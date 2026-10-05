(() => {
    const root = document.querySelector('[data-live-room]');
    if (!root || typeof signalR === 'undefined') return;
    const room = root.dataset.room;
    const status = document.getElementById('liveStatus');
    const connection = document.getElementById('connection');
    const hub = new signalR.HubConnectionBuilder().withUrl('/classroomHub').withAutomaticReconnect().build();
    hub.on('TeacherCommand', cmd => { if (status) status.textContent = 'Teacher command: ' + cmd.toUpperCase(); });
    hub.on('TeacherAnnouncement', msg => { const h = document.createElement('h2'); h.textContent = '📢 ' + msg; const area = document.getElementById('activityArea'); if (area) area.replaceChildren(h); });
    async function start() {
        try { await hub.start(); await hub.invoke('JoinClass', room); if (connection) connection.textContent = 'Connected to classroom.'; }
        catch { if (connection) connection.textContent = 'Connection failed. Check the school network.'; }
    }
    document.querySelectorAll('[data-command]').forEach(b => b.addEventListener('click', () => hub.invoke('TeacherCommand', room, b.dataset.command)));
    start();
})();
