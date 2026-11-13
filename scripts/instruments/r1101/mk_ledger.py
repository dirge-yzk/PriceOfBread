# (1103) 合流の直し（反証役 v の中・低）：H035 の最初の年の切り出し（文字列の [:3] → 値の 3 つ）・H012 を事前登録 P10 どおり〈未確かめ（列なし）〉へ戻す・H038 に段 R の印・H025（K4）に倍率の一掃 B の印。K の名は写した後に S08-K へ改名する。
# (1101) c105s08：S08 の台帳の行（12 列の TSV）を作る。数は out_freq_*.txt・out_extra_*.txt から写す（手で書かない）。
# 錨は木（main の作業ツリー・fff0f1658）を名で引き直す。呼び方：python -X utf8 -I mk_ledger.py <repo> <出力の TSV>
import os, re, sys
repo, outp = sys.argv[1], sys.argv[2]
W = os.path.dirname(os.path.abspath(__file__))
TREE = 'fff0f1658'
ARMS = ['M', 'N1', 'N2', 'S11']

def load(prefix):
    d = {}
    for a in ARMS:
        p = os.path.join(W, f'{prefix}_{a}.txt')
        if not os.path.exists(p): continue
        for line in open(p, encoding='utf-8'):
            c = line.rstrip('\n').split('\t')
            if len(c) >= 3: d.setdefault((c[0], c[1]), {})[a] = c[2:]
    return d
F = load('out_freq'); X = load('out_extra')

def num(s):
    return float(s.rstrip('%'))

def rng(key, idx=0, src=None):
    src = src or F
    v = {a: x for a, x in src[key].items() if x[idx] != 'n/a'}
    xs = [num(v[a][idx]) for a in ARMS if a in v]
    pc = '%' if v[next(iter(v))][idx].endswith('%') else ''
    arms = '' if len(xs) == 4 else '（' + '・'.join(a for a in ARMS if a in v) + ' だけ）'
    lo, hi = min(xs), max(xs)
    return (f'{lo:g}{pc}' if lo == hi else f'{lo:g}〜{hi:g}{pc}') + arms

def raw(key, idx=0, arm='M', src=None):
    return (src or F)[key][arm][idx]

SRC = os.path.join(repo, 'PriceOfBread.SimCore')
_cache = {}
def anchor(fname, name):
    p = os.path.join(SRC, fname)
    if p not in _cache: _cache[p] = open(p, encoding='utf-8-sig').read().split('\n')
    L = _cache[p]; pat = re.compile(r'(?<![A-Za-z0-9_])' + re.escape(name) + r'(?![A-Za-z0-9_])')
    best = None
    for i, s in enumerate(L):
        t = s.strip()
        if t.startswith('//') or t.startswith('///') or not pat.search(s): continue
        if fname == 'Coefficients.cs' and 'public' not in s: continue
        best = i + 1; break
    if best is None: raise SystemExit(f'錨が引けない {fname}:{name}')
    return f'{fname}:{best}:{name}'

def A(*pairs):
    return ';'.join(anchor(f, n) for f, n in pairs)

RMARK = '〈段 R の合流の後に確かめ直す〉'
OV = lambda cls: f'〈倍率の一掃の分類 {cls} と重なる＝二重に線を作らない〉'

fr = lambda k: rng(('flow', k))
rows = []
def row(i, func, exp, anc, owners, typ, verdict, basis, rest, fix):
    rows.append([i, 'S08', func, exp, anc, owners, typ, TREE, verdict, basis, rest, fix])

def canal_lag():
    lags = []
    for a in ARMS:
        for cid in ('suez', 'panama'):
            for t in X[('canalLag', cid)][a][1].split(' '):
                d = t.split(':')[3]
                if d != 'na': lags.append(int(d))
    return f'{min(lags)}〜{max(lags)} 四半期（開いた {len(lags)} 件）'
def canal_years(cid):
    ys = []; never = []
    for k, v in F.items():
        if k[0] == 'H022' and k[1].startswith('canal_s'):
            for a, val in v.items():
                for t in val[0].split(' '):
                    c, y = t.split(':')
                    if c == cid:
                        (never if y == '-1' else ys).append(a + ':' + k[1][7:] if y == '-1' else int(y))
    return f"{min(ys)}〜{max(ys)}" + (f"（未開通 {' '.join(never)}）" if never else '')
def canal_open_count():
    op = tot = 0
    for k, v in F.items():
        if k[0] == 'H022' and k[1].startswith('canal_s'):
            for a, val in v.items():
                for tk in val[0].split(' '):
                    tot += 1; op += tk.split(':')[1] != '-1'
    return f'{op}／{tot}'
def nrows(key, idx=2):
    v = F[key]; xs = [int(v[a][idx]) for a in ARMS if a in v]
    return f'{min(xs)}〜{max(xs)} 行'

