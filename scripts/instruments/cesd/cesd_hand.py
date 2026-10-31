# cesd (709)：割譲の候補と費用の設計書の手計算（読むだけ・シミュは回さない・C# の式を写した近似＝本体ではない）
# 使い方：python -I cesd_hand.py <SimCore のフォルダ> <出力 tsv> <生のフォルダ>...（各フォルダは region_s{n}.tsv・wars_s{n}.tsv・stock_s{n}.tsv）
# 何を数えるか：講和で持ち主が替わった全ての州（本国格 Core・Integrated と植民地格 Colonial）を、案ごとの候補の規則に当てて、残るか落ちるかを静的に数える
#   （落ちた州の代わりに別の州が選ばれる動的な効きは数えない＝落ちる件数の上限）。
# 近似（限界の申告）：
#  - 距離は大圏（海路の下限）。勝者の母点は州の表のその国の最初の行（首都＝Regions[0]）。
#  - 艦は stock_s の navalPowerIndex（その年の最後の行）＝講和の月の値ではない。相対＝勝者÷(勝者＋敗者)（SeaControl と同じ形・双方 0 は 0.5）。世界の比＝勝者÷その年の世界最大。
#  - 陸の隣接は landmass.json の州・植民地の点の〈同じ陸塊の 6 近傍〉の和（PathGraph の k=6 を写す・壁・峠・水の判定は省いた）。
#  - 資源の数は表に無い＝0（費用の下限）。戦勝点は表に無い＝CedeTerritory は |戦勝点|≧1.2＝予算 100（sa は 1 に切られる）と置く。
#  - 経路の見分け：同じ講和（四半期・旧・新）で移った州のうち、本国格は CedeTerritory（cede_home）。植民地格は、勝者が海を渡れない（相対<0.65 か艦 0）なら CedeTerritory（cede_col）、
#    渡れて同じ講和で植民地格が 2 件以上なら複数割譲（multi_col）、1 件なら複数割譲の 1 件か CedeTerritory のどちらか（amb_col）。
#  - 講和＝wars_s の内戦でない戦争の終わり（開戦年＋月数/12）が持ち主の替わった四半期の ±1 年（hcd/colxfer.py と同じ）。受け手が前の四半期に居ない（新しい国）は数えない。
import sys, json, math, os, csv, collections, glob, re
SIM = sys.argv[1]; OUT = sys.argv[2]; DIRS = sys.argv[3:]
lm = {p["name"]: p for p in json.load(open(os.path.join(SIM, "landmass.json"), encoding="utf-8"))}
places = {p["name"]: (p["lat"], p["lon"]) for p in json.load(open(os.path.join(SIM, "places.json"), encoding="utf-8"))}
wd = json.load(open(os.path.join(SIM, "world.json"), encoding="utf-8"))
pops = [c["popK"] for n in wd["nations"] for c in (n.get("colonies") or [])] + [c["popK"] for a in wd["abstractTargets"] for c in (a.get("colonies") or [])]
UNIT = sum(pops) / len(pops)
NORM = 10000.0; W = 1.0; SEAMIN = 0.65; RANGE = 4000.0; PROJMIN = 0.2; BUDGET = 100.0


def coord(n):
    if n in places: return places[n]
    p = lm.get(n)
    return (p["lat"], p["lon"]) if p else None


