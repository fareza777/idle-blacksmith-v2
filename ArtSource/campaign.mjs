import {P,r,e,line,poly,grid} from './palette.mjs';

// Every entry is an Aseprite draw operation, never an externally rasterized image.
// Coordinates and silhouettes are authored at native resolution; poses are not rig tweens.
const C={night:'#202a3e',sky:'#303e56',mist:'#536879',far:'#354e59',pine:'#203e48',pine2:'#2c5357',leaf:'#45685b',leaf2:'#668570',grass:'#3c514c',grass2:'#516659',earth:'#34373e',path:'#746658',pathLight:'#a28e6c',water:'#24505e',water2:'#347582',water3:'#66a6ac',snow0:'#435a72',snow1:'#648398',snow2:'#98b5bd',snow3:'#c9d9d7',violet0:'#302e49',violet1:'#514467',violet2:'#79618f',violet3:'#ad92b5',violet4:'#e0c7da',green0:'#263e39',green1:'#3e6250',green2:'#63866a',green3:'#96b481',red0:'#442b3a',red1:'#723c50',red2:'#a15d69',red3:'#d39187'};
const L=(name,ops)=>({name,ops});
const seq=(a,pts,c,t=1)=>{for(let i=1;i<pts.length;i++)a.push(line(...pts[i-1],...pts[i],c,t));};
const choose=(i,values)=>values[((i%values.length)+values.length)%values.length];
function gem(a,x,y,w,h,dark,mid,light){
  a.push(poly([[x,y+h],[x-3,y+6],[x+Math.floor(w/2),y],[x+w,y+7],[x+w-2,y+h]],dark),poly([[x+1,y+h-2],[x,y+7],[x+Math.floor(w/2),y+1],[x+Math.floor(w/2)+1,y+h-3]],light),poly([[x+Math.floor(w/2)+1,y+2],[x+w-1,y+7],[x+w-3,y+h-1],[x+Math.floor(w/2)+1,y+h-3]],mid));
}
function pine(a,x,y,h,variant=0,snow=false){
  const w=Math.floor(h*.29),d=snow?C.snow0:variant?C.pine:P.teal0,m=snow?C.snow1:variant?C.pine2:P.teal1,hi=snow?C.snow2:C.leaf;
  a.push(line(x,y-h+5,x,y+4,P.wood1,2));
  for(let j=0;j<3;j++){
    const top=y-h+Math.floor(j*h*.20),span=Math.floor(w*(.55+j*.22)),bottom=top+Math.floor(h*.52);
    a.push(poly([[x,top],[x+span,bottom-3],[x+span-3,bottom-2],[x+span+2,bottom],[x+2,bottom-1],[x-3,bottom+2],[x-span-2,bottom],[x-span+1,bottom-4]],d),poly([[x,top+1],[x-1,bottom-5],[x-span,bottom-2],[x-span+3,bottom-7]],m),line(x-2,top+5,x-span+4,bottom-8,hi));
    if(snow)a.push(poly([[x,top],[x+Math.floor(span*.45),top+Math.floor(h*.22)],[x+1,top+Math.floor(h*.18)],[x-4,top+Math.floor(h*.24)],[x-Math.floor(span*.65),top+Math.floor(h*.25)]],C.snow3));
  }
}
function oak(a,x,y,s=1,amber=false){
  const pt=p=>p.map(([dx,dy])=>[x+Math.round(dx*s),y+Math.round(dy*s)]);
  a.push(poly(pt([[-3,0],[-2,-17],[-10,-24],[-8,-27],[1,-20],[7,-29],[10,-28],[3,-16],[4,0]]),P.wood0),line(x,y-15*s,x,y,P.wood4,Math.max(1,s)));
  const d=amber?'#5c4940':P.teal0,m=amber?'#806444':C.pine2,hi=amber?'#a48752':C.leaf2;
  for(const [cx,cy,z] of [[-8,-23,0],[7,-26,1],[0,-33,2]]){
    const q=p=>pt(p.map(([dx,dy])=>[dx+cx,dy+cy]));
    a.push(poly(q([[-10,0],[-11,-5],[-7,-10],[-2,-12],[5,-11],[9,-7],[11,-1],[7,5],[-2,7],[-8,4]]),d),poly(q([[-9,-4],[-6,-9],[0,-11],[6,-9],[8,-5],[4,-2],[1,-3],[-3,1],[-8,0]]),m),poly(q([[-6,-8],[0,-10],[5,-8],[1,-7],[-1,-4],[-7,-4]]),hi));
    a.push(line(x+Math.round((cx-5)*s),y+Math.round((cy+3)*s),x+Math.round((cx-1)*s),y+Math.round((cy+3)*s),m));
  }
}
function mountain(a,x,y,w,h,snow=true){
  const top=x+Math.floor(w*(w%3===0?.16:-.08)),left=x-Math.floor(w*.5),right=x+Math.floor(w*.5),apex=y-h;
  a.push(poly([[left,y],[left+8,y-17],[top-10,apex+15],[top,apex],[right,y-11],[right+9,y+2]],snow?C.snow0:P.stone0),poly([[left+3,y-1],[top-10,apex+15],[top,apex],[top-1,y-8],[top-11,y-17],[top-19,y-6]],snow?C.snow1:P.stone1),poly([[top,apex+3],[top+4,y-25],[right-2,y-8],[top+1,y-14]],snow?C.far:P.slate));
  if(snow)a.push(poly([[top,apex],[top+10,apex+19],[top+5,apex+17],[top+3,apex+25],[top-3,apex+18],[top-8,apex+24],[top-12,apex+23],[top-9,apex+14]],C.snow3),line(top-5,apex+Math.floor(h*.45),top-12,y-7,C.snow2,2));
  else a.push(line(top-2,apex+8,top-13,y-17,P.stone2));
  a.push(poly([[top-2,apex+Math.floor(h*.3)],[top-8,apex+Math.floor(h*.48)],[top-6,apex+Math.floor(h*.64)],[top-14,y-8],[top-4,y-13],[top+1,y-4],[top+3,y-18]],snow?C.snow0:P.slate));
  for(let j=0;j<3;j++){
    const yy=apex+Math.floor(h*(.56+j*.14)),xx=top-Math.floor(w*(.16+j*.07));
    a.push(line(xx,yy,xx+Math.floor(w*.10),yy-2,snow?C.snow2:P.stone1,2));
  }
}
function rock(a,x,y,w=16,h=9,c=[P.stone0,P.stone1,P.stone3]){
  a.push(poly([[x,y+h],[x-1,y+4],[x+4,y],[x+w-5,y-2],[x+w,y+3],[x+w-1,y+h]],c[0]),poly([[x+1,y+4],[x+5,y],[x+w-5,y-1],[x+w-2,y+3],[x+6,y+5]],c[1]),line(x+5,y,x+w-6,y,c[2]));
}
function brick(a,x,y,w,h,bright=false){
  a.push(poly([[x+1,y],[x+w-2,y],[x+w,y+2],[x+w-1,y+h-1],[x,y+h-1],[x,y+2]],P.iron0),r(x+1,y+1,w-3,h-3,bright?P.iron2:P.stone1),line(x+2,y+1,x+w-4,y+1,bright?P.iron4:P.stone3));
}
function house(a,x,y,s=1){
  const pt=p=>p.map(([dx,dy])=>[x+dx*s,y+dy*s]);
  a.push(poly(pt([[-11,0],[10,0],[10,-17],[-2,-22],[-11,-16]]),P.wood0),poly(pt([[-9,-1],[8,-1],[8,-15],[-2,-20],[-9,-15]]),P.wood3),poly(pt([[-14,-15],[-3,-26],[12,-16],[10,-12],[-3,-20],[-11,-12]]),P.teal0),poly(pt([[-12,-16],[-3,-24],[9,-16],[7,-14],[-3,-21]]),P.teal3));
  a.push(r(x+2*s,y-11*s,4*s,5*s,P.fire4),line(x-10*s,y-1*s,x+9*s,y-1*s,P.wood5,s),r(x-5*s,y-9*s,4*s,9*s,P.wood1),line(x-3*s,y-22*s,x-3*s,y-20*s,P.teal5,s));
}
function mapBridge(a,x,y){
  a.push(line(x-13,y+8,x+15,y-9,P.wood0,12),line(x-13,y+6,x+15,y-11,P.wood3,9));
  for(let i=0;i<7;i++)a.push(line(x-13+i*4,y+3-i*2,x-9+i*4,y+10-i*2,P.wood5));
  seq(a,[[x-16,y+3],[x-2,y-6],[x+13,y-13]],P.wood0,3);seq(a,[[x-15,y+2],[x-2,y-7],[x+13,y-14]],P.wood6);
  for(const [dx,dy] of [[-15,4],[0,-5],[14,-13]])a.push(line(x+dx,y+dy,x+dx,y+dy-6,P.wood4,2));
}
export function worldmap(){
  const paper=[],land=[],water=[],flora=[],routes=[],landmarks=[],edge=[];
  paper.push(r(0,0,384,480,P.wood0),poly([[6,5],[88,7],[145,4],[253,7],[379,4],[377,98],[380,219],[377,332],[380,475],[290,473],[198,477],[98,474],[4,477],[7,391],[4,271],[7,158]],P.wood4),r(10,10,364,460,P.wood6),r(14,14,356,452,C.night));
  land.push(poly([[16,30],[67,23],[111,37],[171,18],[229,23],[269,44],[327,40],[369,62],[369,463],[16,463]],C.far),poly([[16,211],[64,186],[98,211],[156,229],[222,212],[267,246],[367,235],[369,463],[16,463]],C.grass),poly([[16,334],[60,315],[106,337],[159,346],[196,388],[154,460],[16,463]],'#46574a'),poly([[205,246],[269,228],[351,257],[369,316],[348,382],[284,388],[233,357],[190,313]],'#2b514f'),poly([[185,106],[233,95],[268,105],[309,88],[368,119],[368,249],[338,265],[306,228],[252,240],[226,210]],'#493c42'),poly([[16,93],[52,68],[109,85],[152,123],[193,142],[183,202],[157,250],[99,267],[45,240],[16,219]],'#506671'),poly([[110,25],[163,17],[219,23],[268,58],[255,102],[195,128],[157,110],[122,78]],'#3d465c'));
  for(let i=0;i<200;i++){
    const x=20+(i*67)%342,y=32+(i*43)%422;
    land.push(line(x,y,x+3+i%5,y,choose(i,[C.far,'#415851','#42515b','#526557'])));
  }
  // Low-contrast contour terraces give the valley a traversable topography.
  for(const [x,y,w,h] of [[30,52,55,17],[69,58,44,16],[276,63,67,24],[304,86,42,19],[181,145,34,18],[175,169,38,14],[38,276,49,14],[108,289,43,17],[185,386,34,18],[282,400,53,17],[310,428,37,18],[147,449,65,13],[169,329,34,15],[313,281,35,16]]){
    land.push(poly([[x,y],[x+7,y-h+4],[x+Math.floor(w*.5),y-h],[x+w-7,y-h+5],[x+w,y],[x+w-10,y+4],[x+9,y+3]],'#3a5255'),poly([[x+3,y-1],[x+9,y-h+5],[x+Math.floor(w*.5),y-h+2],[x+w-10,y-h+6],[x+w-2,y-2],[x+w-12,y],[x+8,y]],'#486063'));
    seq(land,[[x+8,y-h+5],[x+Math.floor(w*.5),y-h+2],[x+w-12,y-h+6]],'#5a7270',2);
  }
  water.push(poly([[115,214],[129,220],[146,249],[174,260],[187,283],[180,313],[162,337],[171,353],[196,364],[213,388],[244,398],[272,418],[275,448],[290,466],[262,466],[256,440],[241,421],[204,408],[181,380],[153,372],[139,350],[145,326],[166,300],[170,284],[159,274],[130,261],[119,235]],P.teal0),poly([[120,220],[129,229],[140,255],[168,266],[179,283],[173,310],[153,338],[158,355],[188,372],[206,394],[243,407],[265,425],[265,448],[278,465],[268,465],[259,434],[239,416],[201,402],[184,380],[153,364],[146,348],[150,333],[169,305],[173,282],[160,271],[135,261]],C.water2));
  seq(water,[[123,229],[135,256],[163,268],[174,283],[168,308],[148,340],[151,353],[172,369]],C.water3,2);
  water.push(poly([[272,304],[298,294],[321,302],[335,319],[327,339],[296,346],[276,336],[255,342],[237,331],[244,317]],C.water),poly([[283,307],[299,299],[317,306],[326,319],[319,333],[294,340],[276,330],[251,332],[249,322]],C.water2));
  seq(water,[[262,324],[225,320],[197,300],[178,293]],C.water2,7);
  for(let i=0;i<26;i++){const x=254+i*17%67,y=309+i*7%28;water.push(line(x,y,x+3+i%4,y,i%4?C.water:C.water3));}
  for(const [x,y,w,h] of [[30,128,49,67],[70,143,68,83],[115,143,48,55],[25,196,39,46],[63,193,65,76],[111,211,59,74],[139,182,38,49],[42,252,36,39],[71,249,46,47],[140,67,48,51],[191,45,53,34],[235,84,49,50]])mountain(flora,x,y,w,h,true);
  for(let row=0;row<7;row++)for(let col=0;col<9;col++){
    const x=25+col*14+(row%2)*6,y=284+row*20+(col*7)%11;
    if((x-77)**2+(y-380)**2<1400||x>128&&y>326)continue;
    pine(flora,x,y,21+(col*7+row*3)%16,col%2);if((row+col)%4===0)oak(flora,x+3,y+4,1,true);
  }
  for(let i=0;i<28;i++){const x=23+i*47%336,y=419+i*19%35;if(x<126||x>226&&x<283)continue;pine(flora,x,y,22+i%13,i%2);}
  for(const [x,y] of [[216,281],[235,268],[272,272],[324,269],[346,290],[351,352],[313,369],[234,355],[218,337]]){
    oak(flora,x,y,1);flora.push(line(x-5,y-3,x-10,y-13,C.leaf2,2),line(x+4,y-2,x+8,y-16,C.leaf2,2));
  }
  for(const [x,y,w,h] of [[236,165,34,43],[323,139,45,46],[349,190,37,59],[287,236,46,47],[216,207,31,44],[341,245,34,28]])mountain(flora,x,y,w,h,false);
  seq(flora,[[302,151],[317,168],[310,185],[329,210],[334,235]],P.fire0,12);seq(flora,[[302,151],[317,168],[310,185],[329,210],[334,235]],P.fire2,6);seq(flora,[[304,155],[315,170],[312,185],[328,207]],P.fire4,2);
  const path=[[[62,444],[67,419],[77,383]],[[77,383],[113,392],[152,389],[183,352],[214,344],[261,322]],[[261,322],[225,289],[185,276],[149,260],[118,241],[94,223]],[[94,223],[155,231],[191,215],[216,181],[247,174],[275,155]],[[275,155],[263,130],[225,116],[219,93],[188,63]]];
  for(const pts of path){seq(routes,pts,P.wood0,7);seq(routes,pts,C.path,5);seq(routes,pts,C.pathLight,2);}
  seq(routes,[[183,352],[194,332],[205,313],[221,309],[244,324]],P.wood0,5);seq(routes,[[183,352],[194,332],[205,313],[221,309],[244,324]],C.path,3);
  mapBridge(routes,173,367);mapBridge(routes,147,260);mapBridge(routes,205,313);
  for(const [x,y] of [[116,389],[211,180],[230,114],[69,412]])routes.push(r(x,y-6,2,7,P.wood1),r(x-2,y-7,6,3,P.gold2));
  // Emberwood's ancient lantern oak, rooted in the route at (77,383).
  landmarks.push(e(52,375,51,15,P.teal0),poly([[68,382],[72,359],[68,338],[77,319],[85,337],[83,362],[91,382],[84,386],[76,380]],P.wood0),poly([[73,380],[76,358],[74,339],[79,330],[81,356],[79,376],[84,383]],P.wood4),line(76,343,66,332,P.wood3,5),line(80,350,94,336,P.wood3,4));
  oak(landmarks,77,365,2,true);landmarks.push(line(70,363,68,371,P.wood0),r(65,370,6,9,P.wood0),r(66,371,4,6,P.fire4),r(67,372,2,4,P.fire5),line(83,365,91,359,P.wood5));
  for(const [x,y] of [[57,383],[86,387],[98,375]])landmarks.push(r(x,y,3,2,P.fire4),r(x+1,y-1,1,1,P.fire5));
  // Whispering Marsh sunken gate and luminous water, not an icon marker.
  landmarks.push(e(239,320,47,13,C.water),r(246,293,10,32,P.teal0),r(247,294,7,28,P.iron2),r(267,286,11,35,P.teal0),r(269,288,7,31,P.iron2),poly([[245,295],[247,284],[258,278],[276,281],[280,290],[273,295],[267,286],[258,285],[252,293]],P.iron1),line(251,286,258,281,P.iron4,2),line(259,281,274,283,P.iron3,2),line(250,297,250,317,P.iron3),r(270,294,3,12,P.teal4),line(250,303,257,309,C.leaf2,2));
  for(const [x,y] of [[245,327],[274,325],[259,321]])landmarks.push(e(x,y,7,2,P.teal3),r(x+3,y-1,2,1,P.teal5));
  // Frostpeak alpine keep, foundation anchored at (94,223).
  landmarks.push(poly([[68,224],[77,204],[103,201],[116,221],[99,230],[78,229]],C.snow0),poly([[72,221],[80,208],[100,206],[111,220],[96,225]],C.snow2),r(81,194,25,28,P.iron0),r(83,194,21,26,P.iron2),r(87,178,14,32,P.iron0),r(89,180,10,25,C.snow1),poly([[82,182],[94,165],[105,182]],P.teal0),poly([[85,180],[94,167],[102,180]],C.snow3),r(91,187,4,6,P.fire4),r(90,210,8,12,P.iron0),line(84,195,103,195,C.snow3),line(94,168,94,157,P.wood4),poly([[95,158],[110,160],[104,165],[95,163]],P.teal3));
  for(const x of [78,102])landmarks.push(r(x,201,6,19,P.iron1),r(x-1,197,8,5,C.snow3));
  // Ashen Caldera volcanic caldera and its ember-lit foundry approach.
  landmarks.push(poly([[240,159],[251,137],[260,116],[279,110],[291,123],[311,157],[294,167],[263,171]],P.ink),poly([[244,157],[261,125],[272,120],[266,153],[253,163]],P.stone1),poly([[276,120],[287,124],[307,155],[285,155]],P.stone0),e(258,114,34,15,P.fire0),e(261,116,27,9,P.fire2),e(266,117,16,5,P.fire4));
  seq(landmarks,[[281,123],[277,137],[282,145],[275,158]],P.fire1,8);seq(landmarks,[[281,123],[277,137],[282,145],[275,158]],P.fire3,4);seq(landmarks,[[280,124],[277,136],[280,143]],P.fire5);
  landmarks.push(poly([[269,108],[263,97],[268,88],[263,78],[275,75],[282,87],[277,95],[284,108]],'#695257'),poly([[270,100],[272,88],[268,81],[277,86],[275,96]],'#8a6871'));
  // Starfall Citadel: moon, floating terraces, buttresses and five spires.
  landmarks.push(e(173,24,30,30,C.violet2),e(178,22,27,26,C.violet0),poly([[149,72],[163,57],[211,55],[230,71],[215,85],[188,101],[162,85]],C.violet0),poly([[153,72],[170,64],[211,62],[225,72],[203,78],[171,80]],C.violet2),poly([[171,81],[184,97],[190,80],[197,91],[207,78]],C.violet1));
  for(const [x,y,h] of [[161,53,19],[174,41,28],[186,33,36],[200,43,27],[213,54,19]]){
    landmarks.push(r(x,y,8,h,P.iron0),r(x+1,y+1,5,h-2,C.violet3),line(x+1,y+3,x+1,y+h-2,P.iron5),poly([[x-3,y+2],[x+4,y-10],[x+11,y+2]],P.teal0),poly([[x-1,y],[x+4,y-8],[x+8,y]],P.teal3),r(x+3,y+6,2,7,P.teal0),r(x+3,y+7,1,4,P.teal5));
  }
  landmarks.push(r(169,64,46,9,C.violet1),line(170,64,214,64,P.iron5),poly([[182,75],[182,64],[188,59],[194,64],[194,75]],P.iron0),poly([[185,74],[185,65],[188,62],[191,65],[191,74]],P.teal3),line(188,65,188,73,P.teal5));
  for(const [x,y] of [[139,44],[226,37],[211,18],[156,25]])landmarks.push(line(x-2,y,x+2,y,P.iron4),line(x,y-2,x,y+2,P.iron5));
  // EmberOak: an actual little settlement at the southwest entrance.
  landmarks.push(poly([[22,451],[35,426],[69,417],[102,434],[106,451],[74,460],[39,460]],P.wood1),poly([[25,449],[36,430],[67,423],[98,436],[100,449],[71,455],[41,454]],C.path));
  for(const [x,y] of [[38,441],[61,433],[88,444],[62,454]])house(landmarks,x,y);
  oak(landmarks,24,448,1,true);landmarks.push(r(92,422,5,13,P.stone0),r(90,421,9,3,P.stone3),r(88,418,6,2,C.mist),r(85,414,7,2,C.mist));
  // Secondary discoveries are smaller and dimmer than the five principal landmarks.
  for(const [x,y] of [[128,410],[296,394],[326,96],[175,196]]){
    landmarks.push(poly([[x-6,y+3],[x-7,y-8],[x-4,y-16],[x+2,y-17],[x+7,y-10],[x+6,y+3]],P.iron0),poly([[x-4,y+1],[x-5,y-8],[x-2,y-14],[x+2,y-14],[x+5,y-9],[x+4,y+1]],P.iron2),line(x-2,y-12,x+1,y-13,P.iron3),r(x-1,y-8,2,6,P.teal0),r(x-1,y-7,1,3,P.teal3),line(x-3,y-3,x+4,y-3,P.iron1));
  }
  for(const [x,y] of [[105,365],[42,291],[292,272],[337,356],[296,75],[132,448]]){
    rock(landmarks,x,y,11,6,[P.iron0,P.iron1,P.iron3]);rock(landmarks,x+8,y+3,8,4,[P.iron0,P.iron1,P.iron2]);
  }
  // Fissures, snow shelves and masonry joints share the workshop's material grammar.
  for(const [x,y] of [[62,168],[93,130],[117,180],[48,229],[29,108],[243,75]]){
    seq(landmarks,[[x,y],[x-5,y+6],[x-3,y+12],[x-10,y+21]],C.snow0,2);seq(landmarks,[[x-1,y+1],[x-6,y+6],[x-4,y+12]],C.snow2);
  }
  for(const [x,y] of [[83,199],[88,205],[99,199],[103,216],[249,296],[270,301],[249,312],[270,313],[174,64],[203,65]]){
    landmarks.push(line(x,y,x+4,y,P.iron1),line(x+1,y+1,x+5,y+1,P.iron3));
  }
  for(const [x,y] of [[162,61],[175,55],[187,46],[201,56],[213,61]]){
    landmarks.push(line(x,y,x+5,y,C.violet1),line(x+1,y+6,x+5,y+6,C.violet1),r(x+4,y+1,1,4,C.violet2));
  }
  for(let j=0;j<4;j++)landmarks.push(line(181-j,77+j*3,194+j,77+j*3,P.iron4),line(181-j,78+j*3,194+j,78+j*3,C.violet1));
  for(const [x,y] of [[247,176],[296,205],[344,167]]){
    seq(landmarks,[[x,y],[x+4,y-9],[x+8,y-13]],P.fire0,4);seq(landmarks,[[x,y],[x+4,y-9],[x+8,y-13]],P.fire2,2);
  }
  // A worn chart edge is physical parchment; there is intentionally no tiny type.
  edge.push(r(0,14,10,452,P.wood4),r(374,14,10,452,P.wood4),r(10,14,4,452,P.wood6),r(370,14,4,452,P.wood6),r(0,14,4,452,P.wood0),r(380,14,4,452,P.wood0));
  edge.push(line(10,10,373,10,P.gold1),line(10,469,373,469,P.gold1),line(10,10,10,469,P.gold1),line(373,10,373,469,P.gold1));
  for(let i=0;i<32;i++){const y=20+i*14;edge.push(line(5,y,8,y+4,P.wood2,2),line(376,y+5,379,y+2,P.wood2,2));}
  for(const [x,y] of [[19,19],[364,19],[19,460],[364,460]])edge.push(poly([[x,y-4],[x+3,y],[x,y+4],[x-3,y]],P.gold2));
  return [L('Worn amber parchment',paper),L('Nocturnal valley landforms',land),L('River watershed and marsh pools',water),L('Hand-shaped forest and mountain ranges',flora),L('Explorable roads and timber bridges',routes),L('Five region landmarks and EmberOak settlement',landmarks),L('Chart edge without labels',edge)];
}

