# (1348) 合流：ident.cfg を新しい基線 m1348 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1341/cfg1341.py の写し）。
#   python -X utf8 -I cfg1348.py <ident.cfg> <BASE_COMMIT>
import sys
p = sys.argv[1]; BC = sys.argv[2]
b = open(p, 'rb').read()
nl = b'
' if b'
' in b else b'
'
def rep(old, new, before=None):
    global b
    o = old.encode('utf-8'); n = new.encode('utf-8')
    assert b.count(o) == 1, old
    if before is not None:
        n = before.encode('utf-8') + nl + n
    b = b.replace(o, n)
rep('B5=measurements/b5_20261124_m1341', 'B5=measurements/b5_20261125_m1348')
rep('B10INS=measurements/b10_20261124_m1341_ins', 'B10INS=measurements/b10_20261125_m1348_ins')
rep('MON=measurements/mon_20261124_m1341.txt', 'MON=measurements/mon_20261125_m1348.txt')
rep('INSRAW=D:/PoB_work/keep_1341/base_ins', 'INSRAW=D:/PoB_work/keep_1348/base_ins',
    before='# (1348) INSRAW を keep_1348/base_ins へ＝採った木 m1348（cn・wd〔旗 WarmupNoDecisions 1〕・lr は外した）の M（アブレーション A_wd）の計器つき 10 種の全表（keepsum は measurements/m1348_20261125/keepsum_insraw1348.tsv）。')
rep('BASE_COMMIT=4274145d3', 'BASE_COMMIT=' + BC,
    before='# (1348) 新しい基線の測った木（INSRAW keep_1348/base_ins を出した木）。')
rep('TESTS_NONFULL=3176', 'TESTS_NONFULL=3183',
    before='# (1348)：3176＋c141cn 3（CoercionNowTests）＋c141wd 4（WarmupDecisionSymmetryTests）＝3183（lr は外した）。')
rep('NG_BASE=rd:measurements/ng1341_20261124:m1341', 'NG_BASE=rd:measurements/ng1348_20261125:m1348')
rep('STAGE_BASE=4274145d3', 'STAGE_BASE=' + BC,
    before='# (1348) STAGE_BASE は新しい基線の測った木（cn・wd の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1348) 合流（2026-11-25）：恒等の線 c141cn と挙動の線 c141wd を採り、挙動の線 c141lr は外した（wd＋lr の M で空洞化 1/10 未満〈線が原因〉1 件＝種 8 バーミンガム・アブレーションの規則〈後から入れた側を外す〉）・c141k59 は線の担当が採らないで止めた＝基準を m1348 へ（B5・B10INS・MON・INSRAW・BASE_COMMIT・TESTS_NONFULL・NG_BASE・STAGE_BASE）。腕 Z（ident m1348z・段 2・cn＋wd〔旗 0〕＋lr〔旗 0〕）〈判定: 恒等〉。KEEPNULL keep_1348（n1・n2・a11）＝n1・n2 は push の後に回す。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
