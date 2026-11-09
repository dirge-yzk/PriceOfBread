# (988) 合流（cfg981.py の写し・基線を m981→m988 へ）
#   python -X utf8 cfg988.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, SH, Z = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m981（★(981) 合流〔波88〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m988（★(988) 合流〔波89〕＝挙動の線 c89dd〔(984) ⑬ 塵の債務・Pop.Take の額の床を〈正なら動かす〉・旗 TakeFloorPositive 既定 1〕を採った新しい基線・'
              '恒等の線 c89ls〔(983) 計器 landsale_s〕・試験と注と台本だけの c89tb〔(982)〕を入れた・腕 Z〔ident m988z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261109_m988・B10INS b10_20261109_m988_ins・MON mon_20261109_m988.txt・INSRAW keep_988/base_ins〔M の計器つき 10 種の生〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2504・NG_BASE ng988）'
              '。もとは基線 m981（') + b[k0 + k1 + len('基線 m981（'):]
one('B5=measurements/b5_20261109_m981\n', 'B5=measurements/b5_20261109_m988\n')
one('B10INS=measurements/b10_20261109_m981_ins\n', 'B10INS=measurements/b10_20261109_m988_ins\n')
one('MON=measurements/mon_20261109_m981.txt\n', 'MON=measurements/mon_20261109_m988.txt\n')
one('INSRAW=D:/PoB_work/keep_981/base_ins\n', 'INSRAW=D:/PoB_work/keep_988/base_ins\n')
one('BASE_COMMIT=9c296d94\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (981) から keep_981/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (988) から keep_988/base_ins（合わせた木 tb＋ls＋dd の M〔TakeFloorPositive 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_988/n1・n2・種 11〜20 keep_988/a11（POB_CEARN=0）。'
             'keep_981 は残す（古い keep_* を消すのは作者のセッション）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2484\n')
b = b[:k] + '# (988)：2484＋c89ls 7（LandsaleInstrumentTests）＋c89dd 13（TakeDustFloorTests ほか）＋c89tb 0＝2504（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2484\n', 'TESTS_NONFULL=2504\n')
one('NG_BASE=rd:measurements/ng981_20261109:m981\n', 'NG_BASE=rd:measurements/ng988_20261109:m988\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