function sky(a,colors){
  a.push(r(0,0,320,180,colors[4]));
  for(let i=0;i<5;i++)a.push(r(0,i*22,320,22,colors[i]));
  for(const [x,y,w] of [[22,21,48],[112,36,63],[210,19,77],[266,44,53]])a.push(poly([[x,y],[x+9,y-3],[x+24,y-2],[x+30,y-4],[x+w,y],[x+w-8,y+3],[x+12,y+2]],colors[2]));
}
function floor(a,colors,kind=0){
  a.push(poly([[0,135],[40,138],[74,134],[111,138],[157,135],[211,137],[260,134],[320,137],[320,180],[0,180]],colors[0]),poly([[0,144],[47,147],[94,142],[150,146],[211,142],[272,146],[320,143],[320,180],[0,180]],colors[1]));
  for(let i=0;i<66;i++){
    const x=(i*47)%318,y=148+(i*13)%31,w=3+i%10;
    a.push(line(x,y,Math.min(319,x+w),y,colors[2+i%2]));
    if(i%3===0)a.push(line(x+1,y+2,Math.min(319,x+4),y+2,colors[0]));
  }
  if(kind===1)for(const [x,y,w] of [[6,165,29],[71,154,26],[128,170,36],[224,160,28],[284,170,25]])rock(a,x,y,w,5,[P.teal0,P.iron2,P.iron3]);
}
function trunk(a,x,y,h,w=19){
  a.push(poly([[x-w,y],[x-w+4,y-9],[x-5,y-h],[x+9,y-h],[x+w-5,y-7],[x+w,y],[x+3,y-3],[x-5,y+3]],P.wood0),poly([[x-w+3,y-3],[x-5,y-h],[x+1,y-h],[x+2,y-10],[x-4,y-2]],P.wood2));
  seq(a,[[x-3,y-h+3],[x-5,y-65],[x-1,y-44],[x-5,y-13]],P.wood4,2);seq(a,[[x+7,y-h+4],[x+4,y-51],[x+10,y-25],[x+8,y-5]],P.wood1,3);
}
function reeds(a,x,y){
  for(const [dx,dy] of [[-7,-13],[-3,-22],[2,-18],[6,-11]])a.push(line(x,y,x+dx,y+dy,P.teal2),line(x+dx,y+dy,x+dx,y+dy-4,P.wood5,2));
}
function pillar(a,x,y,h,ruined=false,violet=false){
  const dark=violet?C.violet0:P.iron0,mid=violet?C.violet1:P.iron1,light=violet?C.violet3:P.iron3;
  a.push(r(x,y-h,15,h,dark),r(x+2,y-h+2,10,h-3,mid),line(x+3,y-h+3,x+3,y-3,light),r(x-3,y-3,21,6,dark),line(x-2,y-3,x+16,y-3,light));
  for(let yy=y-h+13;yy<y-6;yy+=13)a.push(line(x+2,yy,x+12,yy,dark),line(x+2,yy+1,x+10,yy+1,mid));
  a.push(poly(ruined?[[x-2,y-h+4],[x+1,y-h-4],[x+7,y-h],[x+10,y-h-7],[x+16,y-h-2],[x+16,y-h+5]]:[[x-3,y-h],[x-3,y-h-5],[x+18,y-h-5],[x+18,y-h]],mid),line(x-1,y-h+3,x+14,y-h+3,light));
}
function campfire(a,x,y){
  a.push(e(x-15,y-3,31,8,P.wood0),line(x-8,y,x+9,y+3,P.wood3,4),line(x+8,y-1,x-8,y+3,P.wood4,3),poly([[x-6,y],[x-8,y-6],[x-3,y-15],[x-2,y-8],[x+3,y-19],[x+4,y-9],[x+8,y-5],[x+5,y+1]],P.fire2),poly([[x-4,y],[x-4,y-6],[x,y-12],[x+1,y-6],[x+4,y-3],[x+2,y+1]],P.fire4),poly([[x-1,y],[x,y-6],[x+2,y-1]],P.fire5));
  for(const [dx,dy] of [[-12,1],[10,1],[-8,4],[5,5]])rock(a,x+dx,y+dy,6,3,[P.stone0,P.stone2,P.stone3]);
}
export function battlefields(){
  return [0,1,2,3,4].map(region=>{
    const back=[],horizon=[],props=[],ground=[],front=[];
    if(region===0){
      sky(back,[C.night,C.sky,'#455766','#597079','#66777a']);
      back.push(e(236,16,24,24,'#c3c5ab'),e(243,12,23,24,'#455766'),poly([[0,74],[40,42],[81,64],[133,41],[183,65],[221,51],[277,67],[320,50],[320,137],[0,137]],C.far));
      for(let i=0;i<17;i++)pine(horizon,i*21-7,119+(i*7)%15,53+(i*11)%27,i%2);
      horizon.push(poly([[0,122],[47,116],[104,126],[164,119],[224,124],[278,114],[320,120],[320,142],[0,145]],C.grass));
      trunk(props,9,142,149,18);trunk(props,308,145,151,20);
      for(const pts of [[[9,53],[30,30],[58,13]],[[308,58],[290,31],[270,14]]]){seq(props,pts,P.wood0,9);seq(props,pts,P.wood3,3);}
      for(const [x,y,s] of [[11,48,2],[53,32,2],[278,30,2],[315,54,2]]){const canopy=[];oak(canopy,x,y,s);props.push(...canopy.slice(2));}
      props.push(poly([[124,126],[141,95],[174,124]],P.wood0),poly([[127,123],[141,98],[169,122]],P.wood4),poly([[141,100],[151,123],[132,123]],P.wood1),line(141,95,160,126,P.wood6),line(123,127,171,127,P.wood5));
      rock(props,103,130,17,9);rock(props,188,129,22,8);campfire(props,181,129);
      props.push(line(109,127,109,111,P.wood3,2),r(106,112,7,10,P.wood0),r(108,114,3,6,P.fire4));
      floor(ground,['#4d574e','#6b6455','#7a7360','#595b50']);
      for(const [x,y] of [[4,169],[16,178],[302,166],[286,179]])reeds(front,x,y);
      front.push(poly([[0,180],[0,174],[23,170],[44,176],[65,180]],P.teal0),poly([[254,180],[280,174],[299,175],[320,168],[320,180]],P.teal0));
      for(const [x,y] of [[19,159],[297,151],[116,140],[205,143]])front.push(r(x,y,2,2,P.fire4),r(x+2,y-2,1,1,P.fire5));
    }else if(region===1){
      sky(back,['#1d2f3f','#294453','#395b65','#507579','#668889']);
      for(let i=0;i<13;i++)pine(horizon,i*29-8,122+(i*5)%14,61+(i*13)%23,1);
      horizon.push(r(0,124,320,30,C.water),line(11,128,94,128,C.water2),line(191,127,293,127,C.water2));
      for(const [x,y,h] of [[26,131,59],[91,126,78],[172,129,72],[285,134,51]])pillar(props,x,y,h,true);
      props.push(poly([[91,55],[106,38],[131,29],[157,38],[184,57],[178,67],[156,47],[131,39],[111,49],[105,60]],P.iron0),poly([[97,53],[110,40],[131,33],[155,42],[179,59],[175,62],[153,47],[131,38],[112,46],[102,57]],P.iron2),line(112,41,130,35,P.iron4),line(135,35,154,43,P.iron3));
      for(const [x,y] of [[95,59],[177,72],[29,95]]){
        seq(props,[[x,y],[x+7,y+12],[x+2,y+21],[x+9,y+32]],P.teal2,3);
        for(let j=0;j<4;j++)props.push(poly([[x+3,y+5+j*7],[x-4,y+j*7],[x-7,y+3+j*7],[x+2,y+7+j*7]],C.leaf));
      }
      props.push(r(130,105,26,22,P.iron0),r(132,107,22,18,P.iron1),poly([[143,110],[149,117],[143,123],[137,117]],P.teal3),line(140,117,146,117,P.teal5));
      for(const [x,y] of [[16,142],[65,137],[205,138],[309,141]])reeds(props,x,y);
      floor(ground,['#365a59','#3c6768','#4d7b7b','#2e5359'],1);
      for(const [x,y] of [[8,169],[42,179],[290,174],[309,157]]){reeds(front,x,y);front.push(e(x-6,y-2,10,3,P.teal2),r(x-3,y-4,3,2,C.violet3));}
      front.push(e(179,157,13,4,P.teal2),e(187,174,18,4,P.teal1),r(184,154,4,3,C.violet3));
    }else if(region===2){
      sky(back,['#28384f','#384e68','#526e88','#7894a5','#96aeba']);
      back.push(e(232,17,25,25,C.snow3));
      for(const [x,y,w,h] of [[4,101,110,80],[70,113,118,107],[151,104,84,70],[216,119,105,96],[293,108,89,72]])mountain(horizon,x,y,w,h,true);
      horizon.push(poly([[0,117],[52,108],[111,122],[153,110],[210,117],[265,108],[320,119],[320,145],[0,145]],C.snow1));
      for(const [x,y,h] of [[9,146,89],[40,126,54],[290,130,65],[319,146,99]])pine(props,x,y,h,0,true);
      props.push(poly([[102,135],[106,121],[124,112],[153,116],[173,128],[168,137]],C.snow0));
      pillar(props,128,129,28,true);pillar(props,178,133,46,true);
      for(const [x,y,w,h] of [[108,120,16,31],[161,125,9,22],[194,126,12,33]])gem(props,x,y-h,w,h,C.snow0,C.snow1,C.snow3);
      props.push(line(152,119,157,113,P.wood3,2),line(157,113,181,117,P.wood5,2),r(151,119,7,11,P.iron0),r(153,121,3,7,P.fire4));
      floor(ground,[C.snow0,C.snow1,C.snow2,'#7395a6']);
      ground.push(poly([[0,141],[52,142],[83,138],[128,142],[177,137],[224,141],[274,138],[320,142],[320,146],[273,144],[222,146],[176,143],[127,147],[84,143],[50,147],[0,146]],C.snow3));
      for(const [x,y,w] of [[3,174,33],[273,169,30],[157,174,27]])rock(front,x,y,w,8,[C.snow0,C.snow2,C.snow3]);
      for(const [x,y] of [[10,35],[49,67],[88,29],[139,60],[179,20],[207,79],[271,51],[302,19]])front.push(r(x,y,2,2,C.snow2));
    }else if(region===3){
      sky(back,['#302937','#47313d','#664047','#89504c','#ab6552']);
      back.push(e(249,32,31,31,'#c68265'),poly([[0,96],[42,60],[70,89],[101,55],[144,98],[184,66],[226,96],[274,78],[320,98],[320,147],[0,147]],'#4e3943'));
      for(const [x,y,w,h] of [[9,135,69,93],[289,136,84,98],[205,109,47,54]])mountain(horizon,x,y,w,h,false);
      horizon.push(poly([[0,128],[54,119],[89,132],[136,123],[182,129],[239,115],[320,125],[320,150],[0,150]],'#39313d'));
      seq(horizon,[[235,104],[219,115],[225,128],[182,136]],P.fire1,10);seq(horizon,[[235,104],[219,115],[225,128],[182,136]],P.fire3,4);
      for(const [x,y,h] of [[107,130,52],[192,132,69]])pillar(props,x,y,h,true);
      props.push(poly([[104,79],[113,61],[142,52],[172,59],[208,66],[201,76],[172,70],[143,62],[119,70],[117,83]],P.ink),line(120,65,142,57,P.fire1,3),line(143,57,171,64,P.stone2,2));
      for(const [x,y] of [[14,143],[292,145],[142,129]])rock(props,x,y,21,12,[P.ink,P.stone0,P.stone2]);
      props.push(poly([[152,133],[148,126],[151,114],[157,110],[163,117],[163,131]],P.ink),r(154,117,3,10,P.fire3),r(155,118,1,7,P.fire5));
      floor(ground,['#302a36','#403640','#57444a','#352e3a']);
      for(const pts of [[[0,157],[24,164],[40,159],[62,168],[55,180]],[[219,180],[231,165],[257,163],[274,152],[295,159],[319,156]]]){seq(front,pts,P.fire0,10);seq(front,pts,P.fire2,5);seq(front,pts,P.fire4,2);}
      for(const [x,y,w] of [[4,172,28],[285,171,29],[101,169,19]])rock(front,x,y,w,10,[P.ink,P.stone0,P.stone1]);
      for(const [x,y] of [[27,51],[73,118],[115,37],[159,92],[213,44],[279,104],[307,78]])front.push(r(x,y,1,2,P.fire3),r(x+3,y-6,2,1,P.fire4));
    }else{
      sky(back,['#22283f','#33334f','#47415f','#605673','#79708a']);
      back.push(e(147,16,41,41,C.violet3),e(154,12,38,37,'#33334f'));
      for(const [x,y] of [[20,15],[41,46],[75,24],[120,13],[214,23],[241,47],[273,13],[302,36]])back.push(line(x-1,y,x+1,y,P.iron4),line(x,y-1,x,y+1,P.iron5));
      for(const [x,h] of [[20,47],[48,63],[245,54],[278,70]]){
        horizon.push(r(x,116-h,15,h,C.violet0),poly([[x-3,116-h],[x+7,97-h],[x+18,116-h]],C.violet1),r(x+6,125-h,3,12,P.teal2));
      }
      horizon.push(r(0,120,320,22,C.violet0),r(0,121,320,2,C.violet2));
      for(let x=7;x<320;x+=23)horizon.push(r(x,112,15,10,C.violet1),line(x+1,112,x+13,112,C.violet3));
      for(const x of [13,106,193,292])pillar(props,x,140,x===106||x===193?96:139,false,true);
      props.push(poly([[105,51],[115,27],[150,8],[160,6],[173,10],[199,29],[212,51],[202,57],[192,36],[171,20],[160,16],[149,20],[123,37],[115,56]],C.violet0),poly([[110,50],[120,29],[150,12],[160,9],[172,14],[195,31],[207,51],[203,52],[191,35],[171,18],[160,13],[149,17],[124,34],[114,52]],C.violet2));
      seq(props,[[116,45],[125,31],[150,16],[160,12],[171,16],[192,33],[201,48]],C.violet3);
      for(const x of [111,199]){props.push(r(x,70,6,30,P.teal0),line(x+3,72,x+3,95,P.teal3),poly([[x+3,72],[x+6,78],[x+3,84],[x,78]],P.teal5));}
      props.push(poly([[130,137],[141,128],[181,128],[193,137]],C.violet1),line(140,130,182,130,P.iron3),line(132,136,192,136,C.violet3));
      floor(ground,[C.violet0,'#544e66','#726b81','#46425a']);
      for(let row=0;row<3;row++)for(let col=0;col<8;col++){
        const x=col*44-(row%2)*20,y=145+row*13;
        ground.push(line(Math.max(0,x),y,Math.min(319,x+39),y,C.violet2),line(Math.max(0,x),y,Math.max(0,x-3),Math.min(179,y+11),C.violet0));
      }
      front.push(poly([[142,156],[160,149],[179,155],[160,164]],P.iron2),poly([[147,156],[160,152],[173,156],[160,161]],P.teal2),line(153,156,167,156,P.teal4));
      for(const [x,y] of [[6,173],[300,175]])gem(front,x,y-25,12,25,C.violet0,C.violet2,P.teal4);
    }
    if(region===0){
      props.push(poly([[202,135],[204,127],[224,123],[244,125],[242,133],[219,138]],P.wood0),poly([[205,133],[207,128],[226,125],[240,126],[238,131],[220,135]],P.wood3),line(210,130,231,128,P.wood5),line(215,134,231,132,P.wood1),e(201,127,10,10,P.wood4),e(203,129,6,6,P.wood1),e(204,130,4,4,P.wood5));
      for(const [x,y] of [[17,146],[299,150],[31,133],[285,132]]){
        for(const [dx,dy] of [[-9,-6],[-5,-12],[1,-15],[7,-10]]){
          props.push(line(x,y,x+dx,y+dy,P.teal1,2),poly([[x+dx,y+dy],[x+dx-5,y+dy-3],[x+dx-6,y+dy],[x+dx-1,y+dy+3]],C.leaf),line(x+dx-3,y+dy,x+dx,y+dy+1,C.leaf2));
        }
      }
      for(const [x,y] of [[119,130],[191,138],[25,163]])front.push(line(x,y,x,y-5,P.wood6,2),e(x-4,y-8,9,5,P.fire1),line(x-2,y-7,x+1,y-7,P.fire3));
    }else if(region===1){
      for(const [x,y] of [[5,92],[301,78]]){
        const foliage=[];oak(foliage,x,y,2);props.push(...foliage.slice(2));
        for(let j=0;j<5;j++)seq(props,[[x-15+j*7,y-35],[x-17+j*7,y-19],[x-13+j*7,y-7]],P.teal1,2);
      }
      for(const [x,y] of [[105,114],[159,118],[182,125]])rock(props,x,y,17,9,[P.iron0,P.iron2,P.iron3]);
      for(const [x,y] of [[49,157],[97,173],[258,170],[281,148]]){
        front.push(poly([[x-7,y],[x-4,y-3],[x+5,y-2],[x+8,y],[x+2,y+3],[x-6,y+2]],P.teal1),line(x-4,y-1,x+3,y-1,P.teal3),line(x,y,x+5,y+2,C.water));
      }
    }else if(region===2){
      for(const [x,y,w] of [[64,110,29],[213,121,35],[243,134,29]]){
        props.push(poly([[x,y],[x+7,y-5],[x+w-3,y-4],[x+w,y],[x+w-5,y+3],[x+3,y+2]],C.snow2),line(x+7,y-4,x+w-5,y-4,C.snow3));
        seq(props,[[x+9,y+1],[x+12,y+5],[x+10,y+8]],C.snow0,2);
      }
      for(const [x,y] of [[22,144],[300,147]]){
        seq(props,[[x,y],[x+2,y-14],[x-2,y-21],[x-7,y-25]],P.wood1,3);seq(props,[[x+2,y-13],[x+9,y-20],[x+11,y-27]],P.wood1,2);
        props.push(line(x-7,y-26,x-2,y-22,C.snow3,2),line(x+7,y-19,x+11,y-25,C.snow3,2));
      }
    }else if(region===3){
      for(const [x,y,w,h] of [[0,142,28,20],[282,137,29,20],[94,133,17,12],[168,134,20,12]]){
        rock(props,x,y-h,w,h,[P.ink,P.stone0,P.stone2]);seq(props,[[x+4,y-3],[x+9,y-8],[x+8,y-14]],P.fire0,3);seq(props,[[x+4,y-3],[x+9,y-8]],P.fire2);
      }
      for(const [x,y] of [[13,152],[83,165],[179,158],[297,147]])seq(front,[[x,y],[x+9,y+2],[x+14,y+7],[x+23,y+8]],P.ink,2);
      for(const [x,y] of [[130,124],[172,127]]){props.push(r(x,y,10,7,P.ink),line(x+1,y,x+8,y,P.stone2),r(x+3,y+3,4,2,P.fire0));}
    }else{
      for(const [x,y] of [[124,45],[140,27],[177,28],[192,43]]){
        props.push(line(x,y,x+4,y+4,C.violet0,2),line(x+1,y,x+5,y+3,C.violet3));
      }
      for(const x of [38,267]){
        props.push(line(x,53,x+15,53,P.gold1,2),poly([[x+1,55],[x+14,55],[x+14,89],[x+7,96],[x+1,89]],P.teal0),poly([[x+3,57],[x+11,57],[x+11,87],[x+7,92],[x+3,87]],P.teal1),line(x+4,58,x+4,85,P.teal3),poly([[x+7,64],[x+10,71],[x+7,79],[x+4,71]],P.gold2));
      }
      for(const [x,y] of [[92,129],[212,129]])rock(props,x,y,17,7,[C.violet0,C.violet1,C.violet3]);
    }
    return [L('Region sky and atmospheric light',back),L('Distant landforms and horizon',horizon),L('Environmental storytelling behind fighters',props),L('Quiet combat stage ground at y142',ground),L('Foreground material accents',front)];
  });
}

