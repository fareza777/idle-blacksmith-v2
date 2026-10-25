import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import crypto from 'node:crypto';
import { inflateSync } from 'node:zlib';

const folder = new URL('../Game/Assets/Resources/Art/HandDrawn/', import.meta.url);
function png(name) {
  const file = new URL(`${name}.png`, folder);
  assert.ok(fs.existsSync(file), `${name}: illustrated production asset is missing`);
  const bytes = fs.readFileSync(file);
  const width=bytes.readUInt32BE(16), height=bytes.readUInt32BE(20), depth=bytes[24], type=bytes[25];
  assert.equal(depth,8,`${name}: unsupported PNG depth`);
  const channels={0:1,2:3,3:1,4:2,6:4}[type];
  assert.ok(channels,`${name}: unsupported PNG color type`);
  const chunks=[]; let paletteAlpha;
  for(let offset=8;offset<bytes.length;) {
    const size=bytes.readUInt32BE(offset), tag=bytes.toString('ascii',offset+4,offset+8);
    if(tag==='IDAT') chunks.push(bytes.subarray(offset+8,offset+8+size));
    if(tag==='tRNS') paletteAlpha=bytes.subarray(offset+8,offset+8+size);
    offset+=size+12;
  }
  const data=inflateSync(Buffer.concat(chunks)), stride=width*channels, pixels=Buffer.alloc(stride*height);
  for(let y=0;y<height;y++) for(let x=0;x<stride;x++) {
    const raw=data[y*(stride+1)+x+1], filter=data[y*(stride+1)];
    const left=x>=channels?pixels[y*stride+x-channels]:0, up=y?pixels[(y-1)*stride+x]:0, corner=y&&x>=channels?pixels[(y-1)*stride+x-channels]:0;
    let predictor=0;
    if(filter===1) predictor=left;
    else if(filter===2) predictor=up;
    else if(filter===3) predictor=Math.floor((left+up)/2);
    else if(filter===4) { const p=left+up-corner, a=Math.abs(p-left), b=Math.abs(p-up), c=Math.abs(p-corner); predictor=a<=b&&a<=c?left:b<=c?up:corner; }
    else assert.equal(filter,0,`${name}: invalid PNG filter`);
    pixels[y*stride+x]=(raw+predictor)&255;
  }
  return {bytes,width,height,alpha:(x,y)=>type===6?pixels[(y*width+x)*4+3]:type===4?pixels[(y*width+x)*2+1]:type===3?(paletteAlpha?.[pixels[y*width+x]]??255):255};
}
const recipeSource=fs.readFileSync(new URL('../Game/Assets/ForgeEmpire/Core/GameContent.cs',import.meta.url),'utf8');
const recipes=[...recipeSource.matchAll(/Recipe\((\d+),\s*\d+,\s*GearSlot\.\w+,\s*"([^"]+)"/g)].map(match=>match[2]);

for(const name of ['equipment','enemies','guardians']) test(`${name}: production atlas has 20 isolated correctly indexed frames`,()=>{
  const image=png(name), layoutFile=new URL(`${name}-layout.json`,folder);
  assert.ok(fs.existsSync(layoutFile),`${name}: UV metadata is missing`);
  const layout=JSON.parse(fs.readFileSync(layoutFile,'utf8'));
  assert.ok(image.width>=1024&&image.height>=1024,`${name}: source resolution is too small`);
  assert.equal(layout.columns,4); assert.equal(layout.rows,5); assert.equal(layout.frames.length,20);
  assert.equal(layout.sha256,crypto.createHash('sha256').update(image.bytes).digest('hex'),`${name}: metadata is stale`);
  let transparent=0;
  for(let y=0;y<image.height;y+=8) for(let x=0;x<image.width;x+=8) if(image.alpha(x,y)<8) transparent++;
  assert.ok(transparent>image.width*image.height/64*.15,`${name}: genuine transparent gutters are missing`);
  layout.frames.forEach((frame,index)=>{
    assert.equal(frame.id,index); assert.equal(frame.region,Math.floor(index/4));
    assert.ok(frame.x>=0&&frame.y>=0&&frame.w>0&&frame.h>0&&frame.x+frame.w<=1.000001&&frame.y+frame.h<=1.000001,`${name} ${index}: UV outside texture`);
    assert.ok(frame.pivotX>=0&&frame.pivotX<=1&&frame.pivotY>=0&&frame.pivotY<=1,`${name} ${index}: invalid anchor`);
    assert.ok(frame.referenceWidth>0&&frame.referenceHeight>0,`${name} ${index}: no reference scale`);
    const left=Math.round(frame.x*image.width),top=Math.round((1-frame.y-frame.h)*image.height),right=Math.round((frame.x+frame.w)*image.width),bottom=Math.round((1-frame.y)*image.height);
    assert.ok(right-left>=40&&bottom-top>=30,`${name} ${index}: empty/tiny crop`);
    const opaque=frame.alphaBounds;
    assert.ok(left<=opaque[0]&&top<=opaque[1]&&right>=opaque[2]&&bottom>=opaque[3],`${name} ${index}: crop cuts off opaque silhouette`);
    const row=layout.frames.slice(Math.floor(index/4)*4,Math.floor(index/4)*4+4);
    if(index%4)assert.ok(frame.x>=row[index%4-1].x+row[index%4-1].w-.000001,`${name} ${index}: neighboring sprite bleed`);
    if(index>=4)assert.ok(frame.y+frame.h<=Math.max(...layout.frames.slice(Math.floor(index/4)*4-4,Math.floor(index/4)*4).map(f=>f.y))+.000001,`${name} ${index}: neighboring row bleed`);
    let visible=0;for(let y=top;y<bottom;y+=3)for(let x=left;x<right;x+=3)if(image.alpha(x,y)>80)visible++;
    assert.ok(visible>40,`${name} ${index}: transparent/empty frame`);
    if(name==='equipment') assert.equal(frame.label,recipes[index],`item ${index}: stable recipe mapping changed`);
    else { assert.equal(frame.pose,['ready','attack','hurt','defeated'][index%4]);assert.equal(frame.referenceHeight,layout.frames[Math.floor(index/4)*4].referenceHeight,`${name}: pose scales drift`); }
  });
});

test('ordinary creatures and guardians use distinct production art',()=>{
  assert.notEqual(crypto.createHash('sha256').update(png('enemies').bytes).digest('hex'),crypto.createHash('sha256').update(png('guardians').bytes).digest('hex'));
});
for(const name of ['worldmap','emberwood','marsh','frostpeak','caldera','starfall']) test(`${name}: final environment painting is present at useful resolution`,()=>{
  const image=png(name);
  assert.ok(image.width>=1024&&image.height>=720,`${name}: source resolution is too small`);
  if(name==='worldmap') assert.ok(image.height>image.width, 'map must be portrait');
  else assert.ok(image.width>image.height, 'regional battlefield must be landscape');
});