row('S08-H001', '実距離（陸→港→海→港→陸）', '海路＋両端の陸脚×地形×陸海比（L1347）', A(('Coefficients.cs', 'LandSeaCostRatio'), ('BilateralMarket.cs', 'PairKmCore')), 'BilateralMarket.cs SeaGraph.cs Geo.cs', '効き', '働く',
    f"trroute（10 年ごとの上位 3000 組）：海経由の額のうち陸脚＞0 は {rng(('H001','seaRouteValueShareWithLandLeg'))}。式 BilateralMarket.cs:97", RMARK, '—')
row('S08-H002', '陸路の直行', '陸グラフの最短路と海経由の安い方（L1352）', A(('Coefficients.cs', 'LandRouteEnabled'), ('BilateralMarket.cs', 'LandMatrix')), 'BilateralMarket.cs PathGraph.cs', '頻度', '働く',
    f"trbin：陸直行（routeCode 0）の額 国内 {rng(('H002','landDirectValueShare_cross0'))}・越境 {rng(('H002','landDirectValueShare_cross1'))}（越境 1850 年より前 {rng(('H002','landDirectValueShare_cross1_pre1850'))}）。routeCode 0 の海の km の和 {rng(('H002','check_routeCode0_seaKmSum'))}（向きの確かめ）", RMARK + '・LandRouteEnabled は既定 1 の旗＝焼き込みの候補（恒等）', '残す（旗の焼き込みは恒等の線）')
row('S08-H003', '地形の輸送コスト', '州の地形の札が陸脚の費用に効く・馬と鉄道で効き方が違う（L1357）', A(('Coefficients.cs', 'TerrainHorseCost'), ('Coefficients.cs', 'TerrainRailCost'), ('Coefficients.cs', 'TerrainWaterMajorCost'), ('Coefficients.cs', 'TerrainWaterMinorCost')), 'BilateralMarket.cs ArmyShape.cs Places.cs places.json', '効き', '働く（両端の州だけ）',
    f"trroute：両端のどちらかの地形の係数≠1 の額 {rng(('H003','valueShareTerrainCostNot1'))}。陸直行は両端の係数の平均だけ（BilateralMarket.cs:108）＝途中の地形は読まない。売り手の地形の額の内訳（M）：{raw(('H003','sellerTerrainCodeValueShare'))}（0 平地・1 大河・2 支流・3 丘・4 森・5 山・6 荒地）", RMARK + '・峠の線（陸の行列に地形を入れるか）', '残す（途中の地形は峠の線で決める）')
row('S08-H004', '鉄道時代の陸運低下', '陸海の比が 12 から 3 へ〈連続に下がる〉（L1363）', A(('Coefficients.cs', 'RailCostFraction'), ('BilateralMarket.cs', 'RailTransport')), 'BilateralMarket.cs Discovery.cs', '頻度', '働く（2 値）・文書が古い',
    f"機関車の発見の 0/1（BilateralMarket.cs:345〜346）で 12→3 の段＝連続ではない。trroute の売り手側の陸÷海の比の額：{raw(('H004','landSeaRatioSellerValueShare'))}（M）・3.0 の額 4 腕で {', '.join(raw(('H004','landSeaRatioSellerValueShare'), arm=a).split(' ')[0] for a in ARMS)}", RMARK + '・' + OV('B（LandSeaCostRatio・RailCostFraction）') + '＝段 R が φ で混ぜる', '直す（段 R）・文書')
row('S08-H005', '海路（海のグラフの最短路）', '177 地点と 12 通路のグラフ（L1368）', A(('SeaGraph.cs', 'SeaDistanceKmFromNodes')), 'SeaGraph.cs sea_nodes.json passages.json', '不変条件', '働く（静的）',
    'sea_nodes.json 177 行・8 海盆（Atlantic 65・Pacific 54・IndianOcean 28・Mediterranean 17・Baltic 4・RedSea 4・BlackSea 3・Caspian 2）・passages.json 12 行。試験 SeaGraphTests 5 件', '—', '—')
row('S08-H006', '海に出る口（接ぎ木）', '港は陸を越えずに行ける海ノードへ・免除に距離の予算（L1372）', A(('Coefficients.cs', 'SeaStitchLeadBudgetMult'), ('SeaGraph.cs', 'LeadBudgetMult')), 'SeaGraph.cs', '番人', '働く（静的）',
    'SeaGraph.cs:361 は new Coefficients() から 1 回読む＝国ごとの係数の上書きは効かない（静的・記録）。試験 SeaStitchBudgetTests 6 件', '—', '—')
row('S08-H007', '陸のグラフ（州・植民地・バリア・峠）', 'バリア 67・峠 12・k=6（L1377）', A(('PathGraph.cs', 'K'), ('PathGraph.cs', 'BuildEdges')), 'PathGraph.cs barriers.json landmass.json', '不変条件', '働く（静的）・文書が古い',
    'barriers.json は barrier 67・pass 16（文書は峠 12）＝(762)(769) の fix の行で足した峠を含む。試験 PathGraphTests 5・LandGraphFixTests 5', '峠の線（作者 2026-11-12「峠は削除する方向」）', '直す（文書の件数）')
