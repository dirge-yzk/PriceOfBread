# (1103) 合流（m1087/cfg1087.py の型・基線を m1087→m1103 へ）。バイトで読み書き。
#   python -X utf8 cfg1103.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1087 のまま（★(1096)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1103（★(1103) 合流〔波105〕＝挙動の線 c105rr〔(1097) 支配の力の段 R・旗 RailByRegion 既定 1〕・c105gp〔(1099) s18gp・旗 NonElectiveSeatsEmpty 既定 1〕を採った新しい基線・'
              '恒等の線 c105ix〔(1100) armsfill_s ほか〕と c105mc〔(1098)〕の旗 0 の器を入れた・'
              '腕 Z〔ident m1103z・合わせた木＋旗の既定を 0 へ戻す使い捨ての木 f6139a3f〕〈判定: 恒等〉・アブレーション A_rr≡rr の腕 B・A_gp≡gp の腕 B（成績表と共通の表）・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261113_m1103・B10INS b10_20261113_m1103_ins・MON mon_20261113_m1103.txt・INSRAW keep_1103/base_ins〔M の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2808・NG_BASE ng1103・.cost の基準は rr の〈清算：距離〉の 2 行を含む＝以後の恒等の線に --cost-allow は要らない）。もとは') + b[k0 + k1:]
one('B5=measurements/b5_20261113_m1087\n', 'B5=measurements/b5_20261113_m1103\n')
one('B10INS=measurements/b10_20261113_m1087_ins\n', 'B10INS=measurements/b10_20261113_m1103_ins\n')
one('MON=measurements/mon_20261113_m1087.txt\n', 'MON=measurements/mon_20261113_m1103.txt\n')
one('INSRAW=D:/PoB_work/keep_1096/base_ins\n', '# (1103) から keep_1103/base_ins（合わせた木 ix＋mc の器＋gp＋rr の M の計器つき 10 種の生・木 ' + H + '・全表）。ヌル腕 keep_1103/n1・n2（全表・POB_CEARN=0）・種 11〜20 は keep_1103/a11（POB_CEARN=0・(イ)＝SMALL と trrail・trnation・trroute・trbin・trstat の表だけ）。keep_1087・keep_1096 は参照しない（消してよい＝★(1103)）。\nINSRAW=D:/PoB_work/keep_1103/base_ins\n')
one('BASE_COMMIT=1d06d8c9\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2790\n')
b = b[:k] + '# (1103)：2790＋c105ix 5（InstrumentsArmsTests 2・DisplayTradeReadersTests 2・RecipeoutInstrumentTests T6 1）＋c105mc 1（EconomyTests）＋c105gp 4（NonElectiveSeatsTests）＋c105rr 8（RailByRegionTests 7・KmComponentTests 1）＝2808（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2790\n', 'TESTS_NONFULL=2808\n')
one('NG_BASE=rd:measurements/ng1087_20261113:m1087\n', 'NG_BASE=rd:measurements/ng1103_20261113:m1103\n')
one('STAGE_BASE=1d06d8c9b02cb1242c3e5cafe83d3e65e3abb2bc\n', '# (1103) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
