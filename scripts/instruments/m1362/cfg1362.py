# (1362) 合流：ident.cfg を新しい基線 m1362 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1355/cfg1355.py の写し）。
#   python -X utf8 -I cfg1362.py <ident.cfg>
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
rep('B5=measurements/b5_20261125_m1355', 'B5=measurements/b5_20261125_m1362')
rep('B10INS=measurements/b10_20261125_m1355_ins', 'B10INS=measurements/b10_20261125_m1362_ins')
rep('MON=measurements/mon_20261125_m1355.txt', 'MON=measurements/mon_20261125_m1362.txt')
rep('INSRAW=D:/PoB_work/keep_1355/base_ins', 'INSRAW=D:/PoB_work/keep_1362/base_ins',
    before='# (1362) INSRAW を keep_1362/base_ins へ＝採った木 m1362（434325065＝sg・ce・k59〔旗 FemalePartLawFree 1〕・sp〔旗 StateProcureAsSold 1〕）の M の計器つき 10 種の全表（tsreg_s・cwend_s・stproc_s を含む）。')
rep('BASE_COMMIT=98d7a5283', 'BASE_COMMIT=434325065',
    before='# (1362) 新しい基線の測った木（INSRAW keep_1362/base_ins を出した木 m1362）。')
rep('TESTS_NONFULL=3199', 'TESTS_NONFULL=3217',
    before='# (1362)：3199＋c143sg 3（TsregInstrumentTests）＋c143ce 5（CwendInstrumentTests）＋c143k59 3（FemalePartLawFreeTests）＋c143sp 7（StateProcureLedgerTests）＝3217＝Tests 2985＋Tests.Ui 232（採った木の --list-tests で確かめた）。')
rep('NG_BASE=rd:measurements/ng1355_20261125:m1355', 'NG_BASE=rd:measurements/ng1362_20261125:m1362')
rep('STAGE_BASE=98d7a5283', 'STAGE_BASE=434325065',
    before='# (1362) STAGE_BASE は新しい基線の測った木 434325065（k59・sp の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1362) 合流（2026-11-25）：恒等の線 c143sg・c143ce と挙動の線 c143k59・c143sp を採った＝新しい基線 m1362（合わせた木の M と S11 で止める線 0・空洞化 1/10 未満 0・腕 Z〈判定: 恒等〉）。KEEPNULL は keep_1355/n1・n2 と keep_1362/a11（N1・N2 は push の後に回す＝★(1334)）。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
