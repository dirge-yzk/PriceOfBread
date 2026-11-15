# (1155) S27 の台帳の行の書き直しの案（.tsv・12 列）を作る（読むだけ・行の 1〜4 列と持ち主は台帳のまま・錨は取り直す）
# 使い方: mk_ledger.py <ledger_rows_tab.txt> <out_anchors.txt> <出力.tsv>
import sys, re
ROWS, ANC, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
HEAD = '行\t系統\t機能\t期待する動き（出典）\t錨\t持ち主のファイル\t判定の型\t最後に確かめた\t判定\t根拠\t残りタスク\t直す／消す／残す'
COMMIT = '122cdce11'
F = '頻度は 10 種あたり M／N1／N2（S11）＝keep_1150 の base_ins・n1・n2（a11）・out_freq_*.txt'
E = '〔印〕支配の力の段 E（州の行き先＝最大の区分）'
FF = '〔印〕支配の力の段 F（戦争＝力の投射）'
G = '〔印〕支配の力の段 G（属国の読み手を連続量へ）'
C = '〔印〕支配の力の段 C′（目標の切り替え＋届き）'
R3 = '〔印〕宗教と移民の段 3（分離の圧力の宗教の項）'
DEEP = '—（深く読まない）'
# 錨の差し替え（自動の取り直し out_anchors.txt の NG→ より優先）
AOVR = {
 'S27-H007': 'Politics.cs:975:Dominance;MultiNationSim.cs:276:PendingSecessionWars',
 'S27-H008': 'MultiNationSim.cs:294:SpawnNation;MultiNationSim.cs:333:TransferRegion',
 'S27-H010': 'MultiNationSim.cs:156:OpeningColonyDominanceFull;Coefficients.p08_audit_soldiers.cs:112:OpeningColonyDominanceFull',
 'S27-H011': '—（消した＝(956) c85hs・SimCore の grep 0）',
 'S27-H012': 'Politics.cs:956:tUngov;Politics.cs:962:DominanceSpeed',
 'S27-H013': 'Fiscal.cs:421:Rule;Politics.cs:1118:Rule',
 'S27-H024': 'MultiNationSim.cs:285:DeclareCivilWar',
 'S27-H026': 'MultiNationSim.cs:283:SpawnNation',
 'S27-H027': 'Territory.cs:169:Seceded;Territory.cs:179:Sovereign',
 'S27-H028': 'MultiNationSim.cs:385:Discovered',
 'S27-H029': 'MultiNationSim.cs:347:SecessionNames;SecessionNames.cs:26:Resolve',
 'S27-H031': 'MultiNationSim.cs:945:RumpReabsorbYears;MultiNationSim.cs:30:RumpGdpFractionOfParent;Diplomacy.cs:2551:RumpParentName',
 'S27-H032': '—（武力の解錠の器は消した＝(551) gc1・ForceUnlock の grep 0）',
 'S27-H036': 'World.cs:1871:RequiresQuinine;Coefficients.p02_borrow_polity_epidemic.cs:78:EpidemicSeverity',
 'S27-H038': 'Diplomacy.cs:2023:AcquireColony',
 'S27-H039': 'Diplomacy.cs:2173:ColonizeRivalTension;Coefficients.p06_rank_discovery.cs:25:ColonizeRivalTension',
 'S27-H040': 'World.cs:1938:AddProximityTension;World.cs:1956:EraTensionRange;Coefficients.p04_trade.cs:155:ProximityTensionMax',
 'S27-H052': 'Formations.cs:274:Check;CultureRealms.cs:9:Table',
 'S27-H053': 'Formations.cs:302:Qualifies',
 'S27-H054': 'Formations.cs:282:FormationTier',
 'S27-H055': 'Diplomacy.cs:472:RenameNation',
 'S27-H056': 'Formations.cs:295:SetCapital',
 'S27-H057': '—（消した＝(1081) c103dm）',
 'S27-H058': 'Diplomacy.cs:1685:UnificationCandidate;Formations.cs:335:UnificationV1;Coefficients.p09_kingship_tax.cs:70:UnificationNatioShare',
 'S27-P01': 'Instruments.cs:1243:CedeBefore;Territory.cs:237:CedeRuleToLocal',
 'S27-P02': 'Diplomacy.cs:2750:AddForeignShare',
 'S27-P03': 'Diplomacy.cs:466:Sovereign',
 'S27-P04': 'MultiNationSim.cs:329:parentPop',
 'S27-P05': 'Territory.cs:159:PrevStock',
 'S27-V01': 'Diplomacy.cs:466:Sovereign;MultiNationSim.cs:945:RumpReabsorbYears;Diplomacy.cs:2956:Ceded',
}
SEC = '分離の門（支配度が 0）が開いた州×四半期 go 1／0／6（種 7・—・種 2 と 5）＝表 secession の goFlag・成績表の〈分離独立の建国〉4／4／9（5）・分離の戦争の入口 secession 1／0／6（4）'
V = {  # 行 → (判定の型, 判定, 根拠, 残りタスク, 処置)
'S27-H001': ('頻度', '一部だけ働く', f'{SEC}。支配度=1 の州×四半期 0.987（M・N1・N2 とも・表 rule 134 万行／種 10）＝ストックは積分されるが、底へ届くのは 30 種で 7 件', E, '消す（段 E の中で）・Dominance の積分は設計 §7 の〈消す（E）〉'),
'S27-H002': ('頻度', '一部だけ働く', '圧＞閾値 1.43 の州×四半期 M 123（植民地格 87・中核 15・編入 21）／N1 149／N2 267。参政権の割引の列＝1−州の中核の割合（Politics.cs:893 と :1042 RegionAvgSuffrageInclusion＝MigrantNativeSuffrage が Granted でなければ AvgRegionCoreShare）＝文化距離の列と全行一致（M 119 万行で差 0・N1・N2 も 0）＝民族の度合いが関税の項の (1＋文化距離)・民族の加算（(1−中核)²）・参政権の割引で 3〜4 重に掛かる（思想⑨）。中央の圧 植民地格 0.055・中核 0.0009（out_secparts_M.txt）', R3 + '・欠損 K1（下の素案）', '直す（K1・圧力の関数は段 E の後も F_R として残る＝畳まれない）'),
'S27-H003': ('効き', '働く', '投射後÷前の保持の中央 植民地格 0.572・中核 0.932・編入 0.908（M。N1・N2 も ±0.01）。植民地格の余り＜0 の 2,410 州×四半期のうち 1,406（58%）は投射が無ければ余り≥0（N1 1,401／2,396・N2 1,899／2,584）', C + '〜' + E, DEEP),
'S27-H004': ('頻度', '一部だけ働く', '軍が割れる（sharedDiv＞1）国×四半期 18／16／53（111,496 中・0.02〜0.05%）・圧＞閾値の州が 1 以上の国×四半期 354／391／613＝分母が割れるのは閾値を超えた州が 2 以上のときだけ', E, DEEP),
'S27-H005': ('効き', '働く', '文化距離＞0 の州×四半期 植民地格 100%・中核 98.7%（M）＝CultureDistanceFromEthnic 1.0 で 1−中核の割合へ寄せ切る（Politics.cs:883）。H002 の 4 重の 1 つ', R3, '残す（K1 で重なりだけ直す）'),
'S27-H006': ('効き', '働く', '国民意識の割引の中央 植民地格 0.934・中核 0.914・編入 0.863（M）＝効きは 1 割弱', R3, '残す'),
'S27-H007': ('頻度', '一部だけ働く', SEC + '。クールダウン無し（Politics.cs:975 の go は同じ四半期に複数可）は現物どおり', E, DEEP),
'S27-H008': ('頻度', '働く', '建国（cederule の Seceded）125／118／126 行＝蜂起の建国（内戦の入口 civil 124／118／120）が大半・分離の建国は H007。保存は両建て（MultiNationSim.cs:333 TransferRegion）', '—', '残す（設計 §2-2〈残す〉）'),
'S27-H009': ('頻度', '一部だけ働く', 'H001 と同じ（支配度=1 が 0.987・底へ届くのは 30 種で 7 件）。読み手は Politics.cs:930（分離の時計）と :975（go）', E, DEEP),
'S27-H010': ('効き', '働く', '四半期 0 の植民地格 370 州の支配度の中央 1.0000（M・N1・N2）＝OpeningColonyDominanceFull 1（(961)）', E, DEEP),
'S27-H011': ('読まれない欄', '働かない（消した）', 'HeldSince の SimCore の grep 0（(956) c85hs・82ff1811）。文書 L6061 は古い', '—', '—（文書の見出しを直す）'),
'S27-H012': ('効き', '働く', 'RuleSharesEnabled は焼き込み（SimCore の grep 0）・4 区分は毎四半期 Politics.cs:956〜:963 で寄る。中核の州（国×州 2,345）の国家の取り分の幅（最大−最小）の中央 0.136・幅＜0.02 が 22.1%（N1 22.0%・N2 22.6%）＝観戦 M15〈150 年同じ〉は 2 割の州で当たる', C, DEEP),
'S27-H013': ('効き', '働く', 'RuleSharesReach・RuleSharesTaxBase は焼き込み（SimCore の grep 0）。課税の基盤（Fiscal.cs:421）と公共財の届き（Politics.cs:1092・:1118）が Rule を読む。文書の〈休眠〉は古い', '—', '残す（設計 §7〈RuleDelivery を残す〉）'),
'S27-H014': ('頻度', '働く', 'ForeignRuleFromTreaty は 0.5（点火済み・文書の〈休眠〉は古い）。条約港が効く州×四半期 28,078／27,650／20,093・外国の取り分＞0 の州×四半期 32,603／32,957／31,092', FF, DEEP),
'S27-H015': ('届かない門', '休眠', 'ProjectionRuleMin 1.1（届かない値）・ProjectionUsesLand 0。読み手 SimCore 各 2（readers_S27.txt）', C, '消す（設計 §7〈消す（C′）〉・線 c113cr の段の中で）'),
'S27-H016': ('読まれない欄', '休眠', 'PeacetimeConscriptGrievance 0（Military.cs:160・植民地格か在地＞国家の州の平時の徴募）。00一覧 :1820 の N3 は〈採用・修正する（作者裁定）・既定 0 で厳密恒等〉＝点火の線は無い', '—', '直す候補 K3（点火か、倍率を持たない形へ・作者の問い）'),
'S27-H017': ('—', '未確かめ', 'App の画面（SimCore の行は無い）＝この線は App に触れない', '—', '残す'),
'S27-H018': ('頻度', '一部だけ働く', 'H002 と同じ（圧＞閾値 123／149／267 州×四半期・民族の 4 重）', R3 + '・K1', '直す（K1）'),
'S27-H019': ('効き', '働く', 'H005 と同じ', R3, '残す'),
'S27-H020': ('効き', '一部だけ働く', '民族の加算＞0 の州×四半期 植民地格 100%・中核 95.6%。項は SecessionCultureAdditive×MinorityMilitancy×文化距離×(1−中核)×闘争度（Politics.cs:887）＝(1−中核)² の上に参政権の割引 (1−中核) が掛かる＝倍率の一掃の取りまとめ :207 が〈A〉と名指し', R3 + '・K1', '直す（K1）'),
'S27-H021': ('効き', '働く', 'H006 と同じ', R3, '残す'),
'S27-H022': ('効き', '働く', 'H003 と同じ', C + '〜' + E, DEEP),
'S27-H023': ('頻度', '一部だけ働く', 'H004 と同じ', E, DEEP),
'S27-H024': ('頻度', '一部だけ働く', 'H007 と同じ（分離の戦争の入口 1／0／6（4））', E, DEEP),
'S27-H025': ('効き', '働く', '内戦の決着の再併合（cederule の Returned）121／114／117＝蜂起の建国のほぼ全て（V01）。白紙＝分断（成績表の〈内戦分裂〉3／3／6（4））', FF, DEEP),
'S27-H026': ('頻度', '働く', 'H008 と同じ（開戦の四半期に建国＝Seceded の行）', '—', '残す'),
'S27-H027': ('効き', '働く', 'ソース：Seceded は中核の割合を 1−x へ（Territory.cs:169〜:172）・独立の確定は格を中核・支配度 1.0（:179〜:181）。頻度：建国 125／118／126 行の全て', '—', '残す'),
'S27-H028': ('効き', '働く', 'ソース：発見の配列の写し（MultiNationSim.cs:385）・法の継承（:352〜:372）', '—', '残す'),
'S27-H029': ('効き', '働く', 'ソース：SecessionNames.Resolve（分離）と CivilWarName（蜂起）＝nation の表に〈南アフリカ共和国〉〈立憲王政ロシア〉ほか', '—', '残す'),
'S27-H030': ('効き', '未確かめ', 'ソース：RegisterIndependence が緊張を PostWarTension 38 へ（Diplomacy.cs:458）。独立の確定の件数は計器に無い（S27-P03 と同じ限界）', '—', '残す'),
'S27-H031': ('頻度', '一部だけ働く', '残骸の平和的復帰（Returned のうち対の内戦の終わり＋4 年より後）0／0／3（30 種で 3）。対象は白紙の内戦（分断）だけ（Diplomacy.cs:2551 で RumpParentName）＝成績表の〈内戦分裂〉3／3／6。判定の 1% は隠れた定数（MultiNationSim.cs:30 RumpGdpFractionOfParent）', E + '（P1〈戻す〉が畳む）', '残す（段 E の後に読み直す）・1% の定数を係数表へ（恒等の小物 D-r1）'),
'S27-H032': ('読まれない欄', '働かない（消した）', 'ForceUnlock の grep 0（(551) gc1）。独立の確定の後に戻る道は戦争の割譲だけ（V01）', '—', '—（文書の見出しを直す）'),
'S27-H033': ('頻度', '働く', '侵略の行 1,385／1,288／1,407（1,249）・屈服 623／625／605（600）・標的が国 859／793／845（805）', FF + '・' + G, DEEP),
'S27-H034': ('頻度', '働く', 'H033 と同じ。ColonizeWillSuperiorityDiscount は倍率の一掃の取りまとめ :115 で〈A〉（11-09 で膨張意欲を消すと決まった）', FF + '・11-09 の屈服の 1 本化', DEEP),
'S27-H035': ('効き', '未確かめ', '渡海の門（船・制海権・糧食）の通った・止まったの列が invade の表に無い＝0 と数えない', FF, DEEP),
'S27-H036': ('効き', '働く', 'ソース：熱帯の標的は買えたキニーネの割合 fill で防護し、残りが EpidemicSeverity で死ぬ（World.cs:1871〜:1881・戻り値を使う＝植民地の毎月の D2〔S26-21〕とは別）。頻度は計器に無い（未確かめ）。錨 Coefficients.cs:604 は疫病の行＝取り直した', '—', '残す（線 c113qn の D2 は植民地の毎月の買いだけ）'),
'S27-H037': ('頻度', '働く', '屈服 623／625／605（600）・残りが戦争。ColonizeAnnexMilitancy は設計 §7〈消す（F）〉', FF, DEEP),
'S27-H038': ('効き', '働く', 'S26 の確かめ（接収 120〜142 件が植民地格の州へ・(1148)）', FF, DEEP),
'S27-H039': ('効き', '未確かめ', 'ソース：Diplomacy.cs:2173 AddTension(ColonizeRivalTension 22)。足した量の列は計器に無い', FF, DEEP),
'S27-H040': ('頻度', '働く', '侵略で近接の緊張を足した行 1,385／1,288／1,407（1,249）＝全ての侵略。距離の範囲に EraFactor（常に 1.0・Coefficients.p10_ai_silver_overrides.cs:65）×EraTensionRange（World.cs:1956）＝効かない項', '—', '残す・EraTensionRange の項は消す候補 D-r2（恒等）'),
'S27-H041': ('頻度', '働く', 'H033 と同じ。試行は乱数 AiColonizeChance 0.12（Diplomacy.cs:1771）', FF, DEEP),
'S27-H042': ('効き', '未確かめ', '割譲の渡海の列は cess の表（基線の 10 種）にある＝この線では読んでいない', FF, DEEP),
'S27-H043': ('頻度', '働く', '割譲（Ceded）1,235／1,422／1,226 のうち植民地格→植民地格 902（M）', FF, DEEP),
'S27-H044': ('読まれない欄', '働かない（消した）', 'ColonyDevCostFromRegion は 2026-10-18 に削除（文書 L6258 の注）', '—', '—（文書の見出しを直す）'),
'S27-H045': ('効き', '未確かめ', '射程の門は焼き込み（ColonyCaptureReachGate を削除）・ColonyCaptureDistanceWeight の読み手 SimCore 8', FF, DEEP),
'S27-H046': ('効き', '働く', 'ソース：Diplomacy.cs:2651（ConquestColonyMilitancy 1.5）。設計 §7〈消す（F）〉の 1 回の上乗せの不満', FF, DEEP),
'S27-H047': ('読まれない欄', '働かない（消した）', 'ConquestForeignWeight は 2026-09-26 (55) に削除（文書の打消し線）', '—', '—'),
'S27-H048': ('効き', '未確かめ', '講和で狙う土地の列は計器に無い', FF, DEEP),
'S27-H049': ('頻度', '働く', '条約港が効く州×四半期 28,078／27,650／20,093（H014）', FF, DEEP),
'S27-H050': ('効き', '未確かめ', '批准の拒否の列は計器に無い', FF, DEEP),
'S27-H051': ('効き', '働く', 'world.json の従属 3 件（MultiNationSim.cs:237 SetDependency）', G, DEEP),
'S27-H052': ('頻度', '働く', '成績表〈変名（統一/formable）〉22／26／35（19）＝試合の終わりに FormationTier＞0 の国の数（SelfPlay.cs:1518）。どの名かは計器が書かない（nation の名の列は BaseName＝Instruments.cs:285・286）。名の表は Formations.cs 40 行＋CultureRealms.cs 5 文化（名指しのデータ）', '—', '残す（作者の問いの材料：名は表示・遷都は挙動）'),
'S27-H053': ('効き', '働く', 'ソース：Qualifies（Formations.cs:302）・Owns は属国の州も数える（:376〜:380 DependentsOf）', G + '（Owns の属国の読み手）', '残す'),
'S27-H054': ('効き', '働く', 'ソース：rule.Tier ≦ FormationTier を飛ばす（Formations.cs:282）', '—', '残す'),
'S27-H055': ('効き', '働く', 'ソース：RenameNation が同名を拒む（Diplomacy.cs:472〜:475）', '—', '残す'),
'S27-H056': ('効き', '働く', 'ソース：w.SetCapital（Formations.cs:295・FormationCapitalMove は焼き込み）', '—', '残す'),
'S27-H057': ('読まれない欄', '働かない（消した＝(1081)）', 'FormationLegitimacyBoost の SimCore の grep 0・DormantIdentityTests の RetiredFields が戻りを赤にする', '—', '—（文書の見出しを直す）'),
'S27-H058': ('頻度', '一部だけ働く', '統一の戦争（wars の entryCode declare＝DeclareWar の既定の印・Diplomacy.cs:1568）2／2／1（1）。UnificationV1Only 1＝名指しの 3 件だけ（Formations.cs:335）・見送り 20 年と宣戦の間隔 3 年（外部レビュー項目 20 の A1・A2〈根拠のない待ち〉）・AI だけ 30% の見送りの乱数（倍率の一掃 :304）。App の読み手 15 行（UnificationRule・DeclareUnificationWarOn・CanUnificationWar）', '—', '消す候補 D-r3（作者「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」・置き換えは ver2.0 の後）'),
'S27-P01': ('両側の門', '一部だけ働く（期待が古い）', '割譲の行で前と後の 4 区分が変わる（identBreak）1,199／1,235（M・N1 1,375・N2 1,191）＝(915) の CedeRuleToLocal 1（Territory.cs:237）が割譲の 1 回だけ国家の分を在地へ＝設計どおり。Returned・Seceded の 246 行は 0。movedAgain 4／2／2', FF + '（C1 を消す）', '残す（期待を〈割譲の 1 回だけ国家→在地・返還と建国は不変〉へ書き直す）'),
'S27-P02': ('届かない門', '働かない', '受け手の鍵の持分＞0 の割譲 0／0／0・第三国の持分あり 21／32／24（cederule）', FF, DEEP),
'S27-P03': ('読まれない欄', '未確かめ', '独立の確定（Territory.Rehome Sovereign・Diplomacy.cs:466）は TransferRegion を通らない＝cederule に無い（計器の限界・(888) と同じ）', '—', '計器の仕様 I1（下）'),
'S27-P04': ('効き', '働く', 'ソース：国庫×建国の人口÷親の人口（MultiNationSim.cs:329）。建国 125／118／126 行の全てで通る', '—', '残す'),
'S27-P05': ('効き', '働く', 'ソース：Rehome が独立の確定以外で市場の前の在庫・産出・売り・輸入を置き直す（Territory.cs:157〜:159）', '—', '残す'),
'S27-V01': ('頻度', '一部だけ働く', '本国の中核・編入の州を割譲で失った 360／481／402・同じ国へ戻った 29／39／29（7〜8%・戻るまでの中央 27／41／20 年・全て次の戦争の割譲）。蜂起の建国で失った 111／110／111 は 108／106／104 が中央 1 年で戻る（内戦の鎮定）。残骸の平和的復帰 0／0／3。英は割譲で 43／25／38 を失い 25／14／22 が戻らない（観戦 U2）・仏 46／45／66（31／41／45）。独立の確定（P03）の州は計器に無い', E + '（P1〈元の持ち主の取り分が最大に戻れば戻す〉）', '残す（段 E の門の基線に写す）'),
}
anc = {}
for l in open(ANC, encoding='utf-8'):
    if '\t' not in l: continue
    rid, rest = l.rstrip('\n').split('\t', 1)
    parts = []
    for a in rest.split(' ; '):
        if '=OK' in a: parts.append(a.split('=OK')[0])
        elif '=NG→' in a:
            src, dst = a.split('=NG→'); parts.append(dst if dst != 'None' else src + '（引けない）')
        elif '=錨なし' in a: parts.append(a.split('=錨なし')[0])
    anc[rid] = ';'.join(p.strip() for p in parts)
out = [HEAD]
miss = []
for l in open(ROWS, encoding='utf-8'):
    c = [x.strip() for x in l.rstrip('\n').split('\t', 1)[1].split('|')]
    rid = c[1]
    if rid not in V: miss.append(rid); continue
    t, j, why, rest, act = V[rid]
    a = AOVR.get(rid, anc.get(rid, c[5]))
    why = why + '。' + F if any(k in t for k in ('頻度',)) else why
    out.append('\t'.join([rid, c[2], c[3], c[4], a, c[6], t, COMMIT, j, why, rest, act]))
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
print(f'行 {len(out)-1}・判定なし {miss}')
from collections import Counter
print(' '.join(f'{k}:{v}' for k, v in Counter(V[r][1] for r in V).most_common()))
print(' '.join(f'{k}:{v}' for k, v in Counter(V[r][0] for r in V).most_common()))
