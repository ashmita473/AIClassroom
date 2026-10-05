(()=>{
const cards=[...document.querySelectorAll('[data-video-card]')]; if(!cards.length)return;
const token=document.querySelector('input[name="__RequestVerificationToken"]')?.value;
const players=new Map();
function setStatus(card,text,done){const el=card.querySelector('[data-video-status]');if(el)el.textContent=text;if(done)card.classList.add('is-complete');}
async function markWatched(card){if(card.dataset.completed==='1')return;card.dataset.completed='1';const videoId=card.dataset.videoId;const body=new URLSearchParams();body.set('videoId',videoId);body.set('__RequestVerificationToken',token||'');try{const r=await fetch('/Student/CompleteLessonVideo',{method:'POST',headers:{'Content-Type':'application/x-www-form-urlencoded; charset=UTF-8'},body});const data=await r.json();if(data.ok){setStatus(card,data.already?'✓ Watched':'✓ Video complete · +'+data.xp+' XP earned',true);}}catch{card.dataset.completed='0';}}
function initPlayers(){if(!window.YT||!YT.Player)return;cards.forEach((card,index)=>{const iframe=card.querySelector('iframe');if(!iframe)return;const id='lesson-youtube-'+index;iframe.id=id;players.set(card,new YT.Player(id,{events:{onStateChange:e=>{if(e.data===YT.PlayerState.ENDED)markWatched(card);}}}));});}
window.onYouTubeIframeAPIReady=initPlayers;
const tag=document.createElement('script');tag.src='https://www.youtube.com/iframe_api';tag.async=true;document.head.appendChild(tag);
})();
