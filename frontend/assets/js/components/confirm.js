export function confirmDialog({ title = 'Confirmation', message = '', confirmText = 'Confirmer', danger = false } = {}) {
  return new Promise((resolve) => {
    const dlg = document.createElement('dialog');
    
    
    dlg.style.cssText = 'border:none;border-radius:12px;padding:0;width:min(420px,calc(100vw - 32px));box-shadow:0 20px 40px rgba(0,0,0,.2);margin:auto;';
    
    dlg.innerHTML = `
      <div style="padding:16px 20px;border-bottom:1px solid #E5E1F5;background:linear-gradient(180deg,#faf7ff,#fff);font-weight:700;color:#1f1b2e;">${title}</div>
      <div style="padding:20px;color:#4b5563;font-size:14px;">${message}</div>
      <div style="padding:12px 20px;display:flex;justify-content:flex-end;gap:8px;border-top:1px solid #E5E1F5;">
        <button type="button" class="btn btn-ghost" data-r="0">Annuler</button>
        <button type="button" class="btn ${danger ? 'btn-danger' : 'btn-primary'}" data-r="1">${confirmText}</button>
      </div>`;

    const done = (v) => {
      dlg.close();
      dlg.remove();
      resolve(v);
    };

    dlg.addEventListener('click', (e) => {
      const b = e.target.closest('[data-r]');
      if (b) done(b.dataset.r === '1');
    });

    dlg.addEventListener('cancel', (e) => {
      e.preventDefault();
      done(false);
    });

    document.body.appendChild(dlg);
    dlg.showModal();
  });
}
