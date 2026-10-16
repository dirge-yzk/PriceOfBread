# (369) 後半: 扶養力の錨を州と一緒に動かす 腕 A・B・N1・N2（計器つき10種）を事前登録（追記2まで）の線で読む。読むだけ。
# 使い方: PYTHONUTF8=1 python an_an1.py <作業フォルダ(例 D:/PoB_work/an)> <出力txt> [腕の並び=A,B,N1,N2]
#   各腕の ins フォルダ = <作業フォルダ>/out_<腕>_10/ins（anchor_s<種>.tsv と nation_s<種>.tsv を読む）
# 線: M1・M2・M3・H1・H2・H3（採否）／H4（ng.sh と hollow は別）・R1〜R8・止める線の物差しと群の分解（記録）。
# 定義は measurements/an_20261016_prereg.txt の [4] と追記1・追記2。数字の線は読み手に書いてあるだけで、結果を見て動かさない。
# H3 の〈錨が動かない〉の許容差：登録は 1e-9。丸めの累積で該当が 10 国未満になったら 1e-6 に切り替える（読み手が両方を印字し、使った方を書く）。
import csv, sys, collections, statistics as st

W = sys.argv[1]
OUT = sys.argv[2]
ARMS = sys.argv[3].split(',') if len(sys.argv) > 3 else ['A', 'B', 'N1', 'N2']
SEEDS = range(1, 11)
Q_H3 = 524      # 1914 年
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


def anchor_rows(arm, s):
    """anchor_s の行を (nation, quarter) -> dict で返す。"""
    p = f"{W}/out_{arm}_10/ins/anchor_s{s}.tsv"
    d = {}
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t')
        h = next(r)
        ix = {c: i for i, c in enumerate(h)}
        for ln in r:
            q = int(ln[ix['quarter']])
            d[(ln[ix['nation']], q)] = (
                int(ln[ix['born']]), float(ln[ix['popK']]), float(ln[ix['anchorOld']]), float(ln[ix['anchorRegions']]),
                float(ln[ix['anchorUsed']]), int(ln[ix['regionsN']]), int(ln[ix['activeRegionsN']]), int(ln[ix['anchorRegionsN']]),
                float(ln[ix['capacity']]), float(ln[ix['capBase']]), float(ln[ix['arableSum']]))
    return d


BORN, POP, OLD, REG, USED, RN, ACT, ARN, CAP, CB, AR = range(11)


def collapse_rows(arm, s):
    """nation_s から (nation, quarter) の補給崩壊（人口100以上・supplyFillShare<0.3）を返す。"""
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


# ---------------- 読み込み ----------------
A = {}   # arm -> {s: rows}
for arm in ARMS:
    A[arm] = {s: anchor_rows(arm, s) for s in SEEDS}

P('# (369) 後半: 扶養力の錨の腕を事前登録（追記2まで）の線で読む（計器つき 10 種・読むだけ）')
P('腕: ' + ' / '.join(ARMS) + '（入力 ' + W + '/out_<腕>_10/ins）')
P('行数 anchor_s: ' + ' '.join(f"{arm}={sum(len(v) for v in A[arm].values())}" for arm in ARMS))
P('')

