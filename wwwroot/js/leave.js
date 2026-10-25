(() => {
  const searchForm = document.getElementById('requestQueryForm');
  const search = document.getElementById('requestSearch');
  const sort = document.getElementById('dateSort');
  let searchTimer;
  search?.addEventListener('input', () => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(() => searchForm?.requestSubmit(), 350);
  });
  sort?.addEventListener('change', () => searchForm?.requestSubmit());
  const start = document.getElementById('StartDate');
  const end = document.getElementById('EndDate');
  start?.addEventListener('change', () => { end.min = start.value; if (end.value && end.value < start.value) end.value = start.value; });
  const undo = document.querySelector('.undo-banner');
  if (undo) {
    const expiry = Number(undo.dataset.expires);
    const label = undo.querySelector('.undo-countdown');
    const timer = setInterval(() => {
      const left = Math.max(0, Math.ceil((expiry - Date.now()) / 1000));
      if (label) label.textContent = String(left);
      if (left === 0) { undo.remove(); clearInterval(timer); }
    }, 200);
  }
})();
