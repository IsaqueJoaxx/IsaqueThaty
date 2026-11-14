'use strict';
const $=s=>document.querySelector(s);let state={version:1,name:'Novo projeto',axes:[]};
const say=t=>$('#status').textContent=t;
function download(name,text,type='text/plain'){const u=URL.createObjectURL(new Blob([text],{type}));const a=document.createElement('a');a.href=u;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(u),1000);}
function attempt(fn){try{fn();}catch(e){say('Não foi possível concluir: '+e.message);}}
function render(){
 $('#projectTitle').textContent=state.name;$('#count').textContent=state.axes.length;$('#length').textContent=state.axes.reduce((n,a)=>n+a.length,0).toLocaleString('pt-BR',{maximumFractionDigits:3});$('#gaps').textContent=state.axes.reduce((n,a)=>n+a.gaps,0);
 $('#axis').replaceChildren();$('#rows').replaceChildren();
 state.axes.forEach((a,i)=>{const o=document.createElement('option');o.value=i;o.textContent=a.name;$('#axis').append(o);const tr=document.createElement('tr');[a.name,a.length.toFixed(3),a.segments.filter(s=>s.type==='Line').length,a.curves,a.spirals].forEach(v=>{const td=document.createElement('td');td.textContent=v;tr.append(td);});$('#rows').append(tr);});draw();
}
function draw(){const c=$('#map'),ctx=c.getContext('2d');ctx.clearRect(0,0,c.width,c.height);ctx.strokeStyle='#1c3042';ctx.lineWidth=1;for(let x=0;x<c.width;x+=40){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,c.height);ctx.stroke();}for(let y=0;y<c.height;y+=40){ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(c.width,y);ctx.stroke();}
 const a=state.axes[Number($('#axis').value)];if(!a){$('#details').textContent='Nenhum eixo selecionado.';return;}
 const pts=a.segments.flatMap(s=>[s.start,s.end]);let minx=Infinity,miny=Infinity,maxx=-Infinity,maxy=-Infinity;for(const p of pts){minx=Math.min(minx,p[0]);maxx=Math.max(maxx,p[0]);miny=Math.min(miny,p[1]);maxy=Math.max(maxy,p[1]);}const scale=Math.min((c.width-80)/Math.max(maxx-minx,1),(c.height-80)/Math.max(maxy-miny,1));const xy=p=>[40+(p[0]-minx)*scale,c.height-40-(p[1]-miny)*scale];
 for(const s of a.segments){ctx.setLineDash(s.type==='Line'?[]:[9,6]);ctx.strokeStyle=s.type==='Line'?'#53dfc3':'#ffcb7b';ctx.lineWidth=4;ctx.beginPath();ctx.moveTo(...xy(s.start));ctx.lineTo(...xy(s.end));ctx.stroke();}ctx.setLineDash([]);
 $('#details').textContent=`${a.name}\nExtensão declarada: ${a.length.toFixed(3)} m. Curvas: ${a.curves}. Espirais: ${a.spirals}. Descontinuidades: ${a.gaps}.`;
}
function parseXML(text){
 const doc=new DOMParser().parseFromString(text,'application/xml');if(doc.querySelector('parsererror'))throw Error('XML malformado.');
 const all=Array.from(doc.getElementsByTagName('*'));const metric=all.find(e=>e.localName==='Metric');if(!metric||metric.getAttribute('linearUnit')!=='meter')throw Error('Use LandXML com Units/Metric linearUnit="meter".');
 const child=(e,n)=>Array.from(e.children).find(v=>v.localName===n);
 const point=e=>{if(!e)throw Error('Start/End ausente.');const p=e.textContent.trim().split(/\s+/).map(Number);if(p.length<2||!p.every(Number.isFinite))throw Error('Ponto inválido.');return [p[1],p[0]];};
 const axes=all.filter(e=>e.localName==='Alignment').map(e=>{const g=child(e,'CoordGeom');if(!g)throw Error('CoordGeom ausente.');return {name:e.getAttribute('name')||'Sem nome',segments:Array.from(g.children).map(s=>({type:s.localName,length:Number(s.getAttribute('length')),start:point(child(s,'Start')),end:point(child(s,'End'))}))};});
 if(!axes.length)throw Error('Nenhum Alignment encontrado.');return PointTop.analyze(axes);
}
async function readFile(input,fn){const f=input.files[0];if(!f)return;if(f.size>20*1024*1024){say('Limite deste importador: 20 MB. LandXML maior exige o leitor incremental planejado.');input.value='';return;}try{fn(await f.text());}catch(e){say('Importação recusada: '+e.message);}input.value='';}
$('#xml').onchange=e=>readFile(e.target,text=>{const axes=parseXML(text);state.axes=axes;render();say('LandXML importado. Curvas/espirais em planta são apenas cordas; comprimentos vêm do XML.');});
$('#load').onchange=e=>readFile(e.target,text=>{const p=JSON.parse(text);if(p.version!==1||typeof p.name!=='string'||!Array.isArray(p.axes))throw Error('Projeto incompatível.');const axes=PointTop.analyze(p.axes);state={version:1,name:p.name.slice(0,160),axes};render();say('Projeto aberto.');});
$('#rename').onclick=()=>{state.name=$('#projectName').value.trim().slice(0,160)||'Novo projeto';render();};
$('#save').onclick=()=>download('point-top.projeto.json',JSON.stringify(state,null,2),'application/json');
$('#demo').onclick=()=>{state={version:1,name:'Demonstração · dados sintéticos',axes:PointTop.analyze([{name:'Eixo exemplo',segments:[{type:'Line',length:100,start:[500000,7800000],end:[500100,7800000]},{type:'Line',length:100,start:[500100,7800000],end:[500160,7800080]}]}])};render();say('Exemplo sintético: duas tangentes, 200 m; não representa uma obra real.');};
$('#axis').onchange=draw;
$('#csv').onclick=()=>attempt(()=>{if(!state.axes.length)throw Error('Importe um eixo.');const quote=v=>'"'+String(v).replaceAll('"','""')+'"';download('eixos.csv','\ufeffEixo;Extensao_m;Curvas;Espirais;Descontinuidades\n'+state.axes.map(a=>[a.name,a.length,a.curves,a.spirals,a.gaps].map(v=>quote(/^[=+@\-]/.test(String(v))?'\''+v:v)).join(';')).join('\n'),'text/csv');});
$('#dxf').onclick=()=>attempt(()=>{const a=state.axes[Number($('#axis').value)];if(!a)throw Error('Selecione um eixo.');download('eixo-tangentes.dxf',PointTop.dxf(a),'application/dxf');say('DXF 2D exportado em metros. Sem CRS embutido; informe-o no CAD.');});
for(const b of document.querySelectorAll('nav button'))b.onclick=()=>{document.querySelectorAll('main section').forEach(s=>s.hidden=s.id!==b.dataset.view);document.querySelectorAll('nav button').forEach(x=>x.classList.toggle('active',x===b));};render();
