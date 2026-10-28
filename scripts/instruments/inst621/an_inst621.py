# -*- coding: utf-8 -*-
"""(621) 線 inst の読み手：制度と思想の行の解禁（disc_s の E 行）と本国の識字（lawgate_s の literacyHome）を読む。
事前登録 measurements/inst621_20261028_prereg.txt の [3]（基線）と [4]・[8]（G1〜G4・記録 R1〜R3）。計器の新しい列は足していない。
使い方：python an_inst621.py <ins のフォルダ>（例 D:/PoB_work/keep_617/m617_ins）
数字はこの出力を事前登録の [3]（m617・N1・N2）と並べる。判定（G1〜G4）は腕 B の出力に対して読む（基線に対しては G1 と G4 は意味を持たない＝参考）。
追記 2（[8]）の読み：G1＝毎四半期の行がある 9 国だけ・解禁の四半期の前後 2 行の識字が同じ側のときだけ判定／G2・G3＝実際の分母と行ごとの開いた種の数を併記／
G4＝第 1・第 2 条件が判定（配線の確認）・第 3 条件（共産主義だけ）は記録だけ／識字の行が無い件（None）の数を出す。
"""
import csv, glob, re, sys, collections, statistics as st

sys.stdout.reconfigure(encoding="utf-8")
FULL = 0.5                      # 基準（事前登録 [0]①・結果の後に付け替えない）
LOW = 0.9 * FULL                # 識字が低い国の線（G2・G3）
ROWS = ["株式会社", "社会主義", "共産主義", "帝国主義", "ファシズム"]   # d>0 の行（特許は旗の対象外）
MAJ = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "日本", "清", "オスマン"]
PASS = []


def seed_of(path):
    return int(re.search(r"s(\d+)\.tsv$", path).group(1))


def load(d):
    lit = {}
    for f in glob.glob(d + "/lawgate_s*.tsv"):
        s = seed_of(f)
        for r in csv.DictReader(open(f, encoding="utf-8"), delimiter="\t"):
            lit[(s, r["nation"], int(r["quarter"]))] = float(r["literacyHome"])
    ev = collections.defaultdict(list)   # (seed,row) -> [(quarter, nation, tech, need, trans)]
    allev = []
    for f in glob.glob(d + "/disc_s*.tsv"):
        s = seed_of(f)
        for r in csv.DictReader(open(f, encoding="utf-8"), delimiter="\t"):
            if r["kind"] != "E" or r["row"] not in ROWS + ["特許"]:
                continue
            e = (int(r["quarter"]), r["nation"], float(r["techTerm"]), float(r["needTerm"]), float(r["transmitTerm"]), r["row"], s)
            allev.append(e)
            if r["row"] in ROWS:
                ev[(s, r["row"])].append(e[:5])
    return lit, ev, allev


def lit_at(lit, s, n, q):
    for k in range(0, 9):          # 同じ四半期、無ければ 8 四半期以内で最も近い前の行
        v = lit.get((s, n, q - k))
        if v is not None:
            return v
    return None


def verdict(name, ok, note=""):
    PASS.append((name, ok))
    print("判定 %s：%s %s" % (name, "通る" if ok else "外れ", note))


