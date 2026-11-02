# -*- coding: utf-8 -*-
# (779) c8pd 港の形 (d) の門の台本。事前登録 measurements/m779_20261102_prereg_pd.txt [4]。読むだけ。
#   python gate.py <portlm_s のあるフォルダ>... [--as used|a|fin|d] [--landmass <landmass.json>] [--out <出力.txt>]
#   --as＝どの列を〈使った港〉と見なすか（既定 used＝腕の実際の portUsed・a＝今の形の反実仮想 portA・fin＝形 (b) の反実仮想 portFin・d＝形 (d) の portD）。
#        突然変異の確かめ（門が落ちるか）と、腕 B の自分の盤面での反実仮想に使う。
# 門（10 種を合わせた値で読む）：
#   上限 A：港が変わった行（port≠portGc）のうち、lmGc==lmRegion かつ lmX≠lmRegion の割合 ≦1%
#   上限 B：港が変わった行のうち、eff(X)＞2×gcKmMin かつ lmX≠lmRegion の割合 ≦1.0%
#   下限  ：跳びの行（lmA≠lmRegion かつ landKmA が inf かつ sameLmFinitePortCount≧1）のうち lmX==lmRegion の割合 ≧0.9
#   一致  ：usedEqD が 0 の行 0（--as used のときだけ）
# 終了コード 0＝全ての門が通る・1＝落ちる門がある・2＝入力が無い。
import argparse, collections, glob, math, os, re, statistics, sys

THR_A = 0.01
THR_B = 0.010
THR_LOW = 0.9


def fnum(s):
    if s == "inf": return math.inf
    if s in ("", "NaN", "nan"): return math.nan
    return float(s)


def rows_of(path):
    with open(path, "rb") as f:
        lines = f.read().decode("utf-8").split("\n")
    h = lines[0].rstrip("\r").split("\t")
    out = []
    for ln in lines[1:]:
        ln = ln.rstrip("\r")
        if not ln: continue
        c = ln.split("\t")
        if len(c) < len(h): continue
        out.append(dict(zip(h, c)))
    return out


def seed_of(p):
    m = re.search(r"_s(\d+)\.tsv$", p)
    return int(m.group(1)) if m else -1


def pct(a, b):
    return f"{100.0 * a / b:.2f}%（{a}/{b}）" if b else "—（0/0）"


def view(r, mode):
    """(port, lm, eff) を返す。"""
    if mode == "used":
        l = fnum(r["landKmUsed"])
        return r["portUsed"], r["lmUsed"], (l if math.isfinite(l) else fnum(r["gcKmUsed"]))
    if mode == "a": return r["portA"], r["lmA"], fnum(r["effA"])
    if mode == "fin": return r["portFin"], r["lmFin"], fnum(r["effFinKm"])
    if mode == "d": return r["portD"], r["lmD"], fnum(r["effD"])
    raise SystemExit("--as が不明")


class Acc:
    def __init__(self):
        self.n = self.chg = self.a_num = self.b_num = 0
        self.jump = self.jump_same = 0
        self.eqd_bad = self.eqd_ok = 0


