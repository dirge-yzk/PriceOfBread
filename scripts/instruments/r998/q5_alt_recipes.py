# (998) 問い5：レシピごとの設備の規模（eqr_s の scaleSum）・財の中の割合（和÷和）・規模あたりの採算（marginSum÷scaleSum＝単位費用ではない近似）。
# 使い方: python -X utf8 -I q5_alt_recipes.py <生のフォルダ> <ラベル> <出力のファイル> <種の範囲>
import sys, os, statistics as st
RAW, LABEL, OUT, RNG = sys.argv[1:5]
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
YEARS = {1790, 1830, 1870, 1913, 1948}
MAJ = {'イギリス', 'フランス', 'ロシア', 'プロイセン', '清', 'アメリカ'}
def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
def med(xs):
    xs = [x for x in xs if x == x]; return st.median(xs) if xs else float('nan')
R = {}
for s in SEEDS:
    p = os.path.join(RAW, f'eqr_s{s}.tsv')
    if not os.path.exists(p): continue
    acc = {}; gtot = {}
    for ix, r in rows(p):
        y = int(r[ix['year']])
        if y not in YEARS: continue
        n, g, rc = r[ix['nation']], r[ix['good']], r[ix['recipe']]
        sc, mg = fl(r[ix['scaleSum']]), fl(r[ix['marginSum']])
        for scope in (['MAJ6'] if n in MAJ else []) + ['ALL']:
            a = acc.setdefault((scope, g, rc, y), [0.0, 0.0]); a[0] += sc; a[1] += mg
            gtot[(scope, g, y)] = gtot.get((scope, g, y), 0.0) + sc
    for (scope, g, rc, y), a in acc.items():
        t = gtot[(scope, g, y)]
        R.setdefault((scope, g, rc, y), []).append((a[0] / t if t > 0 else float('nan'), a[1] / a[0] if a[0] > 0 else float('nan'), a[0]))
# wp_s：旧と新のレシピの単位費用（3 財）
W = {}
for s in SEEDS:
    p = os.path.join(RAW, f'wp_s{s}.tsv')
    if not os.path.exists(p): continue
    acc = {}
    for ix, r in rows(p):
        y = int(r[ix['year']])
        if y not in YEARS or r[ix['nation']] not in MAJ: continue
        g = r[ix['good']]
        a = acc.setdefault((g, y), [0.0, 0.0, 0.0, 0.0])
        oo, no = fl(r[ix['oldOutputPerMonth']]), fl(r[ix['newOutputPerMonth']])
        a[0] += oo; a[1] += no; a[2] += oo * fl(r[ix['oldCostPerUnit']]); a[3] += no * fl(r[ix['newCostPerUnit']])
    for k, a in acc.items():
        W.setdefault(k, []).append((a[1] / (a[0] + a[1]) if a[0] + a[1] > 0 else float('nan'), a[2] / a[0] if a[0] > 0 else float('nan'), a[3] / a[1] if a[1] > 0 else float('nan')))
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# q5 {LABEL} raw={RAW} seeds={RNG}・eqr は計器の国（9〜10 カ国）だけ・種の中央値\n')
    o.write('scope\tgood\trecipe\tyear\tshareOfScale\tmarginPerScale\tscaleSum\n')
    for k in sorted(R):
        v = R[k]
        o.write('\t'.join(map(str, k)) + f'\t{med([x[0] for x in v]):.3f}\t{med([x[1] for x in v]):.3f}\t{med([x[2] for x in v]):.2f}\n')
    o.write('\n# wp_s（6 大国・旧と新の産出の重みの単位費用）good\tyear\tnewShare\toldCost\tnewCost\n')
    for k in sorted(W):
        v = W[k]
        o.write('\t'.join(map(str, k)) + '\t' + '\t'.join(f'{med([x[i] for x in v]):.3f}' for i in range(3)) + '\n')
print('ok', OUT)
