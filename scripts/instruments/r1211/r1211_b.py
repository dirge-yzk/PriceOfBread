# (1211) 追加の読み：①影で外国が最大の行を〈持ち主が交戦なし（wNationIndex＝0）〉と〈今の外国の取り分 s＝0〉で分ける
# ②rulepower_s の pressureIndex（計器の簡略の圧）と secession_s の pressureIndex（エンジンの分離の圧）を同じ州×四半期で比べる。
# 呼び方: python -X utf8 -I r1211_b.py <生のフォルダ> <種の下> <種の上> <出力のフォルダ>
import sys, os, math
from collections import Counter

raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

fb = Counter(); allr = Counter(); ratios = []; nz = Counter()
for seed in range(s0, s1 + 1):
    sec = {}
    with open(os.path.join(raw, f'secession_s{seed}.tsv'), encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\r\n').split('\t'); sec[(c[0], c[2], c[3])] = fl(c[ix['pressureIndex']])
    with open(os.path.join(raw, f'rulepower_s{seed}.tsv'), encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iW, iB, iR2, iP = ix['wNationIndex'], ix['bestClass'], ix['nowRule2Share'], ix['pressureIndex']
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            peace = not (fl(c[iW]) > 0); s = fl(c[iR2]); s = s if s > 0 else 0.0
            k = ('平時' if peace else '交戦', 's=0' if s == 0 else 's>0')
            allr[k] += 1
            if c[iB] == 'foreign': fb[k] += 1
            pe = sec.get((c[0], c[2], c[3]))
            if pe is not None:
                pr = fl(c[iP])
                if pe > 0 and pr > 0: ratios.append(pr / pe)
                nz[('計器>0' if pr > 0 else '計器=0', 'エンジン>0' if pe > 0 else 'エンジン=0')] += 1

def q(vals, ps):
    v = sorted(vals); n = len(v)
    return [v[min(n - 1, int(p * n))] if n else float('nan') for p in ps]

with open(os.path.join(out, f'summary_b_s{s0}-{s1}.txt'), 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# r1211_b 種 {s0}〜{s1}\n## 影で外国が最大の行／全ての行（持ち主の交戦の有無 × 今の s）\n')
    for k in sorted(allr): o.write(f'{k}\t{fb[k]}\t{allr[k]}\t{fb[k]/allr[k]:.4f}\n')
    o.write(f'## 圧の比 計器÷エンジン（両方＞0）の分位 1/10/50/90/99% {q(ratios,[.01,.1,.5,.9,.99])}・行 {len(ratios)}\n')
    o.write(f'## 0 の組 {dict(nz)}\n')
