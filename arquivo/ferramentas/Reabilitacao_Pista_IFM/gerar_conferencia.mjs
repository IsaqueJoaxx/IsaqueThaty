import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { Workbook, SpreadsheetFile } from '@oai/artifact-tool';

const root=path.dirname(fileURLToPath(import.meta.url));
const data=JSON.parse(await fs.readFile(path.join(root,'Conferencia_BR222_01/conferencia.json'),'utf8'));
const output=path.join(root,'outputs/br222-20261020');
await fs.mkdir(output,{recursive:true});
const wb=Workbook.create();
const sections=wb.worksheets.add('Seções');
const points=wb.worksheets.add('Pontos');
const n=data.points.length, m=data.sections.length, first=8, last=first+n-1, lastS=first+m-1;
const idValue=value=>/^\d+$/.test(value)?Number(value):value;

function base(sheet,lastRow,lastCol,title,subtitle){
  sheet.showGridLines=false;
  const area=sheet.getRange(`A1:${lastCol}${lastRow}`);
  area.format.font={name:'Arial',size:10,color:'#243247'};
  area.format.rowHeight=22;
  area.format.verticalAlignment='center';
  sheet.getRange('A2').values=[[title]];
  sheet.getRange('A2').format.font={name:'Arial',size:14,bold:true,color:'#193B62'};
  sheet.getRange('A4').values=[[subtitle]];
  sheet.getRange('A4').format.font={name:'Arial',size:10,italic:true,color:'#596477'};
  sheet.freezePanes.freezeRows(7);
}
function header(sheet,range,values){
  sheet.getRange(range).values=[values];
  sheet.getRange(range).format={fill:'#193B62',font:{name:'Arial',size:10,bold:true,color:'#FFFFFF'},
    wrapText:true,horizontalAlignment:'center',verticalAlignment:'center',rowHeight:40};
}
base(sections,lastS,'M','REABILITAÇÃO DE PISTA — IFM','Conferência preliminar • metade direita • não é nota de serviço executiva');
base(points,last,'K','PONTOS DO LEVANTAMENTO — IFM','Fonte: Texto colado.txt e EIXO_BR-222.xml • cotas originais preservadas');
sections.tabColor='#193B62';
sections.getRange('A5').values=[['Esquerda sem levantamento. Curvas sem superelevação de projeto calculada nesta conferência.']];
points.getRange('A5').values=[['Papéis propostos por geometria e salto de cota; ausência de referência exige revisão.']];
// Títulos em células largas, sem depender de transbordamento de texto no leitor.
for(const sheet of [sections,points]) sheet.getRange('A2:A5').clear({applyTo:'contents'});
sections.getRange('B2').values=[['BR-222 — IFM']];
sections.getRange('B2').format.font={name:'Arial',size:14,bold:true,color:'#193B62'};
sections.getRange('B4').values=[['CONFERÊNCIA']];
sections.getRange('B5').values=[['89 seções observadas']];
sections.getRange('M2').values=[['REABILITAÇÃO — IFM']];
sections.getRange('M2').format.font={name:'Arial',size:14,bold:true,color:'#193B62'};
sections.getRange('M4').values=[['PRÉVIA NÃO EXECUTIVA']];
sections.getRange('M5').values=[['ESQUERDA NÃO LEVANTADA']];
points.getRange('I2').values=[['PONTOS DO LEVANTAMENTO — IFM']];
points.getRange('I2').format.font={name:'Arial',size:14,bold:true,color:'#193B62'};
points.getRange('I4').values=[['Fonte: Texto colado.txt e EIXO_BR-222.xml']];
points.getRange('I5').values=[['Papéis propostos; conferir antes do cálculo executivo.']];

header(points,'A7:K7',['Seção','Número','Este (m)','Norte (m)','Cota medida (m)','Descrição','Progressiva (m)','Afastamento (m)','Papel proposto','Controle 0–3,50 m','Limite parcial −2% (m)']);
const pointMatrix=data.points.map(p=>[p.section,idValue(p.number),p.e,p.n,p.z,p.description,p.station,p.offset,p.role,
  p.role==='FUNDO DIREITO — CANDIDATO'?1:0,null]);
points.getRange(`A${first}:K${last}`).values=pointMatrix;
points.getRange('M2:N6').values=[['Parâmetro confirmado','Valor'],['Espessura mínima vertical (m)',.23],['Caimento em tangente (%)',-2],['Velocidade de projeto (km/h)',60],['Superelevação máxima (%)',6]];
points.getRange('M2:N2').format={fill:'#E6EDF4',font:{bold:true}};
points.getRange('M3:M6').format.columnWidth=42;
points.getRange('N3:N6').format.columnWidth=15;
points.getRange('M8:N12').values=[['Classificação exploratória','Valor'],['Faixa próxima ao eixo (m)',data.parameters.axis_band],['Salto mínimo de cota (m)',data.parameters.min_step],['Salto máximo de cota (m)',data.parameters.max_step],['Lacuna para separar grupos (m)',data.parameters.group_gap]];
points.getRange('M14').values=[['Alterar os limiares exige reprocessar; a classificação é um retrato desta leitura.']];
points.getRange('M16').values=[['XML: '+data.source_axis]];
points.getRange('M17').values=[['Projeção rastreável: '+data.source_projection]];
points.getRange('M19').values=[['Fundo já cortado: não descontar 26 cm novamente.']];
points.getRange('M20').values=[['Pontos além de +3,50 m: uso construtivo ainda não definido.']];
points.getRange(`K${first}:K${last}`).formulas=data.points.map((p,i)=>[`=IF(J${first+i}=1,E${first+i}+$N$3-H${first+i}*$N$4/100,"")`]);
for(const col of ['C','D','E','G','H','K']) points.getRange(`${col}${first}:${col}${last}`).setNumberFormat('0.000');
for(const [col,width] of Object.entries({A:8,B:10,C:16,D:18,E:14,F:12,G:18,H:16,I:58,J:16,K:19})) points.getRange(`${col}7:${col}${last}`).format.columnWidth=width;
points.getRange(`B${first}:B${last}`).setNumberFormat('00');
points.getRange(`I${first}:I${last}`).conditionalFormats.add('containsText',{text:'REVISAR',format:{fill:'#FFF1D6',font:{color:'#7B4A00'}}});

