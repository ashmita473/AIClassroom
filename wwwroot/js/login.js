(() => {
    const pin = document.getElementById('Pin');
    const toggle = document.getElementById('togglePin');
    if (!pin || !toggle) return;
    toggle.addEventListener('click', () => {
        const visible = pin.type === 'text';
        pin.type = visible ? 'password' : 'text';
        toggle.classList.toggle('visible', !visible);
        toggle.setAttribute('aria-label', visible ? 'Show PIN' : 'Hide PIN');
    });
})();
