# (1334) 合流：ident.cfg を新しい基線 m1334 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1328/cfg1328.py の写し）。
#   python -X utf8 -I cfg1334.py <ident.cfg>
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
rep('B5=measurements/b5_20261124_m1328', 'B5=measurements/b5_20261124_m1334')
rep('B10INS=measurements/b10_20261124_m1328_ins', 'B10INS=measurements/b10_20261124_m1334_ins')
rep('MON=measurements/mon_20261124_m1328.txt', 'MON=measurements/mon_20261124_m1334.txt')
rep('INSRAW=D:/PoB_work/keep_1328/base_ins', 'INSRAW=D:/PoB_work/keep_1334/base_ins',
    before='# (1334) INSRAW を keep_1334/base_ins へ＝採った木 m1334（19ce55583＝sm・sl・nc〔旗 NextCapitalCoreFirst 1〕）の M の計器つき 10 種の全表（wars_s は末尾 +2 列・capcol_s は末尾 +8 列・keepsum は measurements/m1334_20261124/keepsum_insraw1334.tsv）。')
rep('BASE_COMMIT=6c170add5', 'BASE_COMMIT=19ce55583',
    before='# (1334) 新しい基線の測った木（INSRAW keep_1334/base_ins を出した木 m1334）。')
rep('TESTS_NONFULL=3169', 'TESTS_NONFULL=3177',
    before='# (1334)：3169＋c139sm 1（PolicyPoliticsTests）＋c139sl 1（WarsVerdictColumnTests）＋c139nc 6（NextCapitalHomeFirstTests）＝3177＝Tests 2945＋Tests.Ui 232（腕 Z〔ident m1334z〕と採った木の --list-tests で確かめた）。')
rep('NG_BASE=rd:measurements/ng1328_20261124:m1328', 'NG_BASE=rd:measurements/ng1334_20261124:m1334')
rep('STAGE_BASE=6c170add5', 'STAGE_BASE=19ce55583',
    before='# (1334) STAGE_BASE は新しい基線の測った木 19ce55583（sm・sl・nc の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1334) 合流（2026-11-24）：恒等の線 c139sm・c139sl と挙動の線 c139nc を採り、挙動（削除）の線 c139k49（線の止める線〈1 四半期に 100 倍〉の〈それ以外〉・3 回目）は外した＝基準を m1334 へ（B5・B10INS・MON・INSRAW・BASE_COMMIT・TESTS_NONFULL・NG_BASE・STAGE_BASE）。腕 Z（ident m1334z・段 2）〈判定: 恒等〉。KEEPNULL keep_1334（n1・n2・a11）。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
