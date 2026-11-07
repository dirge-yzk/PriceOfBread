# (933) 合流：ident.cfg を新しい基線 m933 へ書き換える（cfg919.py の型・バイトで読み書き）。
#   python -X utf8 cfg933.py <ident.cfg>
import sys
p = sys.argv[1]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m919 のまま（★(926)'); k1 = b.index('）。もとは', k0)
b = b[:k0] + ('# 現在の基準＝基線 m933（★(933) 合流〔波81〕＝挙動の線 c81cw〔(929) 内戦の軍需・旗 CivilWarArmsDemandUnified 既定 1〕と c81la〔(930) B-2a の名簿 7 国の landOwnership のデータ〕を採った新しい基線・'
              '恒等の線 c81sm〔(927) 小物の束・cearn_s の heirDebtSettledPerQuarter を実際に移った額で〕・c81im〔(928) 計器 imp_s と crownpc_s の末尾 1 列 crownRegisterSourceCode〕・'
              '測った木 1af3e5d7・成績表 B5 b5_20261107_m933・B10INS b10_20261107_m933_ins・MON mon_20261107_m933.txt・INSRAW keep_933/base_ins〔M の計器つき 10 種の生〕・BASE_COMMIT 1af3e5d7・TESTS_NONFULL 2418・NG_BASE ng933）'
              '。もとは基線 m919 のまま（★(926)') + b[k0 + len('# 現在の基準＝基線 m919 のまま（★(926)'):]
one('B5=measurements/b5_20261107_m919\n', 'B5=measurements/b5_20261107_m933\n')
one('B10INS=measurements/b10_20261107_m919_ins\n', 'B10INS=measurements/b10_20261107_m933_ins\n')
one('MON=measurements/mon_20261107_m919.txt\n', 'MON=measurements/mon_20261107_m933.txt\n')
one('INSRAW=D:/PoB_work/keep_926/base_ins\n', 'INSRAW=D:/PoB_work/keep_933/base_ins\n')
one('BASE_COMMIT=890d59e0\n', 'BASE_COMMIT=1af3e5d7\n')
k = b.index('# (926) から keep_926/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + '# (933) から keep_933/base_ins（合わせた木 1af3e5d7 の M〔c81cw の旗 1・c81la のデータ〕の計器つき 10 種の生・imp_s 10 本を足した全ての表・crownpc_s は末尾 1 列 crownRegisterSourceCode・cearn_s の heirDebtSettledPerQuarter は実際に移った額）。\n' + b[k:]
k = b.index('TESTS_NONFULL=2410\n')
b = b[:k] + '# (933)：2410＋c81sm 3（(927)・HeirDebtSettledMovedTests・OnDoubleReadScanTests）＋c81im 3（(928)・ImposeAndCrownSourceTests）＋c81la 2（(930)・LandOwnershipDataTests T-a・T-b）＋c81cw 0＝2418。\n' + b[k:]
one('TESTS_NONFULL=2410\n', 'TESTS_NONFULL=2418\n')
one('NG_BASE=rd:measurements/ng919_20261107:m919\n', 'NG_BASE=rd:measurements/ng933_20261107:m933\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
