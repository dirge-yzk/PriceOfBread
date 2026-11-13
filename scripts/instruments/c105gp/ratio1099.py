"""(1099) c105gp 同じ盤面の新÷旧（記録・読むだけ）。nation_s の 9 国の populationK・realGdpPerYear・priceLevel を 1850・1913・1950 の年末（各年の最後の四半期の行）で、種の中央値を取り、腕÷基線とヌル腕÷基線を並べる。
使い方: python -X utf8 -I ratio1099.py <出力> 腕=フォルダ 基線=フォルダ n1=フォルダ n2=フォルダ（種 1〜10）
"""
import sys, csv, statistics, collections

NATIONS = ["清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本", "アメリカ"]
YEARS = [1850, 1913, 1950]
COLS = ["populationK", "realGdpPerYear", "priceLevel"]


def load(d):
    v = collections.defaultdict(list)
    for s in range(1, 11):
        last = {}
        with open(f"{d}/nation_s{s}.tsv", encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r)
            iq, iy, n = h.index("quarter"), h.index("year"), h.index("nation")
            ic = [h.index(c) for c in COLS]
            for row in r:
                y = int(row[iy])
                if y in YEARS and row[n] in NATIONS:
                    last[(row[n], y)] = row
        for (nn, y), row in last.items():
            for c, i in zip(COLS, ic):
                try: v[(nn, y, c)].append(float(row[i]))
                except ValueError: pass
    return {k: statistics.median(x) for k, x in v.items() if x}


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    arms = [a.split("=", 1) for a in sys.argv[2:]]
    data = {name: load(d) for name, d in arms}
    base = arms[1][0]
    others = [a[0] for a in arms if a[0] != base]
    out.write("nation\tyear\tcol\t" + "\t".join(f"{o}/{base}" for o in others) + "\tarm_outside_null\n")
    outside = 0; total = 0
    for nn in NATIONS:
        for y in YEARS:
            for c in COLS:
                b = data[base].get((nn, y, c))
                if not b: continue
                rs = []
                for o in others:
                    x = data[o].get((nn, y, c))
                    rs.append(x / b if x is not None else float("nan"))
                nulls = rs[1:]
                ok = [x for x in nulls if x == x]
                flag = ""
                if ok and rs[0] == rs[0]:
                    total += 1
                    lo, hi = min(ok + [1.0]), max(ok + [1.0])
                    if rs[0] < lo or rs[0] > hi: flag = "外"; outside += 1
                out.write(f"{nn}\t{y}\t{c}\t" + "\t".join(f"{x:.3f}" for x in rs) + f"\t{flag}\n")
    out.write(f"# 腕÷基線がヌル腕÷基線の幅（1 を含む）の外: {outside}/{total}\n")
    out.close()


main()
