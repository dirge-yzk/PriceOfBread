#!/usr/bin/env python
"""(415) 事前登録 [5] R1・R2・R4 の読み手：発見の表の各行の最初の発見者（年・国）と、小国が最初になる回数、行ごとの最初の解禁の年の種の中央値。

入力：<計器の出力フォルダ>/disc_s<seed>.tsv（E 行＝解禁した行）と nation_s<seed>.tsv（開幕の人口）。シミュは回さない。
小国＝その種の開幕（quarter 0）の人口（totalPopulationK）が、その種の国々の中央値より下の国。
使い方：PYTHONUTF8=1 python first_discoverer.py <計器の出力フォルダ> [種の数=10]
出力：行ごとの 〈最初の解禁の年の種の中央値・最初の国の分布・小国の回数〉と、全体の小国の回数（非制度・非思想の行のみ）。
"""
import collections, csv, os, statistics, sys

SPREAD = {"特許", "株式会社", "社会主義", "共産主義", "帝国主義", "ファシズム"}
EXCLUDED = {"合成染料"}  # 旗0は開幕で解禁済みで E 行が無く、旗が正では E 行が入る＝分母が揃わないので両側から除く（反証役の点検）


def main():
    root = sys.argv[1]
    nseeds = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    first = collections.defaultdict(list)  # row -> [(year, quarter, nation, seed, small)]
    small_total = 0
    rows_total = 0
    unseeded = 0  # 開幕に居ない国（独立・建国で生まれた国）が最初の発見者の件数＝小国の数えには入れず別に出す
    for s in range(1, nseeds + 1):
        pop0 = {}
        with open(os.path.join(root, f"nation_s{s}.tsv"), encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r["quarter"] == "0":
                    pop0[r["nation"]] = float(r["totalPopulationK"])
        med = statistics.median(pop0.values()) if pop0 else 0.0
        best = {}
        with open(os.path.join(root, f"disc_s{s}.tsv"), encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r["kind"] != "E":
                    continue
                key = r["row"]
                t = (int(r["year"]), int(r["quarter"]))
                if key not in best or t < best[key][0]:
                    best[key] = (t, r["nation"])
        for row, (t, nat) in best.items():
            if row in EXCLUDED:
                continue
            known = nat in pop0
            small = known and pop0[nat] < med
            first[row].append((t[0], t[1], nat, s, small))
            if row not in SPREAD:
                if not known:
                    unseeded += 1
                    continue
                rows_total += 1
                small_total += 1 if small else 0
    print("row\tn\tmedianYear\tsmallFirst\tfirstNations")
    for row, lst in sorted(first.items(), key=lambda kv: statistics.median([x[0] for x in kv[1]])):
        yrs = [x[0] for x in lst]
        nat = collections.Counter(x[2] for x in lst).most_common(4)
        print(f"{row}\t{len(lst)}\t{statistics.median(yrs):.1f}\t{sum(1 for x in lst if x[4])}\t" + "・".join(f"{n}{c}" for n, c in nat))
    print(f"# 非制度・非思想の行で小国が最初の発見者になった件数（種×行・開幕に居た国だけ・合成染料を除く）：{small_total}／{rows_total}＝比 {small_total / rows_total:.3f}（開幕に居ない国が最初の発見者：{unseeded} 件は別）" if rows_total else "# 該当なし")


if __name__ == "__main__":
    main()
