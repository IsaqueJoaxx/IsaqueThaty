'use strict';
const fs=require('node:fs'),path=require('node:path');
function readSessions(folder,now=Date.now()){
 if(!fs.existsSync(folder))return [];
 const sessions=[];
 for(const name of fs.readdirSync(folder).filter(n=>/^session-\d+\.json$/.test(n))){
  try{const p=path.join(folder,name);if(fs.statSync(p).size>2000000)continue;const value=JSON.parse(fs.readFileSync(p,'utf8'));const beat=Date.parse(value.heartbeat);if(!Number.isFinite(beat))continue;
   sessions.push({id:name,...value,stale:now-beat>15000||beat>now+5000});
  }catch{/* Publicação incompleta ou arquivo inválido: não apresentar como conexão válida. */}
 }return sessions;
}
function isAlive(pid){try{process.kill(pid,0);return true;}catch(e){return e.code==='EPERM';}}
// Remove somente arquivos de sessão de processos que já terminaram, para o painel não listar sessões mortas.
function pruneDeadSessions(folder,alive=isAlive){
 if(!fs.existsSync(folder))return 0;let removed=0;
 for(const name of fs.readdirSync(folder)){const m=/^session-(\d+)\.json(\.tmp)?$/.exec(name);if(!m||alive(Number(m[1])))continue;try{fs.rmSync(path.join(folder,name));removed++;}catch{/* Arquivo em uso: nova tentativa na próxima consulta. */}}
 return removed;
}
module.exports={readSessions,pruneDeadSessions};
