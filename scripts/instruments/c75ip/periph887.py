# (887) c75ip 周辺の点検の読み手（★(885)・事前登録 m887 [9] (ii-a)・読むだけ・エンジンの関数を呼ばない）
#   python -I -X utf8 periph887.py <名>=<ins フォルダ> ... [--seeds 1 10]
#   cpop_s（植民地格の州の POP の行）の debt と popK を読み、人数 ≦1e-12 で debt ＞0 の POP の行を階級ごとに数える：
#     行の数・最後の四半期の debt の和（種の和）・その POP の savings の和（最後の四半期）・debt の続く長さ（同じ (州, POP の添字) で debt ＞0・人数 ≦1e-12 が続いた四半期の最大）・
#     人の居る POP（人数 ＞1e-12）の debt の最後の四半期の和（比べる物）。
#   読み：EmptyPopEstate は人の居ない POP の在庫を毎月抜くが Debt は動かさない＝返す原資の入らない Debt が残る型か（片側だけの門）。
import sys, os
from collections import defaultdict
args = [a for a in sys.argv[1:]]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
arms = [a.split('=', 1) for a in args]
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
for tag, d in arms:
    rows = defaultdict(int); lastDebt = defaultdict(float); lastSav = defaultdict(float); runMax = defaultdict(int)
    liveLastDebt = 0.0; deadLastAll = 0.0; nseeds = 0; firstQ = {}
    for s in range(s0, s1 + 1):
        p = os.path.join(d, f'cpop_s{s}.tsv')
        if not os.path.exists(p): print(f'# {tag} 種 {s} 表なし'); continue
        nseeds += 1
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, ireg, ipi, icl, ipk, idb, isv = ix['quarter'], ix['region'], ix['popIndex'], ix['className'], ix['popK'], ix['debt'], ix['savings']
            run = {}; lastQ = -1; snap = []
            for line in f:
                x = line.rstrip('\n').split('\t')
                q = int(x[iq]); pk = fl(x[ipk]); db = fl(x[idb])
                if q > lastQ: lastQ = q; snap = []
                key = (x[ireg], x[ipi])
                dead = not (pk > 1e-12)
                if dead and db > 0:
                    cl = x[icl]; rows[cl] += 1
                    r = run.get(key)
                    run[key] = (r[0] + 1 if r and r[1] == q - 1 else 1, q)
                    runMax[cl] = max(runMax[cl], run[key][0])
                    if cl not in firstQ or q < firstQ[cl][1]: firstQ[cl] = (s, q, int(x[ix['year']]))
                snap.append((x[icl], dead, db, fl(x[isv])))
            for cl, dead, db, sv in snap:
                if dead and db > 0: lastDebt[cl] += db; lastSav[cl] += sv if sv == sv else 0.0; deadLastAll += db
                elif not dead and db > 0: liveLastDebt += db
    print(f'# {tag} {d} 種 {s0}-{s1}（表のあった種 {nseeds}）')
    for cl in sorted(rows, key=lambda c: -rows[c]):
        print(f'{tag}\t{cl}\t人数 ≦1e-12 で debt ＞0 の行 {rows[cl]}\t最後の四半期の debt の和 {lastDebt[cl]:.6g}\tその savings の和 {lastSav[cl]:.6g}\t続いた四半期の最大 {runMax[cl]}\t最初（種・四半期・年） {firstQ[cl]}')
    print(f'{tag}\t計\t最後の四半期の人の居ない POP の debt の和 {deadLastAll:.6g}\t人の居る POP の debt の和 {liveLastDebt:.6g}')
