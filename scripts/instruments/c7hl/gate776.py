# (776) c7hl：静的な門（S1〜S3）と紙の値。使い方：python gate776.py <作業フォルダ> <旧の変種名> <新の変種名> [--json]
#   入力（runvar.sh と bakevar.sh が書く物）：<作業>/bake/<変種>_land.bin・_sea.bin／<作業>/out/rd_<変種>.tsv・edges_<変種>.tsv
#   箱＝59.0〜61.5N・21〜31E（(765) と同じ）。陸マスクは 3600×1800・LSB 先。
import sys, json, collections
sys.stdout.reconfigure(encoding='utf-8')
W, OLD, NEW = sys.argv[1], sys.argv[2], sys.argv[3]
PLACES = 'D:/PoB_work/wt_c7hl/PriceOfBread.SimCore/places.json'
BOX = (59.0, 61.5, 21.0, 31.0)
H, S, RIGA = 'ヘルシングフォシュ', 'サンクトペテルブルク', 'リガ'
Nx, Ny = 3600, 1800
def mask_diff(a, b):
    A = open(a, 'rb').read(); B = open(b, 'rb').read()
    inb = outb = addin = remin = 0
    for k in range(len(A)):
        x = A[k] ^ B[k]
        if not x: continue
        for j in range(8):
            if not (x >> j) & 1: continue
            i = k * 8 + j; iy, ix = divmod(i, Nx)
            lat = 90 - (iy + 0.5) * 0.1; lon = -180 + (ix + 0.5) * 0.1
            new = (B[k] >> j) & 1
            if BOX[0] <= lat <= BOX[1] and BOX[2] <= lon <= BOX[3]:
                inb += 1; addin += new; remin += 1 - new
            else: outb += 1
    return dict(inbox=inb, add=addin, remove=remin, outbox=outb)
P = {p['name']: (p['lat'], p['lon']) for p in json.load(open(PLACES, encoding='utf-8'))}
def inbox(n):
    la, lo = P.get(n, (0, 0)); return BOX[0] <= la <= BOX[1] and BOX[2] <= lo <= BOX[3]
def load(p):
    R = {}
    for ln in open(p, encoding='utf-8'):
        c = ln.rstrip('\n').split('\t')
        if c[0] == 'a': continue
        R[(c[0], c[1])] = c
    return R
A = load(f'{W}/out/rd_{OLD}.tsv'); B = load(f'{W}/out/rd_{NEW}.tsv')
assert A.keys() == B.keys()
# 列：0 a・1 b・2 portA・3 portB・4 gc・5 land・6〜8 seaGc・9 legA・10 legB・11〜13 seaPort
cols = {'land': [5], 'sea_gc': [6, 7, 8], 'sea_port': [11, 12, 13], 'port': [2, 3], 'legs': [9, 10]}
chg = {k: [0, 0] for k in cols}   # [箱に触れる対, 両端が箱の外]
for key in A:
    ib = inbox(key[0]) or inbox(key[1])
    for k, ix in cols.items():
        if any(A[key][i] != B[key][i] for i in ix): chg[k][0 if ib else 1] += 1
nin = sum(1 for k in A if inbox(k[0]) or inbox(k[1]))
out = dict(old=OLD, new=NEW, pairs=len(A), box_touching_pairs=nin)
out['land_mask'] = mask_diff(f'{W}/bake/{OLD}_land.bin', f'{W}/bake/{NEW}_land.bin')
out['sea_mask'] = mask_diff(f'{W}/bake/{OLD}_sea.bin', f'{W}/bake/{NEW}_sea.bin')
out['changed_pairs_directed'] = {k: dict(box_touching=v[0], both_outside=v[1]) for k, v in chg.items()}
# S1・S3：ヘルシンキの対
def f(x): return float(x)
hs = {}
for (a, b), r in A.items():
    pass
def val(R, a, b, ix): return f(R[(a, b)][ix])
gc_hs = val(B, H, S, 4)
out['S1'] = dict(
    hel_spb=dict(gc=gc_hs, old=val(A, H, S, 5), new=val(B, H, S, 5), ratio_new=val(B, H, S, 5) / gc_hs, ratio_old=val(A, H, S, 5) / gc_hs),
    hel_riga=dict(gc=val(B, H, RIGA, 4), old=val(A, H, RIGA, 5), new=val(B, H, RIGA, 5)))
grow = []   # ヘルシンキの対（有向）で伸びる量（全ての数の列）
worst = 0.0
for key in A:
    if H not in key: continue
    for i in (5, 6, 7, 8, 9, 10, 11, 12, 13):
        d = f(B[key][i]) - f(A[key][i])
        if d > worst: worst = d
        if d > 1.0: grow.append((key, i, f(A[key][i]), f(B[key][i])))
out['S3'] = dict(max_growth_km=worst, grow_over_1km=len(grow), sample=grow[:10])
# 陸の辺・海の辺
EA = [l.rstrip('\n').split('\t') for l in open(f'{W}/out/edges_{OLD}.tsv', encoding='utf-8')][1:]
EB = [l.rstrip('\n').split('\t') for l in open(f'{W}/out/edges_{NEW}.tsv', encoding='utf-8')][1:]
la = [e for e in EA if e[0] == 'land']; lb = [e for e in EB if e[0] == 'land']
sa = [e for e in EA if e[0] == 'sea']; sb = [e for e in EB if e[0] == 'sea']
key = lambda e: (min(e[1], e[2]), max(e[1], e[2]))
da = {key(e): e[3] for e in la}; db = {key(e): e[3] for e in lb}
out['edges'] = dict(land_old=len(la), land_new=len(lb), sea_old=len(sa), sea_new=len(sb), sea_same=(sa == sb),
                    land_added=[list(k) + [db[k]] for k in db if k not in da], land_removed=[list(k) + [da[k]] for k in da if k not in db],
                    land_changed_km=[[*k, da[k], db[k]] for k in da if k in db and da[k] != db[k]])
# 同じ盤面の新÷旧：ヘルシンキの対の陸路
rat = []
for (a, b), r in A.items():
    if a == H and f(r[5]) > 0 and f(B[(a, b)][5]) != f(r[5]): rat.append((b, round(f(r[5]), 1), round(f(B[(a, b)][5]), 1), round(f(r[4]), 1)))
out['hel_land_changes'] = sorted(rat, key=lambda t: t[2] / t[1])[:60]
out['hel_land_changed_count'] = len(rat)
# 判定
m1, m2 = out['land_mask'], out['sea_mask']
g = {}
g['S1a_hel_spb<=1.5gc'] = out['S1']['hel_spb']['new'] <= 1.5 * gc_hs
g['S1b_hel_riga<=1000'] = out['S1']['hel_riga']['new'] <= 1000
g['S2a_outside_pixels_0'] = (m1['outbox'] == 0 and m2['outbox'] == 0)
g['S2b_both_outside_pairs_0'] = all(v[1] == 0 for v in chg.values())
g['S2c_sea_edges_same'] = out['edges']['sea_same']
g['S3_growth<=1km'] = worst <= 1.0
out['gates'] = g
print(json.dumps(out, ensure_ascii=False, indent=1))
