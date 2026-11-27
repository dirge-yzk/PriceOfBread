# (1422) c152sd 門 G3〈誰に届いたか〉の読み手（facfloor_s・10 種の全ての行の和）。読むだけ。
#   門（事前登録 [8]）：Σ facSeenTargetK ≥ 1.10 × Σ targetAboveNK（Σ targetAboveNK＝0 なら Σ facSeenTargetK＞0）。旗 0 の確かめ：全ての行で facSeenTargetK＝targetAboveNK。
#   使い方: python g3.py <ins> [種の数=10] [種の起点=1]
import sys, os, csv
csv.field_size_limit(1 << 30)
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10; s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
S = dict(seen=0.0, aboveN=0.0, aboveP=0.0, target=0.0, partD=0.0, partEng=0.0, pop=0.0); rows = 0; eqdiff = 0.0; neq = 0; fc = 0; maxSeen = 0; maxCellsP = 0; cellsP = 0; cellsN = 0; miss = 0; flags = set()
for s in range(s0, s0 + ns):
    p = os.path.join(d, f"facfloor_s{s}.tsv")
    if not os.path.exists(p): miss += 1; continue
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            g = lambda k: float(x[ix[k]])
            rows += 1; flags.add(x[ix["floorFlag"]])
            S["seen"] += g("facSeenTargetK"); S["aboveN"] += g("targetAboveNK"); S["aboveP"] += g("targetAbovePK"); S["target"] += g("targetK")
            S["partD"] += g("partDK"); S["partEng"] += g("partEngK"); S["pop"] += g("popK")
            dd = abs(g("facSeenTargetK") - g("targetAboveNK")); eqdiff = max(eqdiff, dd); neq += dd > 0
            fc += int(g("factionsCount")); maxSeen = max(maxSeen, int(g("facSeenCount"))); maxCellsP = max(maxCellsP, int(g("cellsAboveP")))
            cellsP += int(g("cellsAboveP")); cellsN += int(g("cellsAboveN"))
print(f"ins={d} 種={s0}..{s0+ns-1} 行 {rows}・表の無い種 {miss}・floorFlag {sorted(flags)}")
if rows == 0: print("G3 判定不能（行なし）"); sys.exit(2)
ratio = S["seen"] / S["aboveN"] if S["aboveN"] > 0 else float("inf")
ok = (S["seen"] >= 1.10 * S["aboveN"]) if S["aboveN"] > 0 else S["seen"] > 0
print(f"Σ facSeenTargetK {S['seen']:.6g}・Σ targetAboveNK {S['aboveN']:.6g}・Σ targetAbovePK {S['aboveP']:.6g}・Σ targetK {S['target']:.6g}")
print(f"比 facSeen÷aboveN {ratio:.6g}（門 ≥ 1.10）→ {'通る' if ok else '落ちる'}")
print(f"記録：facSeen÷targetK {S['seen']/S['target'] if S['target']>0 else float('nan'):.6g}・aboveN÷targetK {S['aboveN']/S['target'] if S['target']>0 else float('nan'):.6g}")
print(f"記録：参加の割合 partEngK÷popK {S['partEng']/S['pop']:.6g}・partDK÷popK {S['partD']/S['pop']:.6g}")
print(f"記録：勢力の数の平均 {fc/rows:.4g}・facSeenCount の最大 {maxSeen}（≤100）・cellsAboveP の最大 {maxCellsP}・cellsAboveP の平均 {cellsP/rows:.4g}・cellsAboveN の平均 {cellsN/rows:.4g}")
print(f"旗 0 の確かめ：facSeenTargetK≠targetAboveNK の行 {neq}・差の最大 {eqdiff:.6g}")
print(f"G3\t{'PASS' if ok else 'FAIL'}\t{ratio:.6g}")
