#!/usr/bin/env python
# (512) 線 pv2：場所の座標とマスクの調べ（読むだけ・木を変えない）。
#   使い方：PYTHONUTF8=1 python placemask.py [木の根=このファイルの3つ上] [場所の名前...]
#   出すもの：①名前を挙げた場所ごとの座標・海路用マスクと陸路用マスクで陸か・州の当たりの吸着（App の MapFill.Nearest の写し＝5 環）の結果・島の格子数
#            ②全ての場所の種が捨てられる件数と、同じ格子に乗る組（基線は 3 件・4 組）。
#   マスクは SimCore の landmask.bin（海路用・海岸線を 1 格子収縮）と landmask_land.bin（陸路用）。0.1° 格子 3600×1800・Landmask.cs と同じ式
#   （緯度の添字は (int)((90-lat)/0.1)＝浮動小数のまま〔境の座標はずれる＝格子の中心に置く理由〕）。
import sys, os, json, math
try: sys.stdout.reconfigure(encoding="utf-8")
except Exception: pass
ROOT = sys.argv[1] if len(sys.argv) > 1 and os.path.isdir(sys.argv[1]) else os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
NAMES = [a for a in sys.argv[1:] if not os.path.isdir(a)]
D = os.path.join(ROOT, "PriceOfBread.SimCore")
sea = open(os.path.join(D, "landmask.bin"), "rb").read(); road = open(os.path.join(D, "landmask_land.bin"), "rb").read()
Nx, Ny = 3600, 1800
places = json.load(open(os.path.join(D, "places.json"), encoding="utf-8"))

def bit(b, y, x):
    if y < 0 or y >= Ny: return False
    i = y * Nx + (x % Nx); return bool(b[i >> 3] & (1 << (i & 7)))
def cell(lat, lon): return int((90 - lat) / 0.1), int(math.floor((lon + 180) / 0.1)) % Nx
def clat(y): return round(90 - (y + 0.5) * 0.1, 2)
def clon(x): return round(-180 + (x + 0.5) * 0.1, 2)
def dist(a, b, c, d):
    r = math.radians; h = math.sin(r(c - a) / 2) ** 2 + math.cos(r(a)) * math.cos(r(c)) * math.sin(r(d - b) / 2) ** 2
    return 12742 * math.asin(math.sqrt(h))
def seed(lat, lon):
    """MapFill の種の吸着：自分の格子が陸なら環 0・無ければ半径 5 格子の環を（上の行から・左から）走査して最初の陸。無ければ None＝その種は捨てられる。"""
    y0, x0 = cell(lat, lon)
    if bit(sea, y0, x0): return (y0, x0, 0)
    for r in range(1, 6):
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if abs(dy) != r and abs(dx) != r: continue
                if bit(sea, y0 + dy, x0 + dx): return (y0 + dy, x0 + dx, r)
    return None
def island(y0, x0):
    seen = {(y0, x0)}; st = [(y0, x0)]
    while st and len(seen) < 200000:
        y, x = st.pop()
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dy == dx == 0: continue
                n = (y + dy, (x + dx) % Nx)
                if n not in seen and bit(sea, *n): seen.add(n); st.append(n)
    return len(seen)

byname = {p["name"]: p for p in places}
for n in NAMES:
    p = byname.get(n)
    if p is None: print(f"{n}: places.json に無い"); continue
    y, x = cell(p["lat"], p["lon"]); s = seed(p["lat"], p["lon"])
    info = f"種=捨てられる" if s is None else f"種の格子=({clat(s[0])},{clon(s[1])}) 環={s[2]} 島の格子数={island(s[0], s[1])}"
    print(f"{n}: ({p['lat']},{p['lon']}) 海路用={'陸' if bit(sea, y, x) else '海'} 陸路用={'陸' if bit(road, y, x) else '海'} {info}")
dropped = [p["name"] for p in places if seed(p["lat"], p["lon"]) is None]
snap = {}
for p in places:
    s = seed(p["lat"], p["lon"])
    if s: snap.setdefault((s[0], s[1]), []).append(p["name"])
print("場所", len(places), "・種が捨てられる", len(dropped), dropped)
col = [v for v in snap.values() if len(v) > 1]
print("同じ格子に乗る組", len(col), col)
