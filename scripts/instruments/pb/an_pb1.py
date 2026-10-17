# -*- coding: utf-8 -*-
"""(384) 政治の束の削除の腕の読み手（読むだけ）。事前登録 measurements/pb_20261017_prereg.txt の [6]・追記1〜3 の定義どおり。腕の結果を見てから式を書かない。
使い方：PYTHONUTF8=1 python an_pb1.py A=<出力>/ins B=<出力>/ins N1=<出力>/ins N2=<出力>/ins [rB=<B の報告> rA=<A の報告> BP=<出力>/ins BM=<出力>/ins]
読む表：scalars_s（pb の列・militancyMean・educationLaw・defeatShock・gdpDrawdown・polityCode）／nation_s（polityCode・civilWarMonths・nationUid・populationK・supplyFillShare・sovereignDefaultCumulativeCount・bond・priceLevel）／region_s（theftPoolPerMonth・州の集合）。
採否に使う線は追記3 の〈止める線〉だけ（発散 (i')(ii')(iii)・空洞化〔an_h2.py〕・計算量〔.cost〕・破綻〔報告〕）。H1〜H4・R* は記録。M0 は成績表の cmp（このファイルの外）。"""
import sys, os, glob, collections, importlib.util

HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("an_pb_div", os.path.join(HERE, "an_pb_div.py"))
DIV = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(DIV)

NAMES = ["Absolutist", "Despotic", "Shogunate", "ConstMon", "Republic", "BD", "PD", "MD", "Anarchy"]
DICT = {5, 6, 7}
DEMO = ["イギリス", "ネーデルラント", "アメリカ", "スイス", "スウェーデン", "デンマーク"]
H4N = ["清", "オスマン", "ロシア", "日本"]
WINDOWS = {"ロシア": (1917, 1925), "プロイセン": (1930, 1940), "スペイン": (1920, 1945), "日本": (1930, 1945), "ポルトガル": (1926, 1945), "清": (1911, 1925), "オスマン": (1908, 1925)}
YEAR_END = 1951
PBCOLS = ["pbDomActive", "pbDomFlip", "pbDomDecisive", "pbDomExec", "pbDomMismatch", "pbRegime", "pbRegimeViolent", "pbRegimeDict", "pbAgil", "pbSeize"]
SCA_COLS = ["quarter", "year", "nation", "polityCode", "educationLaw", "defeatShock", "gdpDrawdown", "militancyMean", "factionRevolts"] + PBCOLS
PB0 = len(SCA_COLS) - len(PBCOLS)   # PBCOLS の先頭の位置
F2_MIN = 1000   # 腕の前に探りで決める（追記3）
F7_MIN = 10     # 腕の前に探りで決める（追記3）


def fnum(s):
    try:
        return float(s)
    except ValueError:
        return float("nan")


def stream(path, names):
    """列名の一覧だけを読む。戻り値：(欠けた列名, 行のイテレータ[リスト])。"""
    f = open(path, encoding="utf-8")
    h = next(f).rstrip("\n").split("\t")
    ix = {n: i for i, n in enumerate(h)}
    miss = [n for n in names if n not in ix]
    idx = [ix.get(n, -1) for n in names]

    def it():
        for l in f:
            r = l.rstrip("\n").split("\t")
            if len(r) < len(h) - 1:
                continue
            yield [r[i] if 0 <= i < len(r) else "" for i in idx]
        f.close()
    return miss, it()


def seeds(d, stem):
    return sorted(int(p.split(stem)[-1].split(".")[0]) for p in glob.glob("%s/%s*.tsv" % (d, stem)))


