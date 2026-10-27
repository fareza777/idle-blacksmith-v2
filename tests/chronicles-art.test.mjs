import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import crypto from 'node:crypto';
const folder=new URL('../Game/Assets/Resources/Art/HandDrawn/',import.meta.url);
const manifest=JSON.parse(fs.readFileSync(new URL('../ArtSource/chronicles-generation-final.json',import.meta.url),'utf8'));
const sheets=[...['rowan','lyra','kael','mira','toren','edda'].map(x=>['hero-'+x,20]),...Array.from({length:6},(_,i)=>['enemy-'+i,32]),...Array.from({length:5},(_,i)=>['gear-'+i,20]),...Array.from({length:3},(_,i)=>['boss-'+i,16]),['people',8],['landmarks',12],['workers',18],['walkers',32],['idles',8],['heroes',12]];
for(const [name,count]of sheets)test(`${name}: full original silhouettes and correct production layout`,()=>{
 const bytes=fs.readFileSync(new URL(name+'.png',folder)),layout=JSON.parse(fs.readFileSync(new URL(name+'-layout.json',folder),'utf8'));
 const hash=crypto.createHash('sha256').update(bytes).digest('hex');assert.equal(layout.sha256,hash);assert.equal(manifest.assets.find(x=>x.asset===name).originalSha256,hash,'source pixels changed');
 const width=bytes.readUInt32BE(16),height=bytes.readUInt32BE(20);assert.equal(layout.width,width);assert.equal(layout.height,height);assert.equal(bytes[25],6,'alpha channel missing');assert.equal(layout.frames.length,count);
 for(const [index,f]of layout.frames.entries()){
  assert.equal(f.id,index);assert.ok(f.referenceWidth>0&&f.referenceHeight>0);assert.ok(f.pivotX>=0&&f.pivotX<=1&&f.pivotY>=0&&f.pivotY<=1);
  const left=Math.round(f.x*width),top=Math.round((1-f.y-f.h)*height),right=Math.round((f.x+f.w)*width),bottom=Math.round((1-f.y)*height),[a,b,c,d]=f.alphaBounds;
  assert.ok(left<=a&&top<=b&&right>=c&&bottom>=d,`${name} ${index}: opaque silhouette is clipped`);assert.ok(f.area>350,'empty frame');
  for(let previous=0;previous<index;previous++){
   const p=layout.frames[previous];assert.ok(f.x+f.w<=p.x+.000001||p.x+p.w<=f.x+.000001||f.y+f.h<=p.y+.000001||p.y+p.h<=f.y+.000001,`${name} ${index}: neighboring crop bleed`);
  }
  const ref=layout.frames[name.startsWith('hero-')?0:Math.floor(index/layout.columns)*layout.columns];if(!name.startsWith('gear'))assert.equal(f.referenceHeight,ref.referenceHeight,'animation scale drift');
 }
});
test('twin-blade art retains both disconnected blades in one equipment frame',()=>{
 const layout=JSON.parse(fs.readFileSync(new URL('gear-1-layout.json',folder),'utf8'));const f=layout.frames[1];assert.ok(f.alphaBounds[2]-f.alphaBounds[0]>70,'second blade was dropped by connected-component ranking');
});
for(const name of ['worldmap-chronicles','dawnforge','forge-closeup'])test(`${name}: selected original environment is present`,()=>{
 const bytes=fs.readFileSync(new URL(name+'.png',folder));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),manifest.assets.find(x=>x.asset===name).originalSha256);assert.ok(bytes.readUInt32BE(16)>=1024&&bytes.readUInt32BE(20)>=720);
});
test('regional audio has finite audible samples and bounded peaks',()=>{
 const folder=new URL('../Game/Assets/Resources/Audio/Chronicles/',import.meta.url),m=JSON.parse(fs.readFileSync(new URL('manifest.json',folder),'utf8'));assert.equal(m.clips.length,33);
 for(const clip of m.clips){const b=fs.readFileSync(new URL(clip.name+'.wav',folder));assert.equal(b.toString('ascii',0,4),'RIFF');assert.equal(b.readUInt32LE(24),24000);assert.ok(clip.peak>0&&clip.peak<=.8);let sum=0;for(let i=44;i<b.length;i+=2)sum+=Math.abs(b.readInt16LE(i));assert.ok(sum/(b.length/2-22)>8,`${clip.name}: silent audio`);}
});
