# (374) 後半: 扶養力の錨＋食料の効きの切り離し 腕 A・N1・N2・B0・B・N1B・N2B（計器つき10種）を事前登録（追記2まで）の線で読む。読むだけ。
# 使い方: PYTHONUTF8=1 python an_an374.py <作業フォルダ(例 D:/PoB_work/an2_374)> <成績表のフォルダ(例 D:/PoB_work/wt_an2/measurements)> <出力txt> [腕の並び]
#   各腕の ins フォルダ = <作業フォルダ>/out_<腕>_10/ins（anchor_s<種>.tsv・nation_s<種>.tsv・pop_s<種>.tsv を読む）
#   成績表（HL1 用）= <成績表のフォルダ>/b10_20261016_an2<腕>_ins（無い腕は HL1 を飛ばす）
# 線の数字は事前登録 measurements/an2_20261016_prereg.txt（追記1・追記2まで）のとおり。ここに書いてあるだけで、結果を見て動かさない。
#   M1 A系: |used/old-1|≦1e-9 の行が 99% 以上／B系: 外れのうち近傍（前後の四半期）に移転の無い行が 0
#   M2 B系の Q0 で anchorRegions と anchorOld の相対差が 1e-9 以内（外れ 0）
#   M3 (i) B系で錨が 1% 以上動いた行が正 (ii) 錨が動いて capBase>arable*(1+1e-6) の行が正 (iii) B/N1B/N2B で 錨0 の行の capBase が arable*f に 1e-6 で合う行が 99% 以上
#   G1 F={born=0,old>reg*(1+1e-6),popK>=100} の崩壊件数: max(B,N1B,N2B) < min(A,N1,N2)・min(A,N1,N2)>=30 でなければ記録／縮み注意＝B系の F 行数が A系の最小の半分未満
#   G2 崩壊の総数 max(B,N1B,N2B) <= max(A,N1,N2)
#   S1 群III（追記1）の国（>=3種）n>=8 で評価。n_B <= max(5, n_N1, n_N2)
import csv, sys, collections, math, re, os, statistics as st

W = sys.argv[1]
MEAS = sys.argv[2]
OUT = sys.argv[3]
ARMS = sys.argv[4].split(',') if len(sys.argv) > 4 else ['A', 'N1', 'N2', 'B0', 'B', 'N1B', 'N2B']
ASER = [a for a in ('A', 'N1', 'N2') if a in ARMS]
BSER = [a for a in ('B', 'N1B', 'N2B') if a in ARMS]     # 錨＋食料
B1SER = [a for a in ('B0', 'B', 'N1B', 'N2B') if a in ARMS]   # 旗1（錨）
SEEDS = range(1, 11)
Q_H3 = 524      # 1914 年
Q_1850 = 268
Q_END = 667     # 1950 年
out = []
P = out.append


def med(v):
    v = list(v)
    return st.median(v) if v else None


def f(x, spec='.2f'):
    return '-' if x is None else format(x, spec)


def rel(a, b):
    return abs(a - b) / abs(b) if b else (0.0 if a == 0 else float('inf'))


def fl(x):
    return 1.0 + 0.5 * (max(0.3, x) - 1.0)   # 形の式の写し（M3 iii・S1 群III の期待値。歯は形の番人の突然変異）


BORN, POP, OLD, REG, USED, RN, ACT, ARN, CAP, CB, AR, CORE, FR = range(13)


def anchor_rows(arm, s):
    p = f"{W}/out_{arm}_10/ins/anchor_s{s}.tsv"
    d = {}
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t')
        h = next(r)
        ix = {c: i for i, c in enumerate(h)}
        gi = (ix['born'], ix['popK'], ix['anchorOld'], ix['anchorRegions'], ix['anchorUsed'], ix['regionsN'], ix['activeRegionsN'],
              ix['anchorRegionsN'], ix['capacity'], ix['capBase'], ix['arableSum'], ix['coreK'], ix['foodRatio'])
        iq, inat = ix['quarter'], ix['nation']
        for ln in r:
            d[(ln[inat], int(ln[iq]))] = (int(ln[gi[0]]),) + tuple(float(ln[i]) for i in gi[1:5]) + (int(ln[gi[5]]), int(ln[gi[6]]), int(ln[gi[7]])) + tuple(float(ln[i]) for i in gi[8:])
    return d


