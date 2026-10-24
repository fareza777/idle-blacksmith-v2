export const P = {
  ink:'#171b27', deep:'#212532', slate:'#303646', stone0:'#3e4352', stone1:'#525561', stone2:'#6b6971', stone3:'#85808a', stone4:'#a49b9d',
  wood0:'#302831', wood1:'#493339', wood2:'#68443b', wood3:'#885b43', wood4:'#ae7950', wood5:'#d3a069', wood6:'#efd29a',
  iron0:'#242e3d', iron1:'#354756', iron2:'#4d6773', iron3:'#73919a', iron4:'#a0b9bb', iron5:'#d3e1d5',
  teal0:'#173c42', teal1:'#23555a', teal2:'#337574', teal3:'#50a096', teal4:'#80c4ae', teal5:'#b8ddbe',
  fire0:'#6b3036', fire1:'#a44537', fire2:'#d9693c', fire3:'#f39348', fire4:'#ffc76a', fire5:'#fff0b1',
  skin0:'#6e413c', skin1:'#a66248', skin2:'#c98258', skin3:'#e7a775', skin4:'#ffd4a0',
  cloth0:'#2c3044', cloth1:'#424760', cloth2:'#606780', cloth3:'#858da1',
  gold0:'#6d503a', gold1:'#a47a44', gold2:'#d8ae61', gold3:'#f3d78f', gold4:'#fff0bc',
};
export const colors = ['#00000000', ...Object.values(P)];
export const r=(x,y,w,h,c)=>({kind:'rect',rect:{x,y,width:w,height:h},color:c,fill:c});
export const box=(x,y,w,h,c)=>({kind:'rect',rect:{x,y,width:w,height:h},color:c});
export const e=(x,y,w,h,c)=>({kind:'ellipse',rect:{x,y,width:w,height:h},color:c,fill:c});
export const line=(x1,y1,x2,y2,c,t=1)=>({kind:'line',from:{x:x1,y:y1},to:{x:x2,y:y2},color:c,thickness:t});
export const poly=(points,c)=>({kind:'polyline',points:points.map(([x,y])=>({x,y})),closed:true,fill:c,color:c});
export const grid=(x,y,rows,legend)=>({kind:'grid',x,y,rows,legend,transparent:'skip'});
