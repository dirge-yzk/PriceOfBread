# (1091) 段 C′ の事前登録の材料：新しい列（rulepower_s の末尾 10 列・rulepoweralloc_s・minecap_s）で、段 C′ の門と MC1 の材料を数える。
# 使い方: python -X utf8 -I gates.py <計器の表のフォルダ> > 出力
# 読む物：門〈同点〉（R の列）・門〈凍結〉（凍結を外した届きと τ の安定）・門〈ρ の回数〉Vb（配りの重み÷(Y×(1−s)×(1−b))＝1 の行数）・q_t の下の影の外国の行（下限）・鉱山の 1〜5（種ごとの年の中央）。
import sys, os, collections, statistics as st
d = sys.argv[1]
def rd(name, s):
    p = os.path.join(d, f'{name}{s}.tsv')
    if not os.path.exists(p): return None, []
    fi = open(p, encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    return ix, [l.rstrip('\n').split('\t') for l in fi]
def f(x): return float('nan') if x == 'nan' else float(x)
tie = tieExact = frz = rows = 0; spread_old = spread_nf = regs = 0
vb_bad = vb_rows = 0; fixed_rows = qt_rows = 0
mine = collections.defaultdict(list)
for s in range(1, 11):
    ix, R = rd('rulepower_s', s)
    if ix is None: continue
    per = collections.defaultdict(lambda: ([], []))
    byq = collections.defaultdict(lambda: collections.defaultdict(dict))
    for c in R:
        rows += 1
        o = f(c[ix['ownerSideShareR']]); u = f(c[ix['ungovSideShareR']])
        if o == o and u == u:
            if o == u and c[ix['bestClass']] == 'ungov': tieExact += 1
            if abs(o - u) <= 1e-9 * max(abs(o), abs(u), 1e-300) and c[ix['bestClass']] == 'ungov': tie += 1
        a = f(c[ix['rhoNewIndex']]); b = f(c[ix['rhoNoFreezeIndex']])
        if a == a and b == b and abs(a - b) > 1e-9 * max(abs(a), 1e-300): frz += 1
        ri0 = f(c[ix['ruleInit0Share']]); key = (c[ix['nation']], c[ix['region']])
        if a > 0 and ri0 > 0: per[key][0].append(f(c[ix['tauIndex']]) * ri0 / a)
        if b > 0 and ri0 > 0: per[key][1].append(f(c[ix['tauNoFreezeIndex']]) * ri0 / b)
        q = int(c[ix['quarter']]); nm = c[ix['nation']]
        if q % 40 == 0: byq[q][nm].setdefault('L', f(c[ix['lNationIndex']])); byq[q][nm].setdefault('WA', f(c[ix['wNationIndex']])); byq[q][nm].setdefault('O', f(c[ix['oNationIndex']])); byq[q][nm]['pop'] = byq[q][nm].get('pop', 0.0) + f(c[ix['popK']])
    for k, (o, n) in per.items():
        if len(o) > 1 and min(o) > 0:
            regs += 1
            if max(o) / min(o) > 1 + 1e-9: spread_old += 1
            if len(n) > 1 and min(n) > 0 and max(n) / min(n) > 1 + 1e-9: spread_nf += 1
    ixa, A = rd('rulepoweralloc_s', s)
    if ixa is not None:
        bynat = collections.defaultdict(list)
        for c in A:
            y = f(c[ixa['yIndex']]); sv = f(c[ixa['sShare']]); bv = f(c[ixa['bShare']]); w = f(c[ixa['wgtIndex']])
            den = y * (1 - min(1.0, sv)) * (1 - bv)
            if den > 0 and w > 0:
                vb_rows += 1
                if abs(w / den - 1) > 1e-9: vb_bad += 1
            bynat[(int(c[ixa['quarter']]), c[ixa['nation']])].append((w, f(c[ixa['allocIndex']])))
        for (q, nm), lst in bynat.items():
            nat = byq.get(q, {})
            if nm not in nat: continue
            totL = sum(v['L'] for v in nat.values()); totP = sum(v['pop'] for v in nat.values())
            if not (totL > 0 and totP > 0): continue
            qt = totL / totP; v = nat[nm]
            wuse = min(v['WA'], v['L']); h = min(max(0, v['L'] - wuse), v['pop'] * qt); ot = max(0, v['L'] - wuse - h)
            pos = sum(1 for w, a in lst if w > 0)
            fixed_rows += sum(1 for w, a in lst if a > 0)
            if ot > 0: qt_rows += pos
    ixm, M = rd('minecap_s', s)
    if ixm is not None:
        for c in M:
            y = int(c[ixm['year']]);
            if y not in (1850, 1880, 1913): continue
            o = f(c[ixm['outputSum']])
            if not o > 0: continue
            mine[y].append((o, c))
print('rulepower_s 行', rows)
print('門〈同点〉：同点（R で厳密に等しい）で無統治が最大', tieExact, '・相対 1e-9 で等しい', tie, '（旗 0 の盤面では >0 が赤）')
print('門〈凍結〉：凍結を外すと届きが変わる行', frz, '・τ×RuleInit0÷ρ が時間で一定でない州（今／凍結を外した）', spread_old, '/', spread_nf, '・調べた州', regs)
print('門〈ρ の回数〉Vb：配りの重み÷(Y×(1−s)×(1−b)) が 1 でない行', vb_bad, '/', vb_rows, '（今は ρ が重みに入る＝>0 が赤）')
print('q_t の下の影の外国の行（四半期 0・40 おき・配る国の候補で重み>0・O が 0 の国は表に行が無いので下限）：今の q', fixed_rows, '・q_t', qt_rows)
for y in sorted(mine):
    L = mine[y]; tot = sum(o for o, _ in L)
    def sh(col): return sum(o * f(c[ixm[col]]) for o, c in L if f(c[ixm[col]]) == f(c[ixm[col]])) / tot
    print(f'鉱山 {y}（種×国×財×級の延べ・産出加重）：目標>1.5 {sh("target15Share"):.3f}・>3.0 {sh("target30Share"):.3f}・国有化 {sh("nationalMinesShare"):.3f}・未捕捉 {sh("depInitMissingOutShare"):.3f}・品位 床以下 {sh("gradeBin0"):.3f} 床〜.5 {sh("gradeBin1"):.3f} .5〜1 {sh("gradeBin2"):.3f} 1 {sh("gradeBin3"):.3f}・残り÷初期 箱 {[round(sh("depBin"+str(i)),3) for i in range(5)]}・BuiltScale>1.5 の産出 {sum(f(c[ixm["builtOver15Output"]]) for _, c in L)/tot:.3f}')
