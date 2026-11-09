"""Joins a model's two photo grips (from handheld-grips.py) into one body: a housing round the agent's screen made of the
grips' own plastic, flowing into them with rounded joins, lit from above like the photo, with a black glass well for the
screen. Writes {key}-body.png at the agent's design size (1740 x 800).

usage: python handheld-body.py GRIPS_DIR OUT_DIR GRIPS_JSON [PREVIEW_DIR]  (GRIPS_JSON: what handheld-grips.py printed)"""
import sys, json
import numpy as np, cv2
from PIL import Image

grips, out, table = sys.argv[1], sys.argv[2], json.load(open(sys.argv[3]))
preview = sys.argv[4] if len(sys.argv) > 4 else None
W, H = 1740, 800
SX, SY, SW, SH, F = 420, 104, 900, 548, 22          # the screen and the housing's rim round it
RX0, RY0, RX1, RY1, RR = SX - F, SY - F, SX + SW + F, SY + SH + F, 44
rng = np.random.default_rng(7)
# the housing is always the controller's own shell colour (owner, 8 Nov 2026: "same housing matches shell")
SHELL = {}

def rounded(mask, x0, y0, x1, y1, r, v=255):
    cv2.rectangle(mask, (x0 + r, y0), (x1 - r, y1), v, -1); cv2.rectangle(mask, (x0, y0 + r), (x1, y1 - r), v, -1)
    for cx, cy in ((x0 + r, y0 + r), (x1 - r, y0 + r), (x0 + r, y1 - r), (x1 - r, y1 - r)): cv2.circle(mask, (cx, cy), r, v, -1, cv2.LINE_AA)

def profile(img, x0, x1):
    """The plastic's colour row by row, from a band of the grip just outside the housing (features ignored, smoothed)."""
    band = img[:, x0:x1]; a = band[:, :, 3] > 220
    rows = np.full((H, 3), np.nan)
    for y in range(H):
        px = band[y][a[y]][:, :3]
        if len(px) >= 4:
            lum = px.mean(1); keep = px[(lum >= np.percentile(lum, 25)) & (lum <= np.percentile(lum, 85))]
            rows[y] = np.median(keep if len(keep) else px, 0)
    ok = ~np.isnan(rows[:, 0]); idx = np.arange(H)
    for c in range(3): rows[:, c] = np.interp(idx, idx[ok], rows[ok, c])
    rows = cv2.GaussianBlur(rows.reshape(H, 1, 3).astype(np.float32), (1, 0), sigmaX=1, sigmaY=10).reshape(H, 3)
    # how much to trust each row: fully where the grip is there, fading out above and below it
    trust = cv2.GaussianBlur(ok.astype(np.float32).reshape(H, 1), (1, 0), sigmaX=1, sigmaY=12).reshape(H)
    return rows, trust

