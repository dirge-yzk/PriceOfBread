# (433) 線 sb：判定の段（読むだけ）。事前登録 [4][6] と追記 3 の線を、基線の計器の生と腕の計器の生から出す。
# 使い方：PYTHONUTF8=1 python judge_sb433.py <基線の計器のフォルダ(m428_ins)> <腕の計器のフォルダ> [<出力のファイル>]
# 力学の値を書かない・乱数を引かない。窓＝Q8-39・Q40-159・Q160-399・Q400-667。国＝挙げた 8 国。値は国×種の中央→国をまたいだ中央。
# T1 は spend_s（基線にもある）、T2・T3 は svcbuy_s（腕にだけある）、T4・T5 は svc_s（基線にもある）。
import csv, sys, os, glob, statistics, collections
base, arm = sys.argv[1], sys.argv[2]
out = sys.argv[3] if len(sys.argv) > 3 else None
PICK = {"清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本"}
WINS = ["Q8-39", "Q40-159", "Q160-399", "Q400-667"]
QR = {"Q8-39": (8, 39), "Q40-159": (40, 159), "Q160-399": (160, 399), "Q400-667": (400, 667)}
BUCKET = {"2": "Q8-39", "3": "Q40-159", "4": "Q160-399", "5": "Q160-399", "6": "Q160-399", "7": "Q160-399", "8": "Q400-667"}
RANK0 = {1, 2, 11, 12}   # 大衆＝農民・工場労働者・職人・使用人（階級 13・14 は spend_s の家事奉仕に出ない＝対応表から外した）
def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float("nan")
def fl(x):
    try: return float(x)
    except Exception: return float("nan")
def win_of(q):
    for w, (a, b) in QR.items():
        if a <= q <= b: return w
    return None
