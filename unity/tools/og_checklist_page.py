"""Writes the rebuild checklist page (published as an artifact) from unity/Design/og/register.json.
    python3 unity/tools/og_checklist_page.py <out.html> "<build label>"
"""
import json, os, sys, html
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
J = json.load(open(os.path.join(ROOT, 'unity', 'Design', 'og', 'register.json')))
out = sys.argv[1]; build = sys.argv[2] if len(sys.argv) > 2 else ''
data = dict(sections=[dict(name=s['name'], items=[dict(s=i['s'], t=i['t'], n=i['n'], w=i['w'], m=i['m'], said=i['said'], beh=i['beh'], nums=i['nums'], states=i['states'])
        for i in s['items']]) for s in J['sections']], totals=J['totals'], brands=J['brands'])
blob = json.dumps(data, ensure_ascii=False, separators=(',', ':')).replace('</', '<\\/')

PAGE = r'''<title>ASBO Rebuild Checklist</title>
<meta name="claude:viewport-fit-sides" content="cover">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Archivo+Black&family=Archivo:wght@400;600&family=JetBrains+Mono:wght@500&display=swap">
<style>
/* A tagged offender's case file: one long sheet, sections as file tabs, each item a line with a status tag that opens to its evidence */
:root{
  --bg:#f3f1ec; --card:#ffffff; --sunk:#f7f5f0; --ink:#1d1f22; --muted:#6a6e74; --rule:#d9d5cc;
  --done:#1f7a46; --done-bg:#dff1e6; --part:#9a6a00; --part-bg:#fbefcf; --todo:#8a2b2b; --todo-bg:#f6e0dd;
  --accent:#d6a400; --mark:#fff1a8;
  --display:"Archivo Black","Arial Black",Impact,sans-serif; --body:"Archivo","Helvetica Neue",Arial,sans-serif; --mono:"JetBrains Mono",ui-monospace,Menlo,monospace;
}
@media (prefers-color-scheme: dark){:root:not([data-theme="light"]){
  --bg:#151618; --card:#1e2023; --sunk:#191b1d; --ink:#ecebe7; --muted:#a2a39f; --rule:#34363a;
  --done:#6fd39a; --done-bg:#17321f; --part:#f1c55a; --part-bg:#3a2f12; --todo:#f19a8f; --todo-bg:#3c1f1c; --accent:#f1c55a; --mark:#5a4a12; color-scheme:dark}}
:root[data-theme="dark"]{
  --bg:#151618; --card:#1e2023; --sunk:#191b1d; --ink:#ecebe7; --muted:#a2a39f; --rule:#34363a;
  --done:#6fd39a; --done-bg:#17321f; --part:#f1c55a; --part-bg:#3a2f12; --todo:#f19a8f; --todo-bg:#3c1f1c; --accent:#f1c55a; --mark:#5a4a12; color-scheme:dark}
*{box-sizing:border-box}
body{background:var(--bg);color:var(--ink);font-family:var(--body);font-size:15px;line-height:1.5;margin:0}
.wrap{max-width:900px;margin:0 auto;padding-block:28px 60px;padding-left:max(16px,env(safe-area-inset-left,0px));padding-right:max(16px,env(safe-area-inset-right,0px))}
header h1{font-family:var(--display);font-size:clamp(28px,6vw,44px);line-height:1.05;margin:0 0 6px;letter-spacing:-.01em;text-wrap:balance}
header p{color:var(--muted);margin:0 0 14px;max-width:64ch}
.tagband{height:10px;border-radius:5px;background:repeating-linear-gradient(90deg,var(--accent) 0 18px,var(--ink) 18px 36px);opacity:.85;margin:0 0 20px}
.summary{display:flex;flex-wrap:wrap;gap:8px;margin:0 0 8px}
.pill{display:inline-flex;align-items:center;gap:8px;font-family:var(--mono);font-size:12.5px;padding:5px 10px;border-radius:999px;border:1px solid var(--rule);background:var(--card)}
.dot{width:9px;height:9px;border-radius:50%}
.d-done{background:var(--done)} .d-part{background:var(--part)} .d-todo{background:var(--todo)}
.bar{display:flex;height:12px;border-radius:6px;overflow:hidden;border:1px solid var(--rule);margin:8px 0 16px}
.bar span{display:block;height:100%}
.how{background:var(--card);border:1px solid var(--rule);border-left:5px solid var(--accent);border-radius:8px;padding:12px 14px;margin:0 0 16px;max-width:72ch}
.how b{font-weight:600}
.tools{display:flex;flex-wrap:wrap;gap:8px;margin:0 0 18px;align-items:center}
.tools button{font:inherit;font-size:13px;padding:6px 12px;border-radius:8px;border:1px solid var(--rule);background:var(--card);color:var(--ink);cursor:pointer}
.tools button[aria-pressed="true"]{border-color:var(--ink);font-weight:600}
.tools button:focus-visible,.tools input:focus-visible,summary:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
.tools input{font:inherit;font-size:14px;padding:7px 10px;border-radius:8px;border:1px solid var(--rule);background:var(--card);color:var(--ink);flex:1 1 220px;min-width:0}
#hits{font-family:var(--mono);font-size:12px;color:var(--muted)}
section{margin:0 0 22px}
section h2{font-family:var(--display);font-size:18px;margin:0;padding:8px 12px;background:var(--ink);color:var(--bg);border-radius:8px 8px 0 0;display:flex;justify-content:space-between;gap:10px;align-items:baseline}
section h2 small{font-family:var(--mono);font-size:12px;font-weight:500;opacity:.8;white-space:nowrap}
.items{background:var(--card);border:1px solid var(--rule);border-top:none;border-radius:0 0 8px 8px}
.item{display:grid;grid-template-columns:86px 1fr;gap:12px;padding:10px 12px;border-top:1px solid var(--rule);align-items:start}
.item:first-child{border-top:none}
.item>div{min-width:0}
.item b{font-weight:600}
.note{display:block;color:var(--muted);font-size:13px;margin-top:2px}
.asked{margin:6px 0 0;padding:0;list-style:none;font-size:13px}
.asked li{padding-left:14px;position:relative}
.asked li::before{content:"";position:absolute;left:2px;top:.62em;width:6px;height:6px;border-radius:1px;background:var(--accent)}
.tag{font-family:var(--mono);font-size:11px;letter-spacing:.06em;text-transform:uppercase;text-align:center;padding:3px 0;border-radius:5px;margin-top:2px}
.t-done{color:var(--done);background:var(--done-bg)} .t-part{color:var(--part);background:var(--part-bg)} .t-todo{color:var(--todo);background:var(--todo-bg)}
details{margin-top:6px}
summary{cursor:pointer;font-family:var(--mono);font-size:12px;color:var(--muted);list-style:none;display:inline-flex;gap:6px;align-items:center}
summary::-webkit-details-marker{display:none}
summary::before{content:"\25B8";display:inline-block;transition:transform .15s}
details[open] summary::before{transform:rotate(90deg)}
.ev{background:var(--sunk);border:1px solid var(--rule);border-radius:8px;padding:10px 12px;margin-top:8px}
.ev h4{font-family:var(--mono);font-size:11px;letter-spacing:.06em;text-transform:uppercase;color:var(--muted);margin:10px 0 6px;font-weight:500}
.ev h4:first-child{margin-top:0}
.lines{margin:0;padding:0;list-style:none;font-size:13.5px;max-height:340px;overflow:auto}
.lines li{padding:3px 0;border-top:1px dashed var(--rule)}
.lines li:first-child{border-top:none}
.chips{display:flex;flex-wrap:wrap;gap:5px}
.chips span{font-family:var(--mono);font-size:11.5px;padding:2px 7px;border-radius:5px;border:1px solid var(--rule);background:var(--card)}
.src{font-family:var(--mono);font-size:11.5px;color:var(--muted);word-break:break-word}
mark{background:var(--mark);color:inherit;border-radius:2px}
.brands{background:var(--card);border:1px solid var(--rule);border-radius:8px;padding:10px 14px;margin:0 0 22px}
.brands h3{font-family:var(--display);font-size:16px;margin:0 0 4px}
.brands p{margin:0 0 8px;color:var(--muted);font-size:13px}
footer{color:var(--muted);font-size:13px;margin-top:26px;max-width:68ch}
@media (max-width:480px){.item{grid-template-columns:70px 1fr}}
</style>

<div class="wrap">
<header>
  <div class="tagband" aria-hidden="true"></div>
  <h1>ASBO: Life on Tag — rebuild checklist</h1>
  <p>Everything in the original browser game (v95), plus everything you've asked for, tracked as it goes into the Unity version. Open any item to see every line, reaction and behaviour recorded for it from the old game's code.</p>
  <div class="summary" id="summary"></div>
  <div class="bar" id="bar" aria-hidden="true"></div>
  <div class="how"><b>How nothing gets lost.</b> All the detail below was pulled straight out of the old game's code, not written from memory: every line people say, every on-screen message, every behaviour, mood and state, and every timing and number. It's all saved with the Unity project, one file per system. Each system is built from its old code, and an item only gets marked done once everything recorded for it is in.</div>
  <div class="tools">
    <input id="q" type="search" placeholder="Search every line and behaviour, e.g. Cashmere, wee, Brenda" aria-label="Search the register">
    <span id="hits"></span>
  </div>
  <div class="tools" role="group" aria-label="Show">
    <button type="button" aria-pressed="true" data-f="all">Everything</button>
    <button type="button" aria-pressed="false" data-f="todo">Not yet</button>
    <button type="button" aria-pressed="false" data-f="part">Partly</button>
    <button type="button" aria-pressed="false" data-f="done">Done</button>
  </div>
</header>
<main id="list"></main>
<div class="brands" id="brands"></div>
<footer>__BUILD__ Your standing rules apply to everything here: UK council-estate look and rough people, the old game's UK car styles, no real brands, customisable touch controls like the old game, commercially safe assets only, and every change checked for smooth frame rate.</footer>
</div>

<script id="reg" type="application/json">__DATA__</script>
<script>
const R=JSON.parse(document.getElementById("reg").textContent);
const label={done:"Done",part:"Partly",todo:"Not yet"};
let filter="all", q="";
const esc=s=>s.replace(/[&<>"]/g,c=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;"}[c]));
const hl=s=>{const e=esc(s); if(!q) return e; const i=e.toLowerCase().indexOf(esc(q).toLowerCase()); return i<0?e:e.slice(0,i)+"<mark>"+e.slice(i,i+esc(q).length)+"</mark>"+e.slice(i+esc(q).length)};
const has=s=>s.toLowerCase().includes(q.toLowerCase());
function matchItem(it){ if(!q) return true; return has(it.t)||has(it.n)||it.w.some(has)||it.said.some(has)||it.beh.some(has)||it.states.some(has); }
function evidence(it,box){
  const said=q?it.said.filter(has):it.said, beh=q?it.beh.filter(has):it.beh, st=q?it.states.filter(has):it.states;
  let h="";
  if(said.length) h+=`<h4>What it says and shows (${said.length})</h4><ul class="lines">${said.map(s=>`<li>${hl(s)}</li>`).join("")}</ul>`;
  if(beh.length) h+=`<h4>Behaviours (${beh.length})</h4><div class="chips">${beh.map(s=>`<span>${hl(s)}</span>`).join("")}</div>`;
  if(st.length) h+=`<h4>States and modes</h4><ul class="lines">${st.map(s=>`<li>${hl(s)}</li>`).join("")}</ul>`;
  if(it.nums) h+=`<h4>Tuning numbers</h4><div class="src">${it.nums} recorded (timings, distances, chances, prices) in the module files.</div>`;
  h+=`<h4>Saved in the project</h4><div class="src">unity/Design/og/modules/ · ${it.m.map(m=>m.replace(":","-")).join(", ")}</div>`;
  box.innerHTML=h;
}
function render(){
  const list=document.getElementById("list"); list.textContent="";
  const c={done:0,part:0,todo:0}; let shownN=0;
  R.sections.forEach(s=>s.items.forEach(it=>c[it.s]++));
  const total=c.done+c.part+c.todo;
  const sum=document.getElementById("summary"); sum.textContent="";
  const pill=(html)=>{const p=document.createElement("span");p.className="pill";p.innerHTML=html;sum.appendChild(p)};
  [["done","d-done"],["part","d-part"],["todo","d-todo"]].forEach(([k,cls])=>pill(`<span class="dot ${cls}"></span>${label[k]} ${c[k]}`));
  pill(`${total} items`);
  pill(`${R.totals.said.toLocaleString()} lines said or shown`); pill(`${R.totals.beh.toLocaleString()} behaviours`); pill(`${R.totals.nums.toLocaleString()} tuning numbers`);
  const bar=document.getElementById("bar"); bar.textContent="";
  [["done","var(--done)"],["part","var(--part)"],["todo","var(--todo)"]].forEach(([k,col])=>{const s=document.createElement("span");s.style.width=(c[k]/total*100)+"%";s.style.background=col;bar.appendChild(s)});
  R.sections.forEach(sec=>{
    const shown=sec.items.filter(it=>(filter==="all"||it.s===filter)&&matchItem(it)); if(!shown.length) return;
    shownN+=shown.length;
    const el=document.createElement("section");
    const done=sec.items.filter(it=>it.s==="done").length;
    el.innerHTML=`<h2><span>${esc(sec.name)}</span><small>${done}/${sec.items.length} done</small></h2>`;
    const box=document.createElement("div"); box.className="items";
    shown.forEach(it=>{
      const d=document.createElement("div"); d.className="item";
      const n=it.said.length+it.beh.length;
      let h=`<span class="tag t-${it.s}">${label[it.s]}</span><div><b>${hl(it.t)}</b>${it.n?`<span class="note">${hl(it.n)}</span>`:""}`;
      if(it.w.length) h+=`<ul class="asked">${it.w.map(w=>`<li>You asked: ${hl(w)}</li>`).join("")}</ul>`;
      if(it.m.length) h+=`<details><summary>From the old game: ${it.beh.length} behaviours · ${it.said.length} lines${it.nums?` · ${it.nums} numbers`:""}</summary><div class="ev"></div></details>`;
      h+=`</div>`; d.innerHTML=h;
      const det=d.querySelector("details");
      if(det){ det.addEventListener("toggle",()=>{ if(det.open) evidence(it,det.querySelector(".ev")); }); if(q && n) { det.open=true; } }
      box.appendChild(d);
    });
    el.appendChild(box); list.appendChild(el);
  });
  document.getElementById("hits").textContent=q?`${shownN} item${shownN===1?"":"s"} mention “${q}”`:"";
  const b=document.getElementById("brands");
  const keys=Object.keys(R.brands).sort();
  b.innerHTML=`<h3>Real names in the old game's text</h3><p>The rebuild uses made-up names only, so these get replaced as each system goes in. Spotify and YouTube stay as jukebox links, since that's a feature you asked for.</p><div class="chips">${keys.map(k=>`<span title="${esc(R.brands[k].join(" / "))}">${esc(k)}</span>`).join("")}</div>`;
}
document.querySelectorAll("[data-f]").forEach(b=>b.addEventListener("click",()=>{filter=b.dataset.f;document.querySelectorAll("[data-f]").forEach(x=>x.setAttribute("aria-pressed",x===b?"true":"false"));render()}));
let t; document.getElementById("q").addEventListener("input",e=>{clearTimeout(t);t=setTimeout(()=>{q=e.target.value.trim();render()},180)});
render();
</script>
'''
open(out, 'w').write(PAGE.replace('__DATA__', blob).replace('__BUILD__', html.escape(build)))
print(out, os.path.getsize(out))
