# (1001) 合流（m988/cfg988.py の写し・基線を m988→m1001 へ）
#   python -X utf8 cfg1001.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, SH, Z = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m988 のまま（★(994) 合流〔波90〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1001（★(1001) 合流〔波91〕＝挙動の線 c91sl〔(997) ⑯ B-1 段 1〜2 の公売・旗 LandShareTaxSale 既定 1〕を採った新しい基線・'
              '恒等の線 c91lc〔(995) B-1 の写しの直し〕・台本だけの c91rx〔(996) 州の出入り〕を入れた・腕 Z〔ident m1001z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261109_m1001・B10INS b10_20261109_m1001_ins・MON mon_20261109_m1001.txt・INSRAW keep_1001/base_ins〔M の計器つき 10 種の生〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2524・NG_BASE ng1001）'
              '。もとは基線 m988 のまま（') + b[k0 + k1 + len('基線 m988 のまま（'):]
one('B5=measurements/b5_20261109_m988\n', 'B5=measurements/b5_20261109_m1001\n')
one('B10INS=measurements/b10_20261109_m988_ins\n', 'B10INS=measurements/b10_20261109_m1001_ins\n')
one('MON=measurements/mon_20261109_m988.txt\n', 'MON=measurements/mon_20261109_m1001.txt\n')
one('INSRAW=D:/PoB_work/keep_994/base_ins\n', 'INSRAW=D:/PoB_work/keep_1001/base_ins\n')
one('BASE_COMMIT=bdb3e1b9\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (994) から keep_994/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (1001) から keep_1001/base_ins（合わせた木 lc＋rx＋sl の M〔LandShareTaxSale 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_1001/n1・n2・種 11〜20 keep_1001/a11（POB_CEARN=0）。'
             'keep_988・keep_994・keep_994_rej は残す（古い keep_* を消すのは作者のセッション）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2521\n')
b = b[:k] + '# (1001)：2521＋c91lc 3（LandsaleInstrumentTests の T8〜T10）＋c91sl 0＋c91rx 0＝2524（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2521\n', 'TESTS_NONFULL=2524\n')
one('NG_BASE=rd:measurements/ng988_20261109:m988\n', 'NG_BASE=rd:measurements/ng1001_20261109:m1001\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
