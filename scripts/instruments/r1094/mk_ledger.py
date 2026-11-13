# (1094) S14 の台帳の行（12 列）を TSV に書く。数は out_freq.txt・out_drift.txt・out_readers.txt から写した（M の値・括弧は N1／N2／S11 の幅）。
# 使い方: python -X utf8 -I mk_ledger.py <out.tsv>
import sys
T = "23aa9d11a"
H = ["行", "系統", "機能", "期待する動き（出典）", "錨", "持ち主のファイル", "判定の型", "最後に確かめた", "判定", "根拠", "残りタスク", "直す／消す／残す"]
R = []
def row(i, name, doc, anc, own, kind, verdict, basis, todo, act):
    # 残りタスクの列は台帳 §1 の S14 の〈残りタスクの対象（00一覧）〉の値を先に残し、この線の物を後ろへ足す（上書きしない）
    pre = "prdd・rd47 R4（612 行）" if isinstance(i, int) else ""
    todo = pre if todo == "—" and pre else (pre + "／" + todo if pre else todo)
    R.append([f"S14-H{i:03d}" if isinstance(i, int) else i, "S14", name, doc, anc, own, kind, T, verdict, basis, todo, act])

row(1, "政治意識", "(識字×就学の変換×都市＋便乗)×報道へ寄るストック（L2401）", "Politics.cs:250:ConsciousnessSpeed;Politics.cs:246:NewsConsciousnessFloor;Politics.cs:238:UrbanConsciousnessBoost", "Politics.cs", "読み手",
    "働く（列なし＝ソースだけ）", "毎四半期・全 POP（:235〜251）。読み手：投票の重み（Ideology.cs:296）・動員（:535〜536）・法の項（:195）ほか。生に政治意識の列は無い（secession_s の consciousnessMeanIndex は国民意識＝別の量）", "計器 pc_s（意識の国の平均と目標の内訳）", "残す（設計 §13-2 で導く量へ）〔§13-4〕〔倍〕ConsciousnessSpeed＝問い1 B・UrbanConsciousnessBoost＝問い1 A・NewsConsciousnessFloor＝問い2 A")
row(2, "便乗（バンドワゴン）", "前期の運動÷保持力の勢いが識字の低い層を政治化（L2407）", "Politics.cs:224:BandwagonSensitivity;Politics.cs:241:BandwagonWeight", "Politics.cs", "頻度",
    "働く（勢いの飽和 27.6%）", "crownpc_s から勢い＝clamp01(前期の動員÷max(1,保持力)×0.45)〔近似〕：0.01 超 99.2%・1 で飽和 27.6%（27.7〜29.1%）・中央 0.477（0.474〜0.494）。〔結果の後の切り分け・out_mom2〕保持力＜1 の床の行は 0.05%＝飽和の根ではない。飽和は立憲 41.9%・共和 41.6%・絶対 1.6%・専制 0.4%（動員が保持力を超えても引力点が自分の政体＝政変にならない国で飽和）。意識の目標への寄与 0.32×勢い×(1−識字) の 9 国の年末の中央 0.067・90% 点 0.176（識字の中央 0.358）", "設計 §13-2 が消す（記録）＝消すと意識の目標が 9 国の中央で約 0.07 下がる＝P1 の急進の項へ届く", "残す（§13 の段 I で消す）〔§13-4〕〔倍〕問い1 B・正の輪の注（L155）")
row(3, "闘争度の成分", "文書は 5 成分（L2413）＝今は法・民族・厭戦・飢えの 4 項（生活は不満度だけ）", "Politics.cs:312:warWeariness;Politics.cs:317:MilitancySpeed;Politics.cs:306:WarWearinessMilitancy", "Politics.cs", "頻度",
    "働く（文書が古い）", "dcont_s 年末の行 102,539（4 腕とも約 10.2 万）：法の項＞0 100%・民族 90.1%・厭戦 50.7%（50.7〜56.0%）・飢え 39.4%（38.0〜39.4%）。4 項の和に占める中央の割合 法 0.335・民族 0.026・厭戦 0.0001・飢え 0。90% 点 法 0.024・厭戦 0.083・飢え 0.105・民族 0.056＝法の項は常に在るが小さく、大きい闘争度は厭戦・飢え・民族が作る。闘争度の中央 0.047・90% 点 0.228", "文書を 4 項へ直す", "直す（文書）〔倍〕WarWearinessMilitancy＝1-1〈裁定済み・削除〉・MilitancySpeed・PoliticalDistanceWeight＝問い1 B")