def collapse_rows(arm, s):
    """nation_s から (nation, quarter) の補給崩壊（人口100以上・supplyFillShare<0.3）。"""
    p = f"{W}/out_{arm}_10/ins/nation_s{s}.tsv"
    res = []
    with open(p, encoding='utf-8', errors='replace', newline='') as fh:
        r = csv.reader(fh, delimiter='\t')
        h = next(r)
        ix = {c: i for i, c in enumerate(h)}
        iq, inat, ipop, ifill = ix['quarter'], ix['nation'], ix['populationK'], ix['supplyFillShare']
        for ln in r:
            try:
                fill = float(ln[ifill]); pop = float(ln[ipop])
            except Exception:
                continue
            if fill < 0.3 and pop >= 100:
                res.append((ln[inat], int(ln[iq])))
    return res


A = {arm: {s: anchor_rows(arm, s) for s in SEEDS} for arm in ARMS}
coll = {arm: {s: collapse_rows(arm, s) for s in SEEDS} for arm in ARMS}

P('# (374) 後半: 扶養力の錨＋食料の効きの切り離しを事前登録（追記2まで）の線で読む（計器つき 10 種・読むだけ）')
P('腕: ' + ' / '.join(ARMS) + '（入力 ' + W + '/out_<腕>_10/ins）')
P('行数 anchor_s: ' + ' '.join(f"{arm}={sum(len(v) for v in A[arm].values())}" for arm in ARMS))
P('')


def is_F(v):
    return v[BORN] == 0 and v[OLD] > v[REG] * (1 + 1e-6) and v[POP] >= 100


# ---------------- M1 ----------------
P('## M1（採否）A系: |anchorUsed÷anchorOld−1|≦1e-9 の行（capacity＞0）が 99% 以上／B0・B系: 外れの行のうち近傍（前後の四半期の行で anchorRegions・regionsN・anchorRegionsN が変わる・前の行が無い）に移転の無い行が 0')
m1_ok = True
for arm in ARMS:
    tot = hit = skipped = 0
    near = 0; far = 0; far_ex = []
    for s, d in A[arm].items():
        for (n, q), v in d.items():
            if not (v[CAP] > 0):
                skipped += 1
                continue
            tot += 1
            if arm in B1SER:
                if rel(v[USED], v[REG]) <= 1e-9:
                    hit += 1
                else:
                    pv = d.get((n, q - 1)); nv = d.get((n, q + 1))
                    ch = (pv is None) or (pv[REG] != v[REG] or pv[RN] != v[RN] or pv[ARN] != v[ARN]) or (nv is not None and (nv[REG] != v[REG] or nv[RN] != v[RN] or nv[ARN] != v[ARN]))
                    if ch:
                        near += 1
                    else:
                        far += 1
                        if len(far_ex) < 5:
                            far_ex.append((s, n, q, v[USED], v[OLD], v[REG]))
            else:
                if rel(v[USED], v[OLD]) <= 1e-9:
                    hit += 1
    if arm in B1SER:
        ok = far == 0
        m1_ok &= ok
        P(f"  {arm}: 一致 {hit}/{tot}（{hit/tot*100:.3f}%）・外れ {tot-hit}＝近傍に移転あり {near}・近傍に移転なし {far}・capacity＝0 の行 {skipped}（除外）→ {'合格' if ok else '外れ（形のバグ）'}")
        for m in far_ex:
            P(f"     移転なしの外れ例 種{m[0]} {m[1]} Q{m[2]} used={m[3]:.6g} old={m[4]:.6g} regions={m[5]:.6g}")
    else:
        a = hit / tot if tot else 0
        ok = a >= 0.99
        m1_ok &= ok
        P(f"  {arm}: 一致 {hit}/{tot}（{a*100:.3f}%）・capacity＝0 の行 {skipped}（除外）→ {'合格' if ok else '外れ'}")
