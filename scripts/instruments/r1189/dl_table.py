# (1189) 問い1：削除の候補の表と順位。読み手の数は dl_readers.py の出力（out_readers.tsv・木 ef7db3cc8）。
#  (b) 他の量と重なる（思想⑨）・(c) 閾値・錠・床・出来事の加算を持つ・(d) 難しい設計の線が触るファイルにある（段 E・F・G＝Diplomacy.cs・Colonial.cs／
#  貿易 T2・T3＝BilateralMarket.cs／王権と議会＝Parliament.cs・CrownParliament.cs／国民意識の物理の削除〔波119〕＝Politics.cs・World.cs・Military.cs・Ideology.cs・MultiNationSim.cs・App・Runner）
#  点＝2×(E≤2)＋1×(3≤E≤5)＋1×(試験≤3)＋2×(b)＋2×(c)＋2×(d でない)＋2×(恒等)＋1×(App と Runner が 0)。外す物は点を付けない。
#  使い方: python -X utf8 -I dl_table.py <out_readers.tsv> <出力 tsv> <出力 順>
import sys, csv
sys.stdout.reconfigure(newline='\n')
rd = {r['id']: r for r in csv.DictReader(open(sys.argv[1], encoding='utf-8'), delimiter='\t')}
# id: (b, c, d, 消し方, 史実で重要か, 失う物, 前提の線, 持ち主のファイル, 外す理由 or '')
A = {
 'K01': (0, 1, 0, '恒等の見込み（ident で確かめる・違えば挙動）', '否', '飢饉で扶養力が錨の 3 割を割る盤面の下支え（(1176)：届いた行 0）', 'なし', 'Simulation.cs:2208', ''),
 'K02': (1, 0, 0, '挙動', '否', '医者の役務の 0.05 の項', '9-2（人口の段C）', 'Simulation.cs:2205', '倍率の一掃の A（扶養力 5 本＝9-2 の線で消す）＝印だけ'),
 'K03': (1, 1, 0, '挙動', '否', '識字で社会保障の医療を上から切る錠', 'なし', 'Simulation.cs:2125', ''),
 'K04': (1, 0, 0, '挙動', '否', '衛生の扶養力の項', '9-2', 'Simulation.cs:2207', '倍率の一掃の A（扶養力）＝印だけ'),
 'K05': (1, 1, 0, '恒等の見込み（上限で 0）', '否', '女子教育の法の識字の上乗せ（月の追従の途中の差だけ）', 'なし', 'Simulation.cs:2220・TechEducation.cs:24', '倍率の一掃の B（残す）と食い違う＝消すなら上書きを名乗る（作者に聞く材料・推奨は消す）'),
 'K06': (1, 0, 1, '挙動（状態を消す・savetest）', '否', '国の主食の充足の平滑の状態（思想⑩）', '9-2 で FoodCapacityWeight を消した後', 'Simulation.cs・Politics.cs・SelfPlay.cs・Diplomacy.cs', '倍率の一掃の A（FoodCapacityWeight）の後＝今は順に入れない'),
 'K07': (0, 0, 1, '恒等（I）', '否', '×1.0 の時代係数と運賃革命の読み', 'App の線（EraFactor は App の GameViewModel.Politics.cs:636 が読む）', 'BilateralMarket.cs・InternationalMarket.cs・Military.cs・Territory.cs・MultiNationSim.cs・App', 'App と貿易の線のファイル＝⑲ の束か T2 の後'),
 'K08': (0, 0, 0, '恒等（休眠）', '否', '行政の届きで義務教育の給付を細らせる器（係数 0）', 'なし', 'EconomyStep.cs:1780', ''),
 'K09': (0, 0, 0, '恒等（休眠）', '否', '師範学校の倍率の器（係数 0・高等教育の需要 0）', 'なし', 'Simulation.cs:1849〜:1873', ''),
 'K10': (0, 1, 1, '恒等（休眠）', '是（同業組合の廃止は 1791〜1860 の欧州の大きな法）', '同業組合法の軸と組合の地代', '—', 'EconomyStep.cs・Government.cs・Parliament.cs・Politics.cs・SelfPlay.cs ほか 8 ファイル', '史実で重要（法の軸ごとは消す側に入れない）＝GuildMobilityDrag だけは K10′'),
 'K10p': (1, 0, 0, '恒等（休眠）', '否', '職人→工場の没落の遅れ（10-18 精査 政4〈消してよい〉）', 'なし', 'Meritocracy.cs:122', ''),
 'K11': (0, 1, 0, '恒等', '否', '温度 0 のときだけ走る総取りの枝・常に +∞ の天井の残り枠（作者「全て推奨で」で線にすると決めた・2026-11-12）', 'なし', 'Meritocracy.cs（:309・:330〜:337・:345・FillJobs の総取りの枝）', ''),
 'K12': (0, 0, 0, '恒等（旗の焼き込み ×1.0）', '否', '旗 1 本（式は残る）', 'なし', 'EconomyStep.cs:2519・:2528', ''),
 'K13': (0, 0, 0, '恒等（旗の焼き込み）', '是（農奴の移動の縛り）＝式は残る', '旗 1 本（式は残る）・試験の旗を切る道具（RdhInstrumentTests.cs:33 ほか）', 'なし', 'Meritocracy.cs:301・:304・PopDecision.cs:73・:74・InstrumentsUrx', ''),
 'K14': (0, 0, 0, '恒等（旗の焼き込み）', '否', '旗 1 本（式は残る）・MobilityTests.cs:52 の書き直し', 'なし', 'Meritocracy.cs:178・:189・:248', ''),
 'K15': (1, 0, 0, '挙動', '否', '職探しの率の識字の ×0.5〜1', 'なし', 'Meritocracy.cs:138', ''),
 'K16': (1, 1, 0, '挙動', '否', '少数派の摩擦の時代ブレンド', '00-BUFF 順 3', 'Meritocracy.cs', '倍率の一掃の A（17）＝印だけ'),
 'K17': (0, 1, 0, '恒等（休眠）', '中', '個人債務の器', 'S22 の線', 'Simulation.cs・EconomyStep.cs・Meritocracy.cs', '持ち主 S22 が〈残す〉（S12-H014e）＝答え待ち'),
 'K18': (0, 0, 0, '恒等（休眠・閉じたテーマ）', '否', '自給の効率化材の倍率を外す旗', 'なし', 'EconomyStep.cs:117・:139', ''),
 'K19': (0, 0, 0, '恒等（休眠）', '否', '外資の余地の器', '—', 'CapitalMarket.cs:11', '作者の答え待ち（★(665) ③〈0 のまま〉）'),
 'K20': (0, 1, 0, '恒等（休眠）', '否', '消費の予算の貯蓄の取り崩しの切り', 'なし', 'EconomyStep.cs:1452〜:1454', ''),
 'K21': (0, 1, 1, '恒等（休眠）', '否', '平時の植民地の徴募の遺恨の器', '波119 の国民意識の削除と K3 の扱い（Military.cs の DrawFromPool）', 'Military.cs:160・:161', ''),
 'K22': (1, 1, 1, '恒等（休眠）', '否', '蜂起の民兵の札', '波119 の後（Politics.cs）・段 E（Diplomacy.cs）', 'Politics.cs:550・:557・Diplomacy.cs:783・:786・:2305', ''),
 'K23': (0, 0, 0, '恒等（休眠）', '否', '電動機の親の産業の旗', 'なし', 'Discovery.cs:530', ''),
 'K24': (0, 0, 0, '恒等（休眠）', '否', '開幕の焼き込みの旗・表・動詞', '工業化度の削除の後に倒す予定（00一覧）', 'MultiNationSim.cs:432・OpeningBake', '条件つき（倒すか消すかが未定）'),
 'K25': (0, 0, 0, '恒等（休眠）', '否', '主食の乗り換えの挟みを外す旗（②）', 'なし', 'EconomyStep.cs:1528・計器 fd の 2 列', ''),
 'K26': (0, 1, 1, '恒等（休眠・閉じたテーマ）', '否', '輸入の払い手の限定', 'T2・T3（BilateralMarket.cs）', 'BilateralMarket.cs:694・:695', ''),
 'K27': (0, 0, 1, '恒等（休眠）', '否', '安く売るの輸出奨励金', 'T2・T3', 'BilateralMarket.cs:760・Simulation.cs:1918', ''),
 'K28': (1, 0, 1, '挙動', '否', '皆兵の徴兵の倍率', '★(1078) ② の答え', 'Military.cs:156', '答え待ち（★(1078) ②）'),
 'K29': (0, 0, 0, '恒等（旗の焼き込み）', '否', '旗 1 本・旧の戦時の倍率の枝', 'hc の合流の後（Simulation.cs:1731・:1775 が hc の :1734 の隣）', 'Simulation.cs:1731・:1775', ''),
 'K30': (0, 1, 1, '恒等', '否', '動的な政党（App の与党名の口は残す）', '王権と議会の作り直し（Parliament.cs）', 'Parliament.cs（16 行）', ''),
 'K31': (0, 1, 1, '恒等（届かない枝）', '否', '経路 6 と violent＝偽の枝', '波119 の後（Politics.cs）', 'Politics.cs:369〜:388', ''),
 'K32': (0, 0, 0, '恒等', '否', 'タンス預金の比', '—', 'Credit.cs:12・:13', '倍率の一掃の B（HoardPanicWeight を残す）と食い違う＝消すなら上書きを名乗る（作者に聞く材料）'),
 'K33': (0, 1, 1, '恒等の見込み（ident）', '否', '金本位を守る与信の床の上乗せ', '波119 の後（World.cs）', 'Credit.cs:41・:44・World.cs:1352', ''),
 'K34': (0, 1, 1, '挙動（区分 II）', '中（変動相場の幅）', '為替の床と天井の Clamp', '波119 の後（World.cs）', 'World.cs:898・:1385・:1392・CapitalMarket.cs:51', ''),
 'K35': (1, 0, 1, '挙動（61 カ国は恒等・単一国の試験の世界が変わる）', '否', '単一国の盤面で代金なしに植民地財を湧かす枝', '段 E の後（Colonial.cs）', 'Simulation.cs・EconomyStep.cs:823・:830・Model.cs:705・:708・Colonial.cs:13', ''),
 'K36': (1, 1, 1, '挙動', '否', '反帝世論の直の加算', '段 E', 'Colonial.cs:41・:53', '倍率の一掃の B（残す）・段 E の持ち物'),
 'K37': (1, 1, 1, '挙動', '否', '戦争が治安維持力を奪う', '—', 'Politics.cs', '倍率の一掃（WarCoercionDrain は A の一覧）＝印だけ'),
 'K38': (0, 1, 1, '挙動', '否', '凍結の解凍', 'ur の後（Diplomacy.cs）', 'Diplomacy.cs:3239', ''),
 'K39': (1, 1, 1, '挙動', '否', '覇権の封じ込め', '段 E・F・G', 'Diplomacy.cs', '倍率の一掃の A の歯止め・段 E・F・G'),
 'K40': (0, 0, 1, '恒等（I）', '否', '運賃革命の上限（×1.0）', '⑲（App 1・Runner 2）', 'BilateralMarket.cs:671', '⑲ の束（App・Runner）'),
 'K41': (0, 1, 1, '恒等か直す', '中', '海上保険の給付', 'Q-s08-2 の答え', 'MultiNationSim.cs・BilateralMarket.cs', '答え待ち（Q-s08-2）'),
 'K42': (0, 0, 1, '挙動', '是（スエズ運河の株 1875 年）', '運河の配当', 'Q-s08-3 の答え', 'MultiNationSim.cs', '史実で重要・答え待ち（Q-s08-3）'),
 'K43': (0, 1, 0, '恒等', '否', '組合法の出力倍率', '—', 'World.cs:1731', '倍率の一掃の A＝印だけ'),
 'K44': (0, 0, 0, '恒等（I）', '否', '発見の表の難易度 Y の旧の枝', '⑲（Runner 1）・M4', 'Discovery.cs:13〜:15・:897', '⑲ の束（Runner）'),
 'K45': (0, 0, 0, '恒等（休眠）', '是（コムストックの銀 1859 年）', '銀の産地の点火の道', '—', 'Simulation.cs:693', '史実で重要（点火の道が消える）'),
 'K46': (1, 0, 0, '挙動', '是の疑い（動力）＝機械の財と二重', '労働力の財の蒸気と電動のレシピ', '枠と機械（資本の在庫）の設計の後', 'Simulation.cs:1113・:1115', '設計の後（作者「全て推奨で」で消すと決めたが線は未起票）'),
 'K47': (1, 0, 0, '挙動', '否', '石油灯油のレシピと行', 'なし', 'Catalog.cs:433・Simulation.cs:1228', ''),
 'K48': (0, 1, 0, '挙動', '否', '工場の休止の動詞', '計器が先（大きさ）', 'EconomyStep.cs:2447・:2449・Simulation.cs:2028・:2031・Model.cs:514', '計器が先（S05-K6）'),
 'K49': (0, 0, 0, '挙動', '否', '勢いの投資（利潤の傾きの外挿）', 'なし', 'EconomyStep.cs:2451', ''),
 'K50': (0, 0, 1, '恒等', '否', '巨大建造物の口', '⑲（App 1・Runner 7）', 'Megaprojects.cs・Diplomacy.cs・App・Runner', '⑲ の束'),
 'K51': (1, 1, 1, '恒等', '否', '海軍の運用燃料の器（状態 NavalFuelFillEma を含む・savetest）', '⑲（App 3）', 'Military.cs・Diplomacy.cs・World.cs・App', '⑲ の束（App）'),
 'K52': (0, 0, 0, '恒等（I）', '否', '切り下げの指数（休眠）', 'なし', 'Simulation.cs:1969・Fiscal', ''),
 'K53': (0, 1, 0, '恒等', '否', '切り下げの月の上限', '—', 'Simulation.cs', '倍率の一掃の A＝印だけ'),
 'K54': (1, 0, 1, '恒等（本番）・試験の世界は変わる', '否', '外圧の連鎖（入力が常に 0）', '波119 の後（Ideology.cs・MultiNationSim.cs）', 'Ideology.cs:18・MultiNationSim.cs:365 ほか', ''),
 'K55': (0, 0, 1, '恒等（I）', '否', '時代係数の残り 2 本と汚職の残骸・ClosedWealthMultiple', '波119 の後（MultiNationSim.cs）', 'Territory.cs:70・:73・MultiNationSim.cs:1065・Simulation.cs:774・:775', ''),
 'K56': (0, 0, 1, '恒等', '否', '文化の 2 段の焼き込み（world.json の札）', '段 E（Diplomacy.cs）', 'Diplomacy.cs・world.json', ''),
 'K57': (0, 1, 1, '挙動', '否', '文化圏の自動の統一の規則', 'ur（統一戦争の AI）の結果の後', 'Formations.cs・CultureRealms', ''),
 'K58': (0, 0, 1, '消す（シミュは恒等）', '否', '宗教統一度の表示', '⑲（App 1）', 'World.cs:883〜:884・App', '⑲ の束'),
 'K59': (1, 0, 0, '挙動', '否', '財産権法の参加率の段（作者「全て推奨で」で倍率の一掃の B を上書きした・2026-11-12）', 'なし', 'Simulation.cs:2218', ''),
 'K60': (1, 1, 1, '挙動', '否', '戦時の強制徴兵の遺恨', 'S23', 'Military.cs:201〜:204', '倍率の一掃の B（残す）・S23 の持ち物'),
 'K61': (0, 0, 0, '挙動', '否', '特許の行', 'M4 の後', 'Discovery.cs・TechEducation.cs', '条件つき（M4 の後）'),
 'K62': (1, 0, 0, '挙動', '否', '住居の移民の摩擦', '—', 'Territory.cs:106', '倍率の一掃＝印だけ'),
 'K63': (1, 0, 0, '挙動', '否', '社会保障の医薬品の倍率', '—', 'EconomyStep.cs:1555', '倍率の一掃の A＝印だけ'),
 'K64': (0, 1, 0, '挙動', '是（教会は消えない）', '聖職者の下限', '人口の段C の人4', 'Meritocracy.cs', '倍率の一掃の A・史実で重要（反証）'),
}
fo = open(sys.argv[2], 'w', encoding='utf-8', newline='\n')
fo.write('\t'.join(['id', '候補', '(a) 読み手 E/I/D/C/T/U/A/R', '(b) 重なり', '(c) 閾値・出来事', '(d) 難しい設計のファイル', '失う物', '史実で重要', '消し方', '前提の線', '持ち主のファイル', '点', '外す理由']) + '\n')
sc = {}
for k, v in A.items():
    b, c, d, how, hist, lose, pre, own, ex = v
    r = rd.get(k if k != 'K10p' else 'K10')
    if k == 'K10p': E, T, A_, R = 1, 2, 0, 0; readers = 'E1（Meritocracy.cs:122）/C1/T2（GuildLawTests・DormantIdentityTests）'
    else:
        E, T, A_, R = int(r['E']), int(r['T']) + int(r['U']), int(r['A']), int(r['R'])
        readers = '/'.join(r[x] for x in ('E', 'I', 'D', 'C', 'T', 'U', 'A', 'R'))
    ident = how.startswith('恒等')
    s = '' if ex else 2 * (E <= 2) + (3 <= E <= 5) + (T <= 3) + 2 * b + 2 * c + 2 * (not d) + 2 * ident + (A_ + R == 0)
    name = r['名'] if r else '同業組合の移動の遅れだけ（S12-H005b）'
    if not ex: sc[k] = s
    fo.write('\t'.join([k, name, readers, '有' if b else '無', '有' if c else '無', '有' if d else '無', lose, hist, how, pre, own, str(s), ex or '—']) + '\n')
print('順に入れる候補', len(sc), '外す', len(A) - len(sc))
for k, s in sorted(sc.items(), key=lambda x: -x[1]): print(k, s, A[k][7], A[k][3][:14], A[k][6][:30])