def feed(acc, r, mode):
    if not r["portUsed"]: return
    port, lm, eff = view(r, mode)
    acc.n += 1
    if r.get("usedEqD", "") == "0": acc.eqd_bad += 1
    elif r.get("usedEqD", "") == "1": acc.eqd_ok += 1
    chg = port != r["portGc"]
    if chg:
        acc.chg += 1
        if r["lmGc"] == r["lmRegion"] and lm != r["lmRegion"]: acc.a_num += 1
        g = fnum(r["gcKmMin"])
        if math.isfinite(eff) and math.isfinite(g) and eff > 2.0 * g and lm != r["lmRegion"]: acc.b_num += 1
    if r["lmA"] != r["lmRegion"] and not math.isfinite(fnum(r["landKmA"])) and int(r["sameLmFinitePortCount"]) >= 1:
        acc.jump += 1
        if lm == r["lmRegion"]: acc.jump_same += 1


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("dirs", nargs="+")
    ap.add_argument("--as", dest="mode", default="used", choices=["used", "a", "fin", "d"])
    ap.add_argument("--out")
    ap.add_argument("--detail", action="store_true", help="種ごとの値・同じ盤面の新÷旧・名指しの 3 州・開幕も出す")
    a = ap.parse_args()
    files = []
    for d in a.dirs: files += sorted(glob.glob(os.path.join(d, "portlm_s*.tsv")), key=seed_of)
    if not files:
        print("portlm_s が無い"); return 2
    tot = Acc(); per = {}; first = Acc()
    allrows = []
    for p in files:
        rs = rows_of(p)
        if not rs: continue
        y0 = min(int(r["year"]) for r in rs)
        acc = Acc()
        for r in rs:
            if "portA" not in r: print(f"{p}: portA の列が無い（足した列の前の計器）"); return 2
            feed(acc, r, a.mode); feed(tot, r, a.mode)
            if int(r["year"]) == y0: feed(first, r, a.mode)
            r["_seed"] = seed_of(p); allrows.append(r)
        per[seed_of(p)] = acc
    out = []
    out.append(f"# (779) c8pd 門の台本 --as {a.mode}・フォルダ {a.dirs}・種 {sorted(per)}・行 {tot.n}")
    A = tot.a_num / tot.chg if tot.chg else math.nan
    B = tot.b_num / tot.chg if tot.chg else math.nan
    L = tot.jump_same / tot.jump if tot.jump else math.nan
    okA = A <= THR_A
    okB = B <= THR_B
    okL = (L >= THR_LOW) if math.isfinite(L) else False
    out.append(f"上限 A（移った行 ≦{THR_A*100:.1f}%）：{pct(tot.a_num, tot.chg)}  {'通る' if okA else '落ちる'}")
    out.append(f"上限 B（U2 かつ cross ≦{THR_B*100:.1f}%）：{pct(tot.b_num, tot.chg)}  {'通る' if okB else '落ちる'}")
    out.append(f"下限（跳びの行で州と同じ札 ≧{THR_LOW}）：{pct(tot.jump_same, tot.jump)}  {'通る' if okL else '落ちる'}")
    ok_eq = True
    if a.mode == "used":
        ok_eq = tot.eqd_bad == 0 and tot.eqd_ok > 0
        out.append(f"一致（usedEqD が全て 1）：不一致 {tot.eqd_bad} 行・一致 {tot.eqd_ok} 行  {'通る' if ok_eq else '落ちる'}")
    out.append(f"港が変わった行 {tot.chg}・跳びの行 {tot.jump}")
    if a.detail:
        out.append("種ごと（A／B／下限）：")
        for s, c in sorted(per.items()):
            out.append(f"  種 {s}：A {pct(c.a_num, c.chg)}・B {pct(c.b_num, c.chg)}・下限 {pct(c.jump_same, c.jump)}・usedEqD 不一致 {c.eqd_bad}")
        out.append(f"開幕（各種の最初の年）：A {pct(first.a_num, first.chg)}・B {pct(first.b_num, first.chg)}・下限 {pct(first.jump_same, first.jump)}・港が変わった行 {first.chg}")
        # 同じ盤面の新÷旧（実際の portUsed と反実仮想 portA）
        diff = [r for r in allrows if r["portUsed"] and r["portUsed"] != r["portA"]]
        out.append(f"同じ盤面の新÷旧：portUsed≠portA の行 {pct(len(diff), tot.n)}")
        if diff:
            ratios = []
            for r in diff:
                e_new = view(r, "used")[2]; e_old = fnum(r["effA"])
                if math.isfinite(e_new) and math.isfinite(e_old) and e_old > 0: ratios.append(e_new / e_old)
            if ratios:
                ratios.sort()
                out.append(f"  eff（新÷旧）：中央 {statistics.median(ratios):.3f}・平均 {sum(ratios)/len(ratios):.3f}・p10 {ratios[len(ratios)//10]:.3f}・p90 {ratios[len(ratios)*9//10]:.3f}・最大 {ratios[-1]:.2f}・n {len(ratios)}")
                inc = sum(1 for x in ratios if x > 1.0)
                out.append(f"  新が長い行 {pct(inc, len(ratios))}（km は内陸の鉄道の減衰の入力＝NearestPortDistanceKm）")
            top = collections.Counter((r["nation"], r["region"], r["portA"], r["portUsed"]) for r in diff)
            out.append("  上位の（持ち主・州・旧の港→新の港）：")
            for (na, rg, pa, pu), c in top.most_common(10): out.append(f"    {na}領{rg}：{pa} → {pu} {c} 行")
        out.append("名指しの 3 州（portA が別の札だった行で、旧→新の港の最頻）：")
        for nm in ["中央メキシコ", "リヨン", "トゥールーズ"]:
            rs = [r for r in allrows if r["region"] == nm and r["lmA"] != r["lmRegion"]]
            if not rs: out.append(f"  {nm}：旧が別の札の行は無い"); continue
            ca = collections.Counter(r["portA"] for r in rs).most_common(1)[0]
            cu = collections.Counter(r["portUsed"] for r in rs).most_common(1)[0]
            out.append(f"  {nm}：{len(rs)} 行・旧 {ca[0]}（{ca[1]}）→ 使った港 {cu[0]}（{cu[1]}）")
    txt = "\n".join(out) + "\n"
    if a.out:
        with open(a.out, "wb") as f: f.write(txt.encode("utf-8"))
    sys.stdout.buffer.write(txt.encode("utf-8"))
    return 0 if (okA and okB and okL and ok_eq) else 1


if __name__ == "__main__":
    sys.exit(main())