row('S08-H008', '陸塊の判定', '全地点に陸塊の番号（L1381）', A(('Landmass.cs', 'Landmass')), 'Landmass.cs landmass.json Diplomacy.cs PathGraph.cs', '不変条件', '働く（静的）・文書が古い',
    'landmass.json 400 行（文書は 394 点）。試験 LandmassTests 3 件', '—', '直す（文書の件数）')
row('S08-H009', '陸のマスクは2枚', '海路用と陸路用（L1385）', A(('Landmask.cs', 'Landmask')), 'Landmask.cs', '不変条件', '働く（ソースだけ）', 'Landmask.cs の 2 枚の読み込み（列なし・静的）', '—', '—')
row('S08-H010', '水域の回避と沿岸の迂回', '陸の辺は水を渡らない・沿岸の迂回（L1389）', A(('PathGraph.cs', 'CrossesWater')), 'PathGraph.cs', '不変条件', '働く（ソースだけ）', '旗 LandEdgesAvoidWater は 2026-10-18 に焼き込み（文書の旗の行は墓標）。PathGraph.cs:260・:298・:328', '—', '—')
row('S08-H011', '山脈の壁（点でなく線分）', '同じ山脈の点を 700km 以内で結ぶ線分（L1394）', A(('PathGraph.cs', 'BarrierClearanceKm'), ('Coefficients.cs', 'WallLinkKm'), ('PathGraph.cs', 'CrossesWall')), 'PathGraph.cs barriers.json', '不変条件', '働く（ソースだけ）',
    '辺の選びで 3 か所（PathGraph.cs:259〜:260・:298・:328）。バリア点は海の辺の棄却にも入る（SeaGraph.cs:79）', '峠の線（作者 2026-11-12「峠は削除する方向」）', '消す（峠の線・挙動）')
row('S08-H012', '峠', '峠を通る辺は割増・峠の側からも近いときだけ結ぶ（L1399）', A(('PathGraph.cs', 'PassCostMult')), 'PathGraph.cs ArmyShape.cs Diplomacy.cs barriers.json', '頻度', '未確かめ（列なし）',
    '峠を通る辺・経路の列なし。読み手：PathGraph.cs:240・:275・:342（×1.5）・:489（壁の免除）・ArmyShape.cs:181〜:182・:225（国境の組×1/1.5）・Diplomacy.cs:3548（割譲の隣）・LandMatrix 経由で BilateralMarket.cs:105・:503・Geo.cs:86・MultiNationSim.cs:224。試験 PathGraphTests〈ヒマラヤは直行できず峠を通る〉', '峠の線（作者 2026-11-12「峠は削除する方向」）・計器 passuse の後', '消す（峠の線・挙動・素案は ledger_S08 §4）')
row('S08-H013', '海盆', '8 海盆・港州は最寄りの海ノードから継ぐ（L1404）', A(('Model.cs', 'Basins')), 'Model.cs SeaGraph.cs Simulation.cs sea_nodes.json', '不変条件', '働く（静的データ）', 'sea_nodes.json の basin 8 種（H005 の内訳）', '—', '—')
row('S08-H014', '輸送の楔（従価）', '荷の値段に比例・距離で上がる・誰にも払われない（L1408）', A(('Coefficients.cs', 'TransportMargin')), 'BilateralMarket.cs Discovery.cs SelfPlay.cs', '効き', '働く（ソースだけ）',
    '清算の行ごとに baseMul＝1＋TransportMargin×adValoremRev（BilateralMarket.cs:906）。adValoremRev は恒等の 1（H016）＝その掛け算は働かない', '—', '—')
row('S08-H015', '重量による楔（従量）', '重くて安い物は遠くへ運べない（L1413）', A(('Coefficients.cs', 'TransportWeightMarginScale')), 'BilateralMarket.cs Catalog.cs', '効き', '働く（ソースだけ）', 'WeightMarginAdd（BilateralMarket.cs:598〜:603）。×freightRevolution は恒等の 1', '—', '—')
row('S08-H016', '運賃革命', '前線技術で楔が半分まで下がる（L1418）', A(('Coefficients.cs', 'TransportRevolutionMax'), ('Coefficients.cs', 'TransportRevolutionOnset'), ('Coefficients.cs', 'TransportRevolutionSpan'), ('InternationalMarket.cs', 'FreightRevolution')), 'BilateralMarket.cs InternationalMarket.cs DiscoveryTables.cs', '効き', '働かない・文書が古い',
    f"InternationalMarket.cs:178〜:179＝常に 1.0（(843) で焼き込み）。scalars_s の freightRevolution≠1 の行：{rng(('H016','freightRevolutionNot1Rows'))}（4 腕・{nrows(('H016','freightRevolutionNot1Rows'))}）。BilateralMarket.cs:590〜:591 は清算ごとに国の数だけ前線技術を数えて捨てる", OV('I（TransportRevolutionMax）'), '消す（K1・恒等の線・App の GameViewModel.News.cs:292 が Onset/Span を読む）')