P(f"→ M1 {'合格' if m1_ok else '外れ（形のバグ＝形を直してから測り直す）'}")
P('')

# ---------------- M2 ----------------
P('## M2（採否）B0・B系の最初の四半期の行で全国の anchorRegions と anchorOld の相対差が 1e-9 以内')
m2_ok = True
for arm in B1SER:
    bad = tot = 0
    for s, d in A[arm].items():
        for (n, q), v in d.items():
            if q != 0:
                continue
            tot += 1
            if rel(v[REG], v[OLD]) > 1e-9:
                bad += 1
    m2_ok &= (bad == 0 and tot > 0)
    P(f"  {arm}: 国×種 {tot}・外れ {bad} → {'合格' if bad == 0 and tot > 0 else '外れ'}")
P('')

# ---------------- M3 ----------------
P('## M3（採否）何もしない機構が緑にならない確かめ（(iii) の期待値は形の式の写し・歯は形の番人の突然変異）')
m3_ok = True
for arm in B1SER:
    moved = land_vs = 0
    for s, d in A[arm].items():
        for (n, q), v in d.items():
            if v[OLD] > 0 and abs(v[REG] / v[OLD] - 1) >= 0.01:
                moved += 1
                if v[CB] > v[AR] * (1 + 1e-6):
                    land_vs += 1
    ok = moved > 0 and land_vs > 0
    m3_ok &= ok
    P(f"  {arm}: (i) 錨が 1% 以上動いた国×四半期 {moved}／(ii) うち capBase＞arableSum×(1+1e-6) の行 {land_vs}（{(land_vs/moved*100 if moved else 0):.1f}%）→ {'合格' if ok else '外れ'}")
for arm in ARMS:
    z = zhit = zfree = 0
    for s, d in A[arm].items():
        for (n, q), v in d.items():
            if v[REG] < 1e-9 and v[CAP] > 0:
                z += 1
                exp = ar_exp = v[AR] * fl(v[FR])
                if abs(v[CB] - exp) <= 1e-6 * max(abs(exp), 1e-12):
                    zhit += 1
                if abs(v[CB] - v[AR]) <= 1e-9 * max(abs(v[AR]), 1e-12):
                    zfree += 1
    if arm in BSER:
        ok = z > 0 and zhit / z >= 0.99
        m3_ok &= ok
        P(f"  {arm}: (iii) 錨 0 の行（capacity＞0）{z}・capBase＝arableSum×f（1e-6）の行 {zhit}（{(zhit/z*100 if z else 0):.2f}%）・capBase＝arableSum の行 {zfree} → {'合格' if ok else '外れ'}")
    else:
        P(f"  {arm}: （記録）錨 0 の行 {z}・capBase＝arableSum の行 {zfree}（{(zfree/z*100 if z else 0):.1f}%）・capBase＝arableSum×f の行 {zhit}")
P(f"→ M3 {'合格' if m3_ok else '外れ'}")
P('')

# ---------------- G1 / G2 / R4 / R10 ----------------
P('## G1（採否）凍った錨の行 F（born＝0・anchorOld＞anchorRegions×(1+1e-6)・popK≧100）の中の補給崩壊の件数（10 種の総数）。max(B,N1B,N2B)＜min(A,N1,N2) で合格。min(A,N1,N2)＜30 なら記録')
Frows = {}; Fcoll = {}
for arm in ARMS:
    fr = fc = 0
    for s in SEEDS:
        d = A[arm][s]
        fr += sum(1 for v in d.values() if is_F(v))
        for (n, q) in coll[arm][s]:
            v = d.get((n, q))
            if v is not None and is_F(v):
                fc += 1
    Frows[arm] = fr; Fcoll[arm] = fc
    P(f"  {arm}: F の行 {fr}・F の中の崩壊 {fc}（件数÷F の行数 {(fc/fr if fr else 0):.4f}）")
