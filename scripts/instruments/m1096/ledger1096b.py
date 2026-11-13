# (1096) 合流：機能の台帳の追記（ledger1096.py の後・reanchor の前に当てる）。バイトで読み書き・LF。
#   ① S14-38 の 12 列目に読み手の数（(1094) の md K1 の数）を足す＝r900/ledger_check.py の門 9（〈消す〉の行は読み手の数を持つ）。
#   ② fb (1089) が焼いた旗 11 本（MC0 の FarmScaleCap を含む）を名指す行の根拠の頭に〈焼いた〉を足す。12 列目が〈消す〉の行は頭に〈済み〉を足す。判定・最後に確かめた・錨は変えない（挙動は旗 1 の道のまま＝確かめ直していない）。
import re
P = 'D:/PoB_work/wt_m1096/docs/機能の台帳.md'
s = open(P, 'rb').read().decode('utf-8'); assert '\r' not in s
L = s.split('\n')
F = ['MintDemandCounted', 'SteamTurbineEnabled', 'CompostFertilizerEnabled', 'SulfurFromPyriteEnabled', 'OilNeedsMachinery', 'CenterIsFirstProvince', 'CoreInitByRegionCulture', 'ScarceCountsIncludeColonies', 'ScarceMineralGate', 'FactoryBuildRefIsReal', 'FarmScaleCap']
n1 = n2 = n3 = 0
for i, l in enumerate(L):
    if not l.startswith('| S'): continue
    c = l[2:-2].split(' | ')
    if len(c) != 12: continue
    if c[0] == 'S14-38':
        assert '読み手' not in c[11]
        c[11] += '・読み手 9（SimCore のコード・ExternalPressure）・試験 12（盤面に置く 11 行・TechLevelRemovalTests.cs:174）・3 つの係数は各 SimCore コード 2（(1094) の md K1 の数・(1096) 合流が門 9 のために写した）'; n1 += 1
    names = [f for f in F if f in l]
    if names:
        c[9] = '(1089) c104fb で旗 ' + '・'.join(names) + ' を焼いた（旗 1 の道だけ残す・FactoryBuildRefIsReal は旗 0 の道・欄は DormantIdentityTests の RetiredFields）＝(1096) 合流で main へ・錨の旗の名は消えた（挙動は確かめ直していない）。' + c[9]; n2 += 1
        if c[11].startswith('消す'):
            c[11] = '済み（(1089) で焼いた＝(1096)）・' + c[11]; n3 += 1
    L[i] = '| ' + ' | '.join(c) + ' |'
open(P, 'wb').write('\n'.join(L).encode('utf-8'))
print('ok', 'S14-38', n1, '焼いた旗の行', n2, '消す→済み', n3)