const faceRows=[
  '.....oooooo.......',
  '...oohjjkkjhoo.....',
  '..ohjjkkllkkjho....',
  '.ohjjkllkkkjjjho...',
  '.ohjjjjjhhjjjjho...',
  '.ohjjhbbbbbjjhho...',
  '..ohhbcddffdbbo....',
  '..ohbcdddfffddbo...',
  '...obcddooooddbo...',
  '...obcddfwoddfdbo..',
  '...obcdddddfffddo..',
  '....obcdddddfddbo..',
  '....obccdddddboo...',
  '.....obccddcbo.....',
  '......obbbbbo......',
];
function face(a,x,y,type=0,hurt=false){
  const hair=type===0?[P.wood1,P.wood3,P.wood4,P.wood5]:type===1?[P.wood0,P.wood2,P.wood3,P.wood4]:[P.cloth0,P.cloth2,P.iron3,P.iron5];
  const legend={'.':null,o:P.ink,h:hair[0],j:hair[1],k:hair[2],l:hair[3],b:P.skin0,c:P.skin2,d:P.skin3,f:P.skin4,w:P.iron5};
  const rows=faceRows.map((s,i)=>hurt&&i===9?'...obcddoooddfdbo..':s);
  a.push(grid(x,y,rows.map(s=>s.padEnd(19,'.')),legend));
}
function blade(a,hx,hy,tx,ty,tones=[P.iron0,P.iron2,P.iron4,P.iron5]){
  const len=Math.hypot(tx-hx,ty-hy),ux=(tx-hx)/len,uy=(ty-hy)/len,nx=-uy,ny=ux;
  const p=(along,across)=>[Math.round(hx+ux*along+nx*across),Math.round(hy+uy*along+ny*across)];
  a.push(line(...p(-3,0),...p(5,0),P.wood0,4),line(...p(-2,0),...p(5,0),P.wood4,2),poly([p(5,-3),p(len-4,-2),[tx,ty],p(len-4,2),p(5,3)],tones[0]),poly([p(6,-2),p(len-4,-1),[tx,ty],p(6,0)],tones[3]),poly([p(6,1),p(len-4,1),p(6,2)],tones[1]),line(...p(5,-5),...p(5,5),P.gold0,3),line(...p(5,-4),...p(5,4),P.gold3),r(...p(-3,-1),3,3,P.gold2));
}
function limb(a,points,shade,mid,light,highlight){
  a.push(poly(points,P.ink));
  // Inset contour follows the actual drawn silhouette rather than a floating tool rig.
  const cx=points.reduce((s,p)=>s+p[0],0)/points.length,cy=points.reduce((s,p)=>s+p[1],0)/points.length;
  const inset=points.map(([x,y])=>[Math.round(x+(cx-x)*.16),Math.round(y+(cy-y)*.16)]);
  a.push(poly(inset,shade),poly([inset[0],inset[1],inset[2],[Math.round(cx),Math.round(cy)]],mid));
  if(highlight)seq(a,highlight,light,2);
  if(points.length>5)a.push(line(points[1][0],points[1][1]+1,points[2][0]-1,points[2][1]+1,mid));
}
const heroLegs=[
  [[[23,40],[30,41],[29,49],[28,54],[32,55],[32,57],[20,57],[20,54],[23,49]],[[33,40],[40,40],[39,49],[40,54],[45,55],[45,57],[34,57],[32,51]]],
  [[[24,40],[32,43],[26,49],[21,55],[23,57],[12,57],[13,53],[19,47]],[[34,40],[41,41],[44,49],[46,54],[52,55],[52,57],[40,57],[38,51]]],
  [[[22,42],[29,43],[26,51],[27,55],[31,56],[31,57],[19,57],[19,54]],[[31,42],[38,42],[35,49],[38,54],[43,55],[43,57],[31,57],[29,51]]],
  [[[23,40],[30,41],[28,49],[27,54],[31,55],[31,57],[20,57],[20,54]],[[33,40],[39,40],[39,49],[41,54],[46,55],[46,57],[34,57],[32,50]]]
];
function legs(a,pose,cloth=P.cloth1){
  for(const pts of heroLegs[pose]){
    limb(a,pts,P.cloth0,cloth,P.cloth2,[[pts[0][0]+2,pts[0][1]+3],[pts[2][0],pts[2][1]]]);
    const xs=pts.filter(p=>p[1]>=54).map(p=>p[0]),min=Math.min(...xs),max=Math.max(...xs);
    a.push(line(min+1,55,max-1,55,P.wood3,2),line(min+1,54,max-3,54,P.wood5));
  }
}
function bow(a,x,y,pose){
  const pts=pose===1?[[x-4,y-19],[x+4,y-14],[x+7,y-4],[x+7,y+7],[x+2,y+16],[x-5,y+20]]:[[x-5,y-19],[x+1,y-12],[x+4,y-3],[x+3,y+7],[x-1,y+15],[x-6,y+19]];
  seq(a,pts,P.wood0,4);seq(a,pts,P.wood5,2);seq(a,pts.slice(1,-1),P.gold3);
  if(pose===1){seq(a,[pts[0],[x-18,y],pts.at(-1)],P.iron4);a.push(line(x-18,y,x+13,y,P.wood5),poly([[x+15,y],[x+10,y-3],[x+10,y+3]],P.iron4),line(x-18,y,x-22,y-3,P.teal4,2));}
  else seq(a,[pts[0],[x-6,y],pts.at(-1)],P.iron4);
  a.push(r(x-1,y-3,4,6,P.wood1),line(x,y-2,x,y+2,P.gold2));
}
function staff(a,hx,hy,tx,ty){
  const bx=hx-Math.round((tx-hx)*.75),by=Math.min(56,hy+22);
  a.push(line(bx,by,tx,ty+5,P.wood0,4),line(bx,by,tx,ty+5,P.wood4,2),line(hx,hy-4,hx,hy+3,P.gold2,3),poly([[tx-6,ty],[tx-7,ty+5],[tx-3,ty+10],[tx+3,ty+10],[tx+7,ty+4],[tx+6,ty],[tx+3,ty+6],[tx-3,ty+6]],P.gold1),line(tx-5,ty+4,tx-2,ty+8,P.gold3,2),poly([[tx,ty-5],[tx+4,ty-1],[tx+3,ty+5],[tx,ty+7],[tx-4,ty+3],[tx-4,ty-1]],P.teal0),poly([[tx,ty-4],[tx+2,ty-1],[tx+1,ty+4],[tx-2,ty+2],[tx-3,ty]],P.teal3),line(tx-1,ty-2,tx-1,ty+2,P.teal5));
}
export function heroes(){
  const frames=[];
  const rowan=[
    {head:[22,8],cloak:[[26,23],[36,25],[34,39],[26,50],[13,48],[18,37],[17,29]],fold:[[22,27],[27,26],[25,39],[18,45]],body:[[24,26],[36,25],[40,30],[39,44],[23,44],[20,33]],arm:[[36,27],[41,28],[45,33],[47,33],[48,39],[43,41],[39,36],[35,34]],hand:[44,35],tip:[51,8]},
    {head:[27,10],cloak:[[28,25],[38,27],[31,43],[21,48],[6,40],[14,39],[10,33],[20,34]],fold:[[25,28],[30,29],[23,40],[15,41],[18,37]],body:[[28,27],[39,27],[43,33],[40,45],[24,43],[23,34]],arm:[[39,29],[44,29],[48,33],[51,33],[52,38],[46,41],[41,37],[38,35]],hand:[49,35],tip:[62,20]},
    {head:[17,11],cloak:[[20,27],[31,27],[34,43],[26,51],[15,48],[16,36]],fold:[[20,29],[25,28],[25,44],[18,46]],body:[[20,28],[31,27],[37,34],[36,46],[23,44],[19,36]],arm:[[32,29],[37,30],[40,36],[41,40],[45,42],[42,47],[36,43],[34,38],[30,35]],hand:[41,43],tip:[59,51]},
    {head:[22,7],cloak:[[25,23],[35,24],[38,41],[30,50],[14,46],[19,35],[17,27]],fold:[[23,27],[27,26],[28,42],[19,44]],body:[[24,25],[36,24],[40,30],[38,44],[23,44],[20,32]],arm:[[36,26],[40,25],[40,19],[41,16],[46,16],[47,22],[45,30],[40,34],[36,32]],hand:[44,18],tip:[43,1]}
  ];
  for(let pose=0;pose<4;pose++){
    const p=rowan[pose],a=[e(16,57,34,4,'#171b2770')];
    a.push(poly(p.cloak,P.ink),poly(p.fold,P.teal2),line(p.cloak[2][0],p.cloak[2][1],p.cloak[3][0],p.cloak[3][1],P.teal1,3));
    legs(a,pose);limb(a,p.body,P.iron1,P.iron2,P.iron4,[[p.body[0][0]+2,29],[p.body[0][0]+3,36]]);
    a.push(poly([[p.body[0][0]+1,30],[p.body[1][0]-1,29],[p.body[1][0],35],[p.body[0][0]+3,37]],P.iron3),line(p.body[0][0]+3,32,p.body[1][0]-2,31,P.iron5));
    a.push(r(p.head[0]+7,p.head[1]+13,7,8,P.skin0),r(p.head[0]+8,p.head[1]+14,5,5,P.skin2));
    face(a,...p.head,0,pose===2);
    a.push(line(p.body[0][0],39,p.body[1][0],40,P.wood0,4),r(p.body[0][0]+6,38,4,4,P.gold2),r(p.body[0][0]+7,39,2,2,P.gold4));
    // Rear arm is an explicitly bent braced fist, attached below the cloak clasp.
    limb(a,[[p.body[0][0],29],[p.body[0][0]+4,32],[p.body[0][0]+2,38],[p.body[0][0]+6,40],[p.body[0][0]+4,44],[p.body[0][0]-2,41],[p.body[0][0]-3,35]],P.teal0,P.teal2,P.teal3);
    limb(a,p.arm,P.iron1,P.iron3,P.iron4,[[p.arm[0][0]+2,p.arm[0][1]+1],[p.arm[1][0],p.arm[1][1]+2],[p.hand[0],p.hand[1]-1]]);
    blade(a,...p.hand,...p.tip);a.push(r(p.hand[0]-2,p.hand[1]-1,4,4,P.skin2),r(p.hand[0]-1,p.hand[1]-1,3,2,P.skin4),r(p.body[0][0]+2,27,4,4,P.gold1),r(p.body[0][0]+3,27,2,2,P.gold3));
    frames.push([L('Rowan copper-haired guardian - explicit poses',a)]);
  }
  const lyra=[
    {head:[21,8],body:[[24,26],[35,25],[39,31],[37,44],[23,45],[20,34]],cape:[[23,24],[33,24],[28,39],[19,48],[12,44],[18,33]],arm:[[34,28],[39,28],[43,32],[46,32],[47,38],[43,40],[37,36],[33,35]],hand:[45,35],bow:[47,34]},
    {head:[23,9],body:[[24,27],[36,27],[40,33],[37,45],[23,44],[21,34]],cape:[[22,25],[32,25],[28,40],[17,47],[8,42],[16,37]],arm:[[34,28],[39,29],[44,32],[51,32],[52,37],[44,39],[37,35],[33,34]],hand:[50,34],bow:[48,34]},
    {head:[17,11],body:[[21,28],[32,27],[37,33],[37,46],[22,44],[18,35]],cape:[[20,25],[29,26],[31,39],[23,48],[13,44],[14,35]],arm:[[30,30],[36,32],[39,38],[46,39],[47,44],[41,46],[35,42],[31,36]],hand:[45,41],bow:[49,37]},
    {head:[21,7],body:[[24,25],[35,24],[39,30],[37,44],[23,44],[21,34]],cape:[[23,23],[33,23],[29,39],[19,48],[12,44],[18,32]],arm:[[34,26],[39,25],[42,21],[45,22],[48,29],[46,33],[41,29],[35,32]],hand:[46,26],bow:[49,26]}
  ];
  for(let pose=0;pose<4;pose++){
    const p=lyra[pose],a=[e(15,57,35,4,'#171b2770')];
    a.push(poly(p.cape,C.green0),poly([[p.cape[0][0],p.cape[0][1]+3],[p.cape[1][0]-3,p.cape[1][1]+3],[p.cape[3][0],p.cape[3][1]-2],[p.cape[4][0]+2,p.cape[4][1]-2]],C.green1));
    // Visible fletching is rooted in her quiver, not detached decorations.
    a.push(line(22,35,17,18,P.wood0,7),line(21,34,17,19,P.wood3,4));
    for(const x of [14,18,21])a.push(line(x+2,25,x,13,P.wood5),poly([[x,18],[x-3,15],[x-3,10],[x,13]],P.iron4));
    legs(a,pose,C.green0);limb(a,p.body,C.green0,C.green1,C.green2,[[p.body[0][0]+2,29],[p.body[0][0]+3,35]]);
    a.push(poly([[p.body[0][0]+4,28],[p.body[1][0],29],[p.body[1][0]-2,39],[p.body[0][0]+4,41]],P.wood2),line(p.body[0][0]+5,30,p.body[1][0]-2,31,P.wood4));
    a.push(r(p.head[0]+7,p.head[1]+13,7,8,P.skin0));face(a,...p.head,1,pose===2);
    const [x,y]=p.head;
    a.push(poly([[x+2,y+14],[x-1,y+8],[x+1,y+1],[x+8,y-3],[x+15,y],[x+18,y+6],[x+13,y+4],[x+5,y+5],[x+4,y+12],[x+7,y+17],[x+2,y+17]],C.green0),poly([[x+1,y+7],[x+3,y+1],[x+8,y-1],[x+14,y+1],[x+15,y+3],[x+5,y+3],[x+3,y+9]],C.green2),line(x+3,y+2,x+8,y,C.green3));
    a.push(line(p.body[0][0],41,p.body[1][0],40,P.wood0,3),r(p.body[0][0]+7,40,4,3,P.gold2));
    limb(a,p.arm,C.green0,C.green1,C.green3,[[p.arm[0][0]+2,p.arm[0][1]+2],[p.hand[0]-2,p.hand[1]]]);
    bow(a,...p.bow,pose);a.push(r(p.hand[0]-2,p.hand[1]-1,5,4,P.skin2),r(p.hand[0]-1,p.hand[1]-1,3,2,P.skin4));
    const rear=pose===1?[[25,29],[29,29],[34,33],[32,37],[25,37],[21,33]]:pose===3?[[25,28],[21,30],[16,25],[17,21],[21,22],[23,25]]:[[24,30],[28,32],[28,37],[32,39],[31,43],[25,42],[22,37]];
    limb(a,rear,C.green0,C.green1,C.green2);a.push(r(pose===1?29:pose===3?16:28,pose===1?33:pose===3?19:39,4,4,P.skin3));
    frames.push([L('Lyra green-hood ranger - explicit bow poses',a)]);
  }
  const kael=[
    {head:[21,8],robe:[[23,26],[35,25],[39,31],[39,43],[44,53],[31,51],[18,54],[20,42],[19,33]],fold:[[25,29],[30,28],[28,42],[25,50],[21,51]],arm:[[34,28],[39,28],[42,32],[47,33],[48,39],[43,41],[38,36],[33,34]],hand:[46,35],tip:[49,13]},
    {head:[24,9],robe:[[24,27],[37,27],[41,33],[38,43],[45,52],[31,50],[14,53],[21,42],[21,33]],fold:[[25,30],[31,29],[29,42],[23,50],[17,51]],arm:[[36,29],[41,30],[45,34],[50,32],[53,36],[50,40],[44,42],[38,37]],hand:[50,36],tip:[56,18]},
    {head:[16,11],robe:[[20,28],[32,27],[38,35],[35,43],[40,53],[28,51],[16,53],[18,41],[17,34]],fold:[[21,30],[27,29],[27,41],[22,50],[18,51]],arm:[[31,30],[36,32],[38,39],[44,40],[44,46],[38,46],[33,41],[29,36]],hand:[42,42],tip:[49,23]},
    {head:[21,7],robe:[[23,25],[35,24],[39,30],[38,42],[45,52],[31,50],[18,53],[21,41],[19,33]],fold:[[25,28],[30,27],[28,41],[25,49],[21,51]],arm:[[35,26],[39,24],[40,18],[44,16],[48,19],[47,25],[43,30],[37,32]],hand:[45,20],tip:[47,8]}
  ];
  for(let pose=0;pose<4;pose++){
    const p=kael[pose],a=[e(14,57,38,4,'#171b2770')];
    legs(a,pose);a.push(poly(p.robe,P.ink),poly(p.fold,C.violet2),poly([[p.robe[0][0]+5,p.robe[0][1]+2],[p.robe[1][0]-1,p.robe[1][1]+2],[p.robe[3][0]-1,42],[p.robe[4][0]-2,50],[p.robe[5][0],48],[p.robe[5][0]-2,37]],C.violet1));
    seq(a,[[p.robe[0][0]+2,30],[p.robe[0][0]+3,40],[p.robe[6][0]+2,51]],P.gold1);a.push(line(p.robe[6][0]+2,51,p.robe[5][0]-1,49,P.gold3),line(p.robe[5][0]+2,49,p.robe[4][0]-2,51,P.gold2));
    a.push(r(p.head[0]+7,p.head[1]+13,7,8,P.skin0));face(a,...p.head,2,pose===2);
    a.push(poly([[p.robe[0][0]-1,27],[p.robe[0][0]+5,28],[p.robe[0][0]+10,33],[p.robe[1][0]+3,27],[p.robe[1][0]+3,33],[p.robe[0][0]+10,37],[p.robe[0][0]+2,33]],C.violet0),line(p.robe[0][0]+2,29,p.robe[0][0]+10,34,C.violet3),r(p.robe[0][0]+8,34,3,4,P.teal3),r(p.robe[0][0]+9,34,1,2,P.teal5));
    limb(a,p.arm,C.violet0,C.violet2,C.violet3,[[p.arm[0][0]+2,p.arm[0][1]+2],[p.arm[1][0],p.arm[1][1]+3],[p.hand[0]-2,p.hand[1]-1]]);
    staff(a,...p.hand,...p.tip);a.push(r(p.hand[0]-2,p.hand[1]-1,4,4,P.skin2),r(p.hand[0]-1,p.hand[1]-1,3,2,P.skin4));
    const rear=pose===1?[[25,30],[29,31],[32,36],[37,34],[39,37],[34,42],[28,40],[24,35]]:[[23,30],[26,32],[25,39],[29,42],[27,46],[21,43],[20,36]];
    limb(a,rear,C.violet0,C.violet1,C.violet3);a.push(r(pose===1?35:25,pose===1?34:42,4,4,P.skin3));
    // Hammer insignia marks a runesmith rather than a generic wizard.
    a.push(r(p.robe[0][0]+8,41,6,2,P.gold2),r(p.robe[0][0]+10,43,2,4,P.gold2));
    frames.push([L('Kael violet runesmith - explicit staff poses',a)]);
  }
  return frames;
}

