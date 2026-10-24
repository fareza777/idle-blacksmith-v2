import {P,r,e,line,poly,grid} from './palette.mjs';
const legend={'.':null,o:P.ink,a:P.skin0,b:P.skin1,c:P.skin2,d:P.skin3,f:P.skin4,h:P.wood1,i:P.wood3,j:P.wood4,k:P.wood5,l:P.wood6,s:P.iron0,t:P.iron1,u:P.iron2,v:P.iron3,w:P.iron4,x:P.iron5,A:P.teal0,B:P.teal1,C:P.teal2,D:P.teal3,E:P.teal4,F:P.teal5,g:P.gold2,G:P.gold3,R:P.fire1,O:P.fire2,Y:P.fire4,Q:P.fire5,n:P.cloth0,m:P.cloth1,N:P.cloth2,M:P.cloth3};
const g=(x,y,rows,colors=legend)=>grid(x,y,rows.map(s=>s.padEnd(Math.max(...rows.map(t=>t.length)),'.')),colors);
const smithHead=[
'......ooooooo.......',
'....oohhiiihhoo.....',
'...ohhijjjkjiihho...',
'..ohijjkkllkjjiho...',
'..ohijjjkkkjjjihho..',
'.ohhiiijjjjjjjjihho.',
'.ohiiioooooooiiiho..',
'.ohiobdddddddoiiho..',
'..hobbddfffddboiho..',
'..obbcddfffdddbbo...',
'.obcbccddddddddbo...',
'.obcddbooobdooodbo..',
'..obdddwoddfwodbo...',
'..oabcdddddfdddbo...',
'...oabcvwddfddbo....',
'...otuvwwcccbvo.....',
'...ouvwxxwvwvuo.....',
'....ouvwxxwvuo......',
'.....ouvwwvuo.......',
'......ouwwuo........',
'.......ouuo.........'];
const smithBent=[
'.....ooooooo........',
'...oohhiiihhoo......',
'..ohhijjjkjihhoo....',
'.ohijjkkllkjjiho....',
'.ohijjjkkkjjjihho...',
'ohhiiijjjjjjjjihho..',
'ohiiioooooooiiiho...',
'ohiobdddddddoiiho...',
'.hobbddfffddboiho...',
'.obbcddfffdddbbo....',
'obcbccddddddddbo....',
'obcddbooobdooodbo...',
'.obdddtoddfwodbo....',
'.oabcdddddfdddbo....',
'..oabcvwddfddbo.....',
'..otuvwwcccbvo......',
'...uvwxxwvwvuo......',
'...ouvwxxwvuo.......',
'....ouvwwvuo........',
'.....ouwwuo.........',
'......ouuo..........'];
const torso=[
'.....ooo......ooo.......',
'...oobddo....obddboo.....',
'..obbdddbooooaddddcbo....',
'.obcdddfdoohoaddfffddbo..',
'obcdddfdbohihobdfffdddbo.',
'obcdddboohijhooobdddddbo.',
'.obccbohijjkkjihobbccbo..',
'..obbohijjkklkjihobbo...',
'...ohijjjkkllkkjiho.....',
'...ohijjjkkkkkjjiho.....',
'...ohijjjjkkkjjjiho.....',
'...ohijjohhhhojjiho.....',
'...ohijohjkkjhojiho.....',
'...ohijohjkkjhojiho.....',
'...ohijjohhhhojjiho.....',
'...ohijjjkkkjjjjiho.....',
'...ohijjjkkjjjjjiho.....',
'...ohhiiijjiiiihho......',
'....oohhiiiihhoo........'];
const boots=[
'..onnnno....onnnno...',
'..onmmno....onmmno...',
'..onmmno....onmmno...',
'..onmmno....onmmno...',
'..ohhhho....ohhhho...',
'.ohjjjho....ohjjjhho.',
'ohjkkjho....ohjkkjjho',
'ohhhhhoo....oohhhhhoo'];
const hand=(a,points)=>{
  a.push(poly(points,P.skin0));
  const inner=points.slice(1,-1);
  if(inner.length>=3)a.push(poly(inner,P.skin2));
};
const hammer=(a,pts,head)=>{
  a.push(line(...pts,P.wood0,5),line(...pts,P.wood4,3));
  a.push(poly(head,P.iron0));
  const [x,y]=head[0];a.push(line(x+1,y+1,head[1][0]-1,head[1][1]+1,P.iron4,2));
  a.push(line(head[3][0]+1,head[3][1]-2,head[2][0]-1,head[2][1]-2,P.iron2,2));
};
export function blacksmith(){
  const arms=[
    {head:[17,7],torso:[14,28],arm:[[36,31],[41,31],[46,35],[45,42],[40,45],[37,41]],shaft:[43,36,48,23],hammer:[[41,19],[54,21],[53,29],[40,27]]},
    {head:[17,6],torso:[14,27],arm:[[36,29],[42,27],[46,28],[46,35],[42,39],[37,36]],shaft:[43,31,48,15],hammer:[[41,10],[54,12],[54,20],[40,18]]},
    {head:[16,5],torso:[13,26],arm:[[34,28],[37,23],[42,21],[47,23],[46,29],[42,32],[39,31]],shaft:[43,25,42,7],hammer:[[36,3],[50,3],[50,11],[36,11]]},
    {head:[17,5],torso:[14,26],arm:[[35,27],[39,22],[44,23],[49,29],[48,34],[43,34],[39,30]],shaft:[46,28,55,14],hammer:[[50,9],[62,15],[58,23],[46,17]]},
    {head:[19,10],torso:[15,29],arm:[[36,32],[42,31],[47,34],[53,36],[53,42],[47,43],[42,39],[38,38]],shaft:[48,39,56,47],hammer:[[50,44],[63,44],[63,52],[50,52]],bent:true},
    {head:[19,10],torso:[15,29],arm:[[36,32],[42,32],[47,35],[53,37],[53,43],[47,43],[42,39],[38,39]],shaft:[48,39,56,47],hammer:[[50,45],[63,45],[63,53],[50,53]],bent:true},
    {head:[18,9],torso:[15,29],arm:[[36,32],[42,32],[48,35],[49,41],[44,44],[39,40]],shaft:[46,38,53,34],hammer:[[49,28],[62,31],[60,39],[47,36]]},
    {head:[17,8],torso:[14,28],arm:[[36,31],[41,31],[46,35],[46,42],[41,45],[37,41]],shaft:[43,37,49,27],hammer:[[43,22],[56,24],[55,32],[42,30]]},
  ];
  return arms.map((p,index)=>{
    const a=[e(14,55,34,7,P.ink),g(17,48,boots),g(...p.torso,torso),g(...p.head,p.bent?smithBent:smithHead)];
    a.push(poly([[15,33],[19,32],[23,38],[22,43],[28,43],[28,47],[21,48],[17,44]],P.skin0),poly([[17,34],[20,34],[21,40],[20,43],[25,44],[25,46],[20,46],[18,41]],P.skin3));
    a.push(poly([[p.torso[0]+18,p.torso[1]+3],[p.arm[0][0]+3,p.arm[0][1]-1],[p.arm[1][0]+2,p.arm[1][1]+3],[p.arm[0][0]+1,p.arm[0][1]+7],[p.torso[0]+18,p.torso[1]+8]],P.skin1));
    a.push(line(p.torso[0]+19,p.torso[1]+4,p.arm[0][0]+2,p.arm[0][1]+2,P.skin3,4));
    hand(a,p.arm);a.push(line(p.arm[1][0],p.arm[1][1]+1,p.arm[2][0]-1,p.arm[2][1]+1,P.skin4,2));
    hammer(a,p.shaft,p.hammer);
    a.push(r(25,42,4,2,P.gold2),r(26,42,2,1,P.gold4));
    if(index===4)a.push(line(51,49,43,45,P.fire4,2),line(58,51,60,59,P.fire3),line(52,51,46,57,P.fire5),r(40,43,2,2,P.fire3),r(61,57,2,2,P.fire5));
    if(index===5)a.push(r(42,54,2,2,P.fire4),r(48,60,2,2,P.fire3),r(61,60,1,2,P.fire2));
    return [{name:'Hand-drawn blacksmith poses',ops:a}];
  });
}
const minerHead=[
'......oooooo.......',
'....oottttttoo.....',
'...otuuuvvvuuto....',
'..otuvvvwwvvuuto...',
'.otuvvvwwwwvvuuto..',
'.otuuuuuggguuuuto..',
'otvvvvvgGQGgvvvvto.',
'ottttttgggggttttto.',
'.obbbdddddddcbbo...',
'.obccddfdfdddcbo...',
'.obcdoooddooodbo...',
'.obcdwoddwodddbo...',
'..obcddddfffdbbo...',
'..ohibcccccbiho....',
'..ohijjkjjjiho.....',
'...ohjjkkjiho......',
'....ohjjjiho.......',
'.....ohhiho........'];
const minerBody=[
'...oo......oo......',
'.ooBCBooooBCBoo....',
'oBBCDDDBBBCDDCBo...',
'oBCDDDEDDCDDDCBo...',
'oBCDDDEDDDCDDCBo...',
'.oBCDDDDCDDDCBo....',
'..oBCDDCDDDCBo.....',
'..oBCDDDCDDCBo.....',
'..oBCDoAAoDCBo.....',
'..oBCDoCCoDCBo.....',
'..oBCDoAAoDCBo.....',
'..oBCCCCCCCBo......',
'..oBAggggABBo......',
'..oBBBBBBBBBo......',
'...onnnonnno.......',
'...onmnonmno.......',
'...onmnonmno.......',
'...ohhhohhho.......',
'..ohjjhohjjho......',
'.ohjkkhohjkkho.....',
'.ohhhhooohhhho.....'];
export function miner(){
  const poses=[
    {h:[11,9],b:[12,27],arm:[[26,29],[30,28],[34,32],[33,38],[29,38],[26,34]],shaft:[30,33,37,19],pick:[[29,16],[37,16],[45,23],[37,20],[31,20]]},
    {h:[10,8],b:[11,26],arm:[[25,28],[28,23],[33,22],[36,27],[32,33],[28,33]],shaft:[31,28,31,10],pick:[[21,10],[28,5],[37,8],[42,15],[34,11],[28,10]]},
    {h:[10,8],b:[11,26],arm:[[25,28],[28,24],[34,24],[38,30],[34,35],[29,32]],shaft:[33,29,39,12],pick:[[30,11],[36,8],[43,12],[46,19],[39,15],[35,13]]},
    {h:[13,12],b:[13,29],arm:[[27,31],[32,31],[38,36],[40,41],[36,45],[31,39]],shaft:[34,38,43,46],pick:[[37,40],[42,42],[45,48],[43,54],[42,47],[36,45]]},
    {h:[13,12],b:[13,29],arm:[[27,31],[32,31],[38,36],[40,41],[36,45],[31,39]],shaft:[34,38,43,46],pick:[[37,40],[42,42],[45,48],[43,54],[42,47],[36,45]]},
    {h:[12,10],b:[12,28],arm:[[26,29],[31,29],[35,34],[34,40],[30,41],[27,35]],shaft:[32,36,42,27],pick:[[35,22],[43,23],[47,31],[42,28],[36,26]]},
  ];
  return poses.map((p,i)=>{
    const a=[e(10,48,27,6,P.ink),g(...p.b,minerBody),g(...p.h,minerHead),poly(p.arm,P.teal0),line(p.arm[1][0],p.arm[1][1]+1,p.arm[2][0],p.arm[2][1]+1,P.teal3,3),r(p.shaft[0]-2,p.shaft[1]-2,5,5,P.skin3),line(...p.shaft,P.wood0,4),line(...p.shaft,P.wood4,2),poly(p.pick,P.iron1),line(p.pick[0][0]+1,p.pick[0][1],p.pick[1][0],p.pick[1][1],P.iron4,2)];
    if(i===3)a.push(r(37,51,2,2,P.iron4),r(45,39,2,2,P.iron3));
    if(i===4)a.push(r(33,52,2,2,P.iron3),r(46,37,1,2,P.iron4));
    return [{name:'Hand-drawn mining poses',ops:a}];
  });
}
const merchantHead=[
'......oooooo.......',
'....ooAAAAAAoo.....',
'...oABCCCCCBBAo....',
'..oABCDDDDCCBBAo...',
'.oABCDDDDEDDCBAo...',
'.oABCDDEDDDDCBAo...',
'oABBBBBggBBBBBBAo..',
'oABCCCCGGCCCCCBAo..',
'.obhhddddddhhbo....',
'.obbcddfffddcbo....',
'.obbcdoooddoobo....',
'.obcddwoddwodbo....',
'..obcddfffdddbo....',
'..obbcddffdcbo.....',
'...obcbcccbbo......',
'....obddddbo.......',
'.....obbbbo........'];
const merchantBody=[
'.....oo....oo.......',
'...ooABo..oBAoo.....',
'..oBCDDgoOgDDCBo....',
'.oBCDDDDgOgDDDCBo...',
'oBCDDDEDgOgDEDDCBo..',
'oBCDDDDDgOgDDDDCBo..',
'oBCDDDCDgOgDCDDCBo..',
'.oBCDDCDgOgDCDDCo...',
'..oBCDCDgOgDCDCBo...',
'..oBCDCDgOgDCDCBo...',
'..oBCCCDgOgDCCCBo...',
'..oBCCCCgOgCCCCBo...',
'..oBCCCCgOgCCCCBo...',
'..oBBBBBgOgBBBBBo...',
'..oAAgggggggggAAo...',
'...onnnnonnnno......',
'...onmmnonmmno......',
'...onmmnonmmno......',
'...ohhhhohhhho......',
'..ohjkkhohkkjho.....',
'..ohhhhooohhhho.....'];
export function merchant(){
  return [0,1,2,3].map(i=>{
    const a=[e(11,48,28,6,P.ink),r(22,26,7,9,P.skin0),r(23,26,5,7,P.skin2),g(12,29,merchantBody),g(12,i===1?11:12,merchantHead)];
    if(i===2)a.push(line(21,23,23,23,P.skin0),line(28,23,30,23,P.skin0));
    const hands=[[[14,34],[11,38],[14,44],[18,43],[17,37]],[[14,34],[9,35],[7,31],[3,31],[4,38],[12,43],[17,40]],[[14,34],[10,35],[7,32],[4,33],[6,39],[12,42],[17,40]],[[14,34],[11,38],[14,44],[18,43],[17,37]]];
    a.push(poly(hands[i],P.teal1),line(13,35,11,38,P.teal4,2),r(i===1?2:i===2?3:13,i===1?28:i===2?30:42,5,5,P.skin2),r(i===1?3:i===2?4:14,i===1?28:i===2?30:42,3,2,P.skin4));
    a.push(poly([[27,33],[32,32],[36,36],[38,41],[35,45],[31,43],[30,38]],P.teal1),line(29,34,33,36,P.teal4,3));
    a.push(poly([[33,35],[37,36],[39,43],[35,46],[31,44],[32,40]],P.teal0),line(35,36,37,42,P.teal3,2),r(32,43,5,4,P.skin3),r(31,46,8,3,P.gold1),line(32,46,37,46,P.gold3));
    return [{name:'Hand-drawn merchant greeting',ops:a}];
  });
}
export function fire(){
  const outer=[
    [[5,35],[2,26],[5,18],[8,24],[10,13],[14,5],[14,16],[20,21],[23,12],[27,22],[29,31],[25,36]],
    [[5,35],[2,28],[4,21],[8,27],[10,18],[12,10],[17,2],[15,17],[20,21],[25,15],[24,24],[29,29],[27,35]],
    [[4,35],[2,27],[7,18],[7,10],[13,18],[13,25],[18,12],[22,6],[21,19],[26,22],[29,32],[26,36]],
    [[4,35],[2,29],[5,21],[9,25],[10,16],[13,7],[16,17],[18,21],[23,14],[23,7],[27,21],[29,31],[26,36]],
    [[5,35],[3,27],[5,16],[10,24],[12,17],[11,8],[15,11],[18,22],[22,18],[27,12],[25,24],[29,30],[26,36]],
    [[4,35],[2,28],[6,20],[9,26],[11,17],[15,9],[17,4],[17,19],[21,24],[25,17],[28,26],[28,33],[25,36]],
  ];
  const middle=[
    [[7,34],[6,25],[10,28],[12,20],[14,15],[17,26],[22,25],[24,21],[26,30],[23,35]],
    [[7,34],[5,28],[9,29],[13,19],[15,12],[16,26],[21,27],[25,23],[26,31],[23,35]],
    [[7,34],[5,28],[8,22],[11,28],[15,29],[18,20],[20,16],[21,27],[25,28],[26,33],[22,35]],
    [[7,34],[5,29],[8,26],[11,29],[12,22],[14,16],[17,27],[22,24],[24,19],[25,31],[23,35]],
    [[7,34],[5,26],[9,29],[13,24],[14,18],[17,28],[21,27],[24,23],[26,31],[23,35]],
    [[7,34],[5,29],[9,27],[12,29],[14,20],[16,13],[18,27],[22,29],[25,25],[26,32],[23,35]],
  ];
  const cores=[[[11,34],[10,29],[13,31],[15,25],[19,30],[22,29],[22,35]],[[10,34],[12,27],[14,30],[17,26],[20,31],[23,32],[22,35]],[[10,34],[10,29],[15,31],[18,24],[18,30],[23,31],[22,35]],[[10,34],[12,28],[16,30],[18,27],[21,32],[22,35]],[[10,34],[12,30],[14,25],[17,31],[20,28],[23,34]],[[10,34],[11,28],[14,30],[16,24],[19,31],[22,30],[23,35]]];
  return outer.map((shape,i)=>[{name:'Hand-drawn flame cycle',ops:[e(2,32,28,7,P.fire0),poly(shape,P.fire2),poly(middle[i],P.fire4),poly(cores[i],P.fire5),r(5,36,22,2,P.wood1),line(4,35,12,37,P.wood3,2),line(19,37,27,35,P.wood4,2),r([20,7,28,8,18,24][i],[9,10,13,3,3,8][i],2,2,P.fire3)]}]);
}
