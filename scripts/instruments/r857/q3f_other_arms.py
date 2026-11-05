# (857) c70ea Q3 の確かめ（反証役の前の補い・結果の後の表＝記録）：他の腕の共和制デンマークの名目 GDP と州・旧ヌル腕の種 8 の州ペルーの人の居ない商人の在庫（読むだけ）
#   python -I -X utf8 q3f_other_arms.py > out/q3f_other_arms.txt   （D:/PoB_work で走らせた＝相対パス）
for d, s in [('keep_852/n2', 1), ('keep_852/n2', 2), ('keep_839/n1', 2)]:
    f = open(f'{d}/nation_s{s}.tsv', encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ig = h.index('nominalGdpPerYear'); ip = h.index('populationK')
    rows = [l.rstrip('\n').split('\t') for l in f if '\t共和制デンマーク\t' in l]
    print(d, s, len(rows), 'q', rows[0][0], rows[-1][0], 'gdp0', sum(1 for r in rows if float(r[ig]) == 0), 'gdp', [round(float(r[ig]), 1) for r in rows][:20], 'popK', round(float(rows[0][ip]), 1))
    g = open(f'{d}/region_s{s}.tsv', encoding='utf-8'); hh = g.readline().rstrip('\n').split('\t'); ir = hh.index('region')
    print('  regions', sorted(set(l.split('\t')[ir] for l in g if '\t共和制デンマーク\t' in l)))
for d in ['keep_839/n1', 'keep_839/n2']:
    f = open(f'{d}/cpop_s8.tsv', encoding='utf-8'); h = f.readline().rstrip('\n').split('\t')
    iq, inat, ireg, icl, ipk, isv = [h.index(c) for c in ['quarter', 'nation', 'region', 'className', 'popK', 'savings']]
    tot = {}; mer = {}; own = {}
    for l in f:
        if '\tペルー\t' not in l: continue
        x = l.rstrip('\n').split('\t'); q = int(x[iq])
        if q not in (40, 120, 240, 320): continue
        tot[q] = tot.get(q, 0) + float(x[isv]); own[q] = x[inat]
        if x[icl] == '商人': mer[q] = (float(x[ipk]), float(x[isv]))
    print(d, 'Peru s8', {q: (own[q], round(tot[q], 1), mer.get(q)) for q in sorted(tot)})
