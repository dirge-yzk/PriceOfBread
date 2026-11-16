# 読むだけ (1176) c116rl：11-12 の S10 の 76 行を、台帳の id（S10-Hnnn＋枝の字）・今の木 b9570c8a7 の錨・写しと読み直しの別で 12 列の TSV に書く。
#  (1169) の mk_tsv.py の形を写した。入力：11-12 の tsv（scratchpad）・out_cmp_S10.tsv。出力：ledger_S10_2026-11-16.tsv（12 列）と side_S10_2026-11-16.tsv（写しの別）。
# 使い方: python -X utf8 -I mk_tsv_s10.py <scratchpad> <作業フォルダ>
import sys, os
SP, W = sys.argv[1:3]
OLD, NEW = '9b18137a', 'b9570c8a7'
MAP = {'H001': [1, 2], 'H002': [3, 4, 5, 6], 'H003': [7, 8, 9, 10, 11], 'H004': [12], 'H005': [14], 'H006': [15], 'H007': [16], 'H008': [17],
       'H009': [18], 'H010': [19, 20], 'H011': [21], 'H012': [22], 'H013': [23], 'H014': [24, 25, 26], 'H015': [27], 'H016': [28], 'H017': [29],
       'H018': [30], 'H019': [31], 'H020': [32], 'H021': [33], 'H022': [34, 35], 'H023': [36], 'H024': [37, 38], 'H025': [39, 40, 41, 42],
       'H026': [43], 'H027': [44], 'H028': [45, 46], 'H029': [47], 'H030': [48, 49, 50, 51, 52, 53], 'H031': [54, 55], 'H032': [56, 57],
       'H033': [58, 59], 'H034': [60], 'H035': [61, 62, 63], 'H036': [64], 'H037': [65, 66], 'H038': [67], 'H039': [68, 69], 'H040': [70],
       'H041': [13], 'H042': [71], 'H043': [72], 'H044': [73], 'H045': [74], 'H046': [75], 'H047': [76]}
def newid(k):
    for h, ks in MAP.items():
        if k in ks: return 'S10-' + h + ('' if len(ks) == 1 else 'abcdefghijklm'[ks.index(k)])
    raise KeyError(k)
