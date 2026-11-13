# (1108) 最低賃金法の施行の行（monlaw_s の MinimumWage>0）を政体・段・移行からの距離・最初の年で分ける（読むだけ）。
# 使い方: python -X utf8 -I mw_split.py <名前>=<生 ins> [...]  種は monlaw_s*.tsv を全て読む。
import sys, csv, os, re, collections, statistics
ELECT = {3, 4}  # 立憲君主制・共和制（Model.cs の Polity の序数）
PNAME = {0: "絶対", 1: "専制", 2: "幕府", 3: "立憲", 4: "共和", 5: "ブル独", 6: "プロ独", 7: "軍独", 8: "無政府"}

def seeds(d):
    out = []
    for f in os.listdir(d):
        m = re.fullmatch(r"monlaw_s(\d+)\.tsv", f)
        if m: out.append(int(m.group(1)))
    return sorted(out)

def read(d, s):
    rows = collections.defaultdict(list)  # 国 -> [(四半期, 年, 政体, 段, 門)]
    with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r)
        iq, iy, inn, ip, im, igt = (h.index(k) for k in ("quarter", "year", "nation", "polityCode", "MinimumWage", "lawGateOpen:MinimumWage"))
        for row in r:
            rows[row[inn]].append((int(row[iq]), int(row[iy]), int(row[ip]), int(float(row[im])), row[igt]))
    for k in rows: rows[k].sort()
    return rows

for spec in sys.argv[1:]:
    name, d = spec.split("=", 1)
    tot = 0; bypol = collections.Counter(); bystage = collections.Counter(); since = collections.Counter()
    firsts = []; firstpol = collections.Counter(); bynat = collections.Counter(); natseeds = collections.Counter()
    pre1894 = 0; enactEvents = 0; enactAfterTrans = collections.Counter(); repeal = 0
    ss = seeds(d)
    for s in ss:
        rows = read(d, s)
        for nat, seq in rows.items():
            lastTrans = None; prevPol = None; prevStage = 0; first = None
            for (q, y, pol, st, gt) in seq:
                if prevPol is not None and pol != prevPol and (prevPol not in ELECT) and pol in ELECT: lastTrans = q
                if prevPol is not None and pol not in ELECT: lastTrans = None
                if st > 0:
                    tot += 1; bypol[PNAME.get(pol, str(pol))] += 1; bystage[st] += 1; bynat[nat] += 1
                    if y < 1894: pre1894 += 1
                    if pol in ELECT:
                        if lastTrans is None: since["選挙政体・移行の記録なし（開幕から選挙政体か、表の最初より前）"] += 1
                        else:
                            dq = q - lastTrans
                            since["選挙政体・移行から 0〜16 四半期" if dq <= 16 else "選挙政体・移行から 17 四半期以上"] += 1
                    else: since["非選挙政体"] += 1
                    if first is None: first = (y, pol)
                if st > prevStage:
                    enactEvents += 1
                    if pol in ELECT and lastTrans is not None and q - lastTrans <= 16: enactAfterTrans["移行から 16 四半期以内"] += 1
                    elif pol in ELECT: enactAfterTrans["選挙政体・それ以外"] += 1
                    else: enactAfterTrans["非選挙政体"] += 1
                if st < prevStage: repeal += 1
                prevPol = pol; prevStage = st
            if first is not None:
                firsts.append(first[0]); firstpol[PNAME.get(first[1], str(first[1]))] += 1; natseeds[nat] += 1
    print(f"== {name} {d} 種 {ss[0]}..{ss[-1]}（{len(ss)} 種）")
    print(f"  施行の行（国×四半期）{tot}・1894 年より前 {pre1894}（{pre1894/max(1,tot):.3f}）")
    print("  政体ごと", dict(bypol.most_common()))
    print("  段ごと", dict(sorted(bystage.items())))
    print("  移行との距離", dict(since.most_common()))
    print(f"  段を上げた回 {enactEvents}・下げた回 {repeal}・上げた回の政体", dict(enactAfterTrans.most_common()))
    if firsts:
        fs = sorted(firsts)
        print(f"  最初の施行（国×種）{len(fs)}・年の中央 {statistics.median(fs)}・最小 {fs[0]}・1894 年より前 {sum(1 for x in fs if x < 1894)}・1909 年より前 {sum(1 for x in fs if x < 1909)}")
        print("  最初の施行の時の政体", dict(firstpol.most_common()))
    print("  国の上位（行）", bynat.most_common(10))
    print("  施行した種の数の上位", natseeds.most_common(10))
