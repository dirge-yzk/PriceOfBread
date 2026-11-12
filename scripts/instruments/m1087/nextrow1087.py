# (1087) 合流：00一覧〈次の波（波104〜）〉の行を、前の行（波103〜）の節を運んで作る（この波で閉じた節だけ落とし、変わった節は直し、新しい節を足す）。
#   python -X utf8 nextrow1087.py <00一覧> <出力 md>
import sys
src, out = sys.argv[1], sys.argv[2]
s = open(src, 'rb').read().decode('utf-8')
L = s.split('\n'); i = [k for k, l in enumerate(L) if l.startswith('| **次の波（波103〜・(1078) の推奨')]; assert len(i) == 1
cells = L[i[0]].split(' | '); assert len(cells) == 3
seg = cells[1].split('<br>'); assert len(seg) == 19
def st(n, pref): assert seg[n].startswith(pref), (n, seg[n][:40]); return seg[n]
def rep(x, a, b): assert x.count(a) == 1, (a, x[:60]); return x.replace(a, b)
head = ('| **次の波（波104〜・(1087) の推奨・AI・作者の承認は未確認）** （2026-11-13 ★(1087)：波103 は挙動 2/2〔c103dm・c103cr〕を採った＝新しい基線 m1087・恒等 2/2〔c103tr・c103st〕を入れた） | '
        '基準は **m1087**（N1・N2・S11＝keep_1087・NG `rd:measurements/ng1087_20261113:m1087`・止める線の基準 `measurements/m1087_20261113/stopbase/`・INSRAW `D:/PoB_work/keep_1087/base_ins`・TESTS_NONFULL 2768）・推定 227.8 秒・名目の余り 約 5.6 秒（保守側）。'
        '**STAGE_BASE は測った木 64e20d396（stage.py の直しの後）＝ident.cfg の基準の鍵だけの差は段 2 に数えない＝恒等の線は計器に触れなければ段 1**。.cost の基準は em の件数の行を含む＝--cost-allow は要らない。各線は種類・持ち主・材料・門の素案・答え待ちの順。'
        'この波で閉じた節（tr の再合流・⑤ 4 州・⑤′ 威信と巨大建造物・stage.py の直し・:282）を落とし、他の節は前の行のまま運んだ。')
