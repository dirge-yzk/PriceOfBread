"""(1057) 問い1・2：女性の権利の段（3 時点）・上下の出どころ・戦線の符号・姿勢の分布。読むだけ。
使い方: python -X utf8 -I q12.py <keep_1053 の親> > q12_out.txt
"""
import csv, sys, os, collections

ROOT = sys.argv[1]
ARMS = [("base", "base_ins", range(1, 11)), ("n1", "n1", range(1, 11)), ("n2", "n2", range(1, 11)), ("a11", "a11", range(11, 21))]
SIX = ["イギリス", "フランス", "プロイセン", "アメリカ", "日本", "ロシア"]
YEARS = [1870, 1913, 1950]
STANCE = {"0": "Modernize", "1": "Industrialize", "2": "Welfare", "3": "Fiscal", "4": "Militarize", "-1": "none"}
csv.field_size_limit(10**9)


def rows(p):
    with open(p, encoding="utf-8", newline="") as f:
        yield from csv.DictReader(f, delimiter="\t")


def group_of(name):
    for b in SIX:
        if b in name:
            return b
    return None


lvl_tab = collections.Counter()          # (arm, year, nation, level)
trans = collections.Counter()            # (arm, origOrSplit, dir, source)
trans_list = []                          # 例を少し
ever_pos = collections.Counter()         # (arm, nationName) 段>0 の四半期
ever_pos_seeds = collections.defaultdict(set)
front = collections.Counter()            # (arm, code)
front_q = collections.Counter()          # (arm, 'quarters')
stance_six = collections.Counter()       # (arm, stance)
stance_all = collections.Counter()
stance02_nat = collections.defaultdict(set)
lg2 = collections.Counter()

for arm, sub, seeds in ARMS:
    d = os.path.join(ROOT, sub)
    for s in seeds:
        # imp_s：矯正講和の (四半期, 負けた国の名)
        imp = set()
        for x in rows(os.path.join(d, f"imp_s{s}.tsv")):
            imp.add((x["quarter"], x["loser"]))
        prev = {}            # uid -> (level, polity)
        first_uid = {}       # 開幕の uid -> 名
        last_by_year = {}    # (uid, year) -> level
        names = {}
        for x in rows(os.path.join(d, f"monlaw_s{s}.tsv")):
            uid = x["nationUid"]; nm = x["nation"]; q = x["quarter"]; y = int(x["year"])
            lv = int(x["WomenRights"]); pc = x["polityCode"]
            if q == "0" and nm in SIX:
                first_uid[uid] = nm
            names.setdefault(uid, set()).add(nm)
            if lv > 0:
                ever_pos[(arm, nm)] += 1; ever_pos_seeds[(arm, nm)].add(s)
            g = group_of(nm)
            if y in YEARS:
                last_by_year[(uid, y)] = lv
            if g is not None:
                kind = "orig" if uid in first_uid else "split"
                if uid not in prev:
                    if lv > 0:
                        trans[(arm, kind, "up", "a建国")] += 1
                        trans_list.append((arm, s, y, nm, uid, 0, lv, "a建国"))
                else:
                    plv, ppc = prev[uid]
                    if lv != plv:
                        dr = "up" if lv > plv else "down"
                        if (q, nm) in imp:
                            src = "b矯正講和"
                        elif pc != ppc:
                            src = "c政変"
                        else:
                            src = "d法の改正"
                        trans[(arm, kind, dr, src)] += 1
                        trans_list.append((arm, s, y, nm, uid, plv, lv, src))
            prev[uid] = (lv, pc)
        for uid, nm in first_uid.items():
            for y in YEARS:
                lv = last_by_year.get((uid, y))
                lvl_tab[(arm, y, nm, "無い" if lv is None else str(lv))] += 1
        # 戦線の符号（開幕の名の 6 国＝名で引く）
        for x in rows(os.path.join(d, f"nation_s{s}.tsv")):
            if x["nation"] not in SIX:
                continue
            front_q[arm] += 1
            st = x.get("openFrontStates", "")
            code = "戦線なし"
            for it in st.split(";"):
                if it.startswith("L.WomenRights:"):
                    code = it.split(":")[1]
            front[(arm, code)] += 1
        for x in rows(os.path.join(d, f"modsig_s{s}.tsv")):
            stv = STANCE.get(x["stance"], x["stance"])
            stance_all[(arm, stv)] += 1
            if x["nation"] in SIX:
                stance_six[(arm, stv)] += 1
            if x["stance"] in ("0", "2"):
                stance02_nat[(arm, x["nation"])].add(s)
        for x in rows(os.path.join(d, f"lawgate2_s{s}.tsv")):
            if x["axis"] == "WomenRights":
                lg2[(arm, x["nation"] in SIX, x["engineCode"], x["reasonCode"])] += 1

print("== 問い1 段の件数（開幕の uid・各年の最後の四半期・10 種）")
for arm, _, _ in ARMS:
    for y in YEARS:
        line = [f"{arm} {y}"]
        for nm in SIX:
            c = {k[3]: v for k, v in lvl_tab.items() if k[0] == arm and k[1] == y and k[2] == nm}
            line.append(nm + ":" + ",".join(f"{l}={c[l]}" for l in sorted(c)))
        print("  ".join(line))
print("\n== 問い1 段の上下の回（6 国の名を含む全ての uid）")
for k in sorted(trans):
    print(k, trans[k])
print("\n== 上下の例（最初の 40）")
for t in trans_list[:40]:
    print(t)
print("\n== 段>0 になった国（腕・国・四半期の数・種）")
for k in sorted(ever_pos, key=lambda k: (k[0], -ever_pos[k])):
    print(k, ever_pos[k], sorted(ever_pos_seeds[k]))
print("\n== 問い2 L.WomenRights の戦線の符号（6 国×四半期）")
for arm, _, _ in ARMS:
    tot = front_q[arm]
    print(arm, "四半期", tot, {k[1]: v for k, v in sorted(front.items()) if k[0] == arm})
print("\n== 姿勢（modsig_s の stance）6 国")
for arm, _, _ in ARMS:
    print(arm, {k[1]: v for k, v in sorted(stance_six.items()) if k[0] == arm})
print("== 姿勢 全ての国")
for arm, _, _ in ARMS:
    print(arm, {k[1]: v for k, v in sorted(stance_all.items()) if k[0] == arm})
print("== 姿勢 0・2 を 1 度でも選んだ国（腕・国・種）")
for k in sorted(stance02_nat):
    print(k, sorted(stance02_nat[k]))
print("\n== lawgate2 の WomenRights 行（腕・6国か・engine・reason）")
for k in sorted(lg2):
    print(k, lg2[k])
