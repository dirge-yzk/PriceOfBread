# 使い方: python -X utf8 -I k49_perm.py <基準の rawdir> [種の起点 1] [種の数 10]
# (1331) c139k49 記録 H の誤りの外れの率（読むだけ・m1321/rev/k49v_perm.py の型を基準の生へ当てる）。
#  基準の 10 種を 5 種と 5 種に分ける全ての分け方（252 通り）で、片方を〈腕〉もう片方を〈基準〉と見なし、
#  mag_c139.py と同じ量（F＝工場の能力・I＝基盤の 3 生産者の能力・国×年）の種の中央の比が 10 以上か 0.1 以下になる率を出す。
#  基準の中央が 1 未満の行は数えない（mag_c139 の〈記録（基準＜1）〉と同じ）。効きが無い盤面で H が〈外〉になる率＝H が門として使えない根拠の数。
import sys, os, itertools, statistics as st
SERV = {"労働力", "教育", "医療", "行政", "兵力", "高度労働力", "農業労働力", "上下水道", "電化インフラ", "家事奉仕", "高等教育"}
INFRA = {"水道局", "ガス灯", "電灯網"}
NAT = ["イギリス", "フランス", "プロイセン", "アメリカ", "ロシア", "オーストリア", "清", "世界"]
def seedvals(raw, s):
    acc = {}; y0 = None
    with open(f"{raw}/prodcap_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        iy, io, ig, ir, irg, ic = ix["year"], ix["ownerNation"], ix["good"], ix["recipe"], ix["isRgo"], ix["capacity"]
        for l in f:
            c = l.rstrip("\n").split("\t"); y = c[iy]
            if y0 is None: y0 = y
            if y not in (y0, "1850", "1913"): continue
            yk = "開幕" if y == y0 else y
            if c[irg] == "0" and c[ig] not in SERV: q = "F"
            elif c[ir] in INFRA: q = "I"
            else: continue
            nm = "プロイセン" if c[io] == "ドイツ帝国" else c[io]
            try: cap = float(c[ic])
            except ValueError: cap = float("nan")
            for k in (nm, "世界"): acc[(k, q, yk)] = acc.get((k, q, yk), 0.0) + cap
    return acc
def main():
    raw = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
    seeds = list(range(s0, s0 + n)); V = {s: seedvals(raw, s) for s in seeds}
    keys = [(k, q, yk) for k in NAT for q in ("F", "I") for yk in ("開幕", "1850", "1913")]
    splits = list(itertools.combinations(seeds, n // 2))
    print(f"# k49_perm 基準={raw} 種 {s0}..{s0+n-1} 分け方 {len(splits)}")
    print("国\t量\t年\t数えた分け方\t外の分け方\t外の率\t種ごとの値")
    anyout = 0; counted = 0
    per = {}
    for idx in splits:
        A = set(idx); hit = False; c = False
        for key in keys:
            a = st.median([V[s].get(key, 0.0) for s in seeds if s in A]); b = st.median([V[s].get(key, 0.0) for s in seeds if s not in A])
            if not (b >= 1.0): continue
            c = True; r = a / b; o = not (0.1 < r < 10)
            t = per.setdefault(key, [0, 0]); t[0] += 1; t[1] += o
            hit = hit or o
        counted += c; anyout += hit
    for key in keys:
        t = per.get(key)
        if not t: continue
        vals = ",".join(f"{V[s].get(key, 0.0):.3g}" for s in seeds)
        print(f"{key[0]}\t{key[1]}\t{key[2]}\t{t[0]}\t{t[1]}\t{t[1]/t[0]:.3f}\t{vals}")
    print(f"どれか 1 行でも外になる分け方の率 {anyout}/{len(splits)} = {anyout/len(splits):.3f}")
main()
