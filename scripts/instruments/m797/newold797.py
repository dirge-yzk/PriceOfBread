# (797) 同じ盤面の新÷旧の表（cpeace_s・m791_20261103_newold_B2.txt と同じ列）を M・S11 に当てる（読むだけ）。python -I newold797.py <名前>=<ins> ...
import sys, os, glob, csv, collections
sys.stdout.reconfigure(encoding='utf-8')
F = lambda x: float(x) if x not in ('', 'nan') else 0.0
for arg in sys.argv[1:]:
    name, d = arg.split('=', 1); ev = []
    for f in glob.glob(os.path.join(d, 'cpeace_s*.tsv')):
        ev += list(csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'))
    g = collections.defaultdict(list)
    for e in ev:
        n = int(e['nCollapse']); g[n if n < 6 else 6].append(e)
    print(f'# {name}（{d}）崩壊の事象 {len(ev)}')
    print('N\t事象\tΣ賠償 旧の式\tΣ賠償 実際\t実際÷旧\tΣ予算 旧\tΣ予算 実際\t奪った植民地\t割譲 実際\t門を通る講和')
    for k in sorted(g):
        E = g[k]; so = sum(F(e['repOldIndex']) for e in E); sr = sum(F(e['repRealIndex']) for e in E)
        print(f"{k if k < 6 else '6+'}\t{len(E)}\t{so:.0f}\t{sr:.0f}\t{sr / so if so else float('nan'):.3f}\t{sum(F(e['budOldIndex']) for e in E):.0f}\t{sum(F(e['budRealIndex']) for e in E):.0f}\t{sum(int(e['capturedCount']) for e in E)}\t{sum(int(e['cedeRealCount']) for e in E)}\t{sum(int(e['cedeEligCount']) for e in E)}")
    E2 = [e for e in ev if int(e['nCollapse']) >= 2]; so = sum(F(e['repOldIndex']) for e in E2); sr = sum(F(e['repRealIndex']) for e in E2)
    print(f'N>=2 合計\t{len(E2)}\t{so:.0f}\t{sr:.0f}\t{sr / so if so else float("nan"):.3f}')
    print(f"N>=2 で nGrouped<N の事象 {sum(1 for e in E2 if int(e['nGrouped']) < int(e['nCollapse']))}・nGrouped==0 {sum(1 for e in E2 if int(e['nGrouped']) == 0)}")
