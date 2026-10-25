"""Analyze original generated PNG alpha; write UV metadata without editing pixels."""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
FOLDER=ROOT/'Game/Assets/Resources/Art/HandDrawn'

def components(mask,min_area=350):
    parents,boxes,previous=[],[],[]
    def find(i):
        while parents[i]!=i:
            parents[i]=parents[parents[i]];i=parents[i]
        return i
    for y,row in enumerate(mask):
        changes=np.diff(np.r_[False,row,False].astype(np.int8));starts=np.flatnonzero(changes==1);ends=np.flatnonzero(changes==-1);current=[]
        for left,right in zip(starts,ends):
            touching=[find(i) for a,b,i in previous if a<=right and b>=left]
            if touching:
                i=min(touching)
                for other in touching:parents[other]=i
            else:i=len(parents);parents.append(i);boxes.append([left,y,right,y+1,0])
            box=boxes[i];box[0]=min(box[0],left);box[1]=min(box[1],y);box[2]=max(box[2],right);box[3]=max(box[3],y+1);box[4]+=right-left;current.append((left,right,i))
        previous=current
    merged={}
    for i,box in enumerate(boxes):
        key=find(i)
        if key not in merged:merged[key]=box.copy()
        else:
            m=merged[key];m[0]=min(m[0],box[0]);m[1]=min(m[1],box[1]);m[2]=max(m[2],box[2]);m[3]=max(m[3],box[3]);m[4]+=box[4]
    return [list(map(int,b)) for b in merged.values() if b[4]>=min_area]

def analyze(name,rows,kind,columns=4):
    source=FOLDER/(name+'.png');img=Image.open(source).convert('RGBA');alpha=np.asarray(img)[:,:,3]
    found=sorted(components(alpha>100,30 if kind=='item' else 350),key=lambda b:b[4],reverse=True);count=rows*columns
    if len(found)<count:raise ValueError(f'{name}: expected {count} sprites, found {len(found)}; inspect and regenerate')
    if kind=='item':
        # Twin blades are two disconnected silhouettes belonging to ONE grid cell.
        # Aggregate every component in that cell rather than dropping the smaller blade.
        cells=[[] for _ in range(count)]
        for box in found:
            c=min(columns-1,int((box[0]+box[2])*.5/img.width*columns));r=min(rows-1,int((box[1]+box[3])*.5/img.height*rows));cells[r*columns+c].append(box)
        if any(not cell for cell in cells):raise ValueError(f'{name}: empty equipment cell; inspect item ordering')
        chosen=[[min(b[0] for b in cell),min(b[1] for b in cell),max(b[2] for b in cell),max(b[3] for b in cell),sum(b[4] for b in cell)] for cell in cells]
        grid=[chosen[r*columns:r*columns+columns] for r in range(rows)]
    else:
        chosen=sorted(found[:count],key=lambda b:(b[1]+b[3])/2)
        grid=[sorted(chosen[r*columns:r*columns+columns],key=lambda b:(b[0]+b[2])/2) for r in range(rows)]
    frames=[]
    for r,boxes in enumerate(grid):
        for c,(left,top,right,bottom,area) in enumerate(boxes):
            xmin=0 if c==0 else round((boxes[c-1][2]+left)/2)
            xmax=img.width if c==columns-1 else round((right+boxes[c+1][0])/2)
            ymin=0 if r==0 else round((grid[r-1][c][3]+top)/2)
            ymax=img.height if r==rows-1 else round((bottom+grid[r+1][c][1])/2)
            if xmin>left or xmax<right or ymin>top or ymax<bottom:raise ValueError(f'{name} frame {r*columns+c}: overlap would cut opaque art; regenerate with larger gutters')
            x0,x1=max(xmin,left-2),min(xmax,right+2);y0,y1=max(ymin,top-2),min(ymax,bottom+2)
            band=alpha[max(top,round(bottom-(bottom-top)*.10)):bottom,x0:x1]>100;weights=band.sum(axis=0)
            feet=x0+float(np.average(np.arange(len(weights)),weights=weights)) if weights.sum() else (left+right)/2
            ref=grid[0][0] if kind=='hero' else boxes[0]
            frames.append(dict(id=r*columns+c,x=x0/img.width,y=1-y1/img.height,w=(x1-x0)/img.width,h=(y1-y0)/img.height,pivotX=(feet-x0)/(x1-x0),pivotY=(y1-bottom)/(y1-y0),referenceWidth=ref[2]-ref[0]+4,referenceHeight=ref[3]-ref[1]+4,alphaBounds=[left,top,right,bottom],area=area))
    data=dict(columns=columns,rows=rows,width=img.width,height=img.height,sha256=hashlib.sha256(source.read_bytes()).hexdigest(),frames=frames)
    (FOLDER/(name+'-layout.json')).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
    return dict(name=name,frames=count,size=list(img.size),components=len(found))

if __name__=='__main__':
    import sys
    names=sys.argv[1:]
    sheets=[('hero-'+hero,5,'hero',4) for hero in ['rowan','lyra','kael','mira','toren','edda']]+[('enemy-'+str(r),8,'enemy',4) for r in range(6)]+[('gear-'+str(r),5,'item',4) for r in range(5)]+[('boss-'+str(r),4,'enemy',4) for r in range(3)]+[('people',2,'item',4),('landmarks',3,'item',4),('workers',3,'person',6),('walkers',4,'person',8),('idles',4,'person',2),('heroes',3,'person',4)]
    report=[];errors=[]
    for name,rows,kind,columns in sheets:
        if names and name not in names:continue
        if not (FOLDER/(name+'.png')).exists():continue
        try:report.append(analyze(name,rows,kind,columns))
        except ValueError as ex:errors.append(str(ex))
    print(json.dumps(dict(sheets=report,errors=errors),indent=2))
    if errors:raise SystemExit(1)
