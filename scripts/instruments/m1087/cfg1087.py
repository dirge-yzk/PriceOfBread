# (1087) 合流（m1069/cfg1069.py の型・基線を m1069→m1087 へ）。バイトで読み書き。
#   python -X utf8 cfg1087.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1069 のまま（★(1078)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1087（★(1087) 合流〔波103〕＝挙動の線 c103dm〔(1081) 威信と巨大建造物を物理で消した〕・c103cr〔(1082) 文化と宗教が空の 21 州のデータ＋番人 RegionLabelInvariantTests〕を採った新しい基線・'
              '恒等の線 c103tr〔(1079) c102tr の再合流・T4 を B′ の列へ・試験 B1〕・c103st〔(1080) stage.py が ident.cfg の基準の鍵だけの差を段 2 に数えない〕を入れた・'
              '腕 Z〔ident m1087z・木 72f9bf86＝decb＋tr＋st〕〈判定: 恒等〉・アブレーション A_dm≡dm の D′・A_cr≡cr の腕 B（共通の表）・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261113_m1087・B10INS b10_20261113_m1087_ins・MON mon_20261113_m1087.txt・INSRAW keep_1087/base_ins〔M の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2768・NG_BASE ng1087・.cost の基準は em の件数の行を含む＝以後の恒等の線に --cost-allow は要らない）。もとは') + b[k0 + k1:]
one('B5=measurements/b5_20261112_m1069\n', 'B5=measurements/b5_20261113_m1087\n')
one('B10INS=measurements/b10_20261112_m1069_ins\n', 'B10INS=measurements/b10_20261113_m1087_ins\n')
one('MON=measurements/mon_20261112_m1069.txt\n', 'MON=measurements/mon_20261113_m1087.txt\n')
one('INSRAW=D:/PoB_work/keep_1078/base_ins\n', '# (1087) から keep_1087/base_ins（合わせた木 tr＋st＋dm＋cr の M の計器つき 10 種の生・木 ' + H + '・全表）。ヌル腕 keep_1087/n1・n2・種 11〜20 は keep_1087/a11（POB_CEARN=0）。keep_1069・keep_1078 は参照しない（消してよい＝★(1087)）。\nINSRAW=D:/PoB_work/keep_1087/base_ins\n')
one('BASE_COMMIT=2ea6e338\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2769\n')
b = b[:k] + '# (1087)：2769＋c103tr 6（TradePhase0Tests 5・RecipeoutInstrumentTests B1 1）＋c103cr 3（RegionLabelInvariantTests）−c103dm 10（消した機能の試験）＝2768（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2769\n', 'TESTS_NONFULL=2768\n')
one('NG_BASE=rd:measurements/ng1069_20261112:m1069\n', 'NG_BASE=rd:measurements/ng1087_20261113:m1087\n')
one('STAGE_BASE=2ea6e338c57a617e749ee06003597e1a74f5b4a9\n', '# (1087) STAGE_BASE は新しい基線の測った木（stage.py の直しの後の木＝以後 ident.cfg の基準の鍵だけの差は段 2 に数えない）。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
