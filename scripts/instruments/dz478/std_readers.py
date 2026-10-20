# (478) 線 dz：標準のピラミッド（8 層＝今・11 層＝新）の導出と、年齢の読み手が標準の盤面で何%動くかの表。
# 読むだけ（SimCore を触らない）。使い方：PYTHONUTF8=1 python scripts/instruments/dz478/std_readers.py
# 11 層の導き方（AI の決め〈確証なし〉）：8 層の累積分布 F（10 歳刻みの境で厳密に通る）を単調な 3 次補間（Fritsch–Carlson の PCHIP）で
#   新しい境（1・6・11・16・21・31・41・51・61・71）へ引く。0 歳の層だけ 0.030（粗出生 0.035÷(1+死亡率+増加率 0.005)の安定人口の形・乳児死亡 約14%）。
#   71 以上は 70 の境の密度（60〜69 の層の平均）の 0.7 年ぶんだけ延ばして切る。層の和は 1。
#   死の年齢の重み μ は今と同じ規則（標準のピラミッドが 1783 年の出生で動かない重み）を 11 層の幅で導く。
import math, sys
G = 0.005; B = 0.035
OLD_P = [0.255, 0.195, 0.160, 0.130, 0.105, 0.080, 0.050, 0.025]
OLD_LO = [0, 10, 20, 30, 40, 50, 60, 70]; OLD_W = [10]*7 + [None]
NEW_P = [0.0300, 0.1317, 0.1150, 0.1005, 0.0903, 0.1568, 0.1273, 0.1025, 0.0770, 0.0474, 0.0215]   # 下の derive() が出す値を 4 桁に丸めた物（データ）
NEW_LO = [0, 1, 6, 11, 16, 21, 31, 41, 51, 61, 71]; NEW_W = [1, 5, 5, 5, 5, 10, 10, 10, 10, 10, None]

def pchip(x, y):
    n = len(x); h = [x[i+1]-x[i] for i in range(n-1)]; d = [(y[i+1]-y[i])/h[i] for i in range(n-1)]
    m = [0.0]*n
    for i in range(1, n-1):
        if d[i-1]*d[i] > 0:
            w1 = 2*h[i]+h[i-1]; w2 = h[i]+2*h[i-1]; m[i] = (w1+w2)/(w1/d[i-1]+w2/d[i])
    def end(h0, h1, d0, d1):
        mm = ((2*h0+h1)*d0-h0*d1)/(h0+h1)
        if mm*d0 <= 0: return 0.0
        if d0*d1 <= 0 and abs(mm) > 3*abs(d0): return 3*d0
        return mm
    m[0] = end(h[0], h[1], d[0], d[1]); m[-1] = end(h[-1], h[-2], d[-1], d[-2])
    def f(t):
        for i in range(n-1):
            if x[i] <= t <= x[i+1]:
                s = (t-x[i])/h[i]
                return (2*s**3-3*s**2+1)*y[i]+(s**3-2*s**2+s)*h[i]*m[i]+(-2*s**3+3*s**2)*y[i+1]+(s**3-s**2)*h[i]*m[i+1]
        return y[-1]
    return f

def derive():
    xs = [0, 1, 10, 20, 30, 40, 50, 60, 70]; Fv = [0.0, 0.030]; c = 0.0
    for v in OLD_P[:-1]: c += v; Fv.append(c)
    f = pchip(xs, Fv)
    Fe = [f(e) for e in NEW_LO[:-1]] + [0.975 + (0.975-0.925)/10*0.7]
    return [Fe[i+1]-Fe[i] for i in range(10)] + [1-Fe[-1]]

def overlap(lo_b, w, last, lo, hi):
    if last: return 1.0 if (lo <= lo_b and math.isinf(hi)) else 0.0
    ov = min(hi, lo_b+w) - max(lo, lo_b)
    return ov/w if ov > 0 else 0.0

def share(P, LO, W, lo, hi, sex=-1):
    s = 0.0
    for b in range(len(P)):
        ov = overlap(LO[b], W[b] or 0, W[b] is None, lo, hi)
        if ov > 0: s += ov*(P[b] if sex < 0 else 0.5*P[b])
    return s

def mu(P, W):
    out = [P[i]/W[i] if W[i] else 0.0 for i in range(len(P))]; r = []; infl = B
    for i in range(len(P)):
        r.append((infl-out[i])/P[i]-G); infl = out[i]
    return r

