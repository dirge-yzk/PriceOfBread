# (1396) c148dm：生の表を読む共通の部品（読むだけ・r1389 の load.py を元に、列は列名で引き、置き場を環境変数で替える）
#   既定の生＝D:/PoB_work/keep_1390/base_ins（INSRAW・木 3227da768・m1383 と全行一致の盤面）。
#   84 件の内戦の取り直し＝POBRAW=E:/PoB_archive/keep_1362/base_ins（m1362 の基線）。
import os
BASE = os.environ.get('POBRAW', 'D:/PoB_work/keep_1390/base_ins')
SEEDS = list(range(1, 11))


def fl(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float('nan')


def rows(t, seed, base=None):
    p = os.path.join(base or BASE, f'{t}_s{seed}.tsv')
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(h, ln.rstrip('\n').split('\t')))


def kprime(mob, co):
    # K' = C / (C + X)。C＝effectiveCoerceIndex・X＝mobilizationIndex（Reformist＋Radical）。分母が 0 か NaN なら 1（下書き X5 の WarCapacity と同じ形）
    den = co + mob
    return 1.0 if not (den > 0) else co / den


def pct(a, ps=(.1, .25, .5, .75, .9)):
    a = sorted(x for x in a if x == x)
    if not a:
        return 'none'
    return ' '.join(f'p{int(p * 100)}={a[min(len(a) - 1, int(p * len(a)))]:.4g}' for p in ps) + f' n={len(a)}'
