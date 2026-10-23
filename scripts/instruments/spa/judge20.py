"""(spa) 20 種の判定（事前登録 measurements/spa_20261023_prereg.txt 追記 2）。読むだけ。
使い方：python judge20.py <S0 の spg2_metrics の出力> <S1 の出力> <S1c の出力>
各項目：S0 の四分位（25・50・75%）・腕の中央値・中央値の差・種で組にした符号の検定（腕−S0 が正の種の数／差が 0 でない種の数・両側の二項の p）。
採らない条件：貿易÷GDP（3 年）か植民地と本国の交易（3 年）のどちらかの量で、腕の中央値が S0 の 25% 点より下の年が 2 つ以上。補給崩壊：腕の中央値が S0 の中央値の 2 倍を超える。"""
import csv, sys, math, statistics
sys.stdout.reconfigure(encoding='utf-8')


def load(p):
    rows = list(csv.reader(open(p, encoding='utf-8'), delimiter='\t'))
    h = rows[0]
    return {int(r[0]): {h[i]: float(r[i]) for i in range(1, len(h))} for r in rows[1:] if r[0].isdigit()}


def q(v, p):
    v = sorted(v); i = p * (len(v) - 1); lo = math.floor(i); hi = math.ceil(i)
    return v[lo] + (v[hi] - v[lo]) * (i - lo)


def sign_p(pos, n):
    if n == 0: return 1.0
    k = min(pos, n - pos)
    return min(1.0, 2 * sum(math.comb(n, j) for j in range(0, k + 1)) / 2 ** n)


S0 = load(sys.argv[1]); arms = {'S1': load(sys.argv[2]), 'S1c': load(sys.argv[3])}
groups = {'貿易÷GDP': ['tradeGdp1850', 'tradeGdp1900', 'tradeGdp1914'], '植民地と本国の交易': ['colonialTrade1850', 'colonialTrade1900', 'colonialTrade1910']}
seeds = sorted(S0)
print(f'種 {len(seeds)}（S0）・' + '・'.join(f'{a} {len(v)}' for a, v in arms.items()))
for a, A in arms.items():
    print(f'\n== {a} ==')
    verdict = []
    for g, ks in groups.items():
        below = 0
        for k in ks:
            s0 = [S0[s][k] for s in seeds if s in A]; av = [A[s][k] for s in seeds if s in A]
            q25, q50, q75 = q(s0, .25), q(s0, .5), q(s0, .75); m = statistics.median(av)
            d = [A[s][k] - S0[s][k] for s in seeds if s in A]; pos = sum(1 for x in d if x > 0); nz = sum(1 for x in d if x != 0)
            low = m < q25; below += low
            print(f'  {k}: S0 四分位 {q25:.4g}／{q50:.4g}／{q75:.4g}・{a} 中央値 {m:.4g}（差 {m - q50:+.4g}）・{"25% 点の下" if low else "内"}・符号 {pos}/{nz}（p={sign_p(pos, nz):.3g}）')
        verdict.append((g, below))
    c0 = statistics.median(S0[s]['collapse'] for s in seeds); ca = statistics.median(A[s]['collapse'] for s in A)
    print(f'  補給崩壊（種の中央値）：S0 {c0:g}・{a} {ca:g}・線 {2 * c0:g} → {"止める" if ca > 2 * c0 else "内"}')
    bad = [g for g, b in verdict if b >= 2]
    print(f'  判定：{"採らない（" + "・".join(f"{g} {b} 年" for g, b in verdict if b >= 2) + "）" if bad or ca > 2 * c0 else "採らない条件に当たらない"}（' + '・'.join(f'{g} 下 {b} 年' for g, b in verdict) + '）')
