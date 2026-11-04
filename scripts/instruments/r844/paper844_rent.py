# (844) c68ds 読むだけの紙の台本 1/3：開幕の実効の地代率の表（world.json と式から・シミュを回さない）
# 式（eccd9155 の木）：
#   基の取り分 base = max(0.3, min(farmLordShare, 1 - SerfCashShareFloor))   Simulation.cs:808（CatalogEconomy に渡す値）・:1013（RentShare = max(0.3, farmLordShare)）
#   賦存の RGO（州の resources にその財）＝ max(base, PlantationRentShare 0.6)・持ち主は貴族   Simulation.cs:843〜846
#   州の上限 FarmRentCap = min(RentCap(LandReform, 0.3), RentCap(SerfEmancipation, 0.5))       Simulation.cs:1678・RentCap は :2831（Full→full／Limited→full+(1-full)*0.5／None→1.0）
#   実効 = min(慣習, FarmRentCap)                                                              EconomyStep.cs:625 RentShareOf
#   開幕の法：initialSerfEmancipation の欄が無い国は NationConfig.cs:81 の既定 Full・LandReform は Policy.cs:182 の既定 None
# 使い方：python paper844_rent.py <world.json> <出力.txt>
import json, sys, collections

SERF_FLOOR = 0.15      # Coefficients.cs:2475 SerfCashShareFloor
PLANT = 0.6            # Coefficients.cs:2191 PlantationRentShare
SE_CAP, LR_CAP = 0.5, 0.3   # Coefficients.cs:249 / :248
STAPLE_RGO = {15: "麦", 93: "米", 98: "野菜", 105: "ジャガイモ", 106: "トウモロコシ"}   # Catalog.cs:412〜423（パンは工場の財）

def rentcap(level, full):
    if level == "Full": return full
    if level == "Limited": return full + (1.0 - full) * 0.5
    return 1.0

def main(wpath, opath):
    w = json.load(open(wpath, encoding="utf-8"))
    out = []
    P = out.append
    P("(844) 開幕の実効の地代率（world.json と式・eccd9155）")
    P("列：国・farmLordShare・農奴解放の開幕の段（欄なし＝既定 Full）・FarmRentCap・非賦存の主食 RGO の実効・賦存の主食 RGO の実効・主食が賦存の州の数／州の数")
    rows = []
    groups = collections.defaultdict(list)
    endowed_prov = 0; prov_all = 0
    for n in w["nations"]:
        fls = n.get("farmLordShare", 0.6)          # NationConfig.cs:102 既定 0.6
        se = n.get("initialSerfEmancipation") or "Full"
        cap = min(rentcap("None", LR_CAP), rentcap(se, SE_CAP))
        base = max(0.3, min(fls, 1.0 - SERF_FLOOR))
        eff_plain = min(base, cap)
        eff_end = min(max(base, PLANT), cap)
        ne = 0
        for p in n.get("provinces", []):
            prov_all += 1
            if any(g in STAPLE_RGO for g in p.get("resources", [])): ne += 1; endowed_prov += 1
        rows.append((n["name"], fls, se, cap, eff_plain, eff_end, ne, len(n.get("provinces", []))))
        groups[(round(eff_plain, 4), round(eff_end, 4))].append(n["name"])
    for r in rows:
        P("%s\t%.2f\t%s\t%.2f\t%.3f\t%.3f\t%d/%d" % r)
    P("")
    P("実効の率ごとの国の数（非賦存・賦存）：")
    for k in sorted(groups):
        P("  非賦存 %.3f・賦存 %.3f：%d 国（%s）" % (k[0], k[1], len(groups[k]), "・".join(groups[k])))
    P("主食が賦存の州：%d／%d 州" % (endowed_prov, prov_all))
    P("")
    P("(831)〜(838) の表との違い（読み）：旧の表は max(0.3, fls) だけで、SerfCashShareFloor の床（露・ポ 1.0→0.85）と FarmRentCap（既定 Full の国は 0.5・Limited は 0.75）と賦存の 0.6 を落としていた。")
    P("")
    P("自給の収穫に実効の率で地代を立てた場合（材料 A-3 の表の取り直し）：s＝実効の率＋税の分 t（t は subst 台本の国ごとの実測で置き換える＝下の paper844_subst.py の出力 [B] を見る）。")
    P("  作った量÷必要量を、食べる割合 0.7 に保つための値＝0.7÷(1−s)。下は t＝0.05（材料 A-3 の仏の概算）で並べた目安。")
    for k in sorted(set(round(r[4], 4) for r in rows)):
        s = k + 0.05
        P("  非賦存の実効 %.3f → s≈%.3f → 0.7÷(1−s)≈%s" % (k, s, ("%.2f" % (0.7 / (1 - s))) if s < 1 else "定義できない"))
    open(opath, "wb").write(("\n".join(out) + "\n").encode("utf-8"))

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
