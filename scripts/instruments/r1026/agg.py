# (1026) attr.py の出力を区分ごとに合計し、予言を判定する
import sys, collections
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
for pre in sys.argv[2:]:
    rows = [l.rstrip('\n').split('\t') for l in open(pre + '_summary.tsv', encoding='utf-8')][1:]
    tot = collections.defaultdict(lambda: [0, 0, 0.0, 0.0, 0.0, 0, 0.0])
    per_seed = collections.defaultdict(lambda: [0, 0])
    for r in rows:
        s, c = r[0], r[1]; t = tot[c]
        t[0] += int(r[2]); t[1] += int(r[3]); t[2] += float(r[4]); t[3] += float(r[5]); t[4] += float(r[6]); t[5] += int(r[7]); t[6] = max(t[6], float(r[8]))
        per_seed[s][0] += int(r[2]); per_seed[s][1] += int(r[3])
    out.write(f'== {pre}\nclass\tpairs\tmismatch\tsumA\tsumAbsD\tsumD\tnegD\tmaxAbsResid(a,a0=d-X; others=|d|)\n')
    for c in ['a', 'a0', 'a2', 'b', 'c', 'd']:
        t = tot[c]; out.write(f'{c}\t{t[0]}\t{t[1]}\t{t[2]:.6g}\t{t[3]:.6g}\t{t[4]:.6g}\t{t[5]}\t{t[6]:.3g}\n')
    allp = sum(t[0] for t in tot.values()); allm = sum(t[1] for t in tot.values())
    sa = sum(t[2] for t in tot.values()); sd = sum(t[3] for t in tot.values())
    out.write(f'all\tpairs={allp}\tmismatch={allm}\tshare={allm/allp:.5f}\tsumAbsD/sumA={sd/sa:.3e}\n')
    out.write('per seed mismatch/pairs: ' + ' '.join(f's{s}:{v[1]}/{v[0]}({v[1]/v[0]*100:.2f}%)' for s, v in sorted(per_seed.items(), key=lambda kv: int(kv[0]))) + '\n')
    # (a)(a0) の相対の残差
    worst = 0.0; nres = 0
    for l in list(open(pre + '_mismatch.tsv', encoding='utf-8'))[1:]:
        f = l.rstrip('\n').split('\t')
        if f[5] in ('a', 'a0'):
            a, b, d, x, rs = map(float, f[6:11])
            rel = abs(rs) / max(abs(a), abs(b), 1e-300)
            if abs(rs) > 5e-10 * max(abs(a), abs(b)) + 1e-9: nres += 1
            worst = max(worst, rel)
    out.write(f'(a)(a0) d-X: rows outside G10 tolerance={nres}  max |d-X|/max(A,B)={worst:.3e}\n')
    # 世界の合計
    w = [l.rstrip('\n').split('\t') for l in open(pre + '_world.tsv', encoding='utf-8')][1:]
    nw = sum(1 for r in w if abs(float(r[3])) > 1e-6); bad = sum(1 for r in w if abs(float(r[5])) > 1e-6 + 1e-9 * abs(float(r[3])) * 0 + 5e-10 * 1e6)
    maxres = max((abs(float(r[5])) for r in w), default=0.0)
    out.write(f'world (seed,year,good) with |sumD|>1e-6: {nw}  max |sumD-sumX|={maxres:.3e}\n')
    info = open(pre + '_info.txt', encoding='utf-8').read().splitlines()
    out.write('info: ' + ' / '.join(l for l in info if 'mismatch q=' not in l)[:1500] + '\n')
print(open(sys.argv[1], encoding='utf-8').read())