row(4, "不満度（導出量）", "生活の失望＋法の隔たり（L2419・文書は〈表示専用〉）", "Politics.cs:157:Discontent;Politics.cs:100:LivingWant;Politics.cs:126:EffectiveVoice", "Politics.cs FactionSnapshot.cs Ideology.cs", "頻度",
    "働く（文書が古い）", "dcont_s：不満度＞0 99.8%・中央 0.020・90% 点 0.117。読み手＝参加の述語（FactionSnapshot.cs:101）・思想の入力（Ideology.cs:56）・画面（AvgMassDiscontent）＝表示専用ではない", "文書の〈表示専用〉を直す", "直す（文書）")
row(5, "参加の述語を不満度で", "max(不満度, 闘争度) で頭数（L2424・文書は〈休眠〉）", "FactionSnapshot.cs:101:ParticipationDiscontent", "FactionSnapshot.cs", "頻度",
    "働く（文書が古い）", "ParticipationDiscontent＝1.0（(431) で点火）。dcont_s：不満度＞闘争度の POP がある行 82.0%（79.8〜82.0%）・新旧の頭数の比 partNewK÷partOldK の中央 29（29〜41）", "文書の〈休眠〉を直す", "直す（文書）")
row(6, "法の隔たりを動かせる法で", "LawGap へ差し替える器（L2429・〈二重の休眠〉）", "FactionSnapshot.cs:88:DiscontentLawGap", "FactionSnapshot.cs PolicyPreferences.cs Politics.cs", "読み手",
    "休眠（一重）", "DiscontentLawGap＝0.0（Coefficients.cs:313）。参加の述語は点火済み＝休眠は 1 重だけ（文書の〈二重〉は古い）。DormantZeroFields に在る（DormantIdentityTests.cs:127）", "—", "残す（休眠の器・差し戻しの記録なし）・文書の〈二重〉を直す")
row(7, "生活の不満は失望から", "参照点からの低下（L2434）", "Politics.cs:320:LivingRefSpeed;Politics.cs:100:LivingWant", "Politics.cs", "頻度",
    "働く", "dcont_s：生活の低下＞0 の行 45.3%（42.9〜45.3%）・中央 −0.008（成長期の負＝支持）・90% 点 0.123。闘争度には入らない（(431)）＝不満度と思想の入力だけ", "—", "—")
row(8, "政治的な疎外（4 軸）", "意識×(1−実効発言権)×4 軸の距離（L2439）", "Politics.cs:185:AxisDistance;Politics.cs:283:PoliticalTerm", "Politics.cs", "頻度",
    "働く", "法の項（milVoice）＞0 100%・中央 0.0090（0.0090〜0.0099）・90% 点 0.024＝常に在るが小さい（和に占める中央 33.5% は他の 3 項が 0 の行が多いため）", "設計 §13-2 ⑤ が好みの距離へ置き換える", "残す（§13 の段 I）〔§13-4〕")
row(9, "3 層分割", "文盲→天命／教育→綱領／中間（L2443）", "PoliticalQuantities.cs:26:MandateBase;Politics.cs:195:Math.Min(p.Consciousness, p.Literacy)", "PoliticalQuantities.cs Politics.cs", "頻度",
    "働く（列なし＝近似）", "MandateBase の列は無い。天命 scalars_s の dynMandate が 0.5 から 0.01 超 動いた年末の行 97.5%（97.5〜98.0%）・10〜90% 点 0.495〜0.862＝文盲の側の道は動く〔近似〕。中間層の項は削除済み", "—", "—")
row(10, "闘争度による手段の上書き", "休眠の器（L2449）", "Politics.cs:544:DesperationMilitancyOverride", "Politics.cs", "読み手",
    "廃止を確かめた（文書が古い）", "(271) で削除・注に名だけ（Politics.cs:544）・コードの読み手 0（out_readers）・墓標の番人 DormantIdentityTests.cs が名を持つ", "文書の〈休眠〉を〈廃止〉へ", "直す（文書）")
