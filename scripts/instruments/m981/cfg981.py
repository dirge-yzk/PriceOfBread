# (981) 合流（cfg975.py の写し・基線を m975→m981 へ）
#   python -X utf8 cfg981.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, SH, Z = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m975（★(975) 合流〔波87〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m981（★(981) 合流〔波88〕＝挙動の線 c88cs〔(978) ⑦ 土地の持ち分 B-2b・露・普の国家の枠・旗 LandShareCrownSlot 既定 1〕を採った新しい基線・'
              '恒等の線 c88sm〔(977) 小物の束 2・SimCore は注だけ〕・試験だけの c88ts〔(976)〕を入れた・腕 Z〔ident m981z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261109_m981・B10INS b10_20261109_m981_ins・MON mon_20261109_m981.txt・INSRAW keep_981/base_ins〔M の計器つき 10 種の生〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2484・NG_BASE ng981）'
              '。もとは基線 m975（') + b[k0 + k1 + len('基線 m975（'):]
one('B5=measurements/b5_20261109_m975\n', 'B5=measurements/b5_20261109_m981\n')
one('B10INS=measurements/b10_20261109_m975_ins\n', 'B10INS=measurements/b10_20261109_m981_ins\n')
one('MON=measurements/mon_20261109_m975.txt\n', 'MON=measurements/mon_20261109_m981.txt\n')
one('INSRAW=D:/PoB_work/keep_975/base_ins\n', 'INSRAW=D:/PoB_work/keep_981/base_ins\n')
one('BASE_COMMIT=37afb26d\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (975) から keep_975/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (981) から keep_981/base_ins（合わせた木 ts＋sm＋cs の M〔LandShareCrownSlot 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_981/n1・n2・種 11〜20 keep_981/a11（POB_CEARN=0）。'
             'keep_968・keep_975・keep_975_rej は残す（古い keep_* を消すのは作者のセッション）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2472\n')
b = b[:k] + '# (981)：2472＋c88sm 2（MoTrendInitTests）＋c88cs 10（LandShareCrownTests）＋c88ts 0＝2484（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2472\n', 'TESTS_NONFULL=2484\n')
one('NG_BASE=rd:measurements/ng975_20261109:m975\n', 'NG_BASE=rd:measurements/ng981_20261109:m981\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
