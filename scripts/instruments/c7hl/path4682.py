# (776) c7hl：worldmap.svg の path4682（フィンランドの本土の多角形）の頂点の読み書きと、箱の中の 0.1° 格子の被覆の概算（純 Python・numpy なし）。
#   頂点番号 k＝d の中の座標の組を先頭から数えた番号（M の 1 点が k=0・C 1 本が 3 点〈制御点 2・端点 1〉＝端点は k%3==0）。
import re
VBW, VBH = 2752.766, 1537.631
lon_of = lambda x: -166.4 + x / VBW * 360
lat_of = lambda y: 98 - y / VBH * 198
y_of = lambda la: (98 - la) * VBH / 198
def read_block(svg_text):
    i = svg_text.index('id="path4682"'); st = svg_text.rfind('<path', 0, i); en = svg_text.index('/>', i)
    blk = svg_text[st:en]; m = re.search(r' d="([^"]*)"', blk)
    return st, en, blk, m.group(1)
TOK = re.compile(r'[MCLZ]|-?\d+\.\d+,-?\d+\.\d+')
def vertices(d):
    out = []
    for t in TOK.findall(d):
        if t in 'MCLZ': continue
        x, y = map(float, t.split(',')); out.append((x, y))
    return out
def fmt(v):
    s = f"{v:.2f}"
    return s
def rewrite(d, newlat):
    """newlat＝{k: 緯度}。その頂点の y だけを書き換える（x と他の頂点の文字列は触らない）。"""
    k = [0]
    def sub(mm):
        kk = k[0]; k[0] += 1
        if kk not in newlat: return mm.group(0)
        return f"{mm.group(1)},{fmt(y_of(newlat[kk]))}"
    return re.sub(r'(-?\d+\.\d+),(-?\d+\.\d+)', sub, d)
def flatten(pts, n=24):
    # pts[0]＝M・以降 3 点ずつ C
    poly = [pts[0]]
    for i in range(1, len(pts) - 2, 3):
        p0 = poly[-1]; p1, p2, p3 = pts[i], pts[i + 1], pts[i + 2]
        for j in range(1, n + 1):
            t = j / n; u = 1 - t
            poly.append((u**3*p0[0] + 3*u*u*t*p1[0] + 3*u*t*t*p2[0] + t**3*p3[0], u**3*p0[1] + 3*u*u*t*p1[1] + 3*u*t*t*p2[1] + t**3*p3[1]))
    return poly
def inside(poly, x, y):
    c = False; n = len(poly); j = n - 1
    for i in range(n):
        xi, yi = poly[i]; xj, yj = poly[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi: c = not c
        j = i
    return c
def coverage_cells(d, lat0, lat1, lon0, lon1, ss=6):
    """0.1° 格子（行＝int((90-lat)/0.1)・列＝int((lon+180)/0.1)）の被覆率 {(iy,ix): 0..1} を箱の中だけ返す。"""
    poly = flatten(vertices(d))
    out = {}
    for iy in range(int((90 - lat1) / 0.1), int((90 - lat0) / 0.1) + 1):
        for ix in range(int((lon0 + 180) / 0.1), int((lon1 + 180) / 0.1) + 1):
            hit = 0
            for a in range(ss):
                for b in range(ss):
                    la = 90 - (iy + (a + 0.5) / ss) * 0.1; lo = -180 + (ix + (b + 0.5) / ss) * 0.1
                    # SVG の x,y へ
                    x = (lo + 166.4) / 360 * VBW; y = (98 - la) / 198 * VBH
                    if inside(poly, x, y): hit += 1
            out[(iy, ix)] = hit / (ss * ss)
    return out
