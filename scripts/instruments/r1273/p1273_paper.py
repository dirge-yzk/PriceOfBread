# (1273) 設計の材料：州ごとの治安の次の段の紙の影（盤面 m1260＝keep_1267/base_ins・読むだけ）
# 呼び方：python -X utf8 -I p1273_paper.py <base_ins のフォルダ> <出力のファイル>
# 今の形（P1-1）＝order_r＝Clamp01(R×m̄÷m_r)・R＝Coercion÷800（secession_s の orderRawNowIndex）
# 候補 A ＝R を頭数の被覆 (L−W)÷ΣN（rulepower_s の陸軍力 L・前線 W・治める必要 N＝人口×q）へ替える（H の州への配りを人口で）
# 候補 AH＝同じ・H（＝min(L−W, ΣN)）÷ΣN（上で切った形）
# 候補 B ＝州の兵の頭数（P4）〈推〉：gs の後に兵が席 ∝ 徴募の母数×届け率 へ寄ると読んで、RA×deliv_r÷deliv̄（rule_s の 9 国だけ・母数の割合は人口の割合で近似〈推〉）
# 計器は自分で州を選ぶ（首都は secession_s に無い＝全ての国は首都以外・rule_s の 9 国は首都を含む）
import sys, math, collections

BASE = sys.argv[1].rstrip('/') + '/'
OUT = sys.argv[2]
SEEDS = range(1, 11)
KS2 = 2.0


def rows(t, s):
    with open(BASE + f'{t}_s{s}.tsv', 'rb') as f:
        head = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        ix = {k: i for i, k in enumerate(head)}
        for line in f:
            yield ix, line.decode('utf-8').rstrip('\r\n').split('\t')


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def c01(x):
    return 0.0 if not (x > 0) else (1.0 if x > 1 else x)


def order(R, mbar, mr):
    return 1.0 if not (mr > 0) else c01(R * mbar / mr)


def period(y):
    return '1783-1815' if y <= 1815 else '1816-1870' if y <= 1870 else '1871-1913' if y <= 1913 else '1914-1950'


def q(v, p):
    if not v:
        return float('nan')
    v = sorted(v)
    k = (len(v) - 1) * p
    a = int(math.floor(k)); b = min(a + 1, len(v) - 1)
    return v[a] + (v[b] - v[a]) * (k - a)


def summ(v):
    n = len(v)
    if n == 0:
        return 'n=0'
    m = sum(v) / n
    a3 = sum(1 for x in v if abs(x) > 1e-3) / n
    a2 = sum(1 for x in v if abs(x) > 1e-2) / n
    a5 = sum(1 for x in v if abs(x) > 5e-2) / n
    pos = sum(1 for x in v if x > 1e-3) / n
    return (f'n={n} mean={m:+.4f} p1={q(v,.01):+.4f} p10={q(v,.1):+.4f} p50={q(v,.5):+.4f} p90={q(v,.9):+.4f} p99={q(v,.99):+.4f} '
            f'|d|>1e-3 {a3:.3f} >0.01 {a2:.3f} >0.05 {a5:.3f} 上向き(>1e-3) {pos:.3f}')


D = collections.defaultdict(list)       # 鍵→差の一覧
NQ = collections.defaultdict(list)      # 国×四半期の人口加重の差
CNT = collections.Counter()
RD = []                                 # 国×四半期の (R, RA, soldierK, popK, 国名, 年)
ONE = collections.Counter()             # 1 州の国の国×四半期・人口
CHK = collections.Counter()
NATQ_REG = collections.Counter()

