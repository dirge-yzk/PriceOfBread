"""(529) 後半の判定の読み手（事前登録 measurements/pf2529_20261024_prereg.txt [8]）。
使い方：python judge.py <腕 B の ins フォルダ> <比べる側（m524）の ins フォルダ> <門の種のカンマ区切り> [<記録の種のカンマ区切り>]
 ・G1・G2（門）：門の種だけ。上限が慣習より高い国（日本・オーストリア・フランス・ロシア・プロイセン）の農民の満足度 0.02 未満の割合（人口加重・全期間）を 腕 B と m524 で対にし、低い／高い／同じ（許容 0.01）を数える。
 ・M1・M5（整合）：腕 B の sbr_s（全ての種）：capErr の最大・legBreach の最大・国×四半期の 地代÷付加価値 の最大。
 ・R1（記録）：年代×国の 0.02／0.1／0.3 未満の割合（腕 B と m524・全ての種〔記録の種〕）。
"""
import sys, collections, os
sys.stdout.reconfigure(encoding='utf-8')
B, A = sys.argv[1], sys.argv[2]
gate_seeds = [int(x) for x in sys.argv[3].split(',')]
rec_seeds = [int(x) for x in sys.argv[4].split(',')] if len(sys.argv) > 4 else list(range(1, 11))
TH = [0.02, 0.1, 0.3]
UP = ['日本', 'オーストリア', 'フランス', 'ロシア', 'プロイセン']   # 初期の法の上限が慣習の取り分より高い国（計器の 9 国のうち）
ERAS = [(1783, 1800), (1800, 1825), (1825, 1850), (1850, 1875), (1875, 1900), (1900, 1925), (1925, 1951)]

def load(base, seeds):
    d = {}
    for s in seeds:
        f = os.path.join(base, f"svcbuy_s{s}.tsv")
        if not os.path.exists(f): continue
        with open(f, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in fh:
                c = l.rstrip('\n').split('\t')
                if c[ix['classId']] != '1': continue
                d.setdefault((s, int(c[ix['quarter']]), c[ix['nation']]), (int(c[ix['year']]), float(c[ix['popK']]), float(c[ix['satisfactionMean']])))
    return d

def frac(d, keys, th, a0=1783, b0=1951, nat=None, seed=None):
    sel = [d[k] for k in keys if a0 <= d[k][0] < b0 and (nat is None or k[2] == nat) and (seed is None or k[0] == seed)]
    tot = sum(x[1] for x in sel)
    return (sum(x[1] for x in sel if x[2] < th) / tot) if tot > 0 else None

allseeds = sorted(set(gate_seeds) | set(rec_seeds))
db = load(B, allseeds); da = load(A, allseeds)
keys = [k for k in db if k in da]
print("共通の（種・四半期・国）", len(keys))

print("\n== G1・G2（門・門の種のみ）：<0.02 の割合 腕 B 対 m524（全期間・人口加重・許容 0.01）")
low = high = same = 0
jp_low = 0
for n in UP:
    row = []
    for s in gate_seeds:
        x = frac(db, keys, 0.02, nat=n, seed=s); y = frac(da, keys, 0.02, nat=n, seed=s)
        if x is None or y is None: row.append(f"s{s}:-"); continue
        if y < 0.02: row.append(f"s{s}:{x:.3f}/{y:.3f}(床〈分母外〉)"); continue   # 事前登録 [8]②：m524 が 0.02 未満の組は 0.01 を超えて下げられない＝門の分母から外す
        t = 'low' if x < y - 0.01 else ('high' if x > y + 0.01 else 'same')
        if t == 'low': low += 1
        elif t == 'high': high += 1
        else: same += 1
        if n == '日本' and t == 'low': jp_low += 1
        row.append(f"s{s}:{x:.3f}/{y:.3f}{'↓' if t=='low' else ('↑' if t=='high' else '=')}")
    print(f"{n}: " + " ".join(row))
print(f"G1：低い {low}・高い {high}・同じ {same}（門＝低い ≧ 10 かつ 低い ≧ 2×高い）→ {'通る' if (low >= 10 and low >= 2 * high) else '外れ'}")
print(f"G2：日本が低い種 {jp_low}/{len(gate_seeds)}（門＝4 以上）→ {'通る' if jp_low >= 4 else '外れ'}")

print("\n== R1（記録）年代×国：腕 B / m524 の <0.02, <0.1, <0.3（種 " + ",".join(map(str, rec_seeds)) + "）")
nats = sorted({k[2] for k in keys})
recKeys = [k for k in keys if k[0] in rec_seeds]
for n in nats:
    print(n)
    for a0, b0 in ERAS:
        xs = [frac(db, recKeys, t, a0, b0, nat=n) for t in TH]; ys = [frac(da, recKeys, t, a0, b0, nat=n) for t in TH]
        if xs[0] is None: continue
        print(f"  {a0}-{b0}: B " + " ".join(f"{v:.3f}" for v in xs) + " | m524 " + " ".join(f"{v:.3f}" for v in ys))
print("全体")
for a0, b0 in ERAS:
    xs = [frac(db, recKeys, t, a0, b0) for t in TH]; ys = [frac(da, recKeys, t, a0, b0) for t in TH]
    if xs[0] is None: continue
    print(f"  {a0}-{b0}: B " + " ".join(f"{v:.3f}" for v in xs) + " | m524 " + " ".join(f"{v:.3f}" for v in ys))

print("\n== M1・M5（整合）腕 B の sbr_s：capErr の最大・legBreach の最大・国×四半期の 地代÷付加価値 の最大")
mx_err = 0.0; mx_leg = 0.0; mx_ratio = 0.0; who = None; hascol = False
for s in allseeds:
    f = os.path.join(B, f"sbr_s{s}.tsv")
    if not os.path.exists(f): print("sbr_s なし", s); continue
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        hascol = 'legBreach' in ix
        for l in fh:
            c = l.rstrip('\n').split('\t')
            va = float(c[ix['rgoVa']])
            n_ = int(c[ix['rgoN']])
            mx_err = max(mx_err, float(c[ix['capErr']]) / max(1, int(c[ix['capN']])))
            if hascol: mx_leg = max(mx_leg, float(c[ix['legBreach']]))
            if va > 0:
                r = float(c[ix['rgoRent']]) / va
                if r > mx_ratio: mx_ratio = r; who = (s, c[ix['year']], c[ix['nation']])
print(f"capErr（産地×月の平均の最大）={mx_err:.3e}（門＝1e-9 以下）→ {'通る' if mx_err <= 1e-9 else '外れ'}")
print(f"legBreach の最大={mx_leg:.3e}（列あり={hascol}・0 のはず）")
print(f"地代÷付加価値 の最大={mx_ratio:.4f} {who}（整合＝0.85 以下）→ {'通る' if mx_ratio <= 0.85 + 1e-9 else '外れ'}")