if __name__ == "__main__":
    dv = derive()
    print("導出（丸め前）:", [round(x, 5) for x in dv], "和", round(sum(dv), 6))
    print("採用（4桁）  :", NEW_P, "和", round(sum(NEW_P), 6))
    print("\n死の年齢の重み μ（年率・標準のピラミッドが動かない重み）")
    mo = mu(OLD_P, OLD_W); mn = mu(NEW_P, NEW_W)
    print("  今の 8 層:", [f"{OLD_LO[i]}:{mo[i]:.4f}" for i in range(8)])
    print("  新の 11 層:", [f"{NEW_LO[i]}:{mn[i]:.4f}" for i in range(11)], "最小", round(min(mn), 4))
    print("  平均（Σμn＝粗死亡率）今", round(sum(m*p for m, p in zip(mo, OLD_P)), 6), "新", round(sum(m*p for m, p in zip(mn, NEW_P)), 6), "（出生−増加率＝", B-G, "）")
    ir = mn[0]/(1+mn[0]+G); print("  0 歳の層の死亡率 μ0 =", round(mn[0], 4), "→ 乳児死亡 μ0/(1+μ0+g) =", round(ir, 4))
    rows = [
        ("学齢（教育の必要量・EconomyStep:1569・Coefficients:547）", 5, 15, -1, "起点合わせ済み：EduPerChild＝0.06÷StdShare(5,15)"),
        ("働く大人 15〜64（EconomyStep:779）男", 15, 65, 0, ""),
        ("老いの労働 65 以上（EconomyStep:780）男", 65, math.inf, 0, ""),
        ("児童労働 10〜14（EconomyStep:784）", 10, 15, -1, ""),
        ("年金と老年 60 以上（Fiscal:535・551）", 60, math.inf, -1, "PensionOldWeight の錨は StdShare(60,∞)"),
        ("選挙 21 以上（InterestGroups:109）", 21, math.inf, -1, ""),
        ("働く大人 16〜59（Military:161）", 16, 60, -1, ""),
        ("徴兵の母数 16〜59 男（Military:388・WorkingMenStdShare）", 16, 60, 0, "起点合わせ済み：約 8 本の換算が割る"),
        ("徴兵 20〜39 男（Military:373）", 20, 40, 0, ""),
        ("出産のプール 15〜49 女（Simulation:2390）", 15, 50, 1, "起点合わせ済み（InitialFertileShare・FertilePoolOf(標準)）"),
        ("子 0〜14（Header・Refresh・App）", 0, 15, -1, ""),
        ("子 0〜19（InstrumentsPop）", 0, 20, -1, ""),
        ("大人 20 以上（Model GenerationalMix・World ReplacementRate）", 20, math.inf, -1, "新は 21 以上へ寄せる"),
        ("大人 21 以上（新の GenerationalMix の読み）", 21, math.inf, -1, ""),
    ]
    print("\n標準の盤面での読み手の値（今 8 層 → 新 11 層）")
    for name, lo, hi, sex, note in rows:
        a = share(OLD_P, OLD_LO, OLD_W, lo, hi, sex); b = share(NEW_P, NEW_LO, NEW_W, lo, hi, sex)
        print(f"  {name}: {a:.4f} → {b:.4f}  ({(b/a-1)*100:+.2f}%)  {note}")
    # 世代の置き換わり率（1 か月あたりの大人に入る流れ÷規模）
    eo = (OLD_P[1]/120.0)           # 10〜19 の層 → 20〜29（男女計）
    en = NEW_P[4]/60.0              # 16〜20 の層 → 21〜30（男女計）
    print(f"\n大人に入る流れ（月あたり・規模比）: 今 {eo:.6f} → 新 {en:.6f} ({(en/eo-1)*100:+.2f}%)")
    ao = share(OLD_P, OLD_LO, OLD_W, 20, math.inf); an = share(NEW_P, NEW_LO, NEW_W, 21, math.inf)
    for mths in (3,):
        ro = mths*eo/(ao+mths*eo); rn = mths*en/(an+mths*en)
        print(f"  四半期の置き換わり率 E/(A+E): 今 {ro:.5f} → 新 {rn:.5f} ({(rn/ro-1)*100:+.2f}%)")
    # 兵士の初期の構成：男の 20〜39（今：20〜29 と 30〜39 の比 16:13）→ 新：21〜30 と 31〜40 の比
    print(f"\n兵士の初期の構成（男）: 今 20〜29:{OLD_P[2]/(OLD_P[2]+OLD_P[3]):.4f}・30〜39:{OLD_P[3]/(OLD_P[2]+OLD_P[3]):.4f}  新 21〜30:{NEW_P[5]/(NEW_P[5]+NEW_P[6]):.4f}・31〜40:{NEW_P[6]/(NEW_P[5]+NEW_P[6]):.4f}")
