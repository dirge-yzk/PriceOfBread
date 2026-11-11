# (1044) 合流（m1027/cfg1027.py の型・基線を m1027→m1044 へ）
#   python -X utf8 cfg1044.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, SH, Z = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1027 のまま（★(1035)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1044（★(1044) 合流〔波98〕＝挙動の線 c98lw〔(1037) 労働力の重み・旗 LaborWeightsByPower 既定 1〕を採った新しい基線・'
              '恒等の線 c98lc〔(1036) 台帳の行 ClearingSkipsLB を評価した件数へ・tradeskip_s の末尾 12 列・新しい表 sellr_s〕・c98da〔(1039) D-1a 国教の器と D-2 の受け口・lordstat_s の末尾 22 列〕を入れた・c98wr〔(1038)〕は外した・腕 Z〔ident m1044z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261111_m1044・B10INS b10_20261111_m1044_ins・MON mon_20261111_m1044.txt・INSRAW keep_1044/base_ins〔M の計器つき 10 種の生・lwk_s・sellr_s・tradeskip_s と lordstat_s の末尾の列を含む〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2673・NG_BASE ng1044）'
              '。もとは') + b[k0 + k1:]
one('B5=measurements/b5_20261110_m1027\n', 'B5=measurements/b5_20261111_m1044\n')
one('B10INS=measurements/b10_20261110_m1027_ins\n', 'B10INS=measurements/b10_20261111_m1044_ins\n')
one('MON=measurements/mon_20261110_m1027.txt\n', 'MON=measurements/mon_20261111_m1044.txt\n')
one('INSRAW=D:/PoB_work/keep_1035/base_ins\n', '# (1044) から keep_1044/base_ins（合わせた木 da＋lc＋lw の M〔LaborWeightsByPower 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_1044/n1・n2・種 11〜20 keep_1044/a11（POB_CEARN=0）。keep_1027・keep_1035 は INSRAW でもヌル腕でもない＝消してよい（作者 2026-11-11「再開」で答えの無かった D: の問いを推奨 (a) で採った）。\nINSRAW=D:/PoB_work/keep_1044/base_ins\n')
one('BASE_COMMIT=9e08cb73\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2651\n')
b = b[:k] + '# (1044)：2651＋c98lc 9（TradeSkipC4Tests 8・ClearingLowerBoundTests T5 1）＋c98da 6（LordstatD1aTests）＋c98lw 7（LaborWeightTests）＝2673（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2651\n', 'TESTS_NONFULL=2673\n')
one('NG_BASE=rd:measurements/ng1027_20261110:m1027\n', 'NG_BASE=rd:measurements/ng1044_20261111:m1044\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