function boar(){
  const poses=[
    {body:[[28,33],[44,26],[60,28],[70,37],[69,53],[59,62],[33,60],[23,49]],head:[[28,35],[36,42],[32,56],[19,63],[6,57],[7,47],[16,43],[18,34]],snout:[7,49],eye:[20,45],ear:[[20,39],[16,28],[28,33],[30,42]],legs:[[[33,53],[42,56],[40,68],[43,72],[29,72],[30,68]],[[56,53],[65,51],[65,66],[72,69],[71,72],[58,72],[56,65]]],moss:[31,29],tail:[[67,38],[73,34],[77,38],[75,44]]},
    {body:[[26,39],[41,31],[59,31],[70,40],[66,57],[52,64],[29,60],[22,51]],head:[[23,42],[31,47],[27,61],[16,65],[3,60],[3,50],[12,47],[14,38]],snout:[3,53],eye:[15,49],ear:[[17,44],[11,33],[24,39],[26,47]],legs:[[[30,55],[39,57],[29,66],[22,70],[24,72],[11,72],[14,66]],[[55,55],[63,53],[65,62],[75,66],[76,72],[65,72],[59,66]]],moss:[29,34],tail:[[67,40],[74,35],[77,39],[74,44]]},
    {body:[[32,34],[48,28],[64,32],[73,44],[66,59],[46,62],[29,56],[26,45]],head:[[28,35],[38,42],[35,56],[22,59],[10,51],[12,42],[20,40],[22,33]],snout:[11,45],eye:[24,43],ear:[[24,39],[20,28],[31,33],[34,42]],legs:[[[34,54],[43,56],[38,65],[41,69],[39,72],[28,72],[29,66]],[[56,55],[65,52],[65,64],[73,68],[73,72],[60,72],[56,67]]],moss:[36,30],tail:[[69,40],[76,37],[77,43],[74,46]]},
    {body:[[28,56],[43,49],[61,52],[70,59],[69,69],[57,73],[31,71],[23,65]],head:[[24,55],[33,60],[30,69],[17,72],[5,69],[4,61],[13,59],[17,53]],snout:[5,63],eye:[17,62],ear:[[19,58],[15,49],[25,53],[28,61]],legs:[[[33,66],[44,67],[45,71],[38,74],[23,74],[23,71]],[[55,66],[64,65],[72,70],[73,74],[57,74],[51,70]]],moss:[32,51],tail:[[68,59],[75,57],[77,62],[72,64]]}
  ];
  return poses.map((p,i)=>{
    const a=[e(8,71,65,5,'#171b2770')];
    seq(a,p.tail,P.wood0,3);seq(a,p.tail,P.wood4);
    for(const leg of p.legs)limb(a,leg,P.wood0,P.wood2,P.wood4,[[leg[0][0]+2,leg[0][1]+2],[leg[2][0],leg[2][1]]]);
    limb(a,p.body,P.wood1,P.wood3,P.wood4);
    const [mx,my]=p.moss;
    a.push(poly([[mx-2,my+6],[mx+5,my],[mx+18,my-1],[mx+29,my+6],[mx+31,my+17],[mx+22,my+14],[mx+17,my+19],[mx+10,my+14],[mx+3,my+17]],C.green0),poly([[mx,my+6],[mx+7,my+1],[mx+18,my+1],[mx+27,my+7],[mx+23,my+10],[mx+16,my+8],[mx+9,my+12],[mx+3,my+10]],C.green1));
    for(const [dx,dy,h] of [[3,4,8],[12,0,10],[21,2,8]]){
      a.push(poly([[mx+dx-2,my+dy+3],[mx+dx,my+dy-h],[mx+dx+5,my+dy+4]],P.wood0),line(mx+dx,my+dy-h+2,mx+dx+2,my+dy+2,P.wood5,2),r(mx+dx-2,my+dy+6,5,2,C.green3));
    }
    for(const [dx,dy] of [[6,8],[15,7],[24,10]])a.push(line(mx+dx,my+dy,mx+dx+3,my+dy-1,C.green2,2));
    limb(a,p.head,P.wood1,P.wood3,P.wood4);a.push(poly(p.ear,P.ink),poly([p.ear[0],[p.ear[1][0]+2,p.ear[1][1]+3],[p.ear[2][0]-1,p.ear[2][1]+1]],P.wood3));
    const [sx,sy]=p.snout,[ex,ey]=p.eye;
    a.push(poly([[sx,sy],[sx+8,sy-1],[sx+10,sy+5],[sx+7,sy+9],[sx-1,sy+6]],P.wood0),line(sx+1,sy+1,sx+6,sy,P.wood4,2),r(sx+1,sy+4,2,3,P.ink),r(sx+6,sy+4,2,3,P.ink));
    a.push(line(ex-1,ey-2,ex+7,ey-3,P.wood0,2),r(ex,ey,4,2,i===3?P.wood0:P.fire4),r(ex,ey,1,2,P.ink));
    // Both tusks grow out of the jaw; highlights stay inside their silhouette.
    a.push(poly([[sx+8,sy+6],[sx+9,sy+1],[sx+7,sy-4],[sx+6,sy-7],[sx+11,sy-4],[sx+13,sy+3],[sx+11,sy+7]],P.wood0),poly([[sx+9,sy+5],[sx+11,sy+2],[sx+9,sy-3],[sx+9,sy],[sx+8,sy+5]],P.wood6),poly([[sx+16,sy+3],[sx+19,sy-5],[sx+16,sy-10],[sx+21,sy-7],[sx+22,sy],[sx+19,sy+5]],P.wood0),line(sx+18,sy+3,sx+20,sy-3,P.iron5,2));
    for(const [dx,dy] of [[20,11],[28,17],[35,15]])a.push(line(sx+dx,sy+dy-9,sx+dx-2,sy+dy-5,P.wood3));
    return [L('Thornback boar - moss tusks and four drawn poses',a)];
  });
}
function wolf(){
  const poses=[
    {body:[[29,33],[41,27],[59,32],[66,41],[62,55],[44,57],[28,50]],head:[[30,27],[34,40],[28,49],[16,47],[5,41],[7,35],[17,33],[22,23]],eye:[18,35],legs:[[[29,45],[38,48],[32,60],[31,69],[36,70],[35,73],[23,73],[24,60]],[[50,48],[59,46],[62,58],[59,67],[68,69],[68,73],[54,73],[51,64]]],tail:[[59,34],[70,33],[76,40],[75,52],[68,59],[71,46],[65,41]],crystal:[33,28]},
    {body:[[25,41],[42,34],[60,36],[67,45],[60,59],[39,60],[25,54]],head:[[25,37],[31,48],[24,55],[13,53],[2,47],[3,40],[12,39],[18,31]],eye:[13,41],legs:[[[28,50],[37,53],[28,63],[20,68],[22,72],[9,72],[11,67],[20,59]],[[50,53],[60,51],[64,60],[74,67],[76,72],[64,72],[56,65]]],tail:[[61,39],[70,37],[77,40],[75,47],[69,45],[67,48]],crystal:[32,35]},
    {body:[[32,32],[46,29],[64,37],[67,49],[58,59],[39,56],[29,45]],head:[[30,25],[36,38],[30,48],[18,43],[9,34],[12,29],[22,30],[25,21]],eye:[23,32],legs:[[[31,47],[40,49],[34,61],[34,68],[40,70],[39,73],[26,73],[26,64]],[[51,49],[61,49],[59,61],[66,68],[70,70],[69,73],[57,73],[53,65]]],tail:[[62,36],[72,34],[77,42],[74,54],[67,60],[70,45]],crystal:[37,30]},
    {body:[[25,61],[38,53],[60,56],[69,64],[65,71],[46,74],[28,70]],head:[[25,53],[30,64],[25,71],[14,73],[3,70],[3,63],[12,61],[18,51]],eye:[15,64],legs:[[[29,65],[39,64],[38,70],[28,74],[12,74],[12,71]],[[49,65],[60,63],[65,69],[74,70],[75,74],[58,74]]],tail:[[64,59],[74,56],[77,61],[75,68],[66,72]],crystal:[31,54]}
  ];
  return poses.map((p,i)=>{
    const a=[e(7,72,68,4,'#171b2770')];
    limb(a,p.tail,C.snow0,C.snow1,C.snow2);for(const leg of p.legs)limb(a,leg,C.snow0,C.snow1,C.snow3,[[leg[0][0]+2,leg[0][1]+2],[leg[2][0],leg[2][1]]]);
    limb(a,p.body,C.snow0,C.snow1,C.snow3,[[p.body[0][0]+2,p.body[0][1]+1],[p.body[1][0],p.body[1][1]+2],[p.body[2][0]-3,p.body[2][1]]]);
    const [cx,cy]=p.crystal;
    for(const [dx,dy,w,h] of [[0,5,7,16],[8,1,8,20],[18,4,7,15]])gem(a,cx+dx,cy+dy-h+5,w,h,P.iron0,C.snow1,P.teal5);
    limb(a,p.head,C.snow0,C.snow1,C.snow3);
    const [ex,ey]=p.eye;
    a.push(poly([[ex+5,ey-6],[ex+1,ey-16],[ex+10,ey-8],[ex+12,ey-2]],P.iron0),poly([[ex+5,ey-8],[ex+4,ey-12],[ex+9,ey-7]],C.snow2),poly([[ex+10,ey+6],[ex+6,ey+14],[ex+11,ey+11],[ex+13,Math.min(76,ey+16)],[ex+16,ey+6]],C.snow2),line(ex-1,ey-2,ex+5,ey-3,P.iron0,2),line(ex,ey,ex+3,ey,i===3?P.iron0:P.teal5,2));
    const nose=p.head[4];a.push(r(nose[0],nose[1]-2,5,4,P.iron0),line(nose[0]+2,nose[1]+4,nose[0]+13,nose[1]+6,P.iron0,2));
    if(i!==3)a.push(poly([[nose[0]+7,nose[1]+5],[nose[0]+10,nose[1]+6],[nose[0]+8,nose[1]+10]],P.iron5));
    for(let j=0;j<4;j++)a.push(line(cx+j*5,cy+17,cx+j*5-2,cy+21,C.snow1));
    return [L('Frostfang wolf - crystalline mane and drawn gait',a)];
  });
}
function mireWarden(){
  const poses=[
    {head:[29,17],body:[[26,31],[48,28],[59,39],[54,58],[37,63],[22,52]],arms:[[[27,33],[19,32],[12,40],[11,53],[7,61],[10,67],[20,66],[25,55],[23,43]],[[49,32],[58,33],[66,43],[65,54],[69,61],[65,67],[56,64],[53,51],[47,42]]],legs:[[[28,51],[39,56],[36,64],[35,69],[41,72],[23,73],[19,69],[25,64]],[[44,52],[54,53],[56,65],[64,70],[64,73],[45,73],[42,64]]]},
    {head:[23,23],body:[[23,35],[45,31],[56,40],[50,58],[32,62],[20,52]],arms:[[[25,36],[18,36],[14,42],[5,42],[2,48],[3,57],[12,58],[22,51],[29,44]],[[45,34],[55,33],[61,39],[62,48],[55,53],[49,47],[45,42]]],legs:[[[25,52],[37,55],[30,64],[23,69],[24,73],[10,73],[12,67],[20,61]],[[41,52],[51,50],[58,63],[68,69],[69,73],[52,73],[45,63]]]},
    {head:[36,19],body:[[29,33],[51,31],[61,41],[55,58],[37,62],[24,51]],arms:[[[30,36],[22,34],[17,38],[21,48],[31,54],[36,48],[28,41]],[[53,33],[61,35],[66,44],[69,56],[74,61],[70,66],[61,64],[58,53],[51,44]]],legs:[[[30,54],[40,55],[35,65],[37,70],[39,73],[23,73],[20,68],[27,62]],[[46,52],[55,52],[57,62],[66,69],[66,73],[47,73],[42,64]]]},
    {head:[18,49],body:[[29,52],[48,47],[61,54],[65,64],[54,72],[31,70],[25,62]],arms:[[[29,56],[21,58],[14,63],[4,64],[3,71],[14,74],[29,68],[36,65]],[[49,52],[59,48],[67,54],[69,64],[63,69],[55,61]]],legs:[[[37,65],[45,65],[45,71],[38,75],[25,75],[25,72]],[[52,63],[62,63],[69,69],[76,71],[76,75],[58,74]]]}
  ];
  return poses.map((p,i)=>{
    const a=[e(5,72,70,5,'#171b2770')];
    for(const part of [...p.legs,...p.arms]){
      limb(a,part,P.wood0,P.wood2,C.green2,[[part[0][0],part[0][1]+2],[part[1][0],part[1][1]+2],[part[2][0]+1,part[2][1]]]);
      seq(a,part.slice(2,5).map(([x,y])=>[x+2,y-1]),P.wood4,2);
    }
    limb(a,p.body,P.teal0,C.green1,C.green3,[[p.body[0][0]+2,p.body[0][1]+2],[p.body[1][0]-2,p.body[1][1]+2]]);
    const [x,y]=p.head;
    // Hanging bark lamellae, reeds and a carved face make a living ruin silhouette.
    {
      const x=i===3?34:p.head[0],y=i===3?36:p.head[1];
      for(const [dx,dy] of [[-1,17],[7,18],[15,17]]){
        a.push(poly([[x+dx,y+dy],[x+dx+7,y+dy-1],[x+dx+8,y+dy+14],[x+dx+2,y+dy+18],[x+dx-1,y+dy+9]],P.wood1),line(x+dx+1,y+dy+1,x+dx+2,y+dy+13,P.wood4,2),line(x+dx+5,y+dy+3,x+dx+5,y+dy+11,P.wood2));
      }
    }
    a.push(poly([[x+1,y+2],[x+16,y],[x+22,y+7],[x+19,y+19],[x+7,y+22],[x-2,y+14],[x-3,y+7]],P.ink),poly([[x+1,y+4],[x+13,y+2],[x+17,y+8],[x+14,y+18],[x+6,y+19],[x,y+13]],P.wood3),line(x+1,y+5,x+11,y+3,P.wood5,2),r(x-1,y+9,7,4,P.teal0),r(x,y+10,4,2,i===3?P.teal1:P.teal4),r(x+9,y+8,7,4,P.teal0),r(x+10,y+9,4,2,i===3?P.teal1:P.teal5),poly([[x+5,y+14],[x+11,y+13],[x+10,y+17],[x+6,y+18]],P.wood0));
    for(const [dx,dy] of [[0,4],[8,0],[17,3]]){
      seq(a,[[x+dx,y+dy+3],[x+dx-2,y+dy-6],[x+dx+1,y+dy-11]],C.green2,2);a.push(r(x+dx,y+dy-13,2,5,P.wood5));
    }
    for(const [dx,dy] of (i===3?[[-3,19],[18,15]]:[[-3,19],[18,15],[5,31]]))a.push(poly([[x+dx,y+dy],[x+dx-6,y+dy-4],[x+dx-8,y+dy-1],[x+dx-1,y+dy+4]],C.green2),line(x+dx-5,y+dy-1,x+dx,y+dy+1,C.green3));
    return [L('Mire Warden - reed armor carved bark and roots',a)];
  });
}
function colossus(){
  const poses=[
    {head:[29,11],body:[[25,28],[47,26],[59,34],[55,56],[43,63],[25,58],[20,40]],arms:[[[25,29],[13,27],[6,39],[8,50],[4,58],[8,67],[20,65],[24,54],[20,43],[29,39]],[[49,28],[61,28],[71,39],[68,51],[74,57],[70,67],[57,65],[52,54],[55,43],[46,38]]],legs:[[[26,53],[39,55],[36,64],[40,72],[21,73],[18,68],[22,62]],[[43,53],[55,53],[58,65],[65,68],[66,73],[45,73],[40,64]]]},
    {head:[23,17],body:[[23,32],[47,29],[57,40],[49,60],[30,61],[19,48]],arms:[[[24,34],[14,32],[9,38],[2,37],[1,45],[3,57],[14,60],[24,53],[29,43]],[[47,30],[59,29],[69,37],[65,48],[54,50],[50,43],[44,39]]],legs:[[[26,54],[38,56],[30,66],[26,73],[10,73],[11,67],[19,62]],[[41,54],[52,53],[60,64],[73,68],[74,73],[54,73],[46,65]]]},
    {head:[36,15],body:[[29,30],[51,29],[61,39],[55,59],[36,62],[25,49]],arms:[[[30,32],[19,29],[11,35],[14,48],[24,53],[30,46],[25,40],[35,40]],[[53,31],[65,32],[71,43],[67,53],[75,60],[72,68],[61,66],[56,55],[56,44],[48,39]]],legs:[[[29,54],[42,56],[36,65],[39,73],[22,73],[19,68],[24,62]],[[44,54],[57,54],[59,64],[67,70],[68,73],[46,73],[42,65]]]},
    {head:[17,44],body:[[29,48],[48,46],[59,54],[63,64],[51,73],[30,70],[24,60]],arms:[[[28,53],[18,55],[12,62],[3,63],[2,71],[12,76],[28,69],[36,64]],[[49,49],[61,46],[69,52],[70,63],[65,69],[56,64],[52,57]]],legs:[[[35,65],[45,65],[44,71],[39,76],[23,76],[23,71]],[[49,64],[61,63],[65,70],[76,71],[77,76],[56,75]]]}
  ];
  return poses.map((p,i)=>{
    const a=[e(3,72,73,5,'#171b2770')];
    for(const part of [...p.legs,...p.arms]){
      limb(a,part,P.ink,P.stone0,P.stone2,[[part[0][0],part[0][1]+2],[part[1][0],part[1][1]+2],[part[2][0]+1,part[2][1]]]);
      seq(a,part.slice(2,5).map(([x,y])=>[x+2,y-1]),i===3?P.fire0:P.fire2,2);
      a.push(line(part[0][0]+1,part[0][1]+5,part[1][0],part[1][1]+4,P.stone1,2));
    }
    limb(a,p.body,P.ink,P.stone0,P.stone2,[[p.body[0][0]+2,p.body[0][1]+2],[p.body[1][0]-2,p.body[1][1]+2]]);
    const [x,y]=p.head;
    {
    const x=i===3?38:p.head[0],y=i===3?28:p.head[1];
    a.push(poly([[x-2,y+19],[x+7,y+16],[x+19,y+20],[x+24,y+30],[x+17,y+43],[x+7,y+44],[x-2,y+33]],P.fire0),poly([[x+3,y+23],[x+10,y+20],[x+19,y+24],[x+20,y+31],[x+14,y+40],[x+7,y+38]],i===3?P.fire1:P.fire3),poly([[x+9,y+23],[x+14,y+24],[x+15,y+31],[x+11,y+36],[x+8,y+31]],i===3?P.fire2:P.fire5));
    for(const pts of [[[x-3,y+19],[x+8,y+17],[x+5,y+27],[x-5,y+30]],[[x+13,y+17],[x+24,y+22],[x+24,y+30],[x+17,y+27]],[[x-4,y+34],[x+5,y+32],[x+8,y+43],[x,y+42]],[[x+16,y+33],[x+24,y+31],[x+22,y+40],[x+12,y+44]]]){
      a.push(poly(pts,P.ink),line(pts[0][0]+1,pts[0][1]+1,pts[1][0]-1,pts[1][1]+1,P.stone1));
    }
    }
    a.push(poly([[x+1,y],[x+16,y-1],[x+24,y+7],[x+21,y+19],[x+9,y+23],[x-2,y+14],[x-3,y+6]],P.ink),poly([[x+2,y+2],[x+14,y+1],[x+18,y+7],[x+14,y+17],[x+7,y+19],[x,y+13]],P.stone0),line(x+2,y+3,x+12,y+2,P.stone2,2),poly([[x,y+9],[x+9,y+10],[x+14,y+8],[x+19,y+9],[x+17,y+13],[x+11,y+13],[x+5,y+15],[x,y+13]],P.fire0),line(x+1,y+11,x+6,y+12,i===3?P.fire1:P.fire4,2),line(x+13,y+10,x+17,y+10,i===3?P.fire1:P.fire4,2));
    seq(a,[[x+11,y+2],[x+9,y+6],[x+11,y+9]],P.fire2);seq(a,[[x+8,y+16],[x+11,y+18],[x+16,y+16]],P.fire1,2);
    // Two broken basalt horns visibly emerge from the skull.
    a.push(poly([[x+1,y+4],[x-4,y],[x-5,y-6],[x+3,y-2],[x+6,y+4]],P.ink),line(x-3,y-3,x+1,y,P.stone1,2),poly([[x+16,y+2],[x+20,y-6],[x+24,y-8],[x+24,y+1],[x+22,y+7]],P.stone0),line(x+21,y-4,x+21,y+2,P.stone2));
    return [L('Cinder Colossus - interlocking obsidian and molten core',a)];
  });
}
function sentinel(){
  const poses=[
    {head:[29,13],body:[[28,29],[46,27],[54,34],[49,51],[35,58],[25,48]],cape:[[45,27],[56,30],[59,49],[71,66],[52,64],[38,58]],arms:[[[29,31],[22,31],[17,39],[19,48],[14,53],[17,58],[24,54],[27,43],[34,39]],[[45,31],[53,31],[59,38],[58,48],[52,53],[46,47],[48,41],[43,38]]],legs:[[[29,48],[40,51],[36,61],[36,68],[42,70],[41,73],[24,73],[25,67]],[[41,49],[49,49],[51,61],[57,68],[64,70],[63,73],[47,73],[43,65]]],hand:[19,52],tip:[6,23],shield:[55,42]},
    {head:[22,18],body:[[24,32],[42,31],[51,39],[45,54],[31,59],[21,48]],cape:[[42,29],[54,31],[61,45],[76,52],[68,63],[49,58],[40,51]],arms:[[[25,34],[18,34],[12,41],[8,42],[7,47],[12,52],[19,48],[25,44],[31,40]],[[43,33],[52,33],[59,39],[58,48],[50,50],[45,45],[46,40]]],legs:[[[27,50],[36,53],[28,64],[23,69],[26,73],[11,73],[12,67],[20,59]],[[37,51],[47,49],[52,62],[65,69],[69,70],[68,73],[51,73],[42,63]]],hand:[10,45],tip:[2,18],shield:[57,40]},
    {head:[35,17],body:[[30,31],[48,31],[56,38],[50,54],[36,58],[27,48]],cape:[[46,29],[57,33],[58,49],[67,67],[49,64],[39,54]],arms:[[[31,33],[25,32],[20,37],[23,47],[31,51],[35,45],[28,40]],[[48,33],[55,35],[63,41],[65,51],[58,56],[52,50],[53,43],[46,39]]],legs:[[[31,50],[41,52],[37,63],[38,69],[43,70],[42,73],[25,73],[25,67]],[[44,50],[52,50],[50,60],[58,68],[65,70],[64,73],[48,73],[42,63]]],hand:[29,47],tip:[6,57],shield:[64,47]},
    {head:[17,47],body:[[31,48],[49,46],[59,55],[55,65],[41,72],[29,64]],cape:[[47,46],[60,48],[65,57],[77,65],[72,73],[49,72],[39,62]],arms:[[[31,52],[24,54],[17,62],[12,64],[13,70],[23,72],[29,65],[36,61]],[[49,51],[58,48],[66,53],[68,62],[61,67],[56,59]]],legs:[[[38,64],[46,64],[44,71],[35,75],[24,75],[25,71]],[[48,63],[56,61],[61,68],[74,72],[74,76],[56,75]]],hand:[16,67],tip:[2,53],shield:[65,62]}
  ];
  return poses.map((p,i)=>{
    const a=[e(8,73,67,4,'#171b2770')];
    limb(a,p.cape,C.violet0,C.violet1,C.violet2,[[p.cape[1][0],p.cape[1][1]+2],[p.cape[2][0]-2,p.cape[2][1]],[p.cape[3][0]-3,p.cape[3][1]-1]]);
    for(const part of [...p.legs,...p.arms])limb(a,part,P.iron0,P.iron2,P.iron4,[[part[0][0]+1,part[0][1]+2],[part[1][0]-1,part[1][1]+2],[part[2][0]+1,part[2][1]]]);
    limb(a,p.body,P.iron0,P.iron2,P.iron4,[[p.body[0][0]+2,p.body[0][1]+2],[p.body[1][0]-2,p.body[1][1]+2]]);
    const [x,y]=p.head;
    {
      const x=i===3?35:p.head[0],y=i===3?37:p.head[1];
      a.push(poly([[x-1,y+21],[x+8,y+18],[x+19,y+21],[x+17,y+31],[x+7,y+35],[x,y+30]],P.iron3),poly([[x+8,y+19],[x+18,y+22],[x+15,y+30],[x+8,y+33]],P.iron1),line(x+1,y+23,x+7,y+26,P.gold2),line(x+7,y+26,x+16,y+23,P.gold2),r(x+6,y+27,3,5,P.teal3),r(x+7,y+27,1,2,P.teal5));
    }
    a.push(poly([[x+3,y],[x+14,y-2],[x+22,y+5],[x+19,y+18],[x+9,y+22],[x-1,y+17],[x-4,y+8]],P.ink),poly([[x+3,y+2],[x+13,y],[x+17,y+5],[x+12,y+17],[x+8,y+19],[x+1,y+15],[x-2,y+9]],P.iron3),line(x+3,y+3,x+12,y+1,P.iron5,2),poly([[x-3,y+9],[x+8,y+9],[x+16,y+6],[x+16,y+11],[x+7,y+14],[x-2,y+13]],P.iron0),line(x-1,y+10,x+6,y+11,i===3?P.teal1:P.teal4),line(x+11,y+9,x+14,y+8,i===3?P.teal1:P.teal4),line(x+7,y+14,x+7,y+19,P.gold1));
    a.push(poly([[x+7,y],[x+9,y-8],[x+15,y-11],[x+20,y-10],[x+18,y-4],[x+13,y],[x+11,y+4]],C.violet0),poly([[x+10,y-1],[x+12,y-7],[x+18,y-9],[x+16,y-4]],C.violet3));
    blade(a,...p.hand,...p.tip,[P.iron0,C.violet1,P.iron4,P.iron5]);a.push(r(p.hand[0]-2,p.hand[1]-1,4,4,P.iron2),r(p.hand[0]-1,p.hand[1]-1,2,2,P.iron4));
    const [sx,sy]=p.shield;
    a.push(poly([[sx-8,sy-9],[sx+5,sy-12],[sx+11,sy-5],[sx+9,sy+11],[sx+1,sy+17],[sx-8,sy+7]],P.iron0),poly([[sx-6,sy-8],[sx+5,sy-10],[sx+9,sy-4],[sx+7,sy+10],[sx+1,sy+14],[sx-6,sy+6]],P.gold1),poly([[sx-4,sy-6],[sx+4,sy-8],[sx+7,sy-3],[sx+5,sy+9],[sx+1,sy+11],[sx-4,sy+5]],C.violet1),line(sx-5,sy-6,sx+3,sy-8,P.gold3),poly([[sx+1,sy-4],[sx+3,sy+1],[sx+6,sy+2],[sx+2,sy+4],[sx+1,sy+9],[sx-1,sy+4],[sx-4,sy+2],[sx-1,sy]],P.teal4));
    return [L('Starbound Sentinel - ancient plate shield and plume',a)];
  });
}
export function enemies(){return [...boar(),...mireWarden(),...wolf(),...colossus(),...sentinel()];}

