# (919) 合流：ident.cfg を新しい基線 m919 へ書き換える（cfg905.py の型・バイトで読み書き）。
#   python -X utf8 cfg919.py <ident.cfg>
import sys
p = sys.argv[1]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m905 のまま（★(912)'); k1 = b.index('）。もとは', k0)
b = b[:k0] + '# 現在の基準＝基線 m919（★(919) 合流〔波79〕＝挙動の線 c79ce〔(915) 割譲の支配度 C1 の 2 回目・旗 CedeRuleToLocal 既定 1〕を採った新しい基線・恒等の線 c79ar〔(913) ArmyShape.Compute の Instruments.On を 1 回だけ読む〕・c79lb〔(914) 試験の盤面だけ〕を入れた・旗 0 の腕 Z〔ident m919z・木 48909304〕は m905 と全行一致・測った木 8a8344ad＝BASE_COMMIT・TESTS_NONFULL 2402' + b[k1:]
one('B5=measurements/b5_20261106_m905\n', 'B5=measurements/b5_20261107_m919\n')
one('B10INS=measurements/b10_20261106_m905_ins\n', 'B10INS=measurements/b10_20261107_m919_ins\n')
one('MON=measurements/mon_20261106_m905.txt\n', 'MON=measurements/mon_20261107_m919.txt\n')
one('INSRAW=D:/PoB_work/keep_912/base_ins\n', 'INSRAW=D:/PoB_work/keep_919/base_ins\n')
one('BASE_COMMIT=b482aa8b\n', 'BASE_COMMIT=8a8344ad\n')
k = b.index('# (912) から keep_912/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + '# (919) から keep_919/base_ins（合わせた木 8a8344ad の M〔c79ce の旗 1〕の計器つき 10 種の生・全ての表 1140 本〔devcap_s・pclamp_s・pfix_s は事象のあった種だけ＝軌道で本数が変わる〕・cearn_s の経路の名は 8a8344ad の行番号＝BASE_COMMIT）。ヌル腕は keep_919/n1・n2（全ての表）・種 11〜20 は keep_919/a11（POB_CEARN=0）。keep_905・keep_912 は残す（古い keep_* を消すのは作者のセッション）。\n' + b[k:]
k = b.index('TESTS_NONFULL=2402\n')
b = b[:k] + '# (919)：2402＋c79ar 0（(913)）＋c79lb 0（(914)・盤面だけ）＋c79ce 0（(915)・DormantIdentityTests の行を外しただけ）＝2402。\n' + b[k:]
one('NG_BASE=rd:measurements/ng905_20261106:m905\n', 'NG_BASE=rd:measurements/ng919_20261107:m919\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
