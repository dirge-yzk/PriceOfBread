# (1232) 紙：段 P の平時の外国の目標（旧の (1−b) の形と案丙の形）で、州が平時に外国へ移る e の閾値を数える。読むだけ。
# 使い方: python -X utf8 -I paper_p.py <INSRAW> <種の下> <種の上> <出力>
# rule_s の tgtLocalShare（l）・tgtUngovShare（u）＝今の在地と無統治の目標（外国を読まない）を州×四半期で読む。
# e（外国の請求の比）は生に列が無い＝e を格子で動かして、外国が最大になる最小の e（閾値 e*）を州ごとに出す。
import sys, statistics
from collections import defaultdict
ins, lo, hi, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
out = []
def P(s): out.append(s)
def fl(x):
    try:
        v = float(x); return v if v == v else None
    except ValueError:
        return None

GRID = [i / 100 for i in range(0, 101)]
def estar(l, u, pi, mode):
    # 案丙：国家 1−l−u（外国で減らない）・在地 l(1−e)・無統治 u(1−e)・外国の最大 f＝e·π·(l＋u)
    # mode 'min'＝持ち主の側＝国家だけ（協力 iκ＝0・在地は自立の側に立つ）／'max'＝国家＋在地（iκ＝1）
    # mode 'old'＝旧の (1−b) の形の不動点：X＝e(l＋u)÷(1−e)・国家＝1−l−u−X・外国の最大＝π·X
    for e in GRID:
        if mode == 'old':
            if e >= 1: return 1.0
            X = e * (l + u) / (1 - e)
            st = max(0.0, 1 - l - u - X)   # 国家が 0 に張り付いた後は近似（和の割り直しを省く）
            f = pi * X
            if f > max(st + l, u): return e
            continue
        f = e * pi * (l + u)
        st = 1 - l - u
        if mode == 'min':
            if f > max(st, l * (1 - e), u * (1 - e)): return e
        else:
            if f > max(st + l * (1 - e), u * (1 - e)): return e
    return None

BINS = [0.2, 0.4, 0.6, 0.8, 1.0]
rows = 0
# 州ごと（種×国×州）に、年の窓 1850〜1914 の l＋u の中央を取り、閾値を出す
acc = defaultdict(list)
s_hist = defaultdict(int)
for s in range(lo, hi + 1):
    with open(f"{ins}/rule_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            y = int(float(c[ix["year"]]))
            l = fl(c[ix["tgtLocalShare"]]); u = fl(c[ix["tgtUngovShare"]])
            if l is None or u is None: continue
            rows += 1
            sv = l + u
            s_hist[min(9, int(sv * 10))] += 1
            if 1850 <= y <= 1914:
                acc[(s, c[ix["nation"]], c[ix["region"]], c[ix["tier"]])].append((l, u))

P(f"rule_s の行 {rows}（種 {lo}-{hi}）。l＋u（今の在地と無統治の目標の和）の度数（0.1 刻み・全ての四半期）：")
P("  " + "  ".join(f"{k/10:.1f}-:{s_hist[k]}" for k in range(10)))
P("")
P("州（種×国×州・1850〜1914 の l と u の中央）ごとに、外国が最大になる最小の e（e*）の度数。π＝一番の債権国の分。")
P("  min＝持ち主の側を国家だけで数える（在地は自立）・max＝国家＋在地（協力）・old＝旧の (1−b) の形の不動点 X＝e(l＋u)÷(1−e)")
modes = [("丙 π1 min", 1.0, 'min'), ("丙 π1 max", 1.0, 'max'), ("丙 π0.44 min", 0.44, 'min'), ("丙 π0.44 max", 0.44, 'max'),
         ("旧 π1", 1.0, 'old'), ("旧 π0.44", 0.44, 'old')]
reg = {}
for k, v in acc.items():
    reg[k] = (statistics.median(a for a, _ in v), statistics.median(b for _, b in v))
P(f"  州の数 {len(reg)}（植民地格 {sum(1 for k in reg if k[3]=='Colonial')}）")
for name, pi, mode in modes:
    cnt = defaultdict(int); cntCol = defaultdict(int)
    for k, (l, u) in reg.items():
        e = estar(l, u, pi, mode)
        b = next((x for x in BINS if e is not None and e <= x), None)
        key = b if b is not None else 'never'
        cnt[key] += 1
        if k[3] == 'Colonial': cntCol[key] += 1
    P(f"  {name}: " + "  ".join(f"e*≦{b}:{cnt[b]}" for b in BINS) + f"  届かない:{cnt['never']}  ／うち植民地格 " + " ".join(f"{cntCol[b]}" for b in BINS) + f" {cntCol['never']}")
P("")
P("国ごと（中核と統合の州だけ・州の数の和／種の和）：e*（丙 π1 min）≦0.4・≦0.6 の州の数と、旧 π1 の ≦0.4")
bynat = defaultdict(lambda: [0, 0, 0, 0])
for k, (l, u) in reg.items():
    if k[3] == 'Colonial': continue
    n = k[1]; bynat[n][0] += 1
    e1 = estar(l, u, 1.0, 'min'); e2 = estar(l, u, 1.0, 'old')
    if e1 is not None and e1 <= 0.4: bynat[n][1] += 1
    if e1 is not None and e1 <= 0.6: bynat[n][2] += 1
    if e2 is not None and e2 <= 0.4: bynat[n][3] += 1
for n, v in sorted(bynat.items(), key=lambda kv: -kv[1][2])[:15]:
    P(f"  {n}: 州 {v[0]}・丙≦0.4 {v[1]}・丙≦0.6 {v[2]}・旧≦0.4 {v[3]}")
P("")
P("清とオスマンの州（種 1 だけ・中央の l と u と e*）：")
for k, (l, u) in sorted(reg.items()):
    if k[0] != lo or k[1] not in ("清", "オスマン") : continue
    e1 = estar(l, u, 1.0, 'min'); e2 = estar(l, u, 1.0, 'old'); e3 = estar(l, u, 0.44, 'min')
    P(f"  {k[1]} {k[2]} {k[3]} l={l:.3f} u={u:.3f} 丙π1min={e1} 丙π0.44min={e3} 旧π1={e2}")
with open(outp, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(out) + "\n")
