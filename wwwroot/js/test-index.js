(() => {
    const cards = [...document.querySelectorAll('.test-card')];
    const filters = [...document.querySelectorAll('[data-filter]')];
    const empty = document.getElementById('testNoResults');
    if (!cards.length || !filters.length) return;

    const apply = (value) => {
        let visible = 0;
        cards.forEach(card => {
            const match = value === 'All' || card.dataset.testType?.toLowerCase() === value.toLowerCase();
            card.hidden = !match;
            if (match) visible++;
        });
        if (empty) empty.hidden = visible !== 0;
        filters.forEach(button => button.classList.toggle('active', button.dataset.filter === value));
    };

    filters.forEach(button => button.addEventListener('click', () => {
        const value = button.dataset.filter || 'All';
        apply(value);
        document.getElementById('available-tests-title')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }));
})();
