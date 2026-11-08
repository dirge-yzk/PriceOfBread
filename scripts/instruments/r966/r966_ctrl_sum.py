# (966) 対照の表の中央と分位：F の国と F の無い国（分かれた国／それ以外）で lev・Eの割合・drain8・imp8・paid8 を並べる。
import sys, statistics as st
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
def q(v, p): v = sorted(v); return v[min(len(v) - 1, int(p * len(v)))] if v else float('nan')
for p in sys.argv[1:]:
    L = [l.rstrip('\n').split('\t') for l in open(p, encoding='utf-8')]; h = L[0]; ix = {k: i for i, k in enumerate(h)}
    for sec in ('分', '-'):
        for F in ('F', '-'):
            R = [r for r in L[1:] if r[3] == sec and r[4] == F and f(r[5]) >= 10]
            out = [p, '分かれた国' if sec == '分' else 'それ以外', 'Fあり' if F == 'F' else 'Fなし', f'国×種 {len(R)}']
            for k in ('lev', 'Eの割合', 'drain8', 'imp8', 'paid8'):
                v = [f(r[ix[k]]) for r in R if f(r[ix[k]]) == f(r[ix[k]])]
                out.append(f'{k} 中央 {st.median(v):.3g}・90% {q(v,0.9):.3g}' if v else f'{k} -')
            print(*out, sep='\t')
    # drain8 ≧0.5 の国の中で F の割合
    R = [r for r in L[1:] if f(r[5]) >= 10]
    for th in (0.1, 0.5, 1.0):
        A = [r for r in R if f(r[ix['drain8']]) >= th]
        print(p, f'drain8≧{th}', f'国×種 {len(A)}', f'そのうち F {sum(r[4]=="F" for r in A)}', f'F の国 {sum(r[4]=="F" for r in R)} のうち drain8≧{th} {sum(r[4]=="F" and f(r[ix["drain8"]])>=th for r in R)}', sep='\t')
