# (1152) c113lc 門の読み手（事前登録 measurements/m1152_20261115_prereg_lc.txt）。読むだけ・書くのは標準出力だけ。
#   python -X utf8 -I gates_lc.py <ins の生> <種の始め> <種の終わり> [G-C1a の床]
# 読む表：disccontact_s（機関車の行・運び手〈機関車〉）・railspend_s（monthsWorldOnly・unknownGovRailAddCap・pre の機関車の払い）。列は名で引く（★(749)）。
# G-C1a：発見の前の国が知る国から買った機関車の額（disccontact_s の row＝機関車・carrier＝機関車 の valuePerQuarter の和）≧ 床。
# G-C2 ：unknownGovRailAddCap の和＝0（許し 1e-9）・正の行の数。新しい 2 列が無い生（基線・ヌル腕）は〈列が無い〉と書く。
# 記録：monthsWorldOnly の和・その行の pre の機関車の払い（建設・保守）・知る国の数 q400 の中央は known_count.py で別に読む。
import sys, os, statistics
from collections import defaultdict

ins, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
floor = float(sys.argv[4]) if len(sys.argv) > 4 else float('nan')

def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if len(c) < len(h): continue
            yield ix, c

T = defaultdict(float); per = []
has_new = None
for s in range(s0, s1 + 1):
    cc = os.path.join(ins, f'disccontact_s{s}.tsv'); rs = os.path.join(ins, f'railspend_s{s}.tsv')
    if not os.path.exists(cc) or not os.path.exists(rs): print(f'種 {s}：表が無い'); continue
    g1 = 0.0
    for ix, c in rd(cc):
        if c[ix['row']] == '機関車' and c[ix['carrier']] == '機関車': g1 += float(c[ix['valuePerQuarter']])
    g2 = 0.0; g2rows = 0; mw = 0; wl_b = 0.0; wl_m = 0.0; wl_rows = 0
    for ix, c in rd(rs):
        if 'monthsWorldOnly' not in ix: has_new = False; break
        has_new = True
        v = float(c[ix['unknownGovRailAddCap']])
        if v > 0: g2 += v; g2rows += 1
        m = int(c[ix['monthsWorldOnly']]); mw += m
        if m > 0:
            wl_rows += 1
            wl_b += float(c[ix['preBuildLocoAmtGold']]); wl_m += float(c[ix['preMaintLocoAmtGold']])
    per.append((s, g1, g2, g2rows, mw, wl_b, wl_m))
    T['G-C1a'] += g1; T['G-C2'] += g2; T['G-C2 行'] += g2rows; T['monthsWorldOnly'] += mw; T['wl_b'] += wl_b; T['wl_m'] += wl_m; T['wl_rows'] += wl_rows; T['種'] += 1
print(f'== {ins} 種 {s0}〜{s1}（読めた種 {int(T["種"])}）')
for s, g1, g2, g2r, mw, b, m in per:
    print(f'  種 {s}：G-C1a {g1:.4g}・G-C2 {g2:.4g}（正の行 {g2r}）・monthsWorldOnly {mw}・その行の pre の機関車の払い 建設 {b:.4g}・保守 {m:.4g}' if has_new else f'  種 {s}：G-C1a {g1:.4g}（新しい 2 列が無い生）')
ok1 = T['G-C1a'] >= floor
print(f'G-C1a 知る国から買った機関車の額 10 種の和 {T["G-C1a"]:.6g}・床 {floor:.6g} → {"緑" if ok1 else "赤"}')
if has_new:
    ok2 = T['G-C2'] <= 1e-9
    print(f'G-C2 自国が知らない州×月の輸送の段の線路の敷き 和 {T["G-C2"]:.6g}・正の行 {int(T["G-C2 行"])} → {"緑" if ok2 else "赤"}')
    print(f'記録 monthsWorldOnly の和 {int(T["monthsWorldOnly"])}・その国×四半期 {int(T["wl_rows"])} の pre の機関車の払い 建設 {T["wl_b"]:.6g}・保守 {T["wl_m"]:.6g}（金建て）')
else:
    print('G-C2 新しい 2 列が無い生＝読めない')
