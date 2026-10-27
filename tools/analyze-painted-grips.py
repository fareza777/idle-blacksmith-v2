"""Normalize inspected hand coordinates and validate original alpha; never edit PNG pixels."""
import hashlib
import json
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[1]
folder=root/'Game/Assets/Resources/Art/HandDrawn'
source=json.loads((root/'ArtSource/painted-grips-source.json').read_text(encoding='utf-8'))
expected={s for hero in ['rowan','lyra','kael','mira','toren','edda'] for s in ['hero-'+hero]+['worn-'+hero+'-'+f for f in ['mail','leather','robe']]}
assert set(source['sheets'])==expected,'All24 original body sheets are required'
maps=[]
for name,data in sorted(source['sheets'].items()):
    path=folder/(name+'.png');sha=hashlib.sha256(path.read_bytes()).hexdigest()
    assert sha==data['sha256'],name+': source painting changed'
    layout=json.loads((folder/(name+'-layout.json')).read_text(encoding='utf-8'))
    image=Image.open(path);assert image.mode=='RGBA'
    assert len(data['points'])==20 and len(layout['frames'])==20
    frames=[]
    for index,(point,frame) in enumerate(zip(data['points'],layout['frames'])):
        assert len(point)==4,name+': incomplete palm pair'
        left=frame['x']*image.width;top=(1-frame['y']-frame['h'])*image.height
        width=frame['w']*image.width;height=frame['h']*image.height
        values=[];alphas=[]
        for x,y in [point[:2],point[2:]]:
            assert left<=x<=left+width and top<=y<=top+height,f'{name}/{index}: palm outside crop'
            alpha=image.getpixel((x,y))[3];assert alpha>=100,f'{name}/{index}: palm at transparent pixel {x},{y}'
            values.extend([round((x-left)/width,8),round((y-top)/height,8)]);alphas.append(alpha)
        frames.append(dict(x=values[0],y=values[1],offX=values[2],offY=values[3],alphaPrimary=alphas[0],alphaOffhand=alphas[1]))
    maps.append(dict(name=name,sha256=sha,frames=frames))
(folder/'painted-grips.json').write_text(json.dumps(dict(sheets=maps),indent=2)+'\n',encoding='utf-8')
print('Validated24 original sheets,480 pose pairs and960 painted palm points; wrote metadata only.')