row('S08-H017', '運賃は海運国が受け取る', '運賃は船腹の大きい国へ按分（L1423）', A(('Coefficients.cs', 'FreightRatePer10Mm')), 'BilateralMarket.cs', '頻度', '一部だけ働く',
    f"受け取り＞0 の国×年 {rng(('H017','freightReceivedPosNationYearShare'))}・9 国の運賃の書き手（BilateralMarket.cs:1209）{fr('H017_freightToShipOwners')}。受け取りの上位（M）：{raw(('freight','nation:recvShare:paidShare:recvOverPaid'), src=X)}（国:受け取りの取り分:払いの取り分:受け取り÷払い）。英は受け取り {', '.join(raw(('freight','イギリス'), arm=a, src=X) for a in ARMS)}（4 腕）＝狙い〈海運国〉と逆。根の候補（推測）：船の目標が脅威と混雑の項を含む同じ target（Transport.cs:15〜:31・:44）・船腹を ColonialShipBurden で割る（BilateralMarket.cs:581）", RMARK + '（線路の運賃の受け手）・計器 shipcap_s', '直す（根を計器で分けてから・挙動の線）')
row('S08-H018', '商人の仲介料', '越境の取扱高×率を商人へ（L1428）', A(('Coefficients.cs', 'MerchantTradeMargin')), 'BilateralMarket.cs InternationalMarket.cs', '頻度', '働く', f"9 国×四半期の {fr('H018_merchantCommission')}（BilateralMarket.cs:1172）", '—', '—')
row('S08-H019', '海上保険', '保険料は成約に上乗せ・封鎖の損に給付（L1433）', A(('Coefficients.cs', 'MarinePremiumRate'), ('Coefficients.cs', 'InsurancePayoutFrac'), ('World.cs', 'LastBlockadeRisk')), 'BilateralMarket.cs MultiNationSim.cs World.cs', '読まれない欄', '一部だけ働く',
    f"保険料（BilateralMarket.cs:1215）9 国×四半期の {fr('H019_premium')}。給付（MultiNationSim.cs:1525）{fr('H019_insurancePayout')}。monlaw_s の blockadeRiskShare＞0 の行 {rng(('H019','blockadeRiskSharePosRows'))}（4 腕・{nrows(('H019','blockadeRiskSharePosRows'))}）＝World.cs:1329 LastBlockadeRisk に書き手が無い。同じ欄を Diplomacy.cs:799（拿捕の損）・:2553（封鎖の厭戦）も読む＝S24 へ", '関連：S24（封鎖の厭戦と拿捕）', '消す（給付の分 K3・恒等）か直す（封鎖の危険の書き手）＝Q-s08-2')
row('S08-H020', '通路（海峡と運河）', '12 本の手置きの通路（L1438）', A(('SeaGraph.cs', 'PassageRow')), 'SeaGraph.cs passages.json', '不変条件', '働く（静的）', 'passages.json 12 行（文書の 12 本と名が一致）', '—', '—')
row('S08-H021', '咽喉と扼者', '8 咽喉の扼者は征服で替わる（L1442）', A(('MultiNationSim.cs', 'ResolveOwner')), 'Diplomacy.cs MultiNationSim.cs world.json', '頻度', '未確かめ（列なし）', '毎四半期 ResolveOwner（MultiNationSim.cs:1352）。扼者の替わりの列なし＝計器 choke_s', '計器 choke_s', '—')
row('S08-H022', '運河の建設（スエズ・パナマ）', '資本家の拠出が総額に達すると開通（L1446）', A(('Coefficients.cs', 'CanalInvestFrac'), ('Megaprojects.cs', 'RequirementMet')), 'MultiNationSim.cs Megaprojects.cs world.json', '頻度', '一部だけ働く',
    f"開通は要る発見（汽船・ダイナマイト）の最初の四半期から {canal_lag()}（out_extra_*.txt）＝総額（150,000・400,000）が律速にならない。拠出の行は 9 国×四半期の {fr('H022_canalInvest')}。開通の年（史実は記録）：スエズ {canal_years('suez')}・パナマ {canal_years('panama')}", '—', '消す候補 K5（拠出と配当の段）＝Q-s08-3')
row('S08-H023', '運河の通行料と出資配当', '海路が縮む国だけ課金・配当は出資の持分で（L1451）', A(('Coefficients.cs', 'CanalTollTrafficRate'), ('Coefficients.cs', 'CanalInvestReturns'), ('MultiNationSim.cs', 'DistributeCanalDividend')), 'MultiNationSim.cs', '頻度', '一部だけ働く',
    f"配当（MultiNationSim.cs:1447）9 国×四半期の {fr('H023_canalDividend')}・最初の四半期 {rng(('flow','H023_canalDividend_firstQuarter'))}。通行料の国庫→国庫は列なし", '—', '消す候補 K5＝Q-s08-3')
