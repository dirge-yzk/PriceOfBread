# (1422) c152sd 内戦の始まりの分け（記録・(1418) 反証役の分け）：wars_s の civilWarFlag=1 の行（sideA＝親の国・q＝始まりの四半期）を、
# 同じ国の同じ四半期±1 に ①勢力の蜂起（dcont_s の factionRevoltsCum が増えた四半期）②首都の蜂起（coupgate_s の route=3）③その他 で分ける。読むだけ。
#   使い方: python cw_split.py <ins> [種の数=10] [種の起点=1]
import sys, os, csv
csv.field_size_limit(1 << 30)
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10; s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
def read(t, s):
    p = os.path.join(d, f"{t}_s{s}.tsv")
    if not os.path.exists(p): return None, []
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); return h, list(r)
tot = [0, 0, 0]; n = 0
for s in range(s0, s0 + ns):
    h, rs = read("dcont", s); rev = set()
    if h:
        i_n, i_q, i_c = h.index("nation"), h.index("quarter"), h.index("factionRevoltsCum"); last = {}
        for r in rs:
            v = float(r[i_c]); k = r[i_n]
            if k in last and v > last[k]: rev.add((k, int(r[i_q])))
            last[k] = v
    h, rs = read("coupgate", s); r3 = set()
    if h:
        i_n, i_q, i_r = h.index("nation"), h.index("quarter"), h.index("route")
        for r in rs:
            if r[i_r] == "3": r3.add((r[i_n], int(r[i_q])))
    h, rs = read("wars", s)
    if not h: continue
    i_f, i_a, i_q = h.index("civilWarFlag"), h.index("sideA"), h.index("q")
    for r in rs:
        if r[i_f] != "1": continue
        n += 1; a = r[i_a]; q = int(r[i_q]); near = [(a, q + k) for k in (-1, 0, 1)]
        if any(x in rev for x in near): tot[0] += 1
        elif any(x in r3 for x in near): tot[1] += 1
        else: tot[2] += 1
print(f"ins={d} 種={s0}..{s0+ns-1} 内戦の始まり {n}：勢力の蜂起 {tot[0]}・route3 {tot[1]}・その他 {tot[2]}")
