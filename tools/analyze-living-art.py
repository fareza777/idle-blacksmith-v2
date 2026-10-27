"""Read selected original RGBA art and write crop/foot metadata; never alter PNGs."""
import hashlib
import json
import sys
import runpy
from pathlib import Path
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parents[1]
folder=root/'Game/Assets/Resources/Art/HandDrawn'
specs={'workpieces-metal':(5,4),'workpieces-fine':(5,4),'forge-fx':(4,3),'held-weapons':(4,2)}
specs.update({f'foes-{region}-{pair}':(3,4) for region in range(6) for pair in range(4)})
components=runpy.run_path(str(root/'tools/analyze-chronicle-art.py'))['components']

def analyze(name):
    columns,rows=specs[name]
    source=folder/(name+'.png')
    picture=Image.open(source)
    if picture.mode!='RGBA':raise ValueError(f'{name}: genuine RGBA required')
    if name!='forge-fx':
        # Use the established opaque-silhouette analysis: it rejects intersecting
        # rectangular crops, with per-row reference scale for creature poses.
        reference=runpy.run_path(str(root/'tools/analyze-chronicle-art.py'))
        info=reference['analyze'](name,rows,'enemy' if name.startswith('foes-') else 'item',columns)
        layoutPath=folder/(name+'-layout.json');data=json.loads(layoutPath.read_text(encoding='utf-8'))
        for index,frame in enumerate(data['frames']):
            frame['gutterClear']=True
            if name.startswith('foes-'):
                ready=data['frames'][index//6*6]
                frame['referenceWidth']=ready['referenceWidth'];frame['referenceHeight']=ready['referenceHeight']
        layoutPath.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
        return info
    alpha=np.asarray(picture)[:,:,3]
    threshold=8 if name=='forge-fx' else 32
    # Generated cells have regular reading order but their centers are not mathematically
    # uniform. Group whole alpha components before deriving crop gaps, rather than cutting
    # a valid figure at an arbitrary equal-cell border.
    frames=[]
    for row in range(rows):
        for col in range(columns):
            left,right=round(col*picture.width/columns),round((col+1)*picture.width/columns)
            top,bottom=round(row*picture.height/rows),round((row+1)*picture.height/rows)
            cell=alpha[top:bottom,left:right]
            ys,xs=np.nonzero(cell>threshold)
            if len(xs)<13:raise ValueError(f'{name} frame {row*columns+col}: empty')
            gutter=not (np.any(cell[:2]>threshold) or np.any(cell[-2:]>threshold) or np.any(cell[:,:2]>threshold) or np.any(cell[:,-2:]>threshold))
            if not gutter:raise ValueError(f'{name} frame {row*columns+col}: opaque backdrop or neighbor at cell edge; inspect/regenerate')
            x0,x1=max(left,left+int(xs.min())-2),min(right,left+int(xs.max())+3)
            y0,y1=max(top,top+int(ys.min())-2),min(bottom,top+int(ys.max())+3)
            opaqueBottom=top+int(ys.max())+1
            band=alpha[max(y0,opaqueBottom-max(2,round((y1-y0)*.10))):opaqueBottom,x0:x1]>threshold
            weights=band.sum(axis=0)
            feet=x0+float(np.average(np.arange(len(weights)),weights=weights)) if weights.sum() else (x0+x1)/2
            ready=frames[row*columns] if col else None
            frames.append(dict(id=row*columns+col,x=x0/picture.width,y=1-y1/picture.height,w=(x1-x0)/picture.width,h=(y1-y0)/picture.height,
                pivotX=(feet-x0)/(x1-x0),pivotY=(y1-opaqueBottom)/(y1-y0),referenceWidth=ready['referenceWidth'] if ready else x1-x0,
                referenceHeight=ready['referenceHeight'] if ready else y1-y0,area=int(len(xs)),gutterClear=gutter,
                alphaBounds=[left+int(xs.min()),top+int(ys.min()),left+int(xs.max())+1,opaqueBottom]))
    result=dict(columns=columns,rows=rows,width=picture.width,height=picture.height,sha256=hashlib.sha256(source.read_bytes()).hexdigest(),frames=frames)
    (folder/(name+'-layout.json')).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    return dict(name=name,frames=len(frames),size=picture.size,minBodyHeight=min(f['referenceHeight'] for f in frames))

if __name__=='__main__':
    names=sys.argv[1:] or list(specs)
    reports=[];errors=[]
    for name in names:
        try:reports.append(analyze(name))
        except (ValueError,FileNotFoundError) as error:errors.append(str(error))
    print(json.dumps(dict(sheets=reports,errors=errors),indent=2))
    if errors:raise SystemExit(1)