new = []
new.append(rep(st(2, '**④ 旗の焼き込み'), 'tr と同じ Simulation.cs＝tr の再合流の後', 'tr が入った＝塞がりが解けた〔★(1087)〕'))
new.append('**恒等 束 A 7 本の削除**（(1084)・SubsistenceNoBoughtTech ほか・持ち主＝EconomyStep.cs・Military.cs・Simulation.cs・Coefficients.cs・材料＝`measurements/m1084_20261113/prereg_draft_bundleA.txt`・門＝ident〈判定: 恒等〉・突然変異 M1〜M5 の赤の表を腕の前に・CapOf と拘束の列〔Instruments.cs:204・:425〜:440〕を残すか宣言するか〔反証役 v〕）。')
new.append('**恒等 計器 MCi → 挙動 鉱山と畑の上限 MC**（紙の段の順・持ち主＝EconomyStep.cs〔dm の後〕・〈耕地を投資の結果として広げる〉を同じ線の材料に〔(1081)・台帳 S05-H004〕）。')
x = st(4, '**置き換えの線**')
x = rep(x, '〔tr の再合流の後〕', '〔(1087) で main へ入った〕')
new.append('**恒等 B′ の列の旗 1 の試験**（(1079) 反証役 v の中・持ち主＝RecipeoutInstrumentTests.cs・〈旗 1 の盤面の x1.IfBprime≈x1.Mc〉・dividedNow を 0 に固定する突然変異で赤）→ ' + x)
x = st(5, '**支配の力の段 C′ の前**')
k = x.index('列強が小国の属国になる経路')
new.append('**支配の力の段 C′ の前**（(1071)・(1083)）：恒等 計器 S1〜S4（配りの表・凍結を外した ρ と τ〔inf でない季節の凍結の州も〕・RuleInit の丸め戻せる書式・i の試し・`measurements/m1083_20261113/report.txt`）→ 段 C′ の事前登録（門＝同点〔同じ盤面で旧新の許容差を数える〕・凍結〔PairKm′ の ρ との比〕・開幕〔今は片側＝旗 0 で落ちない〕・ρ の回数〔読みごとに 2 条件・〈0 回〉の突然変異〕・q）・**答え待ち** §0-2 ★(1087) ①②。読む〈開幕の数年で欧州の陸軍力が半分以下に落ちる理由〉（仏 2.17→0.21）。' + x[k:])
x = st(6, '**中身の無い物の続き**')
x = rep(x, ':282 の母集合を名指しの集合へ（試験だけ・★(743) の区分を事前登録で）・', '')
new.append(x)
new.append(st(7, '**画面**'))
new.append(st(8, '**3 つのブロック**').rstrip('。') + '。(1085) の材料を足した（選挙政体の議席の第一党 99.2〜99.6% が自由・選挙で与党の族が替わるのは 0〜0.77%・札を読む行 13 か所・`measurements/m1085_20261113/ledger_S18_2026-11-12.md` §4）。')
new.append('**S18 の続き**（(1085)）：挙動 s18gp（非選挙の議席を空にし開院の分岐で議席と与党を決める・持ち主＝Parliament.cs:120 と旗・門 G1〜G4〔G2 は開幕と内戦の四半期を除く・G4 は内戦を除く形へ直す＝反証役 v〕・突然変異 M5〈写さないだけ〉・**答え待ち** §0-2 ★(1087) ③）・恒等 s18dp（動的政党を消す・App の与党名の口は残す）・計器 sbase_s（⓪ の答えの後）。')
new.append(st(9, '**軍備と動員**'))
new.append(st(10, '**英の艦隊**'))
new.append(st(11, '**試験の秒**'))
new.append('**宗教と移民**（(1086)・設計の材料・第 2 期＝貿易の後）：恒等 段 0 の計器（(1042) の案と 1 本）・読む 段 0′（国教の法の梯子が動かない根〔SelfPlay.cs:848 を含む〕・米の開幕の値）→ R1 → R6 → 段 1（**答え待ち** §0-2 ★(1087) ④）→ 段 3（材料＝`docs/202611/20261113/20261113_宗教と移民の段0_材料.md`）。')
new.append('**文化の 2 段 CU**（(1082) の続き・持ち主＝world.json・空のまま許した 12 州に小文化の名を置き許しを消す・〈全ての州の名が既にある名の集合に入る〉番人〔反証役 v〕）。**台帳の道具**（(1084) S1〜S3・恒等・道具と試験だけ）・**OpeningBake の旗 0 の間は焼き直さない**（恒等の小さな線）。')
new.append(st(12, '**(1069) の推奨の残り**'))
new.append(st(13, '**前の行から続く物**'))
new.append(rep(st(14, '**貿易の主の線**'), '（tr の再合流と置き換えの線）', '（置き換えの線）'))
new.append(st(15, '⑯**設計の段**'))
new.append(st(16, '⑲**App・Display・Runner'))
new.append('⑲⑳ の足し（★(1087)）：⑲＋**DM-P＝巨大建造物の口を消す**（型 Megaproject・Diplomacy.Megaprojects・CanStart／TryStart・TreasuryCat.Megaproject・Runner の MegaStateText・App の欄＝読み手 SimCore 5 ファイル 7 行・App 2 ファイル 13 行・Runner 1 ファイル 9 行）・束 B（法の軸 Guild と PopDebtDriven・特権の表の添字と World.Guild を含む）・画面の宗教統一の表示（(1082) v2）・選挙の通知の文〈選挙で〉（News.cs:233・(1085)）。⑳＋消した名を指す古い注の一巡（Model.cs の (608)・InstrumentsScalars.cs の見出し・Diplomacy.cs:218・:305・EconomyStep.cs:3024・docs/README.md:346）・stage.py と ident.sh:71 の注〈STAGE_BASE を進めると段 1 になりうる＝ident.cfg は合流だけが触る〉（(1080) 反証役 v）。')
new.append(rep(st(17, '**元の残りタスク**'), '巨大建造物ほか', '巨大建造物ほか（本体は (1081) で消した・口は DM-P）'))
new.append('**この次の波の推奨の組**（AI の推奨）：恒等 ④ 群 1 の焼き込み・恒等 束 A・恒等 計器 MCi・恒等 B′ の列の旗 1 の試験・恒等 支配の力の計器 S1〜S4・恒等 navopen_s／navtgt_s・読む 欧州の陸軍力の開幕の落ち込み・読む 試験の秒の段 0。挙動は答え待ちの無い物から（鉱山の上限 MC は計器の後）。D: は keep_1087（約 89GB）に、挙動の線を入れる波は新しい keep を並べる＝床を先に数える。')
row = head + '<br>'.join(new) + ' | 実装ログ (1079)〜(1087) |'
open(out, 'wb').write(row.encode('utf-8'))
print('ok', len(new))