IDS = {k: newid(k) for k in range(1, 77)}
assert len(set(IDS.values())) == 76
# 錨の手直し（cmp_rows で取り直せなかった物・注の中の名だった物＝墓標は試験の RetiredFields か docs/history の行へ）
ANCH = {
 'S10-01': 'PopClass.cs:15:Researcher;Simulation.cs:783:ResearcherFrac',
 'S10-08': 'Simulation.cs:2208:c.HealthcareCapacityBonus',
 'S10-21': 'PriceOfBread.Tests/DormantIdentityTests.cs:311:FertilityEquilibriumWeight',
 'S10-22': 'PriceOfBread.Tests/DormantIdentityTests.cs:311:FertilityLandDivisionSlope',
 'S10-23': 'PriceOfBread.Tests/DormantIdentityTests.cs:311:FertilityIncomeSurvivalGate',
 'S10-27': 'PriceOfBread.Tests/DormantIdentityTests.cs:361:FertileAbsoluteBlend',
 'S10-33': 'PriceOfBread.Tests/DormantIdentityTests.cs:239:FamineAbsoluteThreshold',
 'S10-57': 'docs/history/Coefficients.p08_audit_soldiers.cs.md:536:HoardDishoardRate;docs/history/Coefficients.p08_audit_soldiers.cs.md:153:HoardCrystallizeRate',
 'S10-67': 'PriceOfBread.Tests/DormantIdentityTests.cs:286:EnclosureRate',
 'S10-71': 'docs/history/Coefficients.p02_borrow_polity_epidemic.cs.md:696:UnemploymentMilitancy',
 'S10-72': 'Model.cs:130:MilVoice;PriceOfBread.Tests/DiscontentSplitTests.cs:10:MilLiving',
 'S10-75': 'PriceOfBread.Tests/DormantIdentityTests.cs:312:EducationGateUsesEma',
 'S10-76': 'PriceOfBread.Tests/DormantIdentityTests.cs:312:NewbornLiteracyDilution',
}
CUR = '（今の生 M＝keep_1171/base_ins・N1／N2＝keep_1171/n1・n2〔絞った表〕・N1′／N2′＝keep_1150/n1・n2〔全表のヌル腕〕・台本 r1176 の s10_v.py）'
AI = '〈AI の推奨・確証なし〉'
# 読み直した行：(判定の型 or None, 判定, 根拠, 12 列目 or None＝元のまま)
RE = {
 'S10-02': (None, '一部だけ働く（老年の兵士＝1950 年の世界の 60 歳以上の 2.7〜8.4%・国で最大 14.5%）',
   '〈読み直し〉ソース：兵士は加齢だけで平常の死が 0（Simulation.cs:2278・注は〔履歴 Simulation.cs#303〕＝作者裁定 Q10〈自然の出生と死は外のまま＝9-2 で〉）＝最上層は死でだけ出る。sf_s 1950 年の soldierElder60K÷elder60K：M 2.72〜8.38%（国の最大 14.5%・ヴュルテンベルク 種 7）・N1 2.37〜7.72%・N2 4.94〜8.43%（11-12 の keep_1053 は 5.3〜7.9%）。m1171 で採った兵 0 の崖の線 c115sv は兵士の席の入口（Meritocracy.cs の FillJobs）を変えたが、この行の錨の関数は字が同じ。観戦 Q15 との結び付けは算術の見積もりだけ（71 歳以上の約 23% 以上が兵士である必要・未確かめ・age_s に兵士と男女の列が無い）',
   '直す（比例の形＝兵士の POP を国の平常の死の配り方〔同じ関数の重みの和と死の率〕に入れる・出生は外のまま・60 歳の閾値の復員は README §1-8〈閾値より比例〉に反するので採らない・作者裁定 Q10 を変える直し＝README §1-7 で報告の先頭へ・書き手は 1 行ではない〔事前のループと軍の欠員の扱いにも触れる〕）' + AI),
 'S10-03': (None, '働く', '〈読み直し〉人口＞扶養力（Simulation.cs:2229 の net が負）の国×四半期 M 24,094／147,597（16.32%）・N1′ 16.90%・N2′ 16.46%（11-12 は 16.5%）＝両側が生で出る', None),
 'S10-05': ('両側の門・頻度', '一部だけ働く（回復期の月 0.47%）',
   '〈読み直し〉pop_s の N の行（10 種）：recoveringMonths 5,841／monthsCount 1,229,739（0.47%）・N1′ 0.49%・N2′ 0.55%（11-12 は種 1〜3 で 0.33%）。門の両側は出るが回復期はまれ（Simulation.cs:2257）', '残す（読み手は Simulation.cs:2257・:2258 の 2 本・人口保存の枝と一体）'),
 'S10-06': ('届かない門・頻度', '働かない（床に届いた行 0／147,597）',
   '〈読み直し〉capacity≦0.3×anchorUsed（Simulation.cs:2211 の Math.Max の第 1 項）の国×四半期 M 0／147,597・N1′ 0・N2′ 0。扶養力が 0 か非有限の行 M 19・N1′ 37・N2′ 33＝同じ行の否定形の門 !(capacity > 1e-9) が人口の段を飛ばす（床は NaN を除かない）。11-12 の理由〈割り算が壊れるのを防ぐ発散の錠〉は否定形の門の役＝床は全滅を防ぐ下限で、思想⑧の例外（発散する量への指数関数的な錠）に当たらない（11-12 反証 中-3 ④）。食料の錨の Math.Max(0.3, …)（:2192）も同じ型・届いた割合は列が無く未数',
   '消す（(i) 測った盤面で届かない＝届いた行 0・読み手 エンジン 1〔Simulation.cs:2211〕・計器 0・試験 0・App 0・失う物＝飢饉で扶養力が錨の 3 割を割る盤面の下限〔10 種とヌル腕 2 本で 0 件〕・消し方＝第 1 項を外し否定形の門だけ残す＝届かないので恒等の見込み〔ident で確かめる〕・印：天井と床）' + AI),
 'S10-10': (None, '働く',
   '〈読み直し〉scalars_s 10 種の workHoursValue：5%点 10.0・中央 12.0・95%点 14.0。12 時間から 1 時間以上離れた国×四半期 M 29.8%（超 12.5%・未満 17.3%）・N1′ 31.2%・N2′ 32.0%（0.5 時間以上 M 33.0%）＝まれな項ではない。読み手はエンジン 2（扶養力 Simulation.cs:2210 の WorkHoursCapacity〔:2360〕・産出 World.cs:1728 の whOut）',
   '残す（11-12 の〈消す〉を外した＝反証 高-1：消すと労働時間の法の短縮が産出の損だけになる・扶養力の ±2〜3% は均衡人口の ±2〜3%）' + AI + '・作者に聞く物 Q2'),
 'S10-11': (None, '働く',
   '〈読み直し〉capBase を食料の錨が決める行 M 32.42%・耕地が決める行 67.58%（N1′ 33.16／66.84%・N2′ 34.00／66.00%・LandAnchorCapacityWeight 1＝capBase は食料の錨の混ぜと耕地の大きい方）。食料の不足の 3 本（乗数の項 Simulation.cs:2209・食料の錨 :2192〜2193・飢えの超過死亡）が揃うのは食料の錨が決める行だけ。11-12 の 28.7%／67.4%〈錨のまま 3.9%〉は反証 中-1 で直した（錨より下の行 M 3.46%・錨と等しい行 1）',
   '直す（食料の不足の入口を死の側の段3 で 1 本に決める・耕地の錨の行ではこの項が唯一の線なので単独では消さない・読み手 エンジン 1〔Simulation.cs:2209〕・計器 0・試験 0）' + AI),
 'S10-13': (None, '働く（ソースだけ・届く割合は未数）', '〈読み直し〉ソース（min の第 3 項・今の木で字が同じ）。量の列（DiagHealthcare・DiagDoctorCare）は Runner の popprobe だけで keep_1171 に無い＝届く割合は数えていない（反証 低-4）', None),
 'S10-15': (None, '一部だけ働く（食料の錨が扶養力の基準を決める行 32.4%）',
   '〈読み直し〉anchor_s 10 種：capBase が耕地（arableSum）と一致しない行＝食料の錨が決める行 M 32.42%・N1′ 33.16%・N2′ 34.00%。錨より下 M 3.46%（5,109 行）・錨と等しい 1 行（11-12 の〈錨のまま 3.9%〉は誤り＝反証 中-1）', None),
 'S10-18': (None, '働く（文書どおり＝出生率は純増に効かない・直の効き 0）',
   '〈読み直し〉文書 L1816・L2292 は出生率が純増に効かないと書き、Simulation.cs:2229 の net は fertility を持たない（今の木で字が同じ）＝期待する動きどおり。年齢の構成→労働→産出→食料→扶養力の間接の道はある。観戦 M3 の構造の根の候補は〈設計の疑い〉（推測・反証 高-2）。都市化の少子化（UrbanCapacityPenalty）だけが純増に効く非対称も設計の疑い（推測）。印：天井の錠（LandRoomFertilityCap＝Simulation.cs:2206）',
   '残す（9-2 の設計の問い〈出生と死を下から積む形にするか〉へ送る・直す行にしない＝人7〔Simulation.cs:2228 の注〕と S10-H011 の削除を開け直す向き）' + AI),
 'S10-19': (None, '働く（文書どおり＝出生の数と子の割合を動かす・純増への直の効き 0）',
   '〈読み直し〉S10-H009 と同じ（文書 L2292・Simulation.cs:2229）。女子教育と所得の 2 本は出生の数・子の割合・死の内訳を動かし、人口の量へは間接の道だけ（反証 高-2）',
   '残す（9-2 の設計の問いへ・M3 の材料）' + AI),
 'S10-20': ('届かない門・頻度', '一部だけ働く（教育権以上の段に届いた国×四半期 0.16%・届いても効きはほぼ 0）',
   '〈読み直し〉scalars_s 10 種 womenRightsCode：段 2（教育権）M 112・段 3（参政権）129（種 1 だけ）＝241／147,616（0.16%）・N1 183（0.12%）・N2 94（0.06%）（11-12 は 185・段 3 は 0）。読み手 WomenEducationLaw（World.cs:324）＝Simulation.cs:2222・TechEducation.cs:24。教育権の段は財産権の段を含み、賃金の保持が 1 へ寄り切ると上乗せは上限（全体の識字）に切られて 0＝追従の途中の月だけ差。段の効きは Meritocracy.cs の GenderAccess（:88 ほか）に残る（反証 中-4）',
   '消す（(i) ほぼ恒等＝届いても上限で 0・読み手 エンジン 2〔Simulation.cs:2222・TechEducation.cs:24〕・試験 1〔GenderTests.cs:361〕・App 0・失う物＝追従の途中の月の女性識字の上乗せ）' + AI),
 'S10-28': (None, '働く', '〈読み直し〉pop_s の N の行（10 種）：飢えの死÷粗死亡（平常＋飢え＋他）M 2.79%・N1′ 3.09%・N2′ 3.08%・飢えの死が正の国×四半期 99.93%（11-12 は種 1〜3 で 2.83%）＝浅い飢えの死がほぼ全ての国で常に少しある', None),
 'S10-31': (None, '働く', None,
   '消す（国の FoodFill の状態＝思想⑩・州の値から導く・読み手 SimCore 約 10 行（Simulation.cs:2209・Politics.cs:198・:283・SelfPlay.cs:434・:1065・Diplomacy.cs:1604 ほか〔州の FoodFill と名が同じ＝消す線は型で割る〕）・計器 InstrumentsAnchor.cs:24 ほか 1・試験 4 本・Runner 2〔Program.cs:4113・:4325〕・値の内訳の説明 World.Explain〔Simulation.cs:2141〜2143〕がこの値を在庫として扱う＝消す線に含める・失う物＝国の主食の充足の平滑〔穀倉の備蓄の時定数〕・印：セーブの往復の穴の確かめ〔状態を消す線は savetest〕）' + AI),
 'S10-46': (None, '働く', None, '直す（感染源を年の頭の盤面で 1 回取る・書き手 1 か所＝恒等ではない小さな線・代価＝同じ年の連鎖〔1918 年型の速い広がり〕が無くなり 1 段に 1 年かかる〔反証 低-3〕）' + AI),
 'S10-48': ('片側だけの門・頻度', '一部だけ働く（賃金の保持が立つ国×四半期 1.96%）',
   '〈読み直し〉ソース：困窮の上乗せは賃金の保持が 0 の国では 0（文書どおり）。scalars_s 10 種 womenRightsCode≧1（財産権以上）M 2,888／147,616（1.96%・段 1 は種ごとに 122〜431）・N1 3,282（2.23%）・N2 2,654（1.80%）（11-12 は 2.3%）＝ほとんどの国は 0.4 の構造値だけ', None),
 'S10-55': (None, '働く', None,
   '消す候補（S23 が持ち主＝S23-H019・(iii) 闘争度への直の加算＝思想⑨の型・係数の注の残す理由〈外すと清が覇権化〉はバランスの理由で残す理由にしない・前提＝徴兵で働き手が抜けた家計の所得の落ち込みから既存の不満の経路で出ること〔推測〕・出ないなら残す・読み手 エンジン 3〔Military.cs:201・:203・:204〕・試験 2〔MilitaryTests.cs:23・:51〕）' + AI),
 'S10-58': (None, '一部だけ働く（門が開き住居が足りない州×四半期 21.7%・崖）',
   '〈読み直し〉region_s 種 1：housingFactor＜1 の州×四半期 47,371／218,495（21.7%・11-12 は 22.8%）。門が開いて充足 1 の州はこの数に入らない（門の開いた割合そのものは列が無い）。門は 0 か全額の崖（文書どおり）。⚠ Catalog.cs の注は〈都市率 45% 超〉のまま（係数は 0.25）',
   '直す（閾値の門 HousingGateOpen〔PopDecision.cs:19・:31・EconomyStep.cs:1574〕だけを消す＝必要量は既に都市の POP の規模に比例〔需要に都市の割合をさらに掛けると 2 乗＝反証 低-1〕・試験 HousingGateSharedTests ほか 5・合成の較正 AgrarianToUrban が門に頼る・持ち主の重なり S04-44・印：天井と床）' + AI),
 'S10-59': (None, '働く', None,
   '消す（倍率＝州の間の引力は賃金から家賃を引いた実質で読めば要らない〔農村→都市は既に家賃を値段で読む PopDecision.cs〕・読み手 エンジン 1〔Territory.cs:106〕・計器 1〔Instruments.cs:2153〕・試験 3〔IndustryThemeTests.cs:22・:32・:44〕・持ち主の重なり S04-45・S13-17・印：倍率の一掃）' + AI),
 'S10-61': (None, '働く', None,
   '消す（(iii) 需要は保険の値下げから市場で出る・倍率は支払い率を見ない〔財政が払えない福祉の法でも医薬品の需要が上がる＝反証 低-5〕・思想⑨・読み手 エンジン 1〔EconomyStep.cs:1555〕・計器 0・試験 0・App 0・持ち主の重なり S04-40・S04-42・印：倍率の一掃）' + AI),
 'S10-64': ('効き・頻度', '一部だけ働く（1950 年に年金の段が正の国 1〜10／61〜63）',
   '〈読み直し〉sf_s 1950 年に pensionLevel＞0 の国：M 種ごとに 7・1・8・7・10・4・9・9・3・6／61〜63・N1 3〜10・N2 4〜11・初めて正になった年 M 1841〜1950（11-12 は 4〜12／種）。⚠ 福祉と年金が同じ上限を別々に取る＝観戦 Q10（S20-H019）。⚠ 老年の兵士も受け手（S10-H001b）', None),
 'S10-66': (None, None, None, '直す（戦の減産を StapleHarvestFactor〔EconomyStep.cs:147〕の中に置く＝呼び手 3 か所〔:179・:186・:1903〕がそろう・市場向けの boost の減産はそのまま・係数は既存の 1 本・持ち主の重なり S03-36〔反証 低-2〕）' + AI),
 'S10-73': (None, '働く', '〈読み直し〉dcont_s 10 種：milFamineMeanShare＞0 の国×四半期 M 59.0%（N1′ 59.2%・N2′ 58.7%）・正の行の中央 0.0133・95%点 0.285（11-12 は種 1〜3 で 58.8%）＝浅い寄与が多く深い駆動は少ない。S14 の行と同じ機構', None),
}
# 写した行の 12 列目の手直し（今の木の行番号・消す候補の見落とし・他の系統の持ち主）
FIX = {
 'S10-07': '消す候補（衛生の項 SanitationCapacityWeight だけ＝同じ衛生が疫病の発生と致死率〔World.cs の EpidemicOnsetSanitationWeight ほか〕で既に死へ入る＝思想⑨・読み手 エンジン 1〔Simulation.cs:2210〕・試験 0・Runner 0・App 0・反証 中-3 ①）' + AI,
 'S10-09': '消す（(iii) 同じ医者の役務を医療の項と 2 度読む＝思想⑨・読み手 エンジン 1〔Simulation.cs:2208〕・計器 0・試験 0・App 0）' + AI,
 'S10-12': '消す（(iii) HealthcareEducationGate＝識字の代理・実体のゲートと二重・読み手 エンジン 1〔Simulation.cs:2128〕・計器 0・試験 0）' + AI,
}
# 列の直書き（ledger_check の門 2：判定を埋めた行は判定の型を持つ＝11-12 は S10-38 を型〈—〉のまま〈支配の力の段で書き直す〉とした＝判定ではない）
OVR = {'S10-38': {8: '未確かめ', 10: '支配の力の段で書き直す（11-12 の読み・機構の判定はしていない）'}}
CMP = {}
for l in open(os.path.join(W, 'out_cmp_S10.tsv'), encoding='utf-8').read().split('\n')[1:]:
    c = l.split('\t')
    if len(c) >= 4: CMP[c[0]] = c
