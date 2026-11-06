# (905) 合流：ident.cfg を新しい基線 m905 へ書き換える（バイトで読み書き）。
#   python -X utf8 cfg905.py <ident.cfg>
import sys
p = sys.argv[1]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
one('# 現在の基準＝基線 m873 のまま（★(899)',
    '# 現在の基準＝基線 m905（★(905) 合流〔波77〕＝挙動の線 c77cs〔(901) 王権と議会 (1)・旗 SummonByInterestShare 既定 1〕を採った新しい基線・恒等の線 c77sm〔(904)〕・c77cw〔(903) 旗 0 の器・計器 cwmult_s〕・c77ce〔(902) 旗 0 の器・点火 e53491c5 を逆適用〕を入れた・旗 0 の腕 Z〔ident m905z・木 7d257bcd〕は m873 と全行一致・測った木 fc9aeb4c〔SOURCE-HASH aaecd9e3b5cce1b8〕・INSRAW keep_905/base_ins＝M の生・BASE_COMMIT fc9aeb4c・TESTS_NONFULL 2391・NG_BASE ng905）。もとは基線 m873 のまま（★(899)')
one('B5=measurements/b5_20261106_m873\n', 'B5=measurements/b5_20261106_m905\n')
one('B10INS=measurements/b10_20261106_m873_ins\n', 'B10INS=measurements/b10_20261106_m905_ins\n')
one('MON=measurements/mon_20261106_m873.txt\n', 'MON=measurements/mon_20261106_m905.txt\n')
one('INSRAW=D:/PoB_work/keep_899/base_ins\n', 'INSRAW=D:/PoB_work/keep_905/base_ins\n')
one('BASE_COMMIT=ea7fcad2\n', 'BASE_COMMIT=fc9aeb4c\n')
k = b.index('# (892) から keep_892/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + '# (905) から keep_905/base_ins（合わせた木 fc9aeb4c の M〔c77cs の旗 1〕の計器つき 10 種の生・全ての表 1133 本・cwmult_s〔(903)〕あり・landshare_s は末尾 3 列〔(904)〕・cearn_s の経路の名は fc9aeb4c の行番号＝BASE_COMMIT）。ヌル腕は keep_905/n1・n2（全ての表）・種 11〜20 は keep_905/a11（POB_CEARN=0）。keep_873・keep_873_rej・keep_879・keep_879_rej・keep_885・keep_885_rej・keep_892・keep_892_rej・keep_899 は残す（古い keep_* を消すのは作者のセッション）。採らなかった腕の生は keep_905_rej（c77ce・c77cw の腕 B と S11 を門と止める線の表へ縮めた物）。\n' + b[k:]
k = b.index('TESTS_NONFULL=2376\n')
b = b[:k] + '# (905)：2376＋c77sm 2（(904)・CedeRuleInstrumentTests は Assert を足しただけ・LandShareTests T7e・WriterRatchetTests M8）＋c77cs 1（(901)・CrownPcInstrumentTests T5′）＋c77ce 6（(902)・CedeRuleToLocalTests）＋c77cw 6（(903)・CivilWarArmsDemandTests）＝2391。\n' + b[k:]
one('TESTS_NONFULL=2376\n', 'TESTS_NONFULL=2391\n')
one('NG_BASE=rd:measurements/ng873_20261106:m873\n', 'NG_BASE=rd:measurements/ng905_20261106:m905\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
