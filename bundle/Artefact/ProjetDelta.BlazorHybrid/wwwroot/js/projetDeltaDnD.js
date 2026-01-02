window.projetDeltaDnD = window.projetDeltaDnD || {};

window.projetDeltaDnD.getIndexFromPoint = (x, y) => {
  const el = document.elementFromPoint(x, y);
  if (!el) return -1;

  const li = el.closest("li[data-index]");
  if (!li) return -1;

  const raw = li.getAttribute("data-index");
  const idx = parseInt(raw, 10);
  return Number.isFinite(idx) ? idx : -1;
};