L = open(os.path.join(SP, 'ledger_S10_2026-11-12.tsv'), 'rb').read().decode('utf-8').split('\n')
hdr = L[0].rstrip('\r').split('\t'); out = ['\t'.join(hdr)]
side = ['\t'.join(['行', '11-12 の行', '区分', 'コードの差（注と空白を除く・錨を囲む関数）', '11-12 の判定', '今の判定', '確証'])]
def ref_old(s):  # 根拠の中の 11-12 の行の id を台帳の id へ
    import re
    return re.sub(r'S10-(\d\d)\b', lambda m: IDS[int(m.group(1))] if 1 <= int(m.group(1)) <= 76 else m.group(0), s)
for l in L[1:]:
    c = l.rstrip('\r').split('\t')
    if len(c) < 12: continue
    old = c[0]; k = int(old.split('-')[1]); nid = IDS[k]
    cm = CMP.get(old)
    anch = ANCH.get(old, cm[2] if cm and cm[2] else c[4])
    oldj = c[8]; diff = (cm[1] + ('：' + cm[3] if len(cm) > 3 and cm[3] else '')) if cm else '—'
    if old in RE:
        kind_, j, basis, fx = RE[old]
        c[7] = NEW
        if kind_: c[6] = kind_
        if j: c[8] = j
        if basis: c[9] = basis + CUR
        else: c[9] = '〈読み直し・12 列目と根拠の訂正だけ＝数は 11-12 の読み（keep_1053）〉' + c[9]
        if fx is not None: c[11] = fx
        kind = '読み直し'; conf = '生とソース（今の木 b9570c8a7・keep_1171）' if basis else 'ソースと 11-12 反証の訂正（数は keep_1053 のまま）'
    else:
        c[7] = OLD; c[9] = '〈写し・11-12 の読み（木 9b18137a・生 keep_1053）〉' + c[9]
        kind = '写し'; conf = '11-12 の読み（keep_1053）＝今の生では数え直していない'
    if old in FIX: c[11] = FIX[old]
    for i, v in OVR.get(old, {}).items(): c[i] = v
    c[0] = nid; c[4] = anch
    c[9] = ref_old(c[9]); c[11] = ref_old(c[11]); c[10] = ref_old(c[10])
    for i in range(12): c[i] = c[i].replace('|', '／').replace('\t', ' ').replace('\n', ' ')
    out.append('\t'.join(c))
    side.append('\t'.join([nid, old, kind, diff.replace('\t', ' ')[:300], oldj, c[8], conf]))
open(os.path.join(W, 'ledger_S10_2026-11-16.tsv'), 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
open(os.path.join(W, 'side_S10_2026-11-16.tsv'), 'w', encoding='utf-8', newline='\n').write('\n'.join(side) + '\n')
import collections
print('行', len(out) - 1, '区分', collections.Counter(s.split('\t')[2] for s in side[1:]), '判定', collections.Counter(x.split('\t')[8].split('（')[0] for x in out[1:]))
print('12 列目', collections.Counter(x.split('\t')[11][:2] for x in out[1:]))
