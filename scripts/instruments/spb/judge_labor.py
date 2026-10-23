"""(spb) 労働の止める線の判定（事前登録 measurements/spb_20261024_prereg.txt ⑤）。読むだけ。
使い方：python judge_labor.py <S0 の spb_labor.py の出力> <腕の出力>
量ごと（unempPlain・unempFarm・floor）に、腕の中央値が S0 の四分位の範囲（25〜75% 点）の外（上か下）の年を数え、2 年以上なら採らない。
S0 の四分位が 0〜0 の年は、腕の中央値が 0 でなければ外に数える（区間の外の定義どおり）。符号の検定（種で組）も並べる（記録だけ）。"""
import csv, sys, math, statistics
sys.stdout.reconfigure(encoding='utf-8')


def load(p):
    rows = list(csv.reader(open(p, encoding='utf-8'), delimiter='\t'))
    h = rows[0]
    return h, {int(r[0]): {h[i]: float(r[i]) for i in range(1, len(h))} for r in rows[1:] if r[0].isdigit()}


def q(v, p):
    v = sorted(v); i = p * (len(v) - 1); lo = math.floor(i); hi = math.ceil(i)
    return v[lo] + (v[hi] - v[lo]) * (i - lo)


def sign_p(pos, n):
    if n == 0: return 1.0
    k = min(pos, n - pos)
    return min(1.0, 2 * sum(math.comb(n, j) for j in range(0, k + 1)) / 2 ** n)


h, S0 = load(sys.argv[1]); _, A = load(sys.argv[2])
seeds = sorted(s for s in S0 if s in A)
print(f'種 {len(seeds)}（S0 {len(S0)}・腕 {len(A)}）')
groups = {}
for k in h[1:]:
    base = k.rstrip('0123456789'); y = int(k[len(base):])
    if base == 'floor' and y < 1900: continue
    groups.setdefault(base, []).append(k)
bad = []
for g, ks in groups.items():
    out = 0
    for k in ks:
        s0 = [S0[s][k] for s in seeds if S0[s][k] == S0[s][k]]; av = [A[s][k] for s in seeds if A[s][k] == A[s][k]]
        if not s0 or not av: print(f'  {k}: 値なし'); continue
        q25, q50, q75 = q(s0, .25), q(s0, .5), q(s0, .75); m = statistics.median(av)
        o = m < q25 or m > q75; out += o
        d = [A[s][k] - S0[s][k] for s in seeds]; pos = sum(1 for x in d if x > 0); nz = sum(1 for x in d if x != 0)
        print(f'  {k}: S0 四分位 {q25:.4g}／{q50:.4g}／{q75:.4g}・腕 中央値 {m:.4g}（差 {m - q50:+.3g}）・{"外" if o else "内"}・符号 {pos}/{nz}（p={sign_p(pos, nz):.3g}）')
    print(f'  → {g}：外 {out} 年')
    if out >= 2: bad.append(f'{g} {out} 年')
print('判定：' + ('採らない（' + '・'.join(bad) + '）' if bad else '採らない条件に当たらない'))