g1 = None
if ASER and BSER:
    mn = min(Fcoll[a] for a in ASER); mxb = max(Fcoll[a] for a in BSER)
    if mn < 30:
        g1 = '記録に落ちる（旗 0 の最小が 30 件未満）'
    else:
        g1 = '合格' if mxb < mn else '外れ'
    shrink = min(Frows[a] for a in ASER)
    note = ''
    if g1 == '合格' and max(Frows[a] for a in BSER) < shrink / 2:
        note = '⚠縮み注意：B系の F の行数が A系の最小の半分を下回る（領土を失った国が縮んで F から消えた可能性）'
    P(f"  max(B系)＝{mxb}・min(A系)＝{mn} → G1 {g1}  {note}")
P('')

P('## G2（採否）補給崩壊（人口 100 以上）の国×四半期の総数：max(B,N1B,N2B) ≦ max(A,N1,N2)。B0 は記録')
tot = {arm: sum(len(v) for v in coll[arm].values()) for arm in ARMS}
stop_k1000 = {}
for arm in ARMS:
    per = [len(coll[arm][s]) for s in SEEDS]
    k1000 = sum(1 for x in per if x >= 1000)
    stop_k1000[arm] = k1000
    top1 = 0; top1n = ''
    for s in SEEDS:
        c = collections.Counter(n for n, q in coll[arm][s])
        if c:
            n, k = c.most_common(1)[0]
            if k > top1:
                top1, top1n = k, f"種{s} {n}"
    P(f"  {arm}: 総数 {tot[arm]}・種ごと {per}・千件級の種 {k1000}/10・上位 1 国の寄与の最大 {top1}（{top1n}）")
g2 = None
if ASER and BSER:
    mxA = max(tot[a] for a in ASER); mnA = min(tot[a] for a in ASER); mxB = max(tot[a] for a in BSER)
    g2 = mxB <= mxA
    P(f"  max(B系)＝{mxB} ≦ max(A系)＝{mxA}（A系の幅 {mnA}〜{mxA}）→ G2 {'合格' if g2 else '外れ'}・max(B系)÷max(A系) {mxB/mxA:.2f}（倍以上の線＝2.00）")
    if 'B0' in tot:
        P(f"  （記録）B0＝{tot['B0']}（B0÷max(A系) {tot['B0']/mxA:.2f}）")
P('')

# ---------------- 群の分解 R4 ----------------
P('## R4 補給崩壊の件数を群で分ける: (a) F の行／(b) 建国の子（born＝1）／(c) 征服で錨が増えた国／(d) その他（(a) の定義は G1 の F）')
for arm in ARMS:
    g = collections.Counter()
    for s in SEEDS:
        d = A[arm][s]
        first_reg = {}
        for (n, q), v in sorted(d.items(), key=lambda t: t[0][1]):
            if n not in first_reg:
                first_reg[n] = v[REG]
        for (n, q) in coll[arm][s]:
            v = d.get((n, q))
            if v is None:
                g['不明'] += 1; continue
            if is_F(v):
                g['a F'] += 1
            elif v[BORN] == 1:
                g['b 子'] += 1
            elif v[REG] > first_reg.get(n, v[REG]) * (1 + 1e-6):
                g['c 征服'] += 1
            else:
                g['d その他'] += 1
    P(f"  {arm}: " + '・'.join(f"{k} {g[k]}" for k in ('a F', 'b 子', 'c 征服', 'd その他', '不明')))
P('')

# ---------------- S1 ----------------
P('## S1（採否）群III（追記1）＝四半期≦524 の A の行（capacity＞0）の 95% 以上で arableSum≦anchorUsed×(1+1e-9) かつ capBase が anchorUsed×f に 1e-9 で合う・A でも B でも移動（I）でない。国×種 3 以上の国。m_ν＝popK(腕)÷popK(A) の四半期 524 の中央')


