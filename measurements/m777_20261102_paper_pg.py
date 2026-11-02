# -*- coding: utf-8 -*-
# (777) 紙 c7pg：港の陸塊の札で、(770) の港の選び方が別の陸塊の港を選ぶ行を数え、形 (a)(b)(c)(c′)(d) を同じ盤面で比べる（読むだけ）。
# 事前登録 measurements/m777_20261102_prereg_pg.txt の [5][6]。
#   ① keep の portch_s（m773 の木の計器の生）と landmass.json だけで、今の形の crossLandmass の割合（分母 (i) 全ての内陸州×年・(ii) 港が変わった行 portUsed≠portGc）と上位の州。
#   ② portlm_s（c7pg の木の計器の生＝m773 と同じ軌道）で形ごとの crossLandmass・U2（eff＞2×gcKmMin）・島に住む州が自分の島の港を選ぶ割合・名指しの 3 州・開幕（最初の年）の盤面。
#   自己検査＝Python の札（landmass.json の名前の引き）と portlm_s の lmRegion・lmUsed の一致・portch_s と portlm_s の portUsed の一致・形 c（字義）と形 b の港の一致（予言 0 の不一致）。
# 使い方：python m777_20261102_paper_pg.py --landmass <SimCore/landmass.json> --portch <dir>[:<種の頭>] ... --portlm <dir> ... [--out <出力.txt>]
import argparse, collections, glob, json, math, os, re, sys

CONT = {"eurasia", "africa", "north-america", "south-america", "australia"}   # 大陸の札（これ以外は島）
NAMED = ["中央メキシコ", "リヨン", "トゥールーズ"]
SHAPES = [("a", "portUsed", "lmUsed", None), ("b", "portFin", "lmFin", "effFinKm"), ("c", "portSea", "lmSea", "effSeaKm"),
          ("c'", "portShore", "lmShore", "effShoreKm"), ("d", "portDrop", "lmDrop", "effDropKm")]


def fnum(s):
    if s == "inf": return math.inf
    if s in ("", "NaN", "nan"): return math.nan
    return float(s)


def load_landmass(p):
    with open(p, "rb") as f:
        d = json.loads(f.read().decode("utf-8-sig"))
    by = {}
    for x in d:
        by.setdefault(x["name"], x["landmass"])
    return by


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


def files(d, kind):
    return sorted(glob.glob(os.path.join(d, f"{kind}_s*.tsv")), key=seed_of)


def pct(a, b):
    return f"{100.0 * a / b:.2f}%（{a}/{b}）" if b else "—（0/0）"


def part1(lm, dirs, out):
    out.append("## ① 今の形 (a) の crossLandmass（keep の portch_s＋landmass.json の名前の引き）")
    for d in dirs:
        fs = files(d, "portch")
        if not fs:
            out.append(f"- {d}: portch_s が無い"); continue
        n_all = n_cross = n_chg = n_chg_cross = n_moved = 0; unk = set()
        top = collections.Counter(); topport = collections.defaultdict(collections.Counter); topnat = collections.defaultdict(collections.Counter)
        yrs = collections.defaultdict(list)
        first = {"all": 0, "cross": 0, "chg": 0, "chgc": 0}
        for p in fs:
            rs = rows_of(p)
            y0 = min(int(r["year"]) for r in rs) if rs else None
            for r in rs:
                lr = lm.get(r["region"]); lu = lm.get(r["portUsed"])
                if lr is None: unk.add(r["region"])
                if lu is None: unk.add(r["portUsed"])
                cr = (lr is not None and lu is not None and lr != lu)
                chg = r["portUsed"] != r["portGc"]
                lg = lm.get(r["portGc"])
                n_all += 1; n_cross += cr; n_chg += chg; n_chg_cross += (chg and cr); n_moved += (chg and cr and lg == lr)
                if int(r["year"]) == y0:
                    first["all"] += 1; first["cross"] += cr; first["chg"] += chg; first["chgc"] += (chg and cr)
                if cr:
                    k = r["region"]; top[k] += 1; topport[k][f"{r['portUsed']}({lu})"] += 1; topnat[k][r["nation"]] += 1; yrs[k].append(int(r["year"]))
        seeds = f"{seed_of(fs[0])}〜{seed_of(fs[-1])}"
        out.append(f"- 種 {seeds}（{d}）：(i) 全ての内陸州×年 {pct(n_cross, n_all)}・(ii) 港が変わった行（portUsed≠portGc）{pct(n_chg_cross, n_chg)}・(iii) 移った行（港が変わった行のうち旧の港 portGc は州と同じ札・新の港は別の札＝★(773) の数え）{pct(n_moved, n_chg)}・名前の札が引けない地点 {len(unk)}" + (f"（{sorted(unk)[:8]}）" if unk else ""))
        out.append(f"  - 開幕（各種の最初の年）：(i) {pct(first['cross'], first['all'])}・(ii) {pct(first['chgc'], first['chg'])}")
        out.append(f"  - 上位の州（crossLandmass の行の数・持ち主の最頻・選んだ港の最頻・年の範囲）：")
        for k, c in top.most_common(12):
            out.append(f"    - {k}［{lm.get(k)}］{c} 行・{topnat[k].most_common(1)[0][0]}・{topport[k].most_common(1)[0][0]}・{min(yrs[k])}〜{max(yrs[k])}")