header(sections,'A7:M7',['Seção','Progressiva média (m)','Pontos','ID original','Ref. original (m)','ID fundo próximo','Offset fundo (m)','Fundo próximo (m)','Dif. ref.−fundo (m)','Controles à direita','Geometria','Limite parcial eixo (m)','Situação']);
const sectionMatrix=data.sections.map(s=>[s.section,s.station,s.point_count,idValue(s.original_ids),s.original_z,idValue(s.near_floor_id),
  s.near_floor_offset,s.near_floor_z,null,s.right_floor_count,s.geometry,null,
  !s.original_ids?'REVISAR REFERÊNCIA/FUNDO':s.geometry==='Curva'?'CURVA: AGUARDAR PROJETO':'CONFERIR CANDIDATOS']);
sections.getRange(`A${first}:M${lastS}`).values=sectionMatrix;
sections.getRange(`D${first}:D${lastS}`).setNumberFormat('00');
sections.getRange(`F${first}:F${lastS}`).setNumberFormat('00');
sections.getRange(`I${first}:I${lastS}`).formulas=data.sections.map((s,i)=>[`=IF(AND(ISNUMBER(E${first+i}),ISNUMBER(H${first+i})),E${first+i}-H${first+i},"")`]);
sections.getRange(`L${first}:L${lastS}`).formulas=data.sections.map((s,i)=>[`=IF(AND(K${first+i}="Reta",J${first+i}>0),_xlfn.MAXIFS(Pontos!$K$${first}:$K$${last},Pontos!$A$${first}:$A$${last},A${first+i},Pontos!$J$${first}:$J$${last},1),"")`]);
for(const col of ['B','E','G','H','I','L']) sections.getRange(`${col}${first}:${col}${lastS}`).setNumberFormat('0.000');
for(const [col,width] of Object.entries({A:8,B:18,C:9,D:13,E:15,F:16,G:15,H:17,I:18,J:14,K:12,L:19,M:32})) sections.getRange(`${col}7:${col}${lastS}`).format.columnWidth=width;
sections.getRange(`M${first}:M${lastS}`).conditionalFormats.add('containsText',{text:'REVISAR',format:{fill:'#FCE4DF',font:{bold:true,color:'#9B3020'}}});
sections.getRange('O2:P6').values=[['Resumo','Quantidade'],['Pontos recebidos',null],['Seções observadas',null],['Sem referência identificada',null],['Seções em curva',null]];
sections.getRange('P3:P6').formulas=[[`=COUNTA(Pontos!B${first}:B${last})`],[`=COUNTA(A${first}:A${lastS})`],[`=COUNTA(A${first}:A${lastS})-COUNT(E${first}:E${lastS})`],[`=COUNTIFS(K${first}:K${lastS},"Curva")`]];
sections.getRange('O2:P2').format={fill:'#E6EDF4',font:{bold:true}};
sections.getRange('O2:O10').format.columnWidth=37;
sections.getRange('P2:P10').format.columnWidth=14;
sections.getRange('O8').values=[['Limite parcial: fundo + 0,23 m e caimento −2%.']];
sections.getRange('O9').values=[['Não é greide harmonizado nem verifica entre os pontos.']];
sections.getRange('O10').values=[['Diferença ref.−fundo: pontos distintos próximos ao eixo.']];
await wb.recalculate();
const summary=sections.getRange('P3:P6').values.flat();
if(summary[0]!==707 || summary[1]!==89 || summary[2]!==5 || summary[3]!==27) throw new Error('Resumo não reconciliado: '+JSON.stringify(summary));
const lower=sections.getRange('L8').values[0][0];
if(Math.abs(lower-data.sections[0].axis_lower_bound_tangent)>1e-9) throw new Error('Limite parcial divergente');
const testRow=data.points.findIndex(p=>p.role==='FUNDO DIREITO — CANDIDATO')+first;
const before=points.getRange(`K${testRow}`).values[0][0];
points.getRange('N3').values=[[.24]];
await wb.recalculate();
if(Math.abs(points.getRange(`K${testRow}`).values[0][0]-before-.01)>1e-9) throw new Error('Recalculo não acompanhou espessura');
points.getRange('N3').values=[[.23]];
await wb.recalculate();
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!|#N/A',options:{useRegex:true,maxResults:15},maxChars:1800});
console.log(errors.ndjson);
for(const [sheetName,range,name] of [['Seções','A1:M14','previa-secoes.png'],['Pontos','A1:K14','previa-pontos.png']]){
  const preview=await wb.render({sheetName,range,scale:1,format:'png'});
  await fs.writeFile(path.join(output,name),new Uint8Array(await preview.arrayBuffer()));
}
const xlsx=await SpreadsheetFile.exportXlsx(wb);
await xlsx.save(path.join(output,'CONFERENCIA_LEVANTAMENTO_BR222_IFM.xlsx'));
console.log(JSON.stringify({verifiedSummary:summary,firstLowerBound:lower,output}));
