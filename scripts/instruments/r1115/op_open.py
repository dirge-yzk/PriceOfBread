# (1115) c107op 開幕の四半期の空白（読むだけ・腕 0）。
# 使い方：python -X utf8 -I op_open.py 名=フォルダ ...（フォルダは monlaw_s*.tsv と sf_s*.tsv の置き場）
# 数える物（事前登録 D:/PoB_work/d1116_work/prereg.txt 問い 1・3）：
#  (a) 四半期 0 の選挙政体（polityCode 3・4）の国×種
#  (b) 四半期 0 に MinimumWage≥1 の国×種（選挙・非選挙）
#  (c) 上げの率の比：四半期 0 の上げ÷他の四半期の上げ（選挙と非選挙で別・他＝前の四半期と同じ類で移行の距離 1〜4 を除く）
#  (d) sf_s の四半期 0・1 の席の和（選挙政体の国）
#  (e) 1894 年より前の MinimumWage≥1 の行のうち、四半期 0 から途切れず続く分（開幕の続き）と国ごとの内訳
import sys, os, re, csv, collections
sys.stdout.reconfigure(newline=chr(10))
ELECT = {3, 4}
def seeds(d, pre):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(pre + r"_s(\d+)\.tsv", f)] if m)
def main():
    for a in sys.argv[1:]:
        name, d = a.split("=", 1)
        ss = seeds(d, "monlaw")
        q0el = collections.Counter(); q0mw = collections.Counter(); q0n = collections.Counter()
        opp = collections.Counter(); up = collections.Counter()
        pre_all = 0; pre_open = 0; pre_open_by = collections.Counter(); pre_by = collections.Counter()
        open_nat = collections.Counter(); el_nat = collections.Counter()
        seat = []
        for s in ss:
            rows = collections.defaultdict(list)
            with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
                r = csv.reader(f, delimiter="\t"); h = next(r)
                iq, iy, inn, ip, im = h.index("quarter"), h.index("year"), h.index("nation"), h.index("polityCode"), h.index("MinimumWage")
                for row in r: rows[row[inn]].append((int(row[iq]), int(row[iy]), int(row[ip]), float(row[im])))
            for nat, seq in rows.items():
                seq.sort()
                if seq[0][0] != 0: continue
                q, y, pol, mw = seq[0]
                cls = "選挙" if pol in ELECT else "非選挙"
                q0n[cls] += 1
                if cls == "選挙": el_nat[nat] += 1
                if mw >= 1:
                    q0mw[cls] += 1
                    if cls == "選挙": open_nat[nat] += 1
                # (c) 他の四半期
                last = None; prev = seq[0]
                for cur in seq[1:]:
                    if cur[2] != prev[2]: last = cur[0]
                    same = (cur[2] in ELECT) == (prev[2] in ELECT)
                    dq = cur[0] - last if last is not None else None
                    if same and not (dq is not None and 1 <= dq <= 4):
                        c2 = "選挙" if cur[2] in ELECT else "非選挙"
                        opp[c2] += 1
                        if cur[3] > prev[3]: up[c2] += 1
                    prev = cur
                # (e)
                cont = seq[0][3] >= 1
                for (q, y, pol, mw) in seq:
                    if mw < 1: cont = False
                    if y < 1894 and mw >= 1:
                        pre_all += 1; pre_by[nat] += 1
                        if cont: pre_open += 1; pre_open_by[nat] += 1
            sp = os.path.join(d, f"sf_s{s}.tsv")
            if os.path.exists(sp):
                with open(sp, encoding="utf-8", newline="") as f:
                    r = csv.reader(f, delimiter="\t"); h = next(r)
                    iq, inn, a0, a1, a2 = h.index("quarter"), h.index("nation"), h.index("seatCons"), h.index("seatLib"), h.index("seatSoc")
                    for row in r:
                        q = int(row[iq])
                        if q > 1: break
                        if row[inn] in el_nat: seat.append((s, q, row[inn], float(row[a0]) + float(row[a1]) + float(row[a2])))
        print(f"== {name}（種 {len(ss)}：{ss[0]}〜{ss[-1]}）")
        print(f"  (a)(b) 四半期 0：選挙政体 {q0n['選挙']} 国×種（MinimumWage≥1 が {q0mw['選挙']}）・非選挙 {q0n['非選挙']}（MinimumWage≥1 が {q0mw['非選挙']}）")
        print("     選挙政体の国（国×種・うち四半期 0 に MinimumWage≥1）：" + "・".join(f"{k} {v}/{open_nat[k]}" for k, v in sorted(el_nat.items(), key=lambda t: -t[1])))
        for c2 in ("選挙", "非選挙"):
            base = up[c2] / max(1, opp[c2]); r0 = q0mw[c2] / max(1, q0n[c2])
            print(f"  (c) {c2}：四半期 0 の上げ率 {r0:.4f}（{q0mw[c2]}/{q0n[c2]}）・他の四半期 {base:.6f}（{up[c2]}/{opp[c2]}）・比 {r0 / base if base > 0 else float('nan'):.0f}")
        sq = collections.defaultdict(list)
        for (s, q, nat, v) in seat: sq[q].append(v)
        for q in (0, 1):
            v = sq.get(q, [])
            print(f"  (d) sf_s の選挙政体の国×種・四半期 {q}：{len(v)} 行・席の和 0 が {sum(1 for x in v if x < 1e-9)}・和の最小 {min(v) if v else float('nan'):.3f}")
        top = pre_by.most_common(5)
        print(f"  (e) 1894 年より前の MinimumWage≥1 の行 {pre_all}・四半期 0 から途切れず続く分 {pre_open}（{100 * pre_open / max(1, pre_all):.1f}%）")
        print("     国ごと（行・うち開幕の続き）：" + "・".join(f"{k} {v}/{pre_open_by[k]}" for k, v in top))
main()
