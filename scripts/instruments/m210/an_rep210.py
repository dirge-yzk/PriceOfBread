"""(210) 読み手（読むだけ）＝国ごとの exact の差（r202→r207）が種の組の間で再現するか（組の間の相関・符号の一致）。
使い方：PYTHONUTF8=1 python an_rep210.py <A の d210 の組1> <B の組1> <起点1> <A の組2> <B の組2> <起点2> [<A の組3> <B の組3> <起点3>]"""
import sys, os, collections, statistics as st
FQ = 523
def ex1914(d, s0):
    out = collections.defaultdict(list)
    for s in range(s0, s0 + 10):
        best = {}
        with open(os.path.join(d, f'd{s}.txt'), encoding='utf-8') as f:
            for line in f:
                if not line.startswith('N|523|'): continue
                t = line.rstrip('\n').split('|')
                if int(t[7]) < 0: continue
                if t[3] not in best or float(t[16]) > float(best[t[3]][16]): best[t[3]] = t
        for n, t in best.items(): out[n].append(int(t[7]))
    return {n: sum(v) / len(v) for n, v in out.items()}
a = sys.argv[1:]
groups = []
for i in range(0, len(a), 3):
    A, B = ex1914(a[i], int(a[i + 2])), ex1914(a[i + 1], int(a[i + 2]))
    groups.append({n: B[n] - A[n] for n in A if n in B})
nats = sorted(set.intersection(*[set(g) for g in groups]))
def corr(x, y):
    mx, my = st.mean(x), st.mean(y); sx = sum((u - mx) ** 2 for u in x) ** .5; sy = sum((v - my) ** 2 for v in y) ** .5
    return sum((u - mx) * (v - my) for u, v in zip(x, y)) / (sx * sy) if sx > 0 and sy > 0 else float('nan')
print(f'国 {len(nats)}・組 {len(groups)}')
for i in range(len(groups)):
    for j in range(i + 1, len(groups)):
        x = [groups[i][n] for n in nats]; y = [groups[j][n] for n in nats]
        same = sum(1 for u, v in zip(x, y) if u * v > 0); opp = sum(1 for u, v in zip(x, y) if u * v < 0)
        print(f'組{i+1}×組{j+1}: 国ごとの Δexact の相関 {corr(x, y):+.3f}・符号が同じ国 {same}・逆の国 {opp}・平均 {100*st.mean(x):+.2f}／{100*st.mean(y):+.2f} pt')
print('国ごとの Δ（組ごと・10種の割合の差）＝大きい順（組1の絶対値）')
for n in sorted(nats, key=lambda n: -abs(groups[0][n]))[:15]:
    print(f'  {n}: ' + '・'.join(f'{100*g[n]:+.0f}' for g in groups))
