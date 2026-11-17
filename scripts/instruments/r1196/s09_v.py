# (1196) c119ra 問い3：台帳 S09 の読み直し（keep_1191 の生・種の範囲は引数）。判定はエンジンの閾値の式（計器が写した門の印・ソースの不等式）で数える。
#  母集合（混ぜない・行ごとに名乗る）：
#   T＝tradeskip_s の 財×国×年 の行（good≠ALL・調べる年の標本＝計器の国）・TN＝同じ表の (年, 国) の組（関税の列は国の値）
#   F＝sf_s の国×四半期（計器の国）  L＝monlaw_s の国×四半期（全ての国）  O＝opium_s の国の行（scope N・全ての国×四半期）
#   C＝secession_s の州×四半期（計器の国）  X＝scalars_s の国×四半期（全ての国）  G＝fiscal_s の国×月（計器の国）
#  式：楔で飛ぶ＝xWedge・関税を抜けば通る＝xWedgeAT（InstrumentsTradeskip.cs:202）・保有の禁令＝OpiumPossession≠None（BilateralMarket.cs:323）
#      輸出禁止＝exportBanFlag 1（:324）・負の関税の輸出補助＝cheapWithNegTariffCount＞0（sf_s）・高く買う＝dearGoodCount＞0（sf_s）
#  使い方: python -X utf8 -I s09_v.py <ins（D:/…）> <種の起点> <種の終わり>
import csv, os, sys, collections, statistics
sys.stdout.reconfigure(newline='\n')
B, sb, se = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])


def fl(v):
    try: return float(v)
    except (ValueError, TypeError): return float('nan')


def rows(t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        for x in csv.DictReader(f, delimiter='\t'): yield x


for s in range(sb, se + 1):
    out = []
    # T・TN
    xw = xat = 0; tn = {}
    for x in rows('tradeskip', s):
        if x['good'] == 'ALL': continue
        xw += int(x['xWedge'] or 0); xat += int(x['xWedgeAT'] or 0)
        tn[(x['year'], x['nation'])] = (fl(x['tariffNow']), fl(x['treatyScale']), fl(x['tarEff']))
    tv = [v[0] for v in tn.values() if v[0] == v[0]]; ts = [v[1] for v in tn.values() if v[1] == v[1]]
    out.append(f"R01 T 楔で飛んだ越境の売り手 {xw:,}・関税を抜けば通る {xat:,}（{100*xat/max(1,xw):.1f}%＝上界）・TN 関税の中央 {statistics.median(tv) if tv else float('nan'):.3f}（{len(tn)} 国×年）")
    out.append(f"R05 TN 関税≧0.5 {sum(1 for v in tv if v >= 0.5)}／{len(tv)}（{100*sum(1 for v in tv if v >= 0.5)/max(1,len(tv)):.1f}%）・≧0.8 {sum(1 for v in tv if v >= 0.8)}・負 {sum(1 for v in tv if v < 0)}")
    out.append(f"R08 TN 目減り treatyScale＜0.99 {sum(1 for v in ts if v < 0.99)}・＜0.9 {sum(1 for v in ts if v < 0.9)}・＜0.5 {sum(1 for v in ts if v < 0.5)}／{len(ts)}")
    # F
    n = neg = dear = 0; dsum = 0
    for x in rows('sf', s):
        n += 1; neg += fl(x['cheapWithNegTariffCount']) > 0
        d = fl(x['dearGoodCount']); dear += d > 0; dsum += d if d == d else 0
    out.append(f"R04 F 負の関税で安く売る財＞0 {neg}／{n}・R34 F 高く買う財＞0 {dear}／{n}（平均 {dsum/max(1,n):.1f} 財）")
    # X
    xn = xneg = 0; aw = 0
    for x in rows('scalars', s):
        xn += 1; t = fl(x['tariffTargetIndex']); xneg += t < 0; aw += x['atWar'] not in ('0', '', 'False')
    out.append(f"R04 X 関税の目標＜0 {xneg}／{xn}（{100*xneg/max(1,xn):.1f}%）・R22/R28 X 交戦中の国×四半期 {aw}／{xn}（{100*aw/max(1,xn):.1f}%）")
    # G
    gn = gneg = 0
    for x in rows('fiscal', s):
        v = fl(x.get('ledgerTariffPerMonth')); gn += v == v; gneg += v < 0
    out.append(f"R04 G 関税の台帳＜0 の国×月 {gneg}／{gn}")
    # L
    ln = lb = le = br = 0
    for x in rows('monlaw', s):
        ln += 1; lb += x['OpiumPossession'] not in ('0', ''); le += x['OpiumExport'] not in ('0', ''); br += fl(x['blockadeRiskShare']) != 0
    out.append(f"R16 L 保有の禁令 {lb}／{ln}（{100*lb/max(1,ln):.1f}%）・R17 L 輸出禁止 {le}（{100*le/max(1,ln):.1f}%）・R24 L blockadeRiskShare≠0 {br}／{ln}")
    # O
    ob = obi = oe = oex = 0
    for x in rows('opium', s):
        if x['scope'] != 'N': continue
        if x['possessionLawCode'] not in ('0', ''):
            ob += 1; obi += fl(x['importsPerClearing']) > 0
        if x['exportBanFlag'] == '1':
            oe += 1; oex += fl(x['exportsPerClearing']) > 0
    out.append(f"R16 O 禁令の四半期 {ob} のうち輸入＞0 {obi}（{100*obi/max(1,ob):.0f}%）・R17 O 輸出禁止の四半期 {oe} のうち輸出＞0 {oex}（{100*oex/max(1,oe):.0f}%）")
    # C
    cn = ci = ct = cf = 0; tt = []
    for x in rows('secession', s):
        cn += 1; a = fl(x['tariffInterestIndex']); b = fl(x['tariffTermIndex']); c = fl(x['consciousnessFactorShare'])
        ci += a == a and a != 0; ct += b == b and b != 0; cf += c == c and c != 1.0
        if b == b: tt.append(b)
    tt.sort()
    q = lambda p: tt[int(p * (len(tt) - 1))] if tt else float('nan')
    out.append(f"R13 C 州の関税利害≠0 {ci}／{cn}・R14 C 関税の項≠0 {ct}（p50 {q(.5):.4g}・p90 {q(.9):.4g}・最大 {tt[-1] if tt else float('nan'):.4g}）・国民意識の因子≠1 {cf}")
    print(f'## 種 {s}'); print('\n'.join(out))