export function equipment(){
  const a=[];
  const materials=[
    [P.iron0,P.iron2,P.iron3,P.iron5],
    ['#303f39','#647551','#a3a06a','#d7d4a0'],
    [C.snow0,C.snow1,C.snow2,P.iron5],
    [P.ink,P.stone0,P.stone2,P.fire4],
    [C.violet0,C.violet2,P.iron3,P.teal5]
  ];
  for(let region=0;region<5;region++)for(let slot=0;slot<4;slot++){
    const x=(region*4+slot)*32,t=materials[region];
    if(slot===0){
      blade(a,x+9,24,x+25,3,t);
      a.push(line(x+7,26,x+10,28,P.gold0,3),line(x+7,26,x+10,28,P.gold2));
      if(region===1)a.push(poly([[x+17,17],[x+13,13],[x+14,10],[x+19,14]],C.green2),line(x+15,13,x+18,15,C.green3));
      if(region===2)a.push(poly([[x+18,14],[x+20,6],[x+23,6],[x+23,9]],P.teal5));
      if(region===3)seq(a,[[x+15,19],[x+17,15],[x+17,12],[x+21,8]],P.fire3);
      if(region===4)a.push(line(x+19,11,x+22,8,P.teal5,2),r(x+16,15,2,2,P.gold3));
    }else if(slot===1){
      a.push(line(x+9,28,x+22,6,P.wood0,5),line(x+10,27,x+22,7,P.wood4,3),line(x+10,25,x+19,10,P.wood6),poly([[x+17,6],[x+25,5],[x+27,9],[x+25,15],[x+20,19],[x+14,19],[x+16,14],[x+15,10],[x+10,9],[x+12,4]],t[0]),poly([[x+18,7],[x+24,7],[x+25,10],[x+23,14],[x+19,17],[x+16,17],[x+18,13]],t[1]),poly([[x+25,7],[x+26,10],[x+24,15],[x+20,18],[x+16,18],[x+19,15],[x+23,12]],t[3]),line(x+12,5,x+15,7,t[2],2),line(x+17,10,x+22,13,P.gold2,2),r(x+18,10,2,2,P.gold4),line(x+8,28,x+11,29,P.gold2,2));
      if(region===1)a.push(poly([[x+15,21],[x+19,20],[x+20,23],[x+16,25]],C.green2));
      if(region===2)a.push(poly([[x+12,4],[x+15,1],[x+17,7]],C.snow3));
      if(region===3)seq(a,[[x+23,8],[x+20,11],[x+22,13],[x+19,16]],P.fire3);
      if(region===4)a.push(poly([[x+21,8],[x+23,10],[x+21,12],[x+19,10]],P.teal4));
    }else if(slot===2){
      a.push(poly([[x+11,4],[x+15,7],[x+19,4],[x+26,8],[x+29,16],[x+24,20],[x+22,17],[x+23,27],[x+17,29],[x+7,27],[x+8,17],[x+5,20],[x+1,16],[x+4,8]],t[0]),poly([[x+10,7],[x+15,10],[x+20,7],[x+23,10],[x+20,16],[x+21,25],[x+16,27],[x+9,25],[x+10,15],[x+6,11]],t[1]),poly([[x+4,10],[x+8,8],[x+9,12],[x+5,17],[x+3,15]],t[2]),poly([[x+22,8],[x+25,10],[x+27,15],[x+25,17],[x+21,12]],t[2]),line(x+10,8,x+14,11,t[3]),line(x+16,11,x+20,8,t[3]));
      for(let row=0;row<4;row++)for(let col=0;col<4;col++){
        const px=x+10+col*3+(row%2),py=14+row*3;
        if(px>x+20)continue;a.push(line(px,py,px+1,py,t[2]),r(px+1,py+1,1,1,t[0]));
      }
      a.push(line(x+8,24,x+21,24,P.wood0,3),line(x+9,24,x+20,24,P.gold1),r(x+13,23,4,3,P.gold2),r(x+14,24,2,1,P.gold4));
      if(region===1)a.push(poly([[x+6,7],[x+7,3],[x+13,8],[x+9,11]],C.green2));
      if(region===2)a.push(poly([[x+4,9],[x+4,3],[x+9,7]],C.snow3),poly([[x+23,7],[x+27,3],[x+27,9]],C.snow3));
      if(region===3)seq(a,[[x+11,13],[x+14,16],[x+13,20],[x+17,22]],P.fire3);
      if(region===4)a.push(poly([[x+15,12],[x+17,16],[x+15,20],[x+13,16]],P.teal4));
    }else{
      a.push(poly([[x+5,7],[x+15,3],[x+27,7],[x+26,20],[x+22,26],[x+16,30],[x+9,26],[x+5,19]],t[0]),poly([[x+7,8],[x+15,5],[x+25,8],[x+24,19],[x+20,25],[x+16,28],[x+10,24],[x+7,18]],P.gold1),poly([[x+9,9],[x+15,7],[x+23,9],[x+22,19],[x+19,23],[x+16,25],[x+11,22],[x+9,17]],t[1]),poly([[x+9,9],[x+15,7],[x+16,24],[x+11,21],[x+9,17]],t[2]),line(x+7,8,x+15,5,P.gold3),line(x+7,9,x+8,17,P.gold2),poly([[x+15,10],[x+18,14],[x+18,18],[x+15,21],[x+12,18],[x+12,14]],t[0]),line(x+15,12,x+15,19,region===3?P.fire3:region===4?P.teal4:P.gold2,2),line(x+12,16,x+18,16,region===3?P.fire4:P.gold3));
      if(region===1)a.push(poly([[x+6,14],[x+2,10],[x+4,7],[x+9,12]],C.green2));
      if(region===2)a.push(poly([[x+13,6],[x+16,1],[x+19,6]],P.iron5));
      if(region===3)seq(a,[[x+23,11],[x+21,16],[x+22,20]],P.fire2);
      if(region===4)a.push(r(x+14,4,3,3,P.teal4),r(x+15,4,1,2,P.teal5));
    }
  }
  return [L('Twenty regional equipment silhouettes - IronSword first',a)];
}

