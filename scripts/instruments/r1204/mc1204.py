# (1204) c120mb 設計の材料：旗 0 の生（keep_1198/base_ins）の mintcap_s を読む。エンジンを回さない。
# 使い方: python -X utf8 -I mc1204.py <ins フォルダ> <出力>
import sys, os, glob, math
src, out = sys.argv[1], sys.argv[2]

def era(y):
    return '1783-1829' if y < 1830 else '1830-1869' if y < 1870 else '1870-1913' if y < 1914 else '1914-1950'

ERAS = ['1783-1829', '1830-1869', '1870-1913', '1914-1950']
MAJ = ['イギリス', 'スペイン', '清', 'フランス', 'アメリカ', 'ロシア', 'オーストリア', 'プロイセン', 'メキシコ', 'ペルー', 'ドイツ', '日本']
tot = {}
def add(k, v):
    if v == v: tot[k] = tot.get(k, 0.0) + v
cat = {e: [0] * 6 for e in ERAS}  # months, capNoGood, capNoStock, capFloor, capAbove, capAtMarket（本位の月だけ）
nat = {}
pov_above, pov_floor, gap_rows = [], [], []
rows = 0; seeds = 0
for f in sorted(glob.glob(os.path.join(src, 'mintcap_s*.tsv'))):
    seeds += 1
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t'); rows += 1
            y = int(c[ix['year']]); e = era(y); n = c[ix['nation']]
            F = lambda k: float(c[ix[k]])
            mo, fi = int(c[ix['months']]), int(c[ix['fiatM']])
            k6 = [mo - fi] + [int(c[ix[k]]) for k in ('capNoGoodM', 'capNoStockM', 'capFloorM', 'capAboveM', 'capAtMarketM')]
            for i in range(6): cat[e][i] += k6[i]
            bc = F('boughtCapAg') + F('boughtCapAu'); bo = F('boughtOtherAg') + F('boughtOtherAu')
            old = F('oldShadowAg') + F('oldShadowAu'); new = F('newShadowAg') + F('newShadowAu')
            for k, v in (('bc', bc), ('bo', bo), ('old', old), ('new', new), ('bcAg', F('boughtCapAg')), ('bcAu', F('boughtCapAu')),
                         ('boAg', F('boughtOtherAg')), ('boAu', F('boughtOtherAu')), ('imp', F('capImportAgSum'))):
                add(k, v); add(k + '|' + e, v)
            for mj in MAJ:
                if n == mj or n.endswith(mj):
                    d = nat.setdefault(mj, [0.0] * 6)
                    d[0] += bc; d[1] += bo; d[2] += old; d[3] += new
                    if F('capImportAgSum') == F('capImportAgSum'): d[4] += F('capImportAgSum')
                    d[5] += k6[0]
            pov = F('capPriceOverParity'); med = F('originPoverMed')
            if mo - fi > 0 and pov == pov:
                if int(c[ix['capAboveM']]) == mo - fi and mo - fi > 0: pov_above.append(pov)
                if int(c[ix['capFloorM']]) == mo - fi: pov_floor.append(pov)
                if med == med and int(c[ix['law']]) == 1: gap_rows.append(pov / med if med > 0 else float('nan'))

def q(a, p):
    a = sorted(x for x in a if x == x)
    return a[min(len(a) - 1, int(p * len(a)))] if a else float('nan')
L = []
L.append(f'# src {src} seeds {seeds} rows {rows}')
L.append(f"bought cap {tot['bc']:.6g} other {tot['bo']:.6g} all {tot['bc']+tot['bo']:.6g} capShare {tot['bc']/(tot['bc']+tot['bo']):.4f} (Ag {tot['bcAg']/(tot['bcAg']+tot['boAg']):.4f} Au {tot['bcAu']/(tot['bcAu']+tot['boAu']):.4f})")
L.append(f"shadow old {tot['old']:.6g} new {tot['new']:.6g} new/old {tot['new']/tot['old']:.4f} selfcheck old/bought {tot['old']/(tot['bc']+tot['bo']):.6f}")
L.append(f"capImportAgSum all {tot['imp']:.6g}")
for e in ERAS:
    m = cat[e]; t = max(1, m[0])
    b = tot.get('bc|' + e, 0) + tot.get('bo|' + e, 0)
    L.append(f"era {e} stdM {m[0]} noGood {m[1]/t:.3f} noStock {m[2]/t:.3f} floor {m[3]/t:.3f} above {m[4]/t:.3f} atMkt {m[5]/t:.4f} | bought {b:.4g} capShare {tot.get('bc|'+e,0)/b if b>0 else float('nan'):.4f} new/old {tot.get('new|'+e,0)/tot.get('old|'+e,1):.4f} capImpAg {tot.get('imp|'+e,0):.4g}")
L.append(f"capPriceOverParity 四半期末 首都が四半期ずっと平価超え: n {len(pov_above)} p10 {q(pov_above,.1):.3f} p50 {q(pov_above,.5):.3f} p90 {q(pov_above,.9):.3f} | ずっと床: n {len(pov_floor)} p50 {q(pov_floor,.5):.3f}")
L.append(f"銀本位の首都÷産地の中央の値段の比 n {len(gap_rows)} p10 {q(gap_rows,.1):.3f} p50 {q(gap_rows,.5):.3f} p90 {q(gap_rows,.9):.3f}")
for mj, d in nat.items():
    a = d[0] + d[1]
    L.append(f"nation {mj} bought {a:.4g} cap {d[0]:.4g} capShare {d[0]/a if a>0 else float('nan'):.4f} other/cap {d[1]/d[0] if d[0]>0 else float('inf'):.1f} new/old {d[3]/d[2] if d[2]>0 else float('nan'):.4f} capImpAg {d[4]:.4g} stdM {d[5]}")
with open(out, 'w', encoding='utf-8', newline='\n') as w: w.write('\n'.join(L) + '\n')
print('\n'.join(L))
