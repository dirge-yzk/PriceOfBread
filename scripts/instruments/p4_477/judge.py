"""(477) 線 p4 の読み手（読むだけ）：事前登録 measurements/p4_20261021_prereg.txt（追記 2 の版）の T1〜T5・M1〜M4 を計器の生から出す。
使い方：PYTHONUTF8=1 python judge.py <腕 B の ins フォルダ> [--base <基線の ins＝D:/PoB_work/keep_475/m475_ins>] [--a <腕 A（旗 0・INS=1・5 種）の ins>] [--seeds 10] [--catalog <Catalog.cs>]
入力：disc_s<seed>.tsv（kind=E の行＝国が行を解禁した四半期・全ての国）・engineer_s<seed>.tsv（kind=T・行ごとの 8 四半期ごとの断面〈replaced〉＝代替する物の値段）・
      procrow_s<seed>.tsv（挙げた国だけ・腕 B と腕 A にしか無い）。
採否の門（追記 2）：T1・T5・M1・M2。記録（採否に使わない）：T2・T3・T4・M3・M4・電解と労働の行。
数え方：T1・T5 は disc_s の E 行が出た全ての国で数える（基線も同じ集合）。M1 は procrow_s の挙げた国（NATIONS）。M2 は engineer_s の国のうち、腕 B で ソルベー法 が開いた（種，国）。
⚠この読み手は限界を自己申告する：①第一発見の年は行の解禁の四半期の年（E 行の year）②engineer_s の断面は 8 四半期ごと（窓の中は 2〜5 点）③M2 の基線との比は同じ種・同じ国・同じ暦の四半期の窓で取る
（腕 B は ソルベー法 が開いた後で軌道が分かれる＝差の差の読みで、軌道の分岐の雑音を含む）④procrow_s の goodPriceReal と engineer_s の replaced は量の定義が違う＝混ぜない（M2 は engineer_s・M4 は procrow_s）
⑤ソルベー法が計器の国で 1 件も開かない・観測が足りない場合は〈読めない〉と出す（外れとは別・採否では未達と同じ扱い）。
"""
import argparse, collections, csv, os, re, statistics, sys

JUDGED = ["熱風炉", "ソルベー法"]          # T1 の判定の行
RECORD_ROWS = ["電解", "三段膨張", "自動ミュール"]   # 記録の行（T5 の数えから外す）
NEW5 = JUDGED + RECORD_ROWS
HIST = {"熱風炉": 1828, "ソルベー法": 1864, "電解": 1892}   # 史実の年（T2 は記録）
PRE = (-20, 0)                      # M2・M4 の前の窓（開いた四半期からの相対）
POST_GATE = (60, 100)               # M2 の門の窓（追記 2：普及の傾きを待つ）
POST_REC = [(20, 40), (100, 140)]   # 記録の窓
M1_WINDOW = 120                     # M1 の門の窓
M1_MIN_OBS = 10                     # M1 が読める最小の件数


def catalog_info(path):
    src = open(path, encoding="utf-8-sig").read()
    i = src.index("public static readonly DiscoveryRow[] Discoveries")
    j = src.index("];", i)
    out = []
    for m in re.finditer(r'new\("([^"]+)",\s*\w+,\s*"[^"]*",\s*DiscoveryField\.\w+,\s*(\d+)', src[i:j]):
        out.append((m.group(1), int(m.group(2))))
    names = re.findall(r'new\("([^"]+)"', src[i:j])
    return names, dict(out)


