#!/usr/bin/env python
"""(439) 線 gs の読み手：金本位の願いの形（旗 GoldWishFromNetwork）の事前登録 [6] の狙いの数字を計器の生から読む。

使い方：PYTHONUTF8=1 python an_gold.py <計器の出力フォルダ（ins）> [seeds=10] [--report=<成績表のファイル>]
読む表：lawgate_s<N>.tsv（INS=1・末尾に足した列を含む）と metal_s<N>.tsv（nGoldLaw）。
出す物（事前登録 gs_20261019_prereg.txt の節に対応）：
  G1 1913 年に金の法の小国 3（ブハラ・ヒヴァ・アラビア）の種数／G2 露・墺・日・英・米・普・オスマンの 1913 年の金の法の種数と、1853 年・1913 年の英
  G3 非継承の金の法への移行について (a) 金の法でない期に願いが立った移行のうち、その期に gain>0 だった割合（★配線の確認＝述語が願いを駆動するので同語反復・弁別の線ではない・追記2）
     (b) gain>0 が 8 連続（四半期）以上に達した（金の法でない）国×種の全てのうち、その後 12 四半期以内に金の法になった割合（金にならなかった国も分母に入れる・毎四半期の行がある国だけ）
     (c) 願いの立った年と gain>0 が最初に立った年の順位相関（全種プール）
  G5 nGoldLaw の年ごとの軌跡と増分／G6 強制離脱の総回数（--report=<成績表> から読む）／G8 非金の法の国×種の願いの反転回数の 90 パーセンタイル（毎四半期の行がある国だけ）
  最後に G1〜G8 の線ごとの合否ブロックを出す（事前登録 [6] と追記2 の線）。
⚠限界（計器が自己申告する）：行は国×四半期（挙げた国〔INS の NATIONS〕だけ毎四半期）・他の国は年末の 1 行。年末だけの国は連続長を 4 倍して読む（四半期の連続を測れない）ので G3(b) は挙げた国だけで数える。
partnerTilt は四半期の末の値（直前の清算）・願いは頭の値＝同じ期に並べても 1 期ずれうる。
"""
import sys, glob, os, re, collections, statistics as st

sys.stdout.reconfigure(encoding="utf-8")

SMALL = ["ブハラ", "ヒヴァ", "アラビア"]
WATCH = ["ロシア", "オーストリア", "日本", "イギリス", "アメリカ", "プロイセン", "オスマン", "フランス", "清"]


def load(path):
    rows = open(path, encoding="utf-8").read().split("\n")
    h = rows[0].split("\t")
    ix = {k: i for i, k in enumerate(h)}
    out = []
    for l in rows[1:]:
        c = l.split("\t")
        if len(c) < len(h):
            continue
        out.append(c)
    return ix, out


def fnum(x):
    try:
        return float(x)
    except ValueError:
        return float("nan")