# ---------------- M1 ----------------
P('## M1（採否）anchorUsed は人口の段が実際に使った錨。A は旧の錨・B は州の和に 1e-9 で合う（capacity が 0 でない行・99% 以上）／錨が動いた行（|regions÷old−1|＞1%）で A は old に近く B は regions に近い（95% 以上）')
m1_ok = True
for arm in ARMS:
    tot = hit = disc = disc_hit = skipped = 0
    miss = []
    for s, d in A[arm].items():
        for (n, q), v in d.items():
            if not (v[CAP] > 0):
                skipped += 1
                continue
            tot += 1
            ref = v[REG] if arm == 'B' else v[OLD]
            if rel(v[USED], ref) <= 1e-9:
                hit += 1
            elif len(miss) < 5:
                miss.append((s, n, q, v[USED], v[OLD], v[REG]))
            if v[OLD] > 0 and abs(v[REG] / v[OLD] - 1) > 0.01:
                disc += 1
                if abs(v[USED] - v[REG]) < abs(v[USED] - v[OLD]) if arm == 'B' else abs(v[USED] - v[OLD]) < abs(v[USED] - v[REG]):
                    disc_hit += 1
    flag = None
    if arm in ('B', 'A'):
        a = hit / tot if tot else 0
        b = disc_hit / disc if disc else 1.0
        ok = a >= 0.99 and b >= 0.95
        if arm in ('A', 'B'):
            m1_ok &= ok
        P(f"  {arm}: 一致 {hit}/{tot}（{a*100:.3f}%）・区別 {disc_hit}/{disc}（{b*100:.1f}%）・capacity＝0 の行 {skipped}（除外）→ {'合格' if ok else '外れ'}")
        for m in miss:
            P(f"     外れ例 種{m[0]} {m[1]} Q{m[2]} used={m[3]:.6g} old={m[4]:.6g} regions={m[5]:.6g}")
    else:
        P(f"  {arm}: 一致 {hit}/{tot}（旗 0 の腕・旧の錨との相対 1e-9）・capacity＝0 の行 {skipped}")
P(f"→ M1 {'合格' if m1_ok else '外れ（形のバグ＝形を直してから測り直す）'}")
P('')

# ---------------- M2 ----------------
P('## M2（採否）B の最初の四半期の行で全国の anchorRegions と anchorOld の相対差が 1e-9 以内')
bad = tot = 0
for s, d in A['B'].items():
    for (n, q), v in d.items():
        if q != 0:
            continue
        tot += 1
        if rel(v[REG], v[OLD]) > 1e-9:
            bad += 1
P(f"  国×種 {tot}・外れ {bad} → {'合格' if bad == 0 and tot > 0 else '外れ'}")
P('')

# ---------------- M3 ----------------
P('## M3（採否）何もしない機構が緑にならない確かめ')
moved = 0; land_vs_anchor = 0; moved_total = 0
for s, d in A['B'].items():
    for (n, q), v in d.items():
        if v[OLD] > 0 and abs(v[REG] / v[OLD] - 1) >= 0.01:
            moved_total += 1
            if v[CB] > v[AR] * (1 + 1e-6):
                land_vs_anchor += 1
m3i = moved_total > 0
# (ii) A に残りの国の行
def remaining(v, born_zero=False):
    return v[ACT] >= 1 and v[ARN] == 0 and v[CAP] > 0 and v[POP] > 0 and (v[BORN] == 0 if born_zero else True)
nrem_A = sum(1 for d in A['A'].values() for v in d.values() if remaining(v))
nrem_A0 = sum(1 for d in A['A'].values() for v in d.values() if remaining(v, True))
P(f"  (i) B で錨が 1% 以上動いた国×四半期 {moved_total} → {'合格' if m3i else '外れ'}")
P(f"  (ii) A の残りの国の行（登録時の定義）{nrem_A}・追記1 の定義（born＝0）{nrem_A0} → {'実在' if nrem_A > 0 else '無し（H1 は記録のみ）'}")
P(f"  (iii) 錨が 1% 以上動き、かつ capBase＞arableSum×(1+1e-6)（錨の側が勝つ）行 {land_vs_anchor}/{moved_total}（{(land_vs_anchor/moved_total*100 if moved_total else 0):.1f}%）→ {'合格' if land_vs_anchor > 0 else '外れ（形が扶養力に効いていない）'}")
m3_ok = m3i and land_vs_anchor > 0
P('')

# ---------------- H1 ----------------
P('## H1（採否）残りの国（activeRegionsN≧1・anchorRegionsN＝0・capacity＞0）の r＝capacity÷popK の中央値（行を種ごとに集めた全体）。B で 3 以下かつ A の 1/5 以下。A の行が 30 未満なら記録のみ')
h1res = {}
for label, bz in (('主（登録時・born を問わない）', False), ('併記（追記1・born＝0）', True)):
    rs = {}
    for arm in ARMS:
        vals = [v[CAP] / v[POP] for d in A[arm].values() for v in d.values() if remaining(v, bz)]
        rs[arm] = vals
    mA, mB = med(rs.get('A', [])), med(rs.get('B', []))
    nA = len(rs.get('A', []))
    if nA < 30:
        verdict = '記録のみ（A の行が 30 未満）'
    else:
        verdict = '合格' if (mB is not None and mB <= 3 and mB <= mA / 5) else '外れ'
    h1res[label] = verdict
    P(f"  {label}: " + ' / '.join(f"{arm} 行{len(rs[arm])}・中央{f(med(rs[arm]))}" for arm in ARMS) + f" → {verdict}")
