# (1150) 合流（m1143/cfg1143.py の型・基線を m1143→m1150 へ）。バイトで読み書き。
#   python -X utf8 cfg1150.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1143（★(1143)'); k1 = b.index('\n', k0)
old_head = b[k0:k1]
b = b[:k0] + ('# 現在の基準＝基線 m1150（★(1150) 合流〔波112〕＝挙動の線 c112hy〔(1145) D-b ハイエナ抑制の枝を消す・係数 DefeatShockTensionRelief 既定 0〕を採った・'
              '恒等の線 c112tt〔(1146) tf と sm の残り・試験と台本だけ〕・c112cm〔(1147) 段 C′ の計器 (b)(c)(d)・rponset_s・rule_s の 10 列・trrail_s の 1 列・CfForeignPort〕を入れた・挙動の線 c112t1〔(1144)〕は止める線〈空洞化〉と門 W2(a) で外した・'
              '腕 Z〔ident m1150z・合わせた木＋DefeatShockTensionRelief を 2.0 へ戻す使い捨ての木 58c7255d4・段 2〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261115_m1150・B10INS b10_20261115_m1150_ins・MON mon_20261115_m1150.txt・INSRAW keep_1150/base_ins〔M の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2870〔Tests 2636＋Tests.Ui 234〕・NG_BASE ng1150）。前の行：' + old_head[2:]) + b[k1:]
one('B5=measurements/b5_20261115_m1143\n', 'B5=measurements/b5_20261115_m1150\n')
one('B10INS=measurements/b10_20261115_m1143_ins\n', 'B10INS=measurements/b10_20261115_m1150_ins\n')
one('MON=measurements/mon_20261115_m1143.txt\n', 'MON=measurements/mon_20261115_m1150.txt\n')
one('INSRAW=D:/PoB_work/keep_1143/base_ins\n', '# (1150) から keep_1150/base_ins（合わせた木 tt＋cm＋hy の M の計器つき 10 種の生・木 ' + H + '・全表・rponset_s・dipcool_s あり・rule_s は +10 列・trrail_s は +1 列）。ヌル腕 keep_1150/n1・n2（全表・POB_CEARN=0・dipcool_s あり）・種 11〜20 は keep_1150/a11（SMALL＋BYPASS＋railspend_s・trrail_s・trnation_s・tport_s・dipcool_s・warband_s・warpair_s）。keep_1143 は残す（指示〈keep_1143 は誰も消さない〉・古い keep_* を消すのは作者のセッション）。\nINSRAW=D:/PoB_work/keep_1150/base_ins\n')
one('BASE_COMMIT=0a8fff47b\n', '# (1150) 新しい基線の測った木（INSRAW keep_1150/base_ins を出した木）。\nBASE_COMMIT=' + H + '\n')
one('TESTS_NONFULL=2858\n', '# (1150)：2858＋c112tt 1（TransportInstrumentTests 1）＋c112cm 8（RuleOnsetInstrumentTests）＋c112hy 3（HyenaBranchTests H1〜H3）＝2870＝Tests 2636＋Tests.Ui 234（合わせた木の --list-tests で確かめた）。\nTESTS_NONFULL=2870\n')
one('NG_BASE=rd:measurements/ng1143_20261115:m1143\n', 'NG_BASE=rd:measurements/ng1150_20261115:m1150\n')
one('STAGE_BASE=0a8fff47b89baec5dd57a3294656503ffc765869\n', '# (1150) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
