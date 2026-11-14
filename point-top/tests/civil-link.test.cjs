'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),os=require('node:os'),path=require('node:path');const {readSessions,pruneDeadSessions}=require('../civil-link.cjs');const dir=fs.mkdtempSync(path.join(os.tmpdir(),'pointtop-link-'));const now=Date.now();
try{
 assert.deepEqual(readSessions(path.join(dir,'absent'),now),[]);
 assert.deepEqual(readSessions(dir,now),[]);
 const write=(name,value)=>fs.writeFileSync(path.join(dir,name),JSON.stringify(value));
 const valid={heartbeat:new Date(now).toISOString(),connected:true,active:true,data:{schema:1,axis:{name:'Teste'}}};
 write('session-1.json',valid);assert.equal(readSessions(dir,now)[0].stale,false);
 assert.equal(readSessions(dir,now+16000)[0].stale,true);
 write('session-2.json',{...valid,connected:false});assert.equal(readSessions(dir,now).find(s=>s.id==='session-2.json').connected,false);
 write('other.json',valid);assert.equal(readSessions(dir,now).length,2);
 fs.writeFileSync(path.join(dir,'session-3.json'),'{');assert.equal(readSessions(dir,now).length,2);
 write('session-4.json',{...valid,heartbeat:'invalid'});assert.equal(readSessions(dir,now).length,2);
 assert.equal(pruneDeadSessions(path.join(dir,'absent')),0);
 write('session-7.json',valid);write('session-8.json',valid);
 const antes=fs.readdirSync(dir).length;assert.equal(pruneDeadSessions(dir,pid=>pid!==7),1);
 assert.equal(fs.readdirSync(dir).length,antes-1);assert.ok(fs.existsSync(path.join(dir,'session-8.json'))&&fs.existsSync(path.join(dir,'other.json')));
 write('session-'+process.pid+'.json',valid);assert.equal(pruneDeadSessions(dir,undefined)>=0,true);assert.ok(fs.existsSync(path.join(dir,'session-'+process.pid+'.json')));
 console.log('14 verificações de publicação aprovadas: ausência, conexão, expiração, desconexão, nomes, JSON inválido e limpeza de sessões encerradas.');
}finally{fs.rmSync(dir,{recursive:true,force:true});}
