import fs from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';
const data=JSON.parse(await fs.readFile(new URL('./translations.json',import.meta.url),'utf8'));
const wb=Workbook.create();
const overview=wb.worksheets.add('안내');
overview.showGridLines=false;
overview.getRange('A2:F2').merge();overview.getRange('A2').values=[['DEVORYA 번역 목록']];
overview.getRange('A4:F4').merge();overview.getRange('A4').values=[['2026-10-23 추출 · 한국어 / 영어 / 일본어 Localization 원문']];
overview.getRange('A6:C6').values=[['분류','문구 수','빈 번역이 있는 문구']];
overview.getRange('A7:C11').values=data.map(s=>[s.sheet,s.rows.length,s.rows.filter(r=>r.status!=='입력됨').length]);
const notes=['능력·스킬은 Unique_Skill, 특성은 General_Skill 기준으로 분류했습니다.','{percent} 등의 변수와 색상 태그는 게임 원문 그대로 유지했습니다.','입력됨은 번역 존재 여부이며, 번역 품질 검수 완료를 의미하지 않습니다.','연결 데이터가 없는 키도 테이블에 남아 있으므로 목록에 포함했습니다.','이 파일을 편집해도 Unity 데이터에 자동 반영되지는 않습니다.'];
notes.forEach((n,i)=>{overview.getRange('A'+(13+i)+':F'+(13+i)).merge();overview.getRange('A'+(13+i)).values=[[n]];});
overview.getRange('A1:F17').format.font.name='Malgun Gothic';
overview.getRange('A1:F17').format.font.size=11;
overview.getRange('A1:F17').format.columnWidth=20;
overview.getRange('A1:F17').format.rowHeight=25;
overview.getRange('A2').format.font.size=16;
overview.getRange('A6:C6').format.fill='#253A52';
overview.getRange('A6:C6').format.font.color='#FFFFFF';
const headers=['항목 이름','문구 종류','한국어','영어','일본어','번역 상태','Localization 키','연결 데이터'];
for(const s of data){
 const sh=wb.worksheets.add(s.sheet);sh.showGridLines=false;
 const rows=s.rows.map(r=>[r.name,r.type,...r.values,r.status,r.key,r.assets.join('\n')||'연결 없음']);
 sh.getRange('A1:H1').values=[headers];
 sh.getRange('A2:H'+(rows.length+1)).values=rows;
 const all=sh.getRange('A1:H'+(rows.length+1));
 all.format.font.name='Malgun Gothic';all.format.font.size=11;all.format.wrapText=true;all.format.verticalAlignment='top';
 const widths=[26,19,52,62,52,18,48,35];
 widths.forEach((w,i)=>sh.getRangeByIndexes(0,i,rows.length+1,1).format.columnWidth=w);
 sh.getRange('A1:H1').format.fill='#253A52';sh.getRange('A1:H1').format.font.color='#FFFFFF';sh.getRange('A1:H1').format.font.bold=true;
 sh.getRange('A1:H1').format.rowHeight=28;
 for(let i=0;i<rows.length;i++){
  const weighted=t=>[...String(t)].reduce((a,c)=>a+(c.charCodeAt(0)>255?2:1),0);
  const lines=Math.max(...rows[i].map((t,j)=>String(t).split('\n').reduce((n,l)=>n+Math.max(1,Math.ceil(weighted(l)/(widths[j]-3))),0)));
  sh.getRange('A'+(i+2)+':H'+(i+2)).format.rowHeight=Math.max(34,lines*17+12);
  if(i%2===1)sh.getRange('A'+(i+2)+':H'+(i+2)).format.fill='#F1F5F9';
 }
 sh.tables.add('A1:H'+(rows.length+1),true,'Translations'+(data.indexOf(s)+1));
 sh.freezePanes.freezeRows(1);
}
wb.recalculate();
console.log((await wb.inspect({kind:'table',range:"'특성'!A1:F4",include:'values',tableMaxRows:4,tableMaxCols:6,maxChars:2000})).ndjson);
const file=await SpreadsheetFile.exportXlsx(wb);
await file.save(fileURLToPath(new URL('./DEVORYA_번역목록_2026-10-23.xlsx',import.meta.url)));
for(const name of ['안내',...data.map(s=>s.sheet)]){
 const blob=await wb.render({sheetName:name,range:name==='안내'?'A1:F17':'A1:F5',scale:1,format:'png'});
 await fs.writeFile(new URL('./preview-'+name.replace('·','-')+'.png',import.meta.url),new Uint8Array(await blob.arrayBuffer()));
}
console.log('EXPORTED',data.reduce((n,s)=>n+s.rows.length,0));