# 使い方: python -X utf8 -I q1_stock.py <生> <種 a-b> [国]
# (1231) 問い 1 の六：資本家（階級 3）の在庫の行き先（窓の区切りごと・計器 9 国か 1 国・10 種の和）。
# ΔS＝貯蓄の末−頭（class_s の四半期の行）・E と P は class_s の月の額×3（四半期の末の月の値＝近似）・extra と消費は spend_s（月の平均×月数）。
# 残り＝ΔS−(E−P)＝税・十分の一税・窃盗・回収・利払い・移民・相続ほかの正味（計器が書かない行を 0 と数えない＝残りは名指さない）。
import sys
NAT = {"フランス", "イギリス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本", "アメリカ"}
def fl(x):
    try: return float(x)
    except: return float("nan")
bk = [(0, 3), (4, 7), (8, 39), (40, 159), (160, 187), (188, 227), (228, 267), (268, 399), (400, 667)]
def bof(q):
    for i, (a, b) in enumerate(bk):
        if a <= q <= b: return i
raw = sys.argv[1]; s0, s1 = map(int, sys.argv[2].split("-")); only = sys.argv[3] if len(sys.argv) > 3 else None
nat = {only} if only else NAT
G = {i: dict(dS=0.0, E=0.0, P=0.0, X=0.0, C=0.0, Sh=0.0) for i in range(len(bk))}
for s in range(s0, s1 + 1):
    S = {}
    with open(f"{raw}/class_s{s}.tsv", encoding="utf-8") as f:
        f.readline()
        for l in f:
            c = l.rstrip("\n").split("\t")
            if c[3] != "3" or c[1] not in nat or not c[0].startswith("q"): continue
            q = int(c[0][1:]); S[(c[1], q)] = fl(c[6]); b = bof(q)
            e, p = fl(c[8]), fl(c[9])
            if e == e and p == p: G[b]["E"] += 3 * e; G[b]["P"] += 3 * p
    for n in nat:
        for i, (a, b) in enumerate(bk):
            s_end = S.get((n, b)); s_beg = S.get((n, a - 1)) if a > 0 else S.get((n, 0))
            if s_end is not None and s_beg is not None and s_end == s_end and s_beg == s_beg: G[i]["dS"] += s_end - s_beg; G[i]["Sh"] += s_beg
    with open(f"{raw}/spend_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip("\n").split("\t")
            if c[ix["nation"]] not in nat or c[ix["classId"]] != "3": continue
            b = int(c[0]); m = fl(c[ix["monthsCount"]]); pay = fl(c[ix["payPerMonth"]])
            if not pay == pay: continue
            if c[ix["kind"]] == "extra": G[b]["X"] += pay * m
            elif c[ix["kind"]] in ("good",): G[b]["C"] += pay * m
print("bucket\t頭の在庫\tΔS\tE（稼ぎ）\tP（支出）\textra（建設ほか）\t財の消費\tE−P\t残り＝ΔS−(E−P)\textra÷E\t消費÷E")
for i, g in G.items():
    print(f"{i}\t{g['Sh']:.4g}\t{g['dS']:.4g}\t{g['E']:.4g}\t{g['P']:.4g}\t{g['X']:.4g}\t{g['C']:.4g}\t{g['E']-g['P']:.4g}\t{g['dS']-(g['E']-g['P']):.4g}\t{g['X']/g['E'] if g['E'] else float('nan'):.3f}\t{g['C']/g['E'] if g['E'] else float('nan'):.3f}")
