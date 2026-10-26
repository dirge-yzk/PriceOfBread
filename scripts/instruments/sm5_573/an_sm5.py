# (573) 線 sm5：枠と機械の 5 回目の読み手（読むだけ）。事前登録 measurements/sm5_573_20261026_prereg.txt の [4]・追記 4 の判定を出す。
# 使い方：
#   python -X utf8 an_sm5.py <腕 B の計器の出力フォルダ> <基線の計器の出力フォルダ> [<腕 B_mod の計器の出力フォルダ>]
# 読む表：slot_s・mach_s・slotopen_s・fb_s・ivf_s・factory_s・wp_s・class_s・clock_s・cons_s（B）／factory_s・wp_s・class_s（基線）
# 判定の印：PASS／FAIL／NA（読めない＝母数が無い）。段1 の採否に数える物＝J2'・J4・J5・J7。J1・J7(a)(c) は配線の番人の類（配りの設計から ρ≈1 になる）＝表に出すが採否に数えない。
# 主要国：英＝イギリス・仏＝フランス・普＝プロイセン・米＝アメリカ。⚠挙げた国（POB_INSTRUMENTS_NATIONS）だけ毎四半期・他は年末の行。
import csv, glob, os, sys, statistics as st, math
NAT = {"英": "イギリス", "仏": "フランス", "普": "プロイセン", "米": "アメリカ", "清": "清", "日": "日本"}
LISTED = ["清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本", "アメリカ"]
M4 = ["英", "仏", "普", "米"]
TOL = 1e-9
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
def rank(v):
    n = len(v); o = sorted(range(n), key=lambda i: v[i]); r = [0.0] * n; i = 0
    while i < n:
        j = i
        while j + 1 < n and v[o[j + 1]] == v[o[i]]: j += 1
        for k in range(i, j + 1): r[o[k]] = (i + j) / 2.0
        i = j + 1
    return r
def pearson(rx, ry):
    mx, my = mean(rx), mean(ry)
    sxy = sum((a - mx) * (b - my) for a, b in zip(rx, ry)); sx = sum((a - mx) ** 2 for a in rx); sy = sum((b - my) ** 2 for b in ry)
    return sxy / math.sqrt(sx * sy) if sx > 0 and sy > 0 else float('nan')
def spearman(xs, ys):
    return pearson(rank(xs), rank(ys)) if len(xs) >= 3 else float('nan')
def partial_spearman(xs, ys, zs):
    """x と y の順位相関から z の順位を取り除いた偏相関（z＝建設の財の配給率）。"""
    if len(xs) < 4: return float('nan')
    rx, ry, rz = rank(xs), rank(ys), rank(zs)
    rxy, rxz, ryz = pearson(rx, ry), pearson(rx, rz), pearson(ry, rz)
    if any(v != v for v in (rxy, rxz, ryz)): return rxy
    den = math.sqrt(max(0.0, (1 - rxz ** 2) * (1 - ryz ** 2)))
    return (rxy - rxz * ryz) / den if den > 0 else float('nan')
def year_q(y): return (y - 1783) * 4
def ratio(a, b): return a / b if (a == a and b == b and b > 0) else float('nan')
def yseries(ser, y0, y1): return mean([v for (y, v) in ser if y0 <= y <= y1])
def load_slot(d, s):
    slot = {}
    for r in rows(os.path.join(d, f'slot_s{s}.tsv')): slot.setdefault(r['nation'], {})[int(r['quarter'])] = r
    return slot
def load_factory(d, s):
    fy = {}
    for r in rows(os.path.join(d, f'factory_s{s}.tsv')):
        fy.setdefault(r['nation'], []).append((int(r['year']), fl(r['outputRefValuePerMonth']), fl(r['industrializationMean'])))
    return fy
def rifle_low(d, s):
    """英の小銃の capitalEquipFill が 0.05 を下回る四半期の数（wp_s・挙げた国は毎四半期）。"""
    n = 0; tot = 0
    for r in rows(os.path.join(d, f'wp_s{s}.tsv')):
        if r['nation'] == 'イギリス' and r['good'] == '小銃':
            v = fl(r['capitalEquipFill'])
            if v == v: tot += 1; n += v < 0.05
    return n, tot
