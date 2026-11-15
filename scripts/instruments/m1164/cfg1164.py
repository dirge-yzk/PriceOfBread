# (1164) 合流（m1157/cfg1157.py の写し・木と作業フォルダ・名を m1164 へ・下の行の m1164 は写しの置き換え＝元は m1157）
# (1157) 合流（m1150/cfg1150.py の型・基線を m1150→m1164 へ）。バイトで読み書き。
#   python -X utf8 cfg1157.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH> <ヌル腕の形〔全表|絞った〕>
import sys
p, H, HF, SH, NK = sys.argv[1:6]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1150（★(1150)'); k1 = b.index('\n', k0)
old_head = b[k0:k1]
b = b[:k0] + ('# 現在の基準＝基線 m1164（★(1157) 合流〔波113〕＝挙動の線 c113lc〔(1152) 機関車の門を世界の発見の和で読む・旗 RailLocoGateWorld 既定 1〕を採った・'
              '恒等の線 c113sw〔(1154) D-b の物理の削除・小物・S26 の D4・D5〕を入れた・挙動の線 c113cr〔(1151)〕は事前登録 F18 で止まった・c113qn〔(1153)〕は腕 B に至らず・どちらも入れていない・'
              '腕 Z〔ident m1164z・合わせた木＋RailLocoGateWorld を 0 へ戻す使い捨ての木 8f7d415b3・段 2〕・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261115_m1164・B10INS b10_20261115_m1164_ins・MON mon_20261115_m1164.txt・INSRAW keep_1157/base_ins〔M の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2874〔Tests 2640＋Tests.Ui 234〕・NG_BASE ng1157）。前の行：' + old_head[2:]) + b[k1:]
one('B5=measurements/b5_20261115_m1150\n', 'B5=measurements/b5_20261115_m1164\n')
one('B10INS=measurements/b10_20261115_m1150_ins\n', 'B10INS=measurements/b10_20261115_m1164_ins\n')
one('MON=measurements/mon_20261115_m1150.txt\n', 'MON=measurements/mon_20261115_m1164.txt\n')
one('INSRAW=D:/PoB_work/keep_1150/base_ins\n', '# (1157) から keep_1157/base_ins（合わせた木 sw＋lc の M の計器つき 10 種の生・木 ' + H + '・全表・railspend_s は +2 列）。ヌル腕 keep_1157/n1・n2（' + NK + '・POB_CEARN=0）・種 11〜20 は keep_1157/a11（SMALL＋BYPASS＋railspend_s・trrail_s・trnation_s・tport_s・dipcool_s・warband_s・warpair_s・disccontact_s）。keep_1150 は残す（ヌル腕の全表の幅・種 11〜20 の旗 0）。\nINSRAW=D:/PoB_work/keep_1157/base_ins\n')
one('BASE_COMMIT=d05c6bea9\n', '# (1157) 新しい基線の測った木（INSRAW keep_1157/base_ins を出した木）。\nBASE_COMMIT=' + H + '\n')
one('TESTS_NONFULL=2870\n', '# (1157)：2870＋c113lc 5（RailLocoGateWorldTests T1〜T5）−c113sw 1（HyenaBranchTests H1）＝2874＝Tests 2640＋Tests.Ui 234（合わせた木の --list-tests で確かめた）。\nTESTS_NONFULL=2874\n')
one('NG_BASE=rd:measurements/ng1150_20261115:m1150\n', 'NG_BASE=rd:measurements/ng1157_20261115:m1164\n')
one('STAGE_BASE=d05c6bea911cab6aaeee92a7cc3ac2a4f9e7b54d\n', '# (1157) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