def moved_I(d, n):
    for q in range(0, Q_H3 + 1):
        v = d.get((n, q))
        if v is not None and v[OLD] > 0 and abs(v[REG] / v[OLD] - 1) > 0.01:
            return True
    return False


def group_of(s, n):
    """'I' / 'II' / 'III' / None（四半期 524 に生きていない）"""
    dA = A['A'][s]
    if (n, Q_H3) not in dA:
        return None
    rows = [dA[(n, q)] for q in range(0, Q_H3 + 1) if (n, q) in dA and dA[(n, q)][CAP] > 0]
    if len(rows) < 8:
        return None
    mv = moved_I(dA, n)
    if 'B' in A:
        mv = mv or moved_I(A['B'][s], n)
    if mv:
        return 'I'
    lift = sum(1 for v in rows if abs(v[CB] - v[AR]) <= 1e-9 * max(v[AR], 1e-12)) / len(rows)
    c3 = sum(1 for v in rows if v[AR] <= v[USED] * (1 + 1e-9) and abs(v[CB] - v[USED] * fl(v[FR])) <= 1e-9 * max(v[CB], 1e-12)) / len(rows)
    if c3 >= 0.95:
        return 'III'
    if lift >= 0.10:
        return 'II'
    return 'その他'


grp = {}
for s in SEEDS:
    for (n, q) in A['A'][s]:
        if q == Q_H3:
            grp[(s, n)] = group_of(s, n)
bynat = collections.defaultdict(list)
for (s, n), g in grp.items():
    if g == 'III':
        bynat[n].append(s)
III = {n: ss for n, ss in bynat.items() if len(ss) >= 3}
P(f"  群の国×種（四半期 524 に生きている）: " + '・'.join(f"{k} {sum(1 for g in grp.values() if g == k)}" for k in ('I', 'II', 'III', 'その他')) + f"・群III で 3 種以上の国 {len(III)}")


def mq(arm, n, ss, q):
    vals = []
    for s in ss:
        a = A['A'][s].get((n, q)); b = A[arm][s].get((n, q))
        if a and b and a[POP] > 0:
            vals.append(b[POP] / a[POP])
    return med(vals)


s1 = '未評価'
if ASER and BSER and 'N1' in ARMS and 'N2' in ARMS and 'B' in ARMS:
    nB = nN1 = nN2 = 0
    P('  国\t種\tm(B)\tm(N1)\tm(N2)\t床\tB超過\tm(B0)')
    for n, ss in sorted(III.items()):
        mB = mq('B', n, ss, Q_H3); m1 = mq('N1', n, ss, Q_H3); m2 = mq('N2', n, ss, Q_H3)
        if None in (mB, m1, m2):
            continue
        fc_ = max(0.05, 2 * max(abs(m1 - 1), abs(m2 - 1)))
        ob = abs(mB - 1) > fc_
        nB += ob
        nN1 += abs(m1 - 1) > max(0.05, 2 * abs(m2 - 1))
        nN2 += abs(m2 - 1) > max(0.05, 2 * abs(m1 - 1))
        mb0 = mq('B0', n, ss, Q_H3) if 'B0' in ARMS else None
        P(f"  {n}\t{len(ss)}\t{mB:.3f}\t{m1:.3f}\t{m2:.3f}\t{fc_:.3f}\t{'超過' if ob else ''}\t{f(mb0,'.3f')}")
    if len(III) < 8:
        s1 = '未評価（群III の国が 8 国未満）'
    else:
        lim = max(5, nN1, nN2)
        s1 = '合格' if nB <= lim else '外れ'
        P(f"  n_B={nB}・自己ヌル n_N1={nN1}・n_N2={nN2}・線 max(5,n_N1,n_N2)={lim}")
P(f"→ S1 {s1}")
P('')

# ---------------- 止める線の材料 ----------------
P('## 止める線の材料（千件級の種 ≧2/10 は止める）')
for arm in ARMS:
    P(f"  {arm}: 千件級の種 {stop_k1000[arm]}/10")