def capitalist_savings_1880(d, s):
    out = {}
    for r in rows(os.path.join(d, f'class_s{s}.tsv')):
        if r['className'] == '資本家' and r['year'] == '1880' and r['nation'] in [NAT[k] for k in M4]:
            out[r['nation']] = fl(r['savingsStock'])
    return out

def analyse(d, dbase):
    ss = seeds(d, 'slot_s'); n = len(ss); need8 = math.ceil(0.8 * n)
    print('seeds', ss, f'（8/10 の線＝{need8}/{n}）')
    # ---- 段0 に戻る記録（M0・R-t0b・S0-C）
    openMax = 0.0; lost = []; resetN = 0
    for s in ss:
        first = set()
        for r in rows(os.path.join(d, f'mach_s{s}.tsv')):
            if r['nation'] in first: continue
            first.add(r['nation']); v = fl(r['openXMaxRel']); openMax = max(openMax, v if v == v else 0); lost.append(fl(r['openLostShare']))
    print(f'[R-m0] 開幕の能力 X÷頭数 の相対差の最大={openMax:.3f}・X が頭数を下回った分の割合の範囲={min(lost):.4f}〜{max(lost):.4f}（原因＝動かない種類の機械がある個体。記録）')
    # ---- 台帳（番人の類）
    resBad = 0; resMax = 0.0; machBad = 0; machMax = 0.0
    for s in ss:
        for r in rows(os.path.join(d, f'slot_s{s}.tsv')): resBad += int(fl(r['residualBad']) or 0); v = fl(r['residualMaxRel']); resMax = max(resMax, v if v == v else 0)
        for r in rows(os.path.join(d, f'mach_s{s}.tsv')): machBad += int(fl(r['residualBad']) or 0); v = fl(r['residualMaxRel']); machMax = max(machMax, v if v == v else 0)
    print(f'[台帳] 枠 residualBad={resBad}・max={resMax:.2e}／機械 residualBad={machBad}・max={machMax:.2e}  [{mark(resBad == 0 and machBad == 0 and resMax <= 1e-9 and machMax <= 1e-9)}]')
    # ---- 種ごとの集計
    j1 = []; j1s = []; j2 = []; j4b = []; j4a = []; j5a = []; j5b = []; j5c = []
    j7a = 0; j7aRows = 0; j7b = 0; j7bRows = 0; j7bMax = 0.0; j7cBad = 0; j7d = []
    uk = []; ukb = []; idleAll = []; rateAll = []
    for s in ss:
        slot = load_slot(d, s)
        # J1：州の中の順位相関（重み＝工場数）
        sm = sw = ssm = ssw = 0.0
        for nat, q in slot.items():
            if nat not in LISTED: continue
            for t, r in q.items():
                a, w = fl(r['rhoAllSum']), fl(r['rhoAllW']); b2, w2 = fl(r['rhoStateSum']), fl(r['rhoStateW'])
                if a == a and w == w and w > 0: sm += a; sw += w
                if b2 == b2 and w2 == w2 and w2 > 0: ssm += b2; ssw += w2
        j1.append(sm / sw if sw > 0 else float('nan')); j1s.append(ssm / ssw if ssw > 0 else float('nan'))
        # J2'：上限のある率と私的な増え÷損耗の偏順位相関（建設の財の配給率を取り除く）
        fb = {}
        for r in rows(os.path.join(d, f'fb_s{s}.tsv')): fb.setdefault(r['nation'], {})[int(r['quarter'])] = r
        xs = []; ys = []; zs = []; xs_u = []; ys_u = []
        xr = []; yr = []
        for k in M4:
            q = slot.get(NAT[k], {}); f = fb.get(NAT[k], {})
            for t, r in q.items():
                ns = fl(r['nStart']); wn = fl(r['wornN']); pc = fl(r['planRateCap']); ap = fl(r['addedPriv'])
                if not (ns > 0 and wn > 0) or pc != pc or ap != ap: continue
                xs_u.append(pc); ys_u.append(ap / wn)
                fr = f.get(t)
                if fr and fl(fr['buildDemand']) > 0 and fl(fr['buildProd']) == fl(fr['buildProd']):
                    xs.append(pc); ys.append(ap / wn); zs.append(min(1.0, fl(fr['buildProd']) / fl(fr['buildDemand'])))
        j2.append((partial_spearman(xs, ys, zs), spearman(xs_u, ys_u)))
        # J4：英の小銃の充足
        j4b.append(rifle_low(d, s)[0]); j4a.append(rifle_low(dbase, s)[0] if dbase else float('nan'))
        # J5 / J7
        idle_by = {}
        for nat, q in slot.items():
            qs = sorted(q); prev = None
            for t in qs:
                r = q[t]; gap = (t - prev) if prev is not None else None; prev = t
                ns = fl(r['nStart']); dN = fl(r['deltaN'])
                if not (ns > 0) or nat not in LISTED: continue
                wn = fl(r['wornN'])
                if not (wn > 0): j7cBad += 1
                if gap == 1:
                    pg = fl(r['planGrow']); den = dN * max(ns, fl(r['sumN']) if fl(r['sumN']) == fl(r['sumN']) else ns) * 3   # 四半期の中で N が増えるので、四半期の頭と終わりの大きい方
                    if pg == pg and den > 0: j7aRows += 1; j7a += pg / den > 1 + TOL
                    ad = fl(r['added']);
                    if ad == ad and wn == wn and den > 0:
                        j7bRows += 1; v = (ad - wn) / den; j7bMax = max(j7bMax, v); j7b += v > 1 + TOL
                pr = fl(r['planRate'])
        rr = []; pp = []
        for k in M4:
            for t, r in slot.get(NAT[k], {}).items():
                wn = fl(r['wornN']); ap = fl(r['addedPriv']); pc = fl(r['planRateCap'])
                if fl(r['nStart']) > 0 and wn > 0 and ap == ap and pc == pc: rr.append(ap / wn); pp.append(pc)
        j7d.append(spearman(pp, rr))
        for k in M4:
            q = slot.get(NAT[k], {}); decs = {}
            ids = []
            for t, r in q.items():
                y = int(r['year']); v = fl(r['idleShare'])
                if v == v:
                    if 1820 <= y <= 1913: ids.append(v)
                    decs.setdefault((y // 10) * 10, []).append(v)
            if ids: j5a.append(med(ids))
            dm = [med(decs[x]) for x in sorted(decs) if 1790 <= x <= 1910]
            if len(dm) >= 8: j5b.append(all(b > a for a, b in zip(dm, dm[1:])))
            idleAll.append(dict((x, med(v)) for x, v in decs.items()))
        cs = capitalist_savings_1880(d, s); cb = capitalist_savings_1880(dbase, s) if dbase else {}
        rat = [cs[x] / cb[x] for x in cs if x in cb and cb[x] > 0]
        j5c.append(med(rat) if rat else float('nan'))
        fy = load_factory(d, s); ser = [(y, o) for (y, o, _) in fy.get('イギリス', [])]
        uk.append((yseries(ser, 1798, 1802), yseries(ser, 1848, 1852), yseries(ser, 1911, 1913)))
        if dbase:
            fb2 = load_factory(dbase, s); ser2 = [(y, o) for (y, o, _) in fb2.get('イギリス', [])]
            ukb.append((yseries(ser2, 1798, 1802), yseries(ser2, 1848, 1852), yseries(ser2, 1911, 1913)))
    print('==== 段1 機構（採否に数える：J2\'・J4・J5・J7(b)(d)） ====')
    ok2 = sum(1 for (p, u) in j2 if p > 0)
    print(f'[J2\'] 偏順位相関(planRateCap, 私的な増え÷損耗 | 建設の財の配給率) が正の種={ok2}/{n}（≥{need8}）値={[round(p,2) for p,_ in j2]}・配給率を取り除かない値={[round(u,2) for _,u in j2]}（記録）  [{mark(ok2 >= need8)}]')
    bl = med([x for x in j4a if x == x]); bb = med(j4b)
    g4 = (bl == bl) and bb <= bl
    print(f'[J4] 英の小銃の充足<0.05 の四半期の数 B 種の中央値={bb}・基線={bl}（種ごと B {j4b}／基線 {j4a}）  [{mark(g4 if bl == bl else None)}]')
    g5a = sum(1 for v in j5a if v <= 0.5) >= math.ceil(0.75 * len(j5a)) if j5a else None
    g5b = (sum(1 for v in j5b if v) <= 0.25 * len(j5b)) if j5b else None
    g5c = med(j5c) >= 0.1 if med(j5c) == med(j5c) else None
    print(f'[J5a] 主要 4 国×種の遊び（1820-1913 の中央値）≤0.5 の割合={sum(1 for v in j5a if v <= 0.5)}/{len(j5a)}（≥3/4）  [{mark(g5a)}]')
    print(f'[J5b] 十年の中央値が毎十年上がり続ける国×種={sum(1 for v in j5b if v)}/{len(j5b)}（≤1/4）  [{mark(g5b)}]')
    print(f'[J5c] 資本家の貯蓄 1880 年÷基線（4 国の中央値の種ごと）の中央値={med(j5c):.3f}（≥0.1）値={[round(x,2) for x in j5c]}  [{mark(g5c)}]')
    g7b = j7bRows > 0 and j7b == 0
    print(f'[J7b=S2] 国×四半期の差し引きの増え (added−wornN)÷(δ_N×nStart×3) の最大={j7bMax:.3f}・(1+1e-9) を超えた行={j7b}/{j7bRows}  [{mark(g7b if j7bRows > 0 else None)}]')
    g7d = sum(1 for v in j7d if v > 0)
    print(f'[J7d] スピアマン(planRateCap, 私的な増え÷損耗) が正の種={g7d}/{n}（≥{need8}）値={[round(v,2) for v in j7d]}  [{mark(g7d >= need8)}]')
    print('==== 配線の番人の類（配りの設計から ρ≈1 になる＝表に出すが採否に数えない） ====')
    j1p = sum(1 for v in j1 if v > 0)
    print(f'[J1] 同じ州の中の rate と 増え÷N の順位相関（種ごと・工場数加重）={[round(v,3) for v in j1]}・正の種={j1p}/{n}  国家＋巨大建造物={[round(v,3) for v in j1s]}')
    print(f'[J7a] 計画の増やす分 planGrow÷(δ_N×nStart×3) が 1+1e-9 を超えた行={j7a}/{j7aRows}  [{mark(j7a == 0 if j7aRows > 0 else None)}]・[J7c] worn≦0 の国×四半期={j7cBad}')
    print('==== 記録（採否に使わない） ====')
    ukA = [(o[1] / o[0] if o[0] > 0 else float('nan'), o[2] / o[1] if o[1] > 0 else float('nan')) for o in uk]
    print(f'[R-out] 英の工場の産出 B の 1850÷1800・1913÷1850 の種の中央値=({med([x[0] for x in ukA]):.3f}, {med([x[1] for x in ukA]):.3f})')
    if ukb:
        rr = [ratio(u[i], b[i]) for u, b in zip(uk, ukb) for i in (0, 1, 2)]
        print('[R-out] 英の工場の産出 B÷基線（1800・1850・1912 前後の種の中央値）:', [round(med([ratio(u[i], b[i]) for u, b in zip(uk, ukb)]), 3) for i in range(3)])
    ind = {}
    for s in ss:
        for nat, lst in load_factory(d, s).items():
            for (y, o, i2) in lst: ind.setdefault(nat, {}).setdefault(y, []).append(i2)
    bind = {}
    if dbase:
        for s in seeds(dbase, 'factory_s'):
            for nat, lst in load_factory(dbase, s).items():
                for (y, o, i2) in lst: bind.setdefault(nat, {}).setdefault(y, []).append(i2)
    print('[R-ind] 導く工業化度の国の平均（B/基線）:')
    for k in ["英", "仏", "普", "米", "清", "日"]:
        nat = NAT[k]
        print(f'   {k}:', ' '.join(f'{y}:{med(ind.get(nat, {}).get(y, [])):.2f}/{med(bind.get(nat, {}).get(y, [])):.2f}' for y in (1784, 1800, 1850, 1880, 1913)))
    mach = {}
    for s in ss:
        for r in rows(os.path.join(d, f'mach_s{s}.tsv')): mach.setdefault((s, r['nation']), {})[int(r['quarter'])] = r
    g8 = [0, 0]
    for s in ss:
        q = mach.get((s, 'イギリス'))
        if q:
            qs = sorted(q); t13 = min(qs, key=lambda t: abs(t - year_q(1913)))
            m0 = sum(fl(q[qs[0]][c]) for c in ('sumMAnimal', 'sumMWater', 'sumMSteam', 'sumMElectric')); m13 = sum(fl(q[t13][c]) for c in ('sumMAnimal', 'sumMWater', 'sumMSteam', 'sumMElectric'))
            g8[1] += 1; g8[0] += m13 > m0
    print(f'[J8] 英の ΣM 1913>開幕の種={g8[0]}/{g8[1]}')
    g1n = {k: 0 for k in M4}
    for s in ss:
        slot = load_slot(d, s)
        for k in M4:
            q = slot.get(NAT[k], {})
            if q:
                qs = sorted(q); t13 = min(qs, key=lambda t: abs(t - year_q(1913)))
                if fl(q[qs[0]]['sumN']) > 0 and fl(q[t13]['sumN']) / fl(q[qs[0]]['sumN']) >= 1.0: g1n[k] += 1
    print('[G1] 主要 4 国の ΣN 1913÷開幕 ≥1 の種:', g1n)
    # 建設の財の値段（J0(c) の記録）
    buildPx = {}
    for s in ss:
        for r in rows(os.path.join(d, f'fb_s{s}.tsv')):
            if r['nation'] == 'イギリス' and int(r['quarter']) < 4: buildPx.setdefault(s, []).append(fl(r['buildPxOverRefMain']))
    bpb = {}
    if dbase:
        for s in seeds(dbase, 'fb_s'):
            for r in rows(os.path.join(dbase, f'fb_s{s}.tsv')):
                if r['nation'] == 'イギリス' and int(r['quarter']) < 4: bpb.setdefault(s, []).append(fl(r['buildPxOverRefMain']))
    print(f"[R-px] 英の建設の財の値段÷参照価格（開幕 4 四半期の平均・種の中央値）B={med([mean(v) for v in buildPx.values()]):.2f}・基線={med([mean(v) for v in bpb.values()]):.2f}")
    # G7
    bindc = [0, 0]
    for s in ss:
        for r in rows(os.path.join(d, f'cons_s{s}.tsv')):
            de = fl(r['desiredMoneyPerMonth']); bu = fl(r['budgetMoneyPerMonth'])
            if de == de and bu == bu and de > 0: bindc[1] += 1; bindc[0] += de > bu
    print(f'[G7] 予算が希望を縛る州×四半期（cons_s）={bindc[0]}/{bindc[1]}（記録）')
    ident = [("J2'", ok2 >= need8), ('J4', g4 if bl == bl else None), ('J5a', g5a), ('J5b', g5b), ('J5c', g5c), ('J7b', g7b if j7bRows > 0 else None), ('J7d', g7d >= need8)]
    print('段1 の合否:', ' '.join(f'{k}={mark(v)}' for k, v in ident), '→', 'すべて PASS' if all(v is True for _, v in ident) else '外れ／NA あり＝採らない')

if __name__ == '__main__':
    analyse(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
