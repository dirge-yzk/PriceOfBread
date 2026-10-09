# 使い方（再現）：
#   1. wt_aud（3497af0 の detached）の PriceOfBread.Runner/Program.cs に measurements/init_20261009_initdump.diff を当てる
#   2. dotnet build PriceOfBread.Runner -c Release --artifacts-path <out>
#   3. dotnet <out>/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.dll initdump dump1.tsv 1789 ／ ... initdump dump2.tsv 7
#   4. このスクリプト（AUD・WJ のパスを直す）で measurements/init_20261009_values.tsv を作る
#   由来の分類規則は setting_prov() と RULES にある（読むだけ・コードは変えない）
# -*- coding: utf-8 -*-
# 初期値の表を作る（読むだけ）。入力＝initdump の2本（シード1789と7）と world.json。出力＝measurements/init_20261009_values.tsv の中身。
import csv, json, collections, re, sys, math

AUD = 'D:/PoB_work/aud/'
WJ = 'D:/PoB_work/wt_aud/PriceOfBread.SimCore/world.json'
OUT = AUD + 'init_20261009_values.tsv'

def load(f):
    rows = list(csv.reader(open(f, encoding='utf-8'), delimiter='\t'))[1:]
    d = collections.OrderedDict()
    for n, q, t, v, u in rows:
        d[(n, q, t)] = (v, u)
    return d

A = load(AUD + 'dump1.tsv')
B = load(AUD + 'dump2.tsv')
W = json.load(open(WJ, encoding='utf-8-sig'))
NAT = W['nations']
IDX = {n['name']: i for i, n in enumerate(NAT)}
KEY = {  # NationConfig 公開フィールド名 -> world.json のキー
 'Nobility':'nobility','Peasant':'peasant','Worker':'worker','Bourgeois':'bourgeois','Professional':'professional','Merchant':'merchant','Artisan':'artisan',
 'NobLit':'nobLit','PeasLit':'peasLit','WorkLit':'workLit','BourgLit':'bourgLit','ProfLit':'profLit','ClergyFrac':'clergyFrac','IntelligentsiaFrac':'intelligentsiaFrac',
 'ClergyLit':'clergyLit','IntelLit':'intelLit','PeasantSubsistence':'peasantSubsistence','Polity':'polity','Education':'education','Coercion':'coercion','Treasury':'treasury',
 'GovDebt':'govDebt','PollTaxStage':'pollTaxStage','LandTaxStage':'landTaxStage','Bureaucracy':'bureaucracy','InstitutionalDecay':'institutionalDecay',
 'NationalConsciousness0':'nationalConsciousness0','InitialMassMilitancy':'initialMassMilitancy','InitialTariff':'initialTariff','Industrialization':'industrialization',
 'Military':'military','EthnicUnity':'ethnicUnity','InitialSuffrage':'initialSuffrage','InitialCensorship':'initialCensorship','InitialCornLaws':'initialCornLaws',
 'InitialSerfEmancipation':'initialSerfEmancipation','InitialImmigration':'initialImmigration','InitialConscription':'initialConscription','InitialMonetaryLaw':'initialMonetaryLaw',
 'KnownDiscoveries':'knownDiscoveries','Rule':'rule','InitialInterestRate':'initialInterestRate','Latitude':'latitude','Longitude':'longitude','FarmLordShare':'farmLordShare',
 'CulturalGoodId':'culturalGoodId'}
# 最頻値（テンプレート既定の検出）
MODE = {}
for f, k in KEY.items():
    c = collections.Counter(json.dumps(n.get(k), ensure_ascii=False) for n in NAT if k in n)
    if c:
        v, cnt = c.most_common(1)[0]
        MODE[f] = (v, cnt)

C_TPL = '開発初期の仮置き'
C_IND = '開発初期の個別の設定（出典の記載なし）'
C_HIST_NOSRC = '史実の数字（注記あり・書名なし【推】）'
C_HIST_SRC = '史実の出典つき（法令名・年の記録あり）'
C_CAL = '較正で決めた'
C_DER = '導出'
C_DEF = 'コードの既定値（world.json に記載なし）'

D1_POP = {'ロシア','オーストリア','アメリカ','ネーデルラント','スペイン','イギリス','サルデーニャ＝ピエモンテ','ジェノヴァ','バイエルン','プロイセン','トスカーナ'}
HIST_DEBT = {'イギリス','フランス','ネーデルラント','アメリカ','スペイン','オーストリア','ロシア'}

