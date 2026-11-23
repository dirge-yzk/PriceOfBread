# (1332) c139nc 門の読み手：計器 capcol_s を自分で集計する（エンジンの関数を呼ばない・列は名前で読む）。
#   G1（狙い）＝pickTierGapCount の和（付け替えで、実際の首都の格が国の最も高い格より下だった回数）＝0 で緑。
#   G2（削り過ぎ）＝diffOldHome0Count の和（本国格 0 の盤面の付け替えで実際≠格を見ない選び）＝0 で緑。
#   G3（同じ格の中の選び違い）＝diffTierCount の和（実際≠計器が組んだ格の順の選び）＝0 で緑。
#   記録：付け替えの回数と格の内訳（Core・Integrated 割譲・Integrated 故地/昇格・植民地格）・内戦の四半期の付け替え・
#         首都が Integrated で同じ四半期の末に Core が有る国×四半期（region_s が有れば）・史実の桁の量（首都が植民地格の主権国〔内戦でない〕の数の四半期ごとの最大）。
#   使い方: python -X utf8 -I cc_gate.py <生のフォルダ> <種の最初> <種の数>
import sys, csv, os, collections
raw, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
T = collections.Counter(); mx = []; missing = 0; gapn = collections.Counter()
K = ['picksCount', 'pickTierGapCount', 'diffOldHome0Count', 'diffTierCount', 'diffOldCount', 'diffNewCount',
     'picksCoreCount', 'picksIntCededCount', 'picksIntOwnCount', 'picksColonialCount']
print('seed\trows\tG1\tG2\tG3\tpicks\tpicksCivil\tcore\tintCeded\tintOwn\tcolonial\tdiffOld\totherChange\tmaxSovColCap')
for s in range(s0, s0 + n):
    p = os.path.join(raw, f'capcol_s{s}.tsv')
    if not os.path.exists(p): print(f'{s}\t(capcol_s が無い)'); missing += 1; continue
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        if 'pickTierGapCount' not in ix: print(f'{s}\t(格の順の列が無い)'); missing += 1; continue
        c = collections.Counter(); perq = collections.Counter()
        for row in r:
            g = lambda k: row[ix[k]]
            c['rows'] += 1
            for k in K: c[k] += int(g(k))
            c['other'] += int(g('otherChangeFlag'))
            if int(g('picksCount')) > 0 and g('civilWarFlag') == '1': c['picksCivil'] += int(g('picksCount'))
            if int(g('pickTierGapCount')) > 0: gapn[g('nation')] += int(g('pickTierGapCount'))
            if g('capitalTier') == 'Colonial' and g('civilWarFlag') == '0': perq[g('quarter')] += 1
    m = max(perq.values()) if perq else 0; mx.append(m)
    for k in c: T[k] += c[k]
    print(f"{s}\t{c['rows']}\t{c['pickTierGapCount']}\t{c['diffOldHome0Count']}\t{c['diffTierCount']}\t{c['picksCount']}\t{c['picksCivil']}\t{c['picksCoreCount']}\t{c['picksIntCededCount']}\t{c['picksIntOwnCount']}\t{c['picksColonialCount']}\t{c['diffOldCount']}\t{c['other']}\t{m}")
mx.sort()
print(f"計\tG1={T['pickTierGapCount']}\tG2={T['diffOldHome0Count']}\tG3={T['diffTierCount']}\tpicks={T['picksCount']}（内戦の四半期 {T['picksCivil']}）\t格の内訳 Core={T['picksCoreCount']}・Integrated 割譲={T['picksIntCededCount']}・Integrated 故地/昇格={T['picksIntOwnCount']}・植民地格={T['picksColonialCount']}\tdiffOld={T['diffOldCount']}\totherChange={T['other']}\tmaxSovColCap 種の中央={mx[len(mx)//2] if mx else 0}・最大={max(mx) if mx else 0}\t欠けた種={missing}")
print('G1 の国：' + ','.join(f'{a}:{b}' for a, b in gapn.most_common(8)))
ok = missing == 0
print('門：G1 ' + ('緑' if ok and T['pickTierGapCount'] == 0 else '赤') + '・G2 ' + ('緑' if ok and T['diffOldHome0Count'] == 0 else '赤') + '・G3 ' + ('緑' if ok and T['diffTierCount'] == 0 else '赤'))
