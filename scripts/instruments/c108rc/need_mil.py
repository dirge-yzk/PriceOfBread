# (1102) 問い 1 と 5 の紙：治める必要を闘争度で重み付けた形（AI の案 U2・新しい係数 0・新しい状態 0）で、清の O の取り分と C9 がどう動くか。表を読むだけ。
# 使い方: python -X utf8 -I need_mil.py <nat_X.tsv> <X の表のフォルダ> [ラベル] ...（2 つずつ繰り返す）
# 形：N′_n＝popK_n×q_t×m̄_n÷m̄_世界（m̄_n＝secession_s の militancyNationMeanIndex・m̄_世界＝国の人口加重の平均）＝Σ_n N′_n＝Σ_n N_n（世界の和は変わらない）。
#   H′＝min(L−wUse, N′)・O′＝L−wUse−H′。secession_s に行の無い国は m̄_n＝m̄_世界（重み 1）として件数と人口の取り分を出す。
# 限界（名乗る）：州ごとの m_r でなく国の平均で数えた（州の行は secession_s に約 6 割しか無い）＝国の段の配り（H と O）だけの紙。F_U の州の割合の紙ではない。
import sys, os, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
QING = '清'
def med(v): return st.median(v) if v else float('nan')
a = sys.argv[1:]
for i in range(0, len(a), 3):
    natp, folder, lab = a[i], a[i + 1], a[i + 2]
    mil = {}
    for s in range(1, 11):
        p = os.path.join(folder, f'secession_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
            for line in f:
                c = line.rstrip('\n').split('\t')
                key = (s, int(c[ix['quarter']]), c[ix['nation']])
                if key not in mil:
                    try: mil[key] = float(c[ix['militancyNationMeanIndex']])
                    except ValueError: pass
    data = collections.defaultdict(dict)
    with open(natp, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            data[(int(c[0]), int(c[1]))][c[ix['nation']]] = {k: float(c[ix[k]]) for k in ('L', 'WA', 'popK')}
    rec = collections.defaultdict(lambda: collections.defaultdict(list)); red = red0 = tot = 0; miss_pop = []; top = collections.Counter()
    for (s, q), nat in data.items():
        totL = sum(v['L'] for v in nat.values()); totP = sum(v['popK'] for v in nat.values())
        if not (totL > 0 and totP > 0): continue
        qt = totL / totP
        have = {nm: mil.get((s, q, nm)) for nm in nat}
        wp = sum(nat[nm]['popK'] for nm, m in have.items() if m is not None and m == m)
        mw = sum(nat[nm]['popK'] * m for nm, m in have.items() if m is not None and m == m) / wp if wp > 0 else float('nan')
        miss_pop.append(1 - wp / totP)
        sumO = sumO2 = 0.0
        for nm, v in nat.items():
            wu = min(v['WA'], v['L'])
            m = have[nm]; fac = (m / mw) if (m is not None and m == m and mw > 0) else 1.0
            o = max(0.0, v['L'] - wu - min(max(0.0, v['L'] - wu), v['popK'] * qt))
            o2 = max(0.0, v['L'] - wu - min(max(0.0, v['L'] - wu), v['popK'] * qt * fac))
            v['O'] = o; v['O2'] = o2; v['fac'] = fac; sumO += o; sumO2 += o2
        tot += 1
        if sumO > 0 and max(v['O'] for v in nat.values()) / sumO >= 0.3: red0 += 1
        if sumO2 > 0:
            nm2, v2 = max(nat.items(), key=lambda kv: kv[1]['O2'])
            if v2['O2'] / sumO2 >= 0.3: red += 1; top[nm2] += 1
        qg = nat.get(QING)
        if qg is not None:
            rec[q]['清 q_t'].append(qg['O'] / sumO if sumO > 0 else 0.0); rec[q]['清 U2'].append(qg['O2'] / sumO2 if sumO2 > 0 else 0.0)
            rec[q]['清の重み m̄÷m̄世界'].append(qg['fac']); rec[q]['ΣO÷ΣL U2'].append(sumO2 / totL)
    print(f'\n## {lab}')
    print(f'secession_s に行の無い国の人口の取り分（種×四半期）：中央 {med(miss_pop):.4g}・最大 {max(miss_pop):.4g}')
    print('四半期\t清 q_t\t清 U2\t清の重み\tΣO÷ΣL U2')
    for q in [0, 20, 40, 100, 200, 300, 400, 500, 600, 660]:
        if q in rec: print(f'{q}\t' + '\t'.join(f'{med(rec[q][k]):.4g}' for k in ('清 q_t', '清 U2', '清の重み m̄÷m̄世界', 'ΣO÷ΣL U2')))
    allq = sorted(rec); u2 = [med(rec[q]['清 U2']) for q in allq]
    print(f'清 U2 の取り分（種の中央）0.3 以上の四半期 {sum(1 for x in u2 if x >= 0.3)}/{len(u2)}・最大 {max(u2):.4g}')
    print(f'C9 が破れる種×四半期：q_t {red0}/{tot}・U2 {red}/{tot}・U2 で破る国の上位 {top.most_common(6)}')
