import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import crypto from 'node:crypto';
const names=['rowan','lyra','kael','mira','toren','edda'].flatMap(h=>['hero-'+h,...['mail','leather','robe'].map(f=>'worn-'+h+'-'+f)]);
for(const name of names)test(name+' has measured primary and offhand attachments for every painted pose',()=>{
 const root=new URL('../',import.meta.url),folder=new URL('Game/Assets/Resources/Art/HandDrawn/',root);
 const source=JSON.parse(fs.readFileSync(new URL('ArtSource/painted-grips-source.json',root),'utf8')).sheets[name];
 const map=JSON.parse(fs.readFileSync(new URL('painted-grips.json',folder),'utf8')).sheets.find(s=>s.name===name);
 const layout=JSON.parse(fs.readFileSync(new URL(name+'-layout.json',folder),'utf8'));
 const sha=crypto.createHash('sha256').update(fs.readFileSync(new URL(name+'.png',folder))).digest('hex');
 assert.equal(source.sha256,sha);assert.equal(map.sha256,sha);assert.equal(source.points.length,20);assert.equal(map.frames.length,20);
 for(let i=0;i<20;i++){
  const p=source.points[i],f=layout.frames[i],m=map.frames[i];assert.equal(p.length,4);
  for(const value of [m.x,m.y,m.offX,m.offY])assert.ok(Number.isFinite(value)&&value>=0&&value<=1,'grip outside its actual painted crop');
  const left=f.x*layout.width,top=(1-f.y-f.h)*layout.height;
  for(const [actual,expected] of [[left+m.x*f.w*layout.width,p[0]],[top+m.y*f.h*layout.height,p[1]],[left+m.offX*f.w*layout.width,p[2]],[top+m.offY*f.h*layout.height,p[3]]])assert.ok(Math.abs(actual-expected)<.001,'normalized grip changed the measured source hand');
  assert.ok(m.alphaPrimary>=100&&m.alphaOffhand>=100,'attachment points at transparent space');
 }
});
