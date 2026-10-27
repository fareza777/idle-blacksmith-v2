import fs from 'node:fs';
const root=new URL('../Game/Assets/Resources/Audio/Chronicles/',import.meta.url);fs.mkdirSync(root,{recursive:true});
const rate=24000,tau=Math.PI*2;
let seed=9137;const noise=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;};
const midi=n=>440*2**((n-69)/12);
const manifest=[];
function write(name,a){let peak=0;for(const x of a)peak=Math.max(peak,Math.abs(x));const gain=peak>.78?.78/peak:1;const b=Buffer.alloc(44+a.length*2);b.write('RIFF');b.writeUInt32LE(b.length-8,4);b.write('WAVEfmt ',8);b.writeUInt32LE(16,16);b.writeUInt16LE(1,20);b.writeUInt16LE(1,22);b.writeUInt32LE(rate,24);b.writeUInt32LE(rate*2,28);b.writeUInt16LE(2,32);b.writeUInt16LE(16,34);b.write('data',36);b.writeUInt32LE(a.length*2,40);for(let i=0;i<a.length;i++)b.writeInt16LE(Math.round(Math.max(-1,Math.min(1,a[i]*gain))*32767),44+i*2);fs.writeFileSync(new URL(name+'.wav',root),b);manifest.push({name,seconds:a.length/rate,peak:peak*gain});}
function add(a,start,length,freq,gain,kind){const offset=Math.round(start*rate),count=Math.min(Math.round(length*rate),a.length-offset);let low=0;for(let n=0;n<count;n++){const t=n/rate,attack=1-Math.exp(-t*(kind==='flute'?15:100)),tail=Math.min(1,(length-t)*12);let v;if(kind==='pluck')v=(Math.sin(tau*freq*t)+.32*Math.sin(tau*freq*2*t)+.12*Math.sin(tau*freq*3*t))*Math.exp(-3.5*t);else if(kind==='bell')v=(Math.sin(tau*freq*t)+.4*Math.sin(tau*freq*2.756*t)+.2*Math.sin(tau*freq*5.404*t))*Math.exp(-2.8*t);else {low=low*.92+noise()*.08;v=(Math.sin(tau*freq*t+.025*Math.sin(tau*4.7*t))+.16*Math.sin(tau*freq*2*t)+low*.25)*Math.exp(-t*.4);}a[offset+n]+=v*gain*attack*tail;}}
const themes=[
 {key:50,beat:.68,scale:[0,2,3,5,7,8,10],motif:[0,4,2,1,3,2,0,4,5,3,2,1,0,2,1,0]},
 {key:48,beat:.78,scale:[0,2,3,5,7,8,10],motif:[0,2,4,3,1,2,0,6,4,2,1,3,2,1,0,0]},
 {key:53,beat:.76,scale:[0,2,4,6,7,9,11],motif:[0,4,6,4,2,3,1,4,5,6,4,2,3,1,2,0]},
 {key:45,beat:.60,scale:[0,1,3,5,7,8,10],motif:[0,1,4,3,0,2,1,4,5,4,3,1,2,1,4,0]},
 {key:51,beat:.72,scale:[0,2,3,5,7,9,10],motif:[0,4,6,5,3,2,4,1,6,4,2,3,5,4,1,0]},
 {key:55,beat:.64,scale:[0,2,4,5,7,9,11],motif:[0,2,4,6,5,4,2,3,4,5,6,4,2,1,3,0]}
];
for(let r=0;r<6;r++){
 const t=themes[r],duration=t.beat*32,a=new Float32Array(Math.round(rate*duration)),amb=new Float32Array(a.length),boss=new Float32Array(a.length);
 for(let step=0;step<32;step++){
  const degree=t.motif[step%16],note=t.key+12+t.scale[degree];add(a,step*t.beat,t.beat*2,note?midi(note):220,.10,r===2||r===4?'bell':'pluck');
  if(step%4===0){const bass=t.key+[0,5,3,7][Math.floor(step/8)%4];add(a,step*t.beat,t.beat*4,midi(bass-12),.07,'flute');add(a,(step+.25)*t.beat,t.beat*3.7,midi(bass),.035,'flute');}
  if(step%8===4)add(a,step*t.beat,t.beat*3,midi(note+12),.04,'flute');
  const at=Math.round(step*t.beat*rate);for(let n=0;n<Math.min(rate*.5,boss.length-at);n++){const x=n/rate;boss[at+n]+=(Math.sin(tau*(58-18*x)*x)*Math.exp(-x*10)+noise()*.13*Math.exp(-x*30))*(step%4===0?.27:.10);}
 }
 let low=0;for(let i=0;i<a.length;i++){const time=i/rate,edge=Math.min(1,time*2,(duration-time)*2);a[i]*=edge;boss[i]*=edge;low=low*.97+noise()*.03;let v=low*.24+noise()*.008;
  if(r===0||r===3)v+=Math.sin(tau*70*time)*.009+((i%13991)<25?noise()*.1:0);
  if(r===1)v+=Math.sin(tau*.6*time)*low*.09;
  if(r===2)v+=Math.sin(tau*.11*time)*low*.13;
  if(r===4||r===5)v+=Math.sin(tau*196*time)*.007*Math.sin(tau*.13*time);
  amb[i]=v*edge;
 }write('region-'+r,a);write('ambience-'+r,amb);write('boss-'+r,boss);
}
for(const [material,partials,decay]of [['iron',[620,1031,1759],11],['wood',[170,329,510],21],['stone',[123,276,693],26],['crystal',[1046,1671,2817],7],['cloth',[80,130,220],32]])for(let variant=0;variant<3;variant++){
 const a=new Float32Array(Math.round(rate*.55));for(let i=0;i<a.length;i++){const t=i/rate;let v=noise()*(material==='cloth'?.28:.13)*Math.exp(-t*55);for(let j=0;j<partials.length;j++)v+=Math.sin(tau*partials[j]*(1+variant*.035)*t)*(.24/(j+1))*Math.exp(-t*(decay+j*3));a[i]=v*Math.min(1,t/.002);}write('impact-'+material+'-'+variant,a);
}
fs.writeFileSync(new URL('manifest.json',root),JSON.stringify({source:'Authored procedural compositions and physical-model inspired effects; no sampled recordings.',sampleRate:rate,clips:manifest},null,2)+'\n');console.log(`${manifest.length} authored audio clips generated`);
