# (605) 線 ind4 段0：工業化度の読み手の全数表（grep ＋ 字面の分類・シミュは回していない）
# 使い方：PYTHONUTF8=1 python -I step0.py <リポジトリの根> [<比べる基点のコミット>] > measurements/ind4_605_20261027_readers.txt
#   探す語：TotalIndustrialization・ProductiveIndustrialization・MeanIndustrialization・IndustrialFrontier・UrbanIndSmoothed・OldVintageCapital・StrandedVintage・
#           `.Industrialization`・`cfg.Industrialization`・`c.Industrialization`・avgInd・indPopSum・IndustryInvestRegionCap・IndustrializationOff
#   対象：PriceOfBread.SimCore/*.cs の非コメント行（// 以降を除く）。分類は (ファイル, 字面) の規則表で機械的に付ける（上から最初に当たった規則）。どの規則にも当たらない行は〈未分類〉。
#   記号：
#     済   ＝(495) の旗 IndustrializationOff が既定 1 で殺した読み（死んだ枝）→ この線の段1 で削除（恒等）。旗そのものは枠と機械の領分の読み手が残るので消さない。
#     旗   ＝この線が既定 0 の旗の下で置き換える読み（置き換え先は盤面に既にある量・新しい数なし）。
#     譲枠 ＝立っている資本の量そのものを読む（保守・州の上限・投資の限界収益・機械化度・書き手・減価）＝枠と機械の線の仕事。置き換え先が今の盤面に無い（設備の Scale は州の資本の中の相対）。
#     譲出 ＝出生の設計（最終段 9-2）の仕事。
#     譲数 ＝置き換えに新しい数（半分点・係数）が要る＝新しい数なしの規則に反する＝この線で作らない（記録）。
#     入力 ＝入力データ（NationConfig の開幕の資本・開幕の置き場）＝状態の読み手ではない。枠と機械が開幕の資本の入力を置き換えるときに一緒に直す。
#     丙   ＝計器・検証・診断・保存と復元・量の定義（既定の出力を変えない）。
#     監   ＝異常の監視（範囲外を報告するだけ）。
#     表示 ＝画面・最終の表・Runner の報告の物差し（この線の段3 で盤面の量へ）。
#     宣言 ＝欄・導出・係数の宣言。
import re, subprocess, sys, glob, os

repo = sys.argv[1]
base = sys.argv[2] if len(sys.argv) > 2 else None
WORDS = re.compile(r'\b(TotalIndustrialization|ProductiveIndustrialization|MeanIndustrialization|IndustrialFrontier|UrbanIndSmoothed|OldVintageCapital|StrandedVintage|IndustryInvestRegionCap|avgInd|indPopSum|indPopWeight)\b|\.Industrialization\b|\bIndustrialization\s*=|\bcfg\.Industrialization\b|\bc\.Industrialization\b')

