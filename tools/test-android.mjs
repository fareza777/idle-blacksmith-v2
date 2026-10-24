import {execFileSync} from 'node:child_process';
import {readFileSync,writeFileSync} from 'node:fs';
import {inflateSync} from 'node:zlib';
import assert from 'node:assert/strict';

const adbPath='C:/Users/FAJAR/AppData/Local/Android/Sdk/platform-tools/adb.exe';
const serial='emulator-5562',pkg='com.emberoak.forgeempire',qa='D:/Idle Blacksmith/QA';
const deviceSave=`/sdcard/Android/data/${pkg}/files/workshop.json`;
const checks=[];
let rootY=100,scale=1;
const wait=ms=>new Promise(resolve=>setTimeout(resolve,ms));
const adb=(...args)=>execFileSync(adbPath,['-s',serial,...args],{maxBuffer:32*1024*1024,timeout:30000,stdio:['ignore','pipe','pipe']});
const shell=(...args)=>adb('shell',...args);
const launch=()=>shell('monkey','-p',pkg,'-c','android.intent.category.LAUNCHER','1');
const tap=(x,y)=>shell('input','tap',String(Math.round(x*scale)),String(Math.round(rootY+y*scale)));
const world=[[225,781],[845,803],[541,1225],[230,1580]];
const tickNow=()=>Number(BigInt(Date.now())*10000n+621355968000000000n);
function check(condition,label){assert.ok(condition,label);if(!checks.includes(label))checks.push(label);console.log('PASS',label);}
function screenshot(name){const bytes=adb('exec-out','screencap','-p');writeFileSync(`${qa}/${name}.png`,bytes);return png(bytes);}
function readState(name='android-observed-save'){
  adb('pull',deviceSave,`${qa}/${name}.json`);
  return JSON.parse(readFileSync(`${qa}/${name}.json`,'utf8'));
}
function png(bytes){
  const width=bytes.readUInt32BE(16),height=bytes.readUInt32BE(20),channels=bytes[25]===6?4:3;
  assert.equal(bytes[24],8);assert.ok(bytes[25]===6||bytes[25]===2);
  const blocks=[];
  for(let p=8;p<bytes.length;){const n=bytes.readUInt32BE(p);if(bytes.toString('ascii',p+4,p+8)==='IDAT')blocks.push(bytes.subarray(p+8,p+8+n));p+=n+12;}
  const raw=inflateSync(Buffer.concat(blocks)),stride=width*channels,data=Buffer.alloc(stride*height);
  for(let y=0;y<height;y++)for(let x=0;x<stride;x++){
    const filter=raw[y*(stride+1)],a=x>=channels?data[y*stride+x-channels]:0,b=y?data[(y-1)*stride+x]:0,c=y&&x>=channels?data[(y-1)*stride+x-channels]:0;
    let predictor=0;if(filter===1)predictor=a;if(filter===2)predictor=b;if(filter===3)predictor=(a+b)>>1;
    if(filter===4){const p=a+b-c,pa=Math.abs(p-a),pb=Math.abs(p-b),pc=Math.abs(p-c);predictor=pa<=pb&&pa<=pc?a:pb<=pc?b:c;}
    data[y*stride+x]=(raw[y*(stride+1)+1+x]+predictor)&255;
  }
  return {width,height,data,channels,pixel:(x,y)=>data.subarray((y*width+x)*channels,(y*width+x)*channels+3)};
}
function calibrate(image){
  scale=image.width/1080;let start=-1,run=0;
  for(let y=0;y<Math.min(image.height,500);y++){
    const c=image.pixel(Math.round(268*scale),y);
    if(c[0]===28&&c[1]===41&&c[2]===50){if(run===0)start=y;run++;if(run>=30*scale){rootY=start-124*scale;console.log('Viewport',scale,rootY);return;}}
    else run=0;
  }
  throw new Error('Cannot locate resource HUD; dismiss welcome/offline dialogs before starting QA');
}
async function checkpoint(label){
  const previous=readState().LastSavedUtc;
  shell('input','keyevent','3');
  const deadline=Date.now()+12000;let state;
  do {await wait(250);state=readState(label);}while(state.LastSavedUtc<=previous&&Date.now()<deadline);
  assert.ok(state.LastSavedUtc>previous,'Android pause did not finish saving');
  launch();await waitForWorkshop('android-resumed-workshop');return state;
}
async function waitForWorkshop(name){
  const deadline=Date.now()+30000;
  while(Date.now()<deadline){
    const image=screenshot(name);
    try {calibrate(image);return image;} catch(error) {if(!error.message.startsWith('Cannot locate resource HUD'))throw error;}
    await wait(500);
  }
  throw new Error('Android workshop did not render its HUD');
}
async function waitForOfflinePanel(){
  const deadline=Date.now()+30000;
  while(Date.now()<deadline){
    const image=screenshot('android-offline-earnings');
    if(image.pixel(Math.round(100*scale),Math.round(rootY+1000*scale)).equals(Buffer.from([28,41,50])))return;
    await wait(500);
  }
  throw new Error('Offline earnings panel did not render');
}
async function installFixture(state,label){
  state.LastSavedUtc=tickNow();
  shell('am','force-stop',pkg);
  writeFileSync(`${qa}/${label}.json`,JSON.stringify(state));adb('push',`${qa}/${label}.json`,deviceSave);
  launch();
  const limit=Date.now()+20000;
  while(Date.now()<limit){await wait(500);const loaded=readState();if(loaded.LastSavedUtc>state.LastSavedUtc){await wait(500);return;}}
  throw new Error('Fixture did not finish loading');
}
function region(image,x,y,w,h){
  const out=[];
  for(let row=Math.round(rootY+y*scale);row<Math.round(rootY+(y+h)*scale);row++){
    const begin=(row*image.width+Math.round(x*scale))*image.channels;
    out.push(image.data.subarray(begin,begin+Math.round(w*scale)*image.channels));
  }
  return Buffer.concat(out);
}

