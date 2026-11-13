# (1115) c107op どの書き手が最低賃金法を上げたか・生まれた国の内戦（読むだけ・腕 0）。
# 最低賃金法の書き手は 2 つ：法の門（Government.Step）と、勢力への譲歩（Politics.ConcedeToFaction→ApplyDemand）。
# 譲歩は計器 reform_s の concedeSpendIndex（ConcedeSpent が足した量）が正の行で見える。wallPassLawCount＝反対を押し切った法の数。
# 内戦は center_s の inCivilWarFlag（Parliament.Step は内戦中は頭で戻る＝席が動かない）。
# 数える行の類：
#  O＝開幕：四半期 0 で選挙政体・MinimumWage≥1（初期値は全国 None）
#  T＝移行：非選挙→選挙の移行の次の四半期（距離 1）で MinimumWage が上がった行
#  B＝誕生：途中で生まれ最初の行が選挙政体の国の 2 行目（距離 1）で上がった行
# あわせて：非選挙政体の四半期 0 で最低賃金の法の門（lawGateOpen:MinimumWage）が開いていた国×種・
#  生まれた選挙政体の国の距離 0〜8 の内戦の割合と sf_s の席が空の割合（選挙政体のままの行だけ・名前がその四半期に 1 国だけの行だけ）。
import sys, os, re, csv, collections
sys.stdout.reconfigure(newline=chr(10))
ELECT = {3, 4}
def seeds(d, pre):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(pre + r"_s(\d+)\.tsv", f)] if m)
def table(path, keys):
    out = {}; dup = set()
    if not os.path.exists(path): return out   # a11 は center_s を書かない
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = [x.strip() for x in next(r)]
        iq, inn = h.index("quarter"), h.index("nation"); ix = [h.index(k) for k in keys]
        for row in r:
            k = (int(row[iq]), row[inn])
            if k in out: dup.add(k)
            out[k] = [float(row[i].strip()) for i in ix]
    for k in dup: out.pop(k, None)
    return out
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    cnt = collections.Counter(); gate_ne = [0, 0]
    bcw = collections.Counter(); btot = collections.Counter(); bemp = collections.Counter(); bseat = collections.Counter()
    for s in seeds(d, "monlaw"):
        rows = collections.defaultdict(list)
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = [x.strip() for x in next(r)]
            iq, iu, inn, ip, im, igate = (h.index(k) for k in ("quarter", "nationUid", "nation", "polityCode", "MinimumWage", "lawGateOpen:MinimumWage"))
            for row in r: rows[row[iu]].append((int(row[iq]), int(row[ip]), float(row[im]), row[inn], float(row[igate])))
        ref = table(os.path.join(d, f"reform_s{s}.tsv"), ["concedeSpendIndex", "wallPassLawCount"])
        cen = table(os.path.join(d, f"center_s{s}.tsv"), ["inCivilWarFlag"])
        sfp = os.path.join(d, f"sf_s{s}.tsv")
        sf = table(sfp, ["seatCons", "seatLib", "seatSoc"]) if os.path.exists(sfp) else {}
        def note(cls, q, nat):
            cnt[(cls, "行")] += 1
            rf = ref.get((q, nat)); cw = cen.get((q, nat))
            if rf is None: cnt[(cls, "reform_s に無い")] += 1
            else:
                if rf[0] > 0: cnt[(cls, "譲歩あり")] += 1
                if rf[1] > 0: cnt[(cls, "押し切り")] += 1
            if cw is not None and cw[0] > 0: cnt[(cls, "内戦")] += 1
        for uid, seq in rows.items():
            seq.sort()
            q0, pol0, mw0, nat0, gate0 = seq[0]
            if q0 == 0 and pol0 not in ELECT:
                gate_ne[1] += 1
                if gate0 > 0: gate_ne[0] += 1
            if q0 == 0 and pol0 in ELECT and mw0 >= 1: note("O 開幕", 0, nat0)
            if q0 > 0 and pol0 in ELECT:
                if len(seq) > 1 and seq[1][2] > seq[0][2]: note("B 誕生", seq[1][0], seq[1][3])
                for k in range(0, min(9, len(seq))):
                    qq, pol, mw, nat, gate_k = seq[k]
                    if pol not in ELECT: break
                    cw = cen.get((qq, nat))
                    if cw is not None:
                        btot[k] += 1
                        if cw[0] > 0: bcw[k] += 1
                    st = sf.get((qq, nat))
                    if st is not None:
                        bseat[k] += 1
                        if sum(st) < 1e-9: bemp[k] += 1
            for k in range(1, len(seq)):
                if seq[k][1] in ELECT and seq[k - 1][1] not in ELECT and k + 1 < len(seq):
                    nx = seq[k + 1]
                    if nx[1] in ELECT and nx[2] > seq[k][2]: note("T 移行", nx[0], nx[3])
    print(f"== {name}")
    for cls in ("O 開幕", "T 移行", "B 誕生"):
        print(f"   {cls}：行 {cnt[(cls, '行')]}・譲歩あり {cnt[(cls, '譲歩あり')]}・押し切り {cnt[(cls, '押し切り')]}・内戦 {cnt[(cls, '内戦')]}・reform_s に無い {cnt[(cls, 'reform_s に無い')]}")
    print(f"   非選挙の四半期 0 で最低賃金の門が開いていた国×種 {gate_ne[0]}/{gate_ne[1]}")
    print("   生まれた選挙政体の国・距離ごとの内戦／席が空：" + "・".join(f"{k}:{bcw[k]}/{btot[k]}・{bemp[k]}/{bseat[k]}" for k in range(9)))
