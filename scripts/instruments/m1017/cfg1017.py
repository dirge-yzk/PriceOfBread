# (1017) 合流（m1001/cfg1001.py の写し・基線を m1001→m1017 へ）
#   python -X utf8 cfg1017.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, SH, Z = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1001 のまま（★(1011) 合流〔波93〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1017（★(1017) 合流〔波94〕＝挙動の線 c94pe〔(1014) 空の行き先の商人を開く・旗 ColonyPettyEntryEmptyTarget 既定 1〕を採った新しい基線・'
              '恒等の線 c94cl〔(1012) 計器 corncf_s・lordstat_s〕・c94ro〔(1013) 計器 recipeout_s〕を入れた・腕 Z〔ident m1017z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261110_m1017・B10INS b10_20261110_m1017_ins・MON mon_20261110_m1017.txt・INSRAW keep_1017/base_ins〔M の計器つき 10 種の生・corncf_s・lordstat_s・recipeout_s を含む〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2595・NG_BASE ng1017）'
              '。もとは基線 m1001 のまま（') + b[k0 + k1 + len('基線 m1001 のまま（'):]
one('B5=measurements/b5_20261109_m1001\n', 'B5=measurements/b5_20261110_m1017\n')
one('B10INS=measurements/b10_20261109_m1001_ins\n', 'B10INS=measurements/b10_20261110_m1017_ins\n')
one('MON=measurements/mon_20261109_m1001.txt\n', 'MON=measurements/mon_20261110_m1017.txt\n')
one('INSRAW=D:/PoB_work/keep_1011/base_ins\n', 'INSRAW=D:/PoB_work/keep_1017/base_ins\n')
one('BASE_COMMIT=a93d52bf\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (1001) から keep_1001/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (1017) から keep_1017/base_ins（合わせた木 cl＋ro＋pe の M〔ColonyPettyEntryEmptyTarget 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_1017/n1・n2・種 11〜20 keep_1017/a11（POB_CEARN=0）。'
             'keep_1001・keep_1006・keep_1011 は残す（古い keep_* を消すのは作者のセッション）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2546\n')
b = b[:k] + '# (1017)：2546＋c94cl 39（CorncfInstrumentTests 27・LordstatInstrumentTests 12）＋c94ro 5（RecipeoutInstrumentTests）＋c94pe 5（ColonyPettyEntryTests）＝2595（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2546\n', 'TESTS_NONFULL=2595\n')
one('NG_BASE=rd:measurements/ng1001_20261109:m1001\n', 'NG_BASE=rd:measurements/ng1017_20261110:m1017\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
