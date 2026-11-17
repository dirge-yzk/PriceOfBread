# (1190) 紙：今の木の外国の取り分（rule_s）と、講和の影（rulepeace_s）の読み。読むだけ。
# 使い方：python -X utf8 -I paper_fg.py <INS のフォルダ> <出力.txt> [種の範囲]
import sys, collections
ins, outp = sys.argv[1], sys.argv[2]
lo, hi = (int(x) for x in (sys.argv[3] if len(sys.argv) > 3 else "1-10").split("-"))
out = []
def P(s): out.append(s)
def fl(x):
    try: return float(x)
    except ValueError: return float("nan")
YEARS = (1790, 1830, 1860, 1890, 1910, 1935)
WATCH = ("清", "オスマン", "エジプト", "ペルシャ", "シャム", "日本", "フランス", "イギリス", "ロシア", "アルゼンチン")
nat_d = collections.defaultdict(list)      # (国, 年) → 種ごとの人口加重の外国の取り分
cnt_pos = collections.defaultdict(list)    # 年 → 種ごとの 外国＞0.01 の州の数
top_owner = collections.Counter()          # 外国＞0.05 の州×年（見る年だけ）の持ち主
for s in range(lo, hi + 1):
    acc = collections.defaultdict(lambda: [0.0, 0.0]); pos = collections.Counter()
    with open(f"{ins}/rule_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            if len(c) < len(h): continue
            y = int(c[ix["year"]]); q = int(c[ix["quarter"]])
            if y not in YEARS or q % 4 != 0: continue
            x = fl(c[ix["ruleForeignShare"]]); w = fl(c[ix["popK"]]); n = c[ix["nation"]]
            if not (w > 0) or x != x: continue
            a = acc[(n, y)]; a[0] += x * w; a[1] += w
            if x > 0.01: pos[y] += 1
            if x > 0.05: top_owner[n] += 1
    for (n, y), (num, den) in acc.items():
        if n in WATCH: nat_d[(n, y)].append(num / den)
    for y in YEARS: cnt_pos[y].append(pos[y])
P(f"# (1190) paper_fg.py {ins} 種 {lo}-{hi}・外国の取り分＝rule_s の ruleForeignShare（年の最初の四半期）")
P("外国＞0.01 の州の数（種ごと）：" + "・".join(f"{y} {sorted(cnt_pos[y])}" for y in YEARS))
P("外国＞0.05 の州×見る年の持ち主の上位：" + "・".join(f"{n} {k}" for n, k in top_owner.most_common(10)))
P("国の人口加重の外国の取り分（種の中央・種の数）：")
for n in WATCH:
    cells = []
    for y in YEARS:
        v = sorted(nat_d.get((n, y), []))
        cells.append(f"{y} {v[len(v)//2]:.4f}({len(v)})" if v else f"{y} -")
    P(f"  {n}：" + "・".join(cells))
# 講和の影
peace = 0; taken = []; gco = 0; lrc = 0
for s in range(lo, hi + 1):
    with open(f"{ins}/rulepeace_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            if len(c) < len(h): continue
            peace += 1; t = int(float(c[ix["takenCount"]])); taken.append(t)
            gco += int(float(c[ix["greatCircleOnlyTakenCount"]])); lrc += int(float(c[ix["loserRegionCount"]]))
taken.sort()
dist = collections.Counter(min(t, 5) for t in taken)
P(f"講和の影（rulepeace_s・段 B の影の寄せ切り）：講和 {peace}（種 {lo}-{hi} の和）・影で取る州の和 {sum(taken)}・敗者の州の和 {lrc}・大圏だけの届きで取る州の和 {gco}")
P("  1 講和で取る州の数の度数（5 は 5 以上）：" + "・".join(f"{k}:{dist[k]}" for k in sorted(dist)))
with open(outp, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(out) + "\n")
print("\n".join(out))
