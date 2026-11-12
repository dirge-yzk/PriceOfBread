"""(1085) 移行の後の食い違いの長さの内訳と内戦の重なり・選挙政体の与党の族の分布（読むだけ）。
長さ＝非選挙→選挙の移行の最初の行から、食い違いが消える行まで（9 国の毎四半期の行）。
内戦＝nation_s の civilWarMonths（その四半期の内戦の月数）が 1 以上の四半期が窓にあるか。
与党の族の分布＝年末の行の選挙政体（全ての国）。
使い方: python -X utf8 -I episodes.py <out>
"""
import sys, csv, collections

ARMS = [
    ("m1069", "D:/PoB_work/keep_1078/base_ins", range(1, 11)),
    ("n1", "D:/PoB_work/keep_1069/n1", range(1, 11)),
    ("n2", "D:/PoB_work/keep_1069/n2", range(1, 11)),
    ("s11", "D:/PoB_work/keep_1069/a11", range(11, 21)),
]
ELECTED = {3, 4}


def read(path, cols):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ix = [h.index(c) for c in cols]
        for row in r:
            yield [row[i] for i in ix]


def am(a):
    b = 0
    if a[1] > a[b]: b = 1
    if a[2] > a[b]: b = 2
    return b


def bucket(n):
    if n <= 3: return "<=3"
    if n == 4: return "=4"
    if n <= 15: return "5-15"
    if n == 16: return "=16"
    if n == 17: return "=17"
    return ">17"


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda s: out.write(s + "\n")
    for arm, d, seeds in ARMS:
        lens = collections.Counter(); cw_long = collections.Counter(); kinds = collections.Counter()
        govdist = collections.Counter(); govdist_seatmax = collections.Counter(); newsfalse = 0; govchanges = 0
        for s in seeds:
            pol = {}
            for q, n, pc in read(f"{d}/scalars_s{s}.tsv", ["quarter", "nation", "polityCode"]):
                pol[(int(q), n)] = int(float(pc))
            cw = {}
            for q, n, m in read(f"{d}/nation_s{s}.tsv", ["quarter", "nation", "civilWarMonths"]):
                cw[(int(q), n)] = int(float(m)) if m not in ("", "nan", "NaN") else 0
            rows = collections.defaultdict(list)
            for q, n, a, b, c, g in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "seatCons", "seatLib", "seatSoc", "governingParty"]):
                q = int(q); pc = pol.get((q, n), -1); seats = (float(a), float(b), float(c)); gp = int(float(g))
                rows[n].append((q, pc, seats, gp))
                if q % 4 == 3 and pc in ELECTED:
                    govdist[gp] += 1
                    if sum(seats) > 1e-9: govdist_seatmax[am(seats)] += 1
            for n, rs in rows.items():
                if len(rs) < 600: continue
                rs.sort()
                start = None; prev = None; had_cw = False
                for q, pc, seats, gp in rs:
                    el = pc in ELECTED
                    mm = sum(seats) > 1e-9 and gp != am(seats)
                    if prev is not None:
                        # 画面の通知 News.cs:233 は与党の族が替わると〈選挙で…〉と出す。議席が動かない族の交代＝選挙でない交代を数える
                        if gp != prev[3]:
                            govchanges += 1
                            if all(abs(seats[i] - prev[2][i]) <= 1e-12 for i in range(3)) or not (el and prev[1] in ELECTED):
                                newsfalse += 1
                    if prev is not None and el and prev[1] not in ELECTED and mm:
                        start = q; had_cw = False
                        kinds[(prev[1], gp, am(seats))] += 1
                    if start is not None:
                        if cw.get((q, n), 0) > 0: had_cw = True
                        if not mm or not el:
                            L = q - start; lens[bucket(L)] += 1
                            if L > 16: cw_long["cw" if had_cw else "nocw"] += 1
                            start = None
                    prev = (q, pc, seats, gp)
        w(f"== {arm}")
        w("移行の後の食い違いの長さ（四半期）: " + " ".join(f"{k}:{lens[k]}" for k in ["<=3", "=4", "5-15", "=16", "=17", ">17"]))
        w(f"17 以上の長さのうち窓に内戦の四半期がある: {cw_long['cw']}・無い: {cw_long['nocw']}")
        w("移行の種類（前の政体・与党の族・議席の第一党）上位: " + " ".join(f"{k}:{v}" for k, v in kinds.most_common(6)))
        tg = sum(govdist.values()); ts = sum(govdist_seatmax.values())
        w("選挙政体の年末の与党の族（0 保守・1 自由・2 社会）: " + " ".join(f"{k}:{govdist[k] / tg:.4f}" for k in range(3)))
        w("同じ行の議席の第一党: " + " ".join(f"{k}:{govdist_seatmax[k] / ts:.4f}" for k in range(3)))
        w(f"9 国の与党の族の交代 {govchanges}・うち議席が動かない交代か政体をまたぐ交代 {newsfalse}（News.cs:233 が〈選挙で〉と出す候補）")
    out.close()


main()
