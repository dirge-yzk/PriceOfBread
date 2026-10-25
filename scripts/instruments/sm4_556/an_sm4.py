# (556) 線 sm4：枠と機械の 4 回目の読み手（読むだけ）。事前登録 measurements/sm4_556_20261026_prereg.txt の [4]・[10] 追記 2 の判定を出す。
# 使い方：
#   python -X utf8 an_sm4.py <腕 B の計器の出力フォルダ> 段0                         … canary の S0 の読み
#   python -X utf8 an_sm4.py <腕 B の計器の出力フォルダ> 腕 [<腕 A′ の計器の出力フォルダ>]  … 識別する線 G0・G2・G3・G4・G5′・G6・G10 と表に出す線 G1・G8・G9 と記録 R
# 読む表：slot_s・slotopen_s・slotdir_s・mach_s・fb_s・ivf_s・factory_s・clock_s・cons_s（B）／factory_s・clock_s（A′）
# 主要国：英＝イギリス・仏＝フランス・普＝プロイセン・米＝アメリカ・清・日＝日本。⚠ 挙げた国（POB_INSTRUMENTS_NATIONS）だけ毎四半期・他は年末の行。
# 判定の印：PASS／FAIL／NA（読めない＝母数が無い）。識別する線＝G0・G2・G3・G4（G4b）・G5′・G6・G10。G1・G8・G9 は頭打ちの速さで構造的に通る＝表に出すが採否に数えない（追記 2 ①）。
import csv, glob, os, sys, statistics as st, math
NAT = {"英": "イギリス", "仏": "フランス", "普": "プロイセン", "米": "アメリカ", "清": "清", "日": "日本"}
LISTED = ["清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本", "アメリカ"]
M4 = ["英", "仏", "普", "米"]
MMAX = 1.6
def rows(p):
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r, None)
        if h is None: return
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def seeds(d, pat):
    return sorted(int(os.path.basename(p)[len(pat):-4]) for p in glob.glob(os.path.join(d, pat + '*.tsv')))
def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')
def mean(v):
    v = [x for x in v if x == x]
    return sum(v) / len(v) if v else float('nan')
def mark(b): return 'PASS' if b is True else ('FAIL' if b is False else 'NA')
def spearman(xs, ys):
    n = len(xs)
    if n < 3: return float('nan')
    def rank(v):
        o = sorted(range(n), key=lambda i: v[i]); r = [0.0] * n; i = 0
        while i < n:
            j = i
            while j + 1 < n and v[o[j + 1]] == v[o[i]]: j += 1
            for k in range(i, j + 1): r[o[k]] = (i + j) / 2.0
            i = j + 1
        return r
    rx, ry = rank(xs), rank(ys); mx, my = mean(rx), mean(ry)
    sxy = sum((a - mx) * (b - my) for a, b in zip(rx, ry)); sx = sum((a - mx) ** 2 for a in rx); sy = sum((b - my) ** 2 for b in ry)
    return sxy / math.sqrt(sx * sy) if sx > 0 and sy > 0 else float('nan')
def year_q(y): return (y - 1783) * 4
def ratio(a, b): return a / b if (a == a and b == b and b > 0) else float('nan')
def yseries(ser, y0, y1): return mean([v for (y, v) in ser if y0 <= y <= y1])

def load_slot(d, s):
    slot = {}
    for r in rows(os.path.join(d, f'slot_s{s}.tsv')):
        slot.setdefault(r['nation'], {})[int(r['quarter'])] = r
    return slot
def load_factory(d, s):
    fy = {}
    for r in rows(os.path.join(d, f'factory_s{s}.tsv')):
        fy.setdefault(r['nation'], []).append((int(r['year']), fl(r['outputRefValuePerMonth']), fl(r['industrializationMean'])))
    return fy

