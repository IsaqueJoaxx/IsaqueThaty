const assert=require('node:assert/strict');const {analyze,dxf}=require('../app/core');
const s=(start,end,length=100,type='Line')=>({start,end,length,type});
const a=analyze([{name:'Eixo',segments:[s([0,0],[100,0]),s([100,0],[160,80])]}])[0];
assert.equal(a.length,200);assert.equal(a.gaps,0);assert.equal((dxf(a).match(/\nLINE\n/g)||[]).length,2);
assert.equal(analyze([{name:'Gap',segments:[s([0,0],[100,0]),s([101,0],[200,0])]}])[0].gaps,1);
assert.throws(()=>analyze([{name:'Erro',segments:[s([0,NaN],[1,0])]}]));
assert.throws(()=>analyze([{name:'Erro',segments:[s([0,0],[1,0],0)]}]));
assert.throws(()=>dxf(analyze([{name:'Curva',segments:[s([0,0],[1,1],2,'Curve')]}])[0]));
assert.equal(analyze([{name:'Espiral',segments:[s([0,0],[1,1],2,'Spiral')]}])[0].spirals,1);
console.log('8 verificações aprovadas: extensão, continuidade, DXF, gap, ponto inválido, comprimento inválido, bloqueio de curvas e inventário de espiral.');