row('S08-H024', '運河の封鎖', '扼者と交戦中の国は運河が閉じた扱い（L1458）', A(('BilateralMarket.cs', 'Blocked')), 'BilateralMarket.cs SeaGraph.cs', '頻度', '未確かめ（列なし）', 'BilateralMarket.cs:234・:253〜:254。交戦×扼者の列なし＝計器 choke_s', RMARK + '・計器 choke_s', '—')
row('S08-H025', '海の出口を握られる', '閉鎖海盆の出口の扼者へ緊張（L1462）', A(('Coefficients.cs', 'RouteDependenceTensionPerMonth'), ('Diplomacy.cs', 'void RouteTension')), 'Diplomacy.cs MultiNationSim.cs', '頻度', '未確かめ（列なし）', '台帳の〈消えた係数 RouteDependenceTensionRate〉は改名（今 RouteDependenceTensionPerMonth）。緊張の理由ごとの列なし。試験 RouteTensionTests 2 件', '計器（緊張の理由ごとの和）', '消す候補 K8＝Q-s08-3')
row('S08-H026', '港湾の建設・減耗・港湾料', '混雑した港州で資本家と国庫が港を広げる（L1467）', A(('Coefficients.cs', 'PortCongestionTarget'), ('Coefficients.cs', 'PortDecay'), ('Coefficients.cs', 'PortBuildCostPerUnit'), ('Coefficients.cs', 'PortFeeRate')), 'MultiNationSim.cs BilateralMarket.cs World.cs', '頻度', '働く',
    f"port_s（9 国の港の州×四半期）：建てた {rng(('H026','builtCapacityPosShare'))}・建設の門 {rng(('H026','builtCapacityPosShare'), idx=2)}。投資家の払い（MultiNationSim.cs:1493）9 国×四半期の {fr('H026_portInvestCap')}・港湾料（BilateralMarket.cs:1213）{fr('H026_portFee')}", OV('A（PortCongestionTarget＝輸送の線で）'), '—')
row('S08-H027', '港の取扱高（四半期の累計）', '海路の成約の額を両端の港へ積む（L1472）', A(('BilateralMarket.cs', 'LastPortThroughput')), 'BilateralMarket.cs Model.cs', '頻度', '働く', f"port_s の取扱高＞0 {rng(('H027','portThroughputPosShare'))}（四半期の終わりの断面）", '—', '—')
row('S08-H028', '凍結港', '高緯度の閉じた海の港は冬に使えない（L1476）', A(('BilateralMarket.cs', 'PortFrozen')), 'BilateralMarket.cs Climate.cs', '頻度', '一部だけ働く（近似・定義どおり）',
    f"rulepower_s：凍結を外した支配の届き≠今の届きの行 {rng(('H028','rhoNoFreezeNeRhoNewShare'))}（近似＝支配の届きの距離への効きだけ・ヌル腕と S11 は列なし）。BilateralMarket.cs:306。旗 WinterPortFreeze は焼き込み", '計器 frozen 列（清算の州×四半期）', '残す')
row('S08-H029', '港の配線は領土に追随する', '毎四半期、盤面から港を張り直す（L1481）', A(('Simulation.cs', 'NearestPortName')), 'MultiNationSim.cs Simulation.cs', '不変条件', '働く（ソース）', f"portch_s：陸路で選んだ港≠大圏の最寄りの州×年 {rng(('H029','portUsedNeGcShare'))}（参考）。試験 PortRewireTests 2・PortByLandTests 14", '—', '—')
row('S08-H030', '内陸州は港が遠い', '最寄り港までの距離で鉄道容量が減衰（L1486）', A(('BilateralMarket.cs', 'InlandRailAfterDecay')), 'BilateralMarket.cs Simulation.cs', '効き', '働く・文書が古い',
    f"portch_s の内陸の行で減衰＜1 {rng(('H030','railDecayUsedLt1Share'))}。係数 InlandPortDistanceDecay は (804) で削除（文書の変数の行は古い）", RMARK, '直す（文書）')
row('S08-H031', '内陸国の通過港と通行料', '港の無い国は外国の港を借り通行料（L1491）', A(('Coefficients.cs', 'TransitTollShare')), 'BilateralMarket.cs MultiNationSim.cs', '頻度', '未確かめ（列なし）', '通行料は国庫→国庫（BilateralMarket.cs:1269）＝flow_s に出ない。試験 TransitTests 5 件', '計器（通過港の行）', '—')
row('S08-H032', '輸送容量（鉄道と船）', '投資家と商人が線路・機関車・船を買って容量に（L1496）', A(('Coefficients.cs', 'TransportDecay'), ('Coefficients.cs', 'TransportCapPerUnit'), ('Coefficients.cs', 'TransportBuildRate'), ('Coefficients.cs', 'ShipTier2CapMult'), ('Coefficients.cs', 'EraFactor')), 'Transport.cs Colonial.cs Discovery.cs', '効き', '一部だけ働く・文書が古い',
    f"stock_s：鉄道の容量＞0 {rng(('H032','railCapacityPosShare'))}・汽船の充足＞0 {rng(('H032','railCapacityPosShare'), idx=2)}。汽船の〈時代とともに上がる〉÷EraFactor は Coefficients.cs:3483 で常に 1＝働かない", RMARK + '・' + OV('I（EraFactor の残骸）'), '直す（文書）・EraFactor は倍率の一掃 I の線')