for s in SEEDS:
    nat = {}
    for ix, r in rows('nation', s):
        nat[(r[ix['quarter']], r[ix['nation']])] = (fl(r[ix['soldierK']]), fl(r[ix['totalPopulationK']]))
    # 候補 S＝頭数の被覆 (兵K÷人口K)÷qS・qS＝最初の四半期の世界の兵の和÷人口の和（即応度は表に無い＝掛けない〈推〉）
    q0 = min(nat, key=lambda k: int(k[0]))[0]
    qS = sum(v[0] for k, v in nat.items() if k[0] == q0 and math.isfinite(v[0])) / sum(v[1] for k, v in nat.items() if k[0] == q0 and math.isfinite(v[1]))
    rp = {}
    natp = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0, 0.0, 0, 0.0])  # SumN, L, W, H, SumPop, regions, year
    for ix, r in rows('rulepower', s):
        key = (r[ix['quarter']], r[ix['nation']])
        a = natp[key]
        a[0] += fl(r[ix['nIndex']]); a[1] = fl(r[ix['lNationIndex']]); a[2] = fl(r[ix['wNationIndex']]); a[3] = fl(r[ix['hNationIndex']])
        a[4] += fl(r[ix['popK']]); a[5] += 1; a[6] = int(r[ix['year']])
    for key, a in natp.items():
        if a[5] == 1:
            ONE['nq'] += 1; ONE['pop_' + period(a[6])] += a[4]
        ONE['nqall'] += 1; ONE['popall_' + period(a[6])] += a[4]
    # 首都以外（全ての国）
    seen_nq = {}
    for ix, r in rows('secession', s):
        qq, nn, y = r[ix['quarter']], r[ix['nation']], int(r[ix['year']])
        a = natp.get((qq, nn))
        if a is None or not (a[0] > 0):
            CNT['sec_no_rp'] += 1; continue
        R = fl(r[ix['orderRawNowIndex']]); mbar = fl(r[ix['militancyNationMeanIndex']]); mr = fl(r[ix['militancyRegionOwnIndex']])
        reach = fl(r[ix['reachOwnShare']]); tnew = fl(r[ix['ungovTargetNewShare']]); popk = fl(r[ix['popK']])
        if not all(math.isfinite(x) for x in (R, mbar, mr, reach, tnew)):
            CNT['sec_nan'] += 1; continue
        tcur = (1 - order(R, mbar, mr)) * (1 - reach)
        CHK['n'] += 1
        if abs(tcur - tnew) > 1e-9:
            CHK['mismatch'] += 1
        RA = max(0.0, a[1] - a[2]) / a[0]
        RH = a[3] / a[0]
        sk0, pk0 = nat.get((qq, nn), (float('nan'), float('nan')))
        RS = (sk0 / pk0) / qS if pk0 > 0 and math.isfinite(sk0) else float('nan')
        tA = (1 - order(RA, mbar, mr)) * (1 - reach)
        tH = (1 - order(RH, mbar, mr)) * (1 - reach)
        tS = (1 - order(RS, mbar, mr)) * (1 - reach) if math.isfinite(RS) else float('nan')
        tS2 = (1 - order(RS / KS2, mbar, mr)) * (1 - reach) if math.isfinite(RS) else float('nan')  # qS×KS2（開幕の RuleInit[3] に合わせた倍率・p1273_calib.py）
        pr = period(y)
        for k, t in (('A', tA), ('AH', tH), ('S', tS), ('S2', tS2)):
            if not math.isfinite(t):
                CNT['nan_' + k] += 1; continue
            d = t - tnew
            D[(k, 'all')].append(d); D[(k, pr)].append(d)
            w = NQ.setdefault((k, s, qq, nn), [0.0, 0.0]); w[0] += d * popk; w[1] += popk
        if (s, qq, nn) not in seen_nq:
            seen_nq[(s, qq, nn)] = 1
            sk, pk = nat.get((qq, nn), (float('nan'), float('nan')))
            RD.append((R, RA, RH, sk, pk, nn, y, RS))
    # rule_s の 9 国（首都を含む）
    for ix, r in rows('rule', s):
        qq, nn, y = r[ix['quarter']], r[ix['nation']], int(r[ix['year']])
        a = natp.get((qq, nn))
        if a is None or not (a[0] > 0):
            continue
        R = fl(r[ix['orderRawIndex']]); mbar = fl(r[ix['militancyNationIndex']]); mr = fl(r[ix['militancyRegionIndex']])
        reach = fl(r[ix['reachShare']]); tnew = fl(r[ix['ungovTargetNewShare']]); deliv = fl(r[ix['nowRuleDelivery']])
        cap = fl(r[ix['capitalDistanceKm']]) == 0
        popk = fl(r[ix['popK']])
        if not all(math.isfinite(x) for x in (R, mbar, mr, reach, tnew)):
            continue
        NATQ_REG[(s, qq, nn)] += 1
        D.setdefault(('rule_rows', s, qq, nn), []).append((mr, mbar, reach, tnew, deliv, popk, cap, a, y, R))
    for key in [k for k in D if k[0] == 'rule_rows' and k[1] == s]:
        regs = D.pop(key)
        sp = sum(x[5] for x in regs if math.isfinite(x[4]))
        dbar = sum(x[4] * x[5] for x in regs if math.isfinite(x[4])) / sp if sp > 0 else float('nan')
        for (mr, mbar, reach, tnew, deliv, popk, cap, a, y, R) in regs:
            RA = max(0.0, a[1] - a[2]) / a[0]
            tcur = (1 - order(R, mbar, mr)) * (1 - reach)
            CHK['n9'] += 1
            if abs(tcur - tnew) > 1e-9:
                CHK['mismatch9'] += 1
            tA = (1 - order(RA, mbar, mr)) * (1 - reach)
            tag = 'cap' if cap else 'noncap'
            D[('A9', tag)].append(tA - tnew); D[('A9', 'all')].append(tA - tnew)
            if math.isfinite(deliv) and dbar > 0:
                tB = (1 - order(RA * deliv / dbar, mbar, mr)) * (1 - reach)
                D[('B9', tag)].append(tB - tnew); D[('B9', 'all')].append(tB - tnew)
                D[('B9-A9', tag)].append(tB - tA)
    print('seed', s, 'done', file=sys.stderr)