if h1res['主（登録時・born を問わない）'] != h1res['併記（追記1・born＝0）']:
    P('  ★主と併記で判定が食い違う（報告の先頭に書く・採否は主の定義）')
P('')

# ---------------- H2 / 補給崩壊 / R8 ----------------
P('## H2（採否）補給崩壊（人口 100 以上・supplyFillShare＜0.3）の国×四半期の総数：B ≦ max(A, N1, N2)（第 2 節の上位 1 国は R8 の記録）')
coll = {}   # arm -> {s: [(n,q)]}
for arm in ARMS:
    coll[arm] = {s: collapse_rows(arm, s) for s in SEEDS}
tot = {arm: sum(len(v) for v in coll[arm].values()) for arm in ARMS}
for arm in ARMS:
    per = [len(coll[arm][s]) for s in SEEDS]
    k1000 = sum(1 for x in per if x >= 1000)
    top1 = 0; top1n = ''
    for s in SEEDS:
        c = collections.Counter(n for n, q in coll[arm][s])
        if c:
            n, k = c.most_common(1)[0]
            if k > top1:
                top1, top1n = k, f"種{s} {n}"
    P(f"  {arm}: 総数 {tot[arm]}・種ごと {per}・千件級の種 {k1000}/10・上位 1 国の寄与の最大 {top1}（{top1n}）")
nulls = [tot[a] for a in ARMS if a in ('A', 'N1', 'N2')]
mx = max(nulls)
if 'B' in ARMS:
    h2 = tot['B'] <= mx
    P(f"  B {tot['B']} ≦ max(A,N1,N2)＝{mx}（ヌル腕の幅 {min(nulls)}〜{mx}・幅 {mx-min(nulls)}）→ {'合格' if h2 else '外れ'}・B÷max(ヌル) {tot['B']/mx:.2f}（倍以上の線＝2.00）")
P('  R8: N1 と N2 の総数の差 ' + (str(abs(tot['N1'] - tot['N2'])) if 'N1' in tot and 'N2' in tot else '-'))
P('')

# ---------------- H3 ----------------
P('## H3（採否）錨が動かない国の人口（四半期 524）。国ごとに m_c＝popK(腕)÷popK(A) の種の中央・床 f_c＝max(0.05, 2×max|N1,N2 の m_c−1|)。超過国数 n_B ≦ max(5, n_N1, n_N2)')


def eligible_sets(tol):
    """(国,種) で A と B の両方の anchor_s が Q_H3 まで |reg/old−1|＜tol・4 腕とも Q_H3 に生きている。"""
    res = collections.defaultdict(list)
    names = set()
    for s in SEEDS:
        for (n, q) in A['A'][s]:
            if q == Q_H3:
                names.add((s, n))
    for (s, n) in names:
        okk = True
        for arm in ARMS:
            if (n, Q_H3) not in A[arm][s]:
                okk = False; break
        if not okk:
            continue
        for arm in ('A', 'B'):
            if arm not in ARMS:
                continue
            for q in range(0, Q_H3 + 1):
                v = A[arm][s].get((n, q))
                if v is None:
                    continue
                if v[OLD] > 0 and abs(v[REG] / v[OLD] - 1) >= tol:
                    okk = False; break
                if v[OLD] <= 0 and v[REG] > 0:
                    okk = False; break
            if not okk:
                break
        if okk:
            res[n].append(s)
    return {n: ss for n, ss in res.items() if len(ss) >= 3}


