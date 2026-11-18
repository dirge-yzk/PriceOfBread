# 読むだけ (1224) c123rb：11-12 の S26 の 49 行を、今の木 81ad1e58d の錨・写しと読み直しの別で 12 列の TSV に書く（(1217) mk_tsv_s13.py の形を写した）。
#  入力：q/ledger_S26_2026-11-12.tsv（木 7bb40c77）・q/s26_cmp.tsv（cmp_rows.py）・q/s26_anchor_hunks.tsv・q/s26_reread_m1219.txt（s26_reread.py）
#  出力：q/ledger_S26_2026-11-19.tsv（12 列）・q/side_S26_2026-11-19.tsv（写した判定と読み直した判定の列を分ける）
#  使い方: python -X utf8 -I mk_tsv_s26.py <作業フォルダ>
import sys, os, csv
W = sys.argv[1]; NEW = '81ad1e58d'
RAW = '（生 keep_1219/base_ins 種 1〜10・q/s26_reread_m1219.txt）'
PRB = '（プローブ r1224・旗 0・種 1〜10・12 か月・英仏の植民地格の州 17・PaySales の呼び出しを数えた＝out/f0_s1_10_P.tsv）'
def rd(p): return list(csv.reader(open(os.path.join(W, p), encoding='utf-8'), delimiter='\t'))
old = rd('q/ledger_S26_2026-11-12.tsv'); hdr, rows = old[0], old[1:]
cmp = {r[0]: r for r in rd('q/s26_cmp.tsv')[1:]}
FIX = {'S26-05': 'Simulation.cs:1281:IsColonial;Simulation.cs:1296:ScarceEndowment;Simulation.cs:1353:AddAdvancedProducers',
       'S26-11': 'Simulation.cs:825:RegionTier.Core;Simulation.cs:1361:Tier;Territory.cs:155:Rehome',
       'S26-24': 'Diplomacy.cs:370:ColonyMass;Diplomacy.cs:1013:ThreatExcess;Diplomacy.cs:1030:EffColonies',
       'S26-48': 'Simulation.cs:655:ScarceEndowment',
       'S26-14': 'Coefficients.p07_wiring_prewarm.cs.md:969:ColonyUpkeepMilitancyScale', 'S26-15': 'Coefficients.p08_audit_soldiers.cs.md:270:ColonyUpkeepCoalPerDist',
       'S26-20': 'Coefficients.p04_trade.cs.md:999:MalariaQuininePerColony', 'S26-21': 'Coefficients.p04_trade.cs.md:999:MalariaQuininePerColony',
       'S26-36': 'Colonial.cs.md:171:墓標', 'S26-38': 'Colonial.cs.md:172:ColonialRegionSettlement', 'S26-39': 'Colonial.cs:7:OutputCurve',
       'S26-40': 'EconomyStep.cs:823:ColonialResources', 'S26-42': 'Diplomacy.cs:90:捕囚', 'S26-43': 'Coefficients.p03_debt_money.cs.md:952:ColonialExtractionFraction',
       'S26-47': 'Coefficients.p05_port_insurance.cs.md:169:CessionRangeTechKm', 'S26-49': 'Coefficients.p08_audit_soldiers.cs.md:120:PreemptiveSeizeWeight'}
