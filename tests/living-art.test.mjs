import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import crypto from 'node:crypto';
const base=new URL('../Game/Assets/Resources/Art/HandDrawn/',import.meta.url);
const specs=[['workpieces-metal',5,4],['workpieces-fine',5,4],['forge-fx',4,3],['held-weapons',4,2],...Array.from({length:24},(_,i)=>[`foes-${Math.floor(i/4)}-${i%4}`,3,4])];
for(const [name,columns,rows] of specs)test(`${name} has complete original alpha frames and isolated layout`,()=>{
 const bytes=fs.readFileSync(new URL(name+'.png',base)),layout=JSON.parse(fs.readFileSync(new URL(name+'-layout.json',base),'utf8'));
 const manifest=JSON.parse(fs.readFileSync(new URL('../ArtSource/living-forge-final.json',import.meta.url),'utf8'));
 const sha=crypto.createHash('sha256').update(bytes).digest('hex');assert.equal(sha,manifest.assets.find(x=>x.asset===name)?.originalSha256);assert.equal(layout.sha256,sha);
 assert.equal(bytes[25],6,'RGBA alpha required');assert.equal(layout.columns,columns);assert.equal(layout.rows,rows);assert.equal(layout.frames.length,columns*rows);
 for(const [i,f] of layout.frames.entries()){
  assert.ok(f.area>12,'empty art frame');assert.ok(f.w>0&&f.h>0&&f.pivotX>=0&&f.pivotX<=1&&f.pivotY>=0&&f.pivotY<=1);assert.ok(f.gutterClear,'neighbor or opaque backdrop at cell edge');
  const [a,b,c,d]=f.alphaBounds;assert.ok(Math.round(f.x*layout.width)<=a&&Math.round((1-f.y-f.h)*layout.height)<=b&&Math.round((f.x+f.w)*layout.width)>=c&&Math.round((1-f.y)*layout.height)>=d,'opaque silhouette clipped');
  for(const other of layout.frames.slice(0,i))assert.ok(f.x+f.w<=other.x+.000001||other.x+other.w<=f.x+.000001||f.y+f.h<=other.y+.000001||other.y+other.h<=f.y+.000001,'adjacent crop overlaps');
 }
 if(name.startsWith('foes-'))for(let row=0;row<2;row++)for(const f of layout.frames.slice(row*6,row*6+6))assert.equal(f.referenceHeight,layout.frames[row*6].referenceHeight,'enemy pose scale changes');
});