use_tol = 1e-9
el = eligible_sets(1e-9)
P(f"  該当国（許容差 1e-9・種 3 つ以上）{len(el)}")
if len(el) < 10:
    el6 = eligible_sets(1e-6)
    P(f"  1e-9 では 10 国未満 → 1e-6 に切り替え（該当 {len(el6)}）")
    el = el6; use_tol = 1e-6


def mc(arm, n, ss):
    return med([A[arm][s][(n, Q_H3)][POP] / A['A'][s][(n, Q_H3)][POP] for s in ss if A['A'][s][(n, Q_H3)][POP] > 0])


rows = []
for n, ss in sorted(el.items()):
    rows.append((n, len(ss), {arm: mc(arm, n, ss) for arm in ARMS if arm != 'A'}))
nB = nN1 = nN2 = 0
P('  国\t種\tm(B)\tm(N1)\tm(N2)\t床\tB超過')
over = []
for n, k, m in rows:
    mB = m.get('B'); m1 = m.get('N1'); m2 = m.get('N2')
    if None in (mB, m1, m2):
        continue
    fc = max(0.05, 2 * max(abs(m1 - 1), abs(m2 - 1)))
    ob = abs(mB - 1) > fc
    nB += ob
    nN1 += abs(m1 - 1) > max(0.05, 2 * abs(m2 - 1))
    nN2 += abs(m2 - 1) > max(0.05, 2 * abs(m1 - 1))
    if ob:
        over.append((n, mB, fc))
    P(f"  {n}\t{k}\t{mB:.3f}\t{m1:.3f}\t{m2:.3f}\t{fc:.3f}\t{'超過' if ob else ''}")
if 'B' in ARMS and 'N1' in ARMS and 'N2' in ARMS:
    lim = max(5, nN1, nN2)
    P(f"  n_B={nB}・自己ヌル n_N1={nN1}・n_N2={nN2}・線 max(5,n_N1,n_N2)={lim} → {'合格' if nB <= lim else '外れ'}（許容差 {use_tol}）")
    for n, mB, fc in over:
        P(f"     超過: {n} m(B)={mB:.3f} 床={fc:.3f}")
P('')

# 四半期 667 の記録
P('## 記録: H3 と同じ国の四半期 667（1950 年）')
for n, ss in sorted(el.items()):
    try:
        vals = {}
        for arm in ARMS:
            vals[arm] = med([A[arm][s][(n, Q_END)][POP] / A['A'][s][(n, Q_END)][POP] for s in ss if (n, Q_END) in A[arm][s] and (n, Q_END) in A['A'][s]])
        P(f"  {n}\t" + '\t'.join(f"{arm}={f(vals[arm],'.3f')}" for arm in ARMS if arm != 'A'))
    except Exception:
        pass
P('')

# ---------------- R1 / R2 ----------------
P('## R1 建国の子（born＝1）の r の分布と人口の軌道（A と B）')
for arm in ARMS:
    if arm not in ('A', 'B'):
        continue
    rs = [v[CAP] / v[POP] for d in A[arm].values() for v in d.values() if v[BORN] == 1 and v[CAP] > 0 and v[POP] > 0]
    rs.sort()
    q = lambda p: rs[int(p * (len(rs) - 1))] if rs else None
    P(f"  {arm}: 行 {len(rs)}・r の四分位 {f(q(0.25))}/{f(q(0.5))}/{f(q(0.75))}")
    # 軌道: 国×種ごとの最初の born=1 の行から +0/+40/+200 四半期の popK の中央
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
    P(f"     建国の国×種 {nborn}・人口の中央 建国 {f(med(traj[0]),'.0f')} / +10年 {f(med(traj[40]),'.0f')} / +50年 {f(med(traj[200]),'.0f')}（行 {len(traj[0])}/{len(traj[40])}/{len(traj[200])}）")
