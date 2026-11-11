# (882) 下調べ（旗 0 の 3 本だけ・腕を見ない）：世界の κ の窓ごとの効きとヌル腕の幅・収穫係数の散らばり
import sys, json, statistics as st
R = json.load(open(sys.argv[1], encoding="utf-8"))
WINS = ("1783-85", "1786-99", "1800-49", "1850-99", "1900-50")
B = ("M", "N1", "N2")
def kap(a, pre, key): return 1 - a[pre + key] / a[pre + "sp"] if a.get(pre + "sp", 0) > 0 else float("nan")
for pre, lab in (("k_", "全て"), ("k_t0_", "T=T0"), ("k_t1_", "T≦1.02"), ("k_t2_", "T≦1.1"), ("k_t3_", "T>1.1")):
    print("== 母集合", lab)
    for wn in WINS:
        eff = []; rng = []; rng2 = []; mu = {k: [] for k in ("m1", "m3", "m6")}; n = 0
        for s in range(1, 11):
            a = {b: R[b][str(s)][wn] for b in B}
            if not all(a[b].get(pre + "sp", 0) > 0 for b in B): continue
            k0 = [kap(a[b], pre, "s0") for b in B]; k2 = [kap(a[b], pre, "s2") for b in B]
            eff.append(k2[0] - k0[0]); rng.append(max(k0) - min(k0)); rng2.append(max(k2) - min(k2)); n += a["M"][pre + "n"]
            for k in mu: mu[k].append(kap(a["M"], pre, k) - k0[0])
        if not eff: print(wn, "なし"); continue
        print("%s 行 %d：効き κ′−κ0 中央 %.5f（種ごと 最小 %.5f）・幅 κ0 中央 %.5f 最大 %.5f・幅 κ′ 中央 %.5f・効き÷幅 %.2f・Mu1 %.5f Mu3 %.5f Mu6 %.5f" % (
            wn, n, st.median(eff), min(eff), st.median(rng), max(rng), st.median(rng2), st.median(eff) / st.median(rng) if st.median(rng) > 0 else float("inf"),
            st.median(mu["m1"]), st.median(mu["m3"]), st.median(mu["m6"])))
print("== 収穫係数 H の |ln H| の sp 加重平均")
for wn in WINS:
    v = {b: [R[b][str(s)][wn]["h_abs"] / R[b][str(s)][wn]["h_sp"] for s in range(1, 11)] for b in B}
    l = {b: [R[b][str(s)][wn]["h_ln"] / R[b][str(s)][wn]["h_sp"] for s in range(1, 11)] for b in B}
    rng = [max(v[b][i] for b in B) - min(v[b][i] for b in B) for i in range(10)]
    print("%s：M の中央 %.5f・幅の中央 %.5f 最大 %.5f・Mu2（2 倍）の差の中央 %.5f・ln H の平均 M 中央 %.5f" % (wn, st.median(v["M"]), st.median(rng), max(rng), st.median(v["M"]), st.median(l["M"])))
