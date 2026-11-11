# (998) 問い2：読める財だけの自給率（S1＝Σmin(産出,需要)÷Σ需要・S2＝1−輸入÷使った量・輸入÷使った量・輸出÷作った量）。和÷和・種の中央値。
#  A) chemgood_s（硫酸・ソーダ灰×計器の国・全年）：輸出の列が無い＝S2′＝1−輸入÷(産出＋輸入)
#  B) ivf_s（鉄・鋼・機械×全ての国）：輸入の列が無い＝S1 だけ
#  C) pxgoods_s（1790 年だけ×9 カ国×31 財）：産出・需要・輸入・輸出がそろう
# 使い方: python -X utf8 -I q2_selfsuff.py <生のフォルダ> <ラベル> <出力のファイル> <種の範囲 例 1-10>
import sys, os, statistics as st
RAW, LABEL, OUT, RNG = sys.argv[1:5]
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
YEARS = [1790, 1830, 1870, 1913, 1948]
MAJ = {'イギリス', 'フランス', 'ロシア', 'プロイセン', '清', 'アメリカ'}

def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0

def med(xs): return st.median(xs) if xs else float('nan')
out = [f'# q2 {LABEL} raw={RAW} seeds={RNG}', '# 比は全て和÷和（国×四半期×財の和）・種の中央値']

# A) chemgood
A = {}  # (scope, good, year) -> list per seed of (S1, S2', imp/use)
for s in SEEDS:
    p = os.path.join(RAW, f'chemgood_s{s}.tsv')
    if not os.path.exists(p): continue
    acc = {}
    for ix, r in rows(p):
        y = int(r[ix['year']])
        if y not in YEARS: continue
        n, g = r[ix['nation']], r[ix['good']]
        P, D, I = fl(r[ix['outputPerMonth']]), fl(r[ix['demandPerMonth']]), fl(r[ix['importPerMonth']])
        for sc in (['MAJ6'] if n in MAJ else []) + ['ALL9', n]:
            a = acc.setdefault((sc, g, y), [0.0, 0.0, 0.0, 0.0])
            a[0] += min(P, D); a[1] += D; a[2] += I; a[3] += P
    for k, a in acc.items():
        A.setdefault(k, []).append((a[0] / a[1] if a[1] > 0 else float('nan'), 1 - a[2] / (a[3] + a[2]) if a[3] + a[2] > 0 else float('nan'), a[2] / (a[3] + a[2]) if a[3] + a[2] > 0 else float('nan')))
out.append('\n## A chemgood（硫酸・ソーダ灰）scope\tgood\tyear\tS1\tS2prime\timp/use')
for k in sorted(A, key=lambda k: (k[0] not in ('MAJ6', 'ALL9'), k[0], k[1], k[2])):
    v = A[k]
    out.append(f'{k[0]}\t{k[1]}\t{k[2]}\t{med([x[0] for x in v if x[0]==x[0]]):.3f}\t{med([x[1] for x in v if x[1]==x[1]]):.3f}\t{med([x[2] for x in v if x[2]==x[2]]):.3f}')

# B) ivf
B = {}
for s in SEEDS:
    p = os.path.join(RAW, f'ivf_s{s}.tsv')
    if not os.path.exists(p): continue
    acc = {}
    for ix, r in rows(p):
        y = int(r[ix['year']])
        if y not in YEARS: continue
        n = r[ix['nation']]
        for g in ('iron', 'steel', 'mach'):
            P, D = fl(r[ix[g + 'Prod']]), fl(r[ix[g + 'Demand']])
            for sc in (['MAJ6'] if n in MAJ else []) + ['ALL', n if n in MAJ else None]:
                if sc is None: continue
                a = acc.setdefault((sc, g, y), [0.0, 0.0, 0.0, 0])
                a[0] += min(P, D); a[1] += D; a[2] += P
    for k, a in acc.items(): B.setdefault(k, []).append((a[0] / a[1] if a[1] > 0 else float('nan'), a[2] / a[1] if a[1] > 0 else float('nan')))
out.append('\n## B ivf（鉄・鋼・機械）scope\tgood\tyear\tS1\tprod/demand')
for k in sorted(B, key=lambda k: (k[0] not in ('MAJ6', 'ALL'), k[0], k[1], k[2])):
    v = B[k]
    out.append(f'{k[0]}\t{k[1]}\t{k[2]}\t{med([x[0] for x in v if x[0]==x[0]]):.3f}\t{med([x[1] for x in v if x[1]==x[1]]):.3f}')

# C) pxgoods 1790
C = {}
for s in SEEDS:
    p = os.path.join(RAW, f'pxgoods_s{s}.tsv')
    if not os.path.exists(p): continue
    acc = {}
    for ix, r in rows(p):
        if r[ix['year']] != '1790': continue
        n, g, tier = r[ix['nation']], r[ix['goodName']], r[ix['tier']]
        P, D, I, E = (fl(r[ix[c]]) for c in ('lastProducedQty', 'lastDemandQty', 'lastImportQty', 'lastExportQty'))
        for sc in (['MAJ6'] if n in MAJ else []) + ['ALL9']:
            for gg in (g, 'TIER:' + tier, 'ALLGOODS'):
                a = acc.setdefault((sc, gg), [0.0] * 6)
                a[0] += min(P, D); a[1] += D; a[2] += I; a[3] += E; a[4] += P; a[5] += P + I - E
    for k, a in acc.items():
        C.setdefault(k, []).append((a[0] / a[1] if a[1] > 0 else float('nan'), 1 - a[2] / a[5] if a[5] > 0 else float('nan'), a[2] / a[5] if a[5] > 0 else float('nan'), a[3] / a[4] if a[4] > 0 else float('nan')))
out.append('\n## C pxgoods 1790（9 カ国・主食・日用品・奢侈）scope\tgood\tS1\tS2\timp/use\texp/prod')
for k in sorted(C, key=lambda k: (not k[1].startswith(('ALLGOODS', 'TIER')), k[0], k[1])):
    v = C[k]
    out.append(f'{k[0]}\t{k[1]}\t' + '\t'.join(f'{med([x[i] for x in v if x[i]==x[i]]):.3f}' for i in range(4)))
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
print('ok', OUT)
