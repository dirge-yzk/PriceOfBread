# (1411) 合流：ident.cfg を新しい基線 m1411 へ書き換える（バイトで読み書き・CR と BOM を保つ・m1404/cfg1404.py の写し）。
#   python -X utf8 -I cfg1411.py <ident.cfg>
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
rep('B5=measurements/b5_20261127_m1404', 'B5=measurements/b5_20261127_m1411')
rep('B10INS=measurements/b10_20261127_m1404_ins', 'B10INS=measurements/b10_20261127_m1411_ins')
rep('MON=measurements/mon_20261127_m1404.txt', 'MON=measurements/mon_20261127_m1411.txt')
rep('INSRAW=D:/PoB_work/keep_1404/base_ins', 'INSRAW=D:/PoB_work/keep_1411/base_ins',
    before='# (1411) INSRAW を keep_1411/base_ins へ＝採った木 m1411（36abd290b＝wz〔恒等・計器 sinkgood_s を足し wexit_s の中身を書き換えた〕・rp〔旗 RailProcureNational 1〕）の M の計器つき 10 種の全表。')
rep('BASE_COMMIT=650bb955a', 'BASE_COMMIT=36abd290b',
    before='# (1411) 新しい基線の測った木（INSRAW keep_1411/base_ins を出した木 m1411・SimCore は main の木と同じ）。')
rep('TESTS_NONFULL=3317', 'TESTS_NONFULL=3333',
    before='# (1411)：3317＋c150wz 11＋c150rp 5（RailProcureNationalTests）＝3333（合わせた木の --list-tests で数えた・Tests＋Tests.Ui）。')
rep('NG_BASE=rd:measurements/ng1404_20261127:m1404', 'NG_BASE=rd:measurements/ng1411_20261127:m1411')
rep('STAGE_BASE=650bb955a', 'STAGE_BASE=36abd290b',
    before='# (1411) STAGE_BASE は新しい基線の測った木 36abd290b（rp の旗 1）。')
if not b.endswith(nl): b += nl
b += ('# (1411) 合流（2026-11-27）：恒等の線 c150wz と挙動の線 c150rp を採った＝新しい基線 m1411（腕 Z〔ident m1411z・段 2〕〈判定: 恒等〉・合わせた木の M と S11 で止める線 0）。c150sd は関連の機構の勢力と蜂起がほぼ止まったので採らない（3 回の規則の 1 回目）。KEEPNULL はヌル腕 N1・N2 を回した後に keep_1411/n1・n2・a11 へ。').encode('utf-8') + nl
open(p, 'wb').write(b)
print('ok')