row(11, "3 層の振る舞い分け", "休眠の器（L2454）", "Politics.cs:544:DesperationLayerBlend", "Politics.cs", "読み手",
    "廃止を確かめた（文書が古い）", "H010 と同じ (271)・コードの読み手 0", "文書の〈休眠〉を〈廃止〉へ", "直す（文書）")
row(12, "秘密投票と紙の参政権", "議会×秘密 1.0／議会×公開 0.9／非議会 0.7（L2459・S19-39 と同じ題）", "Politics.cs:131:OpenBallotReality;Politics.cs:132:PaperFranchiseReality", "Politics.cs", "頻度",
    "働く（3 つの枝が全て出る）", "monlaw_s（全ての国・毎四半期）の参政権＞なし の行 65.9%。その中で 議会×公開 77.3%（77.3〜78.2%）・議会×秘密 21.3%（20.3〜22.3%）・非議会 1.4%（0.3〜1.4%）。〔結果の後の追記〕閉鎖階級は参政権なしでも声 1（InterestGroups.cs:104）＝全ての行で数えると 非議会（0.7 の枝）35.0%・議会 65.0%（out_drift の政体の行数）。法の段と門は S19-39（二度数えない）", "—", "—〔倍〕2-1 で B が正しい")
row(13, "失業の怒りと失業保険", "廃止（L2465）", "Coefficients.cs:673:UnemploymentMilitancy", "Coefficients.cs", "読み手", "廃止を確かめた", "墓標の注だけ（Coefficients.cs:673・:3167）・コードの読み手 0", "—", "—")
row(14, "格差の不満（ジニ）", "闘争度の項は廃止・ジニは表示（L2470）", "Coefficients.cs:496:InequalityMilitancy;Politics.cs:1864:GiniIndex", "Coefficients.cs Politics.cs", "読み手", "廃止を確かめた（ジニは表示）", "墓標の注（:496）・GiniIndex のエンジンの読み手 0・App 1・Tests 4（out_readers）", "—", "—")
row(15, "民族の不満（一本鎖）", "非中核の割合×MinorityMilitancy（L2475）", "Politics.cs:287:MinorityMilitancy", "Politics.cs", "頻度",
    "働く", "dcont_s：民族の項＞0 90.1%（90.0〜90.3%）・中央 0.0005・90% 点 0.056・和に占める中央 0.026", "—", "—〔倍〕問い1 B")
row(16, "飢えの絶対項", "廃止（L2481）・後継は急な落ち込み", "Coefficients.cs:257:FamineAbsoluteMilitancy;Politics.cs:300:FamineDrive", "Coefficients.cs Politics.cs", "頻度",
    "廃止を確かめた（後継は一部だけ）", "後継 FamineDrive の項＞0 の行 39.4%・中央 0・90% 点 0.105・和に占める中央 0", "—", "残す（後継は急な落ち込みだけを読む設計どおり＝0 の行が多いのは狙いどおり）")
row(17, "11 の思想が経済から", "階級・所得源・富・意識・闘争から（L2485）", "Ideology.cs:16:UpdateShares;Ideology.cs:160:BaseWeights", "Ideology.cs", "頻度",
    "一部だけ働く", "ideology_s 年末の国の割合（中央）：自由 0.395・共和 0.230・無政府 0.107・伝統 0.090・国民 0.077・反動 0.014・王党 0.009・社会≈0（0.01 超 24.1%）・共産≈0（16.9%）・帝国≈0（9.2%）・ファシズム 0（0.13%）。無政府は門なしで中央 0.107＝社会主義が未発見の行では無政府が左の受け皿（90% 点は社会 0.261・無政府 0.156）", "設計 §13-2 ② が好みのベクトルへ畳む", "残す（§13 の段 I）〔§13-4〕")
row(18, "貴族の思想は所得構成から", "地代依存→伝統・商業化→自由帝国・没落→国民（L2489）", "Ideology.cs:175:NoblePrivilegeWeight", "Ideology.cs", "読み手",
    "働く（ソースだけ）", "閉鎖階級の枝（Ideology.cs:170〜182）。階級ごとの思想の列は無い（ideoPopMean は国の平均）", "計器（階級×思想）", "残す（§13 の段 I）〔§13-4〕〔倍〕載らない")