P('')

# ---------------- R5 / R11 主要国の人口 ----------------
P('## R5／R11 主要国 15 国（A の四半期 524 の人口の中央の大きい順）の人口 B÷A・B0÷A・B÷B0 と旗 1 のヌルの幅 |N1B÷B−1|・|N2B÷B−1|・旗 0 のヌルの幅 |N1÷A−1|・|N2÷A−1|（中央・種）。群は国×種の多数')
cand = collections.defaultdict(list)
for s in SEEDS:
    for (n, q), v in A['A'][s].items():
        if q == Q_H3:
            cand[n].append(v[POP])
major = sorted(cand, key=lambda n: -(med(cand[n]) or 0))[:15]
for QQ in (Q_H3, Q_END):
    P(f'  --- 四半期 {QQ} ---')
    P('  国\t群\tA(K)\tB÷A\tB0÷A\tB÷B0\t|N1B÷B-1|\t|N2B÷B-1|\t|N1÷A-1|\t|N2÷A-1|')
    for n in major:
        def ratio(x, y):
            vals = []
            for s in SEEDS:
                a = A[x][s].get((n, QQ)); b = A[y][s].get((n, QQ))
                if a and b and b[POP] > 0:
                    vals.append(a[POP] / b[POP])
            return med(vals)
        gs = collections.Counter(grp.get((s, n)) for s in SEEDS if grp.get((s, n)))
        gl = gs.most_common(1)[0][0] if gs else '-'
        av = med([A['A'][s][(n, QQ)][POP] for s in SEEDS if (n, QQ) in A['A'][s]])
        rB = ratio('B', 'A') if 'B' in ARMS else None
        r0 = ratio('B0', 'A') if 'B0' in ARMS else None
        rbb = ratio('B', 'B0') if 'B' in ARMS and 'B0' in ARMS else None
        w1 = ratio('N1B', 'B'); w2 = ratio('N2B', 'B'); u1 = ratio('N1', 'A'); u2 = ratio('N2', 'A')
        ab = lambda x: None if x is None else abs(x - 1)
        P(f"  {n}\t{gl}\t{f(av,'.0f')}\t{f(rB,'.3f')}\t{f(r0,'.3f')}\t{f(rbb,'.3f')}\t{f(ab(w1),'.3f')}\t{f(ab(w2),'.3f')}\t{f(ab(u1),'.3f')}\t{f(ab(u2),'.3f')}")
P('')
P('## R11 群ごとの B÷B0（四半期 524・国×種の中央）と旗 1 のヌルの幅（|N1B÷B−1|・|N2B÷B−1| の国×種の中央）')
for gname in ('I', 'II', 'III', 'その他'):
    rr = []; ww = []
    for (s, n), g in grp.items():
        if g != gname:
            continue
        a = A['B'][s].get((n, Q_H3)); b = A['B0'][s].get((n, Q_H3)) if 'B0' in ARMS else None
        if a and b and b[POP] > 0:
            rr.append(a[POP] / b[POP])
        c1 = A['N1B'][s].get((n, Q_H3)) if 'N1B' in ARMS else None
        c2 = A['N2B'][s].get((n, Q_H3)) if 'N2B' in ARMS else None
        if a and c1 and c2 and a[POP] > 0:
            ww.append(max(abs(c1[POP] / a[POP] - 1), abs(c2[POP] / a[POP] - 1)))
    P(f"  群{gname}: 国×種 {len(rr)}・B÷B0 の中央 {f(med(rr),'.3f')}（四分位 {f(sorted(rr)[len(rr)//4] if rr else None,'.3f')}〜{f(sorted(rr)[3*len(rr)//4] if rr else None,'.3f')}）・旗 1 のヌルの幅の中央 {f(med(ww),'.4f')}")
P('')

