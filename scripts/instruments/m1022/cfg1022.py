# (1022) 合流（m1017/cfg1017.py の写し・基線を m1017→m1022 へ）
#   python -X utf8 cfg1022.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, SH, Z = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1017（★(1017) 合流〔波94〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1022（★(1022) 合流〔波95〕＝挙動の線 c95ub〔(1020) 遍在の財 石灰石・珪砂・粘土を非産地の 0.4 の外へ・旗 UbiquitousMineralExempt 既定 1〕を採った新しい基線・'
              '恒等の線 c95pc〔(1019) 計器 prodcap_s・recipeout_s の siteFound〕を入れた・c95wn〔(1018)〕は外した・腕 Z〔ident m1022z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261110_m1022・B10INS b10_20261110_m1022_ins・MON mon_20261110_m1022.txt・INSRAW keep_1022/base_ins〔M の計器つき 10 種の生・prodcap_s・recipeout_s の siteFound を含む〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2607・NG_BASE ng1022）'
              '。もとは基線 m1017（') + b[k0 + k1 + len('基線 m1017（'):]
one('B5=measurements/b5_20261110_m1017\n', 'B5=measurements/b5_20261110_m1022\n')
one('B10INS=measurements/b10_20261110_m1017_ins\n', 'B10INS=measurements/b10_20261110_m1022_ins\n')
one('MON=measurements/mon_20261110_m1017.txt\n', 'MON=measurements/mon_20261110_m1022.txt\n')
one('INSRAW=D:/PoB_work/keep_1017/base_ins\n', 'INSRAW=D:/PoB_work/keep_1022/base_ins\n')
one('BASE_COMMIT=ecb16335\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (1017) から keep_1017/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (1022) から keep_1022/base_ins（合わせた木 pc＋ub の M〔UbiquitousMineralExempt 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_1022/n1・n2・種 11〜20 keep_1022/a11（POB_CEARN=0）。'
             'keep_1017 は残す（古い keep_* を消すのは作者のセッション）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2595\n')
b = b[:k] + '# (1022)：2595＋c95pc 5（ProdcapInstrumentTests 4・RecipeoutInstrumentTests 1）＋c95ub 7（UbiquitousMineralTests）＝2607（合わせた木の --list-tests で確かめた・c95wn の 11 は外した）。\n' + b[k:]
one('TESTS_NONFULL=2595\n', 'TESTS_NONFULL=2607\n')
one('NG_BASE=rd:measurements/ng1017_20261110:m1017\n', 'NG_BASE=rd:measurements/ng1022_20261110:m1022\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
