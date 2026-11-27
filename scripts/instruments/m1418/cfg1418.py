# (1418) 合流：ident.cfg を新しい基線 m1418 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1411/cfg1411.py の写し）。
#   python -X utf8 -I cfg1418.py <ident.cfg>
import sys
p = sys.argv[1]
b = open(p, 'rb').read()
nl = b'\r\n' if b'\r\n' in b else b'\n'
def rep(old, new, before=None):
    global b
    o = old.encode('utf-8'); n = new.encode('utf-8')
    assert b.count(o) == 1, old
    if before is not None:
        n = before.encode('utf-8') + nl + n
    b = b.replace(o, n)
rep('B5=measurements/b5_20261127_m1411', 'B5=measurements/b5_20261127_m1418')
rep('B10INS=measurements/b10_20261127_m1411_ins', 'B10INS=measurements/b10_20261127_m1418_ins')
rep('MON=measurements/mon_20261127_m1411.txt', 'MON=measurements/mon_20261127_m1418.txt')
rep('INSRAW=D:/PoB_work/keep_1411/base_ins', 'INSRAW=D:/PoB_work/keep_1418/base_ins',
    before='# (1418) INSRAW を keep_1418/base_ins へ＝採った木 m1418（f3a9c151c＝rb〔恒等〕・lk〔WarLuck 0〕・fl〔旗 RepayFloorOnly 1〕）の M の計器つき 10 種の全表 1825 本。')
rep('BASE_COMMIT=36abd290b', 'BASE_COMMIT=f3a9c151c',
    before='# (1418) 新しい基線の測った木（INSRAW keep_1418/base_ins を出した木 m1418・SimCore は main の木と同じ）。')
rep('TESTS_NONFULL=3333', 'TESTS_NONFULL=3327',
    before='# (1418)：3333−c151rb 14＋c151lk 3（WarLuckZeroTests）＋c151fl 5（RepayFloorOnlyTests）＝3327（合わせた木の --list-tests で数えた・Tests 3095＋Tests.Ui 232）。')
rep('NG_BASE=rd:measurements/ng1411_20261127:m1411', 'NG_BASE=rd:measurements/ng1418_20261127:m1418')
rep('STAGE_BASE=36abd290b', 'STAGE_BASE=f3a9c151c',
    before='# (1418) STAGE_BASE は新しい基線の測った木 f3a9c151c（lk の WarLuck 0・fl の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1418) 合流（2026-11-27）：恒等の線 c151rb と挙動の線 c151lk・c151fl を採った＝新しい基線 m1418（腕 Z〔ident m1418z・段 2〕〈判定: 恒等〉・合わせた木の M と S11 で止める線 0・lk の G1′ は S11 で 0）。c151wr は ident 段 2 が未実施で外した。' + nl.decode() +
      '# (1418) KEEPNULL は keep_1418/n1・a11（木 f3a9c151c・SupplySlope 0.50000001・m1411 の 87 表＋wexit_s・fsx_s＝89 表の名簿）と、N2 だけ旧の keep_1411/n2（D: の空きで新しい N2 を回せなかった＝次の合流か作者のセッションで回す）。' + nl.decode()).encode('utf-8')
open(p, 'wb').write(b)
print('ok')
