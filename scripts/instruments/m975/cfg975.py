# (975) 合流（cfg968.py の写し・基線を m968→m975 へ）
#   python -X utf8 cfg975.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH>
import sys
p, H, SH = sys.argv[1:4]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m968（★(968) 合流〔波86〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m975（★(975) 合流〔波87〕＝挙動の線 c87mw〔(973) ⑮ MilitaryWarMult を接触の割合から・旗 MilitaryWarMultFromContact 既定 1・⑰ CivilWarArmsDemandUnified の旧の枝の焼き込み〕と c87mt〔(972) ⓪‴′ (D)・旗 MoTrendInitNeedsGdp 既定 1〕を採った新しい基線・'
              '恒等の線 c87bx〔(970) 計器 basisx_s〕・試験だけの c87ot〔(969)〕を入れた・腕 Z〔ident m975z・木 f95cfe13〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261109_m975・B10INS b10_20261109_m975_ins・MON mon_20261109_m975.txt・INSRAW keep_975/base_ins〔M の計器つき 10 種の生〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2472・NG_BASE ng975）'
              '。もとは基線 m968（') + b[k0 + k1 + len('基線 m968（'):]
one('B5=measurements/b5_20261108_m968\n', 'B5=measurements/b5_20261109_m975\n')
one('B10INS=measurements/b10_20261108_m968_ins\n', 'B10INS=measurements/b10_20261109_m975_ins\n')
one('MON=measurements/mon_20261108_m968.txt\n', 'MON=measurements/mon_20261109_m975.txt\n')
one('INSRAW=D:/PoB_work/keep_968/base_ins\n', 'INSRAW=D:/PoB_work/keep_975/base_ins\n')
one('BASE_COMMIT=ca4d2184\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (968) から keep_968/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (975) から keep_975/base_ins（合わせた木 ot＋bx＋mt＋mw の M〔MoTrendInitNeedsGdp 1・MilitaryWarMultFromContact 1〕の計器つき 10 種の生・木 ' + H + '）。ヌル腕 keep_975/n1・n2・種 11〜20 keep_975/a11（POB_CEARN=0）。'
             'keep_968 は残す（古い keep_* を消すのは作者のセッション）。採らなかった c87hd の腕の生は keep_975_rej（hd_S11・hd_A）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2454\n')
b = b[:k] + '# (975)：2454＋c87bx 11（BasisxInstrumentTests）＋c87mt 5（MoTrendInitTests）＋c87mw 2（MilitaryWarMultContactTests +6・CivilWarArmsDemandTests −4）＋c87ot 0＝2472。\n' + b[k:]
one('TESTS_NONFULL=2454\n', 'TESTS_NONFULL=2472\n')
one('NG_BASE=rd:measurements/ng968_20261108:m968\n', 'NG_BASE=rd:measurements/ng975_20261109:m975\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
