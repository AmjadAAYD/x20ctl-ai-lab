"""Builds the two handheld grips of every colour each controller is sold in, from the app's own front art
(src/assets/controllers/<model>/native or finishes/<id>/front-full.png, all of one model in the same frame): cut down the
middle, what the screen's housing would cut in half painted out, scaled to the agent's design canvas. Writes the grips and
a JSON table (where each model's controls land, each colour's shell swatch) for handheld-body.py and the C# table.

usage: python handheld-grips.py CONTROLLERS_DIR OUT_DIR > table.json"""
import sys, json
import numpy as np, cv2
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
GRIP_H, INNER, TOP, RIGHT = 560, 430, 168, 1310
RIM = 398                                             # where the screen's housing starts covering the left grip

# per model, in the art's own pixels: what the housing's edge would cut in half (circles x,y,r / rects x0,y0,x1,y1 /
# flat fills x0,y0,x1,y1,rgb), mirrored about the controller's centre for the right grip
ERASE = {
    'x20_pro': [('f', 640, 112, 840, 238, (16, 16, 19)), ('r', 700, 285, 775, 318), ('r', 702, 312, 775, 358), ('r', 702, 538, 775, 592)],
    'x20': [('c', 735, 255, 52), ('c', 742, 122, 38), ('r', 698, 688, 775, 714)],
    'x05_pro': [('c', 762, 140, 44)],
    'x10': [('c', 766, 160, 56), ('r', 688, 702, 790, 738)],
    'd10': [('c', 766, 170, 46), ('r', 698, 305, 790, 357), ('c', 766, 640, 36)],
    'x15': [('c', 766, 135, 50), ('r', 688, 258, 780, 302), ('r', 688, 618, 780, 662)],
    'dune': [('f', 688, 108, 830, 232, (16, 16, 19)), ('c', 720, 490, 36)],
}

def clean(img, items, c):
    bgr = cv2.cvtColor(img[:, :, :3], cv2.COLOR_RGB2BGR)
    mask = np.zeros(img.shape[:2], np.uint8); fills = []
    for it in items:
        for mirror in (False, True):
            m = (lambda x: 2 * c - x) if mirror else (lambda x: x)
            if it[0] == 'c': cv2.circle(mask, (int(m(it[1])), int(it[2])), int(it[3]), 255, -1)
            else:
                x0, x1 = sorted((m(it[1]), m(it[3])))
                if it[0] == 'r': cv2.rectangle(mask, (int(x0), int(it[2])), (int(x1), int(it[4])), 255, -1)
                else: fills.append((int(x0), int(it[2]), int(x1), int(it[4]), it[5]))
    mask &= (img[:, :, 3] > 200).astype(np.uint8) * 255
    img[:, :, :3] = cv2.cvtColor(cv2.inpaint(bgr, mask, 11, cv2.INPAINT_TELEA), cv2.COLOR_BGR2RGB)
    for x0, y0, x1, y1, col in fills:
        reg = img[y0:y1, x0:x1]; on = reg[:, :, 3] > 200; reg[on, :3] = col
    return img

def centre_of(path):
    a = np.array(Image.open(path).convert('RGBA'))[:, :, 3]; ys, xs = np.where(a > 128)
    return (xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2, xs.max() - xs.min()

finishes = json.load(open(f'{src}/finishes.json'))
shapes = json.load(open(f'{src}/control-shapes.json'))['models']
table = {'models': {}, 'finishes': {}, 'default': finishes.get('default', {})}
for model, colours in finishes['models'].items():
    native = np.array(Image.open(f'{src}/{model}/native/front-full.png').convert('RGBA'))
    ys, xs = np.where(native[:, :, 3] > 20)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    c = (x0 + x1) / 2; s = GRIP_H / (y1 - y0 + 1); cut = int(round(c))
    lw, rw = round((cut - x0) * s), round((x1 + 1 - cut) * s)
    lx = INNER - lw
    L = lambda x, y: (round(lx + (x - x0) * s, 1), round(TOP + (y - y0) * s, 1))
    R = lambda x, y: (round(RIGHT + (x - cut) * s, 1), round(TOP + (y - y0) * s, 1))
    def mid(b): return b[0] + b[2] / 2, b[1] + b[3] / 2
    f = shapes[model]['front']
    lsx, lsy, lsw = centre_of(f'{src}/{model}/native/stick-l.png'); rsx, rsy, _ = centre_of(f'{src}/{model}/native/stick-r.png')
    dpx = np.mean([mid(f[k]['bounds'])[0] for k in ('DPAD_LEFT', 'DPAD_RIGHT')]); dpy = np.mean([mid(f[k]['bounds'])[1] for k in ('DPAD_UP', 'DPAD_DOWN')])
    arm = (mid(f['DPAD_DOWN']['bounds'])[1] - mid(f['DPAD_UP']['bounds'])[1]) / 2
    table['models'][model] = dict(left=lx, right=RIGHT, width=lw, rightWidth=rw, scale=round(s, 4), ls=L(lsx, lsy), rs=R(rsx, rsy), dp=L(dpx, dpy),
                                  a=R(*mid(f['A']['bounds'])), b=R(*mid(f['B']['bounds'])), x=R(*mid(f['X']['bounds'])), y=R(*mid(f['Y']['bounds'])),
                                  eye=round(lsw * s * .62, 1), arm=round(arm * s, 1), button=round(f['A']['bounds'][2] / 2 * s, 1))
    for colour in colours:
        fid = colour['id']
        path = f'{src}/{model}/native/front-full.png' if fid == 'original' else f'{src}/{model}/finishes/{fid}/front-full.png'
        img = clean(np.array(Image.open(path).convert('RGBA')), ERASE.get(model, []), c)
        im = Image.fromarray(img).crop((x0, y0, x1 + 1, y1 + 1))
        big = im.resize((round(im.width * s), GRIP_H), Image.LANCZOS)
        k = f'{model}-{fid}'
        big.crop((0, 0, lw, GRIP_H)).save(f'{out}/{k}-left.png'); big.crop((lw, 0, big.width, GRIP_H)).save(f'{out}/{k}-right.png')
        table['finishes'][k] = dict(model=model, name=colour['name'], shell=colour['swatch'], left=(lx, TOP, lw), right=(RIGHT, TOP, big.width - lw))
print(json.dumps(table, indent=1))