function flowers(a,x,y,wide=22){
  for(let i=0;i<6;i++){
    const xx=x+(i*7)%wide,yy=y-3-(i*5)%10;
    a.push(line(xx,y,xx,yy,C.green1,2),line(xx,yy+6,xx-3,yy+3,C.green2,2),poly([[xx,yy+3],[xx+4,yy-1],[xx+5,yy+1],[xx+1,yy+5]],P.teal2),r(xx-1,yy-2,4,3,i%2?C.red2:P.gold2),r(xx,yy-2,2,1,i%2?C.red3:P.gold4));
  }
}
function planter(a,x,y,w=28){
  a.push(e(x-2,y+9,w+5,5,'#171b2770'),poly([[x,y],[x+w,y],[x+w-3,y+12],[x+3,y+12]],P.wood0),poly([[x+2,y+2],[x+w-2,y+2],[x+w-4,y+10],[x+4,y+10]],P.wood3),line(x+3,y+2,x+w-3,y+2,P.wood5),line(x+5,y+8,x+w-5,y+8,P.wood1));
  flowers(a,x+4,y,w-8);
}
function tinyLantern(a,x,y){
  a.push(line(x,y-4,x,y,P.wood0),poly([[x-4,y+2],[x-2,y],[x+3,y],[x+5,y+2]],P.iron0),r(x-3,y+3,7,10,P.wood0),r(x-2,y+4,5,7,P.fire3),r(x-1,y+5,3,5,P.fire5),line(x-3,y+3,x-3,y+12,P.gold1),line(x-4,y+13,x+4,y+13,P.gold2));
}
export function townlife(){
  const exit=[],garden=[],festival=[];
  // Level-one overlay stays wholly inside x130..269 / y539..591.
  exit.push(poly([[148,548],[244,548],[267,591],[131,591]],P.wood0),poly([[150,549],[242,549],[262,590],[136,590]],P.wood2));
  for(let i=0;i<7;i++){
    const y=550+i*6,left=149-i*2,right=244+i*3;
    exit.push(line(left,y,right,y,P.wood5,2),line(left,y+2,right+1,y+2,P.wood3,3),line(left+7,y+3,left+25,y+3,P.wood1),line(right-27,y+2,right-9,y+2,P.wood4));
    for(const x of [left+2,right-2])exit.push(r(x,y,2,1,P.iron4));
  }
  for(const pts of [[[139,585],[149,550]],[[258,585],[241,550]]]){
    seq(exit,pts,P.wood0,5);seq(exit,pts.map(([x,y])=>[x,y-1]),P.wood5,2);
  }
  for(const [x,y] of [[141,579],[148,552],[258,579],[242,552]])exit.push(r(x-2,y-9,5,14,P.wood0),r(x-1,y-9,2,12,P.wood4),r(x-3,y-11,7,3,P.wood5));
  // Small supply cart beside, rather than across, the pedestrian center.
  exit.push(e(230,564,37,9,P.wood0),e(234,563,8,9,P.ink),e(255,563,8,9,P.ink),e(236,565,4,5,P.iron3),e(257,565,4,5,P.iron3),poly([[229,549],[262,549],[266,563],[233,567]],P.iron0),poly([[232,551],[260,551],[263,561],[235,564]],P.iron2),line(232,551,260,551,P.iron4),line(237,554,239,562,P.iron0),line(250,553,252,561,P.iron0));
  rock(exit,233,546,13,7,[P.iron0,P.iron2,P.iron4]);rock(exit,245,542,15,9,[P.iron0,P.iron3,P.iron5]);exit.push(r(241,545,3,2,P.gold2));
  // Level two: restored well, garden and bench remain right of the actors' walk.
  garden.push(e(301,444,54,12,'#171b2770'));
  for(let row=0;row<3;row++)for(let col=0;col<3;col++)brick(garden,304+col*14-(row%2)*2,426+row*7,14,8,true);
  garden.push(e(301,418,48,16,P.iron0),e(303,419,44,11,P.iron3),e(309,421,32,7,P.teal0),line(314,423,330,423,P.teal3),line(323,425,335,425,P.teal4));
  for(const x of [305,340])garden.push(r(x,394,5,32,P.wood0),r(x+1,395,2,27,P.wood4));
  garden.push(poly([[298,396],[309,382],[339,382],[351,397]],P.wood0),poly([[302,394],[311,384],[338,384],[347,394]],P.teal1),line(311,384,337,384,P.teal4));
  for(let j=0;j<4;j++)garden.push(line(313+j*7,385,308+j*10,394,P.teal3));
  garden.push(line(305,398,345,398,P.wood5,2),line(307,406,343,406,P.wood1,4),line(308,405,342,405,P.wood4),line(325,406,325,421,P.wood5),r(321,417,9,9,P.wood0),r(323,419,5,5,P.wood3),line(322,419,327,419,P.iron3));
  planter(garden,344,443,29);flowers(garden,305,455,30);
  garden.push(r(316,465,5,11,P.wood0),r(348,465,5,11,P.wood0),r(312,460,45,7,P.wood0),r(314,461,41,3,P.wood4),line(315,461,353,461,P.wood6),r(316,450,4,13,P.wood0),r(350,450,4,13,P.wood0),r(315,451,39,5,P.wood2),line(316,451,352,451,P.wood5));
  // Level three is independent: hanging festival lights plus lower planters.
  const rope=[[42,80],[75,91],[113,98],[151,101],[190,101],[231,97],[269,91],[307,82],[336,80]];
  seq(festival,rope,P.wood0,2);seq(festival,rope.map(([x,y])=>[x,y-1]),P.wood5);
  for(const [x,y,color] of [[53,84,P.teal2],[89,94,C.red1],[130,100,P.teal2],[168,102,C.red1],[208,100,P.teal2],[246,95,C.red1],[324,81,P.teal2]]){
    festival.push(poly([[x,y],[x+13,y+1],[x+11,y+13],[x+5,y+17],[x-1,y+10]],P.wood0),poly([[x+1,y+1],[x+11,y+2],[x+10,y+12],[x+5,y+15],[x+1,y+10]],color),line(x+2,y+3,x+9,y+4,P.gold2),poly([[x+5,y+5],[x+8,y+8],[x+5,y+11],[x+3,y+8]],P.gold3));
  }
  for(const [x,y] of [[76,92],[115,99],[191,102],[232,98],[334,82]])tinyLantern(festival,x,y);
  for(const [x,y,w] of [[64,549,34],[280,551,35],[306,537,26]])planter(festival,x,y,w);
  festival.push(line(42,78,42,90,P.wood0,3),r(40,77,5,3,P.gold2),line(336,79,336,91,P.wood0,3),r(334,77,5,3,P.gold2));
  return [[L('Town level 1 - lower bridge and supply cart',exit)],[L('Town level 2 - restored well garden and bench',garden)],[L('Town level 3 - festival roof lights and planters',festival)]];
}

