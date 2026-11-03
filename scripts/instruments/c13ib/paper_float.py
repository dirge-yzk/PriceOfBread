# (812) c13ib 紙の台本：Toward・OverSteps に畳む前後の式を同じ乱数の盤面で両方計算して、食い違いの数を数える。
# Python の float は IEEE 754 の倍精度で、a*b+c を融合しない（.NET の RyuJIT も自動では融合しない）。
# 予言＝食い違い 0。0 でなければその形は外す。
import random, math, sys

random.seed(812)
N = 1_000_000

def rnd():
    # 幅広い桁（1e-9〜1e9）と符号を混ぜる
    return random.choice((-1.0, 1.0)) * 10.0 ** random.uniform(-9, 9)

def unit():
    return random.random()

def toward(x, t, k):
    return x + k * (t - x)

def over_steps(r, n):
    return 1.0 - math.pow(1.0 - r, n)

bad = {'A':0, 'B':0, 'AB_product':0, 'A_product_K':0, 'over_steps':0, 'clamp_wrap':0}
for _ in range(N):
    x, t, k, a, b = rnd(), rnd(), unit(), unit(), unit()
    # 形 A：x += k*(t-x)
    old = x
    old += k * (t - old)
    if old != toward(x, t, k): bad['A'] += 1
    # 形 B：x += (t-x)*k
    oldb = x
    oldb += (t - oldb) * k
    if oldb != toward(x, t, k): bad['B'] += 1
    # K が積：x += a*b*(t-x)  は (a*b)*(t-x)
    oldp = x
    oldp += a * b * (t - oldp)
    if oldp != toward(x, t, a * b): bad['A_product_K'] += 1
    # 式の中の形：x + k*(t-x)（Clamp01 や三項の中）
    if (x + k * (t - x)) != toward(x, t, k): bad['clamp_wrap'] += 1
    # OverSteps：1 - pow(1 - r, n)（n は 1..90 の整数）
    r = unit() * 0.5
    n = random.randint(1, 90)
    if (1.0 - math.pow(1.0 - r, n)) != over_steps(r, float(n)): bad['over_steps'] += 1

print('組数', N)
for k_, v in bad.items():
    print(k_, '食い違い', v)
sys.exit(0 if sum(bad.values()) == 0 else 1)