# ---------------------------------------------------------------- 段0（canary）
def stage0(d):
    for s in seeds(d, 'slotopen_s'):
        print(f'--- 種 {s}')
        o = {r['nation']: r for r in rows(os.path.join(d, f'slotopen_s{s}.tsv'))}
        print('[S0-C] 国別 indDerived/indOld（開幕）と sumN:')
        for k, n in NAT.items():
            r = o.get(n)
            if r: print(f"  {k}: factories={r['factories']} sumN={fl(r['sumN']):.1f} bK={fl(r['bK']):.2f} bW={fl(r['bW']):.2f} indOld={fl(r['indOldSum']):.3f} indDerived={fl(r['indDerivedSum']):.3f} ratio={ratio(fl(r['indDerivedSum']), fl(r['indOldSum'])):.3f}")
        slot = load_slot(d, s)
        fb = {}
        for r in rows(os.path.join(d, f'fb_s{s}.tsv')): fb.setdefault(r['nation'], {})[int(r['quarter'])] = r
        iv = {}
        for r in rows(os.path.join(d, f'ivf_s{s}.tsv')): iv.setdefault(r['nation'], {})[int(r['quarter'])] = r
        print('[S0-B] T0b：R_b・R_m（開幕 4 四半期の平均・門は ≤0.5）')
        for k in M4:
            n = NAT[k]; qs = sorted(slot.get(n, {}))[:4]
            rb = []; rm = []
            for q in qs:
                sr = slot[n][q]; f = fb.get(n, {}).get(q); i = iv.get(n, {}).get(q)
                if f and fl(f['buildProd']) > 0: rb.append(fl(sr['reNeedBuilding']) / fl(f['buildProd']))
                if i and fl(i['machProd']) > 0: rm.append(fl(sr['reNeedMachine']) / fl(i['machProd']))
            print(f"  {k}: R_b={mean(rb):.3f} R_m={mean(rm):.3f}  [{mark(mean(rb) <= 0.5 and mean(rm) <= 0.5)}]")