def upper_share(src):
    """spend_s：(国,窓) -> 種ごとの 上位の買えた量の割合・農民(階級 1)の買えた量の割合"""
    up = collections.defaultdict(list); farm = collections.defaultdict(list)
    for path in sorted(glob.glob(os.path.join(src, "spend_s*.tsv"))):
        acc = collections.defaultdict(float)
        with open(path, encoding="utf8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                if r["kind"] == "good" and r["slotName"] == "家事奉仕" and r["bucket"] in BUCKET and r["nation"] in PICK:
                    w = BUCKET[r["bucket"]]; c = int(r["classId"])
                    q = float(r["qtyPerMonth"]) * float(r["monthsCount"])
                    acc[(r["nation"], w, "U" if c not in RANK0 else "M")] += q
                    if c == 1: acc[(r["nation"], w, "F")] += q
        for n in {k[0] for k in acc}:
            for w in WINS:
                u, m, fm = acc[(n, w, "U")], acc[(n, w, "M")], acc[(n, w, "F")]
                if u + m > 0: up[(n, w)].append(u / (u + m)); farm[(n, w)].append(fm / (u + m))
    return up, farm
def svc(src):
    """svc_s：(国,窓) -> 種ごとの 使用人÷人口・価格÷参照・不足÷需要（四半期の中央）"""
    sv = collections.defaultdict(list); pr = collections.defaultdict(list); sh = collections.defaultdict(list)
    for path in sorted(glob.glob(os.path.join(src, "svc_s*.tsv"))):
        a = collections.defaultdict(lambda: [[], [], []])
        with open(path, encoding="utf8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                w = win_of(int(r["quarter"]))
                if w is None or r["nation"] not in PICK: continue
                tp, sk, d, ref = fl(r["totalPopulationK"]), fl(r["servantK"]), fl(r["demandPerMonth"]), fl(r["refPrice"])
                k = (r["nation"], w)
                if tp > 0: a[k][0].append(sk / tp)
                if ref > 0: a[k][1].append(fl(r["priceSoldWeighted"]) / ref)
                if d > 0: a[k][2].append(fl(r["shortagePerMonth"]) / d)
        for k, (s, p, h) in a.items():
            sv[k].append(med(s)); pr[k].append(med(p)); sh[k].append(med(h))
    return sv, pr, sh
def svcbuy(src):
    """svcbuy_s（腕だけ）：(国,窓) -> 種ごとの 大衆の充足・上位の充足・使用人(12)の満足度の人数加重平均の中央"""
    f0 = collections.defaultdict(list); fU = collections.defaultdict(list); sat = collections.defaultdict(list)
    for path in sorted(glob.glob(os.path.join(src, "svcbuy_s*.tsv"))):
        a = collections.defaultdict(float); s12 = collections.defaultdict(list)
        with open(path, encoding="utf8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                w = win_of(int(r["quarter"]))
                if w is None or r["nation"] not in PICK: continue
                rk = "0" if int(r["rank"]) == 0 else "U"
                a[(r["nation"], w, rk, "g")] += fl(r["gotPerMonth"]) if fl(r["gotPerMonth"]) == fl(r["gotPerMonth"]) else 0
                a[(r["nation"], w, rk, "d")] += fl(r["demandPerMonth"]) if fl(r["demandPerMonth"]) == fl(r["demandPerMonth"]) else 0
                if int(r["classId"]) == 12 and fl(r["satisfactionMean"]) == fl(r["satisfactionMean"]): s12[(r["nation"], w)].append(fl(r["satisfactionMean"]))
        for n in {k[0] for k in a}:
            for w in WINS:
                if a[(n, w, "0", "d")] > 0: f0[(n, w)].append(a[(n, w, "0", "g")] / a[(n, w, "0", "d")])
                if a[(n, w, "U", "d")] > 0: fU[(n, w)].append(a[(n, w, "U", "g")] / a[(n, w, "U", "d")])
        for k, v in s12.items(): sat[k].append(med(v))
    return f0, fU, sat
def per_nation(d, w):
    """(国,窓)->種のリスト を 国ごとの種の中央へ"""
    return {n: med(v) for (n, ww), v in d.items() if ww == w}
L = []
def P(s=""): L.append(s)
bu, bf = upper_share(base); au, af = upper_share(arm)
bs, bp, bh = svc(base); as_, ap, ah = svc(arm)
f0, fU, sat = svcbuy(arm)
P("(433) 線 sb 判定の読み　基線=%s　腕=%s" % (base, arm))
P("窓\tT1 上位の買えた量の割合(基線)\tT1(腕)\t大衆(農民)の買えた量の割合(基線)\t(腕)\tT1の線")
t1line = {"Q8-39": 0.50, "Q40-159": 0.70, "Q160-399": 0.70, "Q400-667": None}
res = {}
for w in WINS:
    b = med(list(per_nation(bu, w).values())); a = med(list(per_nation(au, w).values()))
    bfm = med(list(per_nation(bf, w).values())); afm = med(list(per_nation(af, w).values()))
    ln = t1line[w]
    ok = (a >= ln) if ln is not None else None
    res[("T1", w)] = ok
    P("%s\t%.3f\t%.3f\t%.4f\t%.4f\t%s" % (w, b, a, bfm, afm, ("≥%.2f %s" % (ln, "合格" if ok else "不合格")) if ln is not None else "記録のみ（基線 %.3f）" % b))
P()
P("T2（配線の確かめ・Q8-39）：|大衆の充足 − 上位の充足| の国の中央　線 ≤0.15　（記録：他の窓も）")
for w in WINS:
    pn0 = per_nation(f0, w); pnU = per_nation(fU, w)
    gaps = [abs(pn0[n] - pnU[n]) for n in pn0 if n in pnU]
    g = med(gaps)
    if w == "Q8-39": res[("T2", w)] = (g <= 0.15)
    P("%s\t大衆の充足 %.3f\t上位の充足 %.3f\t差の国の中央 %.3f%s" % (w, med(list(pn0.values())), med(list(pnU.values())), g, ("\t" + ("合格" if g <= 0.15 else "不合格")) if w == "Q8-39" else "\t記録"))
P()
P("T3（記録のみ）：使用人(階級 12)の満足度の人数加重平均（国×種の中央の国の中央）")
for w in WINS: P("%s\t%.3f" % (w, med(list(per_nation(sat, w).values()))))
P()
P("T4（使用人÷人口の 腕÷基線 の国の中央　線 0.5〜2.0 倍）　T5（価格÷参照　国の中央が基線の 2 倍以内かつ 5 以下／不足÷需要 窓の中央 ≤0.8）")
for w in WINS:
    pb, pa = per_nation(bs, w), per_nation(as_, w)
    ratios = {n: pa[n] / pb[n] for n in pa if n in pb and pb[n] > 0}
    r = med(list(ratios.values()))
    res[("T4", w)] = (0.5 <= r <= 2.0)
    P("%s\tT4 倍率の国の中央 %.3f（国の最小 %.3f 最大 %.3f）%s\t%s" % (w, r, min(ratios.values()), max(ratios.values()), "合格" if res[("T4", w)] else "不合格",
        " ".join("%s=%.2f" % (n, x) for n, x in sorted(ratios.items()))))
    qb, qa = med(list(per_nation(bp, w).values())), med(list(per_nation(ap, w).values()))
    hb, ha = med(list(per_nation(bh, w).values())), med(list(per_nation(ah, w).values()))
    res[("T5", w)] = (qa <= 2 * qb and qa <= 5 and ha <= 0.8)
    P("%s\tT5 価格÷参照 基線 %.3f 腕 %.3f／不足÷需要 基線 %.3f 腕 %.3f\t%s" % (w, qb, qa, hb, ha, "合格" if res[("T5", w)] else "不合格"))
P()
P("事前登録 [6] 追記 3 の判定（腕 B＝配給＋指数 2 の場合）：T1（Q8-39 と Q40-159・Q160-399）・T2・T4（4 窓）・T5（4 窓）の全て合格で『両方採る』。")
allpass = {t: all(v for (tt, ww), v in res.items() if tt == t and v is not None) for t in ("T1", "T2", "T4", "T5")}
for t in ("T1", "T2", "T4", "T5"):
    P("  %s\t%s" % (t, "合格" if allpass[t] else "不合格"))
P("  総合（腕 B）\t%s" % ("全て合格" if all(allpass.values()) else "不合格あり"))
P("事前登録 追記 3 の判定（腕 B1＝配給のみの場合）：T2・T4・T5 の合格で『配給のみ採る』（T1 は記録）。T2:%s T4:%s T5:%s" % tuple("合格" if allpass[t] else "不合格" for t in ("T2", "T4", "T5")))
txt = "\n".join(L) + "\n"
print(txt)
if out:
    with open(out, "wb") as f: f.write(txt.encode("utf8"))
