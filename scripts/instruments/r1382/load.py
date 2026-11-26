# (1382) rc：生 keep_1376/base_ins を読む共通の部品（読むだけ）
import os, glob
BASE = 'D:/PoB_work/keep_1376/base_ins'
def fl(x):
    try: return float(x)
    except: return float('nan')
def rows(t, seed):
    p = os.path.join(BASE, f'{t}_s{seed}.tsv')
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(h, ln.rstrip('\n').split('\t')))
SEEDS = list(range(1, 11))
def kprime(mob, co):
    den = co + mob
    return 1.0 if not (den > 0) else co / den
def crown_k(seed):
    by_name, by_uid = {}, {}
    for v in rows('crownpc', seed):
        k = kprime(fl(v['mobilizationIndex']), fl(v['effectiveCoerceIndex']))
        q = int(v['quarter']); by_name[(q, v['nation'])] = k; by_uid[(q, v['nationUid'])] = k
    return by_name, by_uid
def pct(a, ps=(.1, .5, .9)):
    a = sorted(x for x in a if x == x)
    if not a: return 'none'
    return ' '.join(f'p{int(p*100)}={a[min(len(a)-1,int(p*len(a)))]:.4g}' for p in ps) + f' n={len(a)}'
