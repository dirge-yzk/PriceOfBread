# (1174) c116hd 戦時の買いだめを消す（StockpilePanicWeight 0.5→0.0）の紙の台本と門。
#   エンジンの関数を呼ばない・計器の表だけを読む（教訓 ①）。読む表は ls と head -1 で確かめた物だけ：
#     nationgood_s（10 年ごと＋1913 の年末＝州の Stock と LastDemand の和）・nation_s（四半期の atWarMonths）・armsfill_s・pop_s・epi_s・warpair_s・labor_s・tb_s・sf_s・wars_s
#   使い方:
#     python -X utf8 -I hd.py paper <生のフォルダ> <種の起点> <種の終わり> <出力.json>
#     python -X utf8 -I hd.py gates <腕.json> <M.json> <N1.json> <N2.json> <F1.json> <F2.json>   （F1・F2＝keep_1150/n1・n2＝pop_s を持つヌル腕）
#   戦時＝その四半期の nation_s の atWarMonths > 0（nationgood は年末の四半期の行で読む）。
#   優先財＝主食（tier Staple）と軍需（Coefficients.MilitaryGoods の 7 財の名＝小銃・弾薬・火砲・軍艦・汽船・馬・自動車）＝Model.StockpileFactor の priority と同じ集合。
import sys, os, csv, json, statistics, collections
QMAX = int(os.environ.get('HD_QMAX', '100000'))   # 短い走りの対照：この四半期より前の行だけ（戦時の表が空になる行は読まれない）
MIL = {'小銃', '弾薬', '火砲', '軍艦', '汽船', '馬', '自動車'}
def rd(B, t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8', newline='') as f: return list(csv.DictReader(f, delimiter='\t'))
def fl(v):
    try: x = float(v)
    except (TypeError, ValueError): return float('nan')
    return x
def qn(v): return int(str(v).lstrip('q'))
def era(y): return '1783-1849' if y < 1850 else '1850-1913' if y < 1914 else '1914-1950'
def med(a): return statistics.median(a) if a else float('nan')
def paper(B, sb, se, out):
    g1 = collections.defaultdict(list); arms = []; famD = famP = 0.0; epiD = 0.0; nWarQ = 0
    tot = collections.Counter(); hit = collections.Counter(); impQ = 0.0; bias = 0.0; nwars = 0; miss = collections.Counter(); seeds = 0
    for s in range(sb, se + 1):
        nat = rd(B, 'nation', s)
        if nat is None: miss['nation'] += 1; continue
        seeds += 1
        war = {}; yend = {}
        for r in nat:
            q = qn(r['quarter'])
            if q >= QMAX: continue
            war[(q, r['nation'])] = fl(r['atWarMonths']) > 0
            y = int(r['year']); yend[y] = max(yend.get(y, -1), q)
        nWarQ += sum(1 for v in war.values() if v)
        ng = rd(B, 'nationgood', s)
        if ng is None: miss['nationgood'] += 1
        else:
            for r in ng:
                y = int(r['year']); q = yend.get(y)
                if q is None: continue
                d, st = fl(r['demandQty']), fl(r['stockQty'])
                if not (d > 0) or st != st: continue
                pr = r['tier'] == 'Staple' or r['good'] in MIL
                w = war.get((q, r['nation']), False)
                g1[('war' if w else 'peace') + ('_prio' if pr else '_other')].append(st / d)
                if pr: g1[('war' if w else 'peace') + ('_staple' if r['tier'] == 'Staple' else '_mil')].append(st / d)
        af = rd(B, 'armsfill', s)
        if af is None: miss['armsfill'] += 1
        else:
            for r in af:
                if not war.get((qn(r['quarter']), r['nation']), False): continue
                wq, gq = fl(r['wantQtyTotal']), fl(r['fillContribQtyTotal'])
                if wq > 0 and gq == gq: arms.append(gq / wq)
        pp = rd(B, 'pop', s)
        if pp is None: miss['pop'] += 1
        else:
            for r in pp:
                if r['scope'] != 'N' or not war.get((qn(r['quarter']), r['nation']), False): continue
                f, p = fl(r['deathsFamineK']), fl(r['popMeanK'])
                if f == f and p == p: famD += f; famP += p
        ep = rd(B, 'epi', s)
        if ep is None: miss['epi'] += 1
        else:
            for r in ep:
                if war.get((qn(r['quarter']), r['nation']), False): epiD += fl(r['deadK'])
        lab, wp = rd(B, 'labor', s), rd(B, 'warpair', s)
        if lab is None or wp is None: miss['labor/warpair'] += 1
        else:
            qy = {int(r['quarter']): int(r['year']) for r in lab}
            lq = {}
            for q, y in qy.items(): lq[y] = max(lq.get(y, -1), q)
            endq = set(lq.values()); mx = collections.defaultdict(int)
            for r in wp:
                q = int(r['quarter'])
                if q not in endq: continue
                m = int(fl(r['monthsGt100']))
                for n in (r['nationA'], r['nationB']):
                    if m > mx[(q, n)]: mx[(q, n)] = m
            for r in lab:
                q = int(r['quarter'])
                if q not in endq: continue
                e = era(int(r['year'])); tot[e] += 3; hit[e] += mx.get((q, r['nation']), 0)
        tb = rd(B, 'tb', s)
        if tb is None: miss['tb'] += 1
        else: impQ += sum(fl(r['impQ']) for r in tb if fl(r['impQ']) == fl(r['impQ']))
        sf = rd(B, 'sf', s)
        if sf is None: miss['sf'] += 1
        else:
            lastq = max(qn(r['quarter']) for r in sf)
            bias += sum(fl(r['biasSwitchCum']) for r in sf if qn(r['quarter']) == lastq)
        ws = rd(B, 'wars', s)
        if ws is None: miss['wars'] += 1
        else: nwars += sum(1 for r in ws if r['civilWarFlag'] == '0')
    o = {'dir': B, 'seeds': seeds, 'miss': dict(miss)}
    for k, v in g1.items(): o['g1_' + k + '_med'] = med(v); o['g1_' + k + '_n'] = len(v)
    o['g2_arms_war_med'] = med(arms); o['g2_arms_war_n'] = len(arms)
    o['g3_famine_war_perK'] = famD / famP if famP > 0 else float('nan'); o['g3_famine_war_deadK'] = famD
    o['rec_epi_war_deadK'] = epiD; o['rec_war_nation_quarters'] = nWarQ
    T, H = sum(tot.values()), sum(hit.values())
    o['rec_pin_share_all'] = H / T if T else float('nan')
    for e in tot: o['rec_pin_share_' + e] = hit[e] / tot[e]
    o['rec_tb_impQ'] = impQ; o['rec_sf_biasSwitchCum_end'] = bias; o['rec_wars_interstate'] = nwars
    with open(out, 'w', encoding='utf-8', newline='\n') as f: json.dump(o, f, ensure_ascii=False, indent=1, sort_keys=True)
    for k in sorted(o):
        if k != 'dir': print(k, o[k])
def W(v): return 1 + 2 * (max(v) / min(v) - 1)
def gates(a, m, n1, n2, f1, f2):
    J = lambda p: json.load(open(p, encoding='utf-8'))
    A, M, N1, N2, F1, F2 = map(J, (a, m, n1, n2, f1, f2))
    res = []
    k = 'g1_war_prio_med'; v = [M[k], N1[k], N2[k]]; w = W(v); lim = min(v) / w
    res.append(('G1 働いた（下）戦時×優先財の在庫÷需要の中央', A[k], f'< {lim:.6g}（min {min(v):.6g}÷{w:.6g}）', A[k] < lim))
    k = 'g2_arms_war_med'; v = [M[k], N1[k], N2[k]]; w = W(v); lim = min(v) / w
    res.append(('G2 壊さない（下）戦時の軍需の充足の中央', A[k], f'≧ {lim:.6g}（min {min(v):.6g}÷{w:.6g}）', A[k] >= lim))
    k = 'g3_famine_war_perK'; v = [M[k], F1[k], F2[k]]; w = W(v); lim = max(v) * w
    res.append(('G3 壊さない（上）戦時の飢えの死÷人口（M＋keep_1150 の N1・N2）', A[k], f'≦ {lim:.6g}（max {max(v):.6g}×{w:.6g}）', A[k] <= lim))
    for name, val, cond, ok in res: print(f'{name}: {val:.6g} {cond} → {"緑" if ok else "赤"}')
    for k in ('g1_war_prio_med', 'g1_war_staple_med', 'g1_war_mil_med', 'g1_war_other_med', 'g1_peace_prio_med', 'g1_peace_other_med', 'g2_arms_war_med', 'g3_famine_war_perK',
              'rec_epi_war_deadK', 'rec_war_nation_quarters', 'rec_pin_share_all', 'rec_pin_share_1783-1849', 'rec_pin_share_1850-1913', 'rec_pin_share_1914-1950', 'rec_tb_impQ', 'rec_sf_biasSwitchCum_end', 'rec_wars_interstate'):
        if k in A and k in M: print(f'新÷旧 {k}: 腕 {A[k]:.6g} M {M[k]:.6g} 比 {A[k] / M[k] if M[k] else float("nan"):.4f}  N1 {N1.get(k, float("nan")):.6g} N2 {N2.get(k, float("nan")):.6g}')
    print('判定:', '緑' if all(r[3] for r in res) else '赤')
def loo(m, n1, n2, f1, f2):
    # ヌル腕の一つ抜き：3 本のうち 1 本を腕と見なし、残り 2 本で幅と閾値を作る（G3 は M・F1・F2）。腕と見なしたヌル腕で門の判定が変わる率＝空振りの率。
    J = lambda p: json.load(open(p, encoding='utf-8'))
    M, N1, N2, F1, F2 = map(J, (m, n1, n2, f1, f2))
    for k, arms, lower, want in (('g1_war_prio_med', (M, N1, N2), True, '赤（働いていない）'), ('g2_arms_war_med', (M, N1, N2), True, '緑'), ('g3_famine_war_perK', (M, F1, F2), False, '緑')):
        trip = 0
        for i in range(3):
            v = [arms[j][k] for j in range(3) if j != i]; a = arms[i][k]; w = W(v)
            ok = a < min(v) / w if k.startswith('g1') else (a >= min(v) / w if lower else a <= max(v) * w)
            got = ('緑' if ok else '赤') if not k.startswith('g1') else ('赤（働いていない）' if not ok else '緑（空振り）')
            trip += got != want
            print(f'一つ抜き {k} 腕と見なした={i} 値 {a:.6g} 残り {v[0]:.6g},{v[1]:.6g} 幅 {w:.6g} → {got}')
        print(f'一つ抜き {k} 空振り {trip}/3')
if __name__ == '__main__':
    if sys.argv[1] == 'paper': paper(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5])
    elif sys.argv[1] == 'loo': loo(*sys.argv[2:7])
    else: gates(*sys.argv[2:8])