row(19, "王党派", "王権の受益者×意識（L2495）", "Ideology.cs:110:royalSupply", "Ideology.cs", "頻度",
    "一部だけ働く（量が小さい）", "王党の国の割合 0.01 超 42.3%（42.3〜45.3%）・中央 0.0087・90% 点 0.018", "量が小さい＝〈意識→議会支持の一本道を断つ〉の働きは 2% 未満", "残す（§13 の段 I で好みへ）〔§13-4〕")
row(20, "反動を地位の喪失から", "休眠の器（L2500）", "Ideology.cs:68:ReactionFromLoss", "Ideology.cs", "読み手",
    "休眠", "ReactionFromLoss＝0（Coefficients.cs:367）。旧式の反動の国の割合 中央 0.014（0.0139〜0.0141）・90% 点 0.032。DormantZeroFields に在る（:239）・10-18 の精査 ③-2 に〈政6・政5 の後〉の計画", "設計 §13-4 は消す・10-18 の精査は計画＝食い違い（名乗る）", "残す（休眠の器）〔§13-4〕")
row(21, "敗戦・失地・戦勝・外圧→国民主義", "4 つの出来事が国民主義と帝国主義へ（L2505）", "Ideology.cs:31:nationalismSurge;Ideology.cs:32:ExternalPressureNationalism", "Ideology.cs Politics.cs", "頻度",
    "一部だけ働く（外圧は 0）", "scalars_s 年末：敗戦の衝撃＞0 29.3%（29.3〜33.4%）・外圧＞0 は 4 腕の全ての行で 0（MultiNationSim.cs:858〜859 が 0 を書く）。戦勝の衝撃と失地の在庫は列なし", "外圧の項は F2 群4（紙の段の第 2 回）が消す・計器（戦勝・失地）", "直す（外圧の項は F2 群4）〔§13-4〕〔倍〕ExternalPressureNationalism＝問い1 I・他は B")
row(22, "国民意識からの底上げ", "休眠の器（L2510）", "Coefficients.cs:2468:NationalismFromConsciousness", "Coefficients.cs", "読み手", "廃止を確かめた（文書が古い）", "(551) で削除・墓標の注だけ", "文書の〈休眠〉を〈廃止〉へ", "直す（文書）")
row(23, "恐慌が両極化させる", "文書は〈稼働中〉（L2515）", "Ideology.cs:34:DepressionSocialism", "Ideology.cs", "読み手", "廃止を確かめた（文書が古い）", "(384) で削除（Ideology.cs:34 の注）・番人 ScalarDeletionTests.cs:41 が名を持つ", "文書の〈稼働中〉を〈廃止〉へ", "直す（文書）")
row(24, "飢えの記憶", "国の充足が 0.8 を割った深さを積み 0.95 で薄れる（L2521）", "Politics.cs:483:FamineMemoryKeepPerQuarter;Ideology.cs:36:FamineRepublicanism", "Politics.cs Ideology.cs", "読み手",
    "未確かめ（列なし）", "FamineMemory の列は無い（nation_s の foodMetShare は World.FoodFill と別の量）。同じ不足を FamineDrive（闘争度）・天命の dmFood（Politics.cs:354）・この記憶が 3 度読む＝⑨の疑い（推測）。減衰は固定の 0.95＝09-29 の方針（世代の入れ替わり）と食い違う", "計器 fm2_s（FamineMemory と FoodFill）", "残す（§13-3 で好みの軸へ）〔§13-4〕〔倍〕Famine*＝A 小・KeepPerQuarter は載らない（T の候補）")
row(25, "後の思想の解禁（発見の 2 値）", "発見の表の思想の行（L2527）", "Ideology.cs:127:EraGates", "Ideology.cs Discovery.cs", "頻度",
    "働く", "idn_s（全ての国・毎四半期）：社会主義が既知の行 24.0%・最初の年の中央 1893（1890〜1895）・10% 点 1834。共産 16.8%・1904。帝国 9.3%・1850（10% 点 1783＝開幕で既知の国）。ファシズム 1.1%・1911（1911〜1914）・開いた国×種 24（反乱国を含む）。最も早い社会主義は 10 種ともアメリカ 1785〜1788 年（台帳 S01 の確かめが今の木でも同じ）", "設計 §13-2 ④ が門を消す・文書の〈発展度はファシズムの式に残る〉は古い（Ideology.cs:126＝(640) で消した）", "残す（§13 の段 I）・直す（文書）〔§13-4〕")
