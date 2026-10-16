"""(377) 合流の読み（読むだけ）：汚職の政治の 2 項の削除の記録 R1〜R3（事前登録 measurements/cx_20261016_prereg.txt [4]）。
使い方：python an_cx377.py <名>=<出力>/ins ...（例 t1=D:/PoB_work/merge_377b/out_t1i_10/ins t2=D:/PoB_work/merge_377b/out_t2i_10/ins）
  R1 交戦中と平時の闘争度の差（国×種ごとに atWar=1 と 0 の行の militancyMean の平均の差・各 8 行以上＝an_warpeace377.py と同じ定義）
  R2 天命 dynMandate の中央値・≧0.99 の行の割合（scalars_s の全ての行）
  R3 正統性 legitimacy の中央値（同）・国ごとの天命と正統性の中央値
"""
import sys, glob, statistics as st, collections

NL = "\n"
TAB = "\t"
for arg in sys.argv[1:]:
    name, path = arg.split("=", 1)
    diffs = []
    dm = []
    lg = []
    per = collections.defaultdict(lambda: [[], []])
    for f in sorted(glob.glob(path + "/scalars_s*.tsv")):
        acc = collections.defaultdict(lambda: [[], []])
        with open(f, encoding="utf-8") as fh:
            h = fh.readline().rstrip(NL).split(TAB)
            ix = {k: i for i, k in enumerate(h)}
            for line in fh:
                r = line.rstrip(NL).split(TAB)
                try:
                    m = float(r[ix["militancyMean"]])
                    d = float(r[ix["dynMandate"]])
                    l = float(r[ix["legitimacy"]])
                except Exception:
                    continue
                n = r[ix["nation"]]
                acc[n][1 if r[ix["atWar"]] == "1" else 0].append(m)
                dm.append(d)
                lg.append(l)
                per[n][0].append(d)
                per[n][1].append(l)
        for n, (p, w) in acc.items():
            if len(p) >= 8 and len(w) >= 8:
                diffs.append(st.mean(w) - st.mean(p))
    print(f"== {name}（{path}）")
    if diffs:
        print("  R1 国×種 %d・交戦中−平時の闘争度の中央 %.4f・平均 %.4f・差が正の割合 %.2f" % (len(diffs), st.median(diffs), st.mean(diffs), sum(x > 0 for x in diffs) / len(diffs)))
    if dm:
        print("  R2 天命の中央 %.4f・≧0.99 の行の割合 %.4f（行 %d）" % (st.median(dm), sum(x >= 0.99 for x in dm) / len(dm), len(dm)))
    if lg:
        print("  R3 正統性の中央 %.4f" % st.median(lg))
    for n in ["清", "オスマン", "ロシア", "日本", "フランス", "イギリス", "プロイセン", "オーストリア"]:
        if n in per and per[n][0]:
            print("     %s 天命の中央 %.4f・正統性の中央 %.4f" % (n, st.median(per[n][0]), st.median(per[n][1])))
