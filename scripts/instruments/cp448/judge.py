#!/usr/bin/env python
"""(448) 損失は持ち主の資産で切る旗の採否の読み手（読むだけ）。事前登録 measurements/cp_20261020_prereg.txt [5][12]。

使い方：PYTHONUTF8=1 python judge.py <B の ins> <基準 m443_ins> --src-b <B の木の Simulation.cs> --line-b <B の木の負債化の行> [--line-base 2935] [--seeds 10]
出力：J1（資本家の負債化の和の比）・J2（負債化÷実質GDP の全層の平均）・J4（producersCut の総和・尾の国の operatingScaleSum）・φ の層別・M1（A の計器つき 1 種のとき --m1 で R を出す）。
"""
import argparse, csv, os, sys, collections, statistics
sys.path.insert(0, os.path.dirname(__file__))
from guard import check_line
from cap_count import base, load


def tsv(path):
    with open(path, encoding="utf-8", newline="") as fh:
        yield from csv.DictReader(fh, delimiter="\t")


def fin_by(root, s, line):
    fin = collections.defaultdict(float); n = 0
    for r in tsv(os.path.join(root, f"flow_s{s}.tsv")):
        if r["kind"] == "give" and r["sourceFile"] == "Simulation.cs" and r["sourceLine"] == str(line) and r["classId"] == "3":
            fin[(base(r["nation"]), int(r["quarter"]))] += float(r["amountPerMonth"]); n += 1
    return fin, n


def j1_j2(root, line, seeds, big_k=5000.0):
    tot = 0.0; matched = 0; allq = []
    for s in seeds:
        rows = load(root, s)
        allsz = collections.defaultdict(float); size = {}
        for q, n, c, sz, *_ in rows:
            allsz[(n, q)] += sz
            if c == 3: size[(n, q)] = sz
        bigp = collections.defaultdict(float); peak = collections.defaultdict(float)
        for (n, q), v in allsz.items(): bigp[n] = max(bigp[n], v)
        for (n, q), v in size.items(): peak[n] = max(peak[n], v)
        gdp = {}
        for r in tsv(os.path.join(root, f"money_s{s}.tsv")):
            t = r["time"]
            if t.startswith("q") and r["event"] == "update":
                try: gdp[(base(r["nation"]), int(t[1:]))] = float(r["gdpRealPerMonth"])
                except ValueError: pass
        fin, n = fin_by(root, s, line); matched += n; tot += sum(fin.values())
        for (n_, q), sz in size.items():
            if bigp[n_] < big_k or peak[n_] <= 0 or gdp.get((n_, q), 0) <= 0: continue
            allq.append(fin.get((n_, q), 0.0) / gdp[(n_, q)])
    return tot, matched, (sum(allq) / len(allq) if allq else float("nan")), len(allq)


def cp_rows(root, s):
    p = os.path.join(root, f"cp_s{s}.tsv")
    return list(tsv(p)) if os.path.exists(p) else []


def tail_scale(root, seeds, names=("日本", "清", "オスマン"), q0=600):
    v = []
    for s in seeds:
        for r in tsv(os.path.join(root, f"factory_s{s}.tsv")):
            if base(r["nation"]) in names and int(r["quarter"]) >= q0:
                try: v.append(float(r["operatingScaleSum"]))
                except ValueError: pass
    return (sum(v) / len(v) if v else float("nan")), len(v)


