# (793) 紙の台本：依存の更新の漸化式・不動点・発散の境（係数は Coefficients.cs / Catalog.cs の値）
# 今の式（main 3b95b218）：
#  EconomyStep.cs:1751  need_pop = (B*g + s) * Size        g = clamp01(MiseryScale*(1-lastSupply))
#  EconomyStep.cs:1757〜1806 desired = need*priceFactor ; demand = min(desired, budget/buyerPrice)
#  EconomyStep.cs:1811〜1814 coreDemand = popDemand + inputNeed + invest + mil + rebel + trickle ; coreSold = min(available, coreDemand)
#  EconomyStep.cs:1833  pc = (coreSold+effSold)/totalPop
#  Habit.cs:34          s' = clamp(s + G*pc - D*s, 0, Cap)
G, D, B, MIS = 0.1, 0.02, 0.02, 2.0          # OpiumAddictionGrowth, Decay, BaseNeed(Catalog.cs:155), OpiumMiseryScale
PFMIN, PFMAX_LUX, CAP = 0.4, 2.0, 0.2       # PriceFactorMin, DemandElasticityMaxLuxury, OpiumAddictionCap
print("[1] 今の式（POP だけ・予算が縛らない）: pc = sigma*(B*g + s), sigma = priceFactor*ration")
print("    s' = s*(1 - D + G*sigma) + G*sigma*B*g")
print("    発散の境 sigma* = D/G =", D/G, " ＝ priceFactor の下端 PFMIN =", PFMIN, "より小さい")
print("    ＝予算と配給が縛らなければ、値段がどれほど高くても sigma >= PFMIN*ration で、ration >= %.2f なら必ず発散" % (D/G/PFMIN))
for sig in (0.1, 0.15, 0.19, 0.21, 0.4, 1.0, 2.0):
    if sig < D/G: print("    sigma=%.2f g=1: 不動点 s* = %.4f" % (sig, G*sig*B/(D-G*sig)))
    else: print("    sigma=%.2f: 毎月 ×%.3f で増える（倍の月数 %.1f）" % (sig, 1-D+G*sig, __import__('math').log(2)/__import__('math').log(1-D+G*sig)))
print("    値段の条件（ration=1）：sigma<0.2 には priceFactor<0.2 が要るが下端 0.4 で切られる＝値段では止まらない")
print("[2] 予算が縛る（demand = budget/buyerPrice）：pc = R/(P*N)（R＝アヘンの順番での残りの予算）→ s* = (G/D)*pc = 5*R/(P*N)")
print("    ＝不動点は所得と値段で決まる（有界）。在庫の単位は〈1 人あたり必要量〉＝人の割合ではない。在庫 113 は 1 人あたり月 22.6 単位の消費の釣り合い")
print("[3] 蓋 0.2（旗 0）：蓋に張り付く条件 G*sigma*(B*g+CAP) >= D*CAP → sigma >= %.3f（g=1）" % (D*CAP/(G*(B+CAP))))
print("[4] 次の波の形（紙の不動点・g=1）")
import math
def fp(f, lo=0.0, hi=1e6):
    # f(s)=s'-s の正の根（単調でなくても s=lo から右へ最初の符号の変化）
    s, step = lo, 1e-4
    prev = f(s)
    while s < hi:
        s2 = s + step; v = f(s2)
        if prev > 0 and v <= 0:
            a, b = s, s2
            for _ in range(80):
                m = (a+b)/2
                if f(m) > 0: a = m
                else: b = m
            return (a+b)/2
        prev, s = v, s2
        if s > 1: step = s*1e-3
    return float('inf')
for d in (0.2,):
    print("  形 H（依存を人の割合 h∈[0,1] にする＝SIS 型）：h' = h + G*x*(1-h) - D*h, x = pc/d（d＝依存者 1 人の量・ここでは %.2f）、need = (B*g + h*d)*Size、労働の害 = 害×h" % d)
    print("    ＝在庫の単位のまま書くと s = d*h：s' = s + G*pc*(1 - s/d) - D*s（ロジスティック・漸近点 d＝今の蓋の値・(339) §10 の推奨の形）。pc <= PFMAX*(B+d) = %.2f < d/G なら s' < d が毎月成り立つ（切らない）" % (PFMAX_LUX*(B+d)))
    for sig in (0.1, 0.2, 0.25, 0.3, 0.4, 1.0, 2.0):
        f = lambda h, sig=sig: G*sig*(B + h*d)/d*(1-h) - D*h
        print("    sigma=%.2f: h* = %.4f（need の上乗せ %.4f）" % (sig, fp(f), fp(f)*d))
print("  形 A（成長を〈必要を満たした割合〉で＝s' = s + G*B*g*phi - D*s, phi = 消費/必要）：s* = (G/D)*B*g*phi = %.2f*phi（phi<=2 で <= %.2f）" % (G/D*B, G/D*B*PFMAX_LUX))
print("  形 E（習慣の財だけ priceFactor の下端を外す＝値段で止める）：sigma<0.2 に要る値段 P/Ref = 0.2^(-1/0.8) = %.2f 倍・その上で s* は供給/人口で決まる（s≈5*供給per capita）" % (0.2**(-1/0.8)))
