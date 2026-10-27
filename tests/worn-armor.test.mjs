import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import crypto from 'node:crypto';
const folder=new URL('../Game/Assets/Resources/Art/HandDrawn/',import.meta.url);
const manifest=JSON.parse(fs.readFileSync(new URL('../ArtSource/worn-armor-final.json',import.meta.url),'utf8'));
for(const hero of ['rowan','lyra','kael','mira','toren','edda'])for(const family of ['mail','leather','robe'])test(`${hero} wears ${family} as a complete painted twenty-pose body`,()=>{
 const name=`worn-${hero}-${family}`,bytes=fs.readFileSync(new URL(name+'.png',folder)),layout=JSON.parse(fs.readFileSync(new URL(name+'-layout.json',folder),'utf8'));
 const sha=crypto.createHash('sha256').update(bytes).digest('hex');assert.equal(manifest.assets.find(x=>x.asset===name)?.originalSha256,sha,'not the selected original');assert.equal(layout.sha256,sha);assert.equal(bytes[25],6,'true alpha missing');assert.equal(layout.frames.length,20);
 assert.equal(layout.columns,4);assert.equal(layout.rows,5);assert.equal(layout.width,bytes.readUInt32BE(16));assert.equal(layout.height,bytes.readUInt32BE(20));
 for(const [id,f]of layout.frames.entries()){
  assert.equal(f.id,id);assert.equal(f.referenceHeight,layout.frames[0].referenceHeight,'pose scale jumps');assert.ok(f.referenceHeight>=100,'body lacks useful source detail');assert.ok(f.area>1500,'empty or incomplete body');
  const[a,b,c,d]=f.alphaBounds;assert.ok(Math.round(f.x*layout.width)<=a&&Math.round((1-f.y-f.h)*layout.height)<=b&&Math.round((f.x+f.w)*layout.width)>=c&&Math.round((1-f.y)*layout.height)>=d,'body clipped');
  assert.ok(f.pivotX>=0&&f.pivotX<=1&&f.pivotY>=0&&f.pivotY<=1);
  for(const p of layout.frames.slice(0,id))assert.ok(f.x+f.w<=p.x+.000001||p.x+p.w<=f.x+.000001||f.y+f.h<=p.y+.000001||p.y+p.h<=f.y+.000001,'neighbor bleed');
 }
});
test('worn outfit manifest contains precisely eighteen selected originals',()=>assert.equal(new Set(manifest.assets.map(x=>x.asset)).size,18));