NOTYPE = {'S26-14', 'S26-15', 'S26-36', 'S26-39', 'S26-40', 'S26-42', 'S26-43', 'S26-47', 'S26-49'}   # 11-12 は型〈—〉・錨〈—〉＝削除済みの経路＝型は〈効き〉・錨は削除を記した履歴か墓標の行（他の系統の〈文書が古い〉の行と同じ取り方）
# 読み直した行：(判定, 根拠の頭に足す文, 12 列目〔None＝そのまま〕)
D = {
 'S26-01': ('働く', f'読み直し：開幕の植民地格の州 81（10/10 種・q0）{RAW}。', None),
 'S26-02': ('働く', f'読み直し：q0 の和 81＝昇格しなかった植民地 0（10/10 種）{RAW}。', None),
 'S26-04': ('働く', '読み直し（ソース）：錨の近くの差は Malarial の欄の削除（(1167) 41654e989）だけ＝住民と市場の初期値の式は同じ。', None),
 'S26-05': ('働く', '読み直し（ソース）：ScarceMineralGate は焼かれて条件が消えた（希少鉱物は常に希少の賦存）・Malarial の削除。賦存の式は同じ。', None),
 'S26-06': ('働かない', '読み直し（ソース）：Simulation.cs:1365 は今も Culture = w.Region.Culture（首都の札を写す）。', None),
 'S26-07': ('一部だけ働く', f'読み直し：不在地主の大農園の地代の割合 RentShareOf は英の植民地 7 州で 0.5・仏の 6 州で 0.6 {PRB}。0.6 は Simulation.cs:1298 で置かれるが、RentShareOf（EconomyStep.cs:639）が州の FarmRentCap（Simulation.cs:1441＝持ち主の国の土地改革と農奴解放の法の上限）で削る＝英の植民地は本国の法で 0.5。', '読む（次の波）：本国の土地の法の地代の上限が植民地の大農園に効く経路（Simulation.cs:1441）を史実と並べる＝確証なし・作者の問いの材料'),
 'S26-09': ('働く', f'読み直し：清算の輸出の代金は PaySales（InternationalMarket.cs:29）で大農園の取り分ごとに割られ、地代は AddRentRecipients（EconomyStep.cs:696）で AbsenteeOwner へ行く。代金の和 コーヒー 3,566・サトウキビ 464・金（清算）107・金（造幣）220 {PRB}。⚠ この代金は生産者の LastRevenue に載らない（EconomyStep.cs:2431 は国内の売上だけ）＝帳の穴（保存則ではない）。', '直す（記帳・(1224) 段 B の申し送り）：LastRevenue に PaySales の代金を載せる'),
 'S26-10': ('働く', '読み直し（ソース）：錨の近くの差は Malarial の削除だけ。', None),
 'S26-11': ('働く', '読み直し（ソース）：錨の近くの差は Malarial の削除だけ。格の式は同じ。', None),
 'S26-12': ('未確かめ', '読み直し（ソース）：錨の近くの差は Malarial の削除だけ。', None),
 'S26-16': ('一部だけ働く', '読み直し（ソース）：時代係数 EraColonyCoal は (1154) で消え const era＝1.0（Colonial.cs:32）＝値は同じ。', None),
 'S26-18': ('未確かめ', '読み直し（ソース）：Colonial.Step の差はキニーネの枝の削除（(1167)）・BilateralMarket.cs の差は鉄道の作業配列＝占有の式は同じ。', None),
 'S26-19': ('一部だけ働く', '読み直し（ソース）：Colonial.Step の差はキニーネの枝の削除だけ。', None),
 'S26-20': ('文書が古い', '読み直し（ソース）：キニーネの枝と係数 MalariaQuininePerColony は (1167) 234de832e で消えた（RetiredFields）・計器 colqn_s も 95b581185 で消えた。', '消した（(1167)）＝文書の行を直す'),
 'S26-21': ('文書が古い', '読み直し（ソース）：S26-20 と同じく (1167) で消えた。', '消した（(1167)）＝文書の行を直す'),
 'S26-23': ('働く', '読み直し（ソース）：錨の近くの差はキニーネの行の削除だけ＝homePressure の式は同じ（Colonial.cs:41・:53）。', '消す（線 L1・D1・読み手 エンジン 2〔Colonial.cs:41・:53〕・探針・計器・試験は 11-12 の数のまま〈未数え直し〉）：代わりは作らない（11-12 の理由のまま）'),
 'S26-28': ('働く', f'読み直し：英仏の植民地の産品が清算で輸出される（コーヒー 127 行・サトウキビ 189 行ほか）{PRB}。', None),
 'S26-35': ('一部だけ働く', f'読み直し：植民地格の州の和は q0 81→最大 110→最後 110（10/10 種）・格が植民地格から変わった州 0（10/10）・和が前の最大を下回った四半期は種 7 の q161 の 1 回だけ（最後は 110）{RAW}。', None),
 'S26-37': ('支配の力の段で書き直す', '読み直し（ソース）：残る読み手は Politics.cs:873（分離の圧の飽和）＝この波の恒等の線 c123cu の持ち主の範囲（:861〜:880）の中。', None),
 'S26-38': ('文書が古い', '読み直し（ソース）：計器の Settled と region_s の settlerOut・settlerIn の 2 列は木に無い（SimCore を grep して 0 件）。台本 3 本（hol/an_h2.py・hol/edge_689.py・m749/flows748.py）は列が無い時の枝を持つ。', '消した（計器）＝文書の行を直す'),
 'S26-41': ('働かない', '読み直し（ソース）：ColonialResources の読み手 エンジン 7・ColonialAccess 5（Simulation.cs:1725・:1767）＝今も毎月計算して州へ書く。', '消す（線 L3・D3・読み手 エンジン 12〔ColonialResources 7・ColonialAccess 5〕）：61 カ国では恒等・単一国の試験の盤面は変わる（11-12 の理由のまま）'),
 'S26-44': ('支配の力の段で書き直す', f'読み直し：植民地格から格が変わった州 0（10/10 種）＝確定した独立 0 を支える {RAW}。', None),
 'S26-46': ('一部だけ働く', '読み直し（ソース）：錨は Politics.cs:1031（RegionAvgSuffrageInclusion）へ動いた・式の差は無い。', None),
 'S26-48': ('未確かめ', '読み直し（ソース）：旗 ScarceCountsIncludeColonies は (1089) f0e08f644 で焼かれた（常に植民地を数える）＝錨は関数 ScarceEndowment（Simulation.cs:655）。', '—（旗は焼いた）'),
}
REV = {'S26-13', 'S26-14', 'S26-24', 'S26-27', 'S26-32'}   # _反証.md が名指すが変わらない行＝写しのまま（11-12 の tsv は反証の直しを入れた後の版）
out = []; side = [['行', '区分', '写した判定（11-12・7bb40c77）', '読み直した判定（81ad1e58d）', '錨の比べ（cmp_rows）']]
for r in rows:
    k = r[0]; c = cmp.get(k); na = FIX.get(k) or (c[2] if c and c[2] else r[4] if r[4] else '—')
    nr = list(r); nr[4] = na if na else '—'
    if k == 'S26-37': nr[6] = '効き'
    if k in NOTYPE: nr[6] = '効き'; nr[9] = '錨は削除を記した履歴か墓標の行（(1224) で取った）。' + nr[9]
    if k in D:
        j, why, act = D[k]; nr[7] = NEW; nr[8] = j; nr[9] = why + ' ｜11-12：' + r[9]
        if act is not None: nr[11] = act
        side.append([k, '読み直し', r[8], j, c[1] if c else '—'])
    else:
        side.append([k, '写し', r[8], '—', c[1] if c else '—'])
    out.append(nr)
with open(os.path.join(W, 'q/ledger_S26_2026-11-19.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('\t'.join(hdr) + '\n'); [f.write('\t'.join(x) + '\n') for x in out]
with open(os.path.join(W, 'q/side_S26_2026-11-19.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    [f.write('\t'.join(x) + '\n') for x in side]
import collections
print('判定', dict(collections.Counter(x[8] for x in out))); print('区分', dict(collections.Counter(x[1] for x in side[1:])))
