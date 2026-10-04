(() => {
  document.querySelectorAll('form').forEach(form => form.addEventListener('submit', () => {
    const button = form.querySelector('button[type="submit"],button:not([type])');
    if (button && !button.dataset.keep) { button.dataset.keep = '1'; setTimeout(() => { button.disabled = true; button.style.opacity = '.65'; }, 0); }
  }));
})();
