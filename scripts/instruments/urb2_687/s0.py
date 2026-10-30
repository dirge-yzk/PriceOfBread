# (687) 線 urb2 段0：都市化の天井（UrbanShareCap）の当たり・開幕・所得差の読み（読むだけ・新しい走りゼロ）
# 使い方：PYTHONIOENCODING=utf-8 python scripts/instruments/urb2_687/s0.py <計器つき10種の出力フォルダ> [world.json]
# 入力：urxpd_s<種>.tsv（本国の州の PopDecision.Run の駆動・InstrumentsUrx.cs UrxPd）。出力：標準出力。
import csv, json, statistics as st, collections, sys
D = sys.argv[1].rstrip("/") + "/"
WJ = sys.argv[2] if len(sys.argv) > 2 else None
EPS = [(1783,1815),(1815,1850),(1850,1880),(1880,1914),(1914,1951)]
def ep(y):
    for a,b in EPS:
        if a <= y < b: return f"{a}-{b-1}"
SUMC = ["regionMonths","capBound","serfGated","famineGated","sumTotal","sumRegionPop","sumRegionUrban","wTargetPre","wTargetCapped","wCur","wCap","wIncU","wIncR","wZ","sumFlowPos","sumFlowNeg"]
res = collections.defaultdict(lambda: collections.defaultdict(list))
q0 = collections.defaultdict(lambda: collections.defaultdict(list))
over = collections.defaultdict(lambda: [0,0])   # 国×窓：cur>cap の四半期数／四半期数
for s in range(1, 11):
    acc = collections.defaultdict(lambda: collections.defaultdict(float))
    with open(D + f"urxpd_s{s}.tsv", encoding="utf-8") as f:
        for row in csv.DictReader(f, delimiter="\t"):
            y = int(row["year"]); n = row["nation"]; T = float(row["sumTotal"])
            if T <= 0: continue
            k = (n, ep(y)); a = acc[k]
            for c in SUMC: a[c] += float(row[c])
            a["q"] += 1
            if float(row["wCur"]) > float(row["wCap"]) + 1e-12: a["over"] += 1
            if int(row["quarter"]) == 0:
                q0[n]["cur"].append(float(row["wCur"])/T); q0[n]["cap"].append(float(row["wCap"])/T); q0[n]["pre"].append(float(row["wTargetPre"])/T)
    for k, a in acc.items():
        rm = a["regionMonths"]; T = a["sumTotal"]; d = res[k]
        d["capB"].append(a["capBound"]/rm); d["serf"].append(a["serfGated"]/rm); d["fam"].append(a["famineGated"]/rm)
        d["pre"].append(a["wTargetPre"]/T); d["cap"].append(a["wCap"]/T); d["cur"].append(a["wCur"]/T)
        d["ratio"].append(a["wIncU"]/a["wIncR"] if a["wIncR"] > 0 else 0); d["z"].append(a["wZ"]/T)
        d["gapK_q"].append((a["wTargetPre"]-a["wTargetCapped"])/3/a["q"])      # 天井で止められている目標の差（千人）の四半期平均
        d["net_q"].append((a["sumFlowPos"]-a["sumFlowNeg"])/a["q"])            # 今の四半期の純の流れ（千人・正＝農村→都市）
        d["over"].append(a["over"]/a["q"])
        d["pop"].append(a["sumRegionPop"]/3/a["q"])
med = st.median
nats = sorted({k[0] for k in res})
print("## (1) 天井の当たり（種の中央値）。capB＝天井が目標を切った州×月の割合・over＝都市の割合が天井より上の四半期の割合（天井が都市→農村へ押す）")
print("nation\tepoch\tcapB\tserfG\tfamG\tpre\tcap\tcur\tover\tz\tinc比U/R\tgapK/q\tnet千人/q\t本国人口千人")
for n in nats:
    for a, b in EPS:
        k = (n, f"{a}-{b-1}")
        if k in res:
            d = res[k]
            print(f"{n}\t{k[1]}\t{med(d['capB']):.2f}\t{med(d['serf']):.2f}\t{med(d['fam']):.2f}\t{med(d['pre']):.3f}\t{med(d['cap']):.3f}\t{med(d['cur']):.3f}\t{med(d['over']):.2f}\t{med(d['z']):.1f}\t{med(d['ratio']):.2f}\t{med(d['gapK_q']):.0f}\t{med(d['net_q']):.1f}\t{med(d['pop']):.0f}")
print()
print("## (2) 助走の直後（四半期 0＝第 1 四半期）の対の都市の割合 cur・天井 cap・目標 pre（種の中央値）と、world.json の初期の対（worker÷(peasant+worker)）")
init = {}
if WJ:
    w = json.load(open(WJ, encoding="utf-8"))
    for x in w["nations"]:
        init[x["name"]] = (x.get("worker", 0), x.get("peasant", 0))
print("nation\tcur@Q0\tcap@Q0\tpre@Q0\tworld.jsonの初期の対(全国の素朴な比)")
for n in nats:
    d = q0[n]
    ini = init.get(n)
    s = f"{ini[0]/(ini[0]+ini[1]):.3f}" if ini and ini[0]+ini[1] > 0 else "-"
    print(f"{n}\t{med(d['cur']):.3f}\t{med(d['cap']):.3f}\t{med(d['pre']):.2f}\t{s}")