row(26, "思想の到達", "伝わりが思想を運ぶ（L2533）", "Discovery.cs:732:DiscoveryContactHalf", "Discovery.cs Ideology.cs MultiNationSim.cs", "頻度",
    "一部だけ働く（量が小さい）", "disc_s の思想の行の発見 755（755〜781）：伝わりの項＞0 92.2%・点数に占める中央 0.010（0.009〜0.011）＝技術 0.68・必要性 0.31", "伝わりの項は全ての発見の行で共通（S14 だけの物ではない）", "残す（記録）")
row(27, "ファシズムの結晶", "敗戦か恐慌×左派の脅威（L2539・題の〈発展度〉は古い）", "Ideology.cs:100:FascismGate;Ideology.cs:147:FascismCrisis", "Ideology.cs", "頻度",
    "一部だけ働く", "国の割合 0.001 超 0.84%（0.75〜1.09%）・0.01 超 0.13%。危機＞0 の行 60.2%（idn_s）＝門（発見）が開いた国×四半期 1.1% だけが効く", "題の〈発展度〉を直す", "直す（文書）〔§13-4〕〔倍〕FascismCrisis*＝問い1 B・FascismGate は載らない")
row(28, "公約が果たされると思想が伸びる", "減衰ストック（L2545）", "Ideology.cs:93:ManifestoCredit;Parliament.cs:429:ManifestoCredit", "Ideology.cs Parliament.cs", "読み手",
    "未確かめ（列なし）", "S18-09 と同じ＝計器 ep_s（S18 の §6）を引く。二度書かない", "計器 ep_s", "残す（判定は ep_s の後）〔§13-4 の UpdateShares の項〕〔倍〕ManifestoFulfillCredit＝問い1 B")
row(29, "政体と思想の親和（4 軸）", "座標の近さ（L2550）", "Ideology.cs:260:AffinityOf", "Ideology.cs", "頻度",
    "一部だけ働く（S15-23）", "S15-23 を引く（引力点は立憲・共和・ブル独の 3 つだけ）＝二度数えない", "設計 §13-2 ⑤", "残す（§13 の段 I）〔§13-4〕")
row(30, "思想の集計と発言権", "公共圏の床 0.7（L2555）", "Ideology.cs:295:IdeologyVoiceFloor", "Ideology.cs", "読み手",
    "働く（ソースだけ）", "Transition（Politics.cs:577）と PowerAffinity・CrownParliament が毎四半期読む。S15-23 の引力点の数を引く", "—", "残す〔倍〕問い1 B")
row(31, "現政体の座標は参政権へずれる", "参加の軸を実際の参政権へ（L2560）", "PoliticalQuantities.cs:275:DriftedPolityCoord", "PoliticalQuantities.cs", "頻度",
    "働く（量は小さい）", "monlaw_s：ずれ≠0 の行 77.7%（76.5〜78.0%）・|ずれ| 中央 0.020・90% 点 0.060（4 軸の座標の単位）。共和の行は 100% ずれる＝男女普選の共和が 0 行（S18-02 と同じ根）", "—", "残す（§13 の段 I）〔§13-4〕")