def eff_a(r):
    l = fnum(r["landKmUsed"]); return l if math.isfinite(l) else fnum(r["gcKmUsed"])


def part2(lm, dirs, portch_dirs, out):
    out.append("")
    out.append("## ② 形ごとの量（portlm_s の反実仮想の列＝同じ盤面に形を当てた答え）")
    out.append("形：(a) 今の形〈陸路が有限 ? 陸路 : 大圏〉の最小／(b) 有限だけ（無ければ大圏）／(c) 字義＝∞の港に〈同じ国の最寄りの有限の港までの陸路＋海路〉／(c′) 岸を全ての国の港へ広げた形（担当が足した変種）／(d) 札が同じで有限の港があれば∞を外す")
    for d in dirs:
        fs = files(d, "portlm")
        if not fs:
            out.append(f"- {d}: portlm_s が無い"); continue
        seeds = f"{seed_of(fs[0])}〜{seed_of(fs[-1])}"
        allrows = []; opening = []
        selfchk = collections.Counter()
        pch = {}
        for pd in portch_dirs:
            for p in files(pd, "portch"):
                s = seed_of(p)
                if s < seed_of(fs[0]) or s > seed_of(fs[-1]): continue
                for r in rows_of(p): pch[(s, r["year"], r["nation"], r["region"])] = r["portUsed"]
        for p in fs:
            s = seed_of(p); rs = rows_of(p)
            y0 = min(int(r["year"]) for r in rs) if rs else None
            for r in rs:
                r["_seed"] = s
                allrows.append(r)
                if int(r["year"]) == y0: opening.append(r)
                selfchk["行"] += 1
                if lm.get(r["region"]) != r["lmRegion"]: selfchk["州の札の不一致"] += 1
                if r["portUsed"] and lm.get(r["portUsed"]) != r["lmUsed"]: selfchk["港の札の不一致"] += 1
                if r["portSea"] != r["portFin"]: selfchk["形 c と形 b の港の不一致"] += 1
                if r["crossLandmass"] != ("1" if r["lmUsed"] != r["lmRegion"] else "0"): selfchk["crossLandmass の列の不一致"] += 1
                k = (s, r["year"], r["nation"], r["region"])
                if pch:
                    if k not in pch: selfchk["portch_s に無い行"] += 1
                    elif pch[k] != r["portUsed"]: selfchk["portch_s と portUsed の不一致"] += 1
        if pch:
            ks = {(r["_seed"], r["year"], r["nation"], r["region"]) for r in allrows}
            selfchk["portlm_s に無い portch_s の行"] = sum(1 for k in pch if k not in ks)
        out.append(f"### 種 {seeds}（{d}）")
        out.append("- 自己検査：" + "・".join(f"{k} {v}" for k, v in selfchk.items()))
        for label, rows in (("全期間", allrows), ("開幕（各種の最初の年）", opening)):
            out.append(f"- {label}（行 {len(rows)}）")
            out.append("  | 形 | cross (i) 全ての行 | cross (ii) 港が変わった行 | (iii) 移った行 | 避けられる cross（札が同じ有限の港があるのに別の札） | U2 (i) 全ての行 | U2 (ii) 港が変わった行 | 島の州が自分の島の港 | 港が変わった行の数 |")
            out.append("  |---|---|---|---|---|---|---|---|---|")
            for name, pc, lc, ec in SHAPES:
                n = len(rows); cr = chg = chgcr = u2 = chgu2 = isl = islself = moved = avoid = 0
                for r in rows:
                    port = r[pc]; lmx = r[lc]
                    e = eff_a(r) if ec is None else fnum(r[ec])
                    g = fnum(r["gcKmMin"])
                    c = lmx != r["lmRegion"]
                    u = (e > 2.0 * g) if (math.isfinite(e) and math.isfinite(g)) else False
                    ch = port != r["portGc"]
                    cr += c; u2 += u
                    if ch: chg += 1; chgcr += c; chgu2 += u; moved += (c and r["lmGc"] == r["lmRegion"])
                    avoid += (c and int(r["sameLmFinitePortCount"]) >= 1)
                    if r["lmRegion"] not in CONT and int(r["lmRegionPortCount"]) >= 1:
                        isl += 1; islself += (lmx == r["lmRegion"])
                out.append(f"  | ({name}) | {pct(cr, n)} | {pct(chgcr, chg)} | {pct(moved, chg)} | {pct(avoid, n)} | {pct(u2, n)} | {pct(chgu2, chg)} | {pct(islself, isl)} | {chg} |")
        ca = [r for r in allrows if r["crossLandmass"] == "1"]
        fin = sum(1 for r in ca if math.isfinite(fnum(r["landKmUsed"])))
        nolm = sum(1 for r in ca if r["lmRegionPortCount"] == "0")
        out.append(f"- (a) の crossLandmass {len(ca)} 行の内訳：使った港への陸路が有限（札は違うが陸で繋がる＝例 シナイ）{pct(fin, len(ca))}・州の札に同じ国の港が 1 つも無い（別の札へ出るしかない）{pct(nolm, len(ca))}")
        # (b) と (d) が違う行の型
        diff_bd = [r for r in allrows if r["portFin"] != r["portDrop"]]
        out.append(f"- (b) と (d) が違う行 {len(diff_bd)}（予言：有限の港が全て別の札の行だけ）：sameLmFinitePortCount=0 の行 {sum(1 for r in diff_bd if r['sameLmFinitePortCount']=='0')}")
        ex = collections.Counter((r["nation"], r["region"], r["portFin"], r["portDrop"]) for r in diff_bd)
        for (na, rg, pf, pd), c in ex.most_common(6):
            out.append(f"  - {na}領{rg}：(b) {pf}［{lm.get(pf)}］／(d) {pd}［{lm.get(pd)}］{c} 行")
        out.append("- 名指しの 3 州（(a) が別の陸塊を選んだ行・持ち主の最頻と年・形ごとの港の最頻〔札・eff の中央 km〕）")
        for nm in NAMED:
            rs = [r for r in allrows if r["region"] == nm and r["crossLandmass"] == "1"]
            if not rs:
                out.append(f"  - {nm}：(a) が別の陸塊を選んだ行は無い"); continue
            nat = collections.Counter(r["nation"] for r in rs).most_common(2)
            ys = [int(r["year"]) for r in rs]
            parts = []
            for name, pc, lc, ec in SHAPES:
                cnt = collections.Counter(r[pc] for r in rs).most_common(1)[0]
                es = sorted((eff_a(r) if ec is None else fnum(r[ec])) for r in rs if r[pc] == cnt[0])
                med = es[len(es) // 2] if es else math.nan
                parts.append(f"({name}) {cnt[0]}［{lm.get(cnt[0])}］{cnt[1]}/{len(rs)}・{med:,.0f} km")
            out.append(f"  - {nm}［{lm.get(nm)}］{len(rs)} 行・{'・'.join(f'{a}{b}' for a, b in nat)}・{min(ys)}〜{max(ys)}：" + "／".join(parts))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--landmass", required=True)
    ap.add_argument("--portch", action="append", default=[])
    ap.add_argument("--portlm", action="append", default=[])
    ap.add_argument("--out")
    a = ap.parse_args()
    lm = load_landmass(a.landmass)
    out = ["# (777) 紙 c7pg（読むだけ）", f"大陸の札＝{sorted(CONT)}（これ以外は島）", ""]
    part1(lm, a.portch, out)
    if a.portlm: part2(lm, a.portlm, a.portch, out)
    txt = "\n".join(out) + "\n"
    if a.out:
        with open(a.out, "wb") as f: f.write(txt.encode("utf-8"))
    sys.stdout.buffer.write(txt.encode("utf-8"))


if __name__ == "__main__":
    main()
