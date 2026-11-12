# (1069) 合流（m1053/cfg1053.py の型・基線を m1053→m1069 へ）
#   python -X utf8 cfg1069.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH> <腕 Z の木>
import sys
p, H, HF, SH, Z = sys.argv[1:6]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1053（★(1053)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1069（★(1069) 合流〔波101〕＝挙動の線 c101vs〔(1064) 宗主と属国の輪を切る・旗 DependencyCycleBreak 既定 1・計器 dep_s〕を採った新しい基線・'
              '恒等の線 c101sel〔(1061) 出す表を選ぶ口 POB_INSTRUMENTS_TABLES・二段の判定 stage.py・SMALL・PRUNE の既定 1〕・c101uf〔(1062) 枝 uifix a7d854dd3〕を入れた・c101py〔(1063)〕は手続きで採らない・'
              '腕 Z〔ident m1069z・木 ' + Z + '〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261112_m1069・B10INS b10_20261112_m1069_ins・MON mon_20261112_m1069.txt・INSRAW keep_1069/base_ins〔M の計器つき 10 種の生・dep_s を含む〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2749・NG_BASE ng1069）。もとは') + b[k0 + k1:]
one('B5=measurements/b5_20261112_m1053\n', 'B5=measurements/b5_20261112_m1069\n')
one('B10INS=measurements/b10_20261112_m1053_ins\n', 'B10INS=measurements/b10_20261112_m1069_ins\n')
one('MON=measurements/mon_20261112_m1053.txt\n', 'MON=measurements/mon_20261112_m1069.txt\n')
one('INSRAW=D:/PoB_work/keep_1053/base_ins\n', '# (1069) から keep_1069/base_ins（合わせた木 sel＋uf＋vs の M〔DependencyCycleBreak 1〕の計器つき 10 種の生・木 ' + H + '・dep_s を含む）。ヌル腕 keep_1069/n1・n2・種 11〜20 は keep_1069/a11（POB_CEARN=0）。keep_1053 は作者のセッションが消すまで残る（参照はしない）。\nINSRAW=D:/PoB_work/keep_1069/base_ins\n')
one('KNOWN=burscan_s:seconds,clock_s:worldTotalPerMonth@kind=C\n', '# (1069) dep_s:callerLine は Diplomacy.cs の呼び手の行の番号（計器の限界④・反証役 v）＝Diplomacy.cs の行を動かす恒等の線で変わる＝既知の揺れに足した（AI の判断・callerMember は比べ続ける）。\nKNOWN=burscan_s:seconds,clock_s:worldTotalPerMonth@kind=C,dep_s:callerLine\n')
one('BASE_COMMIT=17d1c3d9\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2689\n')
b = b[:k] + '# (1069)：2689＋c101sel 2（InstrumentsTablesSelectTests）＋c101uf 50（枝 uifix の画面の試験）＋c101vs 8（DependencyCycleTests）＝2749（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2689\n', 'TESTS_NONFULL=2749\n')
one('NG_BASE=rd:measurements/ng1053_20261112:m1053\n', 'NG_BASE=rd:measurements/ng1069_20261112:m1069\n')
one('STAGE_BASE=9b18137a2d0cb8568a9f557cd9d59981a7283949\n', '# (1069) STAGE_BASE は新しい基線の測った木（段 1／段 2 の差はここから数える）。\nSTAGE_BASE=' + HF + '\n')
one('PRESPEC=measurements/m1053_20261112/hash/pre_base.tsv\n', '# (1069) 新しい基線 keep_1069 に対して列が増えた表は無い＝PRESPEC は空（記録の列だけに効く）。\nPRESPEC=\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
