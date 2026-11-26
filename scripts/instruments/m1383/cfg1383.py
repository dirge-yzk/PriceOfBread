# (1383) 合流：ident.cfg を新しい基線 m1383 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1362/cfg1362.py の写し）。
#   python -X utf8 -I cfg1383.py <ident.cfg>
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
rep('B5=measurements/b5_20261125_m1362', 'B5=measurements/b5_20261126_m1383')
rep('B10INS=measurements/b10_20261125_m1362_ins', 'B10INS=measurements/b10_20261126_m1383_ins')
rep('MON=measurements/mon_20261125_m1362.txt', 'MON=measurements/mon_20261126_m1383.txt')
rep('INSRAW=D:/PoB_work/keep_1376/base_ins', 'INSRAW=D:/PoB_work/keep_1383/base_ins',
    before='# (1383) INSRAW を keep_1383/base_ins へ＝採った木 m1383（7e8dd6f97＝kt・rf・wy〔旗 WarWearyPoliticsCut 1〕・bk〔旗 ReentryShortageSameMonth 1〕）の M の計器つき 10 種の全表（1793 本）。')
rep('BASE_COMMIT=9a32a320c', 'BASE_COMMIT=7e8dd6f97',
    before='# (1383) 新しい基線の測った木（INSRAW keep_1383/base_ins を出した木 m1383・SimCore は採った木と同じ）。')
rep('TESTS_NONFULL=3256', 'TESTS_NONFULL=3281',
    before='# (1383)：3256＋c146kt 1（LawstepInstrumentTests）＋c146rf 12（RefactorBundle1Tests）＋c146wy 6（WarWearyPoliticsCutTests 2・CoercionNowTests 3・ScalarDeletionTests 1）＋c146bk 6（BankruptReentryShortageTests）＝3281＝Tests 3049＋Tests.Ui 232（採った木の --list-tests と一致）。')
rep('NG_BASE=rd:measurements/ng1362_20261125:m1362', 'NG_BASE=rd:measurements/ng1383_20261126:m1383')
rep('STAGE_BASE=9a32a320c', 'STAGE_BASE=7e8dd6f97',
    before='# (1383) STAGE_BASE は新しい基線の測った木 7e8dd6f97（wy・bk の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1383) 合流（2026-11-26）：恒等の線 c146kt・c146rf と挙動の線 c146wy・c146bk を採った＝新しい基線 m1383（合わせた木の M と S11 で止める線 0・空洞化 1/10 未満 0・腕 Z〔ident m1383z・段 2〕〈判定: 恒等〉）。KEEPNULL はヌル腕 N1・N2 を回す 2 回目の push まで keep_1362/n1・n2・a11 のまま（種 11〜20 の生は keep_1383/a11＝cpop_s を除く 87 表〔D: の空きのため〕）。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
