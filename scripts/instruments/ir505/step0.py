# (505) 線 ir 段0：技術水準・工業化度の読み手の全数表（grep ＋ 字面の分類・シミュは回していない）
# 使い方：PYTHONUTF8=1 python step0.py <リポジトリの根> <基点のコミット> > measurements/ir505_20261023_step0.txt
#   探す語：TechLevel（WorldTechLevel・Region.TechLevel を含む）・TechProductivity・Industrialization（TotalIndustrialization・ProductiveIndustrialization・MeanIndustrialization・IndustryInvestRegionCap・OldVintageCapital・StrandedVintage）。
#   対象：PriceOfBread.SimCore/*.cs の非コメント行（// 以降を除く）。分類は (ファイル, 字面) の規則表で機械的に付ける（上から最初に当たった規則）。どの規則にも当たらない行は〈未分類〉として末尾に出し、終了コード 1 にする（表の抜けを許さない）。
#   分類の記号は事前登録 measurements/ir505_20261023_prereg.txt [1] の A1・A2・B1・C1〜C11 と、丙・乙・済（(495) で旗が削除する）・状（技術水準の状態の更新と運搬）・宣言。
import re, subprocess, sys, glob, os

repo, base = sys.argv[1], sys.argv[2]
WORDS = re.compile(r'\b(TechLevel|WorldTechLevel|TechProductivity|IndustryInvestRegionCap|TotalIndustrialization|ProductiveIndustrialization|MeanIndustrialization|OldVintageCapital|StrandedVintage|VintageTechLevel)\b|\.Industrialization\b')

