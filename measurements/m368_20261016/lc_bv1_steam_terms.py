# 反証役 lc〔bv〕中#1 の処置：蒸気機関の行の三項（技術力・必要性・伝わり）を A と B で同じ四半期に並べる（読むだけ）
import sys, statistics as st
A, B = sys.argv[1], sys.argv[2]
NAT = ['フランス', 'プロイセン', 'オスマン']
def load(d, sd):
    rows = {}
    with open(f'{d}/disc_s{sd}.tsv', encoding='utf-8') as fh:
        fh.readline()
        for line in fh:
            a = line.rstrip('\n').split('\t')
            if a[4] != '蒸気機関' or a[3] not in NAT: continue
            rows.setdefault(a[3], []).append((a[0], int(a[1]), float(a[5]), float(a[6]), float(a[7])))
    return rows
def med(x): return st.median(x) if x else float('nan')
print('国\t種\tA:開く四半期\tA:技術\tA:必要\tA:伝わり\tB:開く四半期\tB:技術\tB:必要\tB:伝わり\tB@Aの四半期:技術\t必要\t伝わり')
agg = {n: [] for n in NAT}
for sd in range(1, 11):
    ra, rb = load(A, sd), load(B, sd)
    for n in NAT:
        ea = [r for r in ra.get(n, []) if r[0] == 'E']; eb = [r for r in rb.get(n, []) if r[0] == 'E']
        qa = ea[0] if ea else None; qb = eb[0] if eb else None
        same = [r for r in rb.get(n, []) if qa and r[1] == qa[1]]
        s = same[0] if same else None
        f = lambda r: (f'{r[1]}\t{r[2]:.3g}\t{r[3]:.3g}\t{r[4]:.3g}' if r else '開かない\t\t\t')
        g = lambda r: (f'{r[2]:.3g}\t{r[3]:.3g}\t{r[4]:.3g}' if r else '(B は既に開いた/行なし)\t\t')
        print(f'{n}\t{sd}\t{f(qa)}\t{f(qb)}\t{g(s)}')
        agg[n].append((qa, qb, s))
print()
print('国\tA:開く四半期の中央\tB:開く四半期の中央（開かない=668）\tA@qA 技術/必要/伝わり の中央\tB@qA 技術/必要/伝わり の中央\tB@qB 技術/必要/伝わり の中央')
for n in NAT:
    L = agg[n]
    qa = [x[0][1] if x[0] else 668 for x in L]; qb = [x[1][1] if x[1] else 668 for x in L]
    def m3(rs): rs = [r for r in rs if r]; return '/'.join(f'{med([r[k] for r in rs]):.3g}' for k in (2, 3, 4)) + f' (n={len(rs)})'
    print(f'{n}\t{med(qa)}\t{med(qb)}\t{m3([x[0] for x in L])}\t{m3([x[2] for x in L])}\t{m3([x[1] for x in L])}')
