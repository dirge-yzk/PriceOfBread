# 読むだけ (1231) c124rd：台帳 S26 を 11-15 の版（m1148・木 aa8ee7e51）を土台に取り直す。(1224) の読み直し 24 行は残し、S26-12・18・48 は 11-15 の根拠と並べる。
#  入力：measurements/m1148_20261115/ledger_S26_2026-11-15.tsv（土台）・measurements/m1224_20261119/q/ledger_S26_2026-11-19.tsv（(1224) の読み直し）・
#        q/s26_cmp_1231.tsv（cmp_rows.py aa8ee7e51→8846c6a0f）・q/s26_reread_m1226.txt（s26_reread.py を keep_1226/base_ins で）
#  出力：q/ledger_S26_2026-11-19b.tsv（12 列）・q/side_S26_2026-11-19b.tsv（11-15 の判定・(1224) の判定・この版の判定を列で分ける）
#  使い方: python -X utf8 -I mk_tsv_s26_1231.py <repo> <作業フォルダ>
import sys, os, collections
REPO, W = sys.argv[1], sys.argv[2]; NEW = '8846c6a0f'; R1224 = '81ad1e58d'
def rd(p):
    L = open(p, encoding='utf-8').read().rstrip('\n').split('\n'); return L[0].split('\t'), [l.split('\t') for l in L[1:]]
hdr, base = rd(os.path.join(REPO, 'measurements/m1148_20261115/ledger_S26_2026-11-15.tsv'))
_, r24 = rd(os.path.join(REPO, 'measurements/m1224_20261119/q/ledger_S26_2026-11-19.tsv')); r24 = {r[0]: r for r in r24}
_, cmp = rd(os.path.join(W, 'q/s26_cmp_1231.tsv')); cmp = {r[0]: r for r in cmp}
RAW = '（生 keep_1226/base_ins＝m1226・種 1〜10・q/s26_reread_m1226.txt）'
# 錨の取り直し（cmp が引けなかった行と、11-15 で〈—〉の行＝(1224) の FIX の写し・8846c6a0f で grep して確かめた）
FIX = {'S26-11': 'Simulation.cs:825:RegionTier.Core;Simulation.cs:1361:Tier;Territory.cs:155:Rehome',
       'S26-16': 'Colonial.cs:38:coalWant;Colonial.cs:62:OccupiedShipCapacity;Colonial.cs:32:era',
       'S26-20': 'Coefficients.p04_trade.cs.md:999:MalariaQuininePerColony', 'S26-21': 'Coefficients.p04_trade.cs.md:999:MalariaQuininePerColony',
       'S26-37': 'Coefficients.p02_borrow_polity_epidemic.cs:101:ColonyPromotionMinPop;Politics.cs:884:ColonyPromotionMinPop',
       'S26-38': 'Colonial.cs.md:172:ColonialRegionSettlement', 'S26-48': 'Simulation.cs:645:InitialColonies;Simulation.cs:655:ScarceEndowment',
       'S26-14': 'Coefficients.p07_wiring_prewarm.cs.md:969:ColonyUpkeepMilitancyScale', 'S26-15': 'Coefficients.p08_audit_soldiers.cs.md:270:ColonyUpkeepCoalPerDist',
       'S26-36': 'Colonial.cs.md:171:墓標', 'S26-39': 'Colonial.cs:7:OutputCurve', 'S26-42': 'Diplomacy.cs:90:捕囚',
       'S26-43': 'Coefficients.p03_debt_money.cs.md:952:ColonialExtractionFraction', 'S26-47': 'Coefficients.p05_port_insurance.cs.md:169:CessionRangeTechKm',
       'S26-49': 'Coefficients.p08_audit_soldiers.cs.md:120:PreemptiveSeizeWeight'}