# (ファイルの正規表現, 字面の正規表現, 記号, 説明)
RULES = [
 (r'^Instruments.*\.cs$|^Validation\.cs$|^PlayText\.cs$', r'.', '丙', '計器・検証・表示（既定の出力を変えない）'),
 (r'^Coefficients\.cs$', r'.', '宣言', '係数の宣言'),
 (r'^Model\.cs$', r'.', '宣言', '欄・導出の宣言'),
 (r'^OpeningBake\.cs$', r'.', '丙', '開幕の焼き込み（欄の保存と復元）'),
 (r'^(NationConfig|NationBrief)\.cs$', r'.', '丙', '国の入力データ'),
 (r'^MultiNationSim\.cs$', r'TradeWeight', '丙', '国の入力データ（交易比重）'),
 (r'^Territory\.cs$', r'.', '丙', '診断の文字列'),
 (r'^Simulation\.cs$', r'Industrialization = (0\.0|cfg)', '丙', '開幕の資本の置き場（入力データ）'),
 (r'^Simulation\.cs$', r'scrapKeep', '乙', '書き手（恐慌の廃棄）'),
 (r'^Diplomacy\.cs$', r'rz\.Industrialization', '乙', '書き手（戦場の摩耗）'),
 (r'.', r'IndustrializationOff|indOff', '済', '(495) 旗 IndustrializationOff が削除する読み手'),
 (r'^EconomyStep\.cs$', r'sat = 1\.0 \+ region\.ProductiveIndustrialization', 'A1', 'InvestmentReturn の sat → 旗 InvestReturnFromMargin'),
 (r'^(EconomyStep|World|Megaprojects)\.cs$', r'IndustryInvestRegionCap', 'A2', '州の上限（国家建設のヘッドルーム・国庫の近代化投資の適格と重み・巨大建造物の効き）→ 旗 InvestRegionCapOff'),
 (r'^EconomyStep\.cs$', r'effInd = region\.ProductiveIndustrialization', '済', '(495) 生産性チャネル（機械化度と能力の倍率の入力）＝旗 IndustrializationOff の下で使われない'),
 (r'^World\.cs$', r'cap - r\.TotalIndustrialization', 'A2', '州の上限（国庫の按分の重みの余地の因子）→ 旗 InvestRegionCapOff'),
 (r'^World\.cs$', r'oldDev = ', 'B1', '疫病の衛生の代理 → EpidemicSanitationWeight=1（点火と焼き込みで oldDev を消す）'),
 (r'^EconomyStep\.cs$', r'maintenance = coef\.InfraMaintRate', 'C3', 'インフラ保守（立っている資本の量に比例する資材の需要）'),
 (r'^EconomyStep\.cs$', r'region\.(Industrialization|OldVintageCapital|StrandedVintage|VintageTechLevel)\b.*(\+=|-=|\*=|=)|region\.Cum(State|Private)Industrialization|double (totNow|migrate|newOld|techGap)|privateIndGain|Distribute\(region', '乙', '書き手（購入・減価・移行）と配りの φ の分母'),
 (r'^(FactoryEquipment|Megaprojects)\.cs$', r'TotalIndustrialization|Industrialization|"Industrialization"', '乙', '書き手（配りの φ・巨大建造物の効きの先）'),
 (r'^(PopDecision|Meritocracy)\.cs$', r'.', 'C1', '都市化の天井（BaseUrbanCap＋工業化度×UrbanPerInd）'),
 (r'^Simulation\.cs$', r'avgInd|indPopSum|indPopWeight|TotalIndustrialization', 'C2', '扶養力・出生（avgInd）'),
 (r'^Simulation\.cs$', r'creditMonth|box\.Value|MeanIndustrialization', 'C4', '信用へ渡す MeanInd'),
 (r'^Credit\.cs$', r'.', 'C4', '信用の深さ（devFactor）'),
 (r'^CapitalMarket\.cs$', r'.', 'C5', '外資の吸い寄せ（headroom）'),
 (r'^Discovery\.cs$', r'maxInd', 'C6', '合資会社の必要性（立っている資本の大きさ）'),
 (r'^Discovery\.cs$', r'TechLevel', 'C7', '制度と思想の行の技術力（比＝TechLevel÷難易度）'),
 (r'^Ideology\.cs$', r'.', 'C8', '世界最先端の工業化度（思想の発展度）'),
 (r'^MultiNationSim\.cs$', r'indFrontier', 'C8', '世界最先端の工業化度の注入'),
 (r'^World\.cs$', r'public double MeanIndustrialization|total > 1e-9 \? w / total : Region\.TotalIndustrialization|w \+= p \* r\.TotalIndustrialization', '丙', '導出の平均（人口加重）＝量の定義'),
 (r'^World\.cs$', r'Math\.Min\(TechLevel', 'C9', 'TechProductivity の技術水準の項（pn と同じ腕で消す）'),
 (r'^World\.cs$', r'TechProductivity', 'C9', 'TechProductivity の本体と呼び手'),
 (r'^World\.cs$', r'ModernizationTechLagCost', 'C10', '近代化の技術差の費用（AI 側）'),
 (r'^World\.cs$', r'PatentLaw|TechLevel \{ get|WorldTechLevel \{ get|EraFactor\(WorldTechLevel', '状', '技術水準の状態・先端と時代関数'),
 (r'^Simulation\.cs$', r'TechProductivity|techProd', 'C9', 'TechProductivity の呼び（月次の経済）'),
 (r'^EconomyStep\.cs$', r'productivityMultiplier|TechProductivity', 'C9', '生産性の乗数（TechProductivity）'),
 (r'^TechEducation\.cs$', r'.', '状', '技術水準の状態の更新（GrowTech・AbsorptionOf）'),
 (r'^(Agents|ArmyShape|BilateralMarket|Diplomacy|Megaprojects|SelfPlay)\.cs$', r'TechLevel|Industrialization', 'C10', 'AI の梯子・法の門・技術ゲート・陸軍の質・暴走の監視（AI 側の読み手の置き換えの線）'),
 # (1235) 注：Chokepoint.TechGate は MultiNationSim.cs の写しごと 2026-11-19 に消えた（K101）。この字面は古い木を読むときだけ当たる。
 (r'^MultiNationSim\.cs$', r'TechGate|ExternalPressureGapScale', 'C10', '技術ゲート・外圧（AI 側）'),
 (r'^MultiNationSim\.cs$', r'WorldTechLevel|TechLevel|GrowTech|frontier', '状', '技術水準の同期（先端の運搬・成長の呼び）'),
 (r'^Simulation\.cs$', r'TechLevel|GrowTech', '状', '技術水準の複製と成長の呼び'),
 (r'^(Colonial|Military|Transport|Territory)\.cs$', r'WorldTechLevel', '状', '時代関数（世界の技術先端を時代の係数へ）'),
 (r'^EconomyStep\.cs$', r'TechLevel', '済', '(495) ヴィンテージの座礁（技術水準を読む）＝旗 IndustrializationOff の下'),
]
RULES = [(re.compile(f), re.compile(t), k, d) for f, t, k, d in RULES]


