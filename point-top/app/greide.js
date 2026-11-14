'use strict';
(()=>{
 const el=id=>document.getElementById(id),picked={};let running=false,picking=false;
 const ROTULO={ok:'OK',atencao:'Atenção',falha:'Falha',info:'Info'};
 const COR={ok:'#7ce5ce',atencao:'#f2c14e',falha:'#ff7b72',info:'#91a8bb'};
 const fmt=(n,d=2)=>n===null||n===undefined?'—':Number(n).toLocaleString('pt-BR',{minimumFractionDigits:d,maximumFractionDigits:d});
 const msg=t=>{el('greideStatus').textContent=t;};
 async function api(url,data){const r=await fetch(url,{method:data===undefined?'GET':'POST',headers:{'X-Point-Top-Token':window.POINT_TOP_TOKEN,'Content-Type':'application/json'},body:data===undefined?undefined:JSON.stringify(data)});const v=await r.json();if(!r.ok)throw Error(v.error||'Falha no serviço local.');return v;}
 function travar(){const off=running||picking;for(const b of document.querySelectorAll('#greide button'))b.disabled=off;el('greideCalcular').disabled=off||!picked.eixo||!picked.levantamento;}
 for(const b of document.querySelectorAll('[data-greide-pick]'))b.onclick=async()=>{const kind=b.dataset.greidePick;picking=true;travar();msg('Selecione o arquivo na janela do Windows.');try{const s=await api('/api/pick',{kind});if(!s.cancelled){picked[kind]=s.id;el('greideNome_'+kind).textContent=s.path;}msg(s.cancelled?'Seleção cancelada.':'Arquivo selecionado.');}catch(e){msg(e.message);}finally{picking=false;travar();}};
 el('greideTirarPerfil').onclick=()=>{delete picked.perfilIri;el('greideNome_perfilIri').textContent='Sem perfil: IRI não será calculado';};
 const num=id=>{const v=el(id).value;return v===''?null:Number(v);};
 function parametros(){return {
  projeto:{passo:num('gPasso'),sta_ini:num('gIni'),sta_fim:num('gFim'),larg_pista:num('gPista'),larg_acost:num('gAcost'),esp_min:num('gEsp'),inc_conf:num('gIncConf'),inc_final:num('gIncFinal'),d2_mm_5m:num('gD2'),modo:el('gModo').value,fres_max:num('gFres'),super_ativa:el('gSuper').checked,emax:num('gEmax'),velocidade:num('gVel')},
  limites:{rampa_max:num('gRampaMax'),rampa_min:num('gRampaMin'),k_convexa:num('gKConv'),k_concava:num('gKConc')},
  iri:{segmento:num('gSeg')??100,unidade:el('gUnidade').value,limite:num('gLimIri')}};}
 function linhaTabela(corpo,celulas,cor){const tr=document.createElement('tr');celulas.forEach((c,i)=>{const td=document.createElement('td');td.textContent=c===null||c===undefined?'':c;if(cor&&i===cor.coluna){td.style.color=cor.valor;td.style.fontWeight='600';}tr.append(td);});corpo.append(tr);}
 function desenhar(s){const c=el('greideGrafico'),g=c.getContext('2d'),W=c.width,H=c.height;g.clearRect(0,0,W,H);if(!s||s.estaca.length<2)return;
  const E=70,D=20,x0=s.estaca[0],x1=s.estaca[s.estaca.length-1],X=v=>E+(v-x0)/(x1-x0||1)*(W-E-D);
  const faixa=(listas,topo,base)=>{let a=Infinity,b=-Infinity;for(const l of listas)for(const v of l)if(v!==null){a=Math.min(a,v);b=Math.max(b,v);}const m=Math.max((b-a)*0.08,0.01);a-=m;b+=m;return{Y:v=>base-(v-a)/(b-a)*(base-topo),a,b};};
  const eixoY=(f,topo,base,titulo)=>{g.fillStyle='#91a8bb';g.font='20px Segoe UI,Arial';g.fillText(titulo,E,topo-8);g.font='18px Segoe UI,Arial';for(let i=0;i<=4;i++){const v=f.a+(f.b-f.a)*i/4,y=f.Y(v);g.strokeStyle='#22364a';g.lineWidth=1;g.beginPath();g.moveTo(E,y);g.lineTo(W-D,y);g.stroke();g.fillStyle='#91a8bb';g.fillText(fmt(v,f.b-f.a<2?3:1),4,y+6);}};
  const linha=(xs,ys,Y,cor,lw)=>{g.strokeStyle=cor;g.lineWidth=lw;g.beginPath();let on=false;ys.forEach((v,i)=>{if(v===null){on=false;return;}const a=X(xs[i]),b=Y(v);on?g.lineTo(a,b):g.moveTo(a,b);on=true;});g.stroke();};
  const t1=38,b1=H*0.62,t2=b1+56,b2=H-40;
  const f1=faixa([s.terreno,s.conformacao,s.final],t1,b1);eixoY(f1,t1,b1,'Cota no eixo (m)');
  linha(s.estaca,s.terreno,f1.Y,'#8a99a8',2);linha(s.estaca,s.conformacao,f1.Y,'#53dfc3',2);linha(s.estaca,s.final,f1.Y,'#ff7b72',2.5);
  const f2=faixa([s.espessura_min],t2,b2);eixoY(f2,t2,b2,'Espessura mínima na seção (m)');linha(s.estaca,s.espessura_min,f2.Y,'#f2c14e',2);
  g.fillStyle='#91a8bb';g.font='18px Segoe UI,Arial';for(let i=0;i<=6;i++){const v=x0+(x1-x0)*i/6;g.fillText(fmt(v,0)+' m',Math.min(X(v)-20,W-90),H-10);}
 }
 function mostrar(j){const r=j.result;el('greideResultado').hidden=false;
  el('greideSintetico').hidden=!r.sintetico;
  const tag=el('greideSituacao');tag.textContent='Conferências: '+ROTULO[r.situacao];tag.style.color=COR[r.situacao];
  const t=r.resumo;el('gCardEsp').textContent=fmt(t.esp_min,3)+' m';el('gCardVol').textContent=fmt(t.vol_final_terreno,1)+' m³';el('gCardVolInfo').textContent='Mínimo teórico: '+fmt(t.vol_teorico,1)+' m³';el('gCardConf').textContent=fmt(t.vol_conf_aterro,1)+' / '+fmt(t.vol_conf_fresagem,1)+' m³';
  desenhar(r.serie);
  const v=el('gVerif');v.replaceChildren();for(const i of r.verificacoes)linhaTabela(v,[i.item,i.limite,i.valor,ROTULO[i.situacao],i.local,i.nota],{coluna:3,valor:COR[i.situacao]});
  const a=r.levantamento,o=a.ondulacao_mm;el('gLevant').textContent=`Fonte: ${a.fonte}\nExtensão: ${fmt(a.extensao_m,0)} m em ${a.estacas} estacas, a cada ${fmt(a.passo_m,2)} m\nCotas sem dado: ${a.cotas_sem_dado}; interpoladas: ${a.cotas_interpoladas}\nVariação de cota entre lances, no eixo: mediana ${fmt(o.mediana,1)} mm; P95 ${fmt(o.p95,1)} mm; máxima ${fmt(o.maxima,1)} mm; ${fmt(o.pct_acima,0)} % acima de ${fmt(o.limite,2)} mm\nCaimento medido (medianas): LE ${fmt(a.caimento_pct.LE.mediana)} %; LD ${fmt(a.caimento_pct.LD.mediana)} %\nSeções que governam o greide: ${a.secoes_que_governam}`;
  const p=el('gPicos');p.replaceChildren();for(const q of a.picos)linhaTabela(p,[q.estaca_txt,q.ponto,fmt(q.residuo_mm,1),q.governa?'Sim':'Não'],q.governa?{coluna:3,valor:COR.atencao}:null);el('gPicosBloco').hidden=!a.picos.length;el('gPicosTitulo').textContent=`Pontos suspeitos do levantamento (${a.picos_total})`;
  const i=r.iri,s=el('gIriSeg');s.replaceChildren();el('gIriTabela').hidden=i.status!=='calculado';
  if(i.status==='calculado'){el('gIriTexto').textContent=`IRI médio ${fmt(i.media)} m/km; segmentos de ${fmt(i.minimo)} a ${fmt(i.maximo)} m/km. Perfil com pontos a cada ${fmt(i.dx,2)} m. ${i.metodo}.`+(i.classe==='classe2'?' Resultado de Classe 2.':'')+(i.avisos.length?'\n'+i.avisos.join('\n'):'');for(const q of i.segmentos)linhaTabela(s,[fmt(q.inicio,0),fmt(q.fim,0),fmt(q.media),!q.valido?'Falha de dados':q.excede?'Acima do limite':q.aquecimento?'Sem trecho de aquecimento':''],q.excede?{coluna:3,valor:COR.falha}:null);}
  else el('gIriTexto').textContent=i.motivo;
  const l=r.ondas_longas;el('gLongas').textContent=l?`Não é IRI. Resposta do mesmo modelo só às ondas de ${fmt(l.onda_minima_m,0)} m ou mais, em m/km, no eixo: pista existente ${fmt(l.terreno.media)}; conformação ${fmt(l.conformacao.media)}; greide final ${fmt(l.final.media)}.`:'';
  el('gAvisos').textContent=r.avisos.join('\n');
  const f=el('gArquivos');f.replaceChildren();for(const nome of j.files){const b=document.createElement('a');b.className='file secondary';b.textContent=nome;b.href=`/download/${j.id}/${encodeURIComponent(nome)}?key=${j.downloadKey}`;b.style.textDecoration='none';f.append(b);}
  el('gPasta').textContent=j.pasta||'Os arquivos ficam em POINT TOP\\Resultados, em uma pasta Greide_ com data e hora.';
 }
 window.POINT_TOP_GREIDE_MOSTRAR=mostrar;
 async function iniciar(corpo){running=true;travar();el('greideProgresso').value=0;msg('Calculando…');try{let j=await api('/api/greide/jobs',corpo);while(j.state==='running'){await new Promise(r=>setTimeout(r,500));j=await api('/api/jobs/'+j.id);el('greideProgresso').value=j.progress;}if(j.state==='error')throw Error(j.error||'Falha no cálculo.');mostrar(j);msg('Concluído. Confira as conferências e os pontos suspeitos antes de usar a nota.');}catch(e){msg('Falha: '+e.message);}finally{running=false;travar();}}
 el('greideCalcular').onclick=()=>{let p;try{p=parametros();}catch(e){msg(e.message);return;}iniciar({eixo:picked.eixo,levantamento:picked.levantamento,perfilIri:picked.perfilIri,params:p});};
 el('greideExemplo').onclick=()=>iniciar({demo:true,params:parametros()});
 travar();
})();