RULES = [
 (r'^Coefficients\.cs$', r'.', '宣言', '係数の宣言'),
 (r'^Model\.cs$', r'.', '宣言', '欄・導出の宣言'),
 (r'^Instruments.*\.cs$|^Validation\.cs$', r'.', '丙', '計器・検証（既定の出力を変えない）'),
 (r'^OpeningBake\.cs$', r'.', '丙', '開幕の焼き込み（欄の保存と復元）'),
 (r'^NationConfig\.cs$', r'.', '入力', '国の入力データ（開幕の資本）'),
 (r'^NationBrief\.cs$', r'.', '入力', '国の概要の操作量に入力データ c.Industrialization を数える＝画面の推奨の並びだけ・力学に効かない'),
 (r'^Territory\.cs$', r'.', '丙', '診断の文字列'),
 (r'^Simulation\.cs$', r'Industrialization = (0\.0|cfg)', '入力', '開幕の資本の置き場（入力データ）'),
 (r'^Simulation\.cs$', r'scrapKeep', '譲枠', '書き手（恐慌の廃棄）'),
 (r'^Diplomacy\.cs$', r'rz\.Industrialization', '譲枠', '書き手（戦場の摩耗）'),
 (r'^(EconomyStep|Megaprojects|FactoryEquipment)\.cs$', r'IndustrializationOff|indOff', '済', '(495) 旗 IndustrializationOff が既定 1 で殺した読み（EconomyStep 769〜796 の機械化度・ヴィンテージは枠と機械の領分＝旗ごと残す）'),
 (r'^MarketFlow\.cs$', r'IndustrializationOff', '済', '(495) 賃金の天井の工業化度の項（既定 1 で 0）＝段1 で項と係数 WageClampIndScale を削除'),
 (r'^EconomyStep\.cs$', r'MonopolyDissolveInd|Industrialization / Math\.Max', '済', '独占の解体の工業化度の項（既定 1 で 1.0）＝段1 で項と係数 MonopolyDissolveInd を削除'),
 (r'^EconomyStep\.cs$', r'RefrigerationInd', '済', '冷蔵の傾斜（既定 1 で 1.0）＝段1 で傾斜と係数 RefrigerationIndOnset／Span を削除'),
 (r'^EconomyStep\.cs$', r'effInd = region\.ProductiveIndustrialization', '譲枠', '機械化度と能力の倍率の入力（旗 IndustrializationOff の下で使われない）'),
 (r'^EconomyStep\.cs$', r'sat = 1\.0 \+ region\.ProductiveIndustrialization', '譲枠', '投資の限界収益（InvestmentReturn の sat）'),
 (r'^EconomyStep\.cs$', r'maintenance = coef\.InfraMaintRate', '譲枠', 'インフラ保守（立っている資本の量に比例する資材の需要）＝置き換え先は枠の損耗'),
 (r'^EconomyStep\.cs$', r'region\.(Industrialization|OldVintageCapital|StrandedVintage|VintageTechLevel)\b|region\.Cum(State|Private)Industrialization|double (totNow|migrate|newOld|techGap)|privateIndGain|Distribute\(region', '譲枠', '書き手（購入・減価・移行）と配りの φ の分母'),
 (r'^(FactoryEquipment|Megaprojects)\.cs$', r'.', '譲枠', '書き手（配りの φ・巨大建造物の効きの先）と州の上限'),
 (r'^World\.cs$', r'oldDev = ', '旗', '疫病の衛生の代理（工業化度）→ 旗 EpidemicSanitFromBoard＝実衛生財 Region.Sanitation だけ'),
 (r'^World\.cs$', r'IndustryInvestRegionCap|cap - r\.TotalIndustrialization', '譲枠', '州の上限（国庫の近代化投資の適格と重みの余地）'),
 (r'^World\.cs$', r'IndustrialFrontier', '旗', '世界最先端の工業化度の欄（思想の門が読む）→ 旗 IdeologyDevFromUrban で注入も読みも止まる'),
 (r'^World\.cs$', r'public double MeanIndustrialization|total > 1e-9 \? w / total : Region\.TotalIndustrialization|w \+= p \* r\.TotalIndustrialization|foreach \(var r in Regions\) \{ double p = r\.SumPopulation', '丙', '導出の平均（人口加重）＝量の定義'),
 (r'^PopDecision\.cs$|^Meritocracy\.cs$', r'.', '旗', '都市化の天井（BaseUrbanCap＋工業化度×UrbanPerInd）→ 旗 UrbanCapOff（旧名 UrbanCapFromHousing・★(609) 合流で名を合わせた）＝天井を外す（住居の摩擦は足していない）'),
 (r'^Simulation\.cs$', r'avgInd \+= r\.TotalIndustrialization|indPopSum \+=|avgInd \+= c\.Industrializ|double avgInd|double indPopSum|if \(actN > 0\) avgInd|indPopWeighted', '譲出', '扶養力と出生の工業化度の平均 avgInd＝旗 CapacityIndTermsOff は扶養力の 2 項だけ外す・出生の項 FertilityIncomeSlope×avgInd は出生の設計へ'),
 (r'^Simulation\.cs$', r'capAvgInd', '旗', '扶養力の工業化度の 2 項の入力 → 旗 CapacityIndTermsOff で 0'),
 (r'^Simulation\.cs$', r'FertilityIncomeSlope \* avgInd', '譲出', '出生の工業化度の項＝出生の設計（最終段 9-2）の持ち物'),
 (r'^Simulation\.cs$', r'creditMonth|box\.Value|MeanIndustrialization|MeanInd\b', '旗', '信用へ渡す MeanInd → 旗 CreditDevOff で計算もしない'),
 (r'^Credit\.cs$', r'.', '旗', '信用の深さ devFactor（工業化度の倍率）→ 旗 CreditDevOff＝倍率を消す（作者 2026-10-16 バフ／デバフ全削除）'),
 (r'^CapitalMarket\.cs$', r'.', '旗', '外資の余地 headroom＝CapitalIndTarget−MeanInd → 旗 CapitalHeadroomFromReturn＝CapitalIndTarget×ModernizeReturnShare()'),
 (r'^Discovery\.cs$', r'maxInd|TotalIndustrialization', '譲数', '株式会社の必要性＝州の最大工業化度。置き換え（工場労働者の割合）は半分点 DiscoveryNeedHalfInstitution=0.5 に届かず新しい数が要る'),
 (r'^Ideology\.cs$', r'.', '旗', '思想の発展度 dev の世界最先端の工業化度 → 旗 IdeologyDevFromUrban＝都市労働者の割合だけ'),
 (r'^MultiNationSim\.cs$', r'indFrontier|IndustrialFrontier', '旗', '世界最先端の工業化度の注入 → 旗 IdeologyDevFromUrban で注入しない'),
 (r'^MultiNationSim\.cs$', r'TradeWeightIndustrializationSlope|cfg\.Industrialization', '旗', '開幕の交易の比重＝入力データ cfg.Industrialization → 旗 TradeWeightOpeningEven＝世界の平均の比重（動的な式の相対 1）'),
 (r'^PlayText\.cs$', r'.', '表示', '最終の表の工業化度 → 工場の産出の世界の割合'),
 (r'^SelfPlay\.cs$', r'is >= 0 and <= 10|工業化度が異常', '監', '暴走の監視（範囲外を報告するだけ）'),
 (r'^SelfPlay\.cs$', r'w\.MeanIndustrialization', '丙', '成績表の行（NationStanding の欄・成績表の列を変えない）'),
 (r'^SelfPlay\.cs$', r'IndustrializationRoomTarget|IndustrializeInvest|Stance\.Industrialize|enum Stance|string Name, double Score', '丙', 'AI の姿勢の名前・目盛り（AI は工業化度を読まない＝(416)(591)(599) で消えた）'),
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


t = scan(tree_texts())
b = scan(base_texts()) if base else None
print('(605) 線 ind4 段0：工業化度の読み手の全数表' + (f'（基点 {base} と現在の木を並べる）' if base else '（現在の木）'))
print('探した語：TotalIndustrialization・ProductiveIndustrialization・MeanIndustrialization・IndustrialFrontier・UrbanIndSmoothed・OldVintageCapital・StrandedVintage・.Industrialization・IndustryInvestRegionCap・avgInd')
print('対象：PriceOfBread.SimCore/*.cs の非コメント行（// 以降を除く）。分類は scripts/instruments/ind4/step0.py の規則表（ファイル×字面）で機械的。技術水準（TechLevel）の読み手は別の線＝この表に無い。')


def counts(rows):
    c = {}
    for _, _, k, _, _ in rows:
        c[k] = c.get(k, 0) + 1
    return c


print('\n== 件数（記号別' + ('・基点／現在の木' if b is not None else '') + '）==')
ct = counts(t)
cb = counts(b) if b is not None else None
for k in sorted(set(ct) | set(cb or {})):
    print(f'  {k}\t' + (f'{cb.get(k, 0)}\t' if cb is not None else '') + f'{ct.get(k, 0)}')
print('  合計\t' + (f'{len(b)}\t' if b is not None else '') + f'{len(t)}')
print('\n== 語別の行数（番人の許可表の基準＝非コメント行）==')
for w in [r'\bTotalIndustrialization\b', r'\bMeanIndustrialization\b', r'\bProductiveIndustrialization\b', r'\bIndustrialFrontier\b', r'\.Industrialization\b', r'\bUrbanIndSmoothed\b', r'\bavgInd\b']:
    r = re.compile(w)
    print(f'  {w}\t' + (f'{sum(1 for x in b if r.search(x[4]))}\t' if b is not None else '') + f'{sum(1 for x in t if r.search(x[4]))}')
print('\n== 現在の木の全数（ファイル:行 ｜ 記号 ｜ 説明 ｜ 字面）==')
for name, i, k, d, code in sorted(t, key=lambda x: (x[2] == '未分類', x[0], x[1])):
    print(f'{name}:{i}\t{k}\t{d}\t{code[:200]}')
