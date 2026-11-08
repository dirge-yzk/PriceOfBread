# (966) E（請求の半分を払えない四半期が 4 つ以上続く・r966_ef.py と同じ）を、その四半期の投資家の POP の数（investorCount）が 0 か正かで分ける。
#   国×種で〈E の四半期の過半が investorCount＞0〉なら〈投資家あり〉。助走の行は call が最大の 1 行。
import sys, os
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
for arg in sys.argv[1:]:
    name, rest = arg.split('=', 1); d, a0, a1 = rest.rsplit(':', 2)
    nE = withInv = 0; qE = qInv = 0; reqInv = reqAll = 0.0
    for s in range(int(a0), int(a1) + 1):
        D = {}
        with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                x = l.rstrip('\n').split('\t'); k = (x[ix['nation']], int(x[ix['quarter']])); c = int(x[ix['call']])
                if k in D and D[k][0] > c: continue
                D[k] = (c, fl(x[ix['divReqIndex']]), fl(x[ix['paidIndex']]), fl(x[ix['investorCount']]))
        byn = {}
        for (n, q), v in D.items():
            if v[1] > 0 and not (v[2] >= 0.5 * v[1]): byn.setdefault(n, []).append((q, v))
        for n, L in byn.items():
            L.sort(); runs = []; cur = [L[0]]
            for a in L[1:]:
                if a[0] == cur[-1][0] + 1: cur.append(a)
                else: runs.append(cur); cur = [a]
            runs.append(cur); E = [a for r in runs if len(r) >= 4 for a in r]
            if not E: continue
            nE += 1; ni = sum(1 for q, v in E if v[3] > 0); qE += len(E); qInv += ni
            reqAll += sum(v[1] for q, v in E); reqInv += sum(v[1] for q, v in E if v[3] > 0)
            if ni * 2 > len(E): withInv += 1
    print(name, f'E の国×種 {nE}', f'投資家あり {withInv}', f'E の四半期 {qE}・投資家あり {qInv}', f'請求の額のうち投資家ありの四半期 {reqInv/reqAll if reqAll else 0:.3g}', sep='\t')
