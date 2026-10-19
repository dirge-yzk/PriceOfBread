#!/usr/bin/env python
"""(442) 合成後のアブレーション cl→dd の読み手（読むだけ・シミュを回さない）。

問い：清算の片方向の錠（cl・係数 ClearingIdleThrottledScan と ClearingIdlePotentialReweight）の点火が、機関車（`Discovery.cs` の必要性が LastTradePotential を読む）の最初の発見の時を動かしたか。
入力：各腕の計器の生の disc_s<seed>.tsv（kind=E の行＝解禁・列 quarter は 1783 年第 1 四半期を 0 とする通しの番号・year・nation・row）。
定義：(種, 行) ごとに E 行の quarter の最小＝最初の解禁の四半期・同じ四半期に開いた国が最初の発見者（複数なら全員）。
      対象の行＝制度・思想の行（特許・株式会社・社会主義・共産主義・帝国主義・ファシズム）と合成染料（旗の正負で E の有無が違う）を除く（dd422/judge.py と同じ）。
      行ごとに：種の中央値（開かない種は ∞ として数える）と、腕の対（候補−基準）の種ごとの差の中央値・早まった種／遅れた種／同じ種の数。
      雑音の目盛り＝基線とヌル腕（N1・N2）の同じ差（ヌル腕は SupplySlope を ±1e-8 動かすだけ＝同じ世界の別の軌道）。
使い方：PYTHONUTF8=1 python disc_first.py <名前=計器の生のフォルダ>... --base <基線の名前> [--rows 機関車,転炉鋼,汽船] [--seeds 10]
"""
import argparse, csv, collections, os, statistics, sys

SPREAD = {"特許", "株式会社", "社会主義", "共産主義", "帝国主義", "ファシズム"}
EXCLUDED = {"合成染料"}
INF = float("inf")


def rd(path):
    with open(path, encoding="utf-8", newline="") as fh:
        return list(csv.DictReader(fh, delimiter="\t"))


def first_table(root, nseeds):
    """(seed,row) -> (最初の quarter, [最初の発見者の国])"""
    out = {}
    for s in range(1, nseeds + 1):
        best = {}
        for r in rd(os.path.join(root, f"disc_s{s}.tsv")):
            if r["kind"] != "E" or r["row"] in SPREAD or r["row"] in EXCLUDED:
                continue
            q = int(r["quarter"])
            cur = best.get(r["row"])
            if cur is None or q < cur[0]:
                best[r["row"]] = (q, [r["nation"]])
            elif q == cur[0]:
                cur[1].append(r["nation"])
        for row, v in best.items():
            out[(s, row)] = v
    return out


def med(v):
    v = sorted(v)
    n = len(v)
    if n == 0:
        return INF
    if n % 2:
        return v[n // 2]
    a, b = v[n // 2 - 1], v[n // 2]
    return INF if INF in (a, b) else (a + b) / 2


def fmt(x):
    return "開かない" if x == INF else f"{x:g}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("arms", nargs="+", help="名前=フォルダ")
    ap.add_argument("--base", required=True)
    ap.add_argument("--rows", default="機関車,転炉鋼,汽船,蒸気機関,加硫ゴム")
    ap.add_argument("--seeds", type=int, default=10)
    ap.add_argument("--delta-min", type=float, default=2.0, help="全行の表に出す差の中央値の下限（四半期）")
    g = ap.parse_args()
    arms = {}
    for a in g.arms:
        name, folder = a.split("=", 1)
        arms[name] = first_table(folder, g.seeds)
    names = list(arms)
    rows = sorted({row for t in arms.values() for (_, row) in t})
    key = [r for r in g.rows.split(",") if r]
    print(f"基準の腕={g.base}　腕={names}　種={g.seeds}　単位＝最初の解禁の四半期（1783Q1＝0・1 年＝4）")
    print("\n== 行ごとの最初の解禁の四半期（種ごと・開かない＝∞）と中央値 ==")
    for row in key:
        print(f"[{row}]")
        for n in names:
            per = [arms[n].get((s, row), (INF, []))[0] for s in range(1, g.seeds + 1)]
            nat = collections.Counter(x for s in range(1, g.seeds + 1) for x in arms[n].get((s, row), (INF, []))[1])
            print(f"  {n:>6}: 中央値 {fmt(med(per)):>8}  種ごと {[fmt(x) for x in per]}  最初の国 {dict(nat.most_common(4))}")
    print("\n== 基準との種ごとの差（候補−基準・四半期・両方が開いた種だけ）==")
    base = arms[g.base]
    for row in key:
        print(f"[{row}]")
        for n in names:
            if n == g.base:
                continue
            d = [arms[n][(s, row)][0] - base[(s, row)][0] for s in range(1, g.seeds + 1) if (s, row) in arms[n] and (s, row) in base]
            if not d:
                print(f"  {n:>6}: 両方が開いた種が無い")
                continue
            print(f"  {n:>6}: 中央値 {statistics.median(d):+g}　早まった {sum(x < 0 for x in d)}・遅れた {sum(x > 0 for x in d)}・同じ {sum(x == 0 for x in d)}（{len(d)} 種）　差 {[f'{x:+g}' for x in d]}")
    print(f"\n== 全ての行：基準との差の中央値が ±{g.delta_min:g} 四半期以上の行（両方が開いた種が 5 以上）==")
    for n in names:
        if n == g.base:
            continue
        hit = []
        for row in rows:
            d = [arms[n][(s, row)][0] - base[(s, row)][0] for s in range(1, g.seeds + 1) if (s, row) in arms[n] and (s, row) in base]
            if len(d) >= 5 and abs(statistics.median(d)) >= g.delta_min:
                hit.append((row, statistics.median(d), len(d)))
        print(f"  {n:>6}: {len(hit)} 行　" + "　".join(f"{r}{m:+g}({k}種)" for r, m, k in hit))
    print("\n== 開かない行（その腕で 0 種）==")
    for n in names:
        closed = [r for r in rows if not any((s, r) in arms[n] for s in range(1, g.seeds + 1))]
        print(f"  {n:>6}: {len(closed)} 行　{closed}")


if __name__ == "__main__":
    main()