row('S08-H033', '基礎輸送は陸（道路と川）', '人口×基礎の目標（L1503）', A(('Transport.cs', 'PreRailCapacity'), ('Coefficients.cs', 'TransportTargetPerPop')), 'BilateralMarket.cs Transport.cs', '効き', '働く（ソースだけ）', 'BilateralMarket.cs:264（鉄道の容量＋人口×TransportTargetPerPop）', RMARK + '・' + OV('A（TransportTargetPerPop＝輸送の線で）'), '—')
row('S08-H034', '目標容量（人口・混雑・脅威）', '人口比例×混雑×脅威（L1508）', A(('Coefficients.cs', 'ThreatRailTargetWeight'), ('Coefficients.cs', 'TransportCongestionWeight')), 'Transport.cs', '効き', '未確かめ（列なし）', '目標の内訳の列なし。同じ target が船の買いにも入る（Transport.cs:44）＝H017 の根の候補（推測）', RMARK + '・' + OV('A（ThreatRailTargetWeight・TransportCongestionWeight＝輸送の線で）') + '・計器 shipcap_s', '直す（輸送の線）')
row('S08-H035', '鉄道の技術解禁', '機関車の発見で解禁（L1513）', A(('Discovery.cs', 'RailTransport')), 'Transport.cs Discovery.cs', '頻度', '働く',
    f"trnation：解禁の国×年 {rng(('H035','railGateNationYearShare'))}。最初の年（国×種）：最小〜中央〜最大 M {' '.join(F[('H035','firstRailYear_min_med_max')]['M'][:3])}・N1 {' '.join(F[('H035','firstRailYear_min_med_max')]['N1'][:3])}・N2 {' '.join(F[('H035','firstRailYear_min_med_max')]['N2'][:3])}・S11 {' '.join(F[('H035','firstRailYear_min_med_max')]['S11'][:3])}（史実の年は記録）", RMARK, '—')
row('S08-H036', '容量を州へ配る', '前期に運びたかった量に比例（L1518）', A(('Transport.cs', 'RegionShare')), 'BilateralMarket.cs Transport.cs Territory.cs', '頻度', '働く', f"port_s：潜在量＞0 の港の州×四半期 {rng(('H036','tradePotentialPosShare'))}（内陸州は列なし）", RMARK + '（割譲の移し Transport.Receive も）', '—')
row('S08-H037', '輸送容量は同時制約', '容量は取引ごとに両端から引く（L1523）', A(('BilateralMarket.cs', 'capLeft')), 'BilateralMarket.cs', '頻度', '働く', f"port_s：律速＜1 の港の州×四半期 {rng(('H037','throttleLt1Share'))}・その中央 {rng(('H037','throttleLt1Share'), idx=2)}", RMARK, '—')
row('S08-H038', '植民地補給が海運を食う', '遠い植民地ほど海運を占有（L1528）', A(('Coefficients.cs', 'ColonyShipOccupyPerDist'), ('Colonial.cs', 'double OccupiedShipCapacity')), 'Colonial.cs BilateralMarket.cs', '効き', '未確かめ（列なし）', 'ColonialShipBurden の列なし。BilateralMarket.cs:265・:581 が船腹を割る＝H017 の根の候補（推測）', RMARK + '・' + OV('A（ColonyShipOccupyPerDist）') + '・計器 shipcap_s', '—')
row('S08-39', '封鎖の危険の書き手（新しい行）', '（文書なし・H019 の入力）LastBlockadeRisk＝取扱高加重の危険', A(('World.cs', 'LastBlockadeRisk')), 'World.cs Diplomacy.cs MultiNationSim.cs', '読まれない欄', '働かない（書き手 0）',
    f"set の書き手が SimCore に無い（grep）。monlaw_s の blockadeRiskShare＞0 {rng(('H019','blockadeRiskSharePosRows'))}。読み手 3（MultiNationSim.cs:1517・Diplomacy.cs:799・:2553）", '関連：S24', '直すか消す＝Q-s08-2')

# ---- advisor の点検の後の直し（2026-11-13・結果の後の変更を含む＝名乗る） ----
ST = {}
for line in open(os.path.join(W, 'out_static.txt'), encoding='utf-8'):
    c = line.rstrip('\n').split('\t'); ST[(c[0], c[1])] = c[2]
RD = {}
for line in open(os.path.join(W, 'readers_S08.txt'), encoding='utf-8'):
    c = line.rstrip('\n').split('\t'); RD[c[0]] = '/'.join(x.split('=')[1] for x in c[1:5])
def rd(*names):
    return '読み手 ' + '・'.join(f'{RD[n]}（{n}）' for n in names) + '（SimCore/Tests/App/Runner の grep の行数）'