try {
  let settings;
  if(!process.argv.includes('--fixtures-only')) {
  await waitForWorkshop('android-workshop-initial');
  let start=await checkpoint('android-start-state');
  check(start.TutorialStep===1,'Welcome starts a persisted workshop');
  tap(...world[1]);await wait(200);
  const blocked=await checkpoint('android-starved-state');
  check(blocked.Ore===0&&blocked.Ingots===0&&!blocked.Running[1],'Starved hearth touch does not create resources');
  for(let i=0;i<2;i++){
    tap(...world[0]);await wait(2050);tap(...world[1]);await wait(2850);
  }
  tap(...world[2]);await wait(350);tap(696,1926);await wait(150);
  screenshot('android-forging');await wait(4400);
  tap(777,1332);await wait(300);screenshot('android-inventory-earned');
  tap(540,1644);await wait(2600);
  const sold=await checkpoint('android-golden-sale');
  check(sold.TotalCrafted===start.TotalCrafted+1&&sold.TotalSold===start.TotalSold+1,'Actual touch completes mine → smelt → forge → customer sale');
  check(sold.Gold>=start.Gold+24&&sold.Inventory.length===0,'Customer pays once and takes the crafted sword');
  tap(...world[0]);await wait(150);tap(944,1926);await wait(200);
  const upgraded=await checkpoint('android-earned-upgrade');
  check(upgraded.Levels[0]===2,'Context upgrade changes the selected mine');
  screenshot('android-workshop-earned');
  tap(1000,63);await wait(200);tap(870,939);await wait(200);tap(951,582);await wait(150);
  settings=await checkpoint('android-settings-state');
  check(settings.MusicEnabled===false,'Settings respond to touch and persist');
  } else {
    settings=JSON.parse(readFileSync(`${qa}/android-settings-state.json`,'utf8'));
    checks.push(...new Set(JSON.parse(readFileSync(`${qa}/android-test-result.json`,'utf8')).checks));
  }

  const fixture={...settings,Gold:1000,Ore:20,Ingots:10,Levels:[1,1,1,1],Automated:[false,false,false,false],Running:[false,false,false,false],Progress:[0,0,0,0],TotalSold:3,OrdersCompleted:0,BestCombo:3,Combo:0,PendingQuality:0,PendingSaleValue:0,Inventory:Array.from({length:10},(_,i)=>({Name:'Iron Sword',Value:24+6*(i%4),Quality:i%4}))};
  await installFixture(fixture,'android-ui-fixture');
  await waitForWorkshop('android-fixture-workshop');
  tap(777,1332);await wait(250);const beforeScroll=screenshot('android-inventory-top');
  shell('input','swipe',String(Math.round(540*scale)),String(Math.round(rootY+1410*scale)),String(Math.round(540*scale)),String(Math.round(rootY+800*scale)),'600');
  await wait(500);const afterScroll=screenshot('android-inventory-scrolled');
  check(!region(beforeScroll,100,650,800,850).equals(region(afterScroll,100,650,800,850)),'Android drag moves long inventory contents');
  tap(951,467);await wait(150);tap(672,2092);await wait(200);tap(540,1297);await wait(200);tap(951,667);
  const order=await checkpoint('android-commission-state');
  check(order.OrdersCompleted===1&&order.Gold===1040,'Earned commission pays once through its actual button');
  tap(408,2092);await wait(200);
  for(let i=0;i<4;i++){tap(856,857+i*184);await wait(220);}
  screenshot('android-crew-hired');shell('input','keyevent','4');
  const backFrame=await waitForWorkshop('android-back-closes-dialog');
  check(backFrame.pixel(Math.round(268*scale),Math.round(rootY+130*scale)).equals(Buffer.from([28,41,50])),'Android Back closes the dialog without leaving the workshop');
  const crew=await checkpoint('android-crew-state');
  check(crew.Automated.every(Boolean),'All four hire controls activate real automation');
  shell('input','keyevent','3');await wait(8000);launch();await wait(800);
  const resumed=await checkpoint('android-resumed-state');
  check(resumed.TotalMined>crew.TotalMined&&resumed.TotalSold>crew.TotalSold,'Background/resume advances the automated workshop');
  for(let i=0;i<4;i++){tap(...world[i]);await wait(150);tap(944,1926);await wait(200);}
  const expanded=await checkpoint('android-expansion-state');
  check(expanded.Levels.every(level=>level>=2),'Four paid upgrades unlock the visible guild expansion');
  screenshot('android-workshop-expanded');

  shell('am','force-stop',pkg);
  expanded.LastSavedUtc=tickNow()-120*10000000;
  writeFileSync(`${qa}/android-offline-fixture.json`,JSON.stringify(expanded));adb('push',`${qa}/android-offline-fixture.json`,deviceSave);launch();
  const deadline=Date.now()+20000;let offline;
  while(Date.now()<deadline){await wait(600);offline=readState();if(offline.LastSavedUtc>expanded.LastSavedUtc+100*10000000)break;}
  check(offline.Gold>expanded.Gold+100,'Cold restart applies elapsed offline production');
  await waitForOfflinePanel();tap(540,1410);await wait(200);
  const collected=await checkpoint('android-offline-collected');
  check(collected.Gold-offline.Gold<100,'Closing offline summary does not pay the reward again');
  screenshot('android-workshop-final');
  shell('wm','size','720x1280');await wait(700);screenshot('android-compact-screen');
  shell('wm','size','reset');await wait(700);
  const log=adb('logcat','-d','-s','Unity','AndroidRuntime').toString();writeFileSync(`${qa}/android-runtime.log`,log);
  check(!/FATAL EXCEPTION|NullReferenceException|AndroidJavaException|MissingMethodException/.test(log),'Android runtime has no captured fatal or managed exceptions');
  writeFileSync(`${qa}/android-test-result.json`,JSON.stringify({passed:true,checks,device:serial,nativeTouch:true,fixtureUsedAfterGoldenPath:true},null,2));
  console.log('ANDROID_TOUCH_PASS',checks.length);
} catch(error) {
  try{screenshot('android-failure');shell('wm','size','reset');}catch{}
  writeFileSync(`${qa}/android-test-result.json`,JSON.stringify({passed:false,checks,error:error.message},null,2));
  throw error;
}
