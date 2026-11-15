# (1155) K1 の紙の予言：参政権の割引（＝1−中核の割合）を外した圧 p÷sf を同じ盤面で数える（1 次の近似・力学の連鎖は数えない）
import sys, os, csv
D, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); sys.stdout.reconfigure(newline='\n')
acc = {}
for s in range(S0, S1 + 1):
    p = os.path.join(D, f'secession_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            t = x[ix['tier']]; a = acc.setdefault(t, [0, 0, 0, 0, 0, 0, []])
            pr, sf, hs, core = float(x[ix['pressureIndex']]), float(x[ix['suffrageFactorShare']]), float(x[ix['holdShareIndex']]), float(x[ix['coreShareMeanShare']])
            a[0] += 1
            if not (sf > 1e-12): a[1] += 1; continue
            q = pr / sf
            if pr > 1.43: a[2] += 1
            if q > 1.43: a[3] += 1
            if hs - pr < 0: a[4] += 1
            if hs - q < 0: a[5] += 1
            if core >= 0.9: a[6].append(q / pr if pr > 1e-12 else 1.0)
for t, a in sorted(acc.items()):
    v = sorted(a[6]); m = v[len(v)//2] if v else float('nan')
    print(f'tier={t} 行={a[0]} sf=0={a[1]} 圧>1.43 旧 {a[2]}→新 {a[3]} 余り<0 旧 {a[4]}→新 {a[5]} 中核≥0.9 の州の新÷旧 中央 {m:.1f}（{len(v)} 行）')