ORIG = '輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧）'
R_RAIL = '〈段 R の合流の後に確かめ直す：鉄道の容量〉'; R_LEG = '〈段 R の合流の後に確かめ直す：陸の脚の比〉'
R_CEDE = '〈段 R の合流の後に確かめ直す：割譲の移し〉'; R_WAR = '〈段 R の合流の後に確かめ直す：戦災〉'; R_FARE = '〈段 R の合流の後に確かめ直す：運賃の受け手〉'
t = lambda n: ST[('tests', n)]
OVR = {
 'S08-H001': {10: R_LEG}, 'S08-H002': {10: R_LEG + '・LandRouteEnabled は 2026-10-18 の焼き込みで試験の環境固定のため残した（DormantIdentityTests.cs:318）', 11: '残す'},
 'S08-H003': {10: R_LEG + '・峠の線（途中の地形）'}, 'S08-H004': {10: R_LEG + '・' + OV('B（LandSeaCostRatio・RailCostFraction）') + '＝段 R が φ で混ぜる'},
 'S08-H005': {9: f"sea_nodes.json {ST[('static','sea_nodes')]} 行・海盆 {ST[('static','basins')]}・passages.json {ST[('static','passages')]} 行（out_static.txt）。試験 SeaGraphTests {t('SeaGraphTests')} 件"},
 'S08-H006': {9: f"SeaGraph.cs:361 は new Coefficients() から 1 回読む＝国ごとの係数の上書きは効かない（静的・記録）。試験 SeaStitchBudgetTests {t('SeaStitchBudgetTests')} 件"},
 'S08-H007': {9: f"barriers.json は barrier {ST[('static','barrier')]}・pass {ST[('static','pass')]}（文書は峠 12）＝(762)(769) の fix の行で足した峠を含む。試験 PathGraphTests {t('PathGraphTests')}・LandGraphFixTests {t('LandGraphFixTests')}（PathGraphTests に辺の件数の番人）"},
 'S08-H008': {9: f"landmass.json {ST[('static','landmass')]} 行（文書は 394 点）。試験 LandmassTests {t('LandmassTests')} 件"},
 'S08-H011': {11: '消す（峠の線・挙動）・' + rd('BarrierClearanceKm', 'WallLinkKm', 'CrossesWall', 'CrossesBarrier')},
 'S08-H012': {11: '消す（峠の線・挙動・素案は ledger_S08 §4）・' + rd('PassCostMult', 'PassNeighbors', 'barriers.json')},
 'S08-H016': {11: '消す（K1・恒等の線・App の GameViewModel.News.cs:292 が Onset/Span を読む）・' + rd('TransportRevolutionMax', 'TransportRevolutionOnset', 'TransportRevolutionSpan', 'FreightRevolution')},
 'S08-H017': {8: '働く', 9: f"受け取り＞0 の国×年 {rng(('H017','freightReceivedPosNationYearShare'))}・9 国の運賃の書き手（BilateralMarket.cs:1209）{fr('H017_freightToShipOwners')}。受け手の偏りは S08-40", 10: R_FARE, 11: '—'},
 'S08-H019': {11: '消す（給付の分 K2・恒等）か直す（封鎖の危険の書き手）＝Q-s08-2・' + rd('StepMarineInsurance', 'InsurancePayoutFrac')},
 'S08-H022': {8: '働く', 9: f"開通は運河×種の {canal_open_count()}（out_freq の canal_s の行）。開通の年（史実は記録）：スエズ {canal_years('suez')}・パナマ {canal_years('panama')}。拠出の行は 9 国×四半期の {fr('H022_canalInvest')}。総額が律速にならないことは S08-41", 11: '—'},
 'S08-H023': {9: f"配当（MultiNationSim.cs:1447）：スエズが開いた後の 9 国×四半期の {rng(('flow','H023_canalDividend_afterSuez'))}〔結果の後の変更：分母を開通の後へ狭めた〕・全ての 9 国×四半期では {fr('H023_canalDividend')}。通行料の国庫→国庫は列なし", 11: '消す候補 K3＝Q-s08-3・' + rd('CanalInvestReturns', 'DistributeCanalDividend')},
 'S08-H024': {10: '計器 choke_s'},
 'S08-H025': {9: f"台帳の〈消えた係数 RouteDependenceTensionRate〉は改名（今 RouteDependenceTensionPerMonth）。緊張の理由ごとの列なし。試験 RouteTensionTests {t('RouteTensionTests')} 件", 11: '消す候補 K4（計器の後）＝Q-s08-3・' + OV('B（RouteDependenceTensionPerMonth＝docs/202611/20261111/20261111_倍率の一掃_分類の取りまとめ.md:241）') + '・' + rd('RouteDependenceTensionPerMonth', 'RouteTension')},
 'S08-H028': {8: '未確かめ（近似だけ）', 9: f"凍結の列なし（予言 P9）。近似：rulepower_s（活きた国の全ての州×四半期）で凍結を外した支配の届き≠今の届きの行 {rng(('H028','rhoNoFreezeNeRhoNewShare'))}〔分母が全ての州＝高緯度の港の州に限っていない〕。BilateralMarket.cs:306。旗 WinterPortFreeze は焼き込み"},
 'S08-H029': {9: f"portch_s：陸路で選んだ港≠大圏の最寄りの州×年 {rng(('H029','portUsedNeGcShare'))}（参考）。試験 PortRewireTests {t('PortRewireTests')}・PortByLandTests {t('PortByLandTests')}"},
 'S08-H030': {8: '働く（ソース）・文書が古い', 9: f"InlandRailAfterDecay＝鉄道の容量÷(1＋最寄りの港までの km÷DistanceNormKm)（BilateralMarket.cs:35）＝内陸の州で式から必ず＜1（portch_s の内陸の行で {rng(('H030','railDecayUsedLt1Share'))}＝確かめではなく式の写し）。係数 InlandPortDistanceDecay は (804) で削除（文書の変数の行は古い）", 10: R_RAIL},
 'S08-H031': {9: f"通行料は国庫→国庫（BilateralMarket.cs:1269）＝flow_s に出ない。試験 TransitTests {t('TransitTests')} 件"},
 'S08-H032': {9: f"stock_s：鉄道の容量＞0 の国×時点 {rng(('H032','railCapacityPosShare'))}。船の容量の列なし。汽船の〈時代とともに上がる〉÷EraFactor は Coefficients.cs:3483 で常に 1＝働かない", 10: R_RAIL + '・' + R_WAR + '（Diplomacy.cs:2625 の RailCapacity・ShipCapacity の戦災）・' + OV('I（EraFactor の残骸）')},
 'S08-H033': {10: R_RAIL + '・' + OV('A（TransportTargetPerPop＝輸送の線で）')},
 'S08-H034': {10: R_RAIL + '・' + OV('A（ThreatRailTargetWeight・TransportCongestionWeight＝輸送の線で）') + '・計器 shipcap_s'},
 'S08-H035': {10: R_RAIL}, 'S08-H036': {10: R_RAIL + '・' + R_CEDE + '（Territory.cs:367 の Transport.Receive）'}, 'S08-H037': {10: R_RAIL},
 'S08-H038': {10: RMARK + '（鉄道の容量＝BilateralMarket.cs:264〜270 の内側の :265 を読む）・' + OV('A（ColonyShipOccupyPerDist）') + '・計器 shipcap_s'},
}
for r in rows:
    for col, val in OVR.get(r[0], {}).items(): r[col] = val
