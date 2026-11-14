# (1143) 合流（m1122/cfg1122.py の型・基線を m1122→m1143 へ）。バイトで読み書き。
#   python -X utf8 cfg1143.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1122 のまま（★(1136)'); k1 = b.index('\n', k0)
old_head = b[k0:k1]
b = b[:k0] + ('# 現在の基準＝基線 m1143（★(1143) 合流〔波111〕＝挙動の線 c111rb〔(1137) 段 R の need の基底＝産出の比・旗 RailNeedByOutput 既定 1〕・c111tp〔(1138) 条約港の読み手から植民地格の港を外す・旗 TreatyPortHomeOnly 既定 1〕・c110lo〔(1130) 機関車の発見の門・旗 RailLocoNeedsDiscovery 既定 1・載せ直し〕を採った・'
              '恒等の線 c111tf〔(1139) tb の T1 を試験の中で盤面を置く形へ〕・c111sm〔(1140) sm の残り〕を入れた・'
              '腕 Z〔ident m1143z・合わせた木＋旗 3 本を 0 へ戻す使い捨ての木 f70d59138・段 2〕〈判定: 恒等〉・アブレーション A_lo・A_rb・A_tp で各の線の門が通る・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261115_m1143・B10INS b10_20261115_m1143_ins・MON mon_20261115_m1143.txt・INSRAW keep_1143/base_ins〔M の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2858〔Tests 2624＋Tests.Ui 234〕・NG_BASE ng1143）。前の行：' + old_head[2:]) + b[k1:]
one('B5=measurements/b5_20261114_m1122\n', 'B5=measurements/b5_20261115_m1143\n')
one('B10INS=measurements/b10_20261114_m1122_ins\n', 'B10INS=measurements/b10_20261115_m1143_ins\n')
one('MON=measurements/mon_20261114_m1122.txt\n', 'MON=measurements/mon_20261115_m1143.txt\n')
one('INSRAW=D:/PoB_work/keep_1136/base_ins\n', '# (1143) から keep_1143/base_ins（合わせた木 tf＋sm＋lo＋rb＋tp の M の計器つき 10 種の生・木 ' + H + '・全表 1499 本）。ヌル腕 keep_1143/n1・n2（全表・POB_CEARN=0）・種 11〜20 は keep_1143/a11（SMALL＋BYPASS＋railspend_s・trrail_s・trnation_s・tport_s）。keep_1122・keep_1129・keep_1129_rej・keep_1136・keep_1136_rej は INSRAW でもヌル腕でもない（作者 2026-11-11「再開」で答えの無かった D: の問いを推奨 (a) で採った＝消してよい）。\nINSRAW=D:/PoB_work/keep_1143/base_ins\n')
one('BASE_COMMIT=b6d35e2c7\n', '# (1143) 新しい基線の測った木（INSRAW keep_1143/base_ins を出した木）。\nBASE_COMMIT=' + H + '\n')
one('TESTS_NONFULL=2844\n', '# (1143)：2844＋c111tf 3（TradeBilateralInstrumentTests T1b・T6・T7）＋c111sm 2（StateFactoryTests 1・TransportInstrumentTests 1）＋c110lo 3（RailLocoDiscoveryTests）＋c111rb 3（RailNeedByOutputTests）＋c111tp 3（TreatyPortHomeOnlyTests）＝2858＝Tests 2624＋Tests.Ui 234（合わせた木の --list-tests で確かめた）。\nTESTS_NONFULL=2858\n')
one('NG_BASE=rd:measurements/ng1122_20261114:m1122\n', 'NG_BASE=rd:measurements/ng1143_20261115:m1143\n')
one('STAGE_BASE=b6d35e2c7a3dd73459f99fe280696fc2e58b1479\n', '# (1143) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
