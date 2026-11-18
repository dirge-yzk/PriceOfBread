# 使い方: python -X utf8 -I w0.py <rawdir> <seeds 1-10>
# (1223) c123iv 狙い W0・W0b（事前登録 [3]）の読み手（読むだけ）。国×年（class_s の資本家 classId 3 の年の行・sizeK＞0）で
#  P＝nation_s の constructionPlannedMoneyPerMonth のその年の四半期の平均、E＝class_s の earnedPerMonth（E が正でなければ 0）。
#  W0＝P＞0 の行のうち P＞1.25×E の割合（≦0.10 で通る・基準 M は 0.394）。W0b＝1840〜1889 年の P＞0 かつ E＞0 の行の P÷E の中央（≧0.25 で通る）。
import sys, statistics as st
raw, sr = sys.argv[1], sys.argv[2]
s0, s1 = int(sr.split('-')[0]), int(sr.split('-')[1])
def fl(x):
    try: return float(x)
    except: return float("nan")
tot = 0; over = 0; r = []; ns = 0
for s in range(s0, s1 + 1):
    ny = {}
    try: f = open(f"{raw}/nation_s{s}.tsv", encoding="utf-8")
    except FileNotFoundError: continue
    ns += 1
    with f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip("\n").split("\t"); k = (c[ix["nation"]], int(c[ix["year"]]))
            a = ny.setdefault(k, [0, 0.0]); a[0] += 1; a[1] += fl(c[ix["constructionPlannedMoneyPerMonth"]])
    with open(f"{raw}/class_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip("\n").split("\t")
            if c[0] in ("open", "warm") or int(c[3]) != 3: continue
            n, y = c[1], int(c[2]); E = fl(c[ix["earnedPerMonth"]]); z = fl(c[ix["sizeK"]])
            a = ny.get((n, y))
            if not a or not a[0] or not (z > 0): continue
            P = a[1] / a[0]
            if not (P > 0): continue
            Ep = E if E > 0 else 0.0
            tot += 1
            if P > 1.25 * Ep: over += 1
            if 1840 <= y < 1890 and Ep > 0: r.append(P / Ep)
w0 = over / tot if tot else float("nan"); w0b = st.median(r) if r else float("nan")
print(f"{raw} 種 {ns}：行 {tot}・P＞1.25E {over}")
print(f"W0 割合 {w0:.4f} ≦ 0.10 → {'通る' if w0 <= 0.10 else '外れ'}（基準 M 0.394）")
print(f"W0b 1840〜1889 の P÷E の中央 {w0b:.4f}（行 {len(r)}）≧ 0.25 → {'通る' if w0b >= 0.25 else '外れ'}")
