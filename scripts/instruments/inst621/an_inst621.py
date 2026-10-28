# -*- coding: utf-8 -*-
"""(621) 線 inst の読み手：制度と思想の行の解禁（disc_s の E 行）と本国の識字（lawgate_s の literacyHome）を読む。
事前登録 measurements/inst621_20261028_prereg.txt の [3]（基線）と [4]（G1〜G4・記録 R1〜R3）。計器の新しい列は足していない。
使い方：python an_inst621.py <ins のフォルダ>（例 D:/PoB_work/keep_617/m617_ins）
数字はこの出力を事前登録の [3]（m617・N1・N2）と並べる。判定（G1〜G4）は腕 B の出力に対して読む（基線に対しては G1 と G4 は意味を持たない＝参考）。
"""
import csv, glob, re, sys, collections, statistics as st

sys.stdout.reconfigure(encoding="utf-8")
FULL = 0.5                      # 基準（事前登録 [0]①・結果の後に付け替えない）
LOW = 0.9 * FULL                # 識字が低い国の線（G2・G3）
ROWS = ["株式会社", "社会主義", "共産主義", "帝国主義", "ファシズム"]   # d>0 の行（特許は旗の対象外）
MAJ = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "日本", "清", "オスマン"]


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


def main(d):
    lit, ev, allev = load(d)
    print("== G1 エンジンが識字を読む（techTerm と 70×min(1, 識字÷%.2f)）" % FULL)
    hi = hi_ok = lo = lo_ok = mid = pat = pat_ok = 0
    for q, n, t, need, tr, row, s in allev:
        l = lit_at(lit, s, n, q)
        if row == "特許":
            pat += 1; pat_ok += abs(t - 70) < 1e-9
            continue
        if l is None:
            continue
        if l >= FULL * 1.05:
            hi += 1; hi_ok += abs(t - 70) < 1e-9
        elif l <= FULL * 0.9:
            lo += 1; lo_ok += t < 70 - 1e-9
        else:
            mid += 1
    print("識字≧%.3f の件 %d 件中 techTerm=70 は %d／識字≦%.3f の件 %d 件中 techTerm<70 は %d／その間 %d 件／特許 %d 件中 techTerm=70 は %d"
          % (FULL * 1.05, hi, hi_ok, FULL * 0.9, lo, lo_ok, mid, pat, pat_ok))
    print("== G2 世界で最初に開いた国の本国識字が %.2f 未満の（種×行）" % LOW)
    low_first = tot = early = early_low = 0
    for (s, row), l in ev.items():
        l = sorted(l)
        q0, n0 = l[0][0], l[0][1]
        x0 = lit_at(lit, s, n0, q0)
        tot += 1
        low_first += (x0 is not None and x0 < LOW)
        for q, n, *_ in l:
            if q <= q0 + 80:
                x = lit_at(lit, s, n, q)
                if x is None:
                    continue
                early += 1
                early_low += (x < LOW)
    print("%d／%d" % (low_first, tot))
    print("== G3 世界で最初の開きから 20 年以内の開き（国×行）のうち本国識字 %.2f 未満の割合" % LOW)
    print("%d／%d＝%.1f%%" % (early_low, early, 100.0 * early_low / early if early else float("nan")))
    print("== G4 辺：共産主義の四半期と社会主義の四半期（同じ種・同じ国）")
    qq = collections.defaultdict(dict)
    for (s, row), l in ev.items():
        if row in ("社会主義", "共産主義"):
            for q, n, *_ in l:
                qq[(s, n)][row] = q
    both = [v for v in qq.values() if len(v) == 2]
    print("両方開いた %d／同じ四半期 %d／共産主義が先 %d／共産主義が後 %d／共産主義だけ %d／社会主義だけ %d"
          % (len(both), sum(1 for v in both if v["共産主義"] == v["社会主義"]), sum(1 for v in both if v["共産主義"] < v["社会主義"]),
             sum(1 for v in both if v["共産主義"] > v["社会主義"]), sum(1 for v in qq.values() if "共産主義" in v and "社会主義" not in v),
             sum(1 for v in qq.values() if "社会主義" in v and "共産主義" not in v)))
    print("== R1 世界で最初の開きの年（種の中央・全国）と国")
    for row in ROWS + ["特許"]:
        ys = []; ns = collections.Counter()
        for s in range(1, 11):
            l = sorted(e for e in allev if e[5] == row and e[6] == s)
            if l:
                ys.append(1783 + l[0][0] / 4); ns[l[0][1]] += 1
        if ys:
            print("%s 最初の年（中央）%.1f 最初の国 %s" % (row, st.median(ys), dict(ns.most_common(4))))
    print("== R2 主要 9 国：開いた国の数（種の中央）・最初の主要国と同じ四半期に開いた数（中央）・開く年の幅（中央・年）")
    for row in ROWS:
        nopen = []; same = []; rng = []
        for s in range(1, 11):
            dm = {n: q for q, n, *_ in ev.get((s, row), []) if n in MAJ}
            nopen.append(len(dm))
            if dm:
                m = min(dm.values()); same.append(sum(1 for v in dm.values() if v == m)); rng.append((max(dm.values()) - m) / 4)
        print("%s 開いた %s／同四半期 %s／幅 %.1f" % (row, st.median(nopen), st.median(same) if same else "-", st.median(rng) if rng else float("nan")))
    print("== 1800 年以前（四半期≦68）の開き（国×行・全種）と、そのうち本国識字 %.2f 未満" % FULL)
    for row in ROWS + ["特許"]:
        e0 = [e for e in allev if e[5] == row and e[0] <= 68]
        ll = [lit_at(lit, e[6], e[1], e[0]) for e in e0]
        print("%s %d 件・識字<%.2f %d 件" % (row, len(e0), FULL, sum(1 for x in ll if x is not None and x < FULL)))


if __name__ == "__main__":
    main(sys.argv[1])
