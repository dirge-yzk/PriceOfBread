# (1150) 合流の記録（採否に使わない）：c112hy の反証役 v2 の台本 hyena_wars.py の写し（中身は同じ）。開戦を〈前の四半期にどちらかの敗戦の衝撃≧0.3〉×〈前の四半期の遺恨＞0〉で分け、衝撃の組の高い帯（緊張≧100）からの開戦を数える。
#   python -X utf8 -I hywars.py <ins> <ラベル> [<ins> <ラベル> ...]（種 1〜10）
import sys, os
from collections import defaultdict
# war starts by pair, split by whether either side had defeatShock>=0.3 in prev quarter, and whether the pair had grudge>0 prev quarter
def run(D, lab):
    tot = defaultdict(int)
    for s in range(1, 11):
        sh = {}
        with open(os.path.join(D, f'scalars_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
            for l in f:
                r = l.rstrip('\n').split('\t')
                sh[(int(r[0]), r[ix['nation']])] = float(r[ix['defeatShock']])
        prevg = {}
        rows = []
        with open(os.path.join(D, f'warpair_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
            for l in f:
                r = l.rstrip('\n').split('\t')
                rows.append((int(r[0]), r[1], r[2], int(r[ix['starts']]), float(r[ix['grudge']]), float(r[ix['tensionEnd']])))
        g = {}; te = {}
        for q, a, b, st, gr, t in rows:
            g[(q, a, b)] = gr; te[(q, a, b)] = t
        for q, a, b, st, gr, t in rows:
            tot['pairQuarters'] += 1
            if st <= 0: continue
            m = max(sh.get((q - 1, a), 0.0), sh.get((q - 1, b), 0.0))
            pg = g.get((q - 1, a, b), 0.0)
            kind = ('shock' if m >= 0.3 else 'noshock') + ('_grudge' if pg > 0 else '_nogrudge')
            tot[kind] += st
            tot['all'] += st
            # high band entering
            pt = te.get((q - 1, a, b))
            if m >= 0.3 and pt is not None and pt >= 100: tot['shock_highprev'] += st
    print(lab, ' '.join(f'{k}={v}' for k, v in sorted(tot.items())))

for D, lab in zip(sys.argv[1::2], sys.argv[2::2]):
    run(D, lab)
