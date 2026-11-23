# (1328) 合流：ident.cfg を新しい基線 m1328 へ書き換える（バイトで読み書き・CR と BOM を保つ）。
#   python -X utf8 -I cfg1328.py <ident.cfg>
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
rep('B5=measurements/b5_20261123_m1321', 'B5=measurements/b5_20261124_m1328')
rep('B10INS=measurements/b10_20261123_m1321_ins', 'B10INS=measurements/b10_20261124_m1328_ins')
rep('MON=measurements/mon_20261123_m1321.txt', 'MON=measurements/mon_20261124_m1328.txt')
rep('INSRAW=D:/PoB_work/keep_1321/base_ins', 'INSRAW=D:/PoB_work/keep_1328/base_ins',
    before='# (1328) INSRAW を keep_1328/base_ins へ＝採った木 m1328（6c170add5＝sm・lp〔旗 LawPrefSinglePeaked 1〕・df は旗 0 の器のまま）の M の計器つき 10 種の全表（lawdir_s を含む・capskip_s は末尾 +1 列・region_s は末尾 +2 列・arrears_s は末尾 +1 列・keepsum は measurements/m1328_20261123/keepsum_insraw1328.tsv）。')
rep('BASE_COMMIT=6956d6363', 'BASE_COMMIT=6c170add5',
    before='# (1328) 新しい基線の測った木（INSRAW keep_1328/base_ins を出した木 m1328）。')
rep('TESTS_NONFULL=3149', 'TESTS_NONFULL=3169',
    before='# (1328)：3149＋c138sm 6（InstrumentColumnTests・InstrumentHookClosureTests ほか）＋c138lp 14（LawPreferenceDirectionTests）＝3169＝Tests 2937＋Tests.Ui 232（腕 Z〔ident m1328z〕と採った木の --list-tests で確かめた）。')
rep('NG_BASE=rd:measurements/ng1321_20261123:m1321', 'NG_BASE=rd:measurements/ng1328_20261124:m1328')
rep('STAGE_BASE=6956d6363', 'STAGE_BASE=6c170add5',
    before='# (1328) STAGE_BASE は新しい基線の測った木 6c170add5（sm・lp の旗 1・df の旗 0）。')
if not b.endswith(nl): b += nl
b += ('# (1328) 合流（2026-11-24）：恒等の線 c138sm・挙動の線 c138lp を採り、挙動の線 c138df（S11 の空洞化 1/10 未満〈線が原因〉・3 回目）と c138ss（線の止める線 S2）は外した＝基準を m1328 へ（B5・B10INS・MON・INSRAW・BASE_COMMIT・TESTS_NONFULL・NG_BASE・STAGE_BASE）。腕 Z（ident m1328z・段 2）〈判定: 恒等〉。KEEPNULL keep_1328（n1・n2・a11）。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