def spearman(a, b):
    def rank(v):
        order = sorted(range(len(v)), key=lambda i: v[i])
        r = [0.0] * len(v)
        i = 0
        while i < len(v):
            j = i
            while j + 1 < len(v) and v[order[j + 1]] == v[order[i]]:
                j += 1
            for k in range(i, j + 1):
                r[order[k]] = (i + j) / 2.0 + 1
            i = j + 1
        return r

    if len(a) < 3:
        return float("nan")
    ra, rb = rank(a), rank(b)
    ma, mb = sum(ra) / len(ra), sum(rb) / len(rb)
    num = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    den = (sum((x - ma) ** 2 for x in ra) * sum((y - mb) ** 2 for y in rb)) ** 0.5
    return num / den if den else float("nan")


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    report = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--report=")), None)
    d = args[0]
    nseed = int(args[1]) if len(args) > 1 else 10
    gold1913 = collections.defaultdict(int)
    gold1853 = collections.defaultdict(int)
    xs, ys = [], []          # (c) 順位相関
    a_ok = a_wish = 0         # (a) 願いが立った移行（金の法でない期）と、そのうち gain>0 だった数
    b_ok = b_all = 0          # (b)
    toggles = []              # G8（毎四半期の行がある国だけ）
    nonherited = 0
    nowish = 0                # 金の法になる前に金の法でない期の願いが 1 度も無かった移行（同じ期に施行・年末の行だけ ほか）
    nowish_list = []
    debt_gov = collections.defaultdict(list)    # G4 の理由（墺・日の 1905〜1913 年の債務比）
    debt_for = collections.defaultdict(list)
    for s in range(1, nseed + 1):
        path = os.path.join(d, f"lawgate_s{s}.tsv")
        if not os.path.exists(path):
            print(f"!! {path} が無い"); continue
        ix, rows = load(path)
        if "gain" not in ix:
            print("!! 新しい列（gain）が無い＝この木の計器ではない"); sys.exit(1)
        series = collections.defaultdict(list)
        for c in rows:
            series[c[ix["nation"]]].append((int(c[ix["quarter"]]), int(c[ix["year"]]), int(c[ix["goldLawCode"]]),
                                            int(c[ix["goldWishUsed"]]), fnum(c[ix["gain"]]), fnum(c[ix["partnerTilt"]]),
                                            fnum(c[ix["foreignDebtToGdpYear"]]), fnum(c[ix["govDebtToGdpYear"]])))
        for nm, ser in series.items():
            ser.sort()
            for y, store in ((1913, gold1913), (1853, gold1853)):
                last = [r for r in ser if r[1] == y]
                if last and last[-1][2] == 2:
                    for b in SMALL + WATCH:
                        if b in nm:
                            store[b] += 1
            quarterly = len(ser) > 200
            for b in ("オーストリア", "日本"):
                if b in nm and quarterly:
                    late = [r for r in ser if 1905 <= r[1] <= 1913]
                    if late:
                        debt_gov[b].append(st.median(r[7] for r in late))
                        debt_for[b].append(st.median(r[6] for r in late))
            # 反転回数（非金の法の期間だけ・毎四半期の行がある国だけ）
            if quarterly:
                tg = 0; prev = None
                for r in ser:
                    if r[2] == 2:
                        prev = None; continue
                    if prev is not None and r[3] != prev:
                        tg += 1
                    prev = r[3]
                toggles.append(tg)
            # 非継承の金の法への移行：最初に金の法になった期（その国の系列の最初が金の法でない）
            first_gold = next((r for r in ser if r[2] == 2), None)
            if first_gold is not None and ser[0][2] != 2:
                nonherited += 1
                wish_q = None
                for r in ser:
                    if r[0] >= first_gold[0]:
                        break
                    if r[2] != 2 and r[3] == 1:      # 金の法の行は前項で必ず goldWishUsed=1 になる＝除く（追記2）
                        wish_q = r
                        break
                if wish_q is not None:
                    a_wish += 1
                    if wish_q[4] > 0:
                        a_ok += 1
                    gain_first = next((r for r in ser if r[4] > 0), None)
                    if gain_first is not None:
                        xs.append(wish_q[1]); ys.append(gain_first[1])
                else:
                    nowish += 1
                    nowish_list.append((s, nm, first_gold[1], first_gold[0], quarterly))
            if quarterly:
                # (b) 金の法でないまま gain>0 が 8 連続に達した国×種（金にならなかった国も分母）→ 12 四半期以内に金の法になったか
                run = 0
                for i, r in enumerate(ser):
                    run = run + 1 if r[4] > 0 else 0
                    if run == 8 and r[2] != 2:
                        b_all += 1
                        if any(x[2] == 2 for x in ser[i:i + 13]):
                            b_ok += 1
                        break
    print("== G1 小国 3 の 1913 年の金の法の種数（表にある国名を含むもの・成り代わりを含む）")
    for b in SMALL:
        print(f"  {b}: {gold1913[b]}/{nseed}（1853 年 {gold1853[b]}）")
    print("== G2/G7 主要国の金の法の種数")
    for b in WATCH:
        print(f"  {b}: 1913 年 {gold1913[b]}/{nseed}・1853 年 {gold1853[b]}/{nseed}")
    print("== G3 順に従う（非継承の移行 {} 件）".format(nonherited))
    print(f"  (a) ★配線の確認（同語反復・弁別の線ではない）金の法でない期に願いが立った移行 {a_wish}/{nonherited} のうち、その期に gain>0：{a_ok}/{a_wish}"
          + (f" = {a_ok / a_wish:.0%}" if a_wish else "") + f"・金の法でない期の願いが無いまま金になった移行 {nowish} 件")
    for (s_, nm, y_, q_, qly) in nowish_list[:20]:
        print(f"      願い無しで金：種 {s_} {nm} {y_} 年（四半期 {q_}）{'毎四半期の行あり' if qly else '年末の行だけ＝願いの行を見落とした可能性'}")
    print(f"  (b) gain>0 が 8 連続以上（金の法でない）→12 四半期以内に金の法：{b_ok}/{b_all}" + (f" = {b_ok / b_all:.0%}" if b_all else "")
          + "（分母＝金にならなかった国も含む・毎四半期の行がある国だけ）")
    rho = spearman(xs, ys) if len(xs) >= 3 else float("nan")
    print(f"  (c) 願いの年と gain>0 の最初の年の順位相関 n={len(xs)} ρ={rho:.2f}（n≧15・ρ≧0.5 が線）")
    p90 = None
    if toggles:
        t = sorted(toggles)
        p90 = t[int(0.9 * (len(t) - 1))]
        print(f"== G8 非金の法の毎四半期の行がある国×種の願いの反転回数：中央 {st.median(t):.0f}・90 パーセンタイル {p90}（線 ≦10）・最大 {t[-1]}・n={len(t)}")
    # G5 nGoldLaw
    yrs = [1793, 1803, 1823, 1833, 1843, 1853, 1873, 1893, 1913, 1933]
    acc = {y: [] for y in yrs}
    for s in range(1, nseed + 1):
        p = os.path.join(d, f"metal_s{s}.tsv")
        if not os.path.exists(p):
            continue
        ix, rows = load(p)
        for c in rows:
            y = int(c[ix["year"]])
            if y in acc:
                acc[y].append(int(c[ix["nGoldLaw"]]))
    nan = float("nan")
    mean = {y: (sum(acc[y]) / len(acc[y]) if acc[y] else nan) for y in yrs}
    print("== G5 nGoldLaw（全国・10 種×4 四半期の平均・m436: 2.5/2.4/3.8/6.5/14.3/21.0/25.7/34.2/41.0/42.6）")
    print("  " + " ".join(f"{y}:{mean[y]:.1f}" for y in yrs if acc[y]))
    inc = mean[1893] - mean[1853]
    print(f"  1853→1893 の増分 {inc:+.1f}（線 ≧+5・m436 +13.2）")
    # G6
    forced = None
    if report and os.path.exists(report):
        for l in open(report, encoding="utf-8"):
            if l.startswith("金本位の強制離脱"):
                m = re.search(r"総(\d+)回", l)
                if m:
                    forced = int(m.group(1))
                print("== G6 " + l.strip())
    print("== G4（記録だけ）墺・日の債務（1905〜1913 年の中央値の、種をまたぐ中央）")
    for b in ("オーストリア", "日本"):
        if debt_gov[b]:
            print(f"  {b}: 国債/年GDP {st.median(debt_gov[b]):.2f}・対外債務/年GDP {st.median(debt_for[b]):.3f}（n={len(debt_gov[b])}）・1913 年の金の法 {gold1913[b]}/{nseed}")

    # 合否（事前登録 [6]・追記2。G3(a) は配線の確認で合否に使わない）
    def ok(c):
        return "合格" if c else "不合格"

    res = []
    res.append(("G1 小国 3 が 1913 年に各 ≦3/10（自明に通る見込み・単独では弁別しない）", all(gold1913[b] <= 3 * nseed / 10 for b in SMALL)))
    res.append(("G2 露が 1913 年に ≧5/10", gold1913["ロシア"] >= 5 * nseed / 10))
    res.append((f"G3(b) 8 連続→12 四半期以内に金の法 ≧70%（分母 {b_all}）", b_all > 0 and b_ok / b_all >= 0.7))
    res.append((f"G3(c) 順位相関 ρ≧0.5・n≧15（n={len(xs)}）", len(xs) >= 15 and rho >= 0.5))
    res.append(("G5 1793≦6・1803≦8・1833≦12・増分≧+5・1913∈[12,45]",
                mean[1793] <= 6 and mean[1803] <= 8 and mean[1833] <= 12 and inc >= 5 and 12 <= mean[1913] <= 45))
    res.append((f"G6 強制離脱 ≦300（{forced}）", forced is not None and forced <= 300))
    res.append(("G7 英 1853 ≧8/10・1913 ≧9/10", gold1853["イギリス"] >= 8 * nseed / 10 and gold1913["イギリス"] >= 9 * nseed / 10))
    res.append((f"G8 反転 p90 ≦10（{p90}）", p90 is not None and p90 <= 10))
    print("== 合否（G3(a) は配線の確認で合否に使わない・G4 は記録のみ）")
    for name, c in res:
        print(f"  {ok(c)}  {name}")
    bad = "・".join(n.split(" ")[0] for n, c in res if not c)
    print("  総合：" + ("〈機構が狙いどおり働いた〉" if not bad else "〈狙いの数字に届かない＝採らない〉（外れ：" + bad + "）"))


if __name__ == "__main__":
    main()
