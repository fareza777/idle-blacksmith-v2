"""Inspect atlas alpha bounds and write frame metadata; source pixels are never changed."""
import json
from pathlib import Path
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / 'Game/Assets/Resources/Art/HandDrawn'

def components(mask):
    parents, boxes, previous = [], [], []
    def find(n):
        while parents[n] != n:
            parents[n] = parents[parents[n]]
            n = parents[n]
        return n
    for y, row in enumerate(mask):
        changes = np.diff(np.r_[False, row, False].astype(np.int8))
        starts, ends = np.flatnonzero(changes == 1), np.flatnonzero(changes == -1)
        current = []
        for left, right in zip(starts, ends):
            touching = [find(i) for a, b, i in previous if a <= right and b >= left]
            if touching:
                item = min(touching)
                for other in touching:
                    if item != other:
                        parents[other] = item
            else:
                item = len(parents); parents.append(item); boxes.append([left, y, right, y + 1, 0])
            box = boxes[item]
            box[0] = min(box[0], left); box[1] = min(box[1], y)
            box[2] = max(box[2], right); box[3] = max(box[3], y + 1); box[4] += right - left
            current.append((left, right, item))
        previous = current
    merged = {}
    for i, box in enumerate(boxes):
        key = find(i)
        if key not in merged: merged[key] = box.copy()
        else:
            into = merged[key]
            into[0] = min(into[0], box[0]); into[1] = min(into[1], box[1])
            into[2] = max(into[2], box[2]); into[3] = max(into[3], box[3]); into[4] += box[4]
    return [list(map(int, b)) for b in merged.values() if b[4] > 1000]

report = {}
for name, cols, rows in [('workers',6,3),('walkers',8,4),('heroes',4,3),('props',3,3),('idles',2,4)]:
    im = Image.open(folder / (name+'.png')).convert('RGBA')
    bounds = components(np.array(im)[:,:,3] > 80)
    bounds.sort(key=lambda b:b[4], reverse=True)
    if len(bounds)<cols*rows:
        bounds=[]
        for row in range(rows):
            for col in range(cols):
                left,top=round(col*im.width/cols),round(row*im.height/rows)
                right,bottom=round((col+1)*im.width/cols),round((row+1)*im.height/rows)
                box=im.crop((left,top,right,bottom)).getchannel('A').point(lambda a:255 if a>80 else 0).getbbox()
                if not box: raise ValueError(f'{name}: empty frame {row},{col}')
                bounds.append([left+box[0],top+box[1],left+box[2],top+box[3],(box[2]-box[0])*(box[3]-box[1])])
    bounds = bounds[:cols*rows]
    bounds.sort(key=lambda b:(b[1]+b[3])*.5)
    frames=[]
    for row in range(rows):
        row_bounds=sorted(bounds[row*cols:(row+1)*cols],key=lambda b:(b[0]+b[2])*.5)
        for index,(left,top,right,bottom,area) in enumerate(row_bounds):
            if index and left<row_bounds[index-1][2]: left=round((left+row_bounds[index-1][2])*.5)
            if index+1<len(row_bounds) and right>row_bounds[index+1][0]: right=round((right+row_bounds[index+1][0])*.5)
            alpha=np.array(im)[:,:,3]
            band=alpha[round(top+(bottom-top)*.5):round(top+(bottom-top)*.72),left:right]>80
            points=np.flatnonzero(band.sum(axis=0))
            hip=left+(float(np.average(points,weights=band.sum(axis=0)[points])) if len(points) else (right-left)*.5)
            neutral=row_bounds[0]
            frames.append(dict(x=left/im.width,y=1-bottom/im.height,w=(right-left)/im.width,h=(bottom-top)/im.height,pivotX=(hip-left)/(right-left),pivotY=0,referenceWidth=neutral[2]-neutral[0],referenceHeight=neutral[3]-neutral[1]))
    data=dict(columns=cols,rows=rows,frames=frames)
    (folder/(name+'-layout.json')).write_text(json.dumps(data),encoding='utf-8')
    report[name]=dict(size=im.size,components=bounds,frames=len(frames))
(folder/'analysis.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