row('S08-40', '運賃の受け手の偏り（新しい行）', '（文書なし・H017 の狙い〈海運国が受け取る〉）', A(('BilateralMarket.cs', 'shipShare'), ('Transport.cs', 'BuyToward')), 'BilateralMarket.cs Transport.cs Colonial.cs', '頻度', '一部だけ働く（既知）',
    f"受け取りの上位（M）：{raw(('freight','nation:recvShare:paidShare:recvOverPaid'), src=X)}（国:受け取りの取り分:払いの取り分:受け取り÷払い）。英は受け取り {', '.join(raw(('freight','イギリス'), arm=a, src=X) for a in ARMS)}（4 腕）。既知＝00一覧 1691 行 (204)〈世界の運賃の上位はロシア・朝鮮・清・オーストリア＝船腹の目標が人口で決まる・英は ColonialShipBurden 6〜12 で交易の船腹が約 1/9〉と輸送の設計 doc（20261016）§7 の表 2 行目。足す根の候補（推測）：船の目標に脅威の項（ThreatRailTargetWeight・(155) の後）も掛かる（Transport.cs:19〜:23・:44）",
    R_FARE + '・(204) の推奨（船腹の目標を運びたい量へ）と同じ線＝二重に線を作らない・計器 shipcap_s', '直す（既にある (204) の線・挙動）')
row('S08-41', '運河の総額が律速にならない（新しい行）', '（文書なし・H022 の〈総額に達すると開通〉）', A(('MultiNationSim.cs', 'BuildProgress'), ('Coefficients.cs', 'CanalInvestFrac')), 'MultiNationSim.cs world.json', '頻度', '一部だけ働く',
    f"開通は要る発見（汽船・ダイナマイト）の最初の四半期から {canal_lag()}（out_extra_*.txt・disc_s は全ての国の E 行）＝総額（world.json の buildCost 150000・400000）の段がほぼ効かない。根の候補（推測）：進捗は自国通貨の工賃（MultiNationSim.cs:1373）・持分は金建て（:1378）＝総額の単位が通貨か金か未確かめ",
    ORIG, '消す候補 K3（拠出の段）＝Q-s08-3・' + rd('CanalInvestFrac', 'InvestorShares'))
for r in rows:
    r[10] = ORIG if r[10] in ('—', '') else (r[10] if r[10].startswith(ORIG) else ORIG + '・' + r[10])

with open(outp, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\t'.join(['行', '系統', '機能', '期待する動き（出典）', '錨', '持ち主のファイル', '判定の型', '最後に確かめた', '判定', '根拠', '残りタスク', '直す／消す／残す']) + '\n')
    for r in rows:
        assert len(r) == 12 and all('\t' not in x and '\n' not in x for x in r), r[0]
        f.write('\t'.join(r) + '\n')
print('rows', len(rows))