def main(d):
    lit, ev, allev = load(d)
    print("== G1 エンジンが識字を読む（techTerm と 70×min(1, 識字÷%.2f)）" % FULL)
    print("   判定の対象＝毎四半期の行がある 9 国・解禁の四半期の前の行と同じ四半期の行（Step は四半期の途中で識字を読む）が同じ側のときだけ")
    hi = hi_ok = lo = lo_ok = mid = skew = pat = pat_ok = 0
    ohi = ohi_ok = olo = olo_ok = none_n = 0
    for q, n, t, need, tr, row, s in allev:
        if row == "特許":
            pat += 1
            pat_ok += abs(t - 70) < 1e-9
            continue
        if n in MAJ:
            a, b = lit.get((s, n, q - 1)), lit.get((s, n, q))
            if a is None or b is None:
                none_n += 1
                continue
            if min(a, b) >= FULL * 1.05:
                hi += 1
                hi_ok += abs(t - 70) < 1e-9
            elif max(a, b) <= FULL * 0.9:
                lo += 1
                lo_ok += t < 70 - 1e-9
            elif max(a, b) < FULL * 1.05 and min(a, b) > FULL * 0.9:
                mid += 1
            else:
                skew += 1
        else:
            x = lit_at(lit, s, n, q)
            if x is None:
                none_n += 1
            elif x >= FULL * 1.05:
                ohi += 1
                ohi_ok += abs(t - 70) < 1e-9
            elif x <= FULL * 0.9:
                olo += 1
                olo_ok += t < 70 - 1e-9
    print("判定の対象（9 国）：識字≧%.3f の件 %d 件中 techTerm=70 は %d／識字≦%.3f の件 %d 件中 techTerm<70 は %d／線の近く（±5〜10%%）%d 件／前後の行が線をまたぐ %d 件"
          % (FULL * 1.05, hi, hi_ok, FULL * 0.9, lo, lo_ok, mid, skew))
    print("記録（他の国・最大 8 四半期前の行で読む）：識字≧%.3f の件 %d 件中 techTerm=70 は %d／識字≦%.3f の件 %d 件中 techTerm<70 は %d／識字の行が無い件（None）%d"
          % (FULL * 1.05, ohi, ohi_ok, FULL * 0.9, olo, olo_ok, none_n))
    print("特許 %d 件中 techTerm=70 は %d" % (pat, pat_ok))
    verdict("G1", hi == hi_ok and lo == lo_ok and pat == pat_ok and (hi + lo) > 0)

    print("== G2 世界で最初に開いた国の本国識字が %.2f 未満の（種×行）" % LOW)
    low_first = tot = early = early_low = none2 = none3 = 0
    per_row = collections.Counter()
    first_nat = collections.defaultdict(collections.Counter)
    eff = eff_low = 0
    for (s, row), l in ev.items():
        l = sorted(l)
        q0, n0 = l[0][0], l[0][1]
        x0 = lit_at(lit, s, n0, q0)
        tot += 1
        per_row[row] += 1
        first_nat[row][n0] += 1
        if x0 is None:
            none2 += 1
        is_low = x0 is not None and x0 < LOW
        low_first += is_low
        if row in ("社会主義", "共産主義", "ファシズム"):
            eff += 1
            eff_low += is_low
        for q, n, *_ in l:
            if q <= q0 + 80:
                x = lit_at(lit, s, n, q)
                if x is None:
                    none3 += 1
                    continue
                early += 1
                early_low += (x < LOW)
    print("%d／%d（分母＝実際に開いた種×行の数。事前登録の 50 に対する欠け＝%d）・識字の行が無い件 %d" % (low_first, tot, 50 - tot, none2))
    print("行ごとに開いた種の数（10 種中）：" + "・".join("%s %d" % (r, per_row[r]) for r in ROWS))
    print("行ごとの世界で最初の国：" + "／".join("%s %s" % (r, dict(first_nat[r].most_common(3))) for r in ROWS))
    print("実効の分母（社会主義・共産主義・ファシズム。株式会社と帝国主義は最初が英＝作りの上で通る）：%d／%d" % (eff_low, eff))
    collapsed = tot < 40
    verdict("G2", low_first <= 5 and not collapsed,
            "（開いた種×行が %d。40 未満なら行がほとんど開かない劣化＝通っても機構が働いたとは読まない）" % tot)

    print("== G3 世界で最初の開きから 20 年以内の開き（国×行）のうち本国識字 %.2f 未満の割合" % LOW)
    frac = 100.0 * early_low / early if early else float("nan")
    print("%d／%d＝%.1f%%・識字の行が無い件（除外）%d" % (early_low, early, frac, none3))
    verdict("G3", early > 0 and frac <= 40.0 and not collapsed)

    print("== G4 辺：共産主義の四半期と社会主義の四半期（同じ種・同じ国）")
    qq = collections.defaultdict(dict)
    for (s, row), l in ev.items():
        if row in ("社会主義", "共産主義"):
            for q, n, *_ in l:
                qq[(s, n)][row] = q
    both = [v for v in qq.values() if len(v) == 2]
    same = sum(1 for v in both if v["共産主義"] == v["社会主義"])
    first = sum(1 for v in both if v["共産主義"] < v["社会主義"])
    after = sum(1 for v in both if v["共産主義"] > v["社会主義"])
    only_c = sum(1 for v in qq.values() if "共産主義" in v and "社会主義" not in v)
    only_s = sum(1 for v in qq.values() if "社会主義" in v and "共産主義" not in v)
    print("両方開いた %d／同じ四半期 %d／共産主義が先 %d／共産主義が後 %d／共産主義だけ %d（記録だけ）／社会主義だけ %d"
          % (len(both), same, first, after, only_c, only_s))
    gaps = sorted(v["共産主義"] - v["社会主義"] for v in both if v["共産主義"] > v["社会主義"])
    if gaps:
        print("共産主義が後の間隔（四半期）：最小 %d・中央 %s・最大 %d・1 四半期 %d 件・2〜4 %d 件・5 以上 %d 件"
              % (gaps[0], st.median(gaps), gaps[-1], sum(1 for k in gaps if k == 1), sum(1 for k in gaps if 2 <= k <= 4), sum(1 for k in gaps if k >= 5)))
    print("   第 1・第 2 条件（同じ四半期 0・共産主義が先 0）が判定＝Step は全行の点数を先に求めてから known を立てるので作りの上で通る配線の確認。第 3 条件は分離独立の子が親の Discovered を受け継ぐため記録だけ。")
    verdict("G4", same == 0 and first == 0 and len(both) > 0)

    print("== R1 世界で最初の開きの年（種の中央・全国）と国")
    for row in ROWS + ["特許"]:
        ys = []
        ns = collections.Counter()
        for s in range(1, 11):
            l = sorted(e for e in allev if e[5] == row and e[6] == s)
            if l:
                ys.append(1783 + l[0][0] / 4)
                ns[l[0][1]] += 1
        if ys:
            print("%s 最初の年（中央）%.1f 開いた種 %d 最初の国 %s" % (row, st.median(ys), len(ys), dict(ns.most_common(4))))
    print("== R2 主要 9 国：開いた国の数（種の中央）・最初の主要国と同じ四半期に開いた数（中央）・開く年の幅（中央・年）")
    for row in ROWS:
        nopen = []
        same_q = []
        rng = []
        for s in range(1, 11):
            dm = {n: q for q, n, *_ in ev.get((s, row), []) if n in MAJ}
            nopen.append(len(dm))
            if dm:
                m = min(dm.values())
                same_q.append(sum(1 for v in dm.values() if v == m))
                rng.append((max(dm.values()) - m) / 4)
        print("%s 開いた %s／同四半期 %s／幅 %.1f" % (row, st.median(nopen), st.median(same_q) if same_q else "-", st.median(rng) if rng else float("nan")))
    print("== 1800 年以前（四半期≦68）の開き（国×行・全種）と、そのうち本国識字 %.2f 未満" % FULL)
    for row in ROWS + ["特許"]:
        e0 = [e for e in allev if e[5] == row and e[0] <= 68]
        ll = [lit_at(lit, e[6], e[1], e[0]) for e in e0]
        print("%s %d 件・識字<%.2f %d 件" % (row, len(e0), FULL, sum(1 for x in ll if x is not None and x < FULL)))
    print("== R6 G3 の集合（最初の開きから 20 年以内）の項の平均（識字の二重の読みの分解の記録＝技術の項と伝わりの項と必要性の項）")
    buckets = {"識字<%.2f" % LOW: [], "識字>=%.2f" % LOW: []}
    for (s, row), l in ev.items():
        l = sorted(l)
        q0 = l[0][0]
        for q, n, t, need, tr in l:
            if q <= q0 + 80:
                x = lit_at(lit, s, n, q)
                if x is not None:
                    buckets["識字<%.2f" % LOW if x < LOW else "識字>=%.2f" % LOW].append((t, need, tr))
    for k, v in buckets.items():
        if v:
            print("%s %d 件：techTerm 平均 %.1f・needTerm 平均 %.1f・transmitTerm 平均 %.1f" % (k, len(v), st.mean(e[0] for e in v), st.mean(e[1] for e in v), st.mean(e[2] for e in v)))
    print("== 判定のまとめ：" + "・".join("%s %s" % (n, "通る" if ok else "外れ") for n, ok in PASS))


if __name__ == "__main__":
    main(sys.argv[1])
