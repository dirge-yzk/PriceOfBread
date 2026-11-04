# (828) c66dw の門の台本（読むだけ・エンジンを呼ばない）：計器 cpwdft_s を読む。事前登録 measurements/m828_20261104_prereg_dw.txt の [4]。
# 使い方：python -I -X utf8 gate828.py <ins フォルダ> <種 例 1-10>
#   出す量（種ごとと合計）：
#     evF／evC＝旧の規則の額（計器が状態から式で書く）が正の事象（逃避／利札の落とし）
#     osF／osC＝片側の書き込み（エンジンの消えた請求権の累計≠前の点のエンジンの累計＋台帳の差×為替・ビットで）
#     abF／abC＝吸収（旧の規則の額が正なのに外資の負債の台帳の差が 0）
#     actF／actC＝規則の差が持ち主の側へ届く事象（前の点の累計＋旧の規則の額×為替≠前の点の累計＋台帳の差×為替・ビットで）
#                 ＝旗 1 で直した事象の数（旗 0 では osF／osC と同じになるはず）
#     oldF／oldC＝エンジンの累計≠前の点の累計＋旧の規則の額×為替（旗 0 で 0＝計器の旧の式がエンジンを再現する確かめ・旗 1 では act と同じになるはず）
#     maxDevGold＝max |エンジンの累計−台帳の累計|（金建て）
#     hostDevF／hostDevC＝max |旧の規則の額−台帳の差|÷刻み（受け皿の側の減りが丸めの外で動いたか＝上限の門）
#   ⚠累計どうしの比較＝前の点のエンジンの累計から足す（丸めで吸収された差は持ち主の側へ届かない＝数えない）。Python の float は IEEE の double（同じ足し算と掛け算）。
import sys, os, csv, math
d, spec = sys.argv[1], sys.argv[2]
seeds = list(range(int(spec.split('-')[0]), int(spec.split('-')[1]) + 1)) if '-' in spec else [int(x) for x in spec.split(',')]
def ulp(x):
    return math.ulp(abs(x)) if x != 0 else 0.0
K = ['rows', 'evF', 'evC', 'osF', 'osC', 'abF', 'abC', 'actF', 'actC', 'oldF', 'oldC']
TAB = chr(9)
tot = {k: 0 for k in K}
maxdev = 0.0; hostF = 0.0; hostC = 0.0; missing = []
print(TAB.join(['seed'] + K + ['maxDevGold', 'hostDevF_ulp', 'hostDevC_ulp']))
for s in seeds:
    p = os.path.join(d, f'cpwdft_s{s}.tsv')
    if not os.path.exists(p): missing.append(s); continue
    c = {k: 0 for k in K}; md = 0.0; hf = 0.0; hc = 0.0
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter=TAB); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            c['rows'] += 1
            g = lambda k: float(row[ix[k]])
            f0, f1, e = g('fciBeforeFlight'), g('fciAtCoupon'), g('fxRate')
            flO, flL, woO, woL = g('flightOldIndex'), g('flightLedgerIndex'), g('woOldIndex'), g('woLedgerIndex')
            ft0, ft1, ft2 = g('ftBefore'), g('ftAfterFlight'), g('ftAfterCoupon')
            ledF, ledC = ft0 + flL * e, ft1 + woL * e
            oldF = ft0 + flO * e if flO > 0 else ft0
            oldC = ft1 + woO * e if woO > 0 else ft1
            md = max(md, abs(ft1 - ledF), abs(ft2 - ledC))
            if flO > 0:
                c['evF'] += 1; u = ulp(f0)
                if u > 0: hf = max(hf, abs(flO - flL) / u)
            if woO > 0:
                c['evC'] += 1; u = ulp(f1)
                if u > 0: hc = max(hc, abs(woO - woL) / u)
            c['osF'] += ft1 != ledF; c['osC'] += ft2 != ledC
            c['actF'] += oldF != ledF; c['actC'] += oldC != ledC
            c['oldF'] += oldF != ft1; c['oldC'] += oldC != ft2
            c['abF'] += int(row[ix['absorbedFlight']]); c['abC'] += int(row[ix['absorbedCoupon']])
    print(TAB.join([str(s)] + [str(c[k]) for k in K] + [repr(md), repr(hf), repr(hc)]))
    for k in K: tot[k] += c[k]
    maxdev = max(maxdev, md); hostF = max(hostF, hf); hostC = max(hostC, hc)
print(TAB.join(['total'] + [str(tot[k]) for k in K] + [repr(maxdev), repr(hostF), repr(hostC)]))
if missing: print('MISSING seeds', missing)