def layers(root, seeds):
    """cp_s の行を class_s の資本家の人数の経路（ピーク比）で層別：層ごとの needInvestor・minNeedLossSelInvestor・eventsPhi・eventsPhi1・producersCut。"""
    out = collections.defaultdict(lambda: collections.Counter())
    for s in seeds:
        rows = load(root, s)
        size = {}; peak = collections.defaultdict(float)
        for q, n, c, sz, *_ in rows:
            if c == 3: size[(n, q)] = sz; peak[n] = max(peak[n], sz)
        # ピークの四半期（ピークより後だけを尾と呼ぶ）
        qpk = {}
        for (n, q), sz in size.items():
            if sz == peak[n] and n not in qpk: qpk[n] = q
        for r in cp_rows(root, s):
            n = base(r["nation"]); q = int(r["quarter"])
            if (n, q) not in size or peak[n] <= 0: continue
            ratio = size[(n, q)] / peak[n]
            lay = "5%未満" if ratio < 0.05 and q > qpk.get(n, 0) else ("5〜50%" if ratio < 0.5 else "50%以上")
            c = out[lay]
            for k in ("needInvestor", "lossSumInvestor", "lossSumInvestorSel", "minNeedLossSelInvestor", "phiNeedWInvestor", "needSum", "phiNeedW", "scaleCutSum"):
                c[k] += float(r[k])
            for k in ("events", "eventsPhi", "eventsPhi1", "producersCut"):
                c[k] += int(r[k])
            c["rows"] += 1
    return out


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("root"); ap.add_argument("basel")
    ap.add_argument("--src-b", required=True); ap.add_argument("--line-b", type=int, required=True); ap.add_argument("--line-base", type=int, default=2935)
    ap.add_argument("--seeds", type=int, default=10); ap.add_argument("--m1", action="store_true", help="A の計器つき（旗 0）の M1 の R だけ出す")
    a = ap.parse_args()
    check_line(a.src_b, a.line_b)
    seeds = range(1, a.seeds + 1)
    lay = layers(a.root, seeds)
    print(f"# root={a.root} basel={a.basel} line_b={a.line_b} seeds=1..{a.seeds}")
    print("[層別（資本家の人数のピーク比・ピーク後の 5% 未満を〈尾〉）] 層\t行\t事象\tneedInv\tlossInv\tlossInvSel\tmin(need,lossSel)Inv\tR=min/need\tφ>0\tφ=1\tφ=1割合\tφ不足加重(inv)\tproducersCut\tscaleCutSum")
    for k in ("5%未満", "5〜50%", "50%以上"):
        c = lay.get(k, collections.Counter())
        ni = c["needInvestor"]; R = c["minNeedLossSelInvestor"] / ni if ni > 0 else float("nan")
        p1 = c["eventsPhi1"] / c["eventsPhi"] if c["eventsPhi"] else float("nan")
        pw = c["phiNeedWInvestor"] / ni if ni > 0 else float("nan")
        print(f"{k}\t{c['rows']}\t{c['events']}\t{ni:.4g}\t{c['lossSumInvestor']:.4g}\t{c['lossSumInvestorSel']:.4g}\t{c['minNeedLossSelInvestor']:.4g}\t{R:.3f}\t{c['eventsPhi']}\t{c['eventsPhi1']}\t{p1:.3f}\t{pw:.3f}\t{c['producersCut']}\t{c['scaleCutSum']:.4g}")
    if a.m1:
        c = lay.get("5%未満", collections.Counter()); ni = c["needInvestor"]
        R = c["minNeedLossSelInvestor"] / ni if ni > 0 else float("nan")
        print(f"M1 R（尾・投資家）＝{R:.4f}  → J1 の線 = max(0.15, 1-0.85*R) = {max(0.15, 1 - 0.85 * R):.4f}" if R == R else "M1 R が計算できない（尾の事象なし）")
        return
    tb, mb, g2b, nb = j1_j2(a.root, a.line_b, seeds)
    t0, m0, g20, n0 = j1_j2(a.basel, a.line_base, seeds)
    print(f"J1 資本家の負債化の和：B {tb:.4g}（一致 {mb} 行）  基準 {t0:.4g}（一致 {m0} 行）  比 {tb / t0:.4f}")
    print(f"J2 負債化÷実質GDP の全層の平均：B {g2b:.5f}（{nb} 組）  基準 {g20:.5f}（{n0} 組）")
    sb, nsb = tail_scale(a.root, seeds); s0, ns0 = tail_scale(a.basel, seeds)
    pc = sum(int(r["producersCut"]) for s in seeds for r in cp_rows(a.root, s))
    print(f"J4 producersCut の総和 {pc}（≧1000）・尾の国 q≧600 の operatingScaleSum 平均 B {sb:.4g}（{nsb}）  基準 {s0:.4g}（{ns0}）→ B≦基準 か：{sb <= s0}")


if __name__ == "__main__":
    main()
