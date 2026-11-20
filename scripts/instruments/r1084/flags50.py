# (1227) 注：GuildMobilityDrag は c124kb で消えた＝字面で読む古い台本
# (1206) 注：k3 の係数名（(1199) で消えた）を字面で読む＝(1199) 以後の木では動かない・旧の生でだけ動く（中身は変えていない）
# (1084) 休眠 50 本の確かめ直しの表（main decb50fa5）＝rd50.tsv（読み手の数）＋紙の推奨＋区分（prereg 規則 1）＋束
W = 'D:/PoB_work/r1084_work'
rd = {}
for t in open(W + '/rd50.tsv', encoding='utf-8').read().split('\n')[1:]:
    if t.strip():
        c = t.split('\t'); rd[c[0]] = c
paper = {}
for t in open(W + '/t2_paper.tsv', encoding='utf-8').read().split('\n')[2:]:
    if t.strip():
        c = t.split('\t'); paper[c[1]] = c
# 名: (新しい推奨, 区分, 束, 理由 1 行)
K = {
 'SubsistenceNoBoughtTech': ('消す', 'E0', '束A', 'EconomyStep.cs:113・:134 の三項の右（StapleEffTech）を残す・3 回の不採用で閉じた'),
 'ConsumeBudgetSavingsRatePerMonth': ('消す', 'E0', '束A', 'EconomyStep.cs:1737 の if の塊・相方は 10-16 に削除・精査 経7〈不要〉・(1257) c128k20 で枝と係数を削除済み'),
 'EduReachFromAdmin': ('消す', 'E0', '束A', 'EconomyStep.cs:2108 の `* (旗>0 ? … : 1.0)`＝×1.0 の除去（前例 (804)(843)）・⚠精査 経11〈修正〉と作者 2026-09-08 の裁定を覆す（紙 S04-33〈作者裁定を上書き・推奨で〉が作者「全て推奨で」で決定済み）'),
 'StapleDemandRebalance': ('点火の線（紙 S04-L5 で 1 回・不採用なら器を消す）', 'E1', 'S04-L5', '紙 S04-10（19:14・旗の表 17:04 より後）〈直す（線 S04-L5 で 1 回試す）〉作者「全て推奨で」で決定＝消すのは不採用の後。消すときは：EconomyStep.cs:1819 の項・InstrumentsFd.cs:60 の条件と :72 の段の符号（+0）は列の値を変えない・MultiNationSim.cs:202 は CostLedger の説明の文字列＝字を残すか .cost の説明の差を宣言'),
 'FactoryBuildRefIsReal': ('消す', 'E0', '束A', 'Coefficients.cs:1161 の関数の中の if 1 行だけ（作者 11-12 の裁定③）・⚠精査 経3〈修正（点火）〉は 11-12 の裁定が後'),
 'PeacetimeConscriptGrievance': ('消す', 'E0', '束A', 'Military.cs:273 の bool を false に畳み :274 の引数を `wartimeConscript`・`1.0` の字のまま・⚠精査 軍8〈修正〉を覆す'),
 'TeacherQualityWeight': ('消す', 'E0', '束A', 'Simulation.cs:2247・:2268 の枝（教師の質の q）・⚠精査 経6〈必要〉を覆す・高等教育の鎖そのものが休眠（精査の引く作者 08-17「研究者や技師を増やす補助金」は高等教育の鎖の話＝この旗は教師の質の倍率＝作者 10-16「全てのバフ・デバフ(特に倍率を変更するもの）は全て削除したいです。」に当たる）'),
 'MilitaryProcurePriceGateScale': ('消す', 'E0', '束A', 'DormantOne＝1：EconomyStep.cs:3135 の `if (1.0 < 1.0 …)` の行を消す・⚠精査 :235〈残す（参照価格の作り直しの受け皿）〉を覆す'),
 'NiterBedEnabled': ('消す', 'E0', '群1', 'Simulation.cs:1419 の if（構築）＝紙の群1（CompostFertilizer の直後）と同じ線で・tr の後'),
 'GuildLawGate': ('消す', 'E3+E2+E1', '束B', '法の軸 Guild ごと（Government.cs:147・Parliament.cs:371・PolicyPreferences.cs:360・Politics.cs:1294・SelfPlay.cs:364・:972・Privileges.cs:88・Meritocracy.cs:193・EconomyStep.cs:643）・Display/LawCatalog.cs:102・lawgate_s の列 F(GuildLawGate) と GCode（-1）・PolicyAxis の添字が動く（要求の符号 PendingRevoltDemand は軸＋1 の int）・⚠精査 政4〈修正〉と作者 09-07 の特権の段 4 を覆す（紙 S04-39〈作者裁定を上書き・推奨で〉が作者「全て推奨で」で決定済み・〈恒等でなければ軸を残して効き先だけ消す〉）'),
 'GuildRentPremium': ('消す', 'E2', '束B', 'EconomyStep.cs:643・:695・Privileges.cs:88・ProductionDiag.GuildRent と App GameViewModel.Production.cs:1110 の表示の口'),
 'GuildMobilityDrag': ('消す', 'E0', '束B', 'Meritocracy.cs:193 の if（GuildLawGate と同じ束）'),
 'PopDebtDriven': ('消す', 'E2', '束B', '⚠作者裁定の上書き＝紙 S04-20〈作者裁定を上書き・推奨で〉が作者「全て推奨で」で決定済み（09-26 B18〜B21〈点火〉と精査 経8〈必要〉を覆す）・EconomyStep.cs:1958・Meritocracy.cs:313・Simulation.cs:3068・:3110・Display/FiscalTables.cs:252・下位の係数 4 本（HouseholdCreditMonths・DebtRuinRatio・DebtRepayFrac・PopDebtLossShare＝読み手は旗の枝の中だけ）・09-26 外部レビュー B18〜B21〈点火〉と精査 経8〈必要〉を覆す（紙 S04-20 が名乗る）'),
 'ImportPayerLive': ('消す', 'E4', '束C', 'BilateralMarket.cs:631・:632（`!(0>0 && …)` は真）＝貿易の線（BilateralMarket.cs の持ち主）の中で・scripts 38 行は閉じた線の記録'),
 'TradeCheapSellSubsidy': ('消す', 'E4', '束C', 'BilateralMarket.cs:731・Simulation.cs:2347（清算の最も内側の分岐と配列）＝貿易の線の中で'),
 'DiscoveryClockMotorParent': ('消す', 'E4', 'LR-a', '紙 LR-a（電動のレシピ・電動機の行・欄 EngineerParent と一緒）＝その線が表の差を宣言する（engineer_s ほか）・InstrumentsClock.cs:57 は false の字'),
 'OpeningBakeEnabled': ('残す（消す推奨を取り下げ）', 'NO', '—', '11-01 洗い出し :259〈焼き込みは残りタスクにある＝推奨は消さず、焼き直しだけ止める〉・:179 ③〈旗 0 の間は焼き直さない＝OpeningBakeTests の入力のハッシュの番人を旗 1 のときだけ〉＝恒等の小さな線へ。cr がこの波にも焼き直す（11-01 の洗い出しが数えた焼き直し 約 20 回と同じ型・紙 CU も最初のコミットで焼き直す）'),
 'CapitalHeadroomFromReturn': ('残す（消す推奨を取り下げ）', 'NO', '—', '0 の道（CapitalMarket.cs:22）は `CapitalIndTarget − MeanIndustrialization`＝工業化度を読む側＝旗を消すと工業化度の読み手が固まる。資本の在庫と投資の設計（20261030 §⑥・:273〈別の腕 ★(665)③〉）で両方の道を置き換える'),
 'RebelArmsMarketEnabled': ('残す（消す推奨を取り下げ）', 'NO', '—', '紙 RV（19:05・紙の旗の表 17:04 より後）§9〈民兵と武器の市場は今は消さない・法の軸 CivilianArms ごとの広さ＝読む線で範囲を確かめ王権の段 P2 へ〉・王権の作り直し（design_crown_power :325〈内戦は寝返った守備兵と民兵（兵の 1 未満の重み）で戦う〉）が民兵を前提・紙 S04-06〈その設計で書き直す〉・紙 S29-83〈残す（読み手 エンジン 16 行・Runner 8）〉・⚠頼み文の〈削減の方針で消す推奨〉から外れる。読む線の材料＝SimCore の読み手 9 行（EconomyStep.cs:1502・Government.cs:153・Parliament.cs:376・PolicyPreferences.cs:363・Politics.cs:1140・:1299・SelfPlay.cs:369・:753・Display/LawCatalog.cs:106）'),
 'RevoltMilitiaRaise': ('残す（消す推奨を取り下げ）', 'NO', '—', '同上（紙 RV §9）'),
 'MilitiaCombatValue': ('残す（消す推奨を取り下げ）', 'NO', '—', '同上・Diplomacy.cs:872 `soldiersNow + c.MilitiaCombatValue * militiaMen` と :2639 `1.0 + c.MilitiaCombatValue * m` は式の書き換え（−0・NaN）＝消すなら E5（区分 II か式の形を残す）'),
 'RevoltGrievanceWeight': ('消す（RV-D の中）', 'E4', 'RV-D', '紙 RV §4〈RevoltGrievanceWeight は 0 のまま・RV-D で消す〉＝紙の旗の表の〈点火（RevoltIgnitionIntensity と同じ回）〉を後の紙が替えた・App 1・Runner 1 の読み手'),
}
IGN = {  # 点火の線の順（AI の推奨・確証なし）と支配の力との重なり
 'RevoltIgnitionIntensity': (1, '重なる：支配の力の段 E が RevoltIgnitionTests の分離の部分を変える・RV-1 は段 B の後の最初の波（紙 RV）・RV-0 の計器が先・App 4・Runner 1 の読み手'),
 'DiscontentLawGap': (1, '—（紙の旗の表は RV と同じ回とした＝紙 RV は名指ししない＝RV-1 の事前登録で決める・未確認・Display/PopTables.cs:134 の口）'),
 'EndowmentCostFollowsAbundance': (2, '重ならない（貿易の置き換え B＝作者 11-12〈支配の力の段 B の後〉）'),
 'EndowmentCostFollowsScarcity': (2, '同上・非産地を殺しうる向き＝単独で測る'),
 'MineralCostFollowsGrade': (2, '同上'),
 'TechProductivityBracketOff': (3, '—（倍率の一掃 11-11 の A）'),
 'PrivilegeLedgerEnabled': (4, '—（特権の回）'),
 'PrivilegeStripMilitancy': (4, '—（特権の回・作者 10-11〈残す〉）'),
 'PrivilegeMonopolyInLedger': (4, '—（特権の回）'),
 'PrivilegeFromCrownShare': (4, '疑い：王冠の取り分を読む＝王権の作り直しの段 P と同じ量（設計の名指しは無い）'),
 'NationConsciousnessDerived': (5, '弱い重なり：支配の力の押し返し A は州の国民意識（Politics.cs:1693）を読む＝国のスカラー（CrownParliament.cs:278・:361）とは別の量・設計の名指しは無い'),
 'UrbanFlowReal': (6, '—（3 回の規則の最後・空洞化の根に近い）'),
 'CapitalDividendFromHostRent': (6, '—（3 回の規則の最後）'),
 'SilverParityDemandWeighted': (6, '—（3 回の規則の最後・値 2）'),
 'MintParitySells': (6, '—（造幣の組＝表 1 の群8 と同じ回）'),
 'SilverRushEnabled': (7, '—'),
 'FactoryBuildPriceGateScale': (7, '—（参照価格の作り直しと一緒・作者 10-20〈参照価格は気に入っていない〉）'),
 'SerfGateCoversPool': (8, '—'),
 'CriminalClassEnabled': (8, '—'),
 'EduProducersCoexist': (8, '—（教育と医療の回）'),
 'AssimilationRevival': (8, '弱い重なり：文化の札を動かす＝分離の圧力の入力（設計の名指しは無い）'),
 'UnificationV1Only': (9, '重なる：作者 10-20「統一戦争は名指ししないという原則に反しているので、廃止寄りです。すぐに実装はしませんが、汎用的なものに置き替えたい所存」＝支配の力の作者の言葉「完全に支配した州は宗主国のものとしよう。これにより自然な併合となる筈だ。」が汎用の置き換えに当たる（AI の読み）＝段 F・G で旧の道ごと消す候補（設計の名指しは無い＝疑い）'),
}
FOLD = {'CessionUnified': '段 F（§7 :420）', 'ForcedPeaceBlankOverCollapse': '段 F（§4-3）', 'ProjectionUsesLand': '段 C′（§7 :431・§3-4 :183）', 'RevoltVoiceFloor': '段 B（§7 :432・:472）'}
KEEP = {'LandTaxPhiMove': 'A-2b-1d の答え待ち', 'EmptyPopDebtFirst': '後継 B1 の結果で決める（S07-P03）'}
o = open(W + '/flags50.tsv', 'w', encoding='utf-8', newline='\n')
o.write('名\t表\t既定(decb50fa5)\tCoef行(decb50fa5)\tSimCore\tApp\tRunner\tTests\tscripts\t紙の推奨(9b18137a)\t(1084) の推奨\t区分\t束・順\t支配の力との重なり\t理由・読み手の行\n')
for n in sorted(rd):
    r = rd[n]; p = paper[n]
    dflt = r[1].split('=')[1].split(';')[0].strip() if '=' in r[1] else '?'
    if n in K:
        rec, cls, grp, why = K[n]; ov = '—'
    elif n in IGN:
        rec, cls, grp, why = '点火の線', '—', f'順 {IGN[n][0]}', p[9][:120]; ov = IGN[n][1]
    elif n in FOLD:
        rec, cls, grp, why = '支配の力に畳む', '—', FOLD[n], p[9][:120]; ov = '畳む（設計が名指し）'
    elif n in KEEP:
        rec, cls, grp, why = '残す（答え待ち）', '—', '—', KEEP[n]; ov = '—'
    else:
        rec, cls, grp, why = '?', '?', '?', '?'; ov = '?'
    o.write('\t'.join([n, p[0], dflt, r[2], r[3], r[4], r[5], r[6], r[7], p[8], rec, cls, grp, ov, why]) + '\n')
o.close()
import collections
c = collections.Counter(l.split('\t')[10] for l in open(W + '/flags50.tsv', encoding='utf-8').read().split('\n')[1:] if l.strip())
print(dict(c))
