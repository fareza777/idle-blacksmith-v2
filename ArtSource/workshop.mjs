import {P,r,box,e,line,poly} from './palette.mjs';

export const C={sky:'#303e56',sky2:'#465469',haze:'#69747c',cloud:'#7f8188',pine:'#203e48',pine2:'#294c52',pine3:'#365e61',moss:'#394d48',moss2:'#526452',earth:'#292f37',earth2:'#383b40',mortar:'#353a42',rock:'#4b5058',rock2:'#5d6267',rock3:'#73777a',floor:'#494b50',floor2:'#55565a',floor3:'#626064',warm:'#7b6355',warm2:'#9a7b60',brick0:'#4b3437',brick1:'#76463c',brick2:'#935342',brick3:'#b36a4c',brick4:'#cd8760',tile0:'#263844',tile1:'#36535b',tile2:'#466971',tile3:'#5b7e80',tile4:'#799793',plaster:'#4c4a4c',plaster2:'#5c5652',bark:'#392c30',leaf:'#658366'};
const pick=(n,list)=>list[((n%list.length)+list.length)%list.length];
export function stone(a,x,y,w,h,n=0,warm=false){
  const shades=warm?[C.brick1,C.brick2,C.brick3,C.brick2]:[C.rock,C.rock2,P.stone1,C.rock];
  a.push(poly([[x+1,y],[x+w-2,y],[x+w,y+2],[x+w-1,y+h-1],[x+2,y+h],[x,y+h-2],[x,y+2]],warm?C.brick0:C.mortar));
  a.push(poly([[x+2,y+1],[x+w-2,y+1],[x+w-1,y+3],[x+w-2,y+h-2],[x+2,y+h-1],[x+1,y+h-3],[x+1,y+2]],pick(n,shades)),line(x+3,y+1,x+w-4,y+1,warm?C.brick4:C.rock3),line(x+1,y+3,x+1,y+Math.max(3,h-4),warm?C.brick3:C.rock2));
  if(n%3===0)a.push(line(x+5,y+h-3,x+Math.min(w-3,11),y+h-3,warm?C.brick1:C.mortar),r(x+w-5,y+3,2,1,warm?C.brick2:C.rock3));
  if(n%4===1&&w>19)a.push(line(x+8,y+3,x+11,y+5,warm?C.brick1:C.rock),line(x+11,y+5,x+10,y+7,warm?C.brick1:C.rock));
}
export function beam(a,x,y,w,h){
  a.push(r(x,y,w,h,P.wood0),r(x+1,y+1,w-2,h-3,P.wood2));
  if(w>h){
    a.push(line(x+2,y+1,x+w-3,y+1,P.wood5),line(x+3,y+3,x+w-6,y+3,P.wood3),line(x+5,y+h-3,x+w-3,y+h-3,P.wood1));
    for(let i=7;i<w-10;i+=23)a.push(line(x+i,y+5,x+i+13,y+4,P.wood4),line(x+i+2,y+7,x+i+9,y+7,P.wood1));
  }else{
    a.push(line(x+1,y+2,x+1,y+h-4,P.wood5),line(x+3,y+2,x+3,y+h-5,P.wood3),line(x+w-3,y+4,x+w-3,y+h-3,P.wood1));
    for(let i=9;i<h-12;i+=31)a.push(line(x+5,y+i,x+4,y+i+12,P.wood4),line(x+6,y+i+3,x+6,y+i+9,P.wood1));
  }
}
export function plant(a,x,y,s=1){
  for(const [dx,dy] of [[-12,-8],[-8,-15],[-2,-19],[6,-15],[12,-9]]){
    const xx=x+dx*s,yy=y+dy*s;
    a.push(line(x,y,xx,yy,P.teal0,2),poly([[xx,yy],[xx-5*s,yy-4*s],[xx-7*s,yy-2*s],[xx-2*s,yy+2*s]],P.teal2),line(xx-5*s,yy-2*s,xx,yy,P.teal3));
  }
  a.push(poly([[x-4,y],[x-1,y-12],[x+2,y-14],[x+1,y-5],[x+4,y-7],[x+3,y]],C.leaf));
}
export function crate(a,x,y,w=30,h=26){
  a.push(e(x-2,y+h-3,w+7,8,P.wood0),r(x,y,w,h,P.wood0),r(x+2,y+2,w-4,h-4,P.wood2));
  for(let i=3;i<w-4;i+=7)a.push(r(x+i,y+3,5,h-6,P.wood3),line(x+i,y+4,x+i,y+h-6,P.wood4));
  a.push(line(x+3,y+3,x+w-4,y+h-4,P.wood1,4),line(x+3,y+2,x+w-4,y+h-5,P.wood5,2));
  beam(a,x,y,w,5);beam(a,x,y+h-5,w,5);
  for(const xx of [x+2,x+w-4])for(const yy of [y+2,y+h-3])a.push(r(xx,yy,2,1,P.iron4));
}
export function barrel(a,x,y,w=31,h=36,water=false){
  a.push(e(x-3,y+h-4,w+8,10,P.wood0),poly([[x+4,y+2],[x+w-5,y+2],[x+w,y+12],[x+w-2,y+h-5],[x+w-7,y+h],[x+5,y+h],[x,y+h-7],[x-1,y+12]],P.wood0));
  a.push(poly([[x+4,y+4],[x+w-6,y+3],[x+w-2,y+12],[x+w-4,y+h-5],[x+6,y+h-2],[x+2,y+h-8],[x+2,y+12]],P.wood3));
  for(let i=5;i<w-4;i+=6)a.push(line(x+i,y+7,x+i,y+h-4,P.wood1),line(x+i+1,y+8,x+i+1,y+h-5,P.wood4));
  for(const yy of [y+9,y+h-10])a.push(r(x+1,yy,w-3,4,P.iron0),line(x+2,yy,x+w-4,yy,P.iron3),r(x+6,yy+2,2,1,P.iron4));
  a.push(e(x+1,y,w-3,11,P.iron1),e(x+3,y+1,w-7,7,water?P.teal0:P.wood2),line(x+7,y+2,x+w-9,y+2,water?P.teal3:P.wood4));
  if(water)a.push(line(x+7,y+5,x+15,y+5,P.teal4),line(x+18,y+4,x+23,y+4,P.teal2),line(x+w-6,y+4,x+w-10,y+20,P.iron4,2));
}
export function lantern(a,x,y){
  a.push(line(x+2,y-13,x+2,y-5,P.iron0,2),e(x-1,y-7,7,7,P.iron2),e(x,y-6,5,5,P.ink),poly([[x-6,y+1],[x-3,y-3],[x+7,y-3],[x+10,y+1]],P.iron0),r(x-5,y+2,14,19,P.wood0),r(x-3,y+3,10,15,P.fire2),r(x-2,y+4,8,12,P.fire4),r(x,y+6,4,9,P.fire5),line(x-4,y+2,x-4,y+20,P.gold1),line(x+7,y+2,x+7,y+20,P.gold0),r(x-6,y+20,16,3,P.iron0),line(x-5,y+20,x+8,y+20,P.gold2),r(x-1,y+2,1,15,P.gold1));
}
export function sword(a,x,y,h=29){
  a.push(poly([[x,y],[x+3,y+5],[x+3,y+h-9],[x+1,y+h-7],[x-2,y+h-9],[x-2,y+5]],P.iron0),line(x,y+3,x,y+h-8,P.iron4,2),line(x+2,y+6,x+2,y+h-10,P.iron2),line(x-5,y+h-7,x+6,y+h-7,P.gold1,3),line(x-4,y+h-8,x+5,y+h-8,P.gold3),line(x,y+h-5,x,y+h+1,P.wood4,3),r(x-2,y+h+1,5,2,P.gold2));
}
function pine(a,x,y,h,c){
  a.push(line(x,y-h+7,x,y+7,C.bark,2));
  for(let j=0;j<4;j++){
    const top=y-h+j*Math.floor(h/6),span=Math.floor(h*(0.13+j*0.055));
    a.push(poly([[x,top],[x+span,top+Math.floor(h*.32)],[x+span-5,top+Math.floor(h*.31)],[x+span+3,top+Math.floor(h*.39)],[x-3,top+Math.floor(h*.36)],[x-span-4,top+Math.floor(h*.39)],[x-span+2,top+Math.floor(h*.29)]],c));
  }
}
export function anvil(a,upgraded=false){
  a.push(e(179,397,66,10,P.wood0),poly([[191,381],[220,381],[226,402],[219,407],[189,405],[187,398]],P.wood0),poly([[194,383],[217,383],[221,401],[216,404],[192,402]],P.wood3));
  for(const x of [195,201,211,217])a.push(line(x,385,x-1,401,P.wood1),line(x+1,385,x+1,399,P.wood4));
  a.push(line(191,388,221,389,P.iron0,3),line(190,400,223,401,P.iron0,3),r(197,390,2,2,P.iron3),r(215,402,2,1,P.iron3));
  a.push(poly([[178,368],[221,368],[245,372],[235,379],[222,380],[215,385],[214,392],[192,392],[195,384],[191,379],[181,378]],P.iron0),poly([[179,369],[219,369],[243,372],[233,376],[195,376],[181,374]],upgraded?P.iron4:P.iron3),line(181,369,219,369,P.iron5),line(196,374,233,374,P.iron4),poly([[195,378],[219,378],[211,384],[211,388],[197,388],[199,383]],P.iron2),line(194,390,215,390,P.iron3));
  if(upgraded)a.push(line(198,379,214,379,P.gold2),r(203,381,3,4,P.gold3),line(181,371,220,371,P.iron5),r(222,371,5,2,P.iron0));
}
export function counter(a,fine=false){
  a.push(poly([[28,501],[134,501],[131,528],[31,528]],P.wood0),r(31,505,99,21,P.wood2));
  for(let i=0;i<6;i++){
    const x=33+i*16;
    a.push(r(x,505,13,19,i%2?P.wood2:P.wood3),line(x+1,506,x+1,522,P.wood4),line(x+11,507,x+11,524,P.wood1),line(x+4,513,x+4,520,P.wood1));
  }
  a.push(r(28,523,105,5,P.wood1),line(30,523,131,523,P.wood4),poly([[25,493],[131,493],[137,497],[137,504],[25,504]],P.wood0),poly([[26,493],[130,493],[135,496],[28,498]],fine?P.wood6:P.wood5),r(26,499,110,3,P.wood3),line(28,498,135,498,P.wood6),line(43,495,63,495,P.wood3),line(88,496,114,496,P.wood4));
  a.push(r(63,507,39,15,P.wood0),r(65,508,35,12,P.teal0),line(68,510,96,510,P.gold1),poly([[77,512],[88,512],[91,514],[87,516],[83,516],[82,519],[78,519],[79,515]],P.gold2));
}
export function workshop(){
  const sky=[],ground=[],walls=[],roof=[],mine=[],kiln=[],props=[],market=[],light=[],front=[],garden=[];
  sky.push(r(0,0,384,592,C.sky));
  for(const [y,h,c] of [[20,19,'#37455c'],[39,17,C.sky2],[56,20,'#536374'],[76,25,C.haze]])sky.push(r(0,y,384,h,c));
  sky.push(poly([[0,38],[37,35],[61,39],[86,37],[123,40],[109,44],[53,44],[29,42],[0,43]],'#626b7d'),poly([[210,24],[239,20],[269,23],[296,21],[345,27],[327,30],[273,27],[239,29]],'#565f73'),e(75,20,22,22,'#aaa79b'),e(80,17,20,22,C.sky2));
  for(const [x,y] of [[24,18],[149,12],[176,36],[324,14],[360,44],[114,25]])sky.push(r(x,y,1,1,'#b1b3a6'));
  sky.push(poly([[0,88],[0,62],[23,64],[48,73],[88,60],[128,63],[160,50],[203,64],[228,60],[265,75],[305,58],[336,67],[363,53],[384,60],[384,138]],'#405760'),poly([[0,83],[28,71],[56,78],[85,72],[131,91],[163,74],[193,89],[224,76],[262,92],[306,78],[334,92],[369,73],[384,83],[384,172],[0,172]],C.pine3));
  for(const [x,y,h] of [[9,113,49],[32,106,40],[57,117,58],[84,111,35],[113,120,55],[142,110,40],[179,117,45],[213,117,34],[245,116,43],[276,116,39],[320,111,59],[351,119,48],[379,126,68]])pine(sky,x,y,h,C.pine2);
  for(const [x,y,h] of [[0,161,92],[21,158,74],[368,178,110],[390,163,98]])pine(sky,x,y,h,C.pine);
  ground.push(poly([[0,147],[31,129],[76,133],[114,149],[180,139],[235,151],[307,141],[358,133],[384,155],[384,592],[0,592]],C.earth),poly([[17,262],[99,232],[167,249],[221,239],[305,253],[364,279],[372,527],[343,553],[267,560],[248,591],[112,591],[87,564],[24,542],[8,478]],C.earth2));
  ground.push(poly([[26,267],[91,240],[161,250],[234,244],[331,260],[356,291],[348,417],[357,501],[331,539],[257,543],[241,576],[125,576],[109,547],[31,531],[18,472]],C.floor));
  for(let row=0;row<18;row++){
    const y=254+row*16;
    for(let col=0;col<10;col++){
      const x=27+col*32-(row%2)*13;
      if(x<23||x>337)continue;
      const n=col*7+row*3;
      ground.push(poly([[x+3,y+1],[x+27,y],[x+30,y+4],[x+28,y+12],[x+3,y+13],[x,y+9]],pick(n,[C.floor2,C.floor,C.floor3,C.floor2,C.floor])),line(x+4,y+1,x+24,y+1,pick(n,[C.rock2,C.floor3,C.rock2])));
      if(n%4===0)ground.push(line(x+7,y+9,x+13,y+9,C.mortar),r(x+21,y+5,2,1,C.rock2));
      ground.push(line(x+7+(n%6),y+3,x+15+(n%6),y+3,n%3?C.floor3:C.rock2),r(x+4+(n%17),y+7,2,1,C.floor),r(x+19,y+10,2,1,C.floor3));
      if(n%7===0)ground.push(line(x+23,y+1,x+20,y+5,C.mortar),line(x+20,y+5,x+23,y+8,C.mortar));
    }
  }
  for(const [x,y,w,h,n] of [[118,546,136,12,0],[111,559,150,12,1],[104,573,164,12,2],[92,586,187,10,3]]){
    ground.push(poly([[x+5,y],[x+w-8,y],[x+w,y+h],[x-3,y+h]],C.mortar),line(x+5,y,x+w-9,y,C.rock3,2),line(x+1,y+h-2,x+w-1,y+h-2,C.earth));
    for(let i=0;i<4;i++)stone(ground,x+4+i*Math.floor((w-7)/4),y+1,Math.floor((w-7)/4)-1,h-3,n+i);
  }
  walls.push(poly([[15,144],[144,116],[243,123],[365,141],[363,299],[296,308],[233,293],[139,305],[22,292]],P.wood0));
  for(let row=0;row<10;row++)for(let col=0;col<12;col++){
    const x=20+col*29-(row%2)*12,y=143+row*15;
    if(x<18||x+28>366)continue;
    stone(walls,x,y,28,14,col+row);
  }
  walls.push(poly([[127,149],[246,146],[252,274],[240,297],[134,295]],C.plaster),poly([[139,152],[234,151],[240,272],[232,285],[139,287]],C.plaster2));
  for(let j=0;j<18;j++){
    const x=140+(j*31)%91,y=156+(j*23)%129;
    walls.push(line(x,y,x+6,y,C.plaster),line(x+2,y+2,x+11,y+2,C.plaster));
  }
  walls.push(poly([[164,204],[164,176],[171,163],[181,157],[193,157],[206,165],[214,179],[214,204]],P.wood0),poly([[168,201],[168,176],[176,165],[187,161],[199,165],[209,178],[209,201]],P.teal0),poly([[171,198],[171,177],[177,168],[187,164],[198,169],[206,180],[206,198]],C.pine2));
  walls.push(line(187,164,187,200,P.wood3,3),line(170,180,207,180,P.wood3,3),line(170,195,207,195,P.wood3,2),line(175,173,178,169,P.teal3),line(196,184,201,190,P.teal2),r(161,203,56,6,P.wood0),line(163,204,214,204,P.wood5));
  for(const x of [17,124,239,357]){
    beam(walls,x,141,10,162);
    for(const y of [158,277])walls.push(r(x,y,10,7,P.iron0),line(x+1,y,x+8,y,P.iron2),r(x+3,y+3,2,2,P.iron4));
  }
  beam(walls,17,288,350,12);
  walls.push(line(130,147,151,173,P.wood0,8),line(131,147,151,171,P.wood4,3),line(242,148,221,173,P.wood0,8),line(240,148,221,171,P.wood3,3));
  roof.push(poly([[5,134],[30,104],[154,94],[189,84],[224,97],[350,105],[380,136],[376,148],[7,148]],P.ink));
  for(let row=0;row<4;row++)for(let col=0;col<19;col++){
    const x=14+col*19-(row%2)*9,y=106+row*8+Math.floor(Math.abs(190-(14+col*19))/50);
    if((row===0&&(col===0||col===18))||x<8||x>364)continue;
    const n=(row*7+col*3)%5;
    roof.push(poly([[x+4,y],[x+18,y-1],[x+21,y+8],[x+17,y+12],[x+1,y+12],[x-1,y+9]],C.tile0),poly([[x+5,y],[x+16,y],[x+19,y+8],[x+15,y+10],[x+2,y+10]],pick(n,[C.tile1,C.tile2,C.tile1,C.tile3,C.tile2])),line(x+5,y+1,x+15,y+1,C.tile4),line(x+3,y+8,x+15,y+8,C.tile3),line(x+6,y+3,x+7,y+6,C.tile3));
    if(n===1)roof.push(r(x+10,y+7,4,1,C.moss2),r(x+12,y+6,2,1,C.moss));
    if(n===3)roof.push(line(x+13,y+2,x+11,y+5,C.tile1));
  }
  roof.push(poly([[143,104],[187,83],[232,104],[227,109],[187,91],[147,110]],P.wood0),line(147,103,187,86,P.wood5,3),line(188,86,229,105,P.wood4,3));
  beam(roof,8,140,368,11);
  for(const x of [22,63,106,149,232,276,319,361])roof.push(poly([[x,151],[x+12,151],[x+9,160],[x+4,165],[x,160]],P.wood0),line(x+2,153,x+7,159,P.wood4,2));
  roof.push(e(175,109,27,24,P.wood0),e(178,111,21,18,P.wood3),line(181,115,194,124,P.gold2,3),line(195,115,182,126,P.gold3,2),r(177,116,8,4,P.iron3),line(177,116,184,116,P.iron5));
  mine.push(poly([[17,229],[16,177],[29,148],[47,134],[82,128],[113,140],[139,166],[145,232],[134,249],[25,249]],C.earth),poly([[37,238],[37,173],[46,156],[63,146],[91,147],[107,159],[115,181],[113,239]],P.ink),poly([[45,237],[47,177],[57,164],[76,155],[91,161],[103,181],[102,233]],'#202c35'),poly([[49,232],[57,187],[71,176],[79,195],[70,218],[86,233]],'#293a43'),poly([[94,163],[107,188],[106,232],[83,232],[91,211],[84,190]],P.deep));
  for(const [x,y,w,h,n] of [[20,163,18,24,0],[19,186,18,23,1],[20,211,18,27,2],[29,144,22,21,2],[46,132,23,21,1],[69,130,25,20,3],[94,137,22,23,1],[113,154,22,24,2],[120,177,21,22,0],[120,201,22,22,1],[116,224,25,24,2]])stone(mine,x,y,w,h,n);
  for(const [x,y] of [[58,185],[89,176],[94,214]])mine.push(poly([[x,y+4],[x+4,y],[x+9,y+3],[x+7,y+10],[x+1,y+8]],P.iron1),line(x+4,y+1,x+7,y+3,P.iron3));
  beam(mine,32,163,9,79);beam(mine,108,164,9,79);beam(mine,28,158,93,11);
  mine.push(line(40,174,50,168,P.wood4,4),line(100,168,107,177,P.wood3,4));
  for(const x of [34,110])for(const y of [178,227])mine.push(r(x,y,6,5,P.iron0),r(x+2,y+1,2,2,P.iron3));
  for(const [y,w] of [[235,38],[247,48],[261,61],[278,77],[298,91]])mine.push(line(76-Math.floor(w/2),y,76+Math.floor(w/2),y+1,P.wood0,5),line(77-Math.floor(w/2),y-1,76+Math.floor(w/2),y,P.wood3,2));
  mine.push(line(62,234,28,310,P.iron0,4),line(96,234,122,310,P.iron0,4),line(62,234,29,310,P.iron3),line(96,234,121,310,P.iron2));
  mine.push(e(15,286,38,12,P.wood0),poly([[17,264],[48,264],[53,282],[22,284]],P.iron0),poly([[20,266],[46,266],[49,278],[24,280]],P.iron1),line(24,269,46,268,P.iron3),e(24,280,8,9,P.ink),e(43,279,8,9,P.ink),e(26,282,4,5,P.iron3),e(45,281,4,5,P.iron2));
  for(const [x,y] of [[20,262],[30,257],[39,262]])mine.push(poly([[x,y+6],[x+3,y],[x+10,y-2],[x+14,y+6],[x+9,y+10]],P.iron1),poly([[x+2,y+4],[x+5,y],[x+10,y],[x+11,y+3],[x+7,y+6]],P.iron3));
  kiln.push(poly([[281,90],[313,90],[315,153],[331,171],[342,203],[342,260],[335,276],[250,276],[244,257],[247,204],[260,174],[280,151]],C.brick0));
  for(let row=0;row<6;row++)for(let col=0;col<2;col++)stone(kiln,282+col*15-(row%2?2:0),94+row*10,15,10,row+col,true);
  kiln.push(r(278,88,39,7,P.iron0),r(280,88,35,3,P.stone2),line(282,88,312,88,P.stone4),r(280,147,36,6,C.brick0),line(282,148,313,148,C.brick4));
  for(let row=0;row<9;row++){
    const y=165+row*11,left=row===0?271:row===1?261:251,width=row===0?45:row===1?65:86;
    for(let col=0;col<Math.floor(width/17);col++)stone(kiln,left+col*17,y,17,11,row*3+col,true);
  }
  kiln.push(poly([[266,256],[265,221],[270,207],[280,199],[297,198],[309,207],[315,222],[315,257]],C.brick0),poly([[270,254],[270,221],[276,208],[286,204],[296,206],[308,216],[310,253]],P.ink),poly([[270,241],[277,236],[304,236],[310,243],[309,256],[271,256]],P.fire0),e(271,245,39,13,P.fire1),e(276,248,29,9,P.fire2));
  for(const [x,y,w,h,n] of [[258,223,11,18,0],[258,241,11,19,1],[262,209,13,16,1],[270,198,15,13,2],[284,193,15,12,3],[299,199,15,13,2],[310,211,13,16,1],[314,227,12,18,0],[314,245,12,17,1]])stone(kiln,x,y,w,h,n,true);
  kiln.push(poly([[246,262],[338,262],[345,271],[341,283],[245,283],[241,272]],P.wood0),poly([[247,263],[336,263],[341,270],[246,270]],C.brick3),line(248,264,335,264,C.brick4),r(247,273,91,7,C.brick1));
  for(const x of [262,289,318])kiln.push(line(x,274,x,280,C.brick0));
  kiln.push(line(261,182,327,182,P.iron0,4),line(262,181,326,181,P.iron3),r(267,182,3,2,P.gold1),r(320,182,3,2,P.gold1));
  kiln.push(line(340,215,351,272,P.iron0,4),line(340,215,351,272,P.iron3,2),poly([[346,268],[356,267],[360,283],[350,286]],P.iron0),line(349,270,354,270,P.iron3));
  crate(props,319,306,31,25);crate(props,328,282,26,24);
  for(const [x,y] of [[323,277],[335,278]])props.push(poly([[x,y+4],[x+3,y],[x+9,y],[x+11,y+6]],P.iron2),line(x+4,y,x+8,y,P.iron4));
  props.push(e(169,395,80,16,'#30283180'));
  beam(props,132,308,117,10);beam(props,137,317,8,31);beam(props,237,317,8,30);beam(props,141,339,100,7);
  props.push(poly([[136,304],[246,304],[251,309],[130,309]],P.wood4),line(135,304,245,304,P.wood6));
  for(const [x,y,w] of [[143,298,20],[167,297,24],[197,300,17]])props.push(poly([[x,y+5],[x+3,y],[x+w-4,y],[x+w,y+4],[x+w-3,y+7],[x+1,y+7]],P.iron0),line(x+4,y+1,x+w-5,y+1,P.iron4));
  props.push(r(224,293,16,10,P.wood0),r(225,294,14,7,P.fire1),line(227,294,237,294,P.fire3),line(229,291,239,281,P.iron3,2));
  beam(props,146,227,81,7);
  for(const x of [152,174,196,217])props.push(line(x,234,x,242,P.iron3),e(x-2,239,5,6,P.iron2),e(x-1,240,3,4,C.plaster2));
  props.push(line(153,244,153,267,P.wood4,3),poly([[143,244],[162,244],[164,252],[143,251]],P.iron0),line(145,245,160,245,P.iron4,2),line(174,243,171,262,P.iron3,2),line(177,243,181,262,P.iron2,2),line(171,262,174,267,P.iron4,2),line(181,262,177,267,P.iron3,2),line(195,243,204,266,P.wood4,3),poly([[187,243],[198,239],[207,247],[195,250]],P.iron1),line(188,243,198,241,P.iron4),line(218,244,218,269,P.wood4,2),r(212,244,12,6,P.iron2),line(213,244,222,244,P.iron5));
  barrel(props,253,353,33,42,true);
  props.push(line(263,351,251,333,P.wood0,3),line(263,351,251,333,P.wood4));
  props.push(e(297,374,42,14,P.wood0));
  for(const [x,y] of [[306,373],[321,374],[311,363],[325,362],[317,352]]){
    props.push(poly([[x-5,y-3],[x+4,y-9],[x+13,y-7],[x+15,y],[x+4,y+7],[x-6,y+5]],P.wood0),poly([[x-3,y-3],[x+5,y-7],[x+12,y-5],[x+12,y],[x+3,y+5]],P.wood2),line(x+2,y-4,x+10,y-6,P.wood4),line(x+5,y,x+12,y-3,P.wood1),e(x-6,y-3,13,12,P.wood0),e(x-5,y-2,11,10,P.wood4),e(x-3,y,7,6,P.wood2),e(x-2,y+1,5,4,P.wood5),r(x,y+2,1,2,P.wood2));
  }
  props.push(r(24,328,84,9,P.wood0),r(26,329,80,5,P.wood3),line(27,329,104,329,P.wood5),r(29,337,6,28,P.wood1),r(97,337,6,28,P.wood1));
  for(const [x,y,c] of [[32,312,P.teal1],[48,309,P.fire0],[69,316,P.iron2],[88,309,P.teal2]])props.push(r(x+3,y-3,7,4,P.wood0),poly([[x+2,y],[x+10,y],[x+13,y+5],[x+12,328],[x,328],[x-1,y+5]],P.wood0),poly([[x+3,y+1],[x+9,y+1],[x+11,y+6],[x+10,326],[x+1,326],[x+1,y+5]],c),line(x+3,y+3,x+3,y+9,P.iron4));
  props.push(r(33,357,68,6,P.wood0),line(34,357,99,357,P.wood4));
  for(const [x,y] of [[39,345],[55,345],[73,349]])props.push(r(x,y,13,11,P.wood2),r(x+1,y,11,2,P.wood5),line(x+3,y+5,x+10,y+5,P.wood4));
  crate(props,28,374,35,27);barrel(props,70,365,28,35);
  props.push(poly([[310,424],[329,416],[350,425],[352,448],[345,457],[314,456]],P.wood0),poly([[315,427],[330,421],[345,428],[347,447],[341,451],[317,450]],P.wood3),line(316,431,344,432,P.wood5),line(314,443,348,444,P.wood1));
  plant(props,331,423);
  market.push(e(21,519,118,21,P.wood0),poly([[30,441],[127,441],[128,493],[29,493]],P.wood0));
  for(let x=32;x<125;x+=10)market.push(r(x,447,8,46,P.wood2),line(x+1,448,x+1,489,P.wood3));
  beam(market,27,437,6,65);beam(market,125,437,6,65);beam(market,37,464,80,6);
  for(const [x,h] of [[43,23],[105,28],[119,20]])sword(market,x,438,h);
  market.push(poly([[20,440],[29,415],[121,415],[136,440],[133,449],[21,449]],P.ink));
  for(let i=0;i<7;i++){
    const x=29+i*13,bot=22+i*16;
    market.push(poly([[x,416],[x+12,416],[bot+15,439],[bot,439]],i%2?P.teal1:P.teal2),line(x+1,417,bot+2,437,i%2?P.teal2:P.teal3));
    market.push(poly([[bot,440],[bot+15,440],[bot+14,445],[bot+11,448],[bot+4,448],[bot+1,445]],i%2?P.teal1:P.teal3),line(bot+3,441,bot+12,441,P.teal4));
  }
  market.push(line(30,416,120,416,P.teal4),line(22,439,133,439,P.teal0,2));
  market.push(e(36,488,17,6,P.iron0),poly([[38,483],[48,482],[53,487],[37,488]],P.iron3),line(39,483,46,483,P.iron5),e(109,488,14,5,P.gold0),e(111,486,10,4,P.gold2),line(113,486,118,486,P.gold4));
  for(const [x,y] of [[130,194],[347,372],[144,452]]){
    for(let i=7;i>0;i--)light.push(e(x-i*6,y-i*5,i*12+5,i*10+16,`#ffd085${['00','35','2b','22','19','10','09','04'][i]}`));
    lantern(light,x,y);
  }
  for(let i=9;i>0;i--)light.push(e(285-i*9,250-i*4,i*18+15,i*8+27,`#ffa45a${['00','30','28','21','1b','15','10','0b','07','03'][i]}`));
  light.push(line(252,267,258,267,P.fire3),line(307,267,330,267,C.brick4),line(302,282,326,282,C.warm2),line(272,296,292,296,C.warm),line(286,310,302,310,C.warm));
  anvil(front);front.push(line(191,366,220,366,P.fire2,2),line(194,365,218,365,P.fire4));counter(front);
  for(const [x,y] of [[8,263],[6,322],[12,403],[8,483],[31,554],[80,568],[290,566],[344,542],[372,479],[376,407],[369,333],[376,272],[352,165],[23,138]]){
    garden.push(e(x-8,y-2,25,9,C.moss),poly([[x-12,y+5],[x-9,y-4],[x-2,y-7],[x+9,y-5],[x+16,y+3],[x+10,y+9],[x-6,y+10]],C.earth2),line(x-6,y-5,x+6,y-4,C.moss2));
    plant(garden,x,y);
  }
  for(const [x,y] of [[55,546],[315,541],[359,463],[15,357],[367,303],[39,132],[346,158]])garden.push(r(x,y,3,2,P.gold2),r(x+5,y-3,2,3,P.fire4),r(x+8,y+1,3,2,P.gold1));
  for(const [x,y,w,h,n] of [[6,526,21,13,0],[15,542,28,12,1],[58,552,34,12,2],[286,547,31,13,2],[327,535,29,14,1],[357,515,21,14,0],[2,299,19,16,1],[364,289,19,17,0]])stone(garden,x,y,w,h,n);
  garden.push(poly([[2,586],[0,555],[12,540],[18,551],[20,575],[32,565],[43,568],[36,591]],P.teal0),poly([[350,592],[351,571],[365,556],[374,563],[384,549],[384,592]],P.teal0));
  for(const [x,y] of [[12,562],[26,579],[362,578],[377,569]])plant(garden,x,y);
  return [{name:'01 Dusk sky and layered pine hillside',ops:sky},{name:'02 Laid stone courtyard and front steps',ops:ground},{name:'03 Staggered masonry and oak cutaway',ops:walls},{name:'04 Scalloped slate roof and carved fascia',ops:roof},{name:'05 Mine arch ore cart and rails',ops:mine},{name:'06 Warm brick kiln and chimney',ops:kiln},{name:'07 Quench tools shelves and workshop clutter',ops:props},{name:'08 Teal market canopy and stock',ops:market},{name:'09 Authored lamplight and ember bounce',ops:light},{name:'10 Front anvil 178 368 68 40 and counter 25 493 113 36',ops:front},{name:'11 Moss ferns and foreground silhouette',ops:garden}];
}
