# (947) 合流：ident.cfg を新しい基線 m947 へ書き換える（cfg933.py の型・バイトで読み書き）。
#   python -X utf8 cfg947.py <ident.cfg>
import sys
p = sys.argv[1]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m933（★(940) 合流〔波82〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m947（★(947) 合流〔波83〕＝挙動の線 c83ic〔(943) 矯正講和の君主系の押し付けを講和の段の頭の敗者の王冠から取る・旗 ImposeCrownFromLoser 既定 1〕を採った新しい基線・'
              '恒等の線 c83tf〔(941) InstrumentsTests.cs:2240 の試験の直し〕を入れた・腕 Z〔ident m947z・木 8ebe83e0〕〈判定: 恒等〉・'
              '測った木 e8e2d4ae・SOURCE-HASH c20a9220e4a1e5a3・成績表 B5 b5_20261108_m947・B10INS b10_20261108_m947_ins・MON mon_20261108_m947.txt・INSRAW keep_947/base_ins〔M の計器つき 10 種の生・1149 本〕・BASE_COMMIT e8e2d4ae・TESTS_NONFULL 2423・NG_BASE ng947）'
              '。もとは基線 m933（') + b[k0 + k1 + len('基線 m933（'):]
one('B5=measurements/b5_20261107_m933\n', 'B5=measurements/b5_20261108_m947\n')
one('B10INS=measurements/b10_20261107_m933_ins\n', 'B10INS=measurements/b10_20261108_m947_ins\n')
one('MON=measurements/mon_20261107_m933.txt\n', 'MON=measurements/mon_20261108_m947.txt\n')
one('INSRAW=D:/PoB_work/keep_933/base_ins\n', 'INSRAW=D:/PoB_work/keep_947/base_ins\n')
one('BASE_COMMIT=1af3e5d7\n', 'BASE_COMMIT=e8e2d4ae\n')
k = b.index('# (933) から keep_933/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + '# (947) から keep_947/base_ins（合わせた木 e8e2d4ae の M〔c83ic の旗 1〕の計器つき 10 種の生・1149 本〔出来事のときだけ出る表は軌道で変わる〕・c83ic の腕 B の生と表ごとにバイト一致〔burscan_s・clock_s の既知の揺れ 20 を除く〕）。ヌル腕 keep_947/n1・n2・種 11〜20 keep_947/a11（POB_CEARN=0）。keep_912_rej・keep_933 は残す（古い keep_* を消すのは作者のセッション）。採らなかった腕の生は keep_947_rej（c83hd・c83lt の腕 B）。\n' + b[k:]
k = b.index('TESTS_NONFULL=2418\n')
b = b[:k] + '# (947)：2418＋c83ic 5（(943)・ImposeCrownFromLoserTests）＋c83tf 0＝2423。\n' + b[k:]
one('TESTS_NONFULL=2418\n', 'TESTS_NONFULL=2423\n')
one('NG_BASE=rd:measurements/ng933_20261107:m933\n', 'NG_BASE=rd:measurements/ng947_20261108:m947\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
