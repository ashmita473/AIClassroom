(() => {
    const tabs = document.querySelectorAll('.category-tab');
    const sideTools = document.querySelectorAll('.side-tool[data-category]');
    const title = document.getElementById('drawerTitle');
    const hint = document.getElementById('drawerHint');
    const labels = {
        Saved: 'Your starter and saved looks.', Skin: 'Choose a skin tone.', Hair: 'Choose a hairstyle.', HairColor: 'Choose a hair color.',
        Outfit: 'Choose an outfit.', Hat: 'Choose a hat.', Accessory: 'Choose accessories.', Shoes: 'Choose shoes.', Pet: 'Choose a companion.'
    };
    function go(category) { const url = new URL(window.location.href); url.searchParams.set('category', category); window.location.href = url.toString(); }
    tabs.forEach(t => t.addEventListener('click', () => go(t.dataset.category)));
    sideTools.forEach(t => t.addEventListener('click', () => go(t.dataset.category)));
    document.getElementById('cameraButton')?.addEventListener('click', () => window.print());
    document.getElementById('menuButton')?.addEventListener('click', () => document.querySelector('.editor-drawer')?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }));
    document.querySelectorAll('.color-dot').forEach(dot => dot.addEventListener('click', () => {
        document.documentElement.style.setProperty('--selected-color', dot.dataset.color);
        document.querySelectorAll('.color-dot').forEach(x => x.classList.remove('selected'));
        dot.classList.add('selected');
    }));
    const active = document.querySelector('[data-active-category]')?.dataset.activeCategory || 'Saved';
    if (title) title.textContent = active;
    if (hint) hint.textContent = labels[active] || labels.Saved;
})();
