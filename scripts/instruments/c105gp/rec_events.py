"""(1099) c105gp 記録（r1085 の同名の台本の写し・腕を引数 名前=フォルダ:種の始め-種の終わり で渡す）。元の説明：(1085) 9 国の毎四半期の行で、選挙（議席が動いた四半期）・間隔・移行の後の食い違いの長さを数える（読むだけ）。
使い方: python -X utf8 -I rec_events.py <out> 名前=フォルダ:1-10 ...
選挙の推定：選挙政体の 2 四半期続きの行で議席の 3 値のどれかが 1e-12 を超えて動いた四半期（SnapshotSeats は選挙の時だけ＝Parliament.cs:105）。
"""
import sys, csv, collections, statistics

def _arms(argv):
    r = []
    for spec in argv:
        name, rest = spec.split("=", 1)
        d, rng = rest.rsplit(":", 1)
        lo, hi = map(int, rng.split("-"))
        r.append((name, d, range(lo, hi + 1)))
    return r


ARMS = _arms(sys.argv[2:])
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


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda s: out.write(s + "\n")
    w("arm\telections\tintervals_16\tintervals_lt16\tintervals_gt16\tgov_change_share\ttransitions\ttrans_mm_any\tmm_len_median\tmm_len_max\tmm_end_by_election\tmm_end_by_other\tmm_open_at_end\tnormal_mm_dist_hist")
    for arm, d, seeds in ARMS:
        el_n = 0; iv = collections.Counter(); govch = 0; opening = 0
        trans = 0; trans_mm = 0; lens = []; end_el = 0; end_other = 0; open_end = 0
        dist_hist = collections.Counter()
        for s in seeds:
            pol = {}
            for q, n, pc in read(f"{d}/scalars_s{s}.tsv", ["quarter", "nation", "polityCode"]):
                pol[(int(q), n)] = int(float(pc))
            rows = collections.defaultdict(list)
            for q, n, a, b, c, g in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "seatCons", "seatLib", "seatSoc", "governingParty"]):
                rows[n].append((int(q), pol.get((int(q), n), -1), (float(a), float(b), float(c)), int(float(g))))
            for n, rs in rows.items():
                if len(rs) < 600: continue
                rs.sort()
                last_el_q = None; last_nonel_q = None
                prev = None
                in_mm = None  # (start_q)
                for q, pc, seats, gp in rs:
                    el = pc in ELECTED
                    mm = sum(seats) > 1e-9 and gp != am(seats)
                    if prev is not None:
                        pq, ppc, pseats, pgp = prev
                        if el and ppc in ELECTED and q == pq + 1:
                            if any(abs(seats[i] - pseats[i]) > 1e-12 for i in range(3)) and not (sum(pseats) > 1e-9):
                                opening += 1; last_el_q = None  # 開院からの間隔は数えない（開院は四半期 1・最初の定期は数えが 16 に届く四半期）
                            elif any(abs(seats[i] - pseats[i]) > 1e-12 for i in range(3)):
                                el_n += 1
                                if gp != pgp: govch += 1
                                if last_el_q is not None:
                                    gap = q - last_el_q
                                    iv["16" if gap == 16 else "lt" if gap < 16 else "gt"] += 1
                                last_el_q = q
                                if in_mm is not None:
                                    lens.append(q - in_mm); end_el += 1 if not mm else 0; in_mm = None if not mm else in_mm
                        if el and ppc not in ELECTED:
                            trans += 1; last_el_q = None
                            if mm:
                                trans_mm += 1; in_mm = q
                    if in_mm is not None and not mm:
                        lens.append(q - in_mm); end_other += 1; in_mm = None
                    if in_mm is not None and not el:
                        lens.append(q - in_mm); end_other += 1; in_mm = None
                    if el and mm and in_mm is None:
                        # 移行の窓の外の食い違い：最後の非選挙からの距離
                        dist = q - last_nonel_q if last_nonel_q is not None else -1
                        dist_hist[dist if dist <= 20 else ">20"] += 1
                    if not el: last_nonel_q = q
                    prev = (q, pc, seats, gp)
                if in_mm is not None: open_end += 1
        med = statistics.median(lens) if lens else 0
        mx = max(lens) if lens else 0
        dh = ",".join(f"{k}:{v}" for k, v in sorted(dist_hist.items(), key=lambda x: str(x[0])))
        w(f"{arm}\topening={opening}\t{el_n}\t{iv['16']}\t{iv['lt']}\t{iv['gt']}\t{(govch / el_n if el_n else 0):.3f}\t{trans}\t{trans_mm}\t{med}\t{mx}\t{end_el}\t{end_other}\t{open_end}\t{dh}")
    out.close()


main()
