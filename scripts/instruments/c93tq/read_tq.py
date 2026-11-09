# (1008) ident の生で G5・G7・G8 を読み、G6 を fiscal_s と並べて記録し、英 1913 の関税の 4 候補を切る。
# 使い方：python -X utf8 -I read_tq.py <計器の出力フォルダ> [種の一覧 1,2,...,10] [国=イギリス] [年=1913]
import sys, os, csv, statistics as st

d = sys.argv[1]
seeds = [int(x) for x in (sys.argv[2] if len(sys.argv) > 2 else '1,2,3,4,5,6,7,8,9,10').split(',')]
NAT = sys.argv[3] if len(sys.argv) > 3 else 'イギリス'
YR = sys.argv[4] if len(sys.argv) > 4 else '1913'
FLOOR = -0.25


def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        return {c: i for i, c in enumerate(h)}, list(r)


bad = dict(g5=0, g5split=0, g7bin=0, g7sum=0, g7gate=0, g8=0)
rows_n = 0
maxd = dict(g5=0.0, g8=0.0)
nanrows = 0
uk = dict(n=0, scan=0.0, dt=0.0, dc=0.0, dm=0.0, wa=0, wat=0, wac=0, wab=0, wb=0, wc=0, wd=0, xw=0, xp=0, xcap=0, capge1=0, capwedgeall=0)
ukn = {k: [] for k in ('tarEff', 'tariffNow', 'tariffTarget', 'treatyScale', 'exRate')}
ratio_rec = []
ratio1 = []
paid_uk = dict(t=0.0, c=0.0, imp=0.0, ex=[])
for s in seeds:
    pt, pn, pf = f'{d}/tradeskip_s{s}.tsv', f'{d}/nationgood_s{s}.tsv', f'{d}/fiscal_s{s}.tsv'
    if not (os.path.exists(pt) and os.path.exists(pn)):
        print('seed', s, 'MISSING')
        continue
    ix, rows = rd(pt)
    f = lambda r, n: float(r[ix[n]])
    seen = False
    for r in rows:
        if r[ix['nation']] == 'ALL':
            continue
        rows_n += 1
        duty, dt, dc = f(r, 'dutyMean'), f(r, 'dutyTariffMean'), f(r, 'dutyCornMean')
        e = abs(duty - dt - dc) / max(1.0, abs(duty))
        maxd['g5'] = max(maxd['g5'], e)
        if e > 1e-12:
            bad['g5'] += 1
        if f(r, 'xWedgeA') != f(r, 'xWedgeAT') + f(r, 'xWedgeAC') + f(r, 'xWedgeAB'):
            bad['g5split'] += 1
        allb = 0.0
        g = dict(sold=0.0, cap=0.0, wedge=0.0, pas=0.0)
        for k in range(10):
            a1, a2, a3, a4 = (f(r, f'{x}Bin{k}') for x in ('xSold', 'xCap', 'xWedge', 'xPass'))
            if f(r, f'ratioBin{k}') != a1 + a2 + a3 + a4:
                bad['g7bin'] += 1
            allb += a1 + a2 + a3 + a4
            g['sold'] += a1; g['cap'] += a2; g['wedge'] += a3; g['pas'] += a4
        if f(r, 'xPairs') != allb + f(r, 'ratioNaN'):
            bad['g7sum'] += 1
        if f(r, 'ratioNaN') > 0:
            nanrows += 1
        elif (g['cap'] != f(r, 'xCap') or g['wedge'] != f(r, 'xWedge') or g['pas'] != f(r, 'xPass') or g['sold'] != f(r, 'xSoldOut')):
            bad['g7gate'] += 1
        now, sc, eff = f(r, 'tariffNow'), f(r, 'treatyScale'), f(r, 'tarEff')
        e8 = abs(min(1.0, max(FLOOR, now * sc)) - eff)
        maxd['g8'] = max(maxd['g8'], e8)
        if e8 > 1e-12:
            bad['g8'] += 1
        if r[ix['nation']] == NAT and r[ix['year']] == YR:
            uk['n'] += 1
            sc_n = f(r, 'rowScan')
            uk['scan'] += sc_n; uk['dt'] += dt * sc_n; uk['dc'] += dc * sc_n; uk['dm'] += duty * sc_n
            for k in ('wa', 'wat', 'wac', 'wab', 'wb', 'wc', 'wd'):
                uk[k] += f(r, {'wa': 'xWedgeA', 'wat': 'xWedgeAT', 'wac': 'xWedgeAC', 'wab': 'xWedgeAB', 'wb': 'xWedgeB', 'wc': 'xWedgeC', 'wd': 'xWedgeD'}[k])
            uk['xw'] += f(r, 'xWedge'); uk['xp'] += f(r, 'xPairs'); uk['xcap'] += f(r, 'xCap')
            uk['capge1'] += sum(f(r, f'xCapBin{k}') for k in range(4, 10))   # 比 ≧1.0（境の 4 番目＝1.0 以上）
            if not seen:
                seen = True
                for k in ukn:
                    ukn[k].append(f(r, k))
    # G6 の記録：nationgood の払い（国×年の Σ財）と fiscal_s の関税の台帳
    ixn, rn = rd(pn)
    paid = {}
    for r in rn:
        k = (r[ixn['year']], r[ixn['nation']])
        v = float(r[ixn['dutyTariffPaid']]) + float(r[ixn['dutyCornPaid']])
        paid[k] = paid.get(k, 0.0) + v
        if r[ixn['nation']] == NAT and r[ixn['year']] == YR:
            paid_uk['t'] += float(r[ixn['dutyTariffPaid']]); paid_uk['c'] += float(r[ixn['dutyCornPaid']]); paid_uk['imp'] += float(r[ixn['crossImportValue']]); paid_uk['ex'].append(float(r[ixn['exRateAtPay']]))
    if os.path.exists(pf):
        ixf, rf = rd(pf)
        led = {}
        for r in rf:
            led.setdefault((r[ixf['year']], r[ixf['nation']]), []).append((int(r[ixf['monthIndex']]), float(r[ixf['ledgerTariffPerMonth']])))
        for (y, nat), v in paid.items():
            m = led.get((str(int(y) - 1), nat))   # 呼びの時点の年は直前の第 4 四半期の翌年＝台帳は 1 年前の年の印
            if not m or v == 0:
                continue
            m.sort()
            last3 = sum(x[1] for x in m[-3:])
            if last3 != 0:
                ratio_rec.append(v / last3)
            if m[-1][1] != 0:
                ratio1.append(v / m[-1][1])

