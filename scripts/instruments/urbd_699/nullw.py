# urbd (699)：eq の表を m694・N1・N2 で並べ、国×年代の値と 3 本の幅（最大−最小）を出す（読むだけ）
import sys, csv, statistics as st
sys.stdout.reconfigure(encoding='utf-8')
def load(p):
    with open(p, encoding='utf-8') as f:
        r = csv.DictReader(f, delimiter='\t')
        return {(d['nation'], d['era']): d for d in r}
A = [load(p) for p in sys.argv[1:4]]
cols = ['Rnom', 'Rsub', 'Rsh', 'dsym', 'curPair', 'tgt', 'regU', 'subShR', 'fam']
print('nation\tera\t' + '\t'.join(f"{c}\t{c}_w" for c in cols) + '\tgross/net')
summ = {c: [[], [], []] for c in ['absdsym', 'Rnom', 'gap']}
for k in A[0]:
    vals = []
    for c in cols:
        v = [float(a[k][c]) for a in A if k in a]
        vals.append(f"{v[0]:.3f}\t{max(v)-min(v):.3f}")
    g = float(A[0][k]['grossPerMo']); n = float(A[0][k]['netPerMo'])
    print(f"{k[0]}\t{k[1]}\t" + '\t'.join(vals) + f"\t{(g/abs(n) if n else float('nan')):.1f}")
    for i, a in enumerate(A):
        if k in a:
            summ['absdsym'][i].append(abs(float(a[k]['dsym'])))
            summ['Rnom'][i].append(float(a[k]['Rnom']))
            summ['gap'][i].append(float(a[k]['curPair']) - float(a[k]['tgt']))
print()
for c, arms in summ.items():
    print(c, ' '.join(f"{nm}={st.median(v):.4f}" for nm, v in zip(['m694', 'N1', 'N2'], arms)))