def code_of(line):
    c = line.find('//')
    return line[:c] if c >= 0 else line


def scan(texts):
    out = []
    for name, text in texts:
        for i, line in enumerate(text.splitlines(), 1):
            code = code_of(line)
            if not WORDS.search(code):
                continue
            for fr, tr, k, d in RULES:
                if fr.search(name) and tr.search(code):
                    out.append((name, i, k, d, code.strip()))
                    break
            else:
                out.append((name, i, '未分類', '', code.strip()))
    return out


def base_texts():
    names = subprocess.check_output(['git', '-C', repo, 'ls-tree', '--name-only', base, 'PriceOfBread.SimCore/'], text=True, encoding='utf-8').split('\n')
    res = []
    for n in names:
        if not n.endswith('.cs'):
            continue
        res.append((os.path.basename(n), subprocess.check_output(['git', '-C', repo, 'show', f'{base}:{n}'], encoding='utf-8')))
    return res


def tree_texts():
    return [(os.path.basename(f), open(f, encoding='utf-8').read()) for f in sorted(glob.glob(os.path.join(repo, 'PriceOfBread.SimCore', '*.cs')))]


b = scan(base_texts())
t = scan(tree_texts())
print(f'(505) 線 ir 段0：技術水準・工業化度の読み手の全数表（基点 {base}・現在の木と並べる）')
print('探した語：TechLevel（WorldTechLevel を含む）・TechProductivity・IndustryInvestRegionCap・TotalIndustrialization・ProductiveIndustrialization・MeanIndustrialization・OldVintageCapital・StrandedVintage・VintageTechLevel・.Industrialization')
print('対象：PriceOfBread.SimCore/*.cs の非コメント行（// 以降を除く）。分類は scripts/instruments/ir505/step0.py の規則表（ファイル×字面）で機械的。')
print('記号：A1/A2/B1＝この線が扱う（事前登録 [1]）／C1〜C11＝この回は触らない（行き先と理由は事前登録 [1]）／済＝(495) の旗 IndustrializationOff が削除する／乙＝書き手・物理の量／丙＝計器・入力データ・表示・量の定義／状＝技術水準の状態の更新と運搬／宣言')


def counts(rows):
    c = {}
    for _, _, k, _, _ in rows:
        c[k] = c.get(k, 0) + 1
    return c


print('\n== 件数（記号別・基点／現在の木）==')
cb, ct = counts(b), counts(t)
for k in sorted(set(cb) | set(ct)):
    print(f'  {k}\t{cb.get(k, 0)}\t{ct.get(k, 0)}')
print(f'  合計\t{len(b)}\t{len(t)}')


def per_word(rows, w):
    r = re.compile(w)
    return sum(1 for _, _, _, _, code in rows if r.search(code))


print('\n== 語別の行数（番人のラチェットの基準＝非コメント行・基点／現在の木）==')
for w in [r'\bTechLevel\b', r'\bWorldTechLevel\b', r'\bTechProductivity\b', r'\bIndustryInvestRegionCap\b', r'\bTotalIndustrialization\b', r'\bMeanIndustrialization\b', r'\bProductiveIndustrialization\b', r'\.Industrialization\b']:
    print(f'  {w}\t{per_word(b, w)}\t{per_word(t, w)}')
print('\n== 現在の木の全数（ファイル:行 ｜ 記号 ｜ 説明 ｜ 字面）==')
for name, i, k, d, code in sorted(t, key=lambda x: (x[2] == '丙', x[2], x[0], x[1])):
    print(f'{name}:{i}\t{k}\t{d}\t{code[:200]}')
bad = [x for x in t if x[2] == '未分類']
print(f'\n未分類 {len(bad)} 行')
sys.exit(1 if bad else 0)
