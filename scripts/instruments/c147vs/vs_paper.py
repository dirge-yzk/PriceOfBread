# (1384) c147vs 紙の台本：兵の給与の請求書を前の月の値で読んだら、同じ月の使える歳入（SpendableRevenue）がどれだけ下がるか。
#   python -I vs_paper.py <計器の生のフォルダ（fiscal_s*.tsv）> [旗=1]
#   旧（今の木）の記録：adminSpendableAtBudgetPerMonth＝請求書 0 で読んだ使える歳入 S0・adminLastRevenueAtBudgetPerMonth＝R。
#   潤沢の線の余り E＝(S0−R)×12／率（率＝OpulenceSpendRatePerYear 0.6）。請求書を前の月の値 B で読むと線が D＝重み 0.5×月数 5×B×8 四半期だけ上がる。
#   新の使える歳入 S1＝R＋率／12×max(0, E−D)。旗 0 なら D＝0（S1＝S0＝差 0）。軍の予算の枠と行政の予算は S に比例（戦の段の項を除く）。
#   1 次の見込み（新の木で国庫と請求書が動く分を含まない）。
import sys, os, glob, statistics as st

sys.stdout.reconfigure(encoding="utf-8")
d = sys.argv[1]; flag = float(sys.argv[2]) if len(sys.argv) > 2 else 1.0
RATE, W, M, Q = 0.6, 0.5, 5.0, 8.0
tot = above = below = drop0 = 0
ratios = []; bynat = {}
for p in sorted(glob.glob(os.path.join(d, 'fiscal_s*.tsv'))):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        prev = {}
        for line in f:
            a = line.rstrip('\n').split('\t')
            n = a[ix['nation']]
            try:
                s0 = float(a[ix['adminSpendableAtBudgetPerMonth']]); r = float(a[ix['adminLastRevenueAtBudgetPerMonth']])
                bill = float(a[ix['claimSoldierPayPerMonth']]); mi = int(a[ix['monthIndex']])
            except ValueError:
                continue
            pb = prev.get(n); prev[n] = (mi, bill)
            if pb is None or pb[0] != mi - 1 or s0 != s0 or r != r:
                continue
            tot += 1
            e = (s0 - r) * 12.0 / RATE
            if not (e > 1e-9):
                below += 1; continue
            above += 1
            dd = flag * W * M * pb[1] * Q
            s1 = s0 - RATE / 12.0 * min(e, dd)   # ＝R＋率／12×max(0, E−D)（旗 0 で D＝0 なら S0 そのもの）
            if e - dd <= 0: drop0 += 1
            if s0 > 1e-9:
                q = s1 / s0; ratios.append(q); bynat.setdefault(n, []).append(q)
def pct(v, x):
    v = sorted(v); return v[min(len(v) - 1, int(x * len(v)))] if v else float('nan')
print(f'# vs_paper 旗={flag} 生={d}')
print(f'国×月（前の月の請求書あり） {tot}・線の上 {above}（{above / max(tot, 1):.3f}）・線の下〈差 0〉 {below}・線の上で余りが消える {drop0}')
if ratios:
    print(f'線の上の S1／S0：中央 {st.median(ratios):.4f}・10% {pct(ratios, .1):.4f}・90% {pct(ratios, .9):.4f}・最小 {min(ratios):.4f}・差 0 でない {sum(1 for q in ratios if q != 1.0)}')
    print(f'全ての国×月の S1／S0 の平均（線の下を 1 で数える） {(sum(ratios) + below) / max(tot, 1):.4f}')
for n in sorted(bynat, key=lambda k: -len(bynat[k])):
    v = bynat[n]; print(f'{n}\t線の上 {len(v)}\t中央 {st.median(v):.4f}\t10% {pct(v, .1):.4f}')