def tsv(path):
    with open(path, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            yield r


def fnum(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float("nan")


def read_events(d, seeds):
    """seed -> row -> {nation: (quarter, year)}（その国が行を解禁した最初の四半期）"""
    res = {}
    for s in range(1, seeds + 1):
        p = os.path.join(d, f"disc_s{s}.tsv")
        if not os.path.exists(p):
            continue
        ev = collections.defaultdict(dict)
        for r in tsv(p):
            if r["kind"] != "E":
                continue
            q = int(r["quarter"])
            cur = ev[r["row"]].get(r["nation"])
            if cur is None or q < cur[0]:
                ev[r["row"]][r["nation"]] = (q, int(r["year"]))
        res[s] = ev
    return res


def first_of(ev_seed, row):
    """その種で、行を最も早く解禁した国（四半期，年，国）"""
    d = ev_seed.get(row)
    if not d:
        return None
    nat = min(d, key=lambda n: d[n])
    return (d[nat][0], d[nat][1], nat)


def read_repl(d, seeds, row):
    """engineer_s の T 行：(seed, nation) -> [(quarter, replaced)]"""
    res = collections.defaultdict(list)
    for s in range(1, seeds + 1):
        p = os.path.join(d, f"engineer_s{s}.tsv")
        if not os.path.exists(p):
            continue
        for r in tsv(p):
            if r["kind"] == "T" and r["row"] == row:
                v = fnum(r["replaced"])
                if v == v and v > 0:
                    res[(s, r["nation"])].append((int(r["quarter"]), v))
    return res


def read_proc(d, seeds):
    """(seed, nation, row) -> [(quarter, known, share, priceReal)]（四半期順）"""
    res = collections.defaultdict(list)
    for s in range(1, seeds + 1):
        p = os.path.join(d, f"procrow_s{s}.tsv")
        if not os.path.exists(p):
            continue
        for r in tsv(p):
            res[(s, r["nation"], r["row"])].append((int(r["quarter"]), int(r["known"]), fnum(r["recipeOutputShare"]), fnum(r["goodPriceReal"])))
    for v in res.values():
        v.sort()
    return res


def wmean(series, t0, lo, hi):
    xs = [v for q, v in series if t0 + lo <= q < t0 + hi and v == v and v > 0]
    return statistics.mean(xs) if xs else float("nan")


def ratio_of(series, t0, post):
    pre = wmean(series, t0, *PRE); po = wmean(series, t0, *post)
    return po / pre if pre == pre and po == po and pre > 0 else float("nan")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("arm"); ap.add_argument("--base", default="D:/PoB_work/keep_475/m475_ins"); ap.add_argument("--a", default=None)
    ap.add_argument("--seeds", type=int, default=10)
    ap.add_argument("--catalog", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "PriceOfBread.SimCore", "Catalog.cs"))
    a = ap.parse_args()
    names, decade = catalog_info(a.catalog)
    arm = read_events(a.arm, a.seeds); base = read_events(a.base, a.seeds)
    print(f"# 腕 {a.arm}（{len(arm)} 種）／基線 {a.base}（{len(base)} 種）／行 {len(names)} 本／腕 A {a.a}")
    verdict = {}

    def n_open(ev, row): return sum(1 for s in ev if row in ev[s])

    print("\n[T1 入口（門）] 判定の行が 10 種の 8 種以上で、全ての国のどこかで開く")
    ok = True
    for r in JUDGED:
        n = n_open(arm, r); g = n >= 8; ok &= g
        print(f"  {r}: {n}/{len(arm)} {'OK' if g else '未達'}")
    verdict["T1"] = ok
    print("  （記録）電解・労働の行：" + "・".join(f"{r} {n_open(arm, r)}/{len(arm)}" for r in RECORD_ROWS))

    print("\n[T2 時期（記録）] 種ごとの最初の解禁の年の中央値と、基線の同じ年代の既存の行（新しい 5 本を除く）の最初の年の中央値")
    for r in NEW5:
        ys = [first_of(ev, r)[1] for ev in arm.values() if first_of(ev, r)]
        peers = [n for n in names if n not in NEW5 and decade.get(n) == decade.get(r)]
        py = {}
        for pn in peers:
            v = [first_of(ev, pn)[1] for ev in base.values() if first_of(ev, pn)]
            if v: py[pn] = statistics.median(v)
        pm = f"同年代 {decade.get(r)} の既存の行 {len(py)} 本の中央値の範囲 {int(min(py.values()))}〜{int(max(py.values()))}" if py else "同年代の既存の行なし"
        if ys: print(f"  {r}: 中央値 {statistics.median(ys)}（最小 {min(ys)}・最大 {max(ys)}・史実 {HIST.get(r, '—')}）／{pm}")
        else: print(f"  {r}: 開かない／{pm}")

    print("\n[T3 順序（記録）] ソルベー法が電解より前の種")
    both = [s for s, ev in arm.items() if first_of(ev, "ソルベー法") and first_of(ev, "電解")]
    n3 = sum(1 for s in both if first_of(arm[s], "ソルベー法")[:2] < first_of(arm[s], "電解")[:2])
    print(f"  両方開いた種 {len(both)}・うちソルベー法が先 {n3}")

    print("\n[T4 最初の発見者（記録）]")
    for r in NEW5:
        c = collections.Counter(first_of(ev, r)[2] for ev in arm.values() if first_of(ev, r))
        print(f"  {r}: {dict(c.most_common(5))}")

    print("\n[T5 どの種でも開かない行（門）] 電解と労働の行を除き、腕の本数 ≦ 基線の本数")
    def never(ev, excl):
        return [n for n in names if n not in excl and not any(n in e for e in ev.values())]
    na = never(arm, RECORD_ROWS); nb = never(base, NEW5)
    verdict["T5"] = len(na) <= len(nb)
    print(f"  腕 {len(na)} 本 {na}\n  基線 {len(nb)} 本 {nb}\n  {'OK' if verdict['T5'] else '未達'}")
    print(f"  （記録）電解・労働の行のうち、どの種でも開かない行：{[r for r in RECORD_ROWS if n_open(arm, r) == 0]}")

    # M1：procrow_s
    pr = read_proc(a.arm, a.seeds)
    print(f"\n[M1 ソルベー法の普及（門）] 開いた四半期の {M1_WINDOW} 四半期後に、化学薬品の産出のうちソルベー法のレシピの割合 ≧0.5 が、観測できた（種，国）の過半（観測 ≧{M1_MIN_OBS} 件）")
    if not pr:
        print("  (procrow_s が無い＝読めない)"); verdict["M1"] = False
    else:
        inst = []
        for (s, nat, row), ser in pr.items():
            if row != "ソルベー法": continue
            t0 = next((q for q, k, sh, p in ser if k == 1), None)
            if t0 is None: continue
            at = {q - t0: sh for q, k, sh, p in ser}
            first50 = next((q - t0 for q, k, sh, p in ser if sh == sh and sh >= 0.5 and q >= t0), None)
            inst.append((s, nat, t0, at.get(40, float("nan")), at.get(80, float("nan")), at.get(M1_WINDOW, float("nan")), first50))
        obs = [x for x in inst if x[5] == x[5]]
        hit = sum(1 for x in obs if x[5] >= 0.5)
        verdict["M1"] = len(obs) >= M1_MIN_OBS and hit * 2 > len(obs)
        print(f"  開いた（種，国）{len(inst)}・{M1_WINDOW} 四半期後が観測できた {len(obs)}・うち ≧0.5 が {hit}  →  {'OK' if verdict['M1'] else ('読めない' if len(obs) < M1_MIN_OBS else '未達')}")
        for lab, k in (("+40", 3), ("+80", 4)):
            xs = [x[k] for x in inst if x[k] == x[k]]
            print(f"  （記録）{lab} 四半期後の割合：観測 {len(xs)}・中央値 {statistics.median(xs) if xs else float('nan'):.3f}・≧0.5 が {sum(1 for v in xs if v >= 0.5)}")
        f50 = [x[6] for x in inst if x[6] is not None]
        print(f"  （記録）割合が初めて 0.5 に届くまでの四半期：届いた {len(f50)}/{len(inst)}・中央値 {statistics.median(f50) if f50 else float('nan')}")

    # M2：engineer_s の replaced（黄鉄鉱法の行＝化学薬品の値段）を基線と比べる
    print(f"\n[M2 化学薬品の値段（門）] ソルベー法が開いた（種，国）で、開く前 {-PRE[0]} 四半期に対する開いた後 {POST_GATE[0]}〜{POST_GATE[1]} 四半期の比を、基線の同じ（種，国）・同じ暦の窓の比で割る（差の差）。種ごとの国の中央値 ≦1.0 の種が 6/10 以上")
    rb = read_repl(a.arm, a.seeds, "黄鉄鉱法"); rn = read_repl(a.base, a.seeds, "黄鉄鉱法")
    def m2(post):
        by_seed = collections.defaultdict(list); raw = []
        for s, ev in arm.items():
            for nat, (q0, y0) in ev.get("ソルベー法", {}).items():
                sb, sn = rb.get((s, nat)), rn.get((s, nat))
                if not sb or not sn: continue
                r_b, r_n = ratio_of(sb, q0, post), ratio_of(sn, q0, post)
                if r_b == r_b and r_n == r_n and r_n > 0:
                    by_seed[s].append(r_b / r_n); raw.append((r_b, r_n))
        return {s: statistics.median(v) for s, v in by_seed.items()}, raw
    meds, raw = m2(POST_GATE)
    okn = sum(1 for m in meds.values() if m <= 1.0)
    verdict["M2"] = okn >= 6
    print(f"  読めた種 {len(meds)}・≦1.0 の種 {okn}  →  {'OK' if verdict['M2'] else ('読めない' if len(meds) < 6 else '未達')}")
    print("  種ごとの比の中央値（腕 B÷基線）：" + str({s: round(m, 3) for s, m in sorted(meds.items())}))
    if raw:
        print(f"  （記録）腕 B の窓の比（開く前に対する）の中央値 {statistics.median(x[0] for x in raw):.3f}／基線の同じ窓の比の中央値 {statistics.median(x[1] for x in raw):.3f}／件数 {len(raw)}")
    for post in POST_REC:
        m, _ = m2(post)
        print(f"  （記録）窓 {post[0]}〜{post[1]}：読めた種 {len(m)}・≦1.0 の種 {sum(1 for v in m.values() if v <= 1.0)}")

    # M3：合成染料（記録）
    print("\n[M3 合成染料の最初の解禁の年（記録）]")
    def med_first(ev, row):
        ys = [first_of(e, row)[1] for e in ev.values() if first_of(e, row)]
        return (statistics.median(ys), len(ys)) if ys else (None, 0)
    print(f"  腕 B {med_first(arm, '合成染料')}／基線 {med_first(base, '合成染料')}（中央値・開いた種の数）")

    # M4：熱風炉・鉄（記録）＝腕 A の procrow_s を零に
    print("\n[M4 鉄の実質の値段（記録）] procrow_s の goodPriceReal（腕 B÷腕 A・同じ窓）")
    if a.a:
        pa = read_proc(a.a, a.seeds)
        by_seed = collections.defaultdict(list)
        for (s, nat, row), ser in pr.items():
            if row != "熱風炉": continue
            t0 = next((q for q, k, sh, p in ser if k == 1), None)
            sa = pa.get((s, nat, row))
            if t0 is None or not sa: continue
            r_b = ratio_of([(q, p) for q, k, sh, p in ser], t0, POST_GATE); r_a = ratio_of([(q, p) for q, k, sh, p in sa], t0, POST_GATE)
            if r_b == r_b and r_a == r_a and r_a > 0: by_seed[s].append(r_b / r_a)
        meds4 = {s: statistics.median(v) for s, v in by_seed.items()}
        print(f"  読めた種 {len(meds4)}・≦1.0 の種 {sum(1 for v in meds4.values() if v <= 1.0)}  " + str({s: round(m, 3) for s, m in sorted(meds4.items())}))
    else:
        print("  (腕 A の ins が無い＝読めない)")

    print("\n[判定の要約（門）] " + "・".join(f"{k} {'OK' if v else '未達/読めない'}" for k, v in verdict.items())
          + "  →  " + ("門は全て通った（止める線は別に読む）" if all(verdict.values()) else "門に未達あり＝採らない"))


if __name__ == "__main__":
    main()
