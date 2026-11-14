'use strict';
const assert=require('node:assert/strict'),net=require('node:net');
// Porta livre: o teste não pode depender de o painel do usuário estar fechado.
const freePort=()=>new Promise((ok,no)=>{const s=net.createServer();s.once('error',no);s.listen(0,'127.0.0.1',()=>{const {port}=s.address();s.close(()=>ok(port));});});
(async()=>{process.env.POINT_TOP_PORT=String(await freePort());const {createApp,PORT,ORIGIN}=require('../server.cjs');const {server,token}=createApp();await new Promise(r=>server.listen(PORT,'127.0.0.1',r));let assertions=0;
const check=(x)=>{assert.ok(x);assertions++;};
async function api(url,data,headers={}){return fetch(ORIGIN+url,{method:data===undefined?'GET':'POST',headers:{'Content-Type':'application/json','X-Point-Top-Token':token,...headers},body:data===undefined?undefined:JSON.stringify(data)});}
try{
 check((await (await fetch(ORIGIN)).text()).includes('window.POINT_TOP_TOKEN='));
 check((await fetch(ORIGIN+'/api/health')).status===403);
 check((await api('/api/health',undefined,{Origin:'http://example.org'})).status===403);
 const h=await (await api('/api/health')).json();assert.equal(h.ready,true,h.error);assertions++;
 check((await api('/api/jobs',{demo:true,confirmed:false,tolerance:0})).status===400);
 check((await api('/api/jobs',{demo:true,confirmed:true,tolerance:-1})).status===400);
 check((await api('/api/jobs',{confirmed:true,tolerance:0,base:'fake',comparison:'fake'})).status===400);
 const start=await api('/api/jobs',{demo:true,confirmed:true,tolerance:0});check(start.status===202);const j=await start.json();
 check((await api('/api/jobs',{demo:true,confirmed:true,tolerance:0})).status===409);
 let done;const deadline=Date.now()+30000;while(Date.now()<deadline){done=await (await api('/api/jobs/'+j.id)).json();if(done.state!=='running')break;await new Promise(r=>setTimeout(r,100));}
 assert.equal(done.state,'done',done.error);assertions++;
 assert.equal(done.result.corte_m3,48);assert.equal(done.result.aterro_m3,88);assert.equal(done.result.saldo_aterro_menos_corte_m3,40);assert.equal(done.result.area_valida_m2,60);assert.equal(done.result.area_sem_dados_m2,4);assertions+=5;
 check((await fetch(ORIGIN+'/download/'+j.id+'/resultado.json?key=wrong')).status===404);
 for(const file of done.files){const r=await fetch(ORIGIN+'/download/'+j.id+'/'+file+'?key='+done.downloadKey);check(r.status===200);check((await r.arrayBuffer()).byteLength>0);}
 check((await api('/api/pick',{kind:'outro'})).status===400);
 check((await api('/api/greide/jobs',{})).status===400);
 const espera=async id=>{let v;const limite=Date.now()+120000;while(Date.now()<limite){v=await (await api('/api/jobs/'+id)).json();if(v.state!=='running')break;await new Promise(r=>setTimeout(r,200));}return v;};
 const recusado=await (await api('/api/greide/jobs',{demo:true,params:{projeto:{passo:0}}})).json();const fim=await espera(recusado.id);check(fim.state==='error'&&/passo/.test(fim.error));
 const gj=await api('/api/greide/jobs',{demo:true});check(gj.status===202);const g=await espera((await gj.json()).id);
 assert.equal(g.state,'done',g.error);assertions++;
 check(g.result.sintetico===true&&g.result.situacao!=='falha'&&g.result.resumo.esp_min>=0.23&&g.result.iri.status==='calculado');
 check(g.files.includes('Notas_de_servico.xlsx')&&g.files.includes('iri.csv'));
 check((await fetch(ORIGIN+'/download/'+g.id+'/server.cjs?key='+g.downloadKey)).status===404);
 for(const file of g.files){const r=await fetch(ORIGIN+'/download/'+g.id+'/'+encodeURIComponent(file)+'?key='+g.downloadKey);check(r.status===200);check((await r.arrayBuffer()).byteLength>0);}
 console.log(assertions+' verificações da integração aprovadas. Corte 48 m³; aterro 88 m³; saldo 40 m³; área válida 60 m².');
}finally{await new Promise(r=>server.close(r));}})().catch(e=>{console.error(e);process.exitCode=1;});
