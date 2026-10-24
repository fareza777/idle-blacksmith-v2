import {withAseprite,root} from './aseprite.mjs';
import {colors} from '../ArtSource/palette.mjs';
import {workshop} from '../ArtSource/workshop.mjs';
import {blacksmith,miner,merchant,fire} from '../ArtSource/characters.mjs';
import {improvements,smoke,sparks,customer} from '../ArtSource/details.mjs';
import {icons} from '../ArtSource/icons.mjs';
import {campaignAssets} from '../ArtSource/campaign.mjs';
import {writeFile,stat} from 'node:fs/promises';
import path from 'node:path';

const assets=[
  {name:'workshop',width:384,height:592,frames:[workshop()],durations:[1000],slices:[['mine',16,114,129,170],['furnace',231,107,139,183],['forge',104,299,178,123],['market',21,404,114,137],['anvil-front',178,368,68,40],['counter-front',25,493,113,36],['customer-walk',150,458,152,66]]},
  {name:'blacksmith',width:64,height:64,frames:blacksmith(),durations:[160,100,140,65,55,70,100,150]},
  {name:'miner',width:48,height:56,frames:miner(),durations:[200,180,80,70,90,180]},
  {name:'merchant',width:48,height:56,frames:merchant(),durations:[500,180,140,400]},
  {name:'fire',width:32,height:40,frames:fire(),durations:[110,90,110,90,110,90]},
  {name:'icons',width:256,height:32,frames:[icons()],durations:[1000]},
  {name:'improvements',width:384,height:592,frames:improvements(),durations:[1000,1000,1000,1000,1000]},
  {name:'smoke',width:32,height:48,frames:smoke(),durations:[180,180,180,180,180,180]},
  {name:'sparks',width:48,height:48,frames:sparks(),durations:[45,55,70,85,105]},
  {name:'customer',width:48,height:56,frames:customer(),durations:[140,110,130,140,110,130]},
  ...campaignAssets,
];
let selector,outputDir=path.join(root,'Game/Assets/Resources/Art');
for(let i=2;i<process.argv.length;i++){
  const arg=process.argv[i];
  if(arg==='--output-dir'){
    const value=process.argv[++i];
    if(!value||value.startsWith('--'))throw new Error('--output-dir requires a directory');
    outputDir=path.resolve(root,value);
  }else if(arg.startsWith('--output-dir=')){
    const value=arg.slice('--output-dir='.length);
    if(!value)throw new Error('--output-dir requires a directory');
    outputDir=path.resolve(root,value);
  }else if(arg.startsWith('-'))throw new Error(`Unknown option: ${arg}`);
  else if(selector)throw new Error('Select only one asset per invocation');
  else selector=arg;
}
if(selector&&!['campaign','campaign-qa'].includes(selector)&&!assets.some(a=>a.name===selector))throw new Error(`Unknown asset: ${selector}`);
const relative=path.relative(root,outputDir);
if(relative==='..'||relative.startsWith(`..${path.sep}`)||path.isAbsolute(relative))throw new Error('Art output must stay inside Idle Blacksmith');
if(!(await stat(outputDir)).isDirectory())throw new Error(`Output directory does not exist: ${outputDir}`);
const authoredColors=asset=>[...new Set([...colors,...JSON.stringify(asset.frames).match(/#[0-9a-fA-F]{8}|#[0-9a-fA-F]{6}/g)])];
await withAseprite(async call=>{
  for(const asset of assets.filter(a=>!selector||a.name===selector||(selector==='campaign'&&campaignAssets.includes(a)))) {
    await call('sprite_manage',{op:'new',width:asset.width,height:asset.height,colorMode:'rgb'});
    await call('palette',{op:'set',colors:authoredColors(asset),replace:true});
    if(asset.frames.length>1)await call('frame',{op:'add',count:asset.frames.length-1});
    const layers=[...new Set(asset.frames.flatMap(f=>f.map(l=>l.name)))];
    for(const name of layers)await call('layer',{op:'create',name});
    for(let i=0;i<asset.frames.length;i++)for(const layer of asset.frames[i]) {
      for(let offset=0;offset<layer.ops.length;offset+=400)await call('draw',{layer:layer.name,frame:i+1,ops:layer.ops.slice(offset,offset+400),paletteLock:true,label:`${asset.name} authored frame ${i+1}`});
    }
    await call('frame',{op:'set_duration',durations:asset.durations});
    for(const tag of asset.tags??[{name:asset.name==='workshop'?'scene':asset.name==='improvements'?'station-upgrades':'work',from:1,to:asset.frames.length}])await call('tag',{op:'create',...tag,direction:'forward'});
    for(const [name,x,y,width,height] of asset.slices??[])await call('sprite_manage',{op:'slice_create',name,bounds:{x,y,width,height}});
    await call('sprite_manage',{op:'save_as',path:`${root}/ArtSource/${asset.name}.aseprite`});
    await call('export',{op:'spritesheet',path:path.join(outputDir,`${asset.name}.png`),sheetType:'horizontal',padding:0,trim:false,includeJson:true});
    if(asset.name==='improvements'||asset.previewFrames){
      for(let frame=1;frame<=asset.frames.length;frame++)await call('look',{op:'preview',frame,scale:2},`${asset.name}-${frame}-preview`);
    }else await call('look',{op:asset.frames.length>1?'filmstrip':'preview',scale:asset.width>128?2:3},`${asset.name}-preview`);
    if(asset.frames.length>1&&asset.name!=='improvements'&&!asset.noGif)await call('export',{op:'gif',path:`${root}/QA/${asset.name}.gif`,scale:4});
    const report=await call('validate',{});
    await writeFile(`${root}/QA/${asset.name}-validation.json`,JSON.stringify(report,null,2));
    await call('sprite_manage',{op:'close'});
    if(report.passed===false)throw new Error(`${asset.name} failed Aseprite validation; inspect QA/${asset.name}-validation.json`);
    console.log(`EXPORTED ${asset.name}: ${asset.width}x${asset.height}, ${asset.frames.length} authored frames -> ${outputDir}`);
  }
  // QA composites also use real Aseprite draw calls, never a second rasterizer.
  if(selector==='campaign'||selector==='campaign-qa'){
    const shift=(ops,dx,dy)=>ops.map(op=>{
      const moved={...op};
      for(const key of ['from','to','rect'])if(op[key])moved[key]={...op[key],x:op[key].x+dx,y:op[key].y+dy};
      if(op.points)moved.points=op.points.map(p=>({x:p.x+dx,y:p.y+dy}));
      if(op.kind==='grid'){moved.x=op.x+dx;moved.y=op.y+dy;}
      return moved;
    });
    const drawLayers=async(layers)=>{
      for(const layer of layers){
        await call('layer',{op:'create',name:layer.name});
        for(let offset=0;offset<layer.ops.length;offset+=400)await call('draw',{layer:layer.name,frame:1,ops:layer.ops.slice(offset,offset+400),paletteLock:true,label:'Campaign composition QA'});
      }
    };
    const preview=async(name)=>{
      await call('sprite_manage',{op:'save_as',path:`${root}/QA/${name}.aseprite`});
      await call('look',{op:'preview',frame:1,scale:2},name);
      await call('sprite_manage',{op:'close'});
    };
    const campaign=name=>campaignAssets.find(a=>a.name===name);
    const palette=[...new Set(campaignAssets.flatMap(authoredColors))];
    for(let region=0;region<5;region++){
      await call('sprite_manage',{op:'new',width:320,height:180,colorMode:'rgb'});
      await call('palette',{op:'set',colors:palette,replace:true});
      await drawLayers(campaign('battlefields').frames[region]);
      await drawLayers(campaign('heroes').frames[(region%3)*4].map(l=>({name:`QA hero ${region}`,ops:shift(l.ops,35,69)})));
      await drawLayers(campaign('enemies').frames[region*4].map(l=>({name:`QA boss ${region}`,ops:shift(l.ops,214,64)})));
      await preview(`campaign-battle-${region+1}-composite`);
    }
    // Open the existing source read-only in effect: save only a new QA copy.
    await call('sprite_manage',{op:'open',path:`${root}/ArtSource/workshop.aseprite`});
    await call('palette',{op:'set',colors:[...new Set([...authoredColors(assets[0]),...palette])],replace:true});
    for(const frame of campaign('townlife').frames)await drawLayers(frame);
    await preview('campaign-town-restored-composite');
  }
});
