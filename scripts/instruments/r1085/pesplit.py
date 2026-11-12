"""(1085) 非選挙政体で、議席の写し（Parliament.cs:120）を読む PoliticalCapitalRate の分裂の項 pe×split が 0 でない割合（読むだけ）。
pe の近似＝fm の parliamentControlShare × nation_s の parliamentGripIndex（LockedSpending を掛けない＝上に偏る近似）。年末の行。
使い方: python -X utf8 -I pesplit.py <out>
"""
import sys, csv, collections, math

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


def fl(x):
    try:
        v = float(x)
    except ValueError:
        return None
    return None if math.isnan(v) else v


def qt(xs, p):
    if not xs: return float("nan")
    xs = sorted(xs); return xs[min(len(xs) - 1, int(p * len(xs)))]


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda s: out.write(s + "\n")
    w("arm\tnon_rows\tpc_pos\tpe_gt_0.01\tsplit_med\tpeSplit_med_when_pe\tpeSplit_p90_when_pe\tsplit_if_gov_only(0)")
    for arm, d, seeds in ARMS:
        n_rows = 0; pcpos = 0; pepos = 0; splits = []; pes = []
        for s in seeds:
            grip = {}
            for qq, n, g in read(f"{d}/nation_s{s}.tsv", ["quarter", "nation", "parliamentGripIndex"]):
                if int(qq) % 4 != 3: continue
                grip[(int(qq), n)] = fl(g)
            seat = {}
            for qq, n, a, b, c in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "seatCons", "seatLib", "seatSoc"]):
                if int(qq) % 4 != 3: continue
                seat[(int(qq), n)] = (float(a), float(b), float(c))
            for qq, n, pc, pcs in read(f"{d}/fm_s{s}.tsv", ["quarter", "nation", "polityCode", "parliamentControlShare"]):
                if int(qq) % 4 != 3: continue
                pcv = fl(pc)
                if pcv is None or int(pcv) in ELECTED: continue
                k = (int(qq), n)
                if k not in seat: continue
                n_rows += 1
                p = fl(pcs) or 0.0
                g = grip.get(k) or 0.0
                st = seat[k]; tot = sum(st)
                sp = 0.0 if not tot > 1e-9 else max(0.0, min(1.0, 1 - max(st) / tot))
                splits.append(sp)
                if p > 1e-9: pcpos += 1
                pe = p * g
                if pe > 0.01:
                    pepos += 1; pes.append(pe * sp)
        w(f"{arm}\t{n_rows}\t{pcpos / max(1, n_rows):.4f}\t{pepos / max(1, n_rows):.4f}\t{qt(splits, .5):.3f}\t{qt(pes, .5):.4f}\t{qt(pes, .9):.4f}\t0")
    out.close()


main()
