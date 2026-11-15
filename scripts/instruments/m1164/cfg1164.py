# (1164) 合流（m1157/cfg1157.py の型・基線を m1157→m1164 へ）。バイトで読み書き。
#   python -X utf8 cfg1164.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH> <ヌル腕の形〔全表|絞った〕>
import sys
p, H, HF, SH, NK = sys.argv[1:6]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1157（★(1157)'); k1 = b.index('\n', k0)
old_head = b[k0:k1]
b = b[:k0] + ('# 現在の基準＝基線 m1164（★(1164) 合流〔波114〕＝挙動の線 c114qn〔(1158) 植民地の毎月のキニーネ D2・係数 MalariaQuininePerColony 6.0→0.0〕を採った・'
              '恒等の線 c114sr〔(1160) sw の残り・D-r1・D-r2・D4 の region の 2 列〕と c114t1 の段 T1-0〔(1159) 恒等の計器と漏れの直し・A-1 の器・旗 TradeBuyBySupplyCurve 既定 0〕を入れた・'
              '腕 Z〔ident m1164z・合わせた木＋MalariaQuininePerColony を 6.0 へ戻す使い捨ての木 01f659872・段 2〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261115_m1164・B10INS b10_20261115_m1164_ins・MON mon_20261115_m1164.txt・INSRAW keep_1164/base_ins〔M の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2889〔Tests 2655＋Tests.Ui 234〕・NG_BASE ng1164）。前の行：' + old_head[2:]) + b[k1:]
one('B5=measurements/b5_20261115_m1157\n', 'B5=measurements/b5_20261115_m1164\n')
one('B10INS=measurements/b10_20261115_m1157_ins\n', 'B10INS=measurements/b10_20261115_m1164_ins\n')
one('MON=measurements/mon_20261115_m1157.txt\n', 'MON=measurements/mon_20261115_m1164.txt\n')
one('INSRAW=D:/PoB_work/keep_1157/base_ins\n', '# (1164) から keep_1164/base_ins（合わせた木 sr＋t1〔旗 0〕＋qn の M の計器つき 10 種の生・木 ' + H + '・全表・t1buy_s と colqn_s あり・tb_s の 16 列は州の名目の単位・region_s の settler の 2 列なし）。ヌル腕 keep_1164/n1・n2（' + NK + '・POB_CEARN=0）・種 11〜20 は keep_1164/a11（SMALL＋BYPASS＋railspend_s・trrail_s・trnation_s・tport_s・dipcool_s・warband_s・warpair_s・disccontact_s・colqn_s）・a11z（旗 0＝MalariaQuininePerColony 6.0・colqn_s）。keep_1150・keep_1157・keep_1157_rej は残す（古い keep_* を消すのは作者のセッション）。採らなかった腕の生は keep_1164_rej。\nINSRAW=D:/PoB_work/keep_1164/base_ins\n')
one('BASE_COMMIT=b39fe1ce4\n', '# (1164) 新しい基線の測った木（INSRAW keep_1164/base_ins を出した木）。\nBASE_COMMIT=' + H + '\n')
one('TESTS_NONFULL=2874\n', '# (1164)：2874＋c114t1 10（TradeT1Tests）＋c114qn 5（ColonialQuinineTests）＝2889＝Tests 2655＋Tests.Ui 234（合わせた木の --list-tests で確かめた）。\nTESTS_NONFULL=2889\n')
one('NG_BASE=rd:measurements/ng1157_20261115:m1157\n', 'NG_BASE=rd:measurements/ng1164_20261115:m1164\n')
one('STAGE_BASE=b39fe1ce457b263c0df37bccdeff9311ae4ef0d6\n', '# (1164) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
