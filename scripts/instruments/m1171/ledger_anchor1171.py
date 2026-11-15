# (1171) 合流：恒等の線 c115qc (1167)・c115sb (1168) が消した名を指す台帳の錨（reanchor1116.py が〈写せない〉と出した 5 行）を、消した記録の錨へ置き換え、行の最後の列に 1 文を足す（判定の列は書き換えない）。
#   python -X utf8 -I ledger_anchor1171.py <reanchor の後の台帳> <書き出す台帳>
import sys
src, dst = sys.argv[1:3]
b = open(src, 'rb').read(); nl = '\r\n' if b'\r\n' in b else '\n'
L = b.decode('utf-8').split(nl)
A = {
    'S22-H007': ('Simulation.cs:2415:MintBuysAtMarket;Coefficients.p10_ai_silver_overrides.cs:56:MintBuysAtMarket', '—（MintBuysAtMarket は既定 1.0 を焼き込んで消した＝(1168) c115sb）'),
    'S22-H017': ('Credit.cs:13:hoardRatio;Credit.cs:6:HoardRatioMax;', 'Credit.cs:13:hoardRatio;'),
    'S22-H023': ('Credit.cs:41:GoldCoverCreditRestraint;Credit.cs:7:GoldCoverCreditFloor', 'Credit.cs:41:GoldCoverCreditRestraint'),
    'S22-H052': ('MultiNationSim.cs:743:SilverParityFromRatio;Coefficients.p10_ai_silver_overrides.cs:54:SilverParityFromRatio', '—（SilverParityFromRatio は既定 1.0 を焼き込んで消した＝(1168) c115sb）'),
    'S26-20': ('Colonial.cs:24:MalariaQuininePerColony;Colonial.cs:40:BuyUpkeepGood;Coefficients.p04_trade.cs:152:MalariaQuininePerColony', '—（植民地の毎月のキニーネの枝と MalariaQuininePerColony は消した＝(1167) c115qc）'),
}
NOTE = {
    'S22-H007': '(1168) c115sb で焼き込み、旗を消した',
    'S22-H017': '上限 HoardRatioMax は (1168) c115sb で消した（錨から外した）',
    'S22-H023': '床 GoldCoverCreditFloor は (1168) c115sb で消した（錨から外した）',
    'S22-H052': '(1168) c115sb で焼き込み、旗を消した',
    'S26-20': '(1167) c115qc で枝と係数を消した（遠征のキニーネは World.cs に残る）',
}
hit = set()
for i, l in enumerate(L):
    for k, (o, n) in A.items():
        if l.startswith('| ' + k + ' |'):
            c = l.split(' | ')
            assert c[4] == o or c[4].startswith(o), (k, c[4][:120])
            c[4] = c[4].replace(o, n, 1)
            l2 = ' | '.join(c).rstrip()
            assert l2.endswith('|')
            L[i] = l2[:-1].rstrip() + f'〔(1171)：{NOTE[k]}〕 |'
            hit.add(k)
assert hit == set(A), set(A) - hit
open(dst, 'wb').write(nl.join(L).encode('utf-8'))
print('ok', len(hit))