row(32, "利益集団 3 ブロックの力", "（L2564・S18-19 と同じ題）", "InterestGroups.cs:13:Blocs", "InterestGroups.cs PolicyPreferences.cs", "読み手", "働く（S18-19）", "S18-19 を引く", "—", "—")
row(33, "第 4 の束〈小営業〉", "休眠の器（L2568）", "InterestGroups.cs:12:PettyInterestGate", "InterestGroups.cs", "読み手", "廃止を確かめた（文書が古い）", "(271) で削除（Coefficients.cs:3214 の墓標）・S18 も〈文書が古い〉", "文書の〈休眠〉を〈廃止〉へ", "直す（文書）")
row(34, "改革派と急進派の動員力", "規模×意識×(1−闘争度)／×闘争度・外圧の追い風（L2573）", "Politics.cs:526:MeasureMobilization;Politics.cs:551:ReformPressureBoost", "Politics.cs", "頻度",
    "働く（外圧の因子は働かない）", "crownpc_s：動員＞0 100%。外圧の追い風 ×(1＋0.5×外圧) は外圧≡0 で常に ×1。保持力の門は S15-21・S15-28", "外圧の因子は F2 群4 が消す", "直す（F2 群4）〔倍〕ReformPressureBoost＝問い1 I")
row(35, "組合の政治動員", "容認・統制の組合が改革派を動員（L2578）", "Politics.cs:529:UnionMobilization", "Politics.cs", "頻度",
    "一部だけ働く", "prds_s（全ての国・毎四半期）：非合法 86.7%（86.2〜87.3%）・統制 4.2%・容認 9.1%＝動員が届く行 13.3%。設計 §13-2 の〈86% の行で非合法〉と一致", "—", "残す（届かないのは組合の法が非合法の行＝法の側の結果・S19 の領分）〔倍〕問い7 B")
row(36, "窮乏のチャネル", "闘争度＞0.5 の低意識の群衆が急進へ（L2583）", "Politics.cs:547:RiotMilitancyThreshold", "Politics.cs SelfPlay.cs PolicyPreferences.cs", "頻度",
    "働く（近似）", "〔結果の後の追記〕dcont_s の partOldK（参加の側の POP のうち闘争度＞0.5 の規模の和・InstrumentsDiscontent.cs:13）＞0 の年末の行 44.6%（42.6〜44.6%）・そのときの大衆に占める割合の平均 6.8%（6.4〜6.9%）。事前登録の列（大衆の平均＞0.5）は 0.33%＝下限にしかならない。窮乏の項の和そのものは列なし", "計器 dsp_s（窮乏の項の和と兵でない全 POP の頭数）", "残す〔倍〕RiotMilitancyThreshold＝問い3 B")
row(37, "経済危機の物差し（大衆の平均満足）", "平均満足を危機の物差しに（L2588）", "Politics.cs:337:MeanSatisfactionPol;Politics.cs:591:DominanceInstabilitySatisfaction", "Politics.cs CrownParliament.cs World.cs", "頻度",
    "働く", "読み手 4 本（Politics.cs:353・:423・:591・CrownParliament.cs:96）。立憲・共和の年末の行で 平均満足＜0.43（ブル独の門が開く）36.3%（30.3〜36.3%）。門の後の引力点は S15-27（ブル独 11 行）", "—", "—〔倍〕DominanceInstabilitySatisfaction＝問い3 B")
row("S14-38", "外圧の鎖（新しい行）", "（文書なし・H021・H034 の入力）外圧＝技術の格差×海の有無", "MultiNationSim.cs:859:ExternalPressure", "MultiNationSim.cs World.cs Ideology.cs Politics.cs NationConfig.cs Simulation.cs", "読み手",
    "働かない（入力が常に 0）", "MultiNationSim.cs:858 gap＝0.0（(613)(824) で技術の格差を外した）。scalars_s の外圧＞0 は 4 腕の全ての行（約 14.8 万×4）で 0。読み手：ExternalPressureNationalism・ReformPressureBoost・ExternalPressureInlandFactor・コード 9 行・試験 12 行（out_readers）", "紙の段の第 2 回 F2 群4（lines_round2・〈上書き〉で今の回で消す線・00一覧〈次の波〉の〈前の行から続く物 F2〉）＝同じ物＝新しい線にしない。消すと Politics.cs:310 の注の C 裁定 2026-07-09〈外圧は ReformPressureBoost の経路に一本化〉の道が消える", "消す（F2 群4 を引く）〔倍〕問い1 I（入力が常に 0）と一致")

with open(sys.argv[1], "w", encoding="utf-8", newline="\n") as o:
    o.write("\t".join(H) + "\n")
    for r in R:
        assert len(r) == 12, r[0]
        o.write("\t".join(c.replace("\t", " ") for c in r) + "\n")
print(len(R))
