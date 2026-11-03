# (805) c12cm 紙の台本：旧の字句の式と新の関数の式を同じ入力で並べ、食い違いの数を数える（事前登録 [1] (a3)・予言 0）。
#   Python の float は IEEE 754 の倍精度＝C# の double と同じ演算（FMA の自動の畳みは C# の JIT にも Python にも無い）。式の字句は元のソースから写した。
#   使い方: python -X utf8 -I paper.py [件数=1000000]
import sys, math, random, struct
N = int(sys.argv[1]) if len(sys.argv) > 1 else 1000000
EPS = 1e-9
rnd = random.Random(805)
def nxt(x, k):   # k ulp だけずらす
    b = struct.unpack('<q', struct.pack('<d', x))[0]
    return struct.unpack('<d', struct.pack('<q', b + k))[0]
# (1) 住居の門
#   旧（EconomyStep）：urbanShare = totalPop > Eps ? urbanPop / totalPop : 0;  housingGate = urbanShare > thr ? 1.0 : 0.0
#   旧（PopDecision）：urbanShare = totalPop > Eps ? urbanPop / totalPop : 0.0;  urbanShare > thr
#   新：HousingGateOpen(coef, urbanPop, totalPop) => (totalPop > Eps ? urbanPop / totalPop : 0.0) > thr
def old_econ(urban, total, thr): return 1.0 if ((urban / total if total > EPS else 0.0) > thr) else 0.0
def old_pop(urban, total, thr): return (urban / total if total > EPS else 0.0) > thr
def new_gate(urban, total, thr): return (urban / total if total > EPS else 0.0) > thr
mis = 0; n1 = 0
for i in range(N):
    total = rnd.choice([0.0, 1e-10, 1e-9, nxt(1e-9, 1), rnd.random() * 1e3, rnd.random() * 1e8, rnd.random() * 1e12])
    thr = rnd.choice([0.25, 0.45, 0.0, 0.9, rnd.random()])
    if rnd.random() < 0.3 and total > EPS:
        urban = nxt(thr * total, rnd.randint(-3, 3))   # 閾値の際
    else:
        urban = total * rnd.random()
    urban = max(0.0, urban)
    a = old_econ(urban, total, thr) == 1.0; b = old_pop(urban, total, thr); c = new_gate(urban, total, thr)
    n1 += 1
    if not (a == b == c): mis += 1
print(f'(1) 住居の門：{n1} 件・旧(EconomyStep)・旧(PopDecision)・新の食い違い {mis}')
# 加算の順：EconomyStep の 1 巡の局所の和と SumPopulation／SumUrbanPopulation の 2 巡の和（Pops の順に 0 から足す）
mis2 = 0
for i in range(2000):
    sizes = [rnd.random() * rnd.choice([1.0, 1e3, 1e6]) for _ in range(rnd.randint(1, 12))]
    urb = [rnd.random() < 0.4 for _ in sizes]
    t = u = 0.0
    for s_, f in zip(sizes, urb):
        t += s_
        if f: u += s_
    t2 = 0.0
    for s_ in sizes: t2 += s_
    u2 = 0.0
    for s_, f in zip(sizes, urb):
        if f: u2 += s_
    if t != t2 or u != u2: mis2 += 1
print(f'(1b) 加算の順（1 巡の局所の和 と 2 巡の和）：2000 盤面・食い違い {mis2}')
# (2) 工業の飽和
#   旧（indMult）：1.0 + Gain * effInd / (1.0 + effInd / Scale)　旧（InvestmentReturn）：sat = 1.0 + ind / Scale
#   新：IndSaturation(ind, coef) => 1.0 + ind / Scale ；indMult = 1.0 + Gain * effInd / IndSaturation(effInd, coef)
def old_mult(gain, ind, scale): return 1.0 + gain * ind / (1.0 + ind / scale)
def new_mult(gain, ind, scale):
    sat = 1.0 + ind / scale
    return 1.0 + gain * ind / sat
mis3 = 0
for i in range(N):
    gain = rnd.choice([1.2, 0.5, rnd.random() * 3]); scale = rnd.choice([2.5, 1.0, 10.0, rnd.random() * 20 + 0.01]); ind = rnd.choice([0.0, rnd.random() * 5, rnd.random() * 1e3, 1e-12])
    if old_mult(gain, ind, scale) != new_mult(gain, ind, scale): mis3 += 1
    if (1.0 + ind / scale) != (1.0 + ind / scale): mis3 += 1
print(f'(2) 工業の飽和：{N} 件・食い違い {mis3}')
print('総合：食い違い', mis + mis2 + mis3)
