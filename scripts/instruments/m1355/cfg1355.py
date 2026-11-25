# (1355) 合流：ident.cfg を新しい基線 m1355 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1341/cfg1341.py の写し）。
#   python -X utf8 -I cfg1355.py <ident.cfg>
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
rep('B5=measurements/b5_20261124_m1341', 'B5=measurements/b5_20261125_m1355')
rep('B10INS=measurements/b10_20261124_m1341_ins', 'B10INS=measurements/b10_20261125_m1355_ins')
rep('MON=measurements/mon_20261124_m1341.txt', 'MON=measurements/mon_20261125_m1355.txt')
rep('INSRAW=D:/PoB_work/keep_1341/base_ins', 'INSRAW=D:/PoB_work/keep_1355/base_ins',
    before='# (1355) INSRAW を keep_1355/base_ins へ＝採った木 m1355（98d7a5283＝ik・ds〔旗 DestFoodSymmetric 1〕・lr〔旗 ProtectedLawsReversible 1〕）の M の計器つき 10 種の全表（femp_s・migcap_s・lrlock_s を含む・SOURCE-HASH 9a067c165f719333）。')
rep('BASE_COMMIT=4274145d3', 'BASE_COMMIT=98d7a5283',
    before='# (1355) 新しい基線の測った木（INSRAW keep_1355/base_ins を出した木 m1355）。')
rep('TESTS_NONFULL=3179', 'TESTS_NONFULL=3199',
    before='# (1355)：3179＋c142ik 5（FempInstrumentTests 4・IsPlayerBranchCountTests 1）＋c142ds 5（DestFoodSymmetryTests）＋c142lr 10（ProtectedLawReversalTests）＝3199＝Tests 2967＋Tests.Ui 232（採った木の --list-tests で確かめた）。')
rep('NG_BASE=rd:measurements/ng1341_20261124:m1341', 'NG_BASE=rd:measurements/ng1355_20261125:m1355')
rep('STAGE_BASE=4274145d3', 'STAGE_BASE=98d7a5283',
    before='# (1355) STAGE_BASE は新しい基線の測った木 98d7a5283（ds・lr の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1355) 合流（2026-11-25）：恒等の線 c142ik と挙動の線 c142ds・c142lr を採った＝新しい基線 m1355（合わせた木の M と S11 で止める線 0・空洞化 1/10 未満 0・腕 Z〈判定: 恒等〉）。KEEPNULL は keep_1355/n1・n2・a11（N1・N2 は push の後に回す＝★(1334)）。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