const walkFeet=[
  [[[18,36],[25,38],[21,45],[16,50],[19,52],[9,52],[9,49],[15,43]],[[25,36],[31,36],[32,45],[37,49],[40,50],[39,52],[30,52],[26,46]]],
  [[[18,37],[24,37],[25,44],[29,47],[30,50],[23,50],[19,45]],[[25,36],[31,37],[29,46],[29,50],[34,51],[34,52],[24,52],[23,48]]],
  [[[18,36],[24,36],[27,43],[33,48],[37,49],[37,52],[27,52],[22,47]],[[26,36],[31,38],[26,45],[20,50],[21,52],[11,52],[12,49],[19,42]]],
  [[[18,37],[24,37],[22,46],[23,50],[28,51],[28,52],[18,52],[17,48]],[[26,37],[31,37],[32,44],[28,49],[23,49],[23,46],[26,43]]]
];
const villageFace=[
  '....oooooo.....',
  '..oohhjjjhoo...',
  '.ohjjkkkjjjho..',
  '.ohjkllkkjjho..',
  '.ohjjhhhhhhoo..',
  '..ohbcddddbo...',
  '..obcddffddbo..',
  '..obcddoowdbo..',
  '...bcdddfwddbo.',
  '...obcdddffddo.',
  '....obcddddbo..',
  '.....obbbbbo...',
];
function civilianFace(a,x,y,kind){
  const hair=kind===0?[P.wood0,P.wood2,P.wood3,P.wood4]:kind===1?[P.wood1,P.wood3,P.wood4,P.wood5]:[P.cloth0,P.cloth1,P.cloth2,P.iron3];
  a.push(grid(x,y,villageFace.map(s=>s.padEnd(16,'.')),{'.':null,o:P.ink,h:hair[0],j:hair[1],k:hair[2],l:hair[3],b:P.skin0,c:P.skin2,d:P.skin3,f:P.skin4,w:P.iron5}));
}
export function villagers(){
  const frames=[];
  for(let kind=0;kind<3;kind++)for(let pose=0;pose<4;pose++){
    const a=[e(9,52,31,3,'#171b2760')],dy=pose%2,headX=[17,18,17,16][pose],headY=6+dy;
    const colors=kind===0?[P.iron0,P.iron2,P.iron3]:kind===1?[C.green0,C.green1,C.green3]:[C.red0,C.red1,C.red3];
    for(const foot of walkFeet[pose]){
      limb(a,foot,P.cloth0,P.cloth1,P.cloth2,[[foot[0][0]+1,foot[0][1]+2],[foot[2][0],foot[2][1]]]);
      const end=foot.filter(p=>p[1]>=50);if(end.length>1)a.push(line(Math.min(...end.map(p=>p[0]))+1,51,Math.max(...end.map(p=>p[0]))-1,51,P.wood4));
    }
    if(kind===0){
      a.push(poly([[10,20+dy],[21,18+dy],[24,24+dy],[22,38+dy],[9,37+dy],[8,28+dy]],P.wood0),r(10,24+dy,10,12,P.wood2),line(11,26+dy,19,26+dy,P.wood5),line(11,32+dy,19,32+dy,P.wood4));
      rock(a,9,19+dy,10,6,[P.iron0,P.iron2,P.iron4]);rock(a,15,17+dy,8,7,[P.iron0,P.iron3,P.iron5]);
    }
    const torso=pose===0?[[19,19],[28,19],[32,25],[30,38],[18,38],[15,28]]:pose===1?[[20,20],[29,20],[33,26],[31,39],[19,39],[16,29]]:pose===2?[[19,19],[28,19],[32,25],[30,38],[17,38],[15,28]]:[[18,20],[27,20],[31,26],[29,39],[17,39],[14,29]];
    limb(a,torso,colors[0],colors[1],colors[2],[[torso[0][0]+1,torso[0][1]+3],[torso[0][0]+2,torso[0][1]+10]]);
    if(kind===0)a.push(poly([[21,22+dy],[28,23+dy],[28,37+dy],[20,37+dy]],P.wood2),line(22,23+dy,26,24+dy,P.wood5),line(20,20+dy,18,33+dy,P.wood5,2));
    else if(kind===1)a.push(poly([[20,24+dy],[29,24+dy],[32,40+dy],[24,42+dy],[16,40+dy]],C.green1),line(22,26+dy,21,38+dy,C.green2),line(24,25+dy,29,39+dy,P.gold1));
    else a.push(poly([[20,22+dy],[28,22+dy],[30,39+dy],[22,42+dy],[16,38+dy]],C.red1),line(21,24+dy,21,38+dy,C.red2),line(27,24+dy,28,36+dy,P.gold2));
    a.push(r(headX+6,headY+10,6,7,P.skin0));civilianFace(a,headX,headY,kind);
    if(kind===0){
      a.push(poly([[headX+1,headY+5],[headX+2,headY],[headX+9,headY-1],[headX+14,headY+3],[headX+14,headY+5]],P.iron1),line(headX+3,headY+1,headX+9,headY,P.iron3),line(headX+1,headY+5,headX+14,headY+5,P.iron4),r(headX+9,headY+1,3,3,P.gold2));
    }else if(kind===1){
      a.push(poly([[headX,headY+7],[headX-1,headY+2],[headX+5,headY-1],[headX+11,headY],[headX+14,headY+4],[headX+9,headY+3],[headX+3,headY+4],[headX+2,headY+9],[headX+5,headY+14],[headX,headY+13]],C.green0),line(headX+1,headY+2,headX+6,headY,C.green3,2));
    }else{
      a.push(poly([[headX-1,headY+3],[headX+2,headY-1],[headX+10,headY-1],[headX+14,headY+2],[headX+12,headY+5],[headX+1,headY+5]],C.red1),line(headX+1,headY+5,headX+12,headY+5,P.gold2),line(headX+3,headY+1,headX+9,headY,C.red3));
    }
    const frontArms=[[[28,23],[32,24],[35,29],[39,31],[38,35],[33,35],[29,29]],[[29,24],[33,25],[35,31],[33,36],[29,34],[29,29]],[[28,23],[31,24],[29,31],[24,34],[22,31],[26,27]],[[27,24],[31,25],[32,31],[35,34],[33,37],[29,34],[27,29]]];
    const rearArms=[[[19,24],[22,27],[18,33],[14,35],[12,32],[16,28]],[[20,25],[22,27],[19,32],[18,36],[14,35],[15,30]],[[19,24],[21,27],[24,31],[29,32],[28,36],[22,35],[17,29]],[[18,25],[20,27],[20,33],[23,36],[20,38],[16,34],[15,28]]];
    limb(a,rearArms[pose],colors[0],colors[1],colors[2]);
    if(kind===1){
      a.push(poly([[30,34+dy],[39,34+dy],[39,42+dy],[32,44+dy],[29,41+dy]],P.wood0),line(32,35+dy,37,35+dy,P.wood5,2),r(31,38+dy,7,3,P.wood3));
      for(const [x,y] of [[31,31],[35,29],[38,32]])a.push(line(x,36+dy,x,y+dy,P.teal2,2),poly([[x,y+dy],[x-3,y-3+dy],[x-4,y+dy],[x,y+2+dy]],C.green3));
    }
    if(kind===2){
      // Compact lute with neck, tuning pegs and connected fretting hand.
      a.push(line(20,25+dy,31,38+dy,P.gold1),e(26,31+dy,12,13,P.wood0),e(28,32+dy,8,10,P.wood4),e(30,35+dy,4,4,P.wood1),line(33,35+dy,38,22+dy,P.wood0,4),line(33,35+dy,38,22+dy,P.wood5,2),line(31,40+dy,37,24+dy,P.iron4),r(36,22+dy,4,2,P.gold2));
    }
    limb(a,frontArms[pose],colors[0],colors[1],colors[2],[[frontArms[pose][0][0]+1,frontArms[pose][0][1]+2],[frontArms[pose][1][0],frontArms[pose][1][1]+2]]);
    const hands=[[36,32],[30,34],[23,31],[32,34]];a.push(r(hands[pose][0],hands[pose][1],3,3,P.skin3),r(hands[pose][0],hands[pose][1],2,1,P.skin4));
    if(kind===2)a.push(r(35,27+dy,3,4,P.skin3));
    a.push(line(torso[0][0],36+dy,torso[1][0],36+dy,P.wood0,2),r(torso[0][0]+5,35+dy,3,3,P.gold2));
    frames.push([L(['Ore porter - loaded basket and hand-drawn steps','Herbalist - green hood gathering basket and steps','Bard merchant - burgundy coat lute and steps'][kind],a)]);
  }
  return frames;
}

