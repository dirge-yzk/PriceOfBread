# (1425) 合流：ident.cfg を新しい基線 m1425 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1418/cfg1418.py の写し）。
#   python -X utf8 -I cfg1425.py <ident.cfg> <段 1|2> <木の sha>
#   段 1＝基線の成績表・INSRAW・NG・試験の数（N1・N2 の前）。段 2＝新しい N1・N2 の後の KEEPNULL の行を足すだけ。
import sys
p = sys.argv[1]; stage = sys.argv[2]; tree = sys.argv[3]
b = open(p, 'rb').read()
nl = b'\r\n' if b'\r\n' in b else b'\n'
def rep(old, new, before=None):
    global b
    o = old.encode('utf-8'); n = new.encode('utf-8')
    assert b.count(o) == 1, old
    if before is not None:
        n = before.encode('utf-8') + nl + n
    b = b.replace(o, n)
if not b.endswith(nl): b += nl
if stage == '1':
    rep('B5=measurements/b5_20261127_m1418', 'B5=measurements/b5_20261128_m1425')
    rep('B10INS=measurements/b10_20261127_m1418_ins', 'B10INS=measurements/b10_20261128_m1425_ins')
    rep('MON=measurements/mon_20261127_m1418.txt', 'MON=measurements/mon_20261128_m1425.txt')
    rep('INSRAW=D:/PoB_work/keep_1418/base_ins', 'INSRAW=D:/PoB_work/keep_1425/base_ins',
        before=f'# (1425) INSRAW を keep_1425/base_ins へ＝採った木 m1425（{tree}＝ht〔恒等〕・wr〔恒等・計器〕・ms〔MilitarismStrengthBonus 1.0〕）の M の計器つき 10 種の全表 1825 本。')
    rep('BASE_COMMIT=f3a9c151c', f'BASE_COMMIT={tree}',
        before='# (1425) 新しい基線の測った木（INSRAW keep_1425/base_ins を出した木 m1425・SimCore は main の木と同じ）。')
    rep('TESTS_NONFULL=3327', 'TESTS_NONFULL=3343',
        before='# (1425)：3327＋c152ht 0（新しい 1・FullLength へ 1）＋c152wr 13＋c152ms 3（MilitarismBonusRemovalTests）＝3343（合わせた木の --list-tests で数えた）。TESTS_FULL 4 は c152ht。')
    rep('NG_BASE=rd:measurements/ng1418_20261127:m1418', 'NG_BASE=rd:measurements/ng1425_20261128:m1425')
    rep(f'STAGE_BASE=f3a9c151c', f'STAGE_BASE={tree}',
        before=f'# (1425) STAGE_BASE は新しい基線の測った木 {tree}（ms の係数 1.0）。')
    b += ('# (1425) 合流（2026-11-28）：恒等の線 c152ht・c152wr と挙動の線 c152ms を採った＝新しい基線 m1425（腕 Z〔ident m1425z・段 2〕〈判定: 恒等〉・合わせた木の M と S11 で止める線 0・ms の G1 は S11 で 0）。挙動の線 c152sd は反証役 v2 の高（1 人に満たない勢力）が晴れず外した（生は keep_1425_rej/c152sdB）。' + nl.decode()
          + '# (1425) KEEPNULL は段 1 の時点で keep_1425/a11（S11）・旧の keep_1418/n1・旧の keep_1411/n2（E:/PoB_archive/keep_1411/n2 へ移した）。新しい N1・N2 は push の後に回す。' + nl.decode()).encode('utf-8')
else:
    b += ('# (1425) ヌル腕 N1・N2（木 ' + tree + '・SupplySlope 0.50000001／0.49999999・a11 と同じ 89 表の名簿）を push の後に回した＝KEEPNULL は keep_1425/n1・n2・a11（成績表 b10_20261128_m1425_n1_ins・_n2_ins）。' + nl.decode()).encode('utf-8')
open(p, 'wb').write(b)
print('ok')