# ---------------------------------------------------------------- 腕
def arms(d, dap):
    ss = seeds(d, 'slot_s')
    n = len(ss)
    need8 = math.ceil(0.8 * n)
    print('seeds', ss, f'（8/10 の線＝{need8}/{n}）')
    # ---- G0
    resBad = 0; resMax = 0.0; machBad = 0; machMax = 0.0; openMax = 0.0; openRatios = []; openLost = []
    for s in ss:
        for r in rows(os.path.join(d, f'slot_s{s}.tsv')):
            resBad += int(fl(r['residualBad']) or 0); v = fl(r['residualMaxRel']); resMax = max(resMax, v if v == v else 0)
        first = {}
        for r in rows(os.path.join(d, f'mach_s{s}.tsv')):
            machBad += int(fl(r['residualBad']) or 0); v = fl(r['residualMaxRel']); machMax = max(machMax, v if v == v else 0)
            if r['nation'] not in first:
                first[r['nation']] = 1; v = fl(r['openXMaxRel']); openMax = max(openMax, v if v == v else 0)
                openRatios.append(fl(r['openXRatio'])); openLost.append(fl(r['openLostShare']))
    g0 = resBad == 0 and resMax <= 1e-9 and machBad == 0 and machMax <= 1e-9 and openMax <= 1e-9
    print(f'[G0] M1 枠の台帳 residualBad={resBad}・max={resMax:.2e}／M2 機械の台帳 residualBad={machBad}・max={machMax:.2e}／M0 開幕の能力 openXMaxRel max={openMax:.2e}（ΣX÷Σ頭数 の範囲 {min(openRatios):.4f}〜{max(openRatios):.4f}・X が頭数を下回った分の割合 {min(openLost):.4f}〜{max(openLost):.4f}＝計器の注：動かない種類がある個体は X が下がる）  [{mark(g0)}]')
    # ---- 種ごとの集計
    g1 = {k: 0 for k in M4}; g1w = 0; g1wDen = 0; g9 = {k: 0 for k in M4}; g9m = {k: 0 for k in M4}
    viol5 = 0; rows5 = 0; skip5 = 0; maxr5 = 0.0; r5all = []
    wornZero = 0; rhoPos = 0; rhoVals = []; addedPos = [0, 0]
    satN = 0.0; planN = 0.0; satByDecade = {}
    disp = []; rateAll = []
    g2_viol = 0; g2_net_bad = 0; g2_den = 0; g2_up = 0; g2_by = {k: [0, 0] for k in NAT}
    g3 = {k: [] for k in M4}; g3m = {k: [] for k in M4}
    g8a = 0; g8b = 0; g8den = 0
    uk = []
    regCap = {}
    indSeries = {}
    for s in ss:
        slot = load_slot(d, s)
        tot0 = 0.0; tot50 = 0.0
        for nat, q in slot.items():
            qs = sorted(q)
            n0 = fl(q[qs[0]]['sumN']); t50 = min(qs, key=lambda t: abs(t - (year_q(1850) + 3)))
            n50 = fl(q[t50]['sumN'])
            if n0 == n0 and n50 == n50 and n0 > 0: tot0 += n0; tot50 += n50
        g1wDen += 1; g1w += (tot0 > 0 and tot50 / tot0 >= 1.0)
        for k in M4:
            q = slot.get(NAT[k], {})
            if not q: continue
            qs = sorted(q); t13 = min(qs, key=lambda t: abs(t - year_q(1913)))
            n0 = fl(q[qs[0]]['sumN']); n13 = fl(q[t13]['sumN'])
            if n0 > 0 and n13 / n0 >= 1.0: g1[k] += 1
            b0 = fl(q[qs[0]]['nBuilding']); b13 = fl(q[t13]['nBuilding'])
            if b0 > 0 and b13 / b0 >= 1.0: g9[k] += 1
            m0 = fl(q[qs[0]]['nMachinery']); m13 = fl(q[t13]['nMachinery'])
            if m0 > 0 and m13 / m0 >= 1.0: g9m[k] += 1
        xs = []; ys = []
        logs = []
        for nat, q in slot.items():
            qs = sorted(q)
            if nat in LISTED and len(qs) > 2:
                n0 = fl(q[qs[0]]['sumN']); t50 = min(qs, key=lambda t: abs(t - (year_q(1850) + 3))); n50 = fl(q[t50]['sumN'])
                if n0 > 0 and n50 > 0: logs.append(math.log(n50 / n0))
            prev = None
            for t in qs:
                r = q[t]
                gap = (t - prev) if prev is not None else None
                prev = t
                ns = fl(r['nStart']); dN = fl(r['deltaN'])
                if not (ns > 0): continue
                if nat in LISTED:
                    pn = fl(r['planN']); sn = fl(r['planNsat'])
                    if pn == pn and sn == sn:
                        planN += pn; satN += sn
                        dec = (int(r['year']) // 10) * 10
                        a = satByDecade.setdefault(dec, [0.0, 0.0]); a[0] += sn; a[1] += pn
                    pr = fl(r['planRate'])
                    if pr == pr: rateAll.append(pr)
                    if gap == 1:
                        rows5 += 1
                        ap = fl(r['addedPriv']); ratio5 = ap / (dN * ns * 3)
                        r5all.append(ratio5); maxr5 = max(maxr5, ratio5)
                        if ratio5 > (1 + MMAX) * 1.02 + 1e-12: viol5 += 1
                    else: skip5 += 1
                if not (fl(r['wornN']) > 0): wornZero += 1
                pr = fl(r['planRate'])
                if pr == pr and fl(r['wornN']) > 0: xs.append(pr); ys.append(fl(r['added']) / fl(r['wornN']))
                if pr == pr and pr >= 1: addedPos[1] += 1; addedPos[0] += fl(r['added']) > 0
                y = int(r['year']); rc = fl(r['nRegCap'])
                if rc == rc:
                    a = regCap.setdefault(nat, {}).setdefault(y, [0.0, 0.0, 0]); a[0] += rc; a[1] = max(a[1], rc); a[2] += 1
        rho = spearman(xs, ys); rhoPos += rho > 0; rhoVals.append(rho)
        if len(logs) > 2:
            m = mean(logs); disp.append(math.sqrt(mean([(x - m) ** 2 for x in logs])))
        for k, nat in NAT.items():
            q = slot.get(nat, {})
            if not q: continue
            qs = sorted(q); prev = None
            for t in qs:
                v = fl(q[t]['rTextile'])
                if v == v:
                    if prev is not None and v > prev + 1e-12: g2_viol += 1
                    prev = v
            r0 = fl(q[qs[0]]['rTextile']); r13 = fl(q[min(qs, key=lambda t: abs(t - year_q(1913)))]['rTextile'])
            if r0 == r0 and r13 == r13 and r13 > r0 + 1e-9: g2_net_bad += 1
            if r0 == r0 and r0 > 0.25 + 1e-12:
                g2_den += 1; g2_up += r13 < r0 - 1e-12; g2_by[k][1] += 1; g2_by[k][0] += r13 < r0 - 1e-12
        iv = {}
        for r in rows(os.path.join(d, f'ivf_s{s}.tsv')): iv.setdefault(r['nation'], []).append((int(r['year']), r))
        for k in M4:
            ser = []; mser = []
            for (y, r) in iv.get(NAT[k], []):
                mo = fl(r['bottleneckMonths']); rs = fl(r['bottleneckRationSum'])
                if mo > 0 and rs == rs: ser.append((y, rs / mo))
                dem = fl(r['machDemand']); pr = fl(r['machProd'])
                if dem > 0 and pr == pr: mser.append((y, min(1.0, pr / dem)))
            g3[k].append((yseries(ser, 1784, 1814), yseries(ser, 1850, 1880), yseries(ser, 1900, 1913)))
            g3m[k].append((yseries(mser, 1784, 1814), yseries(mser, 1850, 1880), yseries(mser, 1900, 1913)))
        mach = {}
        for r in rows(os.path.join(d, f'mach_s{s}.tsv')): mach.setdefault(r['nation'], {})[int(r['quarter'])] = r
        q = mach.get('イギリス', {})
        if q:
            qs = sorted(q); t13 = min(qs, key=lambda t: abs(t - year_q(1913)))
            m0 = sum(fl(q[qs[0]][c]) for c in ('sumMAnimal', 'sumMWater', 'sumMSteam', 'sumMElectric'))
            m13 = sum(fl(q[t13][c]) for c in ('sumMAnimal', 'sumMWater', 'sumMSteam', 'sumMElectric'))
            g8den += 1; g8a += m13 > m0; g8b += fl(q[t13]['mechShare']) > fl(q[qs[0]]['mechShare'])
        fy = load_factory(d, s)
        ser = [(y, o) for (y, o, _) in fy.get('イギリス', [])]
        uk.append((yseries(ser, 1798, 1802), yseries(ser, 1848, 1852), yseries(ser, 1911, 1913)))
        for nat, lst in fy.items():
            for (y, o, ind) in lst: indSeries.setdefault(nat, {}).setdefault(y, []).append(ind)
    # ---- 出力：識別する線
    print('==== 識別する線（外れが 1 つでもあれば採らない） ====')
    g2den_ok = g2_den > 0
    g2 = g2_net_bad == 0 and g2den_ok and g2_up >= 0.95 * g2_den
    print(f'[G2] 国の繊維の r(1913)>r(開幕) の国×種={g2_net_bad}（0 で PASS）・開幕で r>0.25 の国×種のうち r(1913)<r(開幕)={g2_up}/{g2_den}（≥0.95 で PASS）・国別={ {k: tuple(v) for k, v in g2_by.items()} }・国×四半期で上がった回数（記録）={g2_viol}  [{mark(g2 if g2den_ok else None)}]')
    g3ok = 0
    for k in M4:
        a = [x for x in g3[k] if all(v == v for v in x)]
        ok1 = (len(a) > 0 and med([x[1] for x in a]) > med([x[0] for x in a]) and med([x[2] for x in a]) >= med([x[1] for x in a]))
        g3ok += ok1
        print(f'[G3] {k} 建設の束の配給率（1784-1814 / 1850-1880 / 1900-1913 の種の中央値）:', [round(med([x[i] for x in a]), 3) for i in range(3)], f'（読めた種={len(a)}）向き={mark(ok1 if a else None)}')
    g3mok = 0
    for k in M4:
        am = [x for x in g3m[k] if all(v == v for v in x)]
        ok2 = len(am) > 0 and med([x[2] for x in am]) >= med([x[0] for x in am])
        g3mok += ok2
        print(f'     {k} 機械の供給÷需要（同）:', [round(med([x[i] for x in am]), 3) for i in range(3)], f'1900-1913≥1784-1814={mark(ok2 if am else None)}')
    g3res = g3ok >= 3 and g3mok >= 3
    print(f'[G3] 建設の束 {g3ok}/4 国・機械 {g3mok}/4 国（それぞれ ≥3 で PASS）  [{mark(g3res)}]')
    ukA = [(o[1] / o[0] if o[0] > 0 else float('nan'), o[2] / o[1] if o[1] > 0 else float('nan')) for o in uk]
    g4a = med([x[0] for x in ukA]); g4b = med([x[1] for x in ukA])
    g4own = g4a > 1.0 and g4b > 1.0
    print(f'[G4] 英の工場の産出（B）1850÷1800 の中央値={g4a:.3f}・1913÷1850 の中央値={g4b:.3f}（種ごと {[ (round(x[0],2), round(x[1],2)) for x in ukA]}）  [{mark(g4own)}]  ※水準は記録')
    g4bres = None
    if dap:
        pm = {}
        for s in seeds(dap, 'factory_s'):
            fy = load_factory(dap, s); ser = [(y, o) for (y, o, _) in fy.get('イギリス', [])]
            pm[s] = (yseries(ser, 1798, 1802), yseries(ser, 1848, 1852), yseries(ser, 1911, 1913))
        rat = []; r13 = []; r50 = []
        for s, u in zip(ss, uk):
            p = pm.get(s)
            if not p: continue
            gB = ratio(u[2], u[1]); gA = ratio(p[2], p[1])
            rat.append(ratio(gB, gA)); r13.append(ratio(u[2], p[2])); r50.append(ratio(u[1], p[1]))
        g4bres = (med(rat) > 1.0 and med(r13) > 1.0) if rat else None
        print(f'[G4b] (B の 1913÷1850)÷(A′ の 1913÷1850) の種の中央値={med(rat):.3f}（種ごと {[round(x,2) for x in rat]}）・B÷A′ の 1913 年の中央値={med(r13):.3f}・1850 年の中央値={med(r50):.3f}  [{mark(g4bres)}]')
        print('     ⚠外れたら〈4 回目も伸びの持ち主にならなかった〉を報告の先頭に書き、採らない')
    else:
        print('[G4b] A′ の出力フォルダが無い  [NA]')
    g5 = rows5 > 0 and viol5 == 0
    print(f"[G5'] 私的な増え addedPriv÷(δ_N×nStart×3) の最大={maxr5:.3f}・上限 {(1+MMAX)*1.02:.3f} を超えた行={viol5}/{rows5}（毎四半期の行がある国だけ・間が空いた行={skip5} 件は除外・違反 0 で PASS）・中央値={med(r5all):.3f}  [{mark(g5 if rows5 > 0 else None)}]")
    g6 = wornZero == 0 and rhoPos >= need8 and (addedPos[1] > 0 and addedPos[0] / addedPos[1] >= 0.9)
    print(f'[G6] worn≤0 の国×四半期={wornZero}（0 で PASS）・スピアマン(planRate, added/worn)>0 の種={rhoPos}/{n}（≥{need8}）rho={[round(x,2) for x in rhoVals]}・rate≥1 で added>0 の割合={addedPos[0]}/{addedPos[1]}（≥0.9）  [{mark(g6)}]')
    S = satN / planN if planN > 0 else float('nan')
    g10 = S == S and S <= 0.9
    dec = {k: round(v[0] / v[1], 3) for k, v in sorted(satByDecade.items()) if v[1] > 0}
    print(f'[G10] 張り付き S＝Σ planNsat÷Σ planN（挙げた 9 国・全種）={S:.3f}（≤0.9 で PASS）・十年ごと={dec}  [{mark(g10 if S == S else None)}]')
    ident = [('G0', g0), ('G2', g2 if g2den_ok else None), ('G3', g3res), ('G4', g4own), ('G4b', g4bres), ("G5'", g5 if rows5 > 0 else None), ('G6', g6), ('G10', g10 if S == S else None)]
    print('識別する線の合否:', ' '.join(f'{k}={mark(v)}' for k, v in ident), '→', 'すべて PASS' if all(v is True for _, v in ident) else '外れ／NA あり＝採らない')
    print('==== 表に出す（頭打ちの速さで構造的に通る＝採否に数えない） ====')
    print(f'[G1] 主要 4 国の ΣN 1913÷開幕 ≥1 の種:', {k: g1[k] for k in M4}, f'・世界の ΣN 1850 年末÷開幕 ≥1 の種={g1w}/{g1wDen}')
    print(f'[G8] 英の ΣM 1913>開幕の種={g8a}/{g8den}・機械化の割合が開幕より上の種={g8b}/{g8den}')
    print(f'[G9] 建造物の作り手の ΣN 1913÷開幕 ≥1 の種:', {k: g9[k] for k in M4}, '・機械の作り手:', {k: g9m[k] for k in M4})
    print('==== 記録（採否に使わない） ====')
    pr_q = [round(sorted(rateAll)[int(len(rateAll)*p)], 1) for p in (0.1, 0.5, 0.9)] if rateAll else None
    print(f'[R15] 国別の ΣN の伸び（1850 年末÷開幕の対数）の国の間の標準偏差の種ごと={[round(x,3) for x in disp]}・planRate の分位（9 国×四半期・p10/p50/p90）={pr_q}')
    print('[R7] 導く／旧の工業化度（factory_s の industrializationMean・種の中央値）B と A′ の比（年ごと）:')
    pind = {}
    if dap:
        for s in seeds(dap, 'factory_s'):
            for nat, lst in load_factory(dap, s).items():
                for (y, o, ind) in lst: pind.setdefault(nat, {}).setdefault(y, []).append(ind)
    for k in ["英", "仏", "普", "米", "清", "日"]:
        nat = NAT[k]
        out = []
        for y in (1784, 1790, 1800, 1820, 1850, 1880, 1913):
            b = med(indSeries.get(nat, {}).get(y, [])); a = med(pind.get(nat, {}).get(y, [])) if dap else float('nan')
            out.append(f'{y}:{b:.2f}/{a:.2f}' + (f'={ratio(b, a):.2f}' if a == a and a > 0 else ''))
        print(f'   {k}（B/A′=比）:', ' '.join(out))
    print('[R7] 導く工業化度が州の上限（5）以上の州の数（nRegCap・国×年の 行の平均／最大）:')
    for nat in ["イギリス", "フランス", "アメリカ", "ロシア", "清", "日本"]:
        yrs = regCap.get(nat, {})
        print(f'   {nat}:', {y: (round(v[0] / v[2], 2), int(v[1])) for y, v in sorted(yrs.items()) if y in (1790, 1820, 1850, 1880, 1913)})
    print('[R1] 英の工場の産出（B・種の中央値・1800 前後 / 1850 前後 / 1911-13）:', [round(med([u[i] for u in uk]), 1) for i in range(3)])
    for tag, dd in (('B', d), ('A′', dap)):
        if not dd: continue
        ws = {}
        for s in seeds(dd, 'clock_s'):
            for r in rows(os.path.join(dd, f'clock_s{s}.tsv')):
                if r['kind'] in ('W', 'w'): ws.setdefault(int(r['year']), []).append(fl(r['worldTotalPerMonth']))
        print(f'[R4] 世界の高度労働力 W（{tag}・種の中央値）:', {y: round(med(ws.get(y, [])), 0) for y in (1800, 1855, 1875, 1885, 1913)})
    print('[R16] 労働の需要の段差（mach_s sumL の四半期の前月比の最小・種の中央値と種ごと）:')
    for k in ["英", "仏", "米"]:
        mins = []
        for s in ss:
            q = {int(r['quarter']): fl(r['sumL']) for r in rows(os.path.join(d, f'mach_s{s}.tsv')) if r['nation'] == NAT[k]}
            qs = sorted(q); rr = [q[b] / q[a] for a, b in zip(qs, qs[1:]) if b - a == 1 and q[a] > 0]
            if rr: mins.append(min(rr))
        print(f'   {k}:', round(med(mins), 3), [round(x, 2) for x in mins])
    bind = [0, 0]
    for s in ss:
        for r in rows(os.path.join(d, f'cons_s{s}.tsv')):
            de = fl(r['desiredMoneyPerMonth']); bu = fl(r['budgetMoneyPerMonth'])
            if de == de and bu == bu and de > 0: bind[1] += 1; bind[0] += de > bu
    print(f'[G7] 予算が希望を縛る州×四半期（cons_s・Q160 まで・挙げた国）={bind[0]}/{bind[1]}（記録）')

if __name__ == '__main__':
    d = sys.argv[1]; mode = sys.argv[2] if len(sys.argv) > 2 else '段0'
    if mode == '段0': stage0(d)
    else: arms(d, sys.argv[3] if len(sys.argv) > 3 else None)
