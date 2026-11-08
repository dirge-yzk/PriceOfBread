# (967) c86tq 読むだけの台本：旗 0 の生（keep_961 の base_ins・n1・n2・a11）から、土地税の算定の基準の AI の規則
#   A〈1 年先の請求が大きい方〉・B〈A の戻しの判定から期待インフレを外す〉・C〈A を財政の危機の四半期だけ評価〉の
#   〈置くはずの目標〉の切り替えの回数と、危機の四半期の数を国×種で数える。keep_961 は読むだけ。腕もビルドも回さない。
# 表：money_s（期待インフレ・四半期末＝World.UpdatePriceLevel は QuarterEnd）・fiscal_s（payRateShare・月）・
#     nation_s（payRateBelowOneMonths ほか・四半期）・landbase_s（baseRPerMonth＝国の基数 R・四半期の 3 か月目）。
# 時点の対応（この台本が検算して名乗る）：
#   fiscal_s の monthIndex＝(int)(Tick/30)・Tick++（Simulation.cs:1726）が MonthSettle（:1732）より前＝助走の後の 0 始まりの月 k の行は monthIndex k+1。
#   最初の月（k=0）は HaveLedger（Instruments.cs:1535）で行が無い。四半期 q の月は monthIndex 3q+1〜3q+3。
#   Government.Step は QuarterStart（Simulation.cs:1807）＝四半期 q の頭の FiscalFundingRatio は四半期 q-1 の最後の月（monthIndex 3q）の値（Simulation.cs:2431）。
#   規則の判定（四半期 q の頭）：π＝money の行 q(q-1)（q=0 は助走の最後の行 w19）・R＝landbase の行 q-1（q=0 は行 0 で代用＝名乗る）。
# 影の凍結は国まるごと（RGO ごとでない）＝P1／H 系の RGO ごとの漸進とは違う（上限の物差し・腕の値の予言ではない）。
import sys, os, statistics
ROOT = sys.argv[1]          # D:/PoB_work/keep_961
OUT = sys.argv[2]
ARMS = [('base_ins', range(1, 11)), ('n1', range(1, 11)), ('n2', range(1, 11)), ('a11', range(11, 21))]
FIXED0 = {'イギリス', '清'}   # 枝 origin/c85lt の world.json の landTaxBasis "Fixed"（開幕の定額の国）
ANCHOR = {'フランス': ('P->F', 1790), 'プロイセン': ('P->F', 1861), '日本': ('P->F', 1873), '清': ('F->P', 1941)}

def rows(p):
    with open(p, 'rb') as f:
        data = f.read().decode('utf-8').split('\n')
    head = data[0].rstrip('\r').split('\t')
    return head, data[1:]

def fnum(s):
    try:
        return float(s)
    except ValueError:
        return float('nan')

out = []
def W(s=''):
    out.append(s)

