(() => {
 const search=document.getElementById('teacherNoteSearch'); const filters=[...document.querySelectorAll('.teacher-admin-filter')]; const cards=[...document.querySelectorAll('.admin-teacher-note')]; let category='All';
 function apply(){const q=(search?.value||'').toLowerCase().trim();cards.forEach(c=>{const okCat=category==='All'||c.dataset.category===category;const okText=!q||(c.dataset.search||'').toLowerCase().includes(q);c.style.display=okCat&&okText?'':'none';});}
 filters.forEach(b=>b.addEventListener('click',()=>{filters.forEach(x=>x.classList.remove('active'));b.classList.add('active');category=b.dataset.category;apply();})); search?.addEventListener('input',apply);
})();