def gc(a, b):
    if a is None or b is None: return float("nan")
    (la1, lo1), (la2, lo2) = a, b
    p1, p2 = math.radians(la1), math.radians(la2); dl = math.radians(lo2 - lo1)
    h = math.sin((p2 - p1) / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * 6371 * math.asin(math.sqrt(min(1, h)))


def land(n):
    p = lm.get(n)
    return p["landmass"] if p else ""


pts = [p for p in lm.values() if p["kind"] in ("state", "colony")]
adj = collections.defaultdict(set)
for a in pts:
    c = sorted((gc((a["lat"], a["lon"]), (b["lat"], b["lon"])), b["name"]) for b in pts if b is not a and b["landmass"] == a["landmass"])
    for _, bn in c[:6]:
        adj[a["name"]].add(bn); adj[bn].add(a["name"])


def baseCost(pop): return 30 + 2 * (max(0, pop) / UNIT)
def mHcd(km, sw): return 1 + W * (km / NORM) * (1 - sw)              # hcd・ColonyList 経路の式（世界の比）
def mRho(km, sw, sea):                                                # 1/ρ＝Projection の州版を世界の比で（海越え (1+km/N)/s・陸 1+km/N）
    if not sea: return 1 + km / NORM
    return (1 + km / NORM) / sw if sw > 1e-9 else float("inf")


out = open(OUT, "w", encoding="utf-8")
print("dir\tseed\tkey\tvalue", file=out)
AGG = collections.defaultdict(collections.Counter)
EX = collections.defaultdict(list)
for d in DIRS:
    key = os.path.basename(d.rstrip("/\\"))
    seeds = sorted(int(re.search(r"region_s(\d+)\.tsv", f).group(1)) for f in glob.glob(os.path.join(d, "region_s*.tsv")))
    for s in seeds:
        byq = collections.defaultdict(dict); order = collections.defaultdict(lambda: collections.defaultdict(list)); yr = {}
        with open(os.path.join(d, f"region_s{s}.tsv"), encoding="utf-8") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for row in r:
                q = int(row[0]); nat = row[ix["nation"]]; reg = row[ix["region"]]
                byq[q][reg] = (nat, row[ix["tier"]], float(row[ix["populationK"]]))
                order[q][nat].append(reg); yr[q] = int(row[1])
        wars = []
        with open(os.path.join(d, f"wars_s{s}.tsv"), encoding="utf-8") as f:
            r = csv.reader(f, delimiter="\t"); next(r)
            for row in r: wars.append((int(row[0]), row[1] == "1", row[2], row[3], int(row[4])))
        nav = collections.defaultdict(dict)
        with open(os.path.join(d, f"stock_s{s}.tsv"), encoding="utf-8") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r); i2 = {k: i for i, k in enumerate(h)}
            for row in r:
                try:
                    v = float(row[i2["navalPowerIndex"]])
                    if v == v: nav[int(row[i2["year"]])][row[i2["nation"]]] = v
                except Exception:
                    pass
        qs = sorted(byq); C = collections.Counter()
        for a, b in zip(qs, qs[1:]):
            ev = collections.defaultdict(list)
            for reg, (n0, t0, p0) in byq[a].items():
                if reg not in byq[b]: continue
                n1 = byq[b][reg][0]
                if n1 == n0 or n1 not in order[a]: continue
                y1 = yr[b]
                if not any({w[2], w[3]} == {n0, n1} and not w[1] and abs(w[0] + w[4] / 12.0 - y1) <= 1.0 for w in wars): continue
                ev[(n0, n1)].append(reg)
            for (n0, n1), regs in ev.items():
                y0 = yr[a]; ny = nav.get(y0, {}); mx = max(ny.values() or [0])
                nw = ny.get(n1, 0.0); nl = ny.get(n0, 0.0)
                sw = nw / mx if mx > 0 else 0.0
                srel = nw / (nw + nl) if nw + nl > 1e-9 else 0.5
                cross = nw > 1e-9 and srel >= SEAMIN
                capW = order[a][n1][0]; cW = coord(capW); lW = land(capW)
                winRegs = set(order[a][n1])
                ncol = sum(1 for x in regs if byq[a][x][1] == "Colonial")
                C["events"] += 1

                def eligibleE(x, hcd=False):   # 案 E の候補の規則（下の okE と同じ）・hcd=True は案 E2（費用を hcd の式に替える）
                    kmx = gc(cW, coord(x)); adjx = any(nb in winRegs for nb in adj.get(x, ()))
                    cx = bool(lm.get(x, {}).get("coastal"))
                    m = mHcd(kmx, sw) if hcd else mRho(kmx, sw, True)
                    return adjx or (cx and nw > 1e-9 and srel >= SEAMIN and baseCost(byq[a][x][2]) * m <= BUDGET)
                # 選び直し（本国格の割譲＝CedeTerritory の 1 件）：実際に移った州が案 E で落ちるとき、敗者に首都以外で案 E の候補が残るか（残れば別の州へ移る＝講和の割譲の件数は減らない）
                for x in regs:
                    if byq[a][x][1] == "Colonial" or eligibleE(x): continue
                    alt = [y for y in order[a][n0][1:] if y not in regs and eligibleE(y)]
                    C["E_reselect_alt|cede_home" if alt else "E_reselect_none|cede_home"] += 1
                for x in regs:
                    if byq[a][x][1] == "Colonial" or eligibleE(x, True): continue
                    alt = [y for y in order[a][n0][1:] if y not in regs and eligibleE(y, True)]
                    C["E2_reselect_alt|cede_home" if alt else "E2_reselect_none|cede_home"] += 1
                for x in regs:
                    t = byq[a][x][1]; p = byq[a][x][2]; km = gc(cW, coord(x)); sea = land(x) != lW
                    home = t != "Colonial"
                    if home: path = "cede_home"
                    elif not cross: path = "cede_col"
                    elif ncol >= 2: path = "multi_col"
                    else: path = "amb_col"
                    fw = km >= 3000 and sw < 0.5
                    def inc(k):
                        C[k + "|" + path] += 1
                        if fw: C[k + "_farweak|" + path] += 1
                    inc("n")
                    adjW = any(nb in winRegs for nb in adj.get(x, ()))
                    d0 = km * ((1 + (1 - srel)) if sea else 1)
                    landingFail = sea and srel < SEAMIN
                    colReach = (t == "Colonial") and nw > 1e-9 and srel >= SEAMIN
                    if d0 > RANGE and colReach: inc("viaColonialReach")
                    if landingFail: inc("landingGateFail")   # 今の CedeTerritory なら候補から落ちるはず（艦の値の時点ずれ・経路の見分けの誤りの印）
                    if sw <= 1e-9 and sea: inc("seaNoNavyWorld0")
                    if sea and km >= 3000:   # 海越えの遠い移転を勝者の艦の世界の比で刻む（0.1 未満＝ごく弱い艦・0.1〜0.3・0.3〜0.5）と、敗者の艦が 0 か
                        C[("seaFar_sw<0.1" if sw < 0.1 else "seaFar_sw0.1-0.3" if sw < 0.3 else "seaFar_sw0.3-0.5" if sw < 0.5 else "seaFar_sw>=0.5") + "|" + path] += 1
                        if nl <= 1e-9: C["seaFar_loserNoNavy|" + path] += 1
                        if sw < 0.1 and nl <= 1e-9: C["seaFar_sw<0.1_loserNoNavy|" + path] += 1
                    if not sea:
                        inc("land")
                        if not adjW:
                            inc("land_notAdj")
                            if home: EX[key].append((s, yr[b], n0, n1, x, round(km)))
                    else:
                        inc("sea")
                    c0 = baseCost(p)
                    if c0 > BUDGET: inc("baseCostOver100")
                    if c0 * mHcd(km, sw) > BUDGET: inc("hcdCostOver100")
                    if c0 * mRho(km, sw, sea) > BUDGET: inc("rhoCostOver100")
                    rho = (1 / (1 + km / NORM)) * (sw if sea else 1.0)
                    if rho < PROJMIN: inc("projWorldBelow0.2")
                    # 案 C（推奨）：陸＝勝者の州と隣り合う・海＝上陸の門（相対・既存）＋費用は 1/ρ（世界の比）で予算 100 の内
                    okC = adjW if not sea else (not landingFail and c0 * mRho(km, sw, sea) <= BUDGET)
                    if not okC: inc("C_drop")
                    # 案 D：陸＝隣接・海＝上陸の門＋費用は hcd の式（世界の比）で予算 100 の内
                    okD = adjW if not sea else (not landingFail and c0 * mHcd(km, sw) <= BUDGET)
                    if not okD: inc("D_drop")
                    # 案 E（推奨の形）：陸で隣り合う州か、港の州（landmass.json の coastal）で海から届く州（上陸の門＝相対・既存＋費用 1/ρ〔世界の比〕が予算 100 の内）。
                    #   同じ陸塊でも隣り合わない州は、港の州なら海の経路で数える（海の距離は大圏・陸塊が同じでも海越えと同じ式）。
                    coastal = bool(lm.get(x, {}).get("coastal"))
                    seaOk = coastal and nw > 1e-9 and srel >= SEAMIN and c0 * mRho(km, sw, True) <= BUDGET
                    okE = adjW or seaOk
                    if not okE: inc("E_drop")
                    if not (adjW or (coastal and nw > 1e-9 and srel >= SEAMIN and c0 * mHcd(km, sw) <= BUDGET)): inc("E2_drop")
                    if (not sea) and (not adjW) and coastal: inc("land_notAdj_coastal")
                    # 案 R5 だけ：陸＝隣接・海は今のまま
                    if (not sea) and not adjW: inc("R5_drop")
        for k, v in C.items():
            print(key, s, k, v, sep="\t", file=out); AGG[key][k] += v
print("# 合算（種の和）", file=out)
for key in AGG:
    for k, v in sorted(AGG[key].items()): print(key, "ALL", k, v, sep="\t", file=out)
print("# 例：陸で隣り合わない本国格の州の講和移転（種・年・旧・新・州・km）各フォルダ先頭 30", file=out)
for k, v in EX.items():
    for e in v[:30]: print(k, "EX", *e, sep="\t", file=out)
