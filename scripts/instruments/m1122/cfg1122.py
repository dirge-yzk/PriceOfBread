# (1122) 合流（m1103/cfg1103.py の型・基線を m1103→m1122 へ）。バイトで読み書き。
#   python -X utf8 cfg1122.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1103 のまま（★(1116)'); k1 = b.index('\n', k0)
b = b[:k0] + ('# 現在の基準＝基線 m1122（★(1122) 合流〔波108〕＝挙動の線 c108ps〔(1118) 開院の空白を閉じる・旗 ParliamentSeatsWithoutGap 既定 1〕・c108mc〔(1119) 鉱山の天井を ScaleMax へ・旗 MineralScaleCapLift 既定 1〕・'
              '恒等の線 c108sm〔(1120) 走査の番人の見分け・画面の試験 2 本・docs/history の名・文書〕を入れた・c108rc〔(1117)〕は自分の門 C3 で止まった＝記録だけ・'
              '腕 Z〔ident m1122z・合わせた木＋旗の既定を 0 へ戻す使い捨ての木 0e8f0d01b〕〈判定: 恒等〉・アブレーション A_ps≡ps の腕 B・A_mc≡mc の腕 B・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261114_m1122・B10INS b10_20261114_m1122_ins・MON mon_20261114_m1122.txt・INSRAW keep_1122/base_ins〔M の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2833〔Tests 2599＋Tests.Ui 234〕・NG_BASE ng1122）') + b[k1:]
one('B5=measurements/b5_20261113_m1103\n', 'B5=measurements/b5_20261114_m1122\n')
one('B10INS=measurements/b10_20261113_m1103_ins\n', 'B10INS=measurements/b10_20261114_m1122_ins\n')
one('MON=measurements/mon_20261113_m1103.txt\n', 'MON=measurements/mon_20261114_m1122.txt\n')
one('INSRAW=D:/PoB_work/keep_1116/base_ins\n', '# (1122) から keep_1122/base_ins（合わせた木 sm＋ps＋mc の M の計器つき 10 種の生・木 ' + H + '・全表）。ヌル腕 keep_1122/n1・n2（全表・POB_CEARN=0）・種 11〜20 は keep_1122/a11（POB_CEARN=0・SMALL＋parlseat_s・prds_s・recipeout_s・minecap_s・metal_s）。\nINSRAW=D:/PoB_work/keep_1122/base_ins\n')
one('BASE_COMMIT=4624c4acd\n', '# (1122) 新しい基線の測った木（INSRAW keep_1122/base_ins を出した木）。\nBASE_COMMIT=' + H + '\n')
one('TESTS_NONFULL=2826\n', '# (1122)：2826＋c108sm 3（SourceScanCoverageTests 1・Tests.Ui の PopWeightRegimeTextTests 2）＋c108ps 3（ParliamentSeatsWithoutGapTests）＋c108mc 1（EconomyTests）＝2833＝Tests 2599＋Tests.Ui 234（合わせた木の --list-tests で確かめた）。\nTESTS_NONFULL=2833\n')
one('NG_BASE=rd:measurements/ng1103_20261113:m1103\n', 'NG_BASE=rd:measurements/ng1122_20261114:m1122\n')
one('STAGE_BASE=4624c4acd32667dcfd99e0aac9b2ac84ffcd60ee\n', '# (1122) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