def med(v):
    v = sorted(v)
    n = len(v)
    return float("nan") if n == 0 else (v[n // 2] if n % 2 else 0.5 * (v[n // 2 - 1] + v[n // 2]))


class Arm:
    def __init__(self, tag, d):
        self.tag, self.d = tag, d
        self.seeds = seeds(d, "nation_s")
        self.nat = {}
        self.sca = {}
        self.pbmiss = []
        for s in self.seeds:
            miss, it = stream("%s/nation_s%d.tsv" % (d, s), ["quarter", "year", "nationIndex", "nation", "nationUid", "populationK", "totalPopulationK", "polityCode", "civilWarMonths", "supplyFillShare", "sovereignDefaultCumulativeCount"])
            assert not miss, miss
            rows = []
            for r in it:
                rows.append((int(r[0]), int(r[1]), r[2], r[3], r[4], fnum(r[5]), fnum(r[6]),
                             int(float(r[7])) if r[7] not in ("", "nan") else -1,
                             fnum(r[8]) if r[8] not in ("", "nan") else 0.0, fnum(r[9]),
                             fnum(r[10]) if r[10] not in ("", "nan") else 0.0))
            self.nat[s] = rows
            miss, it = stream("%s/scalars_s%d.tsv" % (d, s), SCA_COLS)
            self.pbmiss = miss
            self.sca[s] = [r for r in it]

    # ---- 政体の遷移（nation_s の polityCode・国は nationUid）----
    def flows(self):
        per = {}
        stay = collections.defaultdict(list)
        demo = 0
        entry = collections.Counter()
        for s, rows in self.nat.items():
            last = {}
            cwl = {}
            start = {}
            t = to_dict = to_non = cw = 0
            for q, y, ni, nm, u, pop, tpop, pc, cwm, sup, dflt in rows:
                if u in last and last[u] != pc:
                    t += 1
                    if pc in DICT:
                        to_dict += 1
                        entry[(NAMES[last[u]] if last[u] >= 0 else "?", NAMES[pc])] += 1
                        if nm in DEMO:
                            demo += 1
                    else:
                        to_non += 1
                    if last[u] in DICT:
                        stay[last[u]].append(q - start[u])
                    start[u] = q
                if u not in last:
                    start[u] = q
                if cwm > 0 and cwl.get(u, 0) <= 0:
                    cw += 1
                last[u] = pc
                cwl[u] = cwm
            per[s] = (t, to_dict, to_non, cw)
        return per, stay, demo, entry

    # ---- scalars_s の最後の累計（国の名前ごとの最大・種ごと）。名前が変わった World は両方の名前で数える（全ての腕で同じ）----
    def pbcum(self):
        out = {}
        for s, rows in self.sca.items():
            m = collections.defaultdict(lambda: [0.0] * (len(PBCOLS) + 1))
            for r in rows:
                v = m[r[2]]
                for i in range(len(PBCOLS)):
                    x = fnum(r[PB0 + i])
                    if x == x and x > v[i]:
                        v[i] = x
                x = fnum(r[8])
                if x == x and x > v[len(PBCOLS)]:
                    v[len(PBCOLS)] = x
            tot = [0.0] * (len(PBCOLS) + 1)
            for v in m.values():
                for i in range(len(tot)):
                    tot[i] += v[i]
            out[s] = tot
        return out

    def windows(self):
        k = collections.Counter()
        alive = collections.Counter()
        for s, rows in self.nat.items():
            uid0 = {}
            hit = set()
            seen = set()
            for q, y, ni, nm, u, pop, tpop, pc, cwm, sup, dflt in rows:
                if q == 0 and nm in WINDOWS:
                    uid0[u] = nm
                if u in uid0:
                    n = uid0[u]
                    seen.add(n)
                    if WINDOWS[n][0] <= y <= WINDOWS[n][1] and pc in DICT:
                        hit.add(n)
            for n in hit:
                k[n] += 1
            for n in seen:
                alive[n] += 1
        return k, alive

    def edu_years(self):
        res = {n: [] for n in H4N}
        for s, rows in self.sca.items():
            first = {}
            for r in sorted(rows, key=lambda r: int(r[0])):
                if r[2] in H4N and r[2] not in first and r[4] not in ("", "nan") and int(float(r[4])) >= 2:
                    first[r[2]] = int(r[1])
            for n in H4N:
                res[n].append(first.get(n, YEAR_END))
        return res

    def supply(self):
        per = {}
        top = collections.Counter()
        for s, rows in self.nat.items():
            c = 0
            for q, y, ni, nm, u, pop, tpop, pc, cwm, sup, dflt in rows:
                if pop >= 100 and sup < 0.3:
                    c += 1
                    top[nm] += 1
            per[s] = c
        return per, top

    def defaults(self):
        tot = 0
        for s, rows in self.nat.items():
            m = {}
            for q, y, ni, nm, u, pop, tpop, pc, cwm, sup, dflt in rows:
                m[u] = max(m.get(u, 0.0), dflt)
            tot += sum(m.values())
        return tot


def main(args):
    arms = {}
    reports = {}
    for a in args:
        k, v = a.split("=", 1)
        if k.startswith("r"):
            reports[k[1:]] = v
        else:
            arms[k] = Arm(k, v)
    A, B = arms["A"], arms["B"]
    out = print
    cache = {t: dict(fl=a.flows(), cum=a.pbcum()) for t, a in arms.items()}

    def tot(t, i):
        return sum(v[i] for v in cache[t]["cum"].values())

    out("== 腕：" + "・".join("%s=%s" % (t, a.d) for t, a in arms.items()))
    for t, a in arms.items():
        if a.pbmiss:
            out("  ⚠ %s の scalars_s に無い列：%s" % (t, a.pbmiss))
    # ------------------------------------------------ M1・M3
    out("--- M1：B の引力の列が全行 0（pbDomActive・pbDomFlip・pbDomDecisive）／B の報告の POB_COEF の写し ---")
    for t in ("B", "BP", "BM"):
        if t in arms:
            bad = sum(1 for rows in arms[t].sca.values() for r in rows if any(fnum(r[PB0 + j]) != 0 for j in (0, 1, 2)))
            out("  %s：引力の列が 0 でない行 %d" % (t, bad))
    for t, p in reports.items():
        try:
            lines = [l.strip() for l in open(p, encoding="utf-8", errors="replace") if "POB_COEF" in l][:2]
            out("  報告 %s：POB_COEF の行 %s" % (t, lines if lines else "（無し）"))
        except OSError as e:
            out("  報告 %s を開けない：%s" % (t, e))
    out("--- M3：計器の複製が本体と一致（pbDomMismatch が全行 0）---")
    for t in ("A", "N1", "N2"):
        if t in arms:
            bad = sum(1 for rows in arms[t].sca.values() for r in rows if fnum(r[PB0 + 4]) != 0)
            out("  %s：0 でない行 %d → %s" % (t, bad, "合格" if bad == 0 else "不合格（pbDomFlip・pbDomDecisive は信用しない）"))
    # ------------------------------------------------ M2
    out("--- M2：何もしない機構でない（A）---")
    rows = [r for s in A.sca.values() for r in s]
    pcs = [int(float(r[3])) for r in rows if r[3] not in ("", "nan")]
    f1 = tot("A", 0) >= 100 and tot("A", 2) >= 1
    out("  F1 pbDomActive %d（≧100）・pbDomDecisive %d（≧1）→ %s" % (tot("A", 0), tot("A", 2), "合格" if f1 else "不合格"))
    nonpar = sum(1 for p in pcs if p not in (3, 4))
    out("  F2（旧・参考）非議会政体の行 %d" % nonpar)
    out("  F2'（採否の確かめ）pbAgil %d（≧%d）→ %s" % (tot("A", 8), F2_MIN, "合格" if tot("A", 8) >= F2_MIN else "不合格（P2 は休眠の掃除に分類）"))
    th = 0
    for s in A.seeds:
        miss, it = stream("%s/region_s%d.tsv" % (A.d, s), ["theftPoolPerMonth"])
        for (v,) in it:
            if fnum(v) > 0:
                th += 1
    out("  F3 theftPoolPerMonth>0 の行 %d（≧100）→ %s" % (th, "合格" if th >= 100 else "不合格"))
    d4 = A.defaults()
    out("  F4a 主権の不履行の累計 %d（≧10）→ %s" % (d4, "合格" if d4 >= 10 else "不合格"))
    out("  F4b/F5 pbRegime %d（≧300）→ %s" % (tot("A", 5), "合格" if tot("A", 5) >= 300 else "不合格"))
    up = 0
    for s, rr in A.sca.items():
        last = {}
        for r in sorted(rr, key=lambda r: int(r[0])):
            x = fnum(r[5])
            if r[2] in last and last[r[2]] < 0.5 and x >= 0.99:
                up += 1
            last[r[2]] = x
    out("  F4c defeatShock が 0.5 未満から 0.99 以上へ上がった行 %d（≧10）→ %s" % (up, "合格" if up >= 10 else "不合格"))
    g6 = sum(1 for r in rows if fnum(r[6]) > 0.05)
    out("  F6 gdpDrawdown>0.05 の行 %d（≧1000）→ %s" % (g6, "合格" if g6 >= 1000 else "不合格"))
    out("  F7 pbSeize %d（≧%d）→ %s" % (tot("A", 9), F7_MIN, "合格" if tot("A", 9) >= F7_MIN else "不合格（E7 は休眠の掃除に分類）"))
    # ------------------------------------------------ 発散（止める線）
    out("--- 止める線：発散 (i')(ii')(iii)（追記3）---")
    dv = {t: DIV.analyse(a.d) for t, a in arms.items()}
    ref = dv["A"]
    for t, r in dv.items():
        r["price"] = DIV.price_div(r["final"], ref["final"])
        out("  %s：(i') 利回りが上限に張り付いた国×四半期 %d（最大の利回り %.4f）・(ii') 州の集合が同じ 3倍/1/3 の跳び %d（州の移りを含む総数 %d）・(iii) 種ごとの最大 %d（%s）" % (t, r["clamped"], r["bond_max"], r["popjump"], r["popjump_all"], max(r["price"].values()) if r["price"] else 0, r["price"]))
    nb = [dv[t] for t in ("A", "N1", "N2") if t in dv]
    for t in [x for x in ("B", "BP", "BM") if x in dv]:
        r = dv[t]
        m1 = max(x["clamped"] for x in nb)
        m2 = max(x["popjump"] for x in nb)
        m3 = max(max(x["price"].values()) for x in nb)
        s1 = r["clamped"] > 3 * m1 and r["clamped"] - m1 >= 300
        s2 = r["popjump"] > m2 and r["popjump"] >= 1
        s3 = max(r["price"].values()) >= 3 and max(r["price"].values()) > m3
        out("  %s の発散：(i') %s・(ii') %s・(iii) %s → %s" % (t, "掛かる" if s1 else "掛からない", "掛かる" if s2 else "掛からない", "掛かる" if s3 else "掛からない", "止める線に掛かる" if (s1 or s2 or s3) else "掛からない"))
    # ------------------------------------------------ 補給崩壊・R2
    out("--- R2：補給崩壊（人口 100 以上・supplyFillShare<0.3・記録）---")
    sup = {}
    for t, a in arms.items():
        per, top = a.supply()
        sup[t] = sum(per.values())
        out("  %s：総数 %d・千件級の種 %d/10・種ごと %s・1 国の寄与の上位 %s" % (t, sup[t], sum(1 for v in per.values() if v >= 1000), [per[s] for s in sorted(per)], top.most_common(3)))
    out("  〈壊す〉の物差し（記録）B ＞ 2×max(A,N1,N2)：%s" % ("超える" if sup["B"] > 2 * max(sup[t] for t in ("A", "N1", "N2") if t in sup) else "超えない"))
    # ------------------------------------------------ H1（記録）
    out("--- H1（記録・追記1 ⑥ の定義）---")

    def qty(t):
        per = cache[t]["fl"][0]
        return dict(a=sum(v[2] for v in per.values()), b=sum(v[1] for v in per.values()), c=sum(v[3] for v in per.values()),
                    d=tot(t, 6) - tot(t, 7), e=tot(t, len(PBCOLS)), bp=tot(t, 7))
    Q = {t: qty(t) for t in arms}
    w = {k: abs(Q["N1"][k] - Q["N2"][k]) for k in Q["A"]}
    for k, lab, side in (("a", "遷移のうち先が独裁でない", 2), ("c", "内戦の発生", 2), ("d", "暴力的な政体変更のうち先が独裁でない", 2), ("e", "蜂起", 2), ("b", "独裁への流入", 1), ("bp", "暴力的な政体変更のうち先が独裁", 1)):
        m = sum(Q[t][k] for t in ("A", "N1", "N2")) / 3.0
        tol = max(2 * w.get(k, 0), 0.25 * m)
        if m < 10:
            ver = "判定不能（m<10）"
        elif side == 2:
            ver = "合格" if abs(Q["B"][k] - m) <= tol else "不合格"
        else:
            ver = "合格" if (0.5 * m <= Q["B"][k] <= m + tol) else "不合格"
        out("  (%s) %s：A %.0f・B %.0f・N1 %.0f・N2 %.0f・m %.1f・幅 %.1f → %s" % (k, lab, Q["A"][k], Q["B"][k], Q["N1"][k], Q["N2"][k], m, tol, ver))
    # ------------------------------------------------ H2（記録）
    out("--- H2：史実の独裁の窓の k（記録）---")
    K = {t: a.windows() for t, a in arms.items()}
    det = fail = 0
    for n in WINDOWS:
        kb = [K[t][0][n] for t in ("A", "N1", "N2")]
        thr = min(kb) - 2
        row = "  %s：" % n + "・".join("%s %d/%d" % (t, K[t][0][n], K[t][1][n]) for t in arms)
        if thr > 0:
            det += 1
            ok = K["B"][0][n] >= thr
            fail += (not ok)
            row += " → 判定でき、%s" % ("合格" if ok else "不合格")
        else:
            row += " → 判定不能"
        out(row)
    out("  H2 判定（記録）：" + ("判定不能" if det == 0 else ("合格" if fail < det / 6.0 else "不合格") + "（判定できる %d・落ち %d）" % (det, fail)))
    # ------------------------------------------------ H3（記録）
    out("--- H3：民主制の核への独裁の流入の合計（記録）---")
    D3 = {t: cache[t]["fl"][2] for t in arms}
    out("  " + "・".join("%s %d" % (t, D3[t]) for t in arms) + " → B ≤ max(A,N1,N2)+10：%s" % ("合格" if D3["B"] <= max(D3[t] for t in ("A", "N1", "N2")) + 10 else "不合格"))
    # ------------------------------------------------ H4（記録）
    out("--- H4：教育法 Public 以上の最初の年の中央値（届かない種は 1951・記録）---")
    E = {t: a.edu_years() for t, a in arms.items()}
    okn = 0
    for n in H4N:
        m = {t: med(E[t][n]) for t in arms}
        un = {t: sum(1 for y in E[t][n] if y >= YEAR_END) for t in arms}
        bm = sum(m[t] for t in ("A", "N1", "N2")) / 3.0
        tol = max(15, 2 * abs(m["N1"] - m["N2"]))
        ok = abs(m["B"] - bm) <= tol
        okn += ok
        out("  %s：" % n + "・".join("%s %.1f（届かない %d）" % (t, m[t], un[t]) for t in arms) + " 基線平均 %.1f 幅 %.1f → %s%s" % (bm, tol, "合格" if ok else "不合格", "（B が前倒し）" if (not ok and m["B"] < bm) else ""))
    out("  H4 判定（記録）：%d/4 国 → %s（P2 の効きは段の法には掛からない＝H4 は P2 の読みではない）" % (okn, "合格" if okn >= 3 else "不合格"))
    # ------------------------------------------------ R3・R4・R5・R6
    out("--- R3：独裁の在任（件数・中央値の四半期）と流入の内訳（記録）---")
    for t in arms:
        stay = cache[t]["fl"][1]
        entry = cache[t]["fl"][3]
        out("  %s：在任 %s・流入 %s" % (t, {NAMES[k]: (len(v), med(v)) for k, v in sorted(stay.items())}, dict(entry.most_common(6))))
    out("--- R4：カオス性（記録）|B−A| ÷ |N1−N2|---")
    for k in ("a", "b", "c", "d", "e", "bp"):
        dn = abs(Q["N1"][k] - Q["N2"][k])
        out("  (%s) |B−A| %.0f ÷ |N1−N2| %.0f = %s" % (k, abs(Q["B"][k] - Q["A"][k]), dn, "%.2f" % (abs(Q["B"][k] - Q["A"][k]) / dn) if dn else "N/A"))
    dn = abs(sup["N1"] - sup["N2"])
    out("  補給崩壊の総数 |B−A| %d ÷ |N1−N2| %d = %s" % (abs(sup["B"] - sup["A"]), dn, "%.2f" % (abs(sup["B"] - sup["A"]) / dn) if dn else "N/A"))
    for t in arms:
        per = cache[t]["fl"][0]
        out("  %s：種ごとの遷移の最小・最大 %d・%d" % (t, min(v[0] for v in per.values()), max(v[0] for v in per.values())))
    out("--- R5：militancyMean の種ごとの中央値と全体（記録）---")
    for t, a in arms.items():
        pm = []
        for s, rr in a.sca.items():
            v = [fnum(r[7]) for r in rr]
            v = [x for x in v if x == x]
            pm.append(med(v))
        out("  %s：種ごと %s・全体の中央値 %.4f" % (t, [round(x, 3) for x in pm], med(pm)))
    out("--- R6：pbDomActive・pbDomFlip・pbDomDecisive・pbDomExec の最後の累計（A）と BD・MD への流入（記録）---")
    out("  A：%s ・流入 A %d・B %d" % ([round(tot("A", i)) for i in range(4)], Q["A"]["b"], Q["B"]["b"]))
    out("  pbAgil（A・B）%d・%d ／ pbSeize（A・B）%d・%d" % (tot("A", 8), tot("B", 8), tot("A", 9), tot("B", 9)))


if __name__ == "__main__":
    main(sys.argv[1:])
