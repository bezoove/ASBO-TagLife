const fs=require('fs');
let src=fs.readFileSync('og_cars.js','utf8').replace('World.CAR_MODELS=','module.exports=');
fs.writeFileSync('og_cars_mod.js',src);
const M=require('./og_cars_mod.js');
const flat=a=>a?a.flat():[];
const out=[];
for(const [id,d0] of Object.entries(M)){
  let d=d0; if(d.base){const b=M[d.base]; d=Object.assign({},b,d0,{gh:Object.assign({},b.gh,d0.gh)});}
  const g=d.gh||{};
  const hx=v=>typeof v==='string'?v:'';
  const o={id,name:d.name,L:d.L,W:d.W,wb:d.wb,fo:d.fo,rad:d.rad,tw:d.tw,arch:d.arch,sill:d.sill,endY:d.endY,rcF:d.rcF,rcR:d.rcR,floor:d.floor,
    top:flat(d.top),crown:flat(d.crown),rakeF:d.rakeF||[0,0],rakeR:d.rakeR||[0,0],crease:d.crease||0,
    gh:{rb:g.rb,rrx:g.rrx,fr:g.fr,fb:g.fb,roof:g.roof,crown:g.crown||0,rR:g.rR||0,rF:g.rF||0,curR:g.curR||0,curF:g.curF||0,beltIn:g.beltIn||0,tumble:g.tumble||0,pA:g.pA||0,pTop:g.pTop||0,pC:g.pC||0,pBelt:g.pBelt||0,xB:g.xB||[0,0],bw:g.bw||0,xQ:g.xQ||[],qw:g.qw||0,van:!!g.van},
    doors:d.doors,van:!!d.van,boot:!!d.boot,seatF:d.seatF||0,seatR:(d.seatR==null?-99:d.seatR),
    seamFdF:flat(d.seams&&d.seams.fdF),seamFdR:flat(d.seams&&d.seams.fdR),seamRdR:flat(d.seams&&d.seams.rdR),
    handles:d.handles||[],handleBody:d.handleCol==='body',
    bumpF:Object.assign({stripe:'',intake:[],plateY:0,step:false},d.bumpF,{stripe:(d.bumpF&&d.bumpF.stripe)||'',intake:(d.bumpF&&d.bumpF.intake)||[]}),
    bumpR:Object.assign({stripe:'',intake:[],plateY:0,step:false},d.bumpR,{stripe:(d.bumpR&&d.bumpR.stripe)||'',intake:[],step:!!(d.bumpR&&d.bumpR.step)}),
    plateRy:(d.plateR&&d.plateR.y)||0.6,plateROnBumper:!!(d.plateR&&d.plateR.onBumper),
    head:Object.assign({oval:0,wrap:0,twin:0},d.head),
    grille:Object.assign({bars:0,chrome:0,oval:0,badge:''},d.grille,{badge:''}),
    indBumper:d.ind==='bumper',
    tail:Object.assign({band:0,tall:0},d.tail),
    clad:d.clad?{y0:d.clad.y0,y1:d.clad.y1,hex:d.clad.hex,on:true}:{y0:0,y1:0,hex:'',on:false},
    moulding:d.moulding?{y:d.moulding.y,h:d.moulding.h,hex:d.moulding.hex,stripe:d.moulding.stripe||'',on:true}:{y:0,h:0,hex:'',stripe:'',on:false},
    mirrorBody:d.mirror==='body',aerial:d.aerial||'',wheel:d.wheel||'steel',fuel:d.fuel||0};
  for(const b of [o.bumpF,o.bumpR]){ if(b.col===undefined) b.col='body'; b.out=b.out||0.05; }
  out.push(o);
}
fs.writeFileSync('CarModels.json',JSON.stringify({models:out},null,1));
console.log(out.map(o=>o.id+' '+o.L).join('\n'));
