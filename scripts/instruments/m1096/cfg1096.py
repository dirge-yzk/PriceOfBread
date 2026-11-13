# (1096) 合流（m1078/cfg1078.py の型・基線は m1087 のまま・INSRAW を keep_1096/base_ins へ・テストの数・BASE_COMMIT・STAGE_BASE だけ）
#   python -X utf8 cfg1096.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1087（★(1087)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1087 のまま（★(1096) 合流〔波104〕＝挙動の線 0 本・恒等の線 c104ui〔(1088) 枝 uifix 18d6253e8 まで〕・c104fb〔(1089) 旗の焼き込み 群 1＋FactoryBuildRefIsReal＋MC0〕・c104cp〔(1090) 影の計器 center_s・poppref_s〕・c104si〔(1091) rulepoweralloc_s・minecap_s・rulepower_s の末尾 10 列〕を入れた・'
              '腕 Z〔ident m1096z・木 ' + H + '〕〈判定: 恒等〉・SOURCE-HASH ' + SH + '・成績表は m1087 のまま・INSRAW keep_1096/base_ins〔腕 Z の計器つき 10 種の生・keep_1087 と同じ表はハードリンク〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2790）。もとは') + b[k0 + k1:]
one('INSRAW=D:/PoB_work/keep_1087/base_ins\n', '# (1096) から keep_1096/base_ins（合わせた木 ui＋fb＋cp＋si の腕 Z〔ident m1096z〕の計器つき 10 種の生・木 ' + H + '・center_s・poppref_s・rulepoweralloc_s・minecap_s を含む・rulepower_s は末尾 10 列・cearn_s の経路の行番号は合わせた木の物・keep_1087/base_ins とバイトで同じ表はハードリンク＝hlink1096.py）。ヌル腕 keep_1087/n1・n2・種 11〜20 は keep_1087/a11 のまま（KEEPNULL）。\nINSRAW=D:/PoB_work/keep_1096/base_ins\n')
one('BASE_COMMIT=64e20d39\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2768\n')
b = b[:k] + '# (1096)：2768＋c104ui 0＋c104fb 4（EconomyTests の機構の試験）＋c104cp 13（PowerCenterTests 8・PopPreferenceTests 5）＋c104si 5（RulePowerAllocTests 3・MineCapInstrumentTests 2）＝2790（合わせた木の --list-tests と ident m1096z で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2768\n', 'TESTS_NONFULL=2790\n')
one('STAGE_BASE=64e20d39680b722895666766a85baaa3d041eb00\n', '# (1096) STAGE_BASE は合わせた木（段 1／段 2 の差を波104 の計器の後から数える）。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
