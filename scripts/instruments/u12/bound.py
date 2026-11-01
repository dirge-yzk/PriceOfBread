# (726) u12：U2＋U4 を 1 本の腕に重ねた形（流れ＋借入）の G-U4-2 の R の上界を、回さずに数から出す（読むだけ）。
# R_A の上界＝R_U4（u4a の腕 B の paid÷基線の paid・record ②）＋ 余地の足し（余地÷(10 年×基線の年の支払い)＝room/D×D/paid÷10）。
#   版 1（1 回だけ）：与信の余地は 1 度使えば埋まる＝試合の十年の数（1790〜1910 年代の 13）で割る。
#   版 2（毎十年に満ちる・甘い側）：余地がその十年の初めに満ちて全て建設へ回る。
# 組は u4a の pin（g2_cells）＝縛る組 tight・緩む組 loose・早い緩む組＝loose のうち 1880 年代以前。
import json, re, statistics as st, sys
PIN, REC, DEF = sys.argv[1], sys.argv[2], sys.argv[3]
pin = json.load(open(PIN, encoding="utf-8"))["g2_cells"]
ru4 = {}
for line in open(REC, encoding="utf-8"):
    m = re.match(r"home\t(\S+)\t(\d+)\t.*paid 基線 ([0-9.e+-]+)／腕 ([0-9.e+-]+)", line)
    if m: ru4[(m.group(1), int(m.group(2)))] = float(m.group(4)) / float(m.group(3))
dd = {}
for line in open(DEF, encoding="utf-8"):
    if line.startswith("#") or line.startswith("nation"): continue
    x = line.rstrip("\n").split("\t"); dd[(x[0], int(x[1]))] = {k: float(v) for k, v in zip(["S/Y", "old/flow", "fcut", "D/paid", "D/Y", "room/D", "rep/D", "L/D", "repB"], x[2:])}
out = {}
for key, c in pin.items():
    n, d = key.split("|"); d = int(d)
    g = c["group"]; gs = [g] + (["early"] if g == "loose" and d <= 1880 else [])
    r = ru4.get((n, d)); q = dd.get((n, d))
    if r is None or q is None: continue
    add = max(0.0, q["room/D"]) * q["D/paid"] / 10.0
    for gg in gs:
        out.setdefault(gg, []).append((r, r + add / 13.0, r + add))
for g, v in out.items():
    a, b, c = zip(*v)
    print(f"{g}\t件数 {len(v)}\tR_U4 の中央 {st.median(a):.3f}\t版1（1 回だけ）の上界の中央 {st.median(b):.3f}\t版2（毎十年に満ちる）の上界の中央 {st.median(c):.3f}")
