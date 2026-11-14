(function(root){
'use strict';
function analyze(items){
 return items.map(a=>{
 const segments=a.segments.map(s=>{
  if(!['Line','Curve','Spiral'].includes(s.type)) throw Error('Geometria não suportada: '+s.type);
  if(!Number.isFinite(s.length)||s.length<=0) throw Error('Comprimento inválido em '+a.name);
  for(const p of [s.start,s.end]) if(!p||p.length!==2||!p.every(Number.isFinite)) throw Error('Coordenadas inválidas');
  return {...s};
 });
 if(!segments.length) throw Error('Eixo sem geometria: '+a.name);
 const length=segments.reduce((n,s)=>n+s.length,0);
 const gaps=segments.slice(1).filter((s,i)=>Math.hypot(s.start[0]-segments[i].end[0],s.start[1]-segments[i].end[1])>0.01).length;
 return {...a,segments,length,gaps,curves:segments.filter(s=>s.type==='Curve').length,spirals:segments.filter(s=>s.type==='Spiral').length};
 });
}
function dxf(a){
 if(a.segments.some(s=>s.type!=='Line')) throw Error('DXF disponível apenas para eixos integralmente em tangentes. Curvas e espirais exigem implementação exata.');
 let t='0\nSECTION\n2\nENTITIES\n';
 for(const s of a.segments)t+=`0\nLINE\n8\nPOINT_TOP\n10\n${s.start[0]}\n20\n${s.start[1]}\n30\n0\n11\n${s.end[0]}\n21\n${s.end[1]}\n31\n0\n`;
 return t+'0\nENDSEC\n0\nEOF\n';
}
const api={analyze,dxf}; if(typeof module!=='undefined')module.exports=api;else root.PointTop=api;
})(globalThis);
