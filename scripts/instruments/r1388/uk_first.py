# (1388) uk_traj.py の出力から、種ごとに英の国債が 0 になった最初の年・その前の不履行・戦争・物価・返済を要約する。
#   python -X utf8 -I uk_first.py <out フォルダ> <タグ...>
import sys, csv, os, math
O = sys.argv[1]
def load(tag):
    T = {}
    with open(os.path.join(O, f'uk_{tag}.tsv'), encoding='utf-8') as fh:
        r = csv.DictReader(fh, delimiter='\t')
        for x in r: T.setdefault(int(x['seed']), {})[int(x['year'])] = {k: float(v) for k, v in x.items() if k not in ('seed', 'year')}
    return T
for tag in sys.argv[2:]:
    T = load(tag)
    print(f'## {tag}')
    print('seed zeroYr  stays0  defYrs(初回..)  peakDebtPct@yr  pct1800 pct1820 pct1840  repay/rev(0前10年)  int/rev(0前10年)  borrow(0前10年)  warM(0前10年) cwM plvl(0年/1783) gold')
    for s in sorted(T):
        Y = T[s]; ys = sorted(Y)
        z = next((y for y in ys if Y[y]['debtEnd'] <= 1e-9 and y > 1785), None)
        stays = z is not None and all(Y[y]['debtEnd'] <= 1e-9 for y in ys if y >= z and y <= 1909)
        defs = [y for y in ys if Y[y]['defMax'] > (Y.get(y - 1, {}).get('defMax', 0))]
        pk = max(ys, key=lambda y: Y[y]['debtPct'] if Y[y]['debtPct'] == Y[y]['debtPct'] else -1)
        def pct(y): v = Y.get(y, {}).get('debtPct', float('nan')); return f'{v:.0f}'
        if z:
            w = [y for y in ys if z - 10 <= y < z]
            rev = sum(Y[y]['revSum'] for y in w); rep = sum(Y[y]['repaySum'] for y in w); it = sum(Y[y]['intSum'] for y in w); bo = sum(Y[y]['borrowSum'] for y in w)
            wm = sum(Y[y]['warM'] for y in w); cw = sum(Y[y]['cwM'] for y in w)
            pl = Y[z]['plvlEnd'] / Y[ys[0]]['plvlEnd'] if Y[ys[0]]['plvlEnd'] > 0 else float('nan')
            extra = f'{rep / rev if rev else float("nan"):.3f}  {it / rev if rev else float("nan"):.3f}  {bo:.3g}  {wm:.0f} {cw:.0f} {pl:.2f} {Y[z]["onGoldEnd"]:.0f}'
        else: extra = '-'
        print(f'{s} {z} {stays} {defs[:4]} {Y[pk]["debtPct"]:.0f}@{pk} {pct(1800)} {pct(1820)} {pct(1840)}  {extra}')
