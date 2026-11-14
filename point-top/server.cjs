'use strict';
const http=require('node:http'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {spawn}=require('node:child_process');
const ROOT=__dirname,PORT=Number(process.env.POINT_TOP_PORT||48731),ORIGIN=`http://127.0.0.1:${PORT}`;
function python(args){
 const c=JSON.parse(fs.readFileSync(path.join(ROOT,'python-runtime.json'),'utf8').replace(/^\uFEFF/,''));
 const env={...process.env,PYTHONHOME:c.home,PYTHONPATH:path.join(ROOT,'runtime','packages'),PYTHONIOENCODING:'utf-8',PYTHONUTF8:'1'};
 return spawn(c.executable,args,{cwd:path.join(ROOT,'python'),env,windowsHide:true,shell:false});
}
const VOLUME_FILES=['relatorio.html','resultado.csv','resultado.json','diferenca_2_menos_1.tif'],filesOf=j=>j.files||VOLUME_FILES;
const PICK={base:{ext:['.tif','.tiff'],filter:'GeoTIFF (*.tif;*.tiff)|*.tif;*.tiff'},comparison:{ext:['.tif','.tiff'],filter:'GeoTIFF (*.tif;*.tiff)|*.tif;*.tiff'},boundary:{ext:['.json','.geojson'],filter:'GeoJSON (*.geojson;*.json)|*.geojson;*.json'},eixo:{ext:['.xml','.landxml'],filter:'Eixo LandXML (*.xml;*.landxml)|*.xml;*.landxml'},levantamento:{ext:['.csv','.txt','.xml','.landxml'],filter:'Levantamento (*.csv;*.txt;*.xml;*.landxml)|*.csv;*.txt;*.xml;*.landxml'},perfilIri:{ext:['.csv','.txt'],filter:'Perfil longitudinal (*.csv;*.txt)|*.csv;*.txt'}};
function runPython(args){return new Promise((resolve,reject)=>{const c=python(args);let out='',err='';c.stdout.on('data',b=>out+=b);c.stderr.on('data',b=>err=(err+b).slice(-4000));c.on('error',reject);c.on('close',code=>code===0?resolve(out):reject(Error(err||out||'Falha no Python.')));});}
function createApp(){
 const token=crypto.randomBytes(32).toString('hex'), selections=new Map(),jobs=new Map();let active=null,picking=false;
 const json=(res,code,value)=>{res.writeHead(code,{'Content-Type':'application/json; charset=utf-8','Cache-Control':'no-store'});res.end(JSON.stringify(value));};
 const body=req=>new Promise((resolve,reject)=>{let s='';req.on('data',b=>{s+=b;if(s.length>8192){reject(Error('Pedido muito grande.'));req.destroy();}});req.on('end',()=>{try{resolve(JSON.parse(s||'{}'));}catch{reject(Error('JSON inválido.'));}});});
 function publicJob(j){return {id:j.id,state:j.state,progress:j.progress,error:j.error,result:j.result,downloadKey:j.state==='done'?j.downloadKey:undefined,files:j.state==='done'?filesOf(j):[]};}
 async function picker(kind){
  if(picking)throw Error('Já existe uma janela de seleção aberta.');picking=true;
  const filter=PICK[kind].filter;
  const script=`Add-Type -AssemblyName System.Windows.Forms; [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); $d = New-Object System.Windows.Forms.OpenFileDialog; $d.Filter = '${filter}'; $d.Title = 'POINT TOP - Selecionar arquivo'; if ($d.ShowDialog() -eq 'OK') { [Console]::Write($d.FileName) }`;
  try{return await new Promise((resolve,reject)=>{const p=spawn('powershell.exe',['-NoProfile','-STA','-EncodedCommand',Buffer.from(script,'utf16le').toString('base64')],{windowsHide:true,shell:false});let out='',err='';p.stdout.on('data',b=>out+=b);p.stderr.on('data',b=>err+=b);p.on('error',reject);p.on('close',c=>c===0?resolve(out.trim()):reject(Error(err||'Falha na seleção.')));});}finally{picking=false;}
 }
 function launch(config,script,res){
    const id=crypto.randomUUID(),j={id,state:'running',progress:0,downloadKey:crypto.randomBytes(24).toString('hex')};jobs.set(id,j);active=id;
    fs.mkdirSync(path.join(ROOT,'runtime','jobs'),{recursive:true});const input=path.join(ROOT,'runtime','jobs',id+'.json');fs.writeFileSync(input,JSON.stringify(config));
    let child;try{child=python(['-u',path.join(ROOT,'python',script),input]);}catch(e){active=null;j.state='error';j.error=e.message;return json(res,500,publicJob(j));}
    let pending='',errorText='',completion;
    child.stdout.setEncoding('utf8');child.stdout.on('data',chunk=>{pending+=chunk;let n;while((n=pending.indexOf('\n'))>=0){const line=pending.slice(0,n);pending=pending.slice(n+1);try{const e=JSON.parse(line);if(e.kind==='progress')j.progress=e.value;if(e.kind==='done')completion=e;if(e.kind==='error')j.error=e.message;}catch{j.error='Resposta inválida do motor.';}}});
    child.stderr.on('data',b=>errorText=(errorText+b).slice(-4000));child.on('error',e=>{j.error=e.message;});
    child.on('close',code=>{if(code===0&&completion&&!j.error){j.state='done';j.progress=100;j.folder=completion.folder;j.result=completion.result;j.files=completion.files;}else{j.state='error';j.error=j.error||errorText||'Processamento interrompido.';}active=null;fs.writeFileSync(path.join(ROOT,'runtime','jobs',id+'.result.json'),JSON.stringify(publicJob(j),null,2));});
    return json(res,202,publicJob(j));
 }
 const server=http.createServer(async(req,res)=>{try{
  if(req.headers.host!==`127.0.0.1:${PORT}`)return json(res,403,{error:'Endereço inválido.'});
  res.setHeader('X-Content-Type-Options','nosniff');res.setHeader('X-Frame-Options','DENY');res.setHeader('Referrer-Policy','no-referrer');
  const u=new URL(req.url,ORIGIN);
  const download=u.pathname.match(/^\/download\/([a-f0-9-]+)\/([^/]+)$/);
  if(req.method==='GET'&&download){
   const j=jobs.get(download[1]),name=download[2];
   if(!j||j.state!=='done'||u.searchParams.get('key')!==j.downloadKey||!filesOf(j).includes(name))return json(res,404,{error:'Arquivo indisponível.'});
   const p=path.join(j.folder,name);res.writeHead(200,{'Content-Type':'application/octet-stream','Content-Disposition':'attachment; filename="'+name+'"','Cache-Control':'no-store'});fs.createReadStream(p).on('error',()=>res.destroy()).pipe(res);return;
  }
  if(u.pathname.startsWith('/api/')){
   if(req.headers['x-point-top-token']!==token || (req.headers.origin&&req.headers.origin!==ORIGIN))return json(res,403,{error:'Sessão inválida. Reabra o painel.'});
   if(req.method==='GET'&&u.pathname==='/api/civil/sessions'){const link=require('./civil-link.cjs'),dir=path.join(ROOT,'runtime','civil3d');link.pruneDeadSessions(dir);return json(res,200,link.readSessions(dir));}
   if(req.method==='GET'&&u.pathname==='/api/health'){try{const info=await runPython(['-c','import rasterio,numpy; print(rasterio.__version__)']);return json(res,200,{ready:true,rasterio:info.trim()});}catch(e){return json(res,200,{ready:false,error:e.message});}}
   if(req.method==='POST'&&u.pathname==='/api/pick'){
    const b=await body(req);if(!Object.hasOwn(PICK,b.kind))throw Error('Seleção inválida.');
    const file=await picker(b.kind);if(!file)return json(res,200,{cancelled:true});
    const ext=path.extname(file).toLowerCase();if(!PICK[b.kind].ext.includes(ext)||!fs.statSync(file).isFile())throw Error('Arquivo inválido.');
    const id=crypto.randomUUID();selections.set(id,{file,kind:b.kind});return json(res,200,{id,name:path.basename(file),path:file});
   }
   if(req.method==='POST'&&u.pathname==='/api/jobs'){
    const b=await body(req);if(active)return json(res,409,{error:'Aguarde o cálculo em andamento.'});
    if(b.confirmed!==true)throw Error('Confirme as unidades e o datum vertical.');
    if(typeof b.tolerance!=='number'||!Number.isFinite(b.tolerance)||b.tolerance<0)throw Error('Tolerância inválida.');
    const config={tolerance:b.tolerance,output:path.join(ROOT,'Resultados')};
    if(b.demo===true){config.demo=true;config.demoFolder=path.join(ROOT,'exemplos','geotiff');}else{
     for(const key of ['base','comparison']){const s=selections.get(b[key]);if(!s||s.kind!==key)throw Error('Selecione as duas superfícies.');config[key]=s.file;}
     if(config.base===config.comparison)throw Error('Selecione duas superfícies diferentes.');
     if(b.boundary){const s=selections.get(b.boundary);if(!s||s.kind!=='boundary')throw Error('Limite inválido.');config.boundary=s.file;}
    }
    return launch(config,'worker.py',res);
   }
   if(req.method==='POST'&&u.pathname==='/api/greide/jobs'){
    const b=await body(req);if(active)return json(res,409,{error:'Aguarde o cálculo em andamento.'});
    const config={params:b.params&&typeof b.params==='object'?b.params:{},output:path.join(ROOT,'Resultados')};
    if(b.demo===true){config.demo=true;config.demoFolder=path.join(ROOT,'exemplos','greide');}else{
     for(const key of ['eixo','levantamento']){const s=selections.get(b[key]);if(!s||s.kind!==key)throw Error('Selecione o eixo e o levantamento.');config[key]=s.file;}
     if(b.perfilIri){const s=selections.get(b.perfilIri);if(!s||s.kind!=='perfilIri')throw Error('Perfil inválido.');config.perfilIri=s.file;}
    }
    return launch(config,'greide_worker.py',res);
   }
   const match=u.pathname.match(/^\/api\/jobs\/([a-f0-9-]+)(?:\/file\/([^/]+))?$/);
   if(req.method==='GET'&&match){const j=jobs.get(match[1]);if(!j)return json(res,404,{error:'Trabalho não encontrado nesta sessão.'});
    if(!match[2])return json(res,200,publicJob(j));const name=match[2];if(j.state!=='done'||!filesOf(j).includes(name))return json(res,404,{error:'Saída indisponível.'});
    const p=path.join(j.folder,name);res.writeHead(200,{'Content-Type':'application/octet-stream','Content-Disposition':`attachment; filename="${name}"`});fs.createReadStream(p).on('error',()=>res.destroy()).pipe(res);return;
   }
   return json(res,404,{error:'Recurso desconhecido.'});
  }
  if(req.method!=='GET')return json(res,405,{error:'Método inválido.'});
  const files={'/':'index.html','/index.html':'index.html','/core.js':'core.js','/ui.js':'ui.js','/volumes.js':'volumes.js','/civil.js':'civil.js','/greide.js':'greide.js'};
  const file=files[u.pathname];if(!file)return json(res,404,{error:'Não encontrado.'});
  let data=fs.readFileSync(path.join(ROOT,'app',file));if(file==='index.html')data=Buffer.from(data.toString().replace('</head>','</head>').replace('<script src="core.js">',`<script>window.POINT_TOP_TOKEN=${JSON.stringify(token)}</script><script src="core.js">`));
  res.writeHead(200,{'Content-Type':file.endsWith('.html')?'text/html; charset=utf-8':'text/javascript; charset=utf-8','Cache-Control':'no-store'});res.end(data);
 }catch(e){if(!res.headersSent)json(res,400,{error:e.message});else res.destroy();}});
 return {server,token};
}
if(require.main===module){const {server}=createApp();server.on('error',e=>{if(e.code==='EADDRINUSE'&&process.argv.includes('--open'))spawn('rundll32.exe',['url.dll,FileProtocolHandler',ORIGIN],{windowsHide:true,stdio:'ignore'}).unref();console.error(e.code==='EADDRINUSE'?`O POINT TOP já pode estar aberto em ${ORIGIN}.`:e.message);process.exitCode=1;});server.listen(PORT,'127.0.0.1',()=>{console.log(`POINT TOP — IFM: ${ORIGIN}`);if(process.argv.includes('--open'))spawn('rundll32.exe',['url.dll,FileProtocolHandler',ORIGIN],{windowsHide:true,stdio:'ignore'}).unref();});}
module.exports={createApp,PORT,ORIGIN};
