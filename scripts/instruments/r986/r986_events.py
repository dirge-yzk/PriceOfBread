# (986) 事象の表：対象の州の独立（cederule_s）・スペインの戦争（wars_s・nation_s）・戦争の年の保持力の上がり下がり（secession_s の国の列）。
# 使い方: python -X utf8 -I r986_events.py <生のフォルダ> <種の最初> <種の最後> <出力のフォルダ> <組の名>
import sys, os, collections as co, statistics as st
raw, s0, s1, out, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
os.makedirs(out, exist_ok=True)
SA = "ヌエバ・エスパーニャ銀地帯 中央メキシコ キューバ ペルー ヌエバ・グラナダ ラ・プラタ プエルトリコ カラカス ルイジアナ・フロリダ チリ サンタフェ エルパソ ロレート レオン ボゴタ キト イキトス ラパス トゥクマン アスンシオン カルメン・デ・パタゴネス プンタアレナス チロエ".split()
BR = "ブラジル アマゾン マナウス サルヴァドール サンルイス ゴイアス クイアバ".split()
SD = "北部サン=ドマング 南部サン=ドマング".split()
G = {**{n: "SA" for n in SA}, **{n: "BR" for n in BR}, **{n: "SD" for n in SD}}
o = [f"# (986) 事象 組 {tag} 種 {s0}〜{s1} 生 {raw}"]
o.append("## 表7 植民地格の州の独立（how=Seceded・tierBefore=Colonial）全ての州と対象の州・種ごと")
o.append("seed\t全 1783-1950\t全 1800-1830\t対象 1783-1950\t対象 1800-1830\t対象の行（年 州 送り手）")
how = co.Counter()
for s in range(s0, s1 + 1):
    allc = all30 = tg = tg30 = 0; ev = []
    with open(os.path.join(raw, f"cederule_s{s}.tsv"), encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.rstrip("\n").split("\t"); y = int(a[1])
            reg = a[ix["region"]]
            if reg in G: how[(a[ix["how"]], a[ix["tierBefore"]])] += 1
            if a[ix["how"]] != "Seceded" or a[ix["tierBefore"]] != "Colonial": continue
            allc += 1; all30 += 1800 <= y <= 1830
            if reg in G:
                tg += 1; tg30 += 1800 <= y <= 1830; ev.append(f"{y} {reg} {a[ix['sender']]}")
    o.append(f"{s}\t{allc}\t{all30}\t{tg}\t{tg30}\t" + "; ".join(ev))
o.append("## 表7b 対象の州の割譲の行（how×tierBefore・種の和）")
for (hw, tb), n in sorted(how.items(), key=lambda kv: -kv[1]): o.append(f"{hw}\t{tb}\t{n}")
o.append("## 表8 スペインが側の戦争（開始 1800〜1830）")
o.append("seed\t件数\t行（開始 内戦 A B 月 死者K）")
for s in range(s0, s1 + 1):
    ws = []
    with open(os.path.join(raw, f"wars_s{s}.tsv"), encoding="utf-8") as fh:
        fh.readline()
        for line in fh:
            a = line.rstrip("\n").split("\t")
            if not (1800 <= int(a[0]) <= 1830): continue
            if "スペイン" in (a[2], a[3]): ws.append(f"{a[0]} {a[1]} {a[2]}-{a[3]} {a[4]}月 {float(a[5]):.0f}")
    o.append(f"{s}\t{len(ws)}\t" + "; ".join(ws))
o.append("## 表9 スペインの nation_s（年末の行・1805〜1815）＝atWarMonths・本国州・植民地州・総人口K")
o.append("seed\t" + "\t".join(str(y) for y in range(1805, 1816)))
for s in range(s0, s1 + 1):
    rows = {}
    with open(os.path.join(raw, f"nation_s{s}.tsv"), encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.split("\t", 12)
            if a[3] != "スペイン": continue
            y = int(a[1]); q = int(a[0])
            if 1805 <= y <= 1815 and q % 4 == 3:
                b = line.rstrip("\n").split("\t")
                rows[y] = f"{b[ix['atWarMonths']]}/{b[ix['coreRegions']]}/{b[ix['colonialRegions']]}"
    o.append(f"{s}\t" + "\t".join(rows.get(y, "-") for y in range(1805, 1816)))
o.append("## 表10 スペインの戦争の年（atWarMonths>0 の年末）と前の年の holdBase の差の符号（secession_s の国の列・1783〜1950）")
up = dn = 0; dl = []; lg = []
for s in range(s0, s1 + 1):
    war = set()
    with open(os.path.join(raw, f"nation_s{s}.tsv"), encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.split("\t", 12)
            if a[3] != "スペイン" or int(a[0]) % 4 != 3: continue
            b = line.rstrip("\n").split("\t")
            if float(b[ix["atWarMonths"]]) > 0: war.add(int(b[1]))
    hb = {}; lgm = {}
    with open(os.path.join(raw, f"secession_s{s}.tsv"), encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.split("\t", 10)
            if a[2] != "スペイン" or int(a[0]) % 4 != 3: continue
            y = int(a[1])
            if y not in hb: hb[y] = float(a[ix["holdBaseIndex"]]); lgm[y] = float(a[ix["legitimacyIndex"]])
    for y in sorted(war):
        if y in hb and (y - 1) in hb and (y - 1) not in war:   # 戦争に入った年（前の年は平時）
            d = hb[y] - hb[y - 1]; dl.append(d); lg.append(lgm[y] - lgm[y - 1])
            if d > 0: up += 1
            else: dn += 1
o.append(f"戦争に入った年の件数 {up + dn}・holdBase 上 {up}・下 {dn}・差の中央 {st.median(dl) if dl else float('nan'):.4g}・威信の差の中央 {st.median(lg) if lg else float('nan'):.4g}")
with open(os.path.join(out, f"events_{tag}.txt"), "w", encoding="utf-8", newline="\n") as fo: fo.write("\n".join(o) + "\n")
print("wrote", len(o))