const heroNames=['Rowan','Lyra','Kael'],bossNames=['ThornbackBoar','MireWarden','FrostfangWolf','CinderColossus','StarboundSentinel'];
const poseTags=(names,poses)=>names.flatMap((name,index)=>poses.map((pose,p)=>({name:`${name}-${pose}`,from:index*4+p+1,to:index*4+p+1})));
export const campaignAssets=[
  {name:'worldmap',width:384,height:480,frames:[worldmap()],durations:[1000],slices:[['Emberwood',69,375,16,16],['WhisperingMarsh',253,314,16,16],['Frostpeak',86,215,16,16],['AshenCaldera',267,147,16,16],['StarfallCitadel',180,55,16,16],['EmberOak',23,418,80,40]],tags:[{name:'valley',from:1,to:1}]},
  {name:'battlefields',width:320,height:180,frames:battlefields(),durations:[1000,1000,1000,1000,1000],previewFrames:true,noGif:true,tags:['Emberwood','WhisperingMarsh','Frostpeak','AshenCaldera','StarfallCitadel'].map((name,i)=>({name,from:i+1,to:i+1})),slices:[['hero-stage',35,69,64,64],['enemy-stage',214,64,80,80],['ground',0,142,320,38]]},
  {name:'heroes',width:64,height:64,frames:heroes(),durations:Array.from({length:3},()=>[520,140,180,650]).flat(),tags:poseTags(heroNames,['idle','attack','hurt','victory'])},
  {name:'enemies',width:80,height:80,frames:enemies(),durations:Array.from({length:5},()=>[540,150,200,900]).flat(),tags:poseTags(bossNames,['idle','attack','hurt','defeated'])},
  {name:'equipment',width:640,height:32,frames:[equipment()],durations:[1000],slices:Array.from({length:20},(_,id)=>[`${['Iron','BogBronze','FrostSilver','EmberObsidian','Starsteel'][Math.floor(id/4)]}${['Sword','Axe','Mail','Ward'][id%4]}`,id*32,0,32,32])},
  {name:'townlife',width:384,height:592,frames:townlife(),durations:[1000,1000,1000],previewFrames:true,noGif:true,tags:['TownLevel1-bridge','TownLevel2-well','TownLevel3-festival'].map((name,i)=>({name,from:i+1,to:i+1}))},
  {name:'villagers',width:48,height:56,frames:villagers(),durations:Array(12).fill(150),tags:['OrePorter','Herbalist','BardMerchant'].map((name,i)=>({name:`${name}-walk`,from:i*4+1,to:i*4+4}))}
];
