# 使い方: python -X utf8 -I rec_k49.py <rawdir> <種の起点> <種の数> <札>
# (1298) c134k49 記録の読み手（読むだけ・採否に使わない）：9 国（計器の国）の
#  ① 開幕（1783 年の最初の四半期）の工場の能力 capacityRefValuePerMonth と産出 outputRefValuePerMonth（助走の終わり＝この線が直接動かす所）
#  ② 能力の四半期の変化率 |ln(C_q/C_{q−1})| の分布（中央・90%・99%・最大）＝規模の四半期の変化率の代わり（規模の生産者ごとの表は 10 年ごと＝四半期が無い）
#  ③ 工場の倒産 factoryBankruptCount の国×四半期の平均と最大
#  ④ 名目 GDP（nation_s の nominalGdpPerYear）の四半期の振れ |ln(Y_q/Y_{q−1})| の分布（中央・90%・99%）
#  を種をまとめて出す。
import sys, os, math, statistics as st
NINE = ["イギリス", "フランス", "プロイセン", "ドイツ帝国", "アメリカ", "ロシア", "オーストリア", "オスマン", "清", "日本"]
def fl(x):
    try: return float(x)
    except: return float("nan")
def q(v, p):
    if not v: return float("nan")
    v = sorted(v); return v[min(len(v) - 1, int(p * (len(v) - 1) + 0.5))]
def rows(p):
    with open(p, encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            yield ix, l.rstrip("\n").split("\t")
def main():
    raw, s0, n, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
    open0 = {}; dC = []; bk = []; dY = []; ns = 0
    for s in range(s0, s0 + n):
        pf, pn = f"{raw}/factory_s{s}.tsv", f"{raw}/nation_s{s}.tsv"
        if not (os.path.exists(pf) and os.path.exists(pn)): continue
        ns += 1
        prev = {}
        for ix, c in rows(pf):
            nm = c[ix["nation"]]
            if nm not in NINE: continue
            qq = int(c[ix["quarter"]]); C = fl(c[ix["capacityRefValuePerMonth"]])
            if qq == 0: open0.setdefault(nm, []).append((C, fl(c[ix["outputRefValuePerMonth"]])))
            pq = prev.get(nm)
            if pq is not None and pq[0] == qq - 1 and pq[1] > 0 and C > 0: dC.append(abs(math.log(C / pq[1])))
            prev[nm] = (qq, C)
            bk.append(fl(c[ix["factoryBankruptCount"]]))
        prev = {}
        for ix, c in rows(pn):
            nm = c[ix["nation"]]
            if nm not in NINE: continue
            qq = int(c[ix["quarter"]]); Y = fl(c[ix["nominalGdpPerYear"]])
            pq = prev.get(nm)
            if pq is not None and pq[0] == qq - 1 and pq[1] > 0 and Y > 0: dY.append(abs(math.log(Y / pq[1])))
            prev[nm] = (qq, Y)
    print(f"# rec_k49 {tag} {raw} 種 {s0}..{s0+n-1} 読めた種 {ns}")
    print("① 開幕（四半期 0）の工場の能力と産出（種の中央）")
    for nm in NINE:
        v = open0.get(nm)
        if not v: continue
        print(f"  {nm}\tcapacity {st.median(x[0] for x in v):.6g}\toutput {st.median(x[1] for x in v):.6g}")
    print(f"② 能力の四半期の |Δln| n={len(dC)} 中央 {q(dC,0.5):.5g} 90% {q(dC,0.9):.5g} 99% {q(dC,0.99):.5g} 最大 {max(dC) if dC else float('nan'):.5g}")
    bv = [x for x in bk if x == x]
    print(f"③ 工場の倒産の数 国×四半期 n={len(bv)} 平均 {st.mean(bv) if bv else float('nan'):.5g} 最大 {max(bv) if bv else float('nan'):.5g}")
    print(f"④ 名目 GDP の四半期の |Δln| n={len(dY)} 中央 {q(dY,0.5):.5g} 90% {q(dY,0.9):.5g} 99% {q(dY,0.99):.5g}")
main()
