"""(1099) c105gp 記録：開院から最初の選挙までの四半期の分布（読むだけ・門ではない・腕 B の後に足した読み手）。
9 国の毎四半期の行。開院＝続いた 2 行が共に選挙の政体で、前の行の議席が空・今の行の議席が空でない四半期。
最初の選挙＝開院の後、選挙の政体のまま議席の 3 値が動いた最初の四半期。間隔＝その四半期−開院の四半期。
  14＝定期（開院の四半期に QuartersSinceElection が 1→2・16 に届くのは 14 四半期後）／14 未満＝繰り上げ（不信任）／14 超＝内戦で数えが止まった型／
  終わり＝最初の選挙の前に非選挙へ戻ったか系列が尽きた。
使い方: python -X utf8 -I rec_open.py <出力> 名前=フォルダ:種の始め-種の終わり ...
"""
import sys, csv, collections

ELECTED = {3, 4}


def read(path, cols):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ix = [h.index(c) for c in cols]
        for row in r:
            yield [row[i] for i in ix]


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda x: out.write(x + "\n")
    w("arm\topenings\tregular14\tearly_lt14\tlate_gt14\tended_before\tearly_gp_changed\tgap_hist")
    for spec in sys.argv[2:]:
        name, rest = spec.split("=", 1)
        d, rng = rest.rsplit(":", 1)
        lo, hi = map(int, rng.split("-"))
        c = collections.Counter(); hist = collections.Counter()
        for s in range(lo, hi + 1):
            pol = {}
            for q, n, pc in read(f"{d}/scalars_s{s}.tsv", ["quarter", "nation", "polityCode"]):
                pol[(int(q), n)] = int(float(pc))
            rows = collections.defaultdict(list)
            for q, n, a, b, cc, g in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "seatCons", "seatLib", "seatSoc", "governingParty"]):
                rows[n].append((int(q), (a, b, cc), float(a) + float(b) + float(cc), int(float(g))))
            for n, rs in rows.items():
                if len(rs) <= 600: continue
                rs.sort()
                for i in range(1, len(rs)):
                    q, sstr, ssum, gp = rs[i]
                    pq, _, psum, _ = rs[i - 1]
                    if pq != q - 1 or pol.get((q, n)) not in ELECTED or pol.get((pq, n)) not in ELECTED: continue
                    if not (psum <= 1e-9 and ssum > 1e-9): continue
                    c["open"] += 1
                    gap = None
                    for j in range(i + 1, len(rs)):
                        qj, sj, _, gj = rs[j]
                        if rs[j - 1][0] != qj - 1 or pol.get((qj, n)) not in ELECTED: break
                        if sj != rs[j - 1][1]:
                            gap = qj - q
                            if gap < 14 and gj != rs[j - 1][3]: c["early_gp"] += 1
                            break
                    if gap is None: c["ended"] += 1; continue
                    hist[gap] += 1
                    if gap == 14: c["reg"] += 1
                    elif gap < 14: c["early"] += 1
                    else: c["late"] += 1
        hs = ",".join(f"{k}:{v}" for k, v in sorted(hist.items()))
        w(f"{name}\t{c['open']}\t{c['reg']}\t{c['early']}\t{c['late']}\t{c['ended']}\t{c['early_gp']}\t{hs}")
    out.close()


main()
