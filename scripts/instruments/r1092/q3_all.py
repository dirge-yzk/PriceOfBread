# (1092) 問い 1 の全ての国の版：stock の年末の行（q3・q7・…）と助走の直後の行（warm）は 61 国ぜんぶにある＝西を含む全ての国で L を分ける。
# あわせて rulepower の L で世界の ΣL の落ち（四半期 0→20）を国ごとに割り付ける。
# 使い方: python -X utf8 -I q3_all.py <表のフォルダ> > q3_out.txt
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]; SPM = 250.0 * 0.2765; TS = ['warm', 'q3', 'q7', 'q11', 'q19', 'q23', 'q39']
V = collections.defaultdict(lambda: collections.defaultdict(lambda: collections.defaultdict(dict)))
drop = collections.defaultdict(list); L0s = collections.defaultdict(list); L20s = collections.defaultdict(list); tot0 = []; tot20 = []
for s in range(1, 11):
    with open(os.path.join(d, f'stock_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t'); t = c[0]
            if t not in TS: continue
            n = c[ix['nation']]; L = float(c[ix['landPowerIndex']]); sol = float(c[ix['soldierK']]); rd = float(c[ix['militaryReadinessShare']]); eq = float(c[ix['capitalEquipFillShare']])
            if not (float(c[ix['totalPopulationK']]) > 0): continue
            V[n]['L'][t][s] = L; V[n]['兵K'][t][s] = sol; V[n]['装備'][t][s] = eq
            V[n]['暮らし'][t][s] = rd / eq if eq > 1e-12 else float('nan')
            b = sol / SPM * rd; V[n]['残り'][t][s] = L / b if b > 1e-12 else float('nan')
    l0 = {}; l20 = {}
    with open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q > 20: break
            if q == 0: l0.setdefault(c[ix['nation']], float(c[ix['lNationIndex']]))
            if q == 20: l20.setdefault(c[ix['nation']], float(c[ix['lNationIndex']]))
    T0 = sum(l0.values()); T20 = sum(l20.values()); tot0.append(T0); tot20.append(T20)
    for n in set(l0) | set(l20):
        a, b = l0.get(n, 0.0), l20.get(n, 0.0); drop[n].append(a - b); L0s[n].append(a); L20s[n].append(b)
def med(a):
    a = [x for x in a if x == x]
    return st.median(a) if a else float('nan')
print(f'世界の ΣL（rulepower・全ての国）：四半期 0 の中央 {med(tot0):.3g}・四半期 20 の中央 {med(tot20):.3g}・落ちの中央 {med([a-b for a,b in zip(tot0,tot20)]):.3g}')
print('ΣL の落ち（四半期 0−20）の国ごとの割り付け（10 種の中央・落ちの大きい順 15 国）：国\tL0\tL20\t落ち\t落ちの割合（落ちの中央÷ΣL の落ちの中央）')
D0 = med([a - b for a, b in zip(tot0, tot20)])
for n in sorted(drop, key=lambda n: -med(drop[n]))[:15]:
    print(f'  {n}\t{med(L0s[n]):.3g}\t{med(L20s[n]):.3g}\t{med(drop[n]):.3g}\t{med(drop[n]) / D0:.2f}')
print()
print('stock の全ての国の分け（warm＝助走の直後・q3 ほか＝年末）・10 種の中央・L0 が 0.1 以上の国だけ・比＝q19÷warm')
print('国\tL warm→q19\t比L\t比兵\t比暮らし\t比装備\t比残り\t装備 q19 が 0.1 未満の種')
rows = []
for n in V:
    Lw = med(list(V[n]['L']['warm'].values()))
    if not (Lw >= 0.1): continue
    def r(k):
        W, Q = V[n][k]['warm'], V[n][k]['q19']; xs = [Q[s] / W[s] for s in W if s in Q and W[s] == W[s] and Q[s] == Q[s] and W[s] > 1e-12]
        return med(xs)
    low = sum(1 for x in V[n]['装備']['q19'].values() if x < 0.1)
    rows.append((Lw, f"{n}\t{Lw:.3g}→{med(list(V[n]['L']['q19'].values())):.3g}\t{r('L'):.3g}\t{r('兵K'):.3g}\t{r('暮らし'):.3g}\t{r('装備'):.3g}\t{r('残り'):.3g}\t{low}/{len(V[n]['装備']['q19'])}"))
for _, t in sorted(rows, reverse=True): print(t)