def is_tpl(f, n):
    k = KEY.get(f)
    if f not in MODE: return False
    v, cnt = MODE[f]
    return cnt >= 8 and json.dumps(NAT[IDX[n]].get(k), ensure_ascii=False) == v

def present(n, f):
    return KEY[f] in NAT[IDX[n]]

def setting_prov(n, f, val):
    """設定/<Field> の行の (由来, 根拠, 扱い)"""
    i = IDX[n]
    base = '助走の入力（開幕の構築で使われる・助走は書き換えない）'
    tpl_note = lambda: f'{MODE[f][1]}国が同値（最頻値）'
    if f in ('Nobility','Peasant','Worker','Bourgeois','Professional','Merchant','Artisan'):
        if n in D1_POP:
            return (C_HIST_NOSRC, '2026-06-27 D1「人口の1789化」（HANDOFF.md:847）か 2026-07-13 照合(28)で国の総数を史実値へ是正。内訳の比は手置き。出典の書名は記録に無し（監査文書 docs/監査_史実距離材_2026-06-27.md は現存せず）', base)
        if i >= 12:
            return (C_TPL, '2026-06-26〜27 gen_world.py の nation() 既定テンプレから生成（HANDOFF.md:826-840）。階級の比が国どうしで似通う（貴族の比 5〜13%）・出典なし。2026-07-22 指摘 B-低「53カ国の初期階級データが実質テンプレ埋め」', base)
        return (C_IND, '2026-06 以前の手置き（既存プレイアブル国）。D1 の是正対象か記録なし' + ('。清は 2026-06-26 に「停滞する巨人」へ貴族 6000→14000 等を調整（HANDOFF.md:827）' if n=='清' else ''), base)
    if f in ('NobLit','PeasLit','WorkLit','BourgLit','ProfLit','ClergyLit','IntelLit'):
        if n in ('スウェーデン','デンマーク','スイス') and f=='PeasLit':
            return (C_IND, '2026-06-26 プロテスタント圏の農民識字を 0.45〜0.5 の欧州水準へ上書き（HANDOFF.md:837）・出典なし', base)
        if is_tpl(f, n): return (C_TPL, 'テンプレ既定と同値（' + tpl_note() + '）', base)
        return (C_IND, '国ごとに置かれた値・出典の記載なし', base)
    if f in ('ClergyFrac','IntelligentsiaFrac','PeasantSubsistence'):
        if is_tpl(f, n): return (C_TPL, '全国ほぼ一律のテンプレ値（' + tpl_note() + '）', base)
        return (C_IND, '少数の国だけ個別に置かれた値・出典の記載なし', base)
    if f in ('Polity','Education'):
        if is_tpl(f, n): return (C_TPL, '多数派の既定（' + tpl_note() + '）', base)
        return (C_IND, '個別に置かれた政体・教育法（2026-06-27 D5 で 5国の政体ラベルを是正〔プロイセン・ポーランド・アラビア・エチオピア・教皇領〕・出典の書名なし）', base)
    if f == 'Coercion':
        if is_tpl(f, n): return (C_TPL, '全国ほぼ一律のテンプレ値 400（' + tpl_note() + '）', base)
        return (C_IND, '個別に置かれた値・出典の記載なし（Coercion/1000＝革命に要る平均意識の閾値）', base)
    if f == 'Treasury':
        extra = {'プロイセン':'年GDP の約30%（戦時財宝）','清':'年GDP の約4%','フランス':'年GDP の約0.5%','アメリカ':'年GDP の約0.5%'}.get(n, '年GDP の約3%')
        return ('史実の桁を意識して決めた（出典の書名なし）', f'2026-09-29 (105) で全61カ国を置き直した＝{extra}（コミット e2bd624）。比はシムの自前の年GDP に対して置いた（史実のGDPではない）。旧の値（2026-07 以前）は年GDP の1.39倍が中央値だった', '助走は毎四半期の終わりに国庫を助走前の値へ戻す（World.RebaseTreasury・MultiNationSim.cs:621-646）＝助走後も同値')
    if f == 'GovDebt':
        if n in HIST_DEBT:
            return (C_HIST_NOSRC, '2026-09-29 (101) 88cd47b・NationConfig.cs:25-31 の注記＝1783年の国債÷年GDP の史実（英 約1.1・仏 約0.6・蘭 約1.6・米 約0.4・西 約0.3・墺 約0.2・露 約0.05）。書名の記載なし', '助走は国債を動かさない（借入・減債・不履行の3つの門）')
        return (C_DEF + '＝拠り所なしの0', 'NationConfig.cs:25-31「他の国は0（数字の拠り所が無い）」', '助走は国債を動かさない')
    if f in ('PollTaxStage','LandTaxStage'):
        if present(n, f):
            return (C_HIST_NOSRC, '2026-09-29 (109) 3a090d4＝史実の直接税÷GDP から国ごとに段を置いた（17国）。NationConfig.cs:32-36 に英 約2%・仏 約3.5〜4%・露 約2.5%・清 約1.5〜2% の注記・書名なし', base)
        return (C_DEF, '段の既定 1（土地税1段＋人頭税1段＝直接税で年GDP の約3%）＝史料の薄い近世の農業国家の仮定', base)
    if f == 'Bureaucracy':
        if present(n, f): return (C_HIST_NOSRC, '清＝科挙・プロイセン＝一般幕僚と文官制度（NationConfig.cs:38-42）・書名なし', base)
        return (C_DEF + '（Spoils）', '縁故任用が18世紀の常態（英ノースコート＝トレヴェリアン改革1854・米ペンドルトン法1883）', base)
    if f == 'InstitutionalDecay':
        if present(n, f): return (C_CAL, '2026-07-18 P7 制度疲労（清0.8/オスマン0.6/露0.4）＝清の覇権を削る較正（memory successor-final-package-2026-07-18）', base)
        return (C_DEF, '0（西欧と日本は明示しない）', base)
    if f == 'NationalConsciousness0':
        if present(n, f): return (C_CAL, '2026-07-18 P9（米0.32＝独立戦争直後・仏0.1＝革命前）', base)
        return (C_DEF, '-1＝係数既定（0.075）', base)
    if f == 'InitialMassMilitancy':
        if present(n, f): return (C_CAL, '2026-07-18 P6 乾隆帝の遺産＝白蓮教乱前夜の慢性的混乱（清のみ）', base)
        return (C_DEF, '0', base)
    if f == 'InitialTariff':
        if is_tpl(f, n): return (C_TPL, '全国ほぼ一律のテンプレ値 0.4（' + tpl_note() + '）。2026-07-22 指摘で「37カ国が初期0.4」と記録', base)
        return (C_IND, '個別に置かれた値（日本＝鎖国・清＝広州システムなど）・出典の記載なし', base)
    if f == 'Industrialization':
        if float(val or 0) == 0: return (C_TPL, '54カ国が0（テンプレ）', base)
        return (C_IND, '7カ国だけ非0・出典の記載なし。州への按分は indShare か人口比（NationConfig.cs の注記）', base)
    if f == 'Military':
        return (C_TPL if is_tpl(f, n) else C_IND, '兵数には使われない（2026-08-02 に国別の初期兵を撤去）。抽象勢力と初期ゲージ表示だけに残り、外交の同期が再導出する（Simulation.cs:877-880）', '開幕の後の外交の同期で再導出される')
    if f == 'EthnicUnity':
        return (C_TPL if is_tpl(f, n) else C_IND, '民族統一度・個別に置かれた値・出典の記載なし（最頻 0.85）', base)
    if f in ('InitialSuffrage','InitialCensorship','InitialCornLaws','InitialSerfEmancipation'):
        if is_tpl(f, n): return (C_TPL, '多数派の既定（' + tpl_note() + '）', base)
        return (C_IND, '1789年の法として個別に置いた（英米蘭など）・出典の記載なし', base)
    if f == 'InitialImmigration':
        if present(n, f): return (C_HIST_SRC, '2026-09-28 (75) d3dc133＝作者裁定 E7（日本・朝鮮＝閉鎖／清＝選別）', base)
        return (C_DEF, '開放（Open）', base)
    if f == 'InitialConscription':
        if present(n, f): return (C_HIST_SRC, '2026-10-03 (178) 43b14a8・99_実装ログ (178) §2 の表（制度と年と出典・一部は【推】＝出典未確認）', base)
        return (C_DEF + '（選抜）', '既定の選抜＝国家が平民を籤・割当てで兵営へ取る国', base)
    if f == 'InitialMonetaryLaw':
        if present(n, f): return (C_HIST_SRC, '英＝1717年以後の事実上の金本位（NationConfig.cs:67 の注記）・2026-09-28 f10e747 / 2026-09-30 (119)', base)
        return (C_DEF + '（銀）', '1783年の銀の世界', base)
    if f == 'KnownDiscoveries':
        if present(n, f): return (C_HIST_SRC, '2026-10-02 (169) eb21f8b＝英の蒸気機関（ワット特許1781・据え付け1782〜83）・特許（専売条例1624・ヴェネツィア特許法1474）', base)
        return (C_DEF, '空', base)
    if f == 'Rule':
        return (C_IND, '2026-09-23 ㊱ f0e754f＝支配の4区分（国家・在地・外国・無統治）を全61カ国に定義・根拠は 99_実装ログ ㊱ §7（仲介者を名指し）。錨4件（英露オスマン清）は据え置き', base)
    if f == 'InitialInterestRate':
        return (C_TPL, '全61カ国が 0.25（中庸の金利）・NationConfig.cs:77', base)
    if f in ('Latitude','Longitude'):
        return ('史実の事実（首都の座標・確か）', '露の首都は 2026-07 に モスクワ→ペテルブルク へ是正（照合(26)）', base)
    if f == 'FarmLordShare':
        if is_tpl(f, n): return (C_TPL, f'テンプレ値 0.6（{tpl_note()}）。コードの既定は 0.5', base)
        return (C_IND, '農制として個別に置いた（露1.0農奴・米0自作農・普土0.8領主農場など）・出典の記載なし', base)
    if f == 'CulturalGoodId':
        return ('表示用のみ（力学は読まない）', 'NationConfig.cs:142-145', base)
    return (C_IND, '', base)

