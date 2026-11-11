# (1027) 合流（m1022/cfg1022.py の写し・基線を m1022→m1027 へ）
#   python -X utf8 cfg1027.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, SH, Z = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1022（★(1022) 合流〔波95〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1027（★(1027) 合流〔波96〕＝挙動の線 c96rd〔(1025) 段0″a 資源のデータ world.json の 10 行・旗 ResourceRowsFix 既定 1〕を採った新しい基線・'
              '恒等の線 c96wn〔(1023) 戦争の計器 warentry_s・warpair_s・warband_s・wars_s・invade_s の列〕・c96kf〔(1024) 計器 cscx_s・pxstep_s・merch_s の末尾 7 列〕を入れた・腕 Z〔ident m1027z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261110_m1027・B10INS b10_20261110_m1027_ins・MON mon_20261110_m1027.txt・INSRAW keep_1027/base_ins〔M の計器つき 10 種の生・cscx_s・pxstep_s・warentry_s ほかを含む〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2642・NG_BASE ng1027）'
              '。もとは基線 m1022（') + b[k0 + k1 + len('基線 m1022（'):]
one('B5=measurements/b5_20261110_m1022\n', 'B5=measurements/b5_20261110_m1027\n')
one('B10INS=measurements/b10_20261110_m1022_ins\n', 'B10INS=measurements/b10_20261110_m1027_ins\n')
one('MON=measurements/mon_20261110_m1022.txt\n', 'MON=measurements/mon_20261110_m1027.txt\n')
one('INSRAW=D:/PoB_work/keep_1022/base_ins\n', 'INSRAW=D:/PoB_work/keep_1027/base_ins\n')
one('BASE_COMMIT=131fd1d2\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (1022) から keep_1022/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (1027) から keep_1027/base_ins（合わせた木 wn＋kf＋rd の M〔ResourceRowsFix 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_1027/n1・n2・種 11〜20 keep_1027/a11（POB_CEARN=0）。'
             'keep_1017・keep_1022 は残す（古い keep_* を消すのは作者のセッション）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2607\n')
b = b[:k] + '# (1027)：2607＋c96wn 18（WarInstrumentTests）＋c96kf 10（ClearingFillInstrumentTests）＋c96rd 7（ResourceRowsTests）＝2642（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2607\n', 'TESTS_NONFULL=2642\n')
one('NG_BASE=rd:measurements/ng1022_20261110:m1022\n', 'NG_BASE=rd:measurements/ng1027_20261110:m1027\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
