# (999) R5 読み手 2：錠 B（小営業の入口の元手の門）と P4（空の商人の稼ぎ）を植民地格の州×四半期で数える（読むだけ・近似）
# 近似：cpop の savings は四半期の末の断面・社会移動（Meritocracy）の判定の時点の値ではない。労働者の所得（fromIncome）は読まない。
# 使い方: python -X utf8 -I r999_lock.py <出力> <名前>=<insフォルダ> ...
import csv, glob, os, sys, statistics
from collections import defaultdict
EMPTY = 1e-6
out = open(sys.argv[1], 'w', encoding='utf-8')
def band(p): return '<1000' if p < 1000 else ('1000-5000' if p < 5000 else '>=5000')
for arg in sys.argv[2:]:
    name, d = arg.split('=', 1)
    ps = sorted(glob.glob(os.path.join(d, 'cpop_s*.tsv')), key=lambda p: int(os.path.basename(p)[6:-4]))
    if not ps: out.write(f'{name}: cpop が無い\n'); continue
    seedrows = []
    beng = []
    for p in ps:
        s = int(os.path.basename(p)[6:-4])
        A = defaultdict(lambda: [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0])  # tot, sav+, wk, wksav, mk, msav, minMarket, art, msavStart
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            qi, ri, ci, pk, sv, im, ss = ix['quarter'], ix['region'], ix['className'], ix['popK'], ix['savings'], ix['inMarketPerQuarter'], ix['savingsStart']
            for x in r:
                a = A[(int(x[qi]), x[ri])]
                k = float(x[pk]); v = float(x[sv]); a[0] += k; a[1] += max(0.0, v)
                c = x[ci]
                if c == '工場労働者': a[2] += k; a[3] += v
                elif c == '商人': a[4] += k; a[5] += v; a[6] += float(x[im]); a[8] += float(x[ss])
                elif c == '職人': a[7] += k
        C = defaultdict(lambda: defaultdict(float))
        for (q, reg), a in A.items():
            tot, savp, wk, wks, mk, ms, mim, art, mss = a
            if tot <= 0: continue
            b = band(tot)
            avg = savp / tot
            avg_xm = (savp - max(0.0, ms)) / tot
            lockB_open = wk > 1e-9 and avg > 1e-9 and (wks / wk) >= avg
            lockB_open_xm = wk > 1e-9 and avg_xm > 1e-9 and (wks / wk) >= avg_xm
            st = 'empty' if mk <= EMPTY else ('tiny' if mk < 1 else 'ok')
            c = C[(b, st)]
            c['n'] += 1; c['lockB_open'] += lockB_open; c['lockB_open_xm'] += lockB_open_xm
            c['mInMarket>0'] += mim > 0; c['mInMarket'] += mim; c['artEmpty'] += art <= EMPTY
            c['msavStart>0'] += mss > 0
            if reg == '中核ベンガル' and st == 'empty': beng.append((name, s, q, lockB_open, mim))
        seedrows.append(C)
    keys = sorted({k for C in seedrows for k in C})
    out.write(f'== {name}（植民地格の州×四半期・種の和・割合は和÷和）\n')
    for k in keys:
        S = defaultdict(float)
        for C in seedrows:
            for kk, v in C.get(k, {}).items(): S[kk] += v
        n = max(1.0, S['n'])
        out.write(f"  帯 {k[0]:<10} 商人 {k[1]:<5} 件 {S['n']:>8.0f}  錠B開 {S['lockB_open']/n:.4f}  錠B開(商人の貯蓄を除く平均) {S['lockB_open_xm']/n:.4f}"
                  f"  商人の市場の入り>0 {S['mInMarket>0']/n:.4f}  入りの和 {S['mInMarket']:.4g}  四半期の頭の商人の貯蓄>0 {S['msavStart>0']/n:.4f}  職人も空 {S['artEmpty']/n:.4f}\n")
    if beng:
        n = len(beng)
        out.write(f'  中核ベンガル 商人が空の四半期 {n}（種の和）：錠B開 {sum(b[3] for b in beng)/n:.4f}  商人の市場の入り>0 {sum(1 for b in beng if b[4] > 0)/n:.4f}  入りの和 {sum(b[4] for b in beng):.4g}\n')
    out.flush()
out.close(); print('ok')
