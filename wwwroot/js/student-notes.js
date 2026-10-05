(() => {
  const search = document.getElementById('noteSearch');
  const myCards = [...document.querySelectorAll('#myNotesGrid .my-note-card')];
  const empty = document.getElementById('myNotesEmpty');
  function filterMine() {
    const q = (search?.value || '').trim().toLowerCase();
    let visible = 0;
    myCards.forEach(card => {
      const show = !q || (card.dataset.search || '').toLowerCase().includes(q);
      card.style.display = show ? '' : 'none';
      if (show) visible++;
    });
    if (empty) empty.style.display = visible ? 'none' : '';
  }
  search?.addEventListener('input', filterMine);

  const filters = [...document.querySelectorAll('.teacher-filter')];
  const teacherCards = [...document.querySelectorAll('#teacherNotesGrid .teacher-note-card')];
  filters.forEach(button => button.addEventListener('click', () => {
    filters.forEach(x => x.classList.remove('active')); button.classList.add('active');
    const category = button.dataset.category;
    teacherCards.forEach(card => { card.style.display = category === 'All' || card.dataset.category === category ? '' : 'none'; });
  }));
})();