# ---------------- HL2 ----------------
P('## HL2 領土を分けた倍率 coreK÷anchorRegions（本国の州の今の人口÷それらの州の開幕の人口）の中央（種）。本国の州がある種が 3 種以上の国')
for QQ in (Q_1850, Q_H3):
    P(f'  --- 四半期 {QQ} ---')
    P('  国\t' + '\t'.join(ARMS))
    for n in major:
        cells = []
        for arm in ARMS:
            vals = []
            for s in SEEDS:
                v = A[arm][s].get((n, QQ))
                if v and v[REG] > 0:
                    vals.append(v[CORE] / v[REG])
            cells.append(f(med(vals), '.3f') if len(vals) >= 3 else '-')
        P(f"  {n}\t" + '\t'.join(cells))
P('')

# ---------------- R1 / R2 / R3 / R8 ----------------
P('## R1 建国の子（born＝1）の r＝capacity÷popK の四分位と人口の軌道')
for arm in ARMS:
    rs = [v[CAP] / v[POP] for d in A[arm].values() for v in d.values() if v[BORN] == 1 and v[CAP] > 0 and v[POP] > 0]
    rs.sort()
    qf = lambda p: rs[int(p * (len(rs) - 1))] if rs else None
    traj = {0: [], 40: [], 200: []}
    nborn = 0
    for s, d in A[arm].items():
        first = {}
        for (n, q_), v in d.items():
            if v[BORN] == 1 and (n not in first or q_ < first[n]):
                first[n] = q_
        for n, q0 in first.items():
            nborn += 1
            for dq in traj:
                v = d.get((n, q0 + dq))
                if v:
                    traj[dq].append(v[POP])
    P(f"  {arm}: 行 {len(rs)}・r の四分位 {f(qf(0.25))}/{f(qf(0.5))}/{f(qf(0.75))}・建国の国×種 {nborn}・人口の中央 建国 {f(med(traj[0]),'.0f')} / +10年 {f(med(traj[40]),'.0f')} / +50年 {f(med(traj[200]),'.0f')}")
P('')
P('## R2 錨 0 の国（anchorRegions＜1e-9・capacity＞0）の capBase÷arableSum の分布')
for arm in ARMS:
    rr = [v[CB] / v[AR] for d in A[arm].values() for v in d.values() if v[REG] < 1e-9 and v[CAP] > 0 and v[AR] > 0]
    rr.sort()
    qf = lambda p: rr[int(p * (len(rr) - 1))] if rr else None
    one = sum(1 for x in rr if abs(x - 1) <= 1e-9)
    P(f"  {arm}: 行 {len(rr)}・1.0 の行 {one}（{(one/len(rr)*100 if rr else 0):.1f}%）・四分位 {f(qf(0.25),'.3f')}/{f(qf(0.5),'.3f')}/{f(qf(0.75),'.3f')}")
P('')
P('## R3 征服で錨が増えた国×四半期（B0・B 系・前の行より anchorRegions が 1e-9 超増えた）')
for arm in B1SER:
    inc_n = 0; inc_sum = 0.0; per_nation = collections.Counter()
    for s, d in A[arm].items():
        by_n = collections.defaultdict(list)
        for (n, q), v in d.items():
            by_n[n].append((q, v[REG]))
        for n, lst in by_n.items():
            lst.sort()
            for (q0, a0), (q1, a1) in zip(lst, lst[1:]):
                if q1 == q0 + 1 and a1 > a0 * (1 + 1e-9) + 1e-9:
                    inc_n += 1; inc_sum += a1 - a0; per_nation[(s, n)] += a1 - a0
    top = '・'.join(f"種{s} {n} +{x:.0f}" for (s, n), x in per_nation.most_common(3))
    P(f"  {arm}: 件数 {inc_n}・合計 {inc_sum:.0f}・上位 {top}")
P('')
P('## R8 近似 0 行（capacity≦1e-9 かつ popK＞0・建国の子の最初の行を除く）')
for arm in ARMS:
    z = 0
    for s, d in A[arm].items():
        for (n, q), v in d.items():
            if v[CAP] <= 1e-9 and v[POP] > 0 and not (v[BORN] == 1 and (n, q - 1) not in d):
                z += 1
    P(f"  {arm}: {z} 行")