P('')
P('## R2 植民地格の州だけの子の国（born＝1・anchorRegionsN＝0）の人口の軌道と、食料の経路が無い行（capBase＝arableSum）の割合')
for arm in ARMS:
    if arm not in ('A', 'B'):
        continue
    rows_ = [v for d in A[arm].values() for v in d.values() if v[BORN] == 1 and v[ARN] == 0 and v[ACT] >= 1 and v[CAP] > 0 and v[POP] > 0]
    noFood = sum(1 for v in rows_ if rel(v[CB], v[AR]) <= 1e-9)
    traj = {0: [], 40: [], 200: []}
    nn = 0
    for s, d in A[arm].items():
        first = {}
        for (n, q_), v in d.items():
            if v[BORN] == 1 and v[ARN] == 0 and v[ACT] >= 1 and (n not in first or q_ < first[n]):
                first[n] = q_
        for n, q0 in first.items():
            nn += 1
            for dq in traj:
                v = d.get((n, q0 + dq))
                if v:
                    traj[dq].append(v[POP])
    P(f"  {arm}: 行 {len(rows_)}・capBase＝arableSum の行 {noFood}（{(noFood/len(rows_)*100 if rows_ else 0):.1f}%）・国×種 {nn}・人口の中央 {f(med(traj[0]),'.0f')} / +10年 {f(med(traj[40]),'.0f')} / +50年 {f(med(traj[200]),'.0f')}")
P('')

# ---------------- R3（B のみ）----------------
P('## R3 征服で錨が増えた国×四半期（B のみ・前の行より anchorRegions が 1e-9 超増えた）')
inc_n = 0; inc_sum = 0.0; per_nation = collections.Counter()
for s, d in A['B'].items():
    by_n = collections.defaultdict(list)
    for (n, q), v in d.items():
        by_n[n].append((q, v[REG]))
    for n, lst in by_n.items():
        lst.sort()
        for (q0, a0), (q1, a1) in zip(lst, lst[1:]):
            if q1 == q0 + 1 and a1 > a0 * (1 + 1e-9) + 1e-9:
                inc_n += 1; inc_sum += a1 - a0; per_nation[(s, n)] += a1 - a0
P(f"  件数 {inc_n}・増えた量の合計 {inc_sum:.0f}")
for (s, n), x in per_nation.most_common(5):
    P(f"     上位: 種{s} {n} +{x:.0f}")
P('')

# ---------------- R4 ----------------
P('## R4（近似）B で capacity＝0 かつ popK＞0 の行（人口の段が飛んだ月を直接は数えられない）')
z = 0
for s, d in A['B'].items():
    for (n, q), v in d.items():
        if v[CAP] == 0 and v[POP] > 0 and not (v[BORN] == 1 and (n, q - 1) not in d):
            z += 1
P(f"  {z} 行")
P('')

# ---------------- R6 ----------------
P('## R6 残りの国（登録時の定義）の数と継続期間（国×種・四半期の合計）')
for arm in ARMS:
    per = collections.defaultdict(int)
    for s, d in A[arm].items():
        for (n, q), v in d.items():
            if remaining(v):
                per[(s, n)] += 1
    por = sum(k for (s, n), k in per.items() if n == 'ポルトガル')
    P(f"  {arm}: 国×種 {len(per)}・四半期の合計 {sum(per.values())}・最長 {max(per.values()) if per else 0}・ポルトガル {por}")
P('')

# ---------------- 群の分解（[7] ⑧）----------------
P('## 群の分解（追記2 ⑧）補給崩壊の件数を群で分ける。(a) 残りの国（born＝0・anchorRegionsN＝0）／(b) 建国の子（born＝1）／(c) 征服で錨が増えた国（anchorRegions が Q0 の 1.000001 倍超）／(d) その他')
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
            if v[BORN] == 0 and v[ARN] == 0:
                g['a 残り'] += 1
            elif v[BORN] == 1:
                g['b 子'] += 1
            elif v[REG] > first_reg.get(n, v[REG]) * (1 + 1e-6):
                g['c 征服'] += 1
            else:
                g['d その他'] += 1
    P(f"  {arm}: " + '・'.join(f"{k} {g[k]}" for k in ('a 残り', 'b 子', 'c 征服', 'd その他', '不明')))

with open(OUT, 'w', encoding='utf-8') as fh:
    fh.write('\n'.join(out) + '\n')
print('\n'.join(out))
