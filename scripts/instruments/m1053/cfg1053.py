# (1053) 合流（m1044/cfg1044.py の型・基線を m1044→m1053 へ）
#   python -X utf8 cfg1053.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, SH, Z = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1044（★(1044)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1053（★(1053) 合流〔波99〕＝挙動の線 c99db〔(1048) 地主の政治 D-1b 国教＋D-2 仲介の取り分・旗 LordReligionStake・LordIntermediaryStake 既定 1〕を採った新しい基線・'
              '恒等の線 c99cd〔(1045) C4d と計器の直しの束・tradeskip_s の末尾 16 列・sellr_s の末尾 rentRMul・台帳の行 LaborPowerScans〕を入れた・c99ov〔(1047)〕は自分の門で採らない・c99dv〔(1046)〕は ident なしで外した・腕 Z〔ident m1053z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261112_m1053・B10INS b10_20261112_m1053_ins・MON mon_20261112_m1053.txt・INSRAW keep_1053/base_ins〔M の計器つき 10 種の生・lordstat_s の末尾 2 列・tradeskip_s の末尾 16 列・sellr_s の末尾 1 列を含む〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2689・NG_BASE ng1053）'
              '。もとは') + b[k0 + k1:]
one('B5=measurements/b5_20261111_m1044\n', 'B5=measurements/b5_20261112_m1053\n')
one('B10INS=measurements/b10_20261111_m1044_ins\n', 'B10INS=measurements/b10_20261112_m1053_ins\n')
one('MON=measurements/mon_20261111_m1044.txt\n', 'MON=measurements/mon_20261112_m1053.txt\n')
one('INSRAW=D:/PoB_work/keep_1044/base_ins\n', '# (1053) から keep_1053/base_ins（合わせた木 cd＋db の M〔LordReligionStake 1・LordIntermediaryStake 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_1053/n1・n2・種 11〜20 keep_1053/a11（POB_CEARN=0）。keep_1044 は INSRAW でもヌル腕でもない＝消してよい（作者 2026-11-11「再開」で答えの無かった D: の問いを推奨 (a) で採った）。\nINSRAW=D:/PoB_work/keep_1053/base_ins\n')
one('BASE_COMMIT=b59dac03\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2673\n')
b = b[:k] + '# (1053)：2673＋c99cd 9（TradeSkipC4dTests 8・WriterRatchetTests の InlineData 1）＋c99db 7（LordStakeD1bD2Tests 5・LordstatD1aTests 2）＝2689（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2673\n', 'TESTS_NONFULL=2689\n')
one('NG_BASE=rd:measurements/ng1044_20261111:m1044\n', 'NG_BASE=rd:measurements/ng1053_20261112:m1053\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