P('')

# ---------------- HL1 ----------------
P('## HL1 成績表の〈人口: 成長倍率〉の行（併合の持ち込みを除く 1783→1914・10 種の平均）と幅 PopGrowthOk に対する距離 D（英仏墺西。墺は領土の変化が大きい＝HL2 を主に読む）')
BAND = {'フランス': (1.25, 1.7), 'オーストリア': (1.8, 2.6), 'イギリス': (2.6, 4.3), 'スペイン': (1.6, 2.2), 'ポルトガル': (1.5, 2.0), '日本': (1.7, 2.4), '清': (1.1, 1.9)}
for arm in ARMS:
    p = f"{MEAS}/b10_20261016_an2{arm}_ins"
    if not os.path.exists(p):
        P(f"  {arm}: 成績表なし")
        continue
    txt = open(p, encoding='utf-8', errors='replace').read()
    m = re.search(r'人口: 成長倍率の一致[^\n]*', txt)
    if not m:
        P(f"  {arm}: 行なし"); continue
    line = m.group(0)
    vals = dict(re.findall(r'([^\s×]+)×([0-9.]+)', line))
    D = 0.0; ds = []
    for nm in ('イギリス', 'フランス', 'オーストリア', 'スペイン'):
        if nm in vals:
            g = float(vals[nm]); lo, hi = BAND[nm]
            d_ = math.log(lo / g) if g < lo else (math.log(g / hi) if g > hi else 0.0)
            d_ = abs(d_); D += d_; ds.append(f"{nm} {d_:.2f}")
    P(f"  {arm}: {line.strip()}")
    P(f"      D(英仏墺西)＝{D:.2f}（" + '・'.join(ds) + '）')
P('')

# ---------------- HL3 ----------------
P('## HL3 領土不変の窓の年率（pop_s の N 行の (出生率−死亡率)・月数で重みづけ・種の中央）。史実【推】: 西 Q16〜Q296 約 0.59%/年・仏 Q24〜Q272 約 0.39%/年・普 Q132〜Q324 約 1.3%/年・サルデーニャ Q152〜Q300 約 0.64%/年（移民を含む純増）')
WIN = [('スペイン', 16, 296), ('フランス', 24, 272), ('プロイセン', 132, 324), ('サルデーニャ', 152, 300)]


def pop_rate(arm, s, nation, q0, q1):
    p = f"{W}/out_{arm}_10/ins/pop_s{s}.tsv"
    num = den = 0.0; seen = 0
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t')
        h = next(r)
        ix = {c: i for i, c in enumerate(h)}
        isc, iq, inat, im, icb, icd = ix['scope'], ix['quarter'], ix['nation'], ix['monthsCount'], ix['cbrPerMille'], ix['cdrPerMille']
        for ln in r:
            if ln[isc] != 'N' or ln[inat] != nation:
                continue
            q = int(ln[iq])
            if q < q0 or q > q1:
                continue
            try:
                m = float(ln[im]); cb = float(ln[icb]); cd = float(ln[icd])
            except Exception:
                continue
            if m > 0 and not (math.isnan(cb) or math.isnan(cd)):
                num += (cb - cd) * m; den += m; seen += 1
    return (num / den / 10.0) if den > 0 else None   # %/年


for nm, q0, q1 in WIN:
    cells = []
    for arm in ARMS:
        vals = [x for x in (pop_rate(arm, s, nm, q0, q1) for s in SEEDS) if x is not None]
        cells.append(f"{arm} {f(med(vals),'.2f')}%({len(vals)}種)")
    P(f"  {nm} Q{q0}〜Q{q1}: " + '　'.join(cells))
P('')

with open(OUT, 'w', encoding='utf-8') as fh:
    fh.write('\n'.join(out) + '\n')
print('\n'.join(out))
