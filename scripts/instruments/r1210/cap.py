# (1210) ② 産地の生産者の捕捉（depInit）の散らばりと、立ち上がりに左右されない基準の候補の素の数（読むだけ）。
# 用法: cap.py <depmin_s の生> <種の始め> <種の終わり> <出力>
# 候補 K1＝開いた後の最初の行の産出×12 で割った捕捉（＝何年分か）・K2＝開いた後 +5 年までの産出の最大で割った捕捉・K3＝同じ財の世界の産地の捕捉の中央で割った比
import sys, csv, math, statistics as st, collections
raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
recs = []
for s in range(s0, s1 + 1):
    ser = collections.defaultdict(list)
    with open(f'{raw}/depmin_s{s}.tsv', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if int(x[ix['siteRow']]) < 0: continue
            ser[(x[ix['region']], x[ix['good']])].append((int(x[ix['q']]), int(x[ix['year']]), x[ix['siteFound']] == '1', float(x[ix['depInit']]), float(x[ix['depLeft']]), float(x[ix['output']])))
    for (reg, good), rows in ser.items():
        rows.sort()
        op = [z for z in rows if z[2]]
        if not op: continue
        di = op[0][3]
        if not (di > 0): continue
        first = next((z for z in op if z[5] > 0), None)
        y0 = op[0][1]
        mx5 = max((z[5] for z in op if z[1] <= y0 + 5), default=0)
        zero = next((z[1] for z in op if z[4] <= 0), None)
        recs.append(dict(seed=s, reg=reg, good=good, open=y0, depInit=di, first=first[5] if first else float('nan'), mx5=mx5, zeroYear=zero))
byg = collections.defaultdict(list)
for d in recs: byg[d['good']].append(d['depInit'])
with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# raw={raw} seeds={s0}..{s1}・産地の生産者（siteRow≥0・開いた）・depInit は開いた最初の行の値・output の単位は計器の行の値（月か四半期かは計器の定義のまま）\n')
    o.write('good\tn\tdepInit_min\tdepInit_med\tdepInit_max\tspread_log10\tK1_spread_log10\tK2_spread_log10\tzeroBy1950\n')
    for g, v in sorted(byg.items()):
        ds = [d for d in recs if d['good'] == g]
        k1 = [d['depInit'] / d['first'] for d in ds if d['first'] > 0]
        k2 = [d['depInit'] / d['mx5'] for d in ds if d['mx5'] > 0]
        sp = lambda a: (math.log10(max(a) / min(a)) if a and min(a) > 0 else float('nan'))
        o.write(f'{g}\t{len(v)}\t{min(v):.3g}\t{st.median(v):.3g}\t{max(v):.3g}\t{sp(v):.2f}\t{sp(k1):.2f}\t{sp(k2):.2f}\t{sum(1 for d in ds if d["zeroYear"] is not None)}\n')
    o.write('\n# 行ごと\nseed\tregion\tgood\topen\tdepInit\tfirstOut\tmax5\tzeroYear\n')
    for d in sorted(recs, key=lambda d: (d['good'], d['seed'])):
        o.write(f"{d['seed']}\t{d['reg']}\t{d['good']}\t{d['open']}\t{d['depInit']:.4g}\t{d['first']:.4g}\t{d['mx5']:.4g}\t{d['zeroYear']}\n")
print('ok', out, len(recs))
