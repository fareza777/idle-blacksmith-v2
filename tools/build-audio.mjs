import {writeFileSync} from 'node:fs';
const rate=32000, root='D:/Idle Blacksmith/Game/Assets/Resources/Audio/';
function wav(name,samples){
 const b=Buffer.alloc(44+samples.length*2);b.write('RIFF');b.writeUInt32LE(b.length-8,4);b.write('WAVEfmt ',8);b.writeUInt32LE(16,16);b.writeUInt16LE(1,20);b.writeUInt16LE(1,22);b.writeUInt32LE(rate,24);b.writeUInt32LE(rate*2,28);b.writeUInt16LE(2,32);b.writeUInt16LE(16,34);b.write('data',36);b.writeUInt32LE(samples.length*2,40);
 let peak=0;for(const s of samples)peak=Math.max(peak,Math.abs(s));const gain=peak>.72?.72/peak:1;for(let i=0;i<samples.length;i++)b.writeInt16LE(Math.round(Math.max(-1,Math.min(1,samples[i]*gain))*32767),44+i*2);
 writeFileSync(root+name+'.wav',b);console.log(name,samples.length/rate+'s');
}
let seed=7341;function noise(){seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;}
const tau=Math.PI*2;
function sound(name,duration,fn){const a=new Float32Array(Math.floor(duration*rate));for(let i=0;i<a.length;i++)a[i]=fn(i/rate);wav(name,a);}
sound('forge',.66,t=>{let v=0;for(const [f,g,d] of [[720,.35,8],[1193,.27,10],[1920,.20,13],[3077,.12,17]])v+=Math.sin(tau*f*t)*g*Math.exp(-d*t);return(v+noise()*.17*Math.exp(-90*t))*Math.min(1,t/.001);});
sound('mine',.28,t=>(noise()*.28*Math.exp(-28*t)+Math.sin(tau*190*t)*.24*Math.exp(-20*t)+Math.sin(tau*390*t)*.12*Math.exp(-32*t))*Math.min(1,t/.002));
sound('smelt',.7,t=>noise()*.25*Math.sin(Math.PI*t/.7)*Math.exp(-2*t)+Math.sin(tau*82*t)*.05*Math.sin(Math.PI*t/.7));
sound('coin',.55,t=>{const second=Math.max(0,t-.11);return Math.sin(tau*1318.5*t)*.26*Math.exp(-15*t)+(t>.11?Math.sin(tau*1975.5*second)*.24*Math.exp(-12*second):0);});
sound('click',.085,t=>Math.sin(tau*(420-170*t)*t)*.20*Math.exp(-60*t));
const duration=32,a=new Float32Array(rate*duration);
const melody=[62,69,74,72,69,65,67,69,62,65,69,67,65,60,62,57,62,69,74,77,74,72,69,65,67,69,65,62,60,57,60,62];
const bass=[38,38,41,41,36,36,43,43];
function note(start,midi,length,volume){const f=440*Math.pow(2,(midi-69)/12);for(let j=0;j<length*rate&&start*rate+j<a.length;j++){let t=j/rate,env=(1-Math.exp(-t*130))*Math.exp(-t*2.7)*Math.min(1,(length-t)*6);a[Math.floor(start*rate)+j]+=volume*env*(Math.sin(tau*f*t)+.28*Math.sin(tau*f*2*t)+.11*Math.sin(tau*f*3*t));}}
for(let i=0;i<32;i++){note(i,melody[i],2,.17);note(i+.16,melody[i]+12,1.7,.045);}
for(let i=0;i<8;i++){note(i*4,bass[i],4,.14);note(i*4+.5,bass[i]+12,3,.055);note(i*4+2,bass[i]+7,2,.07);}
for(let i=0;i<a.length;i++){const t=i/rate; a[i]*=Math.min(1,t*2,(duration-t)*2);}
wav('workshop',a);