results = {}   # (arm, seed, nation) -> dict
checks = []
for arm, seeds in ARMS:
    for s in seeds:
        d = os.path.join(ROOT, arm)
        # money
        h, L = rows(os.path.join(d, 'money_s%d.tsv' % s))
        iN, iT, iY, iP = h.index('nation'), h.index('time'), h.index('year'), h.index('expectedInflationPerYear')
        pw, pq, yq = {}, {}, {}
        for ln in L:
            if not ln.strip():
                continue
            c = ln.rstrip('\r').split('\t')
            t = c[iT]
            if t.startswith('w'):
                pw.setdefault(c[iN], []).append(fnum(c[iP]))
            else:
                pq.setdefault(c[iN], {})[int(t[1:])] = fnum(c[iP])
                yq.setdefault(c[iN], {})[int(t[1:])] = int(float(c[iY]))
        # fiscal
        h, L = rows(os.path.join(d, 'fiscal_s%d.tsv' % s))
        iM, iN2, iR = h.index('monthIndex'), h.index('nation'), h.index('payRateShare')
        pr = {}
        for ln in L:
            if not ln.strip():
                continue
            c = ln.rstrip('\r').split('\t')
            pr.setdefault(c[iN2], {})[int(c[iM])] = fnum(c[iR])
        # nation
        h, L = rows(os.path.join(d, 'nation_s%d.tsv' % s))
        want = ['quarter', 'nation', 'payRateBelowOneMonths', 'landTaxEndShare', 'shortfallMonths', 'borrowedMeanPerMonth',
                'bondDebtToGdpYearMeanRatio', 'govDebtStock', 'polityCode', 'atWarMonths', 'civilWarMonths', 'nominalGdpPerYear']
        ix = {k: h.index(k) for k in want}
        nat = {}
        for ln in L:
            if not ln.strip():
                continue
            c = ln.rstrip('\r').split('\t')
            nat.setdefault(c[ix['nation']], {})[int(c[ix['quarter']])] = {k: (c[ix[k]] if k in ('nation', 'polityCode') else fnum(c[ix[k]])) for k in want}
        # landbase
        h, L = rows(os.path.join(d, 'landbase_s%d.tsv' % s))
        iQ, iN3, iB = h.index('quarter'), h.index('nation'), h.index('baseRPerMonth')
        rb = {}
        for ln in L:
            if not ln.strip():
                continue
            c = ln.rstrip('\r').split('\t')
            rb.setdefault(c[iN3], {})[int(c[iQ])] = fnum(c[iB])
        for n in sorted(pq):
            Q = max(pq[n]) + 1
            # 検算 1：monthIndex の四半期ごとの行数
            cnt = {}
            for m in pr.get(n, {}):
                cnt[(m - 1) // 3] = cnt.get((m - 1) // 3, 0) + 1
            ok1 = cnt.get(0) == 2 and all(cnt.get(q) == 3 for q in range(1, Q)) and sum(cnt.values()) == 3 * Q - 1
            # 検算 2：nation_s の payRateBelowOneMonths と fiscal の月の数え（q>=1）
            bad2 = 0
            for q in range(1, Q):
                k = sum(1 for m in (3 * q + 1, 3 * q + 2, 3 * q + 3) if pr[n].get(m, 1.0) < 1)
                if n in nat and q in nat[n] and int(nat[n][q]['payRateBelowOneMonths']) != k:
                    bad2 += 1
            checks.append((arm, s, n, ok1, bad2))
            # 危機の述語
            c1 = [False] * Q   # 本線：四半期 q の頭の r＝前の四半期の最後の月（monthIndex 3q）＜1
            c2 = [False] * Q   # 別の列：前の四半期のどれかの月が r＜1（nation_s）
            for q in range(1, Q):
                c1[q] = pr[n].get(3 * q, 1.0) < 1
                c2[q] = n in nat and (q - 1) in nat[n] and nat[n][q - 1]['payRateBelowOneMonths'] > 0
            def piq(q):
                if q == 0:
                    return pw[n][-1] if pw.get(n) else float('nan')
                return pq[n][q - 1]
            def Rq(q):
                return rb[n].get(max(0, q - 1), float('nan'))
            # 規則 A0（(960) の再現：符号の反転）
            negw = [x < 0 for x in pw.get(n, [])] + [pq[n][q] < 0 for q in range(Q)]
            negq = [pq[n][q] < 0 for q in range(Q)]
            a0w = sum(1 for a, b in zip(negw, negw[1:]) if a != b)
            a0q = sum(1 for a, b in zip(negq, negq[1:]) if a != b)
            def run(rule, crisis):
                S = 'F' if n in FIXED0 else 'P'
                f = Rq(0) if S == 'F' else None
                sw = []
                for q in range(Q):
                    if crisis is not None and not crisis[q]:
                        continue
                    p, R = piq(q), Rq(q)
                    if not (p == p) or not (R == R):
                        continue
                    if S == 'P':
                        if p < 0:
                            S, f = 'F', R; sw.append((q, 'P->F'))
                    else:
                        back = (R * (1 + p) > f) if rule == 'A' else (R > f)
                        if back:
                            S, f = 'P', None; sw.append((q, 'F->P'))
                return sw
            r = {}
            r['A0w'], r['A0q'] = a0w, a0q
            r['A'] = run('A', None); r['B'] = run('B', None)
            r['C1'] = run('A', c1); r['C2'] = run('A', c2); r['CB1'] = run('B', c1)
            r['nC1'], r['nC2'] = sum(c1), sum(c2)
            r['tau0'] = sum(1 for q in range(Q) if n in nat and q in nat[n] and not (nat[n][q]['landTaxEndShare'] > 0))
            r['yq'] = yq[n]
            r['Q'] = Q
            # 日本 1873 の候補の述語（四半期の数・全期間と 1865〜1880）
            if n == '日本':
                cand = {}
                pol = [nat[n][q]['polityCode'] for q in range(Q) if q in nat[n]]
                for q in range(Q):
                    if q not in nat[n]:
                        continue
                    y = yq[n][q]; v = nat[n][q]
                    win = 1865 <= y <= 1880
                    def add(k, b):
                        a = cand.setdefault(k, [0, 0]); a[0] += int(b); a[1] += int(b and win)
                    add('C1 r<1（前の四半期の最後の月）', c1[q])
                    add('C2 前の四半期のどれかの月 r<1', c2[q])
                    add('shortfallMonths>0（払えない額のある月）', v['shortfallMonths'] > 0)
                    add('borrowedMeanPerMonth>0（借りた月）', v['borrowedMeanPerMonth'] > 0)
                    add('国債÷年 GDP>0.5', v['bondDebtToGdpYearMeanRatio'] > 0.5)
                    add('polityCode が前の四半期と違う', q >= 1 and (q - 1) in nat[n] and nat[n][q - 1]['polityCode'] != v['polityCode'])
                    add('atWarMonths>0', v['atWarMonths'] > 0)
                    add('civilWarMonths>0', v['civilWarMonths'] > 0)
                r['cand'] = cand
            results[(arm, s, n)] = r

W('# r967read：土地税の算定の基準の AI の規則 A・B・C の切り替えの回数と危機の四半期（国×種・旗 0 の生 keep_961・読むだけ）')
W('# 国＝計器の 9 国（POB_INSTRUMENTS_NATIONS・61 国でない）。種：base_ins 1〜10・n1/n2 1〜10（ヌル腕）・a11 11〜20。')
W('# 開幕の状態：イギリス・清＝定額（枝 c85lt の world.json）・他は比例。影の凍結は国まるごと（f＝切り替えの四半期の R）。')
W('# 列の意味：A0w/A0q＝(960) の再現（π<0 の符号の反転・助走込み/抜き）。A・B・C1・C2・CB1＝状態機械の切り替えの回数（助走抜き＝q0〜q667）。')
W('#   C1＝規則 A を〈四半期の頭の r＜1〉（前の四半期の最後の月）だけで評価（本線・Government.cs:658 と同じ量）。C2＝前の四半期のどれかの月 r＜1。CB1＝規則 B を C1 で。')
W('#   nC1/nC2＝危機の四半期の数。tau0＝土地税の率が 0 の四半期の数（landTaxEndShare）。助走の危機は定義上 0（fiscal の行が無い・Government.cs:249 で法も動かない）。')
okall = all(c[3] for c in checks); bad = sum(c[4] for c in checks)
W('# 検算：monthIndex の四半期ごとの行数（q0＝2 行・他 3 行・計 3Q−1）＝%s（%d 組）。nation_s.payRateBelowOneMonths と fiscal の月の数えの食い違い＝%d 四半期' % ('全て一致' if okall else '食い違いあり', len(checks), bad))
W('#   食い違いの型（chk_offset.py で種 3 を読んだ）：3 か月とも印字が "1.0" で計数が 1 以上＝r が 1 のすぐ下（G10 の印字で 1 に丸まる・Instruments.cs:296）。')
W('#   ＝C1（印字の列を読む）はわずかに少なく数える・C2（エンジンの中で数えた nation_s の計数）は丸めの外。エンジンの述語（Government.cs:658 の `!(w.FiscalFundingRatio.Value < 1.0)` の内側＝r＜1）は 1e-10 の不足でも危機に数える。')
if not okall:
    for c in checks:
        if not c[3]:
            W('#   行数の食い違い %s s%d %s' % c[:3])
W()
nats = sorted(set(k[2] for k in results))
keys = ['A0w', 'A0q', 'A', 'B', 'C1', 'C2', 'CB1', 'nC1', 'nC2', 'tau0']
def val(r, k):
    v = r[k]
    return len(v) if isinstance(v, list) else v
for arm, seeds in ARMS:
    W('## 腕 %s：国ごとの中央〔最小,最大〕（%d 種）' % (arm, len(seeds)))
    W('nation\t' + '\t'.join(keys))
    for n in nats:
        cells = []
        for k in keys:
            xs = [val(results[(arm, s, n)], k) for s in seeds if (arm, s, n) in results]
            cells.append('%g〔%d,%d〕' % (statistics.median(xs), min(xs), max(xs)))
        W(n + '\t' + '\t'.join(cells))
    for k in ('A0w', 'A0q', 'A', 'C1'):
        xs = [val(results[(arm, s, n)], k) for s in seeds for n in nats if (arm, s, n) in results]
        W('# %s の国×種の最小〜最大（%s）：%d〜%d' % (k, arm, min(xs), max(xs)))
    W()
W('## no-go の読み（錨に切り替えがある国で、10 種とも 0 回の規則）')
W('nation\t錨\t' + '\t'.join('%s:%s' % (a, k) for a, _ in ARMS for k in ('A', 'B', 'C1', 'C2', 'CB1')))
for n, (dirn, yr) in ANCHOR.items():
    cells = []
    for arm, seeds in ARMS:
        for k in ('A', 'B', 'C1', 'C2', 'CB1'):
            hit = sum(1 for s in seeds if any(x[1] == dirn for x in results[(arm, s, n)][k]))
            cells.append('%d/%d%s' % (hit, len(seeds), ' no-go' if hit == 0 else ''))
    W('%s\t%s %d\t%s' % (n, dirn, yr, '\t'.join(cells)))
W('# 数え方：その向きの切り替えが 1 回以上あった種の数／種の数。0／10 が〈10 種とも 0 回〉。')
W()
W('## 錨の向きの最初の切り替えの年（base_ins・種 1〜10・無ければ -）')
for n, (dirn, yr) in ANCHOR.items():
    for k in ('A', 'B', 'C1', 'C2', 'CB1'):
        ys = []
        for s in range(1, 11):
            r = results[('base_ins', s, n)]
            f = [x for x in r[k] if x[1] == dirn]
            ys.append(str(r['yq'][f[0][0]]) if f else '-')
        W('%s\t%s\t%s' % (n, k, ' '.join(ys)))
W()
W('## 日本：1 件でも立つ述語の候補の列挙（四半期の数・全期間／1865〜1880・base_ins・種 1〜10）')
cks = list(results[('base_ins', 1, '日本')]['cand'].keys())
for k in cks:
    xs = [results[('base_ins', s, '日本')]['cand'][k] for s in range(1, 11)]
    W('%s\t全期間 %s\t1865〜1880 %s' % (k, ' '.join(str(x[0]) for x in xs), ' '.join(str(x[1]) for x in xs)))
W()
W('## 国×種（base_ins）の行：nation seed A0w A0q A B C1 C2 CB1 nC1 nC2 tau0')
for n in nats:
    for s in range(1, 11):
        r = results[('base_ins', s, n)]
        W('%s\t%d\t%s' % (n, s, '\t'.join(str(val(r, k)) for k in keys)))
W('# 限界（自己申告）：①影の凍結は国まるごと・R は四半期の 3 か月目の基数（輸出の地代を含まない・landbase の注 ②）②π は四半期末の値＝四半期の中の往復は数えない')
W('#   ③q0 の判定の R は行 0 で代用（助走の最後の月の行が無い）④門で止まる分・予算闘争・τ の段は数えていない＝AI の〈置くはずの目標〉の数で、施行の回数ではない')
W('#   ⑤開幕の定額の国の f は R(q0)＝枝の凍結（助走の最後の月の RGO ごとの地代）と同じではない')
with open(OUT, 'wb') as f:
    f.write(('\n'.join(out) + '\n').encode('utf-8'))
