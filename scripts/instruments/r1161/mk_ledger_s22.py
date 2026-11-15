# (1161) S22 の台帳の行の書き直しの案（.tsv・12 列）を作る（読むだけ・行の 1〜4 列と持ち主は台帳のまま・錨は取り直す）
# 使い方: mk_ledger_s22.py <ledger_rows_tab.txt> <out_anchors.txt> <出力.tsv>
import sys
ROWS, ANC, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
HEAD = '行\t系統\t機能\t期待する動き（出典）\t錨\t持ち主のファイル\t判定の型\t最後に確かめた\t判定\t根拠\t残りタスク\t直す／消す／残す'
COMMIT = 'e621e5ea3'
F = '頻度は 10 種の和 M／N1／N2（全表の幅 N1f／N2f＝keep_1150 の n1・n2）＝keep_1157 の base_ins・n1・n2・out_freq_*.txt'
B1 = '〔印〕B1-r（造幣の床の置き場・md の 4 節）'
CF = '〔印〕数値の天井と床の一覧'
MA = '〔印〕倍率の一掃（分類の取りまとめの区分）'
SV = '〔印〕マルコフ性の洗い出し（根拠のないクールダウン）'
DEEP = '—（深く読まない）'
DISP = '〔印〕表示の行（画面の点検の線・App は読むだけ）'
AOVR = {
 'S22-H001': 'SelfPlay.cs:593:GoldWishFromBoard;SelfPlay.cs:195:GoldLiteracyLine;PolicyPreferences.cs:306:MonetaryPeerPullScale;Government.cs:35:MonetaryStandard',
 'S22-H002': 'Fiscal.cs:214:GoldSuspendMinFundingRatio;Coefficients.p03_debt_money.cs:111:GoldCoverMin;Coefficients.p03_debt_money.cs:112:GoldStandardMinFunding',
 'S22-H003': 'Fiscal.cs:226:GoldRejoinCoverMargin;Coefficients.p07_wiring_prewarm.cs:12:GoldRejoinCoverMargin',
 'S22-H006': 'Simulation.cs:2396:MintGold;Simulation.cs:2442:MintFloorBuyRatePerMonth;Coefficients.p10_ai_silver_overrides.cs:51:MintFloorBuyRatePerMonth;Coefficients.p10_ai_silver_overrides.cs:68:MintReserveTargetWeight',
 'S22-H007': 'Simulation.cs:2415:MintBuysAtMarket;Coefficients.p10_ai_silver_overrides.cs:56:MintBuysAtMarket',
 'S22-H008': 'Simulation.cs:2462:LastDemand',
 'S22-H009': 'Simulation.cs:2473:SellDemonetizedReserves;Simulation.cs:2486:SellReserve;Coefficients.p10_ai_silver_overrides.cs:52:MintReserveSellRatePerMonth',
 'S22-H010': 'Simulation.cs:2449:impShare;Simulation.cs:2452:PaySales',
 'S22-H016': 'Credit.cs:15:bank.Gold',
 'S22-H017': 'Credit.cs:14:hoardRatio;Credit.cs:6:HoardRatioMax;Coefficients.p08_audit_soldiers.cs:55:CurrencyHoldingRatio;Coefficients.p08_audit_soldiers.cs:56:HoardPanicWeight',
 'S22-H018': 'Credit.cs:41:CreditMultiplier',
 'S22-H023': 'Credit.cs:42:GoldCoverCreditRestraint;Credit.cs:7:GoldCoverCreditFloor',
 'S22-H025': 'Credit.cs:5:HeatClampMax',
 'S22-H026': 'CapitalMarket.cs:11:CapitalIndTarget;CapitalMarket.cs:74:CapitalContagion;Coefficients.p03_debt_money.cs:141:CapitalCreditFloor',
 'S22-H031': 'Invariants.cs:11:MoneyConservation',
 'S22-H040': '—（錨 InstrumentsCornint.cs:7 は穀物法の計器の見出し＝取り違え）',
 'S22-H043': '—（CoercionUpkeepRate の grep 0＝(135) で消した）',
 'S22-H044': '—（BondRateDebtQuad の grep 0＝(96) で消した）',
 'S22-H047': '—（CreditDemandDriven の grep 0＝(82) で消した）',
 'S22-H049': '—（FiscalQuarterlyGating の grep 0）',
 'S22-H050': '—（MintFloorRespectsReserve の SimCore の grep 0＝(210) で器ごと消した）',
 'S22-H052': 'MultiNationSim.cs:744:SilverParityFromRatio;Coefficients.p10_ai_silver_overrides.cs:54:SilverParityFromRatio',
 'S22-H053': 'Simulation.cs:693:SilverRushEnabled;Coefficients.p04_trade.cs:109:SilverRushEnabled',
}
V = {
'S22-H001': ('頻度', '働く（期待の文が古い）', '銀→金 226／222／212（N1f 218・N2f 206）・金になった国 212／215／208・最初の年の中央 1870.5／1872／1872・1900 年の国の数 銀 40.5・金 21.1（M）。AI の金の志向は識字 0.75 の線（SelfPlay.cs:195 GoldLiteracyLine・隠れた定数）＝文書の〈工業化の閾 3.5〉は古い', '〔印〕倍率の一掃（隠れた定数 GoldLiteracyLine＝md の 3 節 D-s5）', '残す（文書の見出しを直す）'),
'S22-H002': ('頻度', '働く', '兌換の強制離脱（fiscal の onGold 1→0・計器の 9 国×月）43／45／43（N1f 51・N2f 44）・法の金→不換 8／5／0', '—', '残す'),
'S22-H003': ('頻度', '働く', '兌換への復帰（onGold 0→1）39／44／42（N1f 48・N2f 41）・金属本位の月の割合 0.993／0.993／0.994', '—', '残す'),
'S22-H004': ('頻度', '働く', '兌換の連続量が 0.05〜0.95 の国×月 0.086／0.092／0.094・1 か月に 0.3 超の落ち 36／39／37', '—', '残す'),
'S22-H005': ('頻度', '働く（銀と金の別は未確かめ）', '兌換中の切り下げの月 4,658／5,806／3,980（切り下げの月 4,980／5,930／4,126 のうち）。fiscal に本位の列が無い＝銀だけに窓が開くかは計器が書かない', '—', '残す'),
'S22-H006': ('頻度', '一部だけ働く', '造幣の累計が増えた国×四半期 0.933（M）・N1f 0.919・N2f 0.931。買いは〈その州の在庫×0.25／月・平価以下の月だけ〉（Simulation.cs:2439〜:2442）＝在庫の無い州は買わない＝輸入の需要を作らない（B1-r）', B1 + '・' + CF + '（MintFloorBuyRatePerMonth＝率の床）', '直す（B1-r の形・md の 4-5）'),
'S22-H007': ('効き', '働く（頻度は未確かめ）', 'ソース：準備の不足の月だけ市場の値段で買う（Simulation.cs:2415〜:2416・:2431）。atMarket の月の数は計器が書かない', B1, DEEP),
'S22-H008': ('効き', '働く', 'ソース：引き取りを州の LastDemand へ足す（Simulation.cs:2462・MintDemandCounted は焼き込み済み＝係数の grep 0）。この量が銀の鉱山の労働の重みの〈売れる量〉（EconomyStep.cs:852 sellR）＝輸出は LastExport で sellR に入らない（BilateralMarket.cs:1006・B10）', B1, '残す'),
'S22-H009': ('頻度', '働く', '引き金＝銀から金・不換へ 256／258／257（法の替わり）。売りは首都の州の市場だけ（Simulation.cs:2478 World.Region）＝買った州でなく首都へ吐く', B1, '残す'),
'S22-H010': ('効き', '働く', 'ソース：在庫の輸入の分は商人へ・残りは④の分け方（Simulation.cs:2448〜:2454）。pay の mintEmptyN・mintUnmovedN は M で 0（受け手が無くて動かない額 0）', '—', '残す'),
'S22-H011': ('効き', '働く（頻度は未確かめ）', 'ソース：Government.cs:176 の coverTarget へ政策金利が応答（TrilemmaRateResponse）。金利の列が計器に無い', '—', DEEP),
'S22-H012': ('頻度', '一部だけ働く', '資本移動の法の替わり 146／87／127・既定でない段の国×四半期 0.0054／0.0035／0.0071（全ての国）', '—', '残す'),
'S22-H013': ('頻度', '働く', '物価水準が床 0.4 に当たった行（pclamp の floor）444／436（8 種）／470（6 種）・N1f 484・N2f 499（pclamp は当たった種だけ書く＝無い種は 0）・天井 11／30／32', CF + '（PriceLevelFloor＝分母の守り・B）', '残す'),
'S22-H014': ('頻度', '一部だけ働く', '為替の天井 3.0・床 0.3 に当たった国×四半期 0／—／—（M）・N1f 0・N2f 0（silverpar の eAtCeil・eAtFloor）＝30 種で一度も当たらない。偏りの幅の角は未確かめ', CF + '（ExchangeRateCeil＝A・ExchangeRateFloor＝B）', '消す候補（区分 II・md の 3 節 D-s3）'),
'S22-H015': ('効き', '働く', 'ソース：CapitalMarket.cs:47〜:66。債務比が直の項と国債の利回りで 2 度読まれる（倍率の一掃 §2-2 DebtDistressRatio＝A）', MA + '（DebtDistressRatio＝A）', DEEP),
'S22-H016': ('効き', '働く', 'ソース：準備＝基礎貨幣×(1−退蔵率)（Credit.cs:15）', '—', '残す'),
'S22-H017': ('効き', '働く（天井は届かない）', '退蔵率＝0.1×(1＋1×(1−信頼))≦0.2（信頼は Clamp01）＝上限 0.9（Credit.cs:6 HoardRatioMax）は今の係数で届かない〈ソース〉', CF + '（HoardRatioMax・md の 3 節）・' + MA + '（HoardPanicWeight＝問い6 B）', '消す候補（恒等・md の 3 節 D-s1）'),
'S22-H018': ('効き', '一部だけ働く（期待の文が古い）', 'ソース：与信の上限＝準備×(CreditMultiplier−1)（Credit.cs:41）＝発展度で尺度を変える項は無い＝見出しの〈発展度スケール〉は古い', '—', '残す（文書の見出しを直す）'),
'S22-H019': ('効き', '働く（頻度は未確かめ）', 'ソース：Credit.cs:28〜:37（崩れは 0.20・育ちは 0.02 の非対称）。信頼の列が計器に無い', MA + '（PanicSensitivity・ConfidencePivot・DrawdownPanic＝A §2-2）', DEEP),
'S22-H020': ('効き', '働く（頻度は未確かめ）', 'ソース：Credit.cs:28〜:32（財政の不履行・負債デフレ・国債の評価損・過熱）', MA + '（FiscalDefaultPanic・DebtDeflationWeight＝A）', DEEP),
'S22-H021': ('頻度', '働く', '過熱度＞1.0 の国×四半期 86／—／—（M 14,471 のうち 0.0059）・N1f 86・N2f 69', MA + '（OverheatFragility＝問い6 B）', DEEP),
'S22-H022': ('頻度', '働く', '最後の貸し手の門（過熱度＞0.9）117／—／—（M 0.0081）・N1f 120・N2f 102。取り付けは BankRunMax 0.5 で切る（Credit.cs の run）', MA + '（BankRunSensitivity＝A・LolrDrawdownTrigger＝A）・' + CF, DEEP),
'S22-H023': ('効き', '働く（床は届かない〈推〉）', '絞りは被覆が 0.25〜0.375 の間だけ。金本位の月は被覆 0.25 未満で強制離脱（Fiscal.cs:214）＝被覆÷0.375≧0.667＞床 0.25（Credit.cs:7 GoldCoverCreditFloor）〈ソース・月の中の順は未確かめ〉', CF + '（GoldCoverCreditFloor・md の 3 節）', '消す候補（ident で測る・md の 3 節 D-s2）'),
'S22-H024': ('効き', '働く', 'ソース：Credit.cs:70 の歩調 CreditAdjustSpeed', '—', '残す'),
'S22-H025': ('頻度', '働く', '過熱度≧1.49（上限 1.5＝Credit.cs:5 HeatClampMax）12／—／—（M 0.0008）・N1f 11・N2f 12', CF + '（Credit.cs:10 の 1.5）', DEEP),
'S22-H026': ('効き', '一部だけ働く', '伸びしろ＝CapitalIndTarget−工業化度（CapitalMarket.cs:12・CapitalHeadroomFromReturn 0 のまま＝★(665)③の答え待ち）＝工業化度の読み手が残る', MA + '（CapitalCreditFloor・EliteForFullTech＝問い10）', DEEP),
'S22-H027': ('頻度', '未確かめ', '表 cpwdft・cpworld は有る（M）＝この回は読んでいない', '—', '—'),
'S22-H028': ('効き', '働く（頻度は未確かめ）', 'ソース：累積の切り下げ÷名目 GDP−0.02 で逃げる（CapitalMarket.cs:83）', '—', DEEP),
'S22-H029': ('頻度', '未確かめ', '表 cpwdft の absorbedFlight は有る＝この回は読んでいない', '—', '—'),
'S22-H030': ('効き', '働く', 'ソース：世界の平均の信用で全ての資本の移動を一律に縮める（CapitalMarket.cs:74）', MA + '（CapitalContagion＝A §2-2）', DEEP),
'S22-H031': ('効き', '働く', 'ソース：MoneyConservation（Invariants.cs:11）・leakhunt（合流ごと）', '—', '残す'),
'S22-H032': ('頻度', '働く', '予防の引き締めの域（過熱度＞0.75）223／—／—（M 0.0154）・N1f 237・N2f 209', '—', '残す'),
'S22-H033': ('読まれない欄', '休眠', 'LoanSpreadPerYear 0・PolicyRateLoanWeight 0＝常に 0%（読み手 SimCore 各 1）', '—', '残す（00一覧 :2504 の部分実装の器・DormantIdentityTests:240〜:244）'),
'S22-H034': ('表示', '未確かめ', 'World.cs:653 の ExplainLedger（既定 null）＝表示の行', DISP, '—'),
'S22-H035': ('表示', '未確かめ', '同上', DISP, '—'),
'S22-H036': ('表示', '未確かめ', '同上', DISP, '—'),
'S22-H037': ('表示', '未確かめ', '表示の行（App）', DISP, '—'),
'S22-H038': ('表示', '未確かめ', '表示の行（App）', DISP, '—'),
'S22-H039': ('表示', '一部だけ働く', '貸出金利は器が休眠＝常に 0.0%（H033）', DISP, '—（H033 を消すなら行も畳む）'),
'S22-H040': ('表示', '未確かめ', '錨が取り違え（穀物法の計器）', DISP, '—（錨を直す）'),
'S22-H041': ('表示', '一部だけ働く', '家計債務は器が休眠＝常に 0（H048）', DISP, '—（H048 を消すなら行も畳む）'),
'S22-H042': ('表示', '未確かめ', '表示の行（App）', DISP, '—'),
'S22-H043': ('読まれない欄', '働かない（消した＝(135)）', 'CoercionUpkeepRate の SimCore・Tests の grep 0', '—', '—（文書の見出しを直す）'),
'S22-H044': ('読まれない欄', '働かない（消した＝(96)）', 'BondRateDebtQuad の grep 0', '—', '—（文書の見出しを直す）'),
'S22-H045': ('読まれない欄', '休眠', 'LoanSpreadPerYear 0・PolicyRateLoanWeight 0（Credit.cs の利息の枝は通らない）。読み手 SimCore 1・Tests 7／3', '—', '残す（00一覧 :2504 の部分実装の器）'),
'S22-H046': ('読まれない欄', '休眠', 'InterestCapitalization 0（Credit.cs の capitalized は 0）。読み手 SimCore 1・Tests 3', '—', '残す（DormantIdentityTests:240 の休眠の表）'),
'S22-H047': ('読まれない欄', '働かない（消した＝(82)）', 'CreditDemandDriven の grep 0', '—', '—（文書の見出しを直す）'),
'S22-H048': ('読まれない欄', '休眠', 'PopDebtDriven 既定 0（DormantIdentityTests の休眠の表に有る・EconomyStep.cs:1639・Meritocracy.cs:211・Simulation.cs:2506）', '—', '残す（作者の史実の型〈デカン暴動〉・00一覧の休眠の器）'),
'S22-H049': ('読まれない欄', '働かない（消した）', 'FiscalQuarterlyGating の grep 0＝文書の〈休眠〉は古い', SV, '—（文書の見出しを直す）'),
'S22-H050': ('読まれない欄', '働かない（消した＝(210)）', 'MintFloorRespectsReserve の SimCore の grep 0（Tests 1 は戻りの番人）', B1, '—（文書の見出しを直す）'),
'S22-H051': ('読まれない欄', '休眠', 'MintParitySells 0（Simulation.cs:2422）。係数の注〈閉じる条件が誤り…造幣が一度も買わない〉は (210) で消した跛行本位の理由の写し〈推〉', B1, '残す（本位の両側＝B1-r の材料・DormantIdentityTests:93）'),
'S22-H052': ('効き', '働く（期待の文が古い）', 'SilverParityFromRatio 1.0（波57 c7ag で点火・MultiNationSim.cs:744）＝文書の〈休眠〉は古い', '—', '残す（文書の見出しを直す）'),
'S22-H053': ('読まれない欄', '休眠', 'SilverRushEnabled 0（Simulation.cs:693 で表の銀の行を置かない）。作者 2026-09-07 の器（文書）', B1, '残す（作者の裁定の器）'),
}
exec(open(__file__.replace('mk_ledger_s22.py', '_tail.py'), encoding='utf-8').read())
