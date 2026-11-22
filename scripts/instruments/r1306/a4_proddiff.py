# (1306) 州ごとの生産者の差（重税−軽税）を四半期 q で並べる：使い方 a4_proddiff.py <out> <tag> <region> <q> [n]
import sys, csv
out, tag, reg, q = sys.argv[1:5]; n = int(sys.argv[5]) if len(sys.argv) > 5 else 15
rows = [r for r in csv.DictReader(open(f'{out}/prod_{tag}.tsv', encoding='utf-8'), delimiter='\t') if r['region'] == reg and r['q'] == q]
L = {r['idx']: r for r in rows if r['tax'] == '0'}; H = {r['idx']: r for r in rows if r['tax'] == '0.6'}
d = []
for i in L:
    if i not in H: continue
    a, b = L[i], H[i]
    d.append((abs(float(b['rev']) - float(a['rev'])), i, a['recipe'], a['good'], float(a['scale']), float(b['scale']), float(a['rev']), float(b['rev']), float(a['smMargin']), float(b['smMargin']), a['buysLabor']))
d.sort(reverse=True)
print(f'# {tag} region {reg} q {q}: |Δrev| 上位')
for x in d[:n]:
    print(f'{x[1]:>4} {x[2]:<12} g{x[3]:>3} scale {x[4]:.3f}->{x[5]:.3f} rev {x[6]:.4g}->{x[7]:.4g} sm {x[8]:+.3f}->{x[9]:+.3f} bl{x[10]}')
