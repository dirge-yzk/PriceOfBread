# (1162) c114k1 読むだけ：兵 0 の崖の原因を植民地格の州で数える（腕 0）。
# 引数：生のフォルダ 種の始め 種の終わり D1 の紙の出力（r1059_d1 の A1 で 0 に届く州の一覧つき） 出力
# 読む表：cpop_s（植民地格の州の兵士の POP＝className 兵士・月の頭）・monlaw_s（ConscriptionLaw）・admrule_s（seatsK＝国の定員）・rule_s（nowRuleDelivery）
# 機構（ソースの行）：志願（Volunteer）の国は Military.Step が州の募集を飛ばす（`if (!lawConscripts) continue;`）＝兵は社会移動の職の選び（Meritocracy.RouteViaPool）だけで入る。
#   職の選びは大きさ ≤1e-9 の POP を行き先にしない（`t.Size <= Eps) continue`）＝大きさ 0 の兵士の POP は志願の国では 0 のまま（吸い込む状態）。
#   徴兵（Selective 以上）の国は Military.Step が desired＝定員×州の人手÷国の人手 へ寄せる。州の人手＝募れる男×RuleDelivery（0 なら 0）。
import sys, os, csv
from collections import defaultdict

RAW, S0, S1, D1OUT, OUT = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
EPS = 1e-9
LAW = {0: "志願", 1: "選抜徴兵", 2: "国民皆兵"}

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def rd(path):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x

hitsA1 = defaultdict(list)   # 種 → [(国, 州, 年)]
with open(D1OUT, encoding="utf-8") as f:
    on = False
    for line in f:
        if line.startswith("# A1 で 0 に届く州の一覧"): on = True; continue
        if on and line.strip():
            t = line.rstrip("\n").split("\t")
            hitsA1[int(t[0])].append((t[1], t[2], int(t[3])))

cnt = defaultdict(int); natZero = defaultdict(int)
hitClass = defaultdict(int); hitDetail = []
for seed in range(S0, S1 + 1):
    cp = os.path.join(RAW, f"cpop_s{seed}.tsv")
    if not os.path.exists(cp): continue
    law = {}
    for ix, x in rd(os.path.join(RAW, f"monlaw_s{seed}.tsv")):
        law[(int(x[ix["quarter"]]), x[ix["nation"]])] = int(float(x[ix["ConscriptionLaw"]]))
    seats = {}
    for ix, x in rd(os.path.join(RAW, f"admrule_s{seed}.tsv")):
        seats[(int(x[ix["quarter"]]), x[ix["nation"]])] = fnum(x[ix["seatsK"]])
    deliv = {}
    for ix, x in rd(os.path.join(RAW, f"rule_s{seed}.tsv")):
        deliv[(int(x[ix["quarter"]]), x[ix["nation"]], x[ix["region"]])] = fnum(x[ix["nowRuleDelivery"]])
    ser = defaultdict(list)
    for ix, x in rd(cp):
        if x[ix["className"]] != "兵士": continue
        ser[(x[ix["nation"]], x[ix["region"]])].append((int(x[ix["quarter"]]), fnum(x[ix["popK"]])))
    for (nat, reg), s in ser.items():
        s.sort()
        first0 = s[0][1] <= EPS
        always0 = all(v <= EPS for _, v in s)
        cnt["州"] += 1
        if first0: cnt["州：最初の行で兵 0"] += 1
        if always0: cnt["州：ずっと兵 0"] += 1
        prev = None
        for q, v in s:
            L = law.get((q, nat), -1); sk = seats.get((q, nat), float("nan")); dv = deliv.get((q, nat, reg), float("nan"))
            cnt["行"] += 1
            if v <= EPS:
                cnt["兵 0 の行"] += 1; natZero[nat] += 1
                cnt[f"兵 0：法={LAW.get(L, '不明')}"] += 1
                if L >= 1:
                    if not (dv > EPS): cnt["兵 0：徴兵の国×RuleDelivery=0"] += 1
                    elif not (sk > EPS): cnt["兵 0：徴兵の国×定員 0"] += 1
                    else: cnt["兵 0：徴兵の国×届く×定員あり（他の理由）"] += 1
                if first0: cnt["兵 0：最初の行から 0 の州"] += 1
            if prev is not None:
                if prev <= EPS and v > EPS: cnt[f"0→正：法={LAW.get(L, '不明')}"] += 1
                if prev > EPS and v <= EPS: cnt[f"正→0：法={LAW.get(L, '不明')}"] += 1
            prev = v
    # A1 で 0 に届く州の分け（届く前の 3 年＝12 四半期の兵）
    for (nat, reg, y) in hitsA1.get(seed, []):
        s = ser.get((nat, reg))
        if not s: hitClass["表に無い（植民地格でない・cpop の外）"] += 1; continue
        qh = (y - 1783) * 4
        win = [v for q, v in s if qh - 12 <= q <= qh + 3]
        if not win: hitClass["窓に行が無い"] += 1; continue
        z = all(v <= EPS for v in win)
        L = law.get((qh, nat), law.get((qh + 3, nat), -1))
        key = ("兵 0（3 年ずっと）" if z else "兵あり") + f"×法={LAW.get(L, '不明')}"
        hitClass[key] += 1
        hitDetail.append((seed, nat, reg, y, "0" if z else "+", LAW.get(L, "不明")))

with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write(f"# (1162) 兵 0 の崖 生={RAW} 種={S0}〜{S1}・植民地格の州（cpop_s の兵士の行）\n")
    for k in sorted(cnt): o.write(f"{k}\t{cnt[k]}\n")
    o.write("# 兵 0 の行の多い国\n")
    for n, c in sorted(natZero.items(), key=lambda z: -z[1])[:12]: o.write(f"{n}\t{c}\n")
    o.write("# D1 案1（A1）で 0 に届く州の分け（r1059_d1 の一覧・届く前 3 年の兵）\n")
    for k in sorted(hitClass): o.write(f"{k}\t{hitClass[k]}\n")
    o.write("# 一覧（種, 国, 州, 年, 兵 0=0/あり=+, 法）\n")
    for h in sorted(hitDetail): o.write("\t".join(str(a) for a in h) + "\n")
