# (1389) dz：生 keep_1383/base_ins を読む共通の部品（読むだけ・r1382 の load.py の写しで置き場だけ替えた）
import os
BASE = os.environ.get('POBRAW', 'D:/PoB_work/keep_1383/base_ins')   # 84 件の取り直しは POBRAW=E:/PoB_archive/keep_1362/base_ins（m1362＝keep_1376 と全行一致の基線）
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
def pct(a, ps=(.1, .5, .9)):
    a = sorted(x for x in a if x == x)
    if not a: return 'none'
    return ' '.join(f'p{int(p*100)}={a[min(len(a)-1,int(p*len(a)))]:.4g}' for p in ps) + f' n={len(a)}'
def crisis_map(s):
    # 同盟の参戦の国難から厭戦を除いた述語（Diplomacy.cs:1616〜:1619 の写し・四半期の値）。支払い率は nation の payRateMeanShare で代える〈推〉
    leg = {(int(v['quarter']), v['nation']): fl(v['legitimacyIndex']) for v in rows('crownpc', s)}
    out = {}
    for v in rows('nation', s):
        q = int(v['quarter']); nm = v['nation']; L = leg.get((q, nm), float('nan'))
        c = (fl(v['civilWarMonths']) > 0, not (fl(v['foodMetShare']) >= 0.7), not (fl(v['payRateMeanShare']) >= 0.6), not (L >= 0.4))
        out[(q, nm)] = (any(c), c, fl(v['atWarMonths']) > 0)
    return out
def crown_k(seed):
    by_name, by_uid = {}, {}
    for v in rows('crownpc', seed):
        k = kprime(fl(v['mobilizationIndex']), fl(v['effectiveCoerceIndex']))
        q = int(v['quarter']); by_name[(q, v['nation'])] = k; by_uid[(q, v['nationUid'])] = k
    return by_name, by_uid