# ---------- 導出量・派生量のルール（量の接頭辞 -> 区分・由来・根拠・助走の扱い） ----------
PL = '助走は価格と在庫だけを動かす（暦・人口・識字・技術・外交・資本形成は凍結）'
RULES = []
def R(pat, kind, origin, basis, warm):
    RULES.append((re.compile(pat), kind, origin, basis, warm))

PER_HEAD = '全61カ国一律の定数（1000人あたり）：貴族・聖職3000・農民200・工場労働者300・資本家3200・技師/教師/官僚/医者/研究者800・商人1500・職人400・使用人150（Simulation.cs:837-857）。兵士は元の階級の貯蓄を持ち越す（MoveCohort）'
R(r'^階級/.+/貯蓄$', '置いた（国一律の1人あたり定数×人数）', C_TPL, PER_HEAD + '。2026-08-04 の注記（貴族3000/人は資本家3200/人とほぼ同額）が最古の記録・git は 2026-09-21 が底', '助走で所得と支出が回り変わる（朝の値は定数のまま）')
R(r'^階級/.+/貯蓄÷人', '置いた（国一律の定数）', C_TPL, PER_HEAD, '助走で変わる')
R(r'^階級/.+/人数$', '導出（設定の階級人数から）', C_DER, '貴族＝Nobility×(1−ClergyFrac)・聖職者＝Nobility×ClergyFrac・農民/労働者＝設定−兵士へ移した分・資本家＝Bourgeois・技師＝Professional×(1−IntelligentsiaFrac)・教師/官僚/医者/研究者＝Professional×IntelligentsiaFrac の0.40/0.35/0.25/（研究者0）・商人・職人＝設定・使用人＝(貴族+資本家)の人数を上乗せ・兵士＝MilitarySpendingTarget×MaxSoldierFraction(0.04)×兵役の母数（Simulation.cs:837-890）', '助走は人口を凍結。兵士は定員へ寄って増える')
R(r'^階級/.+/識字$', '導出（設定の識字から）', C_DER, '階級の識字は設定（NobLit など）・聖職者/インテリは ClergyLit/IntelLit・商人0.70・職人0.45・使用人0.30は定数（Simulation.cs:837-855）', '助走の技術反復で識字は教育市場へ寄る場合がある（人口の識字は凍結）')
R(r'^階級/.+/満足度$', '置いた（全員1.0）→助走で導出', C_DER, '開幕は全 pop 満足度 1.0 の初期値（Model.cs の Satisfaction=1.0）。助走後は消費の充足から導かれる', '助走で変わる')
R(r'^階級/.+/前月の所得$', '導出', C_DER, 'EconomyStep.cs:514（前月の Earned を退避）。開幕は未計測（値は表から除いた）', '助走で変わる')
R(r'^階級/.+/(個人債務)$', '導出', C_DER, '家計の与信（既定は休眠・0）', '')
R(r'^階級/.+/国債保有$', '導出', C_DER, 'Fiscal.SeedBondHoldings（Fiscal.cs:804）＝国債（設定）を投資家へ貯蓄比で配分', '助走は国債を動かさない')
R(r'^人口/総数$', '導出（置いた値の和）', C_DER, '設定の階級人数の和＋使用人の上乗せ（(貴族+資本家)の人数・Simulation.cs:857）＋植民地の人口ウェイト×ColonialPopScale 500（Coefficients.cs:704）。州の合計＝国の合計', '助走は人口を凍結（WarmupFrozen）。ただし助走の最初の四半期に1回だけ減る国がある（シード依存：種1789＝朝鮮 −1.8%・ブハラ −2.7%・ザクセン −4.1%／種7＝英 −0.2%）。Simulation.cs:1774-1822 の年次ブロック（harvestYear≠_lastHarvestYear=-1）が Tick=0 で1度発火し、収穫の豊凶と疫病を引く【推】（popdrop 計器で1四半期目だけの低下を確認）')
R(r'^人口/本国', '導出', C_DER, '国の総数から植民地格の州の人口を除いた値', '')
R(r'^人口/都市人口比$', '導出', C_DER, '⚠IsUrban＝非農業階級（工場労働者・職人・資本家・商人・使用人…）のシェアで、都市居住率ではない（2026-07-22 指摘 B-リファレンス訂正3：英0.748 対 史実1801年約27%）', '凍結')
R(r'^人口/識字率', '導出', C_DER, '階級の識字×階級人数の人口加重', '凍結')
R(r'^人口/兵士', '導出（式）', C_DER, 'draft＝MilitarySpendingTarget×MaxSoldierFraction(0.04)×兵役の母数（働き盛りの男・Military.RecruitMen）×農民/労働者の比（Simulation.cs:881-890）。2026-08-02 に国別のハードコード初期兵（cfg.Military×250）を撤去', '助走で定員（払える予算÷費用と徴兵法の割当ての小さい方）へ寄る＝助走後÷開幕の中央値 2.5倍（0.95〜3.5倍）')
R(r'^人口/(稼働の州の数|州の数)', '置いた（州の数）', C_IND, 'world.json の provinces の数＋昇格した植民地格の州（places.json）', '')
R(r'^人口/植民地格の州', '導出', C_DER, '設定 InitialColonies の人口ウェイト×ColonialPopScale 500（Coefficients.cs:704）。植民地は 2026-08-26 に州へ昇格（ColoniesAreRegions）', '')
R(r'^投資家/', '導出', C_DER, 'CapitalMarket.IsInvestor＝IsModernElite の pop（資本家・技師・教師・官僚・医者・研究者・商人）。⚠(215) の「英 187万」は首都州（ロンドン）の資本家 pop の貯蓄（州の資本家 3200/人 ×人数）', '助走で変わる')
R(r'^GDP/', '導出', C_DER, 'World.Gdp＝Σ(産出×参照価格)（月次）。名目＝実質×物価水準。産出の元は 生産者の能力＝労働の人数×生産性×賦存（州の resources＝産地の列挙・Simulation.cs:795-820）', '助走で産出が実勢へ寄る（工場の産出の助走後÷開幕は中央値 2.1倍・0.17〜22.5倍）')
R(r'^財政/国庫$', '置いた', '', '設定 Treasury', '毎四半期、助走前の値へ戻す（同値）')
R(r'^財政/国債$', '置いた', '', '設定 GovDebt', '助走は国債を動かさない')
R(r'^財政/うち対外債', '置いた（0）', C_DEF, '開幕の対外債は0（国債は国内債として起票）', '')
R(r'^財政/(国債÷|国庫÷)', '導出（比）', C_DER, '分子＝設定、分母＝助走後の自前の名目GDP（年）。⚠史実のGDPではない', '')
R(r'^財政/(人頭税|土地税)$', '導出（段から率）', C_DER, 'Government.TaxValue(段)。段は設定 PollTaxStage/LandTaxStage', '')
R(r'^財政/所得税$', '置いた（0）', C_HIST_NOSRC, 'NationConfig.cs:32「所得税は全ての国で0（英1799・普1811/20・墺1849・日1887・米1861/1913）」', '')
R(r'^財政/関税$', '置いた', '', '設定 InitialTariff', '')
R(r'^財政/国債金利$', '置いた（World の初期値 0.04）→助走で再計算', C_TPL, 'World.cs:1117 GovBondRate=0.04（全国一律）。債務/GDP と買い手の希少さで毎月再計算', '助走で変わる')
R(r'^財政/金利', '置いた', '', '設定 InitialInterestRate（全国 0.25）', '')
R(r'^財政/(行政費|教育費|陸軍維持費|海軍維持費)', '置いた（係数・政策の既定）', C_TPL, '行政費0.5（World.cs:231）・教育費0・陸海の維持費＝PolicyDefaults の軍事費の段を陸海に分けた値（全国一律）', '')
R(r'^財政/直近の四半期', '導出', C_DER, 'World.LastRevenue/LastTariffRevenue＝税率×課税ベース。開幕は未計測（値は表から除いた）。⚠純税収が年GDP の2〜7%（英 約2.3%）', '')
R(r'^財政/第1四半期の台帳', '導出', C_DER, 'World.TreasuryLedger（区分別・収入が正）。助走は台帳を捨てる（RebaseTreasury）ので、助走後の最初の実四半期の値。⚠英だけ OtherOut が年GDP 換算で −78%（調査未）', '')
R(r'^軍/艦の隻数', '導出（外交の最初の同期）', C_DER, 'Diplomacy.cs:956-975＝最初の外交で EconomyStep.FleetTargetUnits（NeedSea＝仮想敵国への注意配分から）の隻数を播く。助走は外交を凍結するので助走後は0。⚠港のある国がほぼ全て 200〜550隻（米535・朝鮮505・日本217）', '助走中は0（外交凍結）')
R(r'^軍/', '導出', C_DER, 'Power のゲージ（外交の同期が毎月導く）。設定 Military は使われない', '助走中は外交凍結')
R(r'^植民地/', '置いた（人口ウェイト）→導出', C_IND, '設定 InitialColonies（名前・産地・人口ウェイト）。ColonyList は州へ昇格済みで0・質量は植民地格の州の人口÷500', '')
R(r'^設備/工業化度', '導出（設定を州へ按分）', C_DER, '設定 Industrialization の合計を indShare か人口比 Share で州へ按分（NationConfig.cs の注記・Σは不変）', '')
R(r'^設備/(建てた工場|工場|産地|役務)', '導出', C_DER, '生産者の産出（Producer.Output）×参照価格。能力 Capacity は毎月更新なので開幕は0', '助走で産出が実勢へ寄る')
R(r'^設備/(港|鉄道|海運)', '導出（0スタート）', C_DER, '国家＋資本家の投資で積む（0スタート）', '')
R(r'^土地/', '導出', C_DER, 'ArableLand＝初期農民×LandHeadroom 1.8（Simulation.cs:749・918）。州に arableLand を明示した国だけ手置き', '')
R(r'^貨幣/基礎貨幣', '導出', C_DER, 'World.BaseMoney＝TotalMoney＋国庫−与信残高。TotalMoney＝全 pop の貯蓄の和＝（国一律の1人あたり定数×人数）', '')
R(r'^貨幣/広義', '導出', C_DER, '全 pop の貯蓄の和（階級/…/貯蓄の和）', '')
R(r'^貨幣/貯蓄÷', '導出（比）', C_DER, '⚠貯蓄が年名目GDP の倍数：開幕 中央値79（10〜407）・助走後 中央値45（15〜78）。1人あたり貯蓄が国一律の定数で GDP は国ごとに違うため、貧しい国ほど大きい。階級別の貯蓄は助走で ±70% 以上動く（使用人 −98%・農民 −74%・資本家 +114%〔中央値〕）＝定数は均衡の値ではない', '')
R(r'^貨幣/(与信|準備)', '導出', C_DER, '銀行の与信は Credit.Step が毎日 BaseMoney から導く。準備の金銀は開幕0・助走で積む', '')
R(r'^貨幣/通貨制度', '置いた', '', '設定 InitialMonetaryLaw', '')
R(r'^物価/物価水準|^物価/自己費用', '置いた（1.0）→導出', C_TPL, 'World.PriceLevel の初期値 1.0（全国一律）', '助走で変わる')
R(r'^物価/', '置いた（参照価格）→導出', C_TPL, '開幕の価格＝参照価格 Catalog.RefPrices（goods.json のレシピから自動派生・単位は任意）。市場の初期値は価格＝参照価格・在庫200・目標在庫200（Simulation.cs:416・968）', '助走で均衡へ寄る')
R(r'^市場/', '置いた（全財の在庫を一律200）', C_TPL, 'Simulation.cs:416/498/968 `Stock = 200, TargetStock = 200`（全財・全州・全国一律）。軍需の4財は首都に InitialArsenalMonths(12) ヶ月分を上積み（Simulation.cs:575-600）', '助走で消費され大きく変わる')
R(r'^制度/技術水準|^制度/専門人材率', '導出', C_DER, '開幕は0・WarmupTech の8反復が識字×世俗水路から導く（MultiNationSim.cs:654）', '助走で変わる')
R(r'^制度/(教育法|政体)', '置いた', '', '設定 Education / Polity', '')
R(r'^制度/国民意識', '置いた（-1＝未捕捉）', C_DEF, '設定 NationalConsciousness0（米0.32・仏0.1のみ）', '')
R(r'^制度/発見の解禁', '置いた', '導出', '設定 KnownDiscoveries（英2・ヴェネツィア1）＋WarmupEconomy の後の世界の和の配布', '')
R(r'^抽象/', '置いた（abstractTargets）', C_IND, 'world.json の abstractTargets（21件・経済を持たない標的勢力）。軍ゲージ・植民地ウェイトは手置き・GDP は静的', '')
R(r'^助走/', '係数', C_TPL, 'Coefficients.cs:2350-2351（20四半期・技術8反復）', '')
R(r'^国数|^勢力数', '数', '', '', '')

