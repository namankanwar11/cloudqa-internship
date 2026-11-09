// Only generic element types are enumerated. No application identifiers are selectors.
const kind = arguments[0];
const text = e => (e?.innerText || e?.textContent || '').trim();
const refs = (e, attr) => (e.getAttribute(attr)||'').split(/\s+/)
  .filter(Boolean).map(id => text(document.getElementById(id))).join(' ');
return Array.from((arguments[1] || document).querySelectorAll('input,select')).filter(e =>
 kind === 'text' ? e.tagName === 'INPUT' && ['text',''].includes(e.getAttribute('type')||'') :
 kind === 'radio' ? e.type === 'radio' : e.tagName === 'SELECT'
).map(e => {
 const labelText = l => { const c=l.cloneNode(true); c.querySelectorAll('input,select,textarea').forEach(n=>n.remove()); return text(c); }; const standard = Array.from(e.labels||[]); const related = e.id ? Array.from(document.querySelectorAll('[for]')).filter(n=>n.getAttribute('for')===e.id && n.tagName!=='LABEL' && n.closest('form')===e.closest('form')) : []; const labels = [...standard,...related].map(labelText).join(' ');
 const accessible = [e.getAttribute('aria-label'),refs(e,'aria-labelledby')].filter(Boolean).join(' ');
 let nearby = '';
 // A nearby-text fallback is allowed only within a small single-control container.
 for(let p=e.parentElement, depth=0;p && depth<4;p=p.parentElement,depth++){
   if(p.querySelectorAll('input,select,textarea').length===1 && text(p).length<160){
     nearby=text(p); if(nearby) break;
   }
 }
 const group=e.closest('fieldset,[role="radiogroup"]');
 const context=group ? [text(group.querySelector('legend')),group.getAttribute('aria-label'),refs(group,'aria-labelledby')].filter(Boolean).join(' ') : '';
 return {element:e, labels, accessible, nearby, context, placeholder:e.getAttribute('placeholder')||''};
});