# (1404) 合流：ident.cfg を新しい基線 m1404 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1383/cfg1383.py の写し）。
#   python -X utf8 -I cfg1404.py <ident.cfg>
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
rep('B5=measurements/b5_20261126_m1383', 'B5=measurements/b5_20261127_m1404')
rep('B10INS=measurements/b10_20261126_m1383_ins', 'B10INS=measurements/b10_20261127_m1404_ins')
rep('MON=measurements/mon_20261126_m1383.txt', 'MON=measurements/mon_20261127_m1404.txt')
rep('INSRAW=D:/PoB_work/keep_1397/base_ins', 'INSRAW=D:/PoB_work/keep_1404/base_ins',
    before='# (1404) INSRAW を keep_1404/base_ins へ＝採った木 m1404（650bb955a＝wx〔旗 WarExitFromCapacity 0〕・pm〔旗 PriceOnlyStepDays 30〕・rn〔旗 RandomOnlyClimateEpidemic 1〕）の M の計器つき 10 種の全表（1811 本＝wexit_s・rnd_s を足した）。')
rep('BASE_COMMIT=6056269fc', 'BASE_COMMIT=650bb955a',
    before='# (1404) 新しい基線の測った木（INSRAW keep_1404/base_ins を出した木 m1404・SimCore は main の木と同じ）。')
rep('TESTS_NONFULL=3295', 'TESTS_NONFULL=3317',
    before='# (1404)：3295＋c149wx 14（WarExitFromCapacityTests 13・InstrumentHookClosureTests 1）＋c149pm 2（PriceMonthStepTests）＋c149rn 6（RandomOnlyClimateEpidemicTests）＝3317（合わせた木の --list-tests で数えた・Tests＋Tests.Ui）。')
rep('NG_BASE=rd:measurements/ng1383_20261126:m1383', 'NG_BASE=rd:measurements/ng1404_20261127:m1404')
rep('STAGE_BASE=6056269fc', 'STAGE_BASE=650bb955a',
    before='# (1404) STAGE_BASE は新しい基線の測った木 650bb955a（pm・rn の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1404) 合流（2026-11-27）：恒等の線 c149wx と挙動の線 c149pm・c149rn を採った＝新しい基線 m1404（合わせた木の M と S11 で止める線 0・空洞化 1/10 未満 0・腕 Z〔ident m1404z・段 2〕〈判定: 恒等〉）。c149sd は腕 B が未実施で入れていない。KEEPNULL はヌル腕 N1・N2 を回した後の 2 回目の push で keep_1404/n1・n2・a11 へ。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