CLASS_SETTING = None

def rule_for(q):
    for pat, kind, origin, basis, warm in RULES:
        if pat.search(q): return kind, origin, basis, warm
    return ('', '', '', '')

def origin_chain(n, q):
    """導出量の由来を入力の由来から決める（国ごと）"""
    # 階級の人数・識字・貯蓄・人口・GDPなどは設定の由来に従う
    f = 'Nobility'
    if n in IDX:
        o = setting_prov(n, 'Nobility', None)[0]
        return o
    return ''

# ---------- 設定の値を文字列で ----------
def fmt(v):
    return v

rows = []
miss = 0
# 開幕でも未計測の行は表から除く
def unmeasured_open(q, t):
    if t != '開幕': return False
    return any(s in q for s in ('前月の所得', '前月所得', '貯蓄÷年所得', '直近の四半期', '第1四半期の台帳'))
# 助走後・最初の四半期での変化率
def pct(a, b):
    try:
        x = float(a); y = float(b)
    except: return ''
    if math.isnan(x) or math.isnan(y): return ''
    if x == y: return '0'
    if x == 0: return '開幕0→' + format(y, '.4g')
    return format((y - x) / abs(x) * 100, '+.1f') + '%'

HEADER = ['国','量','時点','値','単位','区分','由来','根拠','助走の扱い','助走後の変化(開幕比)','別シード(7)の値']
for (n, q, t), (v, u) in A.items():
    if unmeasured_open(q, t): continue
    if (n, q, t) in A and v == 'NaN' and t == '開幕': continue
    kind = origin = basis = warm = ''
    if t == '置いた':
        if q.startswith('設定/'):
            f = q.split('/', 1)[1]
            if f in KEY and n in IDX:
                origin, basis, warm = setting_prov(n, f, v)
                kind = '置いた'
            elif f == '階級人数の和(商人・職人を含む)' or f.startswith('階級人数'):
                origin, basis, warm = setting_prov(n, 'Nobility', None); kind = '置いた（設定の和）'
            elif f.startswith('州'):
                origin, basis, warm = (C_IND, 'world.json の provinces（州の数・人口の按分 Share の和1・arableLand/indShare の明示）。2026-06-26 の 287州の作り直し〔docs/無題のファイル.md〕で手置き', '助走の入力')
                kind = '置いた'
            elif f.startswith('植民地'):
                origin, basis, warm = (C_IND, 'world.json の initialColonies（名前・産地・人口ウェイト）。ウェイトは「史実の相対規模」とされるが出典の記載なし（Diplomacy.cs:102）', '助走の入力')
                kind = '置いた'
            else:
                origin, basis, warm = (C_IND, '', ''); kind = '置いた'
        elif q.startswith('州/'):
            sub = q.split('/')[-1]
            if sub == 'share':
                origin, basis, warm = (C_IND, '州の人口の按分 Share（合計1）。2026-06-26 の 287州の作り直しで手置き。米は 1790 年センサスから約3倍ずれる（2026-07-22 指摘 B-高）', '助走の入力')
            elif sub == 'arableLand':
                origin, basis, warm = (C_IND, '州に明示した耕地賦存量（辺境の廃止に伴う一般化・NationConfig.cs:150）', '助走の入力')
            else:
                origin, basis, warm = (C_IND, '州に明示した工業化の按分キー（2026-09-01）', '助走の入力')
            kind = '置いた'
    else:
        kind, origin_r, basis, warm = rule_for(q)
        origin = origin_r
        # 国ごとの由来（設定に依る量）
        if q.startswith('州/'):
            sub = q.split('/')[-1]
            if sub in ('人口',):
                kind, origin, basis, warm = '導出（人口の按分 Share×階級人数）', origin_chain(n, q) or C_DER, '国の階級人数×州の Share（設定）。植民地格の州＝ウェイト×500', '凍結'
            elif sub in ('資本家の貯蓄',):
                kind, origin, basis, warm = '置いた（国一律の定数）', C_TPL, '資本家 3200/人 ×州の資本家の人数（Simulation.cs:841）。(215) の「英 187万」はロンドンの資本家', '助走で変わる'
            elif sub in ('資本家の前月所得', '資本家の貯蓄÷年所得'):
                kind, origin, basis, warm = '導出', C_DER, 'EconomyStep.cs:514 の前月の所得。年所得＝前月×12（月次の累積。earnprobe で月々の値が 6〜9千の間で動くのを確認）', '助走で変わる'
            elif sub == '耕地賦存量':
                kind, origin, basis, warm = '導出', C_DER, '初期農民×1.8（Simulation.cs:918）。明示した州だけ手置き', ''
            elif sub == '工業化度':
                kind, origin, basis, warm = '導出（設定を按分）', C_DER, '設定 Industrialization を indShare か Share で按分', ''
            elif sub == '格':
                kind, origin, basis, warm = '導出', C_DER, 'HeldSince から（1633年より前から保有＝Core）・植民地の昇格＝Colonial', ''
            elif sub == '都市人口比':
                kind, origin, basis, warm = '導出', C_DER, 'IsUrban（非農業階級）のシェア', ''
            elif sub.startswith('産出額'):
                kind, origin, basis, warm = '導出', C_DER, 'Σ(産出×参照価格)', '助走で変わる'
        if origin == C_DER and n in IDX and re.match(r'^(階級/.+/(人数|識字)|人口/総数|人口/識字率|GDP/)', q):
            oc = setting_prov(n, 'Nobility', None)[0]
            origin = f'導出（入力の階級人数の由来＝{oc}）'
        if q == '財政/国庫':
            origin = setting_prov(n, 'Treasury', None)[0]; warm = setting_prov(n, 'Treasury', None)[2]
        elif q == '財政/国債':
            origin = setting_prov(n, 'GovDebt', None)[0]; warm = setting_prov(n, 'GovDebt', None)[2]
        elif q == '財政/関税':
            origin = setting_prov(n, 'InitialTariff', None)[0]
        elif q == '財政/金利(政策)':
            origin = setting_prov(n, 'InitialInterestRate', None)[0]
        elif q == '貨幣/通貨制度':
            origin = setting_prov(n, 'InitialMonetaryLaw', None)[0]
        elif q == '制度/教育法':
            origin = setting_prov(n, 'Education', None)[0]
        elif q == '制度/政体':
            origin = setting_prov(n, 'Polity', None)[0]
        elif q in ('財政/人頭税', '財政/土地税') and n in IDX:
            origin = setting_prov(n, 'PollTaxStage', None)[0]
        elif q.startswith('制度/発見の解禁') and n in IDX:
            origin = setting_prov(n, 'KnownDiscoveries', None)[0]
        elif q.startswith('植民地/') and n in IDX:
            origin = C_IND
        elif q.startswith('設備/工業化度') and n in IDX:
            origin = setting_prov(n, 'Industrialization', None)[0]
    chg = ''
    other = ''
    if t in ('助走後', '最初の四半期の後'):
        o = A.get((n, q, '開幕'))
        if o: chg = pct(o[0], v)
        b = B.get((n, q, t))
        if b: other = b[0]
    if t == '最初の四半期の後': warm = '最初の実四半期（1783Q1）の末の値＝助走の外・外交と政治が動き出した後'
    rows.append([n, q, t, v, u, kind, origin, basis, warm, chg, other])

with open(OUT, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f, delimiter='\t', lineterminator='\n')
    w.writerow(HEADER)
    w.writerows(rows)
print(len(rows), 'rows ->', OUT)