for key, t in table.get('finishes', table).items():
    canvas = np.zeros((H, W, 4), np.uint8)
    for side in ('left', 'right'):
        g = np.array(Image.open(f'{grips}/{key}-{side}.png').convert('RGBA'))
        x, y = t[side][0], t[side][1]
        a0, b0 = max(0, x), min(W, x + g.shape[1])
        part = g[:, a0 - x:b0 - x].astype(np.float32); al = part[:, :, 3:4] / 255
        dst = canvas[y:y + g.shape[0], a0:b0].astype(np.float32)
        dst[:, :, :3] = part[:, :, :3] * al + dst[:, :, :3] * (1 - al); dst[:, :, 3:] = np.maximum(dst[:, :, 3:], part[:, :, 3:])
        canvas[y:y + g.shape[0], a0:b0] = dst.astype(np.uint8)
    grip_a = canvas[:, :, 3].copy()

    # the housing's outline: the rim's rounded rectangle, joined to the grips with fillets (a closing, kept to the joins)
    rim = np.zeros((H, W), np.uint8); rounded(rim, RX0, RY0, RX1, RY1, RR)
    union = np.maximum(rim, (grip_a > 128).astype(np.uint8) * 255)
    k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (71, 71))
    closed = cv2.morphologyEx(union, cv2.MORPH_CLOSE, k)
    near = np.zeros((H, W), np.uint8); near[:, RX0 - 60:RX0 + 40] = 255; near[:, RX1 - 40:RX1 + 60] = 255
    body = np.maximum(rim, cv2.bitwise_and(closed, near))
    body = cv2.GaussianBlur(body, (3, 3), 0)                          # anti-aliased edge

    # its plastic: the shell's own colour, blended into the grip's actual colour row by row where the two meet
    base = np.array(SHELL.get(key) or [int(t['shell'][i:i + 2], 16) for i in (1, 3, 5)], np.float32)
    plastic = np.broadcast_to(base, (H, W, 3)).copy()
    (L, Lok), (R, Rok) = profile(canvas, RX0 - 22, RX0 - 4), profile(canvas, RX1 + 4, RX1 + 22)
    x = np.arange(W)
    wl = np.clip(1 - (x - RX0) / 70, 0, 1)[None, :, None] * Lok[:, None, None]
    wr = np.clip(1 - (RX1 - x) / 70, 0, 1)[None, :, None] * Rok[:, None, None]
    plastic = plastic * (1 - wl) + L[:, None, :] * wl
    plastic = plastic * (1 - wr) + R[:, None, :] * wr
    # lit from above as in the photos: brighter at the top, a soft fall-off below, a sheen along the upper edge
    ys = np.arange(H)[:, None, None]
    plastic *= 1.10 - .22 * np.clip((ys - RY0) / (RY1 - RY0), 0, 1)
    inner = cv2.erode(body, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)))
    edge = cv2.GaussianBlur((body.astype(np.float32) - inner) / 255, (7, 7), 0)
    up = np.clip(1.2 - (np.arange(H) - RY0) / 160, 0, 1)[:, None]
    plastic += (edge * up * 70)[:, :, None]
    down = np.clip((np.arange(H) - (RY1 - 120)) / 120, 0, 1)[:, None]
    plastic -= (edge * down * 45)[:, :, None]
    plastic += rng.normal(0, 2.2, plastic.shape)                      # the grain of real plastic

    # lay the housing in, fading in over the grip where the rim crosses it so the join never shows as a line
    fade = np.ones((H, W), np.float32)
    ramp = 14
    for x in range(RX0, RX0 + ramp): fade[:, x] = (x - RX0) / ramp
    for x in range(RX1 - ramp, RX1 + 1): fade[:, x] = (RX1 - x) / ramp
    on_grip = grip_a > 128
    cover = (body.astype(np.float32) / 255) * np.where(on_grip & (rim == 0), 0, 1) * np.where(on_grip, fade, 1)
    out_rgb = canvas[:, :, :3].astype(np.float32) * (1 - cover[:, :, None]) + plastic * cover[:, :, None]
    out_a = np.maximum(canvas[:, :, 3], body)

    # the screen's well: black glass, a dark bevel into it and a hairline of light on its lower lip
    well = np.zeros((H, W), np.uint8); rounded(well, SX - 10, SY - 10, SX + SW + 10, SY + SH + 10, 32)
    bevel = np.zeros((H, W), np.uint8); rounded(bevel, SX - 14, SY - 14, SX + SW + 14, SY + SH + 14, 36)
    bevel = cv2.GaussianBlur(bevel, (9, 9), 0).astype(np.float32) / 255
    out_rgb *= (1 - .45 * bevel)[:, :, None]
    glass = np.zeros((H, W, 3), np.float32)
    gy = np.clip((np.arange(H) - SY) / SH, 0, 1)[:, None]
    glass[:] = (14 + 6 * (1 - gy))[:, :, None]
    wa = cv2.GaussianBlur(well, (3, 3), 0).astype(np.float32)[:, :, None] / 255
    out_rgb = out_rgb * (1 - wa) + glass * wa
    lip = np.zeros((H, W), np.uint8); rounded(lip, SX - 10, SY - 10, SX + SW + 10, SY + SH + 10, 32)
    lip = (lip.astype(np.float32) - cv2.erode(lip, np.ones((3, 3), np.uint8))) / 255
    lip *= np.clip((np.arange(H) - (SY + SH - 40)) / 40, 0, 1)[:, None]
    out_rgb += lip[:, :, None] * 40

    res = np.dstack([np.clip(out_rgb, 0, 255), out_a]).astype(np.uint8)
    Image.fromarray(res).save(f'{out}/{key}-body.png', optimize=True)
    if preview:
        bg = Image.new('RGBA', (W, H), (10, 16, 34, 255)); bg.alpha_composite(Image.fromarray(res))
        bg.convert('RGB').save(f'{preview}/{key}-body.png')
print('ok')
