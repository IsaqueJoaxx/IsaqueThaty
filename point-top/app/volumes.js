'use strict';
(()=>{
 const el=id=>document.getElementById(id),picked={};let running=false,ready=false,picking=false;
 async function api(url,data){const r=await fetch(url,{method:data===undefined?'GET':'POST',headers:{'X-Point-Top-Token':window.POINT_TOP_TOKEN,'Content-Type':'application/json'},body:data===undefined?undefined:JSON.stringify(data)});const v=await r.json();if(!r.ok)throw Error(v.error||'Falha no serviço local.');return v;}
 const message=t=>el('volumeStatus').textContent=t;
 function buttons(){el('calculateVolume').disabled=running||picking||!ready;el('demoVolume').disabled=running||picking||!ready;document.querySelectorAll('[data-pick],#clearBoundary,#volumeTolerance,#volumeConfirm').forEach(b=>b.disabled=running||picking);}
 for(const b of document.querySelectorAll('[data-pick]'))b.onclick=async()=>{picking=true;buttons();message('Selecione o arquivo na janela do Windows.');try{const s=await api('/api/pick',{kind:b.dataset.pick});if(!s.cancelled){picked[b.dataset.pick]=s.id;el(b.dataset.pick+'Name').textContent=s.path;}message(s.cancelled?'Seleção cancelada.':'Arquivo selecionado.');}catch(e){message(e.message);}finally{picking=false;buttons();}};
 el('clearBoundary').onclick=()=>{delete picked.boundary;el('boundaryName').textContent='Sem limite: interseção válida';};
 const format=n=>n.toLocaleString('pt-BR',{minimumFractionDigits:3,maximumFractionDigits:3});
 async function poll(id){let failures=0;while(running){let j;try{j=await api('/api/jobs/'+id);failures=0;}catch(e){if(++failures<5){message('Conexão interrompida. Tentando acompanhar o cálculo…');await new Promise(r=>setTimeout(r,1500));continue;}throw Error('Não foi possível acompanhar o trabalho. O motor pode continuar em execução; confira a pasta Resultados.');}
 el('volumeProgress').value=j.progress;
 if(j.state==='error')throw Error(j.error||'Falha no cálculo.');
 if(j.state==='done'){
  const r=j.result;el('volumeCut').textContent=format(r.corte_m3);el('volumeFill').textContent=format(r.aterro_m3);el('volumeBalance').textContent=format(r.saldo_aterro_menos_corte_m3);
  el('volumeMeta').textContent=`Área válida: ${format(r.area_valida_m2)} m² | Sem dados: ${format(r.area_sem_dados_m2)} m² | ${r.crs} | ${r.alinhamento} | Tolerância: ${r.tolerancia_m} m.`;
  el('volumeFolder').textContent='Resultados: '+r.superficie_1+' e '+r.superficie_2;
  for(const file of j.files){const a=document.createElement('a');a.className='file secondary';a.textContent='Baixar '+file;a.href='/download/'+id+'/'+file+'?key='+j.downloadKey;a.download=file;el('volumeFiles').append(a);}
  message('Cálculo concluído. Resultados gravados em POINT TOP\\Resultados.');return;
 }
 message('Calculando por blocos… '+Math.round(j.progress)+'%');await new Promise(r=>setTimeout(r,700));
 }}
 async function start(demo){if(running)return;try{
 const t=el('volumeTolerance').value.trim(),tolerance=Number(t.replace(',','.'));if(!demo&&(!t||!Number.isFinite(tolerance)||tolerance<0))throw Error('Informe uma tolerância válida.');
 if(!demo&&!el('volumeConfirm').checked)throw Error('Confirme as unidades e o datum vertical.');
 if(!demo&&(!picked.base||!picked.comparison))throw Error('Selecione as duas superfícies.');
 running=true;buttons();el('volumeFiles').replaceChildren();for(const id of ['volumeCut','volumeFill','volumeBalance'])el(id).textContent='—';el('volumeMeta').textContent=demo?'Teste com GeoTIFF sintéticos: corte esperado 48 m³ e aterro 88 m³.':'Processamento em andamento.';el('volumeFolder').textContent='';el('volumeProgress').value=0;message('Iniciando cálculo…');
 const j=await api('/api/jobs',{...picked,demo,confirmed:demo||el('volumeConfirm').checked,tolerance:demo?0:tolerance});await poll(j.id);
 }catch(e){message('Não foi possível concluir: '+e.message);}finally{running=false;buttons();}}
 el('calculateVolume').onclick=()=>start(false);el('demoVolume').onclick=()=>start(true);
 window.addEventListener('beforeunload',e=>{if(running){e.preventDefault();e.returnValue='';}});
 if(!window.POINT_TOP_TOKEN){el('engineState').textContent='Reabra pelo iniciador';message('Feche esta versão em arquivo e abra ABRIR_POINT_TOP.cmd para usar volumes integrados.');buttons();return;}
 api('/api/health').then(r=>{ready=r.ready;el('engineState').textContent=ready?'Motor pronto · Rasterio '+r.rasterio:'Motor indisponível';message(ready?'Selecione as superfícies ou teste o exemplo sintético.':r.error);buttons();}).catch(e=>{el('engineState').textContent='Serviço indisponível';message(e.message);});
})();
