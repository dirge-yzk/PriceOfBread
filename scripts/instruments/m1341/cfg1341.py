# (1341) 合流：ident.cfg を新しい基線 m1341 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1334/cfg1334.py の写し）。
#   python -X utf8 -I cfg1341.py <ident.cfg>
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
rep('B5=measurements/b5_20261124_m1334', 'B5=measurements/b5_20261124_m1341')
rep('B10INS=measurements/b10_20261124_m1334_ins', 'B10INS=measurements/b10_20261124_m1341_ins')
rep('MON=measurements/mon_20261124_m1334.txt', 'MON=measurements/mon_20261124_m1341.txt')
rep('INSRAW=D:/PoB_work/keep_1334/base_ins', 'INSRAW=D:/PoB_work/keep_1341/base_ins',
    before='# (1341) INSRAW を keep_1341/base_ins へ＝採った木 m1341（4274145d3＝nt・dz・hf〔旗 DomesticStayFoodFill 1〕・sp は外した）の M（アブレーション A_hf・線 hf の腕 B と成績表が全行一致）の計器つき 10 種の全表（keepsum は measurements/m1341_20261124/keepsum_insraw1341.tsv）。')
rep('BASE_COMMIT=19ce55583', 'BASE_COMMIT=4274145d3',
    before='# (1341) 新しい基線の測った木（INSRAW keep_1341/base_ins を出した木 m1341h）。')
rep('TESTS_NONFULL=3177', 'TESTS_NONFULL=3176',
    before='# (1341)：3177＋c140nt 2（InstrumentHookClosureTests）−c140dz 7（RevoltIgnitionTests の段5）＋c140hf 4（HungerFlightTests）＝3176＝Tests 2944＋Tests.Ui 232（採った木の --list-tests で確かめた・sp は外した）。')
rep('NG_BASE=rd:measurements/ng1334_20261124:m1334', 'NG_BASE=rd:measurements/ng1341_20261124:m1341')
rep('STAGE_BASE=19ce55583', 'STAGE_BASE=4274145d3',
    before='# (1341) STAGE_BASE は新しい基線の測った木 4274145d3（nt・dz・hf の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1341) 合流（2026-11-24）：恒等の線 c140nt・c140dz と挙動の線 c140hf を採り、挙動の線 c140sp は外した（hf＋sp の M で空洞化 1/10 未満〈線が原因〉1 件＝種 1 フォートウィリアム・アブレーションで単独はどちらも通る＝規則〈後から入れた側を外す〉）＝基準を m1341 へ（B5・B10INS・MON・INSRAW・BASE_COMMIT・TESTS_NONFULL・NG_BASE・STAGE_BASE）。腕 Z（ident m1341z・段 2・nt＋dz＋hf〔旗 0〕＋sp〔旗 0 の器〕）〈判定: 恒等〉。KEEPNULL keep_1341（n1・n2・a11）＝n1・n2 は push の後に回す。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
