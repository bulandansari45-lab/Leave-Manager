(() => {
  const root = document.documentElement;
  const stored = localStorage.getItem('dayoff-theme');
  if (stored === 'dark' || stored === 'light') root.dataset.theme = stored;
  const syncThemeButton = () => document.querySelectorAll('[data-theme-toggle]').forEach(button => { const dark = root.dataset.theme === 'dark'; button.setAttribute('aria-pressed', String(dark)); button.textContent = dark ? 'Switch to light mode' : 'Switch to dark mode'; });
  syncThemeButton();
  document.addEventListener('click', event => {
    const button = event.target.closest('[data-theme-toggle]');
    if (!button) return;
    const next = root.dataset.theme === 'dark' ? 'light' : 'dark';
    root.dataset.theme = next;
    localStorage.setItem('dayoff-theme', next);
    button.setAttribute('aria-pressed', String(next === 'dark'));
    syncThemeButton();
  });
})();