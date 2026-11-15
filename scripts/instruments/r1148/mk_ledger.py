# (1148) S26 の台帳の行の書き直しの案（.tsv・12 列）を作る（読むだけ・行の 1〜7 列は台帳のまま、錨だけ取り直す）
import sys
ROWS, OUT = sys.argv[1], sys.argv[2]
HEAD = '行\t系統\t機能\t期待する動き（出典）\t錨\t持ち主のファイル\t判定の型\t最後に確かめた\t判定\t根拠\t残りタスク\t直す／消す／残す'
COMMIT = 'aa8ee7e51'
ANCHOR = {   # 引けなかった錨の取り直し（out_anchors.txt の NG→ の先）
    'BilateralMarket.cs:241:shipCapNat': 'BilateralMarket.cs:304:shipCapNat',
    'BilateralMarket.cs:571:shipShare': 'BilateralMarket.cs:591:shipShare',
    'Diplomacy.cs:1126:ThreatExcess': 'Diplomacy.cs:1047:ThreatExcess',
    'EconomyStep.cs:908': None,
    'Instruments.cs:2059:Settled': 'Instruments.cs:2096:Settled',
    'EconomyStep.cs:911:ColonialResources': 'EconomyStep.cs:823:ColonialResources',
    'Politics.cs:1475': 'Politics.cs:865:CheckSecession',
    'Politics.cs:293': 'Politics.cs:165:RegionAvgSuffrageInclusion',
    'Simulation.cs:701:ScarceCountsIncludeColonies': 'Simulation.cs:646:InitialColonies',
    'Coefficients.p08_audit_soldiers.cs:458:ScarceCountsIncludeColonies': None,
}
S = '頻度は M＝keep_1143/base_ins・N1・N2＝ヌル腕・S11＝種 11〜20（out_freq_*.txt）'
V = {  # 行 → (判定, 根拠, 残りタスク, 処置)
'S26-01': ('働く', '頻度：開幕の植民地格の州の和 81 が 40/40 種（M・N1・N2・S11）＝world.json の initialColonies 81（英 27・西 26・葡 9・仏 6・蘭 6・露 4・オマーン 2・丁 1）・国の植民地の一覧は昇格で空（MultiNationSim.cs:150 で外す）', '—', '残す'),
'S26-02': ('働く', '片側だけの門：同名の州があれば continue（MultiNationSim.cs:149）。頻度：昇格しなかった植民地 0/81（40 種）＝閉じる側は今のデータで一度も出ない', '—', '残す'),
'S26-03': ('支配の力の段で書き直す', '頻度：四半期 0 の植民地格の州（rule の表に載る 37 州）の支配度 1.000 が 37/37（M・N1・N2 の 30 種）。途中の接収は 0 から（Simulation.cs:1366）', '〔印〕支配の力の段 E（州の行き先＝最大の区分）', '—（深く読まない）'),
'S26-04': ('働く', '頻度：四半期 0 の終わりの植民地格の州の人口のうち農民 99.28〜99.36%（M 10 種・cpop）・残りは 1 四半期の移り（工場労働者 0.5%・兵士 0.1% ほか）。在庫 0（Simulation.cs:1385）・耕地＝農民×余裕（:1372）', '—', '残す'),
'S26-05': ('働く', 'ソース：気候ゲート財の削り（Simulation.cs:1286）・賦存の当て（:1302）・高度な生産者に渡された coef（:1358）。頻度：全ての昇格（開幕 81＋接収）で同じ道', '—', '残す'),
'S26-06': ('働かない', 'ソース：Culture＝w.Region.Culture・Religion＝w.Region.Religion（Simulation.cs:1370）・CulturalGoodId も首都の写し（:1373）＝植民地格の州 110 全てが首都の札（頻度 110/110・接収した抽象標的も同じ道 Diplomacy.cs:2028）。データの欠け：world.json の initialColonies（name・resources・popK）と abstractTargets の colonies、places.json（name・lat・lon・kind・terrain・rule）に文化・宗教の欄が無い。割譲（Territory.cs:155 Rehome）は札を変えない＝本国の州の札は保たれる。観戦 N19・U11・X1', '〔印〕宗教と移民 R8', '直す（データ＋1 行・挙動）：places.json の colony 104・target 13 の行に土地の文化・宗教を足し Simulation.cs:1370 で読む。改宗・混ざり合いは作らない（作者「改宗・文化の混ざり合いや分離などは、実装しないでください。」＝札は動かないデータ）'),
'S26-07': ('働く', '両側の門：植民地格・RGO・輸出作物だけ地代 0.6・持ち主を資本家に（Simulation.cs:1303〜1306）・主食は自作農のまま。頻度：植民地の地代＞0 の国×四半期 6,831〜9,935／種（M）・N1 7,017〜10,280・N2 7,946〜9,788＝植民地を持つ国×四半期のほぼ全て（M 種 1：8,484／8,494）', '植民地の稼ぎ（616・618 行 ⑥）・貿易の置き換え §4', '残す（倍率の一掃の区分〈個別：所有の構造を定数で置く〉＝作者の問いの材料）'),
'S26-08': ('働く', '頻度：送金≠0 の国×四半期 4,713〜7,317／種（M）＝植民地の地代＞0 の 69〜78%・N1 4,635〜7,597・N2 5,477〜7,788。送金÷植民地格の州の地代 の中央 0.025（M）・0.021（N1）・0.017（N2）＝主食の地代を含む分母', '植民地の稼ぎ・貿易の置き換え §4-3', '残す'),
'S26-09': ('未確かめ', 'ソースでは働く：清算の地代の受け手（EconomyStep.cs:684〜696）が AbsenteeRgo のとき首都の投資家（Colonial.AbsenteeOwner）。頻度：輸出の地代の受け手の列は階級の和（InstrumentsLandshare.cs の expSlot）だけで植民地格の分けが無い＝0 と数えない（§6 の計器）', '植民地の稼ぎ・貿易の置き換え §4-3', '—（列は §6 の計器）'),
'S26-10': ('働く', 'データ：landmass.json に植民地 110 名が全て登録・内陸と沿岸の両側がある（S26-H004）。記録：パナマ（9.0,-79.5）が coastal false のまま（aa8ee7e51 で再確認）＝データの誤りの疑い', '—', '直す（データ 1 行）：パナマを沿岸に（landmass.json）'),
'S26-11': ('働く', '頻度（nation の州の数×四半期の和／種）：中核 139,921〜144,969・編入 6,667〜11,715・植民地格 64,331〜68,907（M）＝3 つの格が全ての種で出る（N1・N2・S11 も同じ桁）', '〔印〕支配の力の段 F（割譲の経路が替わる）', '—'),
'S26-12': ('働く', 'ソース：BuildProvince（Simulation.cs:820）と PromoteColonyToRegion（:1361）が同じ AddAlternateRecipes（:949）を渡された coef で呼ぶ。試験の盤面（BuildFullChain :442・BuildFrance :514）だけ new Coefficients()＝61 カ国の世界は通らない。頻度：全ての州の構築', '—', '残す'),
'S26-13': ('働く', '頻度（nation の adminFiscalMeanMul・1790/1850/1900 年）：植民地を持つ国の中央 1.103〜1.113・最大 1.88〜1.91／持たない国の中央 1.004（M・N1・N2・S11）＝距離の割増が植民地を持つ国だけで立つ', '—', '残す'),
'S26-14': ('働かない', '削除済み：ColonyUpkeepBase・ColonyUpkeepMilitancyScale は SimCore・App・Runner・Tests で grep 0', '—', '—'),
'S26-15': ('働かない', '削除済み：ColonyUpkeepCoalPerDist・ColonyUpkeepFoodPerDist は grep 0＝占有を通してだけ効く', '—', '—'),
'S26-16': ('一部だけ働く', '頻度：植民地の維持費の請求＞0 の月 49.5〜66.6%（M・計器の国）・N1 44.5〜60.2%・N2 45.8〜65.4%・S11 37.7〜73.7%。最初の年 1798〜1815（汽船の前は 0）＝門の両側は出る。量：請求の合計に占める割合の中央 2.6e-5〜3.5e-5・最大 0.0074＝財政に効かない桁。燃料＝燃料率×汽船の割合×占有された容量（Colonial.cs:39）・時代係数 EraColonyCoal は EraFactor が常に 1.0（Coefficients.p10_ai_silver_overrides.cs:65）＝効かない', '—', '残す（S26-18 の線 L8 の中で読み直す）。EraColonyCoal の項は消す候補 D5（恒等）'),
'S26-17': ('未確かめ', '燃料の財の選びの列が無い（0 と数えない）。同じ関数を海軍（Military.cs:183）と民間の汽船（Transport.cs:128・:190）が読む＝植民地だけの機構ではない', '—', '—'),
'S26-18': ('一部だけ働く', '占有（World.ColonialShipBurden）の列は 10 種の表に無い。見積もり（out_burden_est.txt・Colonial.cs:19〜36 の式を world.json と places.json に当てた・大圏の km＝下側・帆走の時代）：開幕の英 5.37（×1.5 の仮で 8.05）＝交易の実効の海運が国の船腹の 1/6.4 以下・西 2.24・蘭 0.83・葡 0.37。占有は植民地の住民の数に比例（Colonial.cs:30 の pw）＝補給の物の量ではない。交易の船腹が拘束するかは未確かめ（transportdump の 1 種で読む）', '貿易の置き換え（BilateralMarket.cs:304・:608 を T1 が触る）', '読む→直す（線 L8）：transportdump で今の占有と、拘束する州×四半期の割合を数える。倍率の一掃の区分は問い10 A（AI の推奨・確認待ち〈今消す〉）'),
'S26-19': ('一部だけ働く', '頻度（canal・40 種）：スエズの開通 1803〜1815（中央 1806〜1808・史実 1869）・パナマ 1833〜1891（中央 1858〜1870・史実 1914）＝喜望峰の側が効くのは開幕の約 20〜30 年だけ。着工の門は発見〈汽船〉だけ（Megaprojects.cs:19・world.json の requires）・拠出は発見の済んだ国の投資家の貯蓄の割合（MultiNationSim.cs:1119〜1130）', '輸送の段1と運河（616 行 ⑦）', '直す（線 L7）：拠出を拠出国の汽船の量に比例させる。巨大建造物を消す線は運河の requires を Chokepoint へ移す（Megaprojects.RequirementMet の読み手を残す）'),
'S26-20': ('一部だけ働く', '頻度：キニーネの標的（熱帯の抽象標的 7・10 州）を植民地格で持つ国×四半期＝活きた国×四半期の 3.1〜6.3%（M）・3.2〜7.3%（N1）・3.2〜6.4%（N2）・2.9〜6.2%（S11）・40/40 種で出る・最初は四半期 62〜88（1798〜1805）。量は 1 州 月 6（Colonial.cs:24）＝S26-16 の請求に含む', '—', '消す候補 D2（S26-21 と同じ線・読み手 SimCore 12＝MalariaQuininePerColony 2・quinineWant 3・Malarial 7・試験 0）'),
'S26-21': ('働かない', 'ソース：BuyUpkeepGood の戻り値（買えた量）を捨てる（Colonial.cs:43）・Region.Malarial の読み手は Colonial.cs:31 だけ＝買えなくても誰も死なない。作者「キニーネは材から消してよいのではないか。これは医薬品の発見だと思う」', '死の側の段3（医薬品の発見×医薬品の量）', '消す候補 D2（読み手 SimCore 12・試験 0・遠征のキニーネは残す）'),
'S26-22': ('未確かめ', '買い控えの量の列が無い。式は全ての政府支出と同じ 1 本（EconomyStep.StatePriceAfford・Colonial.cs:119）＝S20 の行で確かめる', '—', '—'),
'S26-23': ('働く', '頻度：植民地を持つ計器の国×四半期 51,402（M）・52,338（N1）・54,243（N2）の全てで月の加算＞0。月の加算の 5/50/95/99% 点（M 種 1）0.00028/0.0010/0.0029/0.0040・全体の中央 0.00098〜0.00102・最大 0.0058〜0.0059（3 腕）【近似：secession の popK を重み・闘争度は州の平均】。闘争度は目標へ 0.30／月で追う（Politics.cs:177・Coefficients.cs:86）＝定常のずれ ≈ 加算÷0.30＝中央 約 0.003・最大 約 0.02【推】。⚠ 目標を通さず直に足す（Colonial.cs:60）・足す先は植民地の住民を含む全ての大衆・規模は 3 州で頭打ち（:57）', '—', '消す候補 D1（読み手 SimCore 6＝ColonialUnrestHomeWeight 3・AntiImperialOpinion 3・試験 0・App 0）（倍率の一掃の区分は問い1a B〈残す〉＝AI の推奨・確認待ち。直に足す形は問い1a の条件〈思想⑨〉に当たる疑い＝作者の問いの材料）'),
'S26-24': ('未確かめ', '組ごとの脅威の項の列が無い（S25-03 と同じ）。ソース：植民地格の州 1 つに 0.5＋人口/2（Diplomacy.cs:373〜382）・帝国の大きさの物差しが 3 本（Colonies＝:47・ColonyMassUnits＝:51・ColonyMass）。植民地格の州の産出は既に GDP の項に入る＝GDP と植民地の項の重なり【推】', '〔印〕支配の力の段 G（覇権の脅威を実効の GDP へ）・S25 の緊張の計器', '直す（段 G と同じ腕）'),
'S26-25': ('支配の力の段で書き直す', '支配の力の設計 §7 で DependentThreatWeight を段 G で消す（比例の実効の GDP へ）＝判定しない', '〔印〕支配の力の段 G', '—'),
'S26-26': ('文書が古い', 'ソース：海の比は植民地を読まない（ArmyShape.cs の Colonial は陸の通行の home の印 :127 だけ）', '—', 'S23 の行へ移す（合流が S23 に 1 行）'),
'S26-27': ('働く', 'ソース：国を持たない Power は毎月 0.10（Diplomacy.cs:778）＝定数の代入。頻度：抽象標的が残る四半期だけ（植民地格の州は 81→最後 106〜110／110＝多くの種で抽象標的は終局までに全て取られる）', '—', '残す（倍率の一掃の区分〈個別：根拠の無い重み〉＝作者の問いの材料）'),
'S26-28': ('働く', '頻度（nationgood・植民地財 11 品の越境の輸出額÷全ての財）：1790 年 0.43〜0.60・1850 年 0.14〜0.24・1900 年 0.04〜0.28・1940 年 0.03〜0.24（M・N1・N2）', '貿易の置き換え §4-3', '残す'),
'S26-29': ('働く', '頻度（migration・全長）：国内の移民 4,807万〜7,238万人（M）・植民地格の州へ 730万〜2,452万人（M）・N1 621万〜2,122万・N2 249万〜2,274万・S11 482万〜1,772万', '〔印〕宗教と移民 R6（移民の作り直し）', '残す（R6 が書き直す）'),
'S26-30': ('一部だけ働く', '頻度（xmig・全長）：越境の移民 379万〜759万人（M）・N1 353万〜825万・N2 408万〜690万・人で重みの大圏 km の中央 7,338（M 種 1）。上位の向き＝英→日 71 万・米→日 52 万（種 1）・英→清 201 万・米→清 158 万（種 2）＝向きが史実と逆', '〔印〕宗教と移民 R1（向きが逆の根）', '直す（R1 の線・既にある残りタスク）'),
'S26-31': ('未確かめ', 'ソース：国内（Territory.cs の IsFinite の否定形）・越境（MultiNationSim.cs の IsFinite）で評価できない州を外す＝片側だけの門。閉じる側の回数の列は無い', '—', '残す'),
'S26-32': ('一部だけ働く', 'ソース：活きた州の人口で重みの多数派（Diplomacy.cs:820・:1078）＝式は働く。入力の植民地格の州の札が首都の写し（S26-06）＝植民地は多数派を首都の側へ寄せるだけ', '〔印〕宗教と移民 R8', '直す（S26-06 と同じ腕）'),
'S26-33': ('支配の力の段で書き直す', '統治コスト（GovCost）の読み手は講和の属国化だけ＝支配の力の設計の段 F で属国化ごと消える。緊張の側は S25-06', '〔印〕支配の力の段 F・S25-06', '—'),
'S26-34': ('未確かめ', '扼者の名の列が無い。ソース：助走中も毎四半期解決（MultiNationSim.cs:1110）', '—', '—'),
'S26-35': ('一部だけ働く', '頻度（nation）：植民地格の州の和 開幕 81→最後 106〜110（40 種）＝初期 110（81＋抽象標的 29）。移転（接収）の側は踏む・独立と吸収の出口は列が無い＝検査の片側だけを生で読める', '〔印〕植民地の独立（第 2 期）', '残す（番人）'),
'S26-36': ('文書が古い', 'Colony の住民の経路は 2026-10-18 に削除＝木に無い', '—', '—'),
'S26-37': ('支配の力の段で書き直す', '経路は削除済み。残る係数 ColonyPromotionMinPop の読み手は分離の群衆の項（Politics.cs:895）と計器 2（InstrumentsRulePower.cs・InstrumentsSecession.cs）＝名と意味が食い違う', '〔印〕支配の力の段 E', '—（段 E の申し送りに名を書く）'),
'S26-38': ('文書が古い', '残骸：計器 Instruments.Settled（Instruments.cs:2096）の呼び手 0・region の settlerOutKPerQuarter・settlerInKPerQuarter は 216,970/216,970 行が 0（M 種 1）', '—', '消す候補 D4（恒等・計器・読み手 3＝Instruments.Settled 1〔呼び手 0〕・2 列の試験 2 行・台本 14〜17 本）'),
'S26-39': ('文書が古い', '経路ごと削除（木に無い）', '—', '—'),
'S26-40': ('文書が古い', '多国の枝は物理削除（EconomyStep.cs の植民地素材の輸入は単一国の枝 :823 だけ）', '—', '—'),
'S26-41': ('働かない', '61 カ国の世界では ColonialResources は常に空の配列（Simulation.cs:841・:1377・Territory.cs:160・World.cs:1154）＝単一国の枝（EconomyStep.cs:823〜834）に入らない。それでも ColonialAccess() を毎月計算して全ての州へ書く（Simulation.cs:1730・:1773）＝読まれない欄。頻度 0/全ての州×月', '—', '消す候補 D3（61 カ国では恒等・読み手 SimCore 12＝ColonialAccess 5・ColonialResources 7・試験 5）'),
'S26-42': ('働かない', '削除済み（CaptiveMarket ほか grep 0）', '—', '—'),
'S26-43': ('文書が古い', 'ColonialExtractionFraction・ColonialProfit* は grep 0', '—', '—'),
'S26-44': ('支配の力の段で書き直す', '記録（判定しない）：分離独立の 1 本（Politics.cs:865 CheckSecession→Territory.cs:169 Seceded）', '〔印〕支配の力の段 E・植民地の独立（第 2 期）', '—'),
'S26-45': ('未確かめ', 'S19-28 と同じ門（法の動きがほぼ無い）。算入率の列は secession の suffrageInclusionShare だけ', 'S19-28・王権と議会の作り直し（段 P2）', '—'),
'S26-46': ('一部だけ働く', 'ソース：州の平均の算入率は移民原住民参政権だけを読み植民地参政権を読まない（Politics.cs:1041〜1042）＝同じ問いに式が 2 本（S26-45 と別）。頻度：植民地格の州の行で算入率＞0 が 99.89〜99.93%（M・N1・N2）＝1−中核度の側でほぼ常に正', '〔印〕支配の力の段 E（分離の側 :893）', '直す（線 L9）：少数派の不満の側 Politics.cs:165 を POP ごとの 1 本へ畳む（挙動）'),
'S26-47': ('文書が古い', 'CessionRangeTechKm は grep 0', '—', '—'),
'S26-48': ('働く', 'ソース：旗 ScarceCountsIncludeColonies は焼かれた（grep 0）・希少鉱物の総数は開幕の植民地を無条件で数える（Simulation.cs:636〜652）', '—', '残す'),
'S26-49': ('文書が古い', 'PreemptiveSeizeWeight は DormantIdentityTests の復活の番人の名だけ（DormantIdentityTests.cs:354）', '—', '—'),
'S26-V01': ('働かない', 'S26-06 と同じ（Simulation.cs:1370）。観戦 X1・N19・U11（3 本とも）', '〔印〕宗教と移民 R8', '直す（S26-06 と同じ腕）'),
}
out = open(OUT, 'w', encoding='utf-8', newline='\n'); out.write(HEAD + '\n'); n = 0; miss = []
for row in open(ROWS, encoding='utf-8'):
    ln, r = row.rstrip('\n').split('\t', 1)
    c = [x.strip() for x in r.split('|')][1:13]
    if c[0].startswith('S26-H'): continue
    if c[0] not in V: miss.append(c[0]); continue
    anc = []
    for a in c[4].split(';'):
        a = a.strip()
        if a in ANCHOR:
            if ANCHOR[a]: anc.append(ANCHOR[a])
        elif a: anc.append(a)
    c[4] = ';'.join(anc) if anc else ('EconomyStep.cs:823:ColonialResources' if c[0] == 'S26-40' else '—')   # S26-40：多国の枝を消した後に残る単一国の枝
    j, why, rest, act = V[c[0]]
    vals = c[:7] + [COMMIT, j, why, rest, act]
    assert all('\t' not in v and '\n' not in v for v in vals)
    out.write('\t'.join(vals) + '\n'); n += 1
print('rows', n, 'missing', miss)