print(f'行(国×財×種)={rows_n}  不一致：G5 duty の和 {bad["g5"]}（最大 {maxd["g5"]:.2e}）・xWedgeA の 3 分け {bad["g5split"]}')
print(f'G7：旧の度数との不一致 {bad["g7bin"]}・総和＋NaN≠xPairs {bad["g7sum"]}・門の件数との不一致 {bad["g7gate"]}（ratioNaN>0 の行 {nanrows}）')
print(f'G8：tarEff の不一致 {bad["g8"]}（最大 {maxd["g8"]:.2e}）')
if ratio1:
    print(f'G6 記録（直近 1 か月の台帳）：n={len(ratio1)} 中央値 {st.median(ratio1):.6f}  1±1e-6 の割合 {sum(abs(x - 1) < 1e-6 for x in ratio1) / len(ratio1):.3f}')
if ratio_rec:
    print(f'G6 記録（門ではない）：nationgood の払い÷fiscal_s の台帳の直近 3 か月の和  n={len(ratio_rec)} 中央値 {st.median(ratio_rec):.4f}  1±1e-6 の割合 {sum(abs(x - 1) < 1e-6 for x in ratio_rec) / len(ratio_rec):.3f}')
if uk['scan'] > 0:
    print(f'{NAT} {YR}（種を合算・行 {uk["n"]}）走査 {int(uk["scan"])}：duty 平均 {uk["dm"] / uk["scan"]:.4f}＝関税 {uk["dt"] / uk["scan"]:.4f}＋穀物法 {uk["dc"] / uk["scan"]:.4f}')
    wa = uk['wa']
    print(f'  楔 {int(uk["xw"])}（越境の組 {int(uk["xp"])} の {100 * uk["xw"] / max(1, uk["xp"]):.1f}%）のうち A {int(wa)}＝関税の分で通る {int(uk["wat"])}・穀物法の分で通る {int(uk["wac"])}・両方要る {int(uk["wab"])}／B {int(uk["wb"])} C {int(uk["wc"])} D {int(uk["wd"])}')
    print(f'  xCap {int(uk["xcap"])}（越境の組の {100 * uk["xcap"] / max(1, uk["xp"]):.1f}%）のうち売り手の値段が自分以上（比≧1.0）{int(uk["capge1"])}（{100 * uk["capge1"] / max(1, uk["xcap"]):.1f}%）')
    print('  国の値（種ごと・中央値）：' + '・'.join(f'{k} {st.median(v):.4f}' for k, v in ukn.items() if v))
    exm = st.median(paid_uk['ex']) if paid_uk['ex'] else float('nan')
    print(f'  払い（買い手の通貨・合算）：関税 {paid_uk["t"]:.4g}・穀物法 {paid_uk["c"]:.4g}・越境の輸入額（金建て・運賃込み）{paid_uk["imp"]:.4g}・払い÷輸入額（金建てに換算 ×exRate）{(paid_uk["t"] + paid_uk["c"]) * exm / max(1e-9, paid_uk["imp"]):.4f}')
