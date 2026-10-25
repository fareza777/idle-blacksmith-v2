"""Read generated alpha and write atlas metadata. Never modify source image pixels."""
import hashlib
import json
import re
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'Game/Assets/Resources/Art/HandDrawn'


def components(mask):
    """Connected opaque runs, with area and bounds; tiny antialias specks are ignored."""
    parents, boxes, previous = [], [], []

    def find(index):
        while parents[index] != index:
            parents[index] = parents[parents[index]]
            index = parents[index]
        return index

    for y, row in enumerate(mask):
        changes = np.diff(np.r_[False, row, False].astype(np.int8))
        starts, ends = np.flatnonzero(changes == 1), np.flatnonzero(changes == -1)
        current = []
        for left, right in zip(starts, ends):
            touching = [find(index) for a, b, index in previous if a <= right and b >= left]
            if touching:
                item = min(touching)
                for other in touching:
                    parents[other] = item
            else:
                item = len(parents)
                parents.append(item)
                boxes.append([left, y, right, y + 1, 0])
            box = boxes[item]
            box[0], box[1] = min(box[0], left), min(box[1], y)
            box[2], box[3] = max(box[2], right), max(box[3], y + 1)
            box[4] += right - left
            current.append((left, right, item))
        previous = current
    merged = {}
    for index, box in enumerate(boxes):
        key = find(index)
        if key not in merged:
            merged[key] = box.copy()
        else:
            into = merged[key]
            into[0], into[1] = min(into[0], box[0]), min(into[1], box[1])
            into[2], into[3] = max(into[2], box[2]), max(into[3], box[3])
            into[4] += box[4]
    return [list(map(int, box)) for box in merged.values() if box[4] >= 700]


content = (ROOT / 'Game/Assets/ForgeEmpire/Core/GameContent.cs').read_text(encoding='utf-8')
recipes = re.findall(r'Recipe\(\d+,\s*\d+,\s*GearSlot\.\w+,\s*"([^"]+)"', content)
names = {
    'equipment': recipes,
    'enemies': ['Cinderroot Stalker', 'Mistbound Ferryman', 'Icebound Sentinel', 'Slag Colossus', 'Void Sentry'],
    'guardians': ['The Thornback', 'The Mire Warden', 'The Frostfang', 'The Cinder Colossus', 'The Starbound Sentinel'],
}
report = {}
for name, labels in names.items():
    source = FOLDER / (name + '.png')
    image = Image.open(source).convert('RGBA')
    alpha = np.array(image)[:, :, 3]
    found = sorted(components(alpha > 100), key=lambda box: box[4], reverse=True)
    if len(found) < 20:
        raise ValueError(f'{name}: only {len(found)} connected sprites; inspect the generated atlas')
    chosen = sorted(found[:20], key=lambda box: (box[1] + box[3]) / 2)
    rows = [sorted(chosen[row * 4:row * 4 + 4], key=lambda box: (box[0] + box[2]) / 2) for row in range(5)]
    frames = []
    for row, boxes in enumerate(rows):
        top_limit = 0 if row == 0 else round((max(box[3] for box in rows[row-1]) + min(box[1] for box in boxes)) / 2)
        bottom_limit = image.height if row == 4 else round((max(box[3] for box in boxes) + min(box[1] for box in rows[row+1])) / 2)
        reference_width, reference_height = boxes[0][2] - boxes[0][0] + 4, boxes[0][3] - boxes[0][1] + 4
        for column, (left, top, right, bottom, area) in enumerate(boxes):
            lower = 0 if column == 0 else round((boxes[column-1][2] + left) / 2)
            upper = image.width if column == 3 else round((right + boxes[column+1][0]) / 2)
            x0, x1 = max(0, lower, left-2), min(image.width, upper, right+2)
            y0, y1 = max(0, top_limit, top-2), min(image.height, bottom_limit, bottom+2)
            if x0 > left or y0 > top or x1 < right or y1 < bottom:
                raise ValueError(f'{name} frame {row*4+column}: neighboring bounds would cut opaque pixels; regenerate with more spacing')
            # Bottom opaque band estimates planted feet/base; metadata remains reviewable per pose.
            band = alpha[max(top, round(bottom-(bottom-top)*.12)):bottom, x0:x1] > 100
            weights = band.sum(axis=0)
            foot_x = x0 + float(np.average(np.arange(len(weights)), weights=weights))
            foot_y = min(y1, bottom)
            index = row*4+column
            frames.append(dict(id=index, region=row, label=labels[index] if name == 'equipment' else labels[row],
                               pose='item' if name == 'equipment' else ['ready','attack','hurt','defeated'][column],
                               x=x0/image.width, y=1-y1/image.height, w=(x1-x0)/image.width, h=(y1-y0)/image.height,
                               pivotX=(foot_x-x0)/(x1-x0), pivotY=(y1-foot_y)/(y1-y0),
                               referenceWidth=reference_width, referenceHeight=reference_height,
                               alphaBounds=[left,top,right,bottom], area=area))
    layout = dict(columns=4, rows=5, width=image.width, height=image.height,
                  sha256=hashlib.sha256(source.read_bytes()).hexdigest(), frames=frames)
    (FOLDER / (name + '-layout.json')).write_text(json.dumps(layout, indent=2)+'\n', encoding='utf-8')
    report[name] = dict(size=list(image.size), connectedComponents=len(found), frames=len(frames),
                        alphaBounds=[frame['alphaBounds'] for frame in frames])
(FOLDER / 'adventure-analysis.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
print(json.dumps({name: {key: value for key, value in data.items() if key != 'alphaBounds'} for name, data in report.items()}))
