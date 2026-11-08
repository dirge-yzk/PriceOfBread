# (961) 合流：機能の台帳の行の更新を写す（線の報告の文・バイトで読み書き・列の数は変えない・ledger954.py の型）。
#   python -X utf8 ledger961.py <台帳 md> <合わせた木の短いコミット>
import sys
p, H = sys.argv[1], sys.argv[2]
lines = open(p, 'rb').read().decode('utf-8').split('\n')
def upd(rid, f):
    hit = [i for i, l in enumerate(lines) if l.startswith('| ' + rid + ' |')]
    assert len(hit) == 1, (rid, len(hit))
    c = lines[hit[0]].split(' | '); n0 = len(c); f(c); assert len(c) == n0
    lines[hit[0]] = ' | '.join(c)
def h010(c):
    assert c[2] == '昇格した植民地は支配度0から始まる', c[2]
    c[2] = '~~昇格した植民地は支配度0から始まる~~（2026-11-08 (961)）開幕の一括昇格（MultiNationSim.cs の PromoteAllColonies の呼び口の直後）は 1.0 から・途中の接収（Diplomacy.cs の AcquireColony→PromoteColonyToRegion）は 0 から'
    c[7] = H
    c[9] = c[9] + '（2026-11-08 (957) c85kp：K の 1 回目を腕 B まで回した＝旗 OpeningColonyDominanceFull 既定 1・G1 1.0000〔旗 0 は 0.0595〕・G2 0〔上限 17・旗 0 は 45〕・G3 0.0257〔上限 0.05〕・止める線 0。(961) 合流で採った＝合わせた木 M の G1 1.0000・G2 0・G3 0.0250／S11 1.0000・0・0.0253・番人 OpeningColonyDominanceTests の T1・T3・T3′・T4・1.0 という値は AI の推奨・確証なし〈作者「全て推奨で」で推奨を採った〉）'
def h011(c):
    c[9] = c[9] + '（2026-11-08 (956) c85hs：HeldSinceFromData・CoreHeldYears・ProvinceSpec の HeldSince・world.json の heldSince 29 行を消した〔コミット 82ff1811・ident c85hs〈判定: 恒等〉・(961) で main へ〕・消す前の 29 州の値は measurements/m946_20261107/table29.md・ダンツィヒ 1793 ほかのデータの食い違いはこの表で読む）'
    c[11] = '消した（2026-11-08 (956) c85hs・82ff1811・(961) で main へ・錨は Coefficients.cs の墓標の行）'
def h005(c):
    assert c[4] == 'Coefficients.cs:3293:CoreHeldYears', c[4]
    c[4] = 'Simulation.cs:936:RegionTier.Core'
    c[9] = c[9] + '（2026-11-08 (956)：CoreHeldYears を消した＝開幕の州は全て Core・支配度 1.0 の直書き〔Simulation.cs の BuildProvince〕・植民地格は PromoteColonyToRegion・錨を写し直した）'
def h006(c):
    c[9] = c[9] + '（2026-11-08 (958) c85lt：A-2〈定額の土地税〉を腕つきの 2 回目で回した＝旗 LandTaxFixedBasis 既定 1・腕 B の門 G7 20・G4 0・G8a 0・G8b 0・清の G8c 0.5097〔窓 0.2314〜1.2594〕・止める線 0。(961) 合流で採った＝合わせた木 M の清の G8c 0.4368・S11 0.5301・G7 20・G4・G8a・G8b 0）'
def s1617(c):
    assert c[10] == '—', c[10]
    c[10] = '(959) c85mw MilitaryWarMult を接触の割合から導く案 A（2 回目）は腕の前の旗 1 のフルスイートで予言の外の赤 3 件〔WriterRatchetTests の 2 行・TransportInstrumentTests.cs:91〕と旧の規則を字で固定した番人 7 件で止めた（★(912)・3 回の規則に数えない・枝 origin/c85mw・(961) は採っていない＝この行の挙動は変わらない）'
upd('S27-H010', h010)
upd('S27-H011', h011)
upd('S26-H005', h005)
upd('S20-H006', h006)
upd('S16-17', s1617)
open(p, 'wb').write('\n'.join(lines).encode('utf-8'))
print('ok')
