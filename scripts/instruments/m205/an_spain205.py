"""(205) スペインの植民地格の州の支配度・引き留め・圧力（ghost の R 行・読むだけ）。python an_spain205.py <out_dir>... """
import sys, os, statistics as st
from collections import defaultdict
NAT = 'スペイン'
WIN = [(1783, 1787), (1788, 1792), (1793, 1797), (1798, 1802), (1803, 1807), (1808, 1812)]
for d in sys.argv[1:]:
    g = os.path.join(d, 'ghost')
    print(f'## {d}')
    print('窓 | 植民地格の州（種の中央）| 支配度 | 引き留め | 圧力 | 闘争度 | 戦争の強度 | 主食')
    for (a, b) in WIN:
        R = defaultdict(list)
        for fn in sorted(os.listdir(g))[:5] if False else sorted(os.listdir(g)):
            if not fn.endswith('.txt'): continue
            s = int(fn[1:-4])
            if s > 5: continue
            vals = defaultdict(list); nreg = []
            qs = defaultdict(int)
            for ln in open(os.path.join(g, fn), encoding='utf-8'):
                if not ln.startswith('R\t'): continue
                p = ln.rstrip('\n').split('\t')
                yr = int(p[2])
                if not (a <= yr <= b) or p[4] != NAT or p[7] != 'col' or p[8] != 'a': continue
                qs[int(p[1])] += 1
                vals['dom'].append(float(p[30])); vals['hold'].append(float(p[31])); vals['pres'].append(float(p[32])); vals['mil'].append(float(p[33])); vals['war'].append(float(p[34])); vals['food'].append(float(p[35]))
            if not qs: continue
            R['n'].append(st.mean(qs.values()))
            for k, v in vals.items(): R[k].append(st.mean(v))
        if not R: continue
        m = {k: st.median(v) for k, v in R.items()}
        print(f"{a}-{b} | {m['n']:.1f} | {m['dom']:.3f} | {m['hold']:.4f} | {m['pres']:.4f} | {m['mil']:.3f} | {m['war']:.3f} | {m['food']:.2f}")