with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    w = lambda *a: print(*a, file=f)
    w('# (1273) 紙の影・盤面 m1260（keep_1267/base_ins・種 1〜10）・差＝候補の無統治の目標−今の P1-1 の目標')
    w(f'# 読みの検算：今の式で数え直した目標と secession_s の ungovTargetNewShare の不一致 {CHK["mismatch"]}/{CHK["n"]}（1e-9）・rule_s {CHK["mismatch9"]}/{CHK["n9"]}')
    w(f'# 読めなかった行：rulepower に無い {CNT["sec_no_rp"]}・非有限 {CNT["sec_nan"]}')
    w('## 首都以外の全ての国（secession_s）')
    for k in ('A', 'AH', 'S', 'S2'):
        for pr in ('all', '1783-1815', '1816-1870', '1871-1913', '1914-1950'):
            w(f'{k}\t{pr}\t{summ(D[(k, pr)])}')
    w('## 国×四半期の人口加重の差（首都以外）')
    for k in ('A', 'AH', 'S', 'S2'):
        v = [a / b for (kk, *_), (a, b) in NQ.items() if kk == k and b > 0]
        w(f'{k}\tnation-quarter\t{summ(v)}')
    w('## rule_s の 9 国（首都を含む）・B は〈推〉')
    for k in ('A9', 'B9', 'B9-A9'):
        for tag in ('all', 'cap', 'noncap'):
            if (k, tag) in D:
                w(f'{k}\t{tag}\t{summ(D[(k, tag)])}')
    w('## 被覆 R の中身（国×四半期）')
    good = [x for x in RD if all(math.isfinite(v) and v > 0 for v in (x[0], x[3], x[4]))]
    def corr(xs, ys):
        n = len(xs); mx = sum(xs) / n; my = sum(ys) / n
        sxy = sum((a - mx) * (b - my) for a, b in zip(xs, ys)); sx = math.sqrt(sum((a - mx) ** 2 for a in xs)); sy = math.sqrt(sum((b - my) ** 2 for b in ys))
        return sxy / (sx * sy) if sx > 0 and sy > 0 else float('nan')
    lr = [math.log(x[0]) for x in good]
    w(f'n={len(good)}  corr(log R, log 兵K)={corr(lr, [math.log(x[3]) for x in good]):.3f}  corr(log R, log 兵K÷人口K)={corr(lr, [math.log(x[3] / x[4]) for x in good]):.3f}  corr(log R, log 人口K)={corr(lr, [math.log(x[4]) for x in good]):.3f}')
    g2 = [x for x in good if x[1] > 0]
    la = [math.log(x[1]) for x in g2]
    w(f'n={len(g2)}  corr(log RA, log 兵K)={corr(la, [math.log(x[3]) for x in g2]):.3f}  corr(log RA, log 兵K÷人口K)={corr(la, [math.log(x[3] / x[4]) for x in g2]):.3f}  corr(log RA, log 人口K)={corr(la, [math.log(x[4]) for x in g2]):.3f}')
    w(f'R≥1 の国×四半期 {sum(1 for x in good if x[0] >= 1) / len(good):.3f}・RA≥1 {sum(1 for x in g2 if x[1] >= 1) / len(g2):.3f}・RH≥0.999 {sum(1 for x in g2 if x[2] >= 0.999) / len(g2):.3f}')
    w(f'R の分位 p10 {q([x[0] for x in good], .1):.3f} p50 {q([x[0] for x in good], .5):.3f} p90 {q([x[0] for x in good], .9):.3f}・RA の分位 p10 {q([x[1] for x in g2], .1):.3f} p50 {q([x[1] for x in g2], .5):.3f} p90 {q([x[1] for x in g2], .9):.3f}')
    by = collections.defaultdict(list)
    for x in g2:
        by[x[5]].append(x)
    w('国\t国×四半期\tR中央\tRA中央\t兵K中央\t人口K中央\t兵÷人口(%)中央')
    lst = sorted(by.items(), key=lambda kv: -q([x[4] for x in kv[1]], .5))
    for nn, xs in lst[:12] + lst[-6:]:
        w(f'{nn}\t{len(xs)}\t{q([x[0] for x in xs], .5):.3f}\t{q([x[1] for x in xs], .5):.3f}\t{q([x[3] for x in xs], .5):.1f}\t{q([x[4] for x in xs], .5):.0f}\t{100 * q([x[3] / x[4] for x in xs], .5):.3f}')
    g3 = [x for x in good if math.isfinite(x[7]) and x[7] > 0]
    ls_ = [math.log(x[7]) for x in g3]
    w(f'n={len(g3)}  corr(log RS, log 兵K÷人口K)={corr(ls_, [math.log(x[3] / x[4]) for x in g3]):.3f}  corr(log RS, log 人口K)={corr(ls_, [math.log(x[4]) for x in g3]):.3f}  RS≥1 {sum(1 for x in g3 if x[7] >= 1) / len(g3):.3f}  RS p10 {q([x[7] for x in g3], .1):.3f} p50 {q([x[7] for x in g3], .5):.3f} p90 {q([x[7] for x in g3], .9):.3f}')
    for nn, xs in lst[:12] + lst[-6:]:
        w(f'RS	{nn}	{q([x[7] for x in xs], .5):.3f}')
    w('## 1 州の国（CheckSecession の頭の return＝Rule が動かない）')
    w(f'国×四半期 {ONE["nq"]}/{ONE["nqall"]}')
    for pr in ('1783-1815', '1816-1870', '1871-1913', '1914-1950'):
        w(f'{pr}\t人口の割合 {ONE["pop_" + pr] / ONE["popall_" + pr]:.4f}')
