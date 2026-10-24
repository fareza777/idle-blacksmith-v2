import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { inflateSync } from 'node:zlib';
const root = new URL('../', import.meta.url);
const assets = [['workshop',384,592,1],['blacksmith',64,64,8],['fire',32,40,6],['miner',48,56,6],['merchant',48,56,4],['icons',256,32,1],['improvements',384,592,5],['smoke',32,48,6],['sparks',48,48,5],['customer',48,56,6],['worldmap',384,480,1],['battlefields',320,180,5],['heroes',64,64,12],['enemies',80,80,20],['equipment',640,32,1],['townlife',384,592,3],['villagers',48,56,12]];
for (const [name,w,h,frames] of assets) {
  test(`${name}: editable Aseprite source and unscaled spritesheet`, () => {
    const source = readFileSync(new URL(`ArtSource/${name}.aseprite`,root));
    assert.equal(source.readUInt16LE(4),0xa5e0);
    assert.equal(source.readUInt16LE(6),frames);
    assert.equal(source.readUInt16LE(8),w);
    assert.equal(source.readUInt16LE(10),h);
    const png=readFileSync(new URL(`Game/Assets/Resources/Art/${name}.png`,root));
    assert.equal(png.readUInt32BE(16),w*frames);
    assert.equal(png.readUInt32BE(20),h);
    assert.equal(png.subarray(1,4).toString(),'PNG');
  });
}
function readAlpha(name){
  const png=readFileSync(new URL(`Game/Assets/Resources/Art/${name}.png`,root));
  const w=png.readUInt32BE(16),h=png.readUInt32BE(20),type=png[25],channels=type===6?4:type===2?3:type===4?2:1;
  assert.equal(png[24],8,'8-bit authored PNG');
  const chunks=[];let transparency;
  for(let p=8;p<png.length;){let n=png.readUInt32BE(p),tag=png.toString('ascii',p+4,p+8);if(tag==='IDAT')chunks.push(png.subarray(p+8,p+8+n));if(tag==='tRNS')transparency=png.subarray(p+8,p+8+n);p+=n+12;}
  const data=inflateSync(Buffer.concat(chunks)),rows=Buffer.alloc(w*h*channels),stride=w*channels;
  for(let y=0;y<h;y++)for(let x=0;x<stride;x++){
    let raw=data[y*(stride+1)+1+x],filter=data[y*(stride+1)],a=x>=channels?rows[y*stride+x-channels]:0,b=y?rows[(y-1)*stride+x]:0,c=y&&x>=channels?rows[(y-1)*stride+x-channels]:0;
    let predictor=0;if(filter===1)predictor=a;if(filter===2)predictor=b;if(filter===3)predictor=Math.floor((a+b)/2);if(filter===4){let p=a+b-c,pa=Math.abs(p-a),pb=Math.abs(p-b),pc=Math.abs(p-c);predictor=pa<=pb&&pa<=pc?a:pb<=pc?b:c;}
    rows[y*stride+x]=(raw+predictor)&255;
  }
  return {w,h,alpha:(x,y)=>type===6?rows[(y*w+x)*4+3]:type===4?rows[(y*w+x)*2+1]:type===3?(transparency?.[rows[y*w+x]]??255):255};
}
for(const [name,w,h,frames] of assets.filter(a=>['blacksmith','miner','merchant','customer'].includes(a[0]))){
  test(`${name}: no detached arms or tools in authored frames`,()=>{
    const png=readAlpha(name);
    for(let frame=0;frame<frames;frame++){
      const seen=new Set(),sizes=[],height=h-10;
      for(let y=0;y<height;y++)for(let x=0;x<w;x++){
        let key=y*w+x;if(seen.has(key)||png.alpha(frame*w+x,y)<128)continue;
        let count=0,stack=[[x,y]];seen.add(key);
        while(stack.length){let [cx,cy]=stack.pop();count++;for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){
          const nx=cx+dx,ny=cy+dy,k=ny*w+nx;
          if(nx<0||nx>=w||ny<0||ny>=height||seen.has(k)||png.alpha(frame*w+nx,ny)<128)continue;
          seen.add(k);stack.push([nx,ny]);
        }}sizes.push(count);
      }
      sizes.sort((a,b)=>b-a);
      assert.ok((sizes[1]??0)<=18,`${name} frame ${frame+1} has detached ${sizes[1]}-pixel part (${sizes})`);
    }
  });
}
