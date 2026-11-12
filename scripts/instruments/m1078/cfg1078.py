# (1078) 合流（m1069/cfg1069.py の型・基線は m1069 のまま・INSRAW を keep_1078/base_ins へ・テストの数・BASE_COMMIT・STAGE_BASE だけ）
#   python -X utf8 cfg1078.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1069（★(1069)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1069 のまま（★(1078) 合流〔波102〕＝挙動の線 0 本・恒等の線 c102ui〔(1070) 枝 uifix 601cd9c2e まで〕・c102rp〔(1071) PairKm の純関数の口・影の計器 rulepower_s・rulepeace_s〕・c102em〔(1072) 中身の無い物の計器 ①〜③・消費の段で人口 0 の POP を飛ばす〕を入れた・c102tr〔(1073)〕は手続きで採らない・'
              '腕 Z〔ident m1078z・木 ' + H + '〕〈判定: 恒等〉・SOURCE-HASH ' + SH + '・成績表は m1069 のまま・INSRAW keep_1078/base_ins〔腕 Z の計器つき 10 種の生・rulepower_s・rulepeace_s を含む・keep_1069 と同じ表はハードリンク〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2769）。もとは') + b[k0 + k1:]
one('INSRAW=D:/PoB_work/keep_1069/base_ins\n', '# (1078) から keep_1078/base_ins（合わせた木 ui＋rp＋em の腕 Z〔ident m1078z〕の計器つき 10 種の生・木 ' + H + '・rulepower_s・rulepeace_s を含む・cearn_s の経路の行番号は合わせた木の物・keep_1069/base_ins とバイトで同じ表はハードリンク＝hlink1078.py）。ヌル腕 keep_1069/n1・n2・種 11〜20 は keep_1069/a11 のまま。\nINSRAW=D:/PoB_work/keep_1078/base_ins\n')
one('BASE_COMMIT=b0edc896\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2749\n')
b = b[:k] + '# (1078)：2749＋c102ui 1（ObservationFixTests）＋c102rp 4（RulePowerTests）＋c102em 15（ZeroPopSkipTests 5・WriterRatchetTests の InlineData 10）＝2769（合わせた木の --list-tests と ident m1078z で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2749\n', 'TESTS_NONFULL=2769\n')
one('STAGE_BASE=b0edc89635a96324f66bbe13fcfbfdf5721bb4bf\n', '# (1078) STAGE_BASE は合わせた木（段 1／段 2 の差を波102 の計器の後から数える）。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
