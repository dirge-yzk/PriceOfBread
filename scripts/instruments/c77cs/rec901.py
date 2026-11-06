# (901) c77cs 記録の読み手（読むだけ・門ではない）：fm_s の立憲化の最初の年（種ごと・中央）・立憲の四半期の PC の中央（同じ盤面の新÷旧）・政体の四半期の数。
# 使い方: python -X utf8 rec901.py <out.txt> <名前=ins> ...（種 1〜10、名前が S で始まれば種 11〜20）
import csv, sys, os, math, statistics
from collections import defaultdict

NAT = ["イギリス", "清", "フランス", "ロシア", "プロイセン", "オーストリア", "オスマン", "アメリカ", "日本"]

def fnum(s):
    try: return float(s)
    except ValueError: return math.nan

def read(d, sb):
    first = defaultdict(list); pcs = defaultdict(list); poly = defaultdict(lambda: defaultdict(int))
    for k in range(sb, sb + 10):
        p = os.path.join(d, f"fm_s{k}.tsv")
        if not os.path.exists(p): continue
        seen = {}
        with open(p, encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r); ix = {x: i for i, x in enumerate(h)}
            for l in r:
                cf = fnum(l[ix["polityCode"]])
                if not (cf == cf): continue
                nat = l[ix["nation"]]; code = int(cf); yr = int(fnum(l[ix["year"]])) if "year" in ix else None
                poly[nat][code] += 1
                if code == 3:
                    pcs[nat].append(fnum(l[ix["parliamentControlShare"]]))
                    if nat not in seen and yr is not None: seen[nat] = yr
        for nat in NAT: first[nat].append(seen.get(nat))
    return first, pcs, poly

def main():
    out = sys.argv[1]; arms = [a.split("=", 1) for a in sys.argv[2:]]
    o = []; P = o.append
    R = {nm: read(d, 11 if nm.startswith("S") else 1) for nm, d in arms}
    P("(901) 記録：立憲（polityCode 3）の最初の年（fm_s・種ごと・無い種は —）と中央")
    for nm in R:
        P(f"-- {nm}")
        for nat in NAT:
            ys = R[nm][0][nat]; v = [y for y in ys if y is not None]
            P(f"{nat}\t" + " ".join(str(y) if y is not None else "—" for y in ys) + f"\t中央 {statistics.median(v) if v else '—'}（{len(v)}/10 種）")
    P("== 立憲の四半期の PC の中央（fm_s・四半期の頭の写し）==")
    P("国\t" + "\t".join(R))
    for nat in NAT:
        P(nat + "\t" + "\t".join(f"{statistics.median(R[nm][1][nat]):.4f}（{len(R[nm][1][nat])}）" if R[nm][1][nat] else "—" for nm in R))
    names = list(R)
    if len(names) >= 2:
        P(f"新÷旧（{names[1]}÷{names[0]}・中央の比）")
        for nat in NAT:
            a, b = R[names[1]][1][nat], R[names[0]][1][nat]
            if a and b: P(f"{nat}\t{statistics.median(a) / statistics.median(b):.3f}")
    P("== 政体の四半期の数（fm_s・9 国・種の和）code: 0 絶対 1 専制 2 幕府 3 立憲 4 共和 5〜 奪取 ==")
    for nm in R:
        P(f"-- {nm}")
        for nat in NAT: P(nat + "\t" + " ".join(f"{c}:{n}" for c, n in sorted(R[nm][2][nat].items())))
    with open(out, "wb") as f: f.write(("\n".join(o) + "\n").encode("utf-8"))

main()