NOANCHOR = {'S26-14', 'S26-15', 'S26-36', 'S26-39', 'S26-42', 'S26-43', 'S26-47', 'S26-49'}
# この版で生を数え直した行（判定の型＝頻度）
FREQ = {
 'S26-01': ('働く', f'読み直し（頻度）：開幕 q0 の植民地格の州 81 が 10/10 種 {RAW}。'),
 'S26-02': ('働く', f'読み直し（頻度）：q0 の和 81＝昇格しなかった植民地 0 が 10/10 種 {RAW}。'),
 'S26-35': ('一部だけ働く', f'読み直し（頻度）：植民地格の州の和は q0 81→最大 107〜110→最後＝最大（10/10 種）・格が植民地格から変わった州 0（10/10）・和が前の最大を下回った四半期のある種 2/10（種 4 の q520・種 7 の q161＝keep_1219 では種 7 だけ）。出口（独立・吸収）の列は無い {RAW}。'),
 'S26-44': ('支配の力の段で書き直す', f'読み直し（頻度）：植民地格から格が変わった州 0 が 10/10 種＝確定した独立 0 {RAW}。'),
}
# 11-15 の根拠と並べて判定を戻す行（(1224) は 11-12 の版から読んだため未確かめへ下げた）
BACK = {
 'S26-12': ('働く', f'並べ（ソース・{NEW}）：BuildProvince（Simulation.cs:819）と PromoteColonyToRegion（:1356）が同じ AddAlternateRecipes を渡された coef で呼ぶ＝11-15 の根拠のまま。錨の近くの差は Malarial の欄の削除（(1167)）だけ（q/s26_anchor_hunks_1231.tsv）。(1224) の〈未確かめ〉は 11-12 の版の根拠を読んだため。'),
 'S26-18': ('一部だけ働く', f'並べ（ソース・{NEW}）：占有の式（Colonial.cs:23 の ColonyShipOccupyPerDist×海路 km×州の人口の比・:35 の steamMult）と BilateralMarket.cs:359 の国の船腹÷(1＋占有)は 11-15 から変わらない（近くの差はキニーネの枝の削除と era＝1.0 の焼き込み＝値は同じ）。11-15 の見積もり（開幕の英 5.37＝交易の船腹が 1/6.4 以下・大圏の km＝下側）をそのまま写す＝見積もりで確証 中。占有の列は今も無い＝拘束の頻度は未確かめ。(1224) の〈未確かめ〉は列が無いことを読んだ物。'),
 'S26-48': ('働く', f'並べ（ソース・{NEW}）：旗 ScarceCountsIncludeColonies は (1089) で焼かれ、希少鉱物の総数は開幕の植民地を無条件で数える（Simulation.cs:645 の foreach と :655 の ScarceEndowment）＝11-15 の根拠のまま。(1224) は錨の移りを読んで〈未確かめ〉にした（判定の材料は同じ）。'),
}
# 処置（12 列目）と理由 1 行。畳む＝支配の力の段が書き直す行（E・F・G）。
ACT = {
 'S26-01': '残す（開幕の 81 が 10/10 種で成り立つ）', 'S26-02': '残す（閉じる側の件数 0 が 10/10 種）',
 'S26-03': '畳む（支配の力の段 E：開幕の支配度は段 E の支配の取り分へ）', 'S26-04': '残す（式は同じ・差は Malarial の欄の削除だけ）',
 'S26-05': '残す（賦存の式は同じ）', 'S26-06': '直す（R8：places.json の植民地の行に土地の札を足し Simulation.cs:1365 で読む・挙動）',
 'S26-07': '—（決めない：FarmRentCap が植民地に効く件は ef〔(1232)〕の材料へ渡す）', 'S26-08': '残す（11-15 の頻度のまま・錨の近くの差 0）',
 'S26-09': '残す（清算の代金は PaySales で大農園の取り分ごとに割られ地代は AbsenteeOwner へ＝働く・記帳の穴は bk の線）',
 'S26-10': '直す（データ 1 行：パナマを沿岸に・landmass.json）', 'S26-11': '残す（格の式は同じ・段 F が割譲の経路を替える時に読み直す）',
 'S26-12': '残す（同じ関数で全ての州を作る）', 'S26-13': '残す（11-15 の頻度のまま・錨の近くの差 0）',
 'S26-14': '直す（文書：削除済みの経路の行を直す）', 'S26-15': '直す（文書：削除済みの経路の行を直す）',
 'S26-16': '残す（L8 の中で読み直す・EraColonyCoal は (1154) で消えた＝D5 は済み）', 'S26-17': '残す（燃料の選びの列が無い＝計器の仕様 §6）',
 'S26-18': '直す（L8：読む→直す・占有を住民の数でなく補給の物の量へ）', 'S26-19': '直す（L7：拠出を拠出国の汽船の量に比例）',
 'S26-20': '直す（文書：キニーネの枝は (1167) で消えた）', 'S26-21': '直す（文書：(1167) で消えた）',
 'S26-22': '残す（未確かめ・調達の門の列が無い）', 'S26-23': '消す（K99・D1・読み手 エンジン 2〔Colonial.cs:41・:53〕・係数 1・台本 1・試験 0・App 0・挙動の線）',
 'S26-24': '畳む（支配の力の段 G：植民地の質量は実効の支配の量へ）', 'S26-25': '畳む（支配の力の段 G：属国の 0.3 の合算は支配の取り分へ）',
 'S26-26': '直す（文書：S23 の行へ移す）', 'S26-27': '残す（倍率の一掃の区分の材料）', 'S26-28': '残す（植民地の産品が清算で輸出される）',
 'S26-29': '残す（R6 が書き直す）', 'S26-30': '直す（R1：越境の向き）', 'S26-31': '残す（安全弁・未確かめ）', 'S26-32': '直す（S26-06 と同じ腕）',
 'S26-33': '畳む（支配の力の段 F：文化と宗教の距離は統治の費用の段へ）', 'S26-34': '残す（扼者の列が無い＝未確かめ）',
 'S26-35': '残す（番人・下回りの種 2/10 は格の変わり 0 と両立＝出口の列を計器へ）', 'S26-36': '直す（文書が古い）',
 'S26-37': '畳む（支配の力の段 E：残る読み手 Politics.cs:884 の分離の圧の飽和）', 'S26-38': '直す（文書：計器の Settled と 2 列は木に無い）',
 'S26-39': '直す（文書が古い）', 'S26-40': '直す（文書が古い）',
 'S26-41': '消す（K100a・K100b・D3・読み手 エンジン 12〔ColonialResources 7・ColonialAccess 5〕・試験 5・61 カ国で恒等）',
 'S26-42': '直す（文書：捕囚市場は無い）', 'S26-43': '直す（文書が古い）',
 'S26-44': '畳む（支配の力の段 E：植民地の独立は分離の 1 本へ）', 'S26-45': '残す（未確かめ・S19-28 と同じ計器）',
 'S26-46': '直す（L9：少数派の不満の側 Politics.cs:165 を POP ごとの 1 本へ）', 'S26-47': '直す（文書が古い）',
 'S26-48': '残す（総数は植民地を数える）', 'S26-49': '直す（文書が古い）', 'S26-V01': '直す（S26-06 と同じ腕）',
}
out = []; side = [['行', '区分', '11-15 の判定（aa8ee7e51）', '(1224) の判定（81ad1e58d）', 'この版の判定', '錨の比べ（cmp aa8ee7e51→8846c6a0f）']]
for r in base:
    k = r[0]; c = cmp.get(k); nr = list(r)
    na = FIX.get(k) or (c[2] if c and c[2] and '?' not in c[2] else r[4])
    nr[4] = na
    if k in NOANCHOR: nr[9] = '錨は削除を記した履歴か墓標の行（(1224) で取り、8846c6a0f で grep した）。' + nr[9]
    j24 = r24[k][8] if k in r24 else '—'
    if k in FREQ:
        j, why = FREQ[k]; nr[6] = '頻度'; nr[7] = NEW; nr[8] = j; nr[9] = why + ' ｜11-15：' + r[9]; tag = '読み直し（1231・頻度）'
    elif k in BACK:
        j, why = BACK[k]; nr[7] = NEW; nr[8] = j; nr[9] = why + ' ｜11-15：' + r[9]; tag = '並べ（1231）'
    elif k in r24 and r24[k][9].startswith('読み直し'):
        why = r24[k][9].split(' ｜11-12：')[0]; nr[7] = R1224; nr[8] = j24; nr[9] = why + ' ｜11-15：' + r[9]; tag = '読み直し（1224）'
    else:
        tag = '写し（11-15）'
        if c and c[1] == '変わらない': nr[9] = nr[9] + f' ｜錨は {NEW} で同じ字（cmp）。'
        elif c: nr[9] = nr[9] + f' ｜錨の近くの差は q/s26_anchor_hunks_1231.tsv。'
    nr[11] = ACT[k]
    side.append([k, tag, r[8], j24, nr[8], c[1] if c else '—'])
    out.append(nr)
assert all(len(x) == 12 for x in out)
with open(os.path.join(W, 'q/ledger_S26_2026-11-19b.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('\t'.join(hdr) + '\n'); [f.write('\t'.join(x) + '\n') for x in out]
with open(os.path.join(W, 'q/side_S26_2026-11-19b.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    [f.write('\t'.join(x) + '\n') for x in side]
print('判定', dict(collections.Counter(x[8] for x in out))); print('区分', dict(collections.Counter(x[1] for x in side[1:])))
print('処置', dict(collections.Counter(x[11].split('（')[0] for x in out)))
