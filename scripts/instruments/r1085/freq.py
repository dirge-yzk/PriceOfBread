"""(1085) S18 の行の頻度（読むだけ）。年末の行（quarter%4==3＝全ての国が出る行）だけで数える。
使い方: python -X utf8 -I freq.py <out>
"""
import sys, csv, collections, statistics

ARMS = [
    ("m1069", "D:/PoB_work/keep_1078/base_ins", range(1, 11)),
    ("n1", "D:/PoB_work/keep_1069/n1", range(1, 11)),
    ("n2", "D:/PoB_work/keep_1069/n2", range(1, 11)),
    ("s11", "D:/PoB_work/keep_1069/a11", range(11, 21)),
]
ELECTED = {3, 4}
RIOT = 0.5  # Coefficients.cs:410 RiotMilitancyThreshold


def read(path, cols):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        miss = [c for c in cols if c not in h]
        if miss: raise SystemExit(f"{path}: no column {miss}")
        ix = [h.index(c) for c in cols]
        for row in r:
            yield [row[i] for i in ix]


def q(xs, p):
    if not xs: return float("nan")
    xs = sorted(xs); return xs[min(len(xs) - 1, int(p * len(xs)))]


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda s: out.write(s + "\n")
    for arm, d, seeds in ARMS:
        c = collections.Counter(); mil = []; parlShare = []; pcNon = []; suff = collections.Counter(); armyShare = []
        vwRatio = []
        for s in seeds:
            pol = {}
            for qq, n, pc, sf in read(f"{d}/scalars_s{s}.tsv", ["quarter", "nation", "polityCode", "suffrage"]):
                if int(qq) % 4 != 3: continue
                pc = int(float(pc)); pol[(int(qq), n)] = pc
                c["rows"] += 1; c["elected"] += pc in ELECTED
                suff[int(float(sf))] += 1
            for qq, n, ns, nm, nmass, npar, narmy, pcs in read(f"{d}/fm_s{s}.tsv", ["quarter", "nation", "netStreetTradIndex", "netStreetModernIndex", "netStreetMassIndex", "netParliamentIndex", "netArmyIndex", "parliamentControlShare"]):
                if int(qq) % 4 != 3: continue
                pc = pol.get((int(qq), n))
                if pc is None: continue
                fv = [float(x) for x in (ns, nm, nmass, npar, narmy)]
                if any(v != v for v in fv):
                    c["fm_nan"] += 1; continue
                vals = [abs(v) for v in fv]
                tot = sum(vals)
                el = pc in ELECTED
                c["fm_rows_el" if el else "fm_rows_non"] += 1
                if vals[3] > 1e-12: c["npar_nz_el" if el else "npar_nz_non"] += 1
                if el and tot > 1e-12: parlShare.append(vals[3] / tot)
                if tot > 1e-12: armyShare.append(vals[4] / tot)
                p = float(pcs)
                if p != p: p = 0.0
                if not el:
                    if p > 1e-9: c["pc_pos_non"] += 1
                    pcNon.append(p)
            for qq, n, m in read(f"{d}/dcont_s{s}.tsv", ["quarter", "nation", "militancyMeanShare"]):
                if int(qq) % 4 != 3: continue
                try: m = float(m)
                except ValueError: continue
                if m != m: continue
                mil.append(m); c["dc_rows"] += 1
                if m > RIOT: c["mil_gt_riot"] += 1
            for qq, n, va, vs, vu in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "voteWeightAll", "voteWeightScoped", "voteWeightUsed"]):
                if int(qq) % 4 != 3: continue
                va = float(va); vu = float(vu)
                if va > 1e-9: vwRatio.append(vu / va)
        R = c["rows"]
        w(f"== {arm}  年末の行 {R}")
        w(f"選挙政体の割合 {c['elected'] / R:.4f}")
        w("参政権の段の割合 " + " ".join(f"{k}:{suff[k] / R:.4f}" for k in sorted(suff)))
        fe, fn = c["fm_rows_el"], c["fm_rows_non"]
        w(f"fm 軍事費の合計の施行の一歩が無い行（NaN）{c['fm_nan']}・有る行 {fe + fn}")
        w(f"fm 議会の項が 0 でない：選挙政体 {c['npar_nz_el']}/{fe}={c['npar_nz_el'] / max(1, fe):.4f}・非選挙 {c['npar_nz_non']}/{fn}={c['npar_nz_non'] / max(1, fn):.4f}")
        w(f"fm 選挙政体の |議会の項|÷Σ|項| 中央 {q(parlShare, .5):.3f} p10 {q(parlShare, .1):.3f} p90 {q(parlShare, .9):.3f}")
        w(f"fm |軍の項|÷Σ|項|（全政体）中央 {q(armyShare, .5):.3f} p90 {q(armyShare, .9):.3f}")
        w(f"fm 非選挙政体で議会の統制力>0 {c['pc_pos_non']}/{fn}={c['pc_pos_non'] / max(1, fn):.4f}・中央 {q(pcNon, .5):.3f} p90 {q(pcNon, .9):.3f}")
        w(f"dcont 大衆の闘争度 中央 {q(mil, .5):.3f} p90 {q(mil, .9):.3f}・RiotMilitancyThreshold 0.5 超 {c['mil_gt_riot']}/{c['dc_rows']}={c['mil_gt_riot'] / max(1, c['dc_rows']):.4f}")
        w(f"sf 票の重み 使った÷全体 中央 {q(vwRatio, .5):.4f} p10 {q(vwRatio, .1):.4f}")
    out.close()


main()
