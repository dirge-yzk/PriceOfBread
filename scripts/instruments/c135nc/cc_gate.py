# (1304) c135nc 門の読み手：計器 capcol_s を自分で集計する（エンジンの関数を呼ばない）。
#   G1（下の側）＝g1Flag の和（内戦でない・本国格 1 以上・首都が植民地格の国×四半期）＝0 で緑。G2（上の側）＝diffOldHome0Count の和（本国格 0 の盤面の付け替えで実際≠旧の規則）＝0 で緑。
#   記録：史実の桁の量（首都が植民地格の主権国の数の四半期ごとの最大・種の中央と最大）・付け替えの回数・旧と違った回数（新の規則が働いた回数）・新と違った回数・受け口の外の遷都・G1 の残りの内訳（受け口の外の遷都か、本国格が後から増えたか）。
#   使い方: python -X utf8 -I cc_gate.py <生のフォルダ> <種の最初> <種の数>
import sys, csv, os, collections
raw, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
T = collections.Counter(); mx = []; g1n = collections.Counter(); seeds_g1 = 0; missing = 0
print('seed\trows\tG1\tG2\tpicks\tdiffOld\tdiffNew\totherChange\tmaxSovColCap\tG1_after_other\tG1_after_pickHome0\tG1_other')
for s in range(s0, s0 + n):
    p = os.path.join(raw, f'capcol_s{s}.tsv')
    if not os.path.exists(p): print(f'{s}\t(capcol_s が無い)'); missing += 1; continue
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        c = collections.Counter(); perq = collections.Counter(); last = {}
        for row in r:
            g = lambda k: row[ix[k]]
            ni = (g('nationUid'), g('nationIndex'))
            c['rows'] += 1
            pk = int(g('picksCount')); c['picks'] += pk; c['diffOld'] += int(g('diffOldCount')); c['diffNew'] += int(g('diffNewCount'))
            c['G2'] += int(g('diffOldHome0Count')); c['other'] += int(g('otherChangeFlag'))
            if pk > 0: last[ni] = ('pick', int(g('lastPickHomeRegions')))
            if g('otherChangeFlag') == '1': last[ni] = ('other', -1)
            if g('capitalTier') == 'Colonial' and g('civilWarFlag') == '0': perq[g('quarter')] += 1
            if g('g1Flag') == '1':
                c['G1'] += 1; g1n[g('nation')] += 1
                why = last.get(ni, ('none', -1))
                if why[0] == 'other': c['G1o'] += 1
                elif why[0] == 'pick' and why[1] == 0: c['G1h0'] += 1
                else: c['G1x'] += 1
    m = max(perq.values()) if perq else 0; mx.append(m)
    for k in c: T[k] += c[k]
    if c['G1'] > 0: seeds_g1 += 1
    print(f"{s}\t{c['rows']}\t{c['G1']}\t{c['G2']}\t{c['picks']}\t{c['diffOld']}\t{c['diffNew']}\t{c['other']}\t{m}\t{c['G1o']}\t{c['G1h0']}\t{c['G1x']}")
mx.sort()
print(f"計\tG1={T['G1']}（種 {seeds_g1}）\tG2={T['G2']}\tpicks={T['picks']}\tdiffOld={T['diffOld']}\tdiffNew={T['diffNew']}\totherChange={T['other']}\tmaxSovColCap 種の中央={mx[len(mx)//2] if mx else 0}・最大={max(mx) if mx else 0}\t欠けた種={missing}")
print('G1 の国：' + ','.join(f'{a}:{b}' for a, b in g1n.most_common(8)))
print('門：G1 ' + ('緑' if T['G1'] == 0 and missing == 0 else '赤') + '・G2 ' + ('緑' if T['G2'] == 0 and missing == 0 else '赤'))
