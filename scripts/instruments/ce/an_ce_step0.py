#!/usr/bin/env python
# (349) 消費の入口 段0：なぜ大衆の所得が籠に届かないか＝階級ごとの1人あたりの量（月・名目）で並べる読み手。
# 入力＝標準の計器 ce_s<seed>.tsv（C 行）。国全体の行は植民地の州（英は印度の農民 約3,900万人）を含む＝本国だけで読むには、計器を出す走行に
#   環境変数 POB_CE_REGIONS=ロンドン,マンチェスター,... を立てる（Instruments.EntryBegin が挙げた州以外を null にする＝読むだけ・力学は変わらない）。
# 使い方：PYTHONUTF8=1 python an_ce_step0.py <計器の出力フォルダ> <国名> [種=1]
# 列：Y/cap＝前の月の所得（正の分）÷人口、food_sup／daily_sup／lux_sup＝供給後の籠（欲しい量×前でなく今の月の配給率×買い手の値段）÷人口、
#     foodWant／dailyWant＝欲しい量×買い手の値段（供給で切る前・予算で切る前）÷人口、paid＝消費の財の支払い÷人口、held＝在庫切れの食料を予算から外した額÷人口、
#     sav＝月初の貯蓄÷人口。窓＝四半期の番号（Q5–12・Q13–40・Q5–40・Q41–100）。
import csv, sys, collections

d = sys.argv[1]
nat = sys.argv[2]
seed = sys.argv[3] if len(sys.argv) > 3 else "1"
rd = csv.reader(open(f"{d}/ce_s{seed}.tsv", encoding="utf-8"), delimiter="\t")
hdr = next(rd)
ix = {h: i for i, h in enumerate(hdr)}
acc = collections.defaultdict(lambda: collections.defaultdict(float))
wins = [(5, 12), (13, 40), (5, 40), (41, 100)]
for r in rd:
    if r[0] != "C" or r[ix["nation"]] != nat:
        continue
    q = int(r[ix["quarter"]])
    cl = r[ix["className"]]
    for lo, hi in wins:
        if lo <= q <= hi:
            a = acc[(cl, (lo, hi))]
            a["n"] += 1
            for h in hdr[12:]:
                try:
                    a[h] += float(r[ix[h]])
                except ValueError:
                    pass
mass = ["農民", "工場労働者", "職人", "使用人"]
for w in wins:
    print(f"== 窓 Q{w[0]}–Q{w[1]}（{nat}）")
    for cl in mass + ["貴族", "資本家", "商人"]:
        a = acc.get((cl, w))
        if not a:
            continue
        n = a["n"]
        pop = a["popK"] / n
        if pop <= 0:
            continue

        def g(k):
            return a[k] / n / pop

        Y = g("earnedLastPositivePerMonth")
        basket = g("foodSuppliedWantPerMonth") + g("dailySuppliedWantPerMonth")
        print(f"{cl:6} popK={pop:9.1f} Y/cap={Y:6.3f} food_sup={g('foodSuppliedWantPerMonth'):6.3f} daily_sup={g('dailySuppliedWantPerMonth'):6.3f} "
              f"lux_sup={g('luxurySuppliedWantPerMonth'):6.3f} foodWant={g('foodWantPerMonth'):6.3f} dailyWant={g('dailyWantPerMonth'):6.3f} "
              f"paid={g('consumptionPaidPerMonth'):6.3f} held={g('foodUnfilledHeldPerMonth'):6.3f} sav={g('savingsStartStock'):8.2f} "
              f"籠/Y={basket / Y if Y > 0 else float('nan'):6.2f} 日用品/食料={g('dailySuppliedWantPerMonth') / g('foodSuppliedWantPerMonth') if g('foodSuppliedWantPerMonth') > 0 else float('nan'):5.2f}")
