"""(809) c13hl：州の空洞化（ピークの 1/4 未満）の事象ごとの減りの内訳と時系列（読むだけ）。
使い方: python -I r809_ev.py <ラベル>=<計器の出力ディレクトリ> ... [-o 出力の接頭辞]
  事象の定義は an_h2.py と同じ（ピーク 1 千人以上・ピークから初めて 1/4 未満・例外の規則 A2 は印を付けて残す）。
  ここで足す物（an_h2 に無い）：
   ①持ち主の国と格の交代（年・前→後）
   ②減りの内訳＝移民（州の間・入植・越境）＋残り（出生−死亡ほか）を、さらに 疫病の死（epi_s の deadK＝州ごと・正確）と
     〈持ち主の国の 1 人あたりの自然の増減〉（pop_s の N 行＝出生−平常の死−飢えの死−その他−戦死）を州の人口に当てた期待値へ分ける
     ＝残り − 疫病 − 期待値 ＝〈州に固有の差〉（年齢の構成の差・飢えの死の州への偏り・州の戦死ほか）。
     国の飢えの死を州へ配る近似（fd_s の belowNewK の州の割合・計器の国だけ）も別の列に出す＝近似と名乗る。
   ③10 年ごとの時系列（人口・農村・持ち主・格・主食の充足・1 人あたりの稼ぎ・機会・残る値打ち・正味の移民・残り・国の自然の率）。
入力の表：region_s・epi_s・pop_s（scope N）・fd_s（あれば）。エンジンの関数は呼ばない（表を自分で読む）。"""
import sys, os, collections, math
sys.path.insert(0, 'C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/hol')
import hol_common

def fl(x):
    try: return float(x)
    except: return float('nan')

def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            yield dict(zip(head, line.rstrip('\n').split('\t')))

args = sys.argv[1:]
outp = 'D:/PoB_work/r809_work/ev'
if '-o' in args:
    i = args.index('-o'); outp = args[i + 1]; del args[i:i + 2]
sets = [a.split('=', 1) for a in args]

summ = open(outp + '_summary.tsv', 'w', encoding='utf-8', newline='\n')
summ.write('\t'.join(['set', 'seed', 'region', 'exempt', 'tierAtPeak', 'ownerAtPeak', 'ownerAtDrop', 'ownerChanges', 'peakK', 'peakYear', 'dropK', 'dropYear', 'minK', 'minYear', 'endK',
                      'D', 'migInter', 'settler', 'cross', 'other', 'epiDead', 'expectedNatural', 'regionSpecific', 'famineApprox', 'migShare', 'cause_anh2', 'foodMeanWin', 'warMeanWin']) + '\n')
ts = open(outp + '_series.tsv', 'w', encoding='utf-8', newline='\n')
ts.write('\t'.join(['set', 'seed', 'region', 'year', 'nation', 'tier', 'popK', 'ruralK', 'food', 'earnPerCapMonth', 'opp', 'keep', 'wage', 'hous', 'emp',
                    'netMigK_10y', 'otherK_10y', 'epiK_10y', 'nationNatPerMille_y', 'nationFaminePerMille_y', 'regionNatPerMille_y']) + '\n')

for lab, d in sets:
    for gf in sorted([f for f in os.listdir(d) if f.startswith('region_s') and f.endswith('.tsv')], key=lambda f: int(f[len('region_s'):-4])):
        sd = int(gf[len('region_s'):-4])
        R = collections.defaultdict(list)
        for row in table(os.path.join(d, gf)):
            r = dict(q=int(row['quarter']), yr=int(row['year']), nat=row['nation'], reg=row['region'], tier=row['tier'], act=row['activeFlag'],
                     pop=fl(row['populationK']), rural=fl(row['ruralPopulationK']), opp=fl(row['opportunityIndex']), keep=fl(row['holdingFactor']),
                     wage=fl(row['wageFactor']), hous=fl(row['housingFactor']), emp=fl(row['employmentShare']),
                     mOut=fl(row['migrationOutKPerQuarter']), mIn=fl(row['migrationInKPerQuarter']), cOut=fl(row.get('settlerOutKPerQuarter', '0')), cIn=fl(row.get('settlerInKPerQuarter', '0')),
                     xOut=fl(row['crossBorderOutKPerQuarter']), xIn=fl(row['crossBorderInKPerQuarter']), war=fl(row['warIntensity']), food=fl(row['foodFillShare']),
                     earn=fl(row.get('earnedPerMonth', 'nan')))
            R[r['reg']].append(r)
        # 事象を先に見つける（an_h2 と同じ規則）
        evs = []
        for reg, rows in R.items():
            pk = 0.0; pkI = 0
            for i, r in enumerate(rows):
                v = r['pop']
                if not math.isfinite(v): continue
                if v > pk: pk = v; pkI = i
                if pk < 1.0: continue
                if v < pk / 4:
                    evs.append((reg, pkI, i)); break
        if not evs: continue
        need = set(e[0] for e in evs)
        EPI = collections.defaultdict(float)   # (region, q) -> deadK
        ep = os.path.join(d, 'epi_s%d.tsv' % sd)
        if os.path.exists(ep):
            for row in table(ep):
                if row['region'] in need: EPI[(row['region'], int(row['quarter']))] += fl(row['deadK'])
        NAT = {}   # (q, nation) -> (popMeanK, births, dNorm, dFam, dOth, war)
        pp = os.path.join(d, 'pop_s%d.tsv' % sd)
        for row in table(pp):
            if row['scope'] != 'N': continue
            NAT[(int(row['quarter']), row['nation'])] = tuple(fl(row[k]) for k in ('popMeanK', 'birthsK', 'deathsNormalK', 'deathsFamineK', 'deathsOtherK', 'warDeadK'))
        FD = {}   # (q, region) -> belowNewK ; FDN (q, nation) -> Σ belowNewK
        FDN = collections.defaultdict(float)
        fdp = os.path.join(d, 'fd_s%d.tsv' % sd)
        if os.path.exists(fdp):
            for row in table(fdp):
                q = int(row['quarter']); b = fl(row['belowNewK'])
                if not math.isfinite(b): continue
                FDN[(q, row['nation'])] += b
                if row['region'] in need: FD[(q, row['region'])] = b
        for reg, pkI, iDrop in evs:
            rows = R[reg]
            a = rows[pkI]; z = rows[iDrop]
            ex = hol_common.is_exempt('rule', reg)
            seg = rows[pkI + 1: iDrop + 1]
            D = a['pop'] - z['pop']
            mig = sum(s['mOut'] - s['mIn'] for s in seg); col = sum(s['cOut'] - s['cIn'] for s in seg); xb = sum(s['xOut'] - s['xIn'] for s in seg)
            other = D - mig - col - xb
            epi = sum(EPI.get((reg, s['q']), 0.0) for s in seg)
            expN = 0.0; famA = 0.0; famOK = True
            for s in seg:
                n = NAT.get((s['q'], s['nat']))
                if n is None or not (n[0] > 0): continue
                natChange = n[1] - n[2] - n[3] - n[4] - n[5]
                expN += s['pop'] * natChange / n[0]
                tb = FDN.get((s['q'], s['nat']), 0.0)
                if (s['q'], reg) in FD and tb > 0: famA += n[3] * FD[(s['q'], reg)] / tb
                else: famOK = False
            # 残り（減りの側の正）＝ −（出生−死亡）＝ other。期待値は増減（増えれば正）なので、州に固有の差＝other − epi − (−expN)
            regSpec = other - epi + expN
            M = mig + col + xb
            wv = [s['war'] for s in seg if math.isfinite(s['war'])]; fv = [s['food'] for s in seg if math.isfinite(s['food'])]
            warMean = sum(wv) / len(wv) if wv else 0.0; foodMean = sum(fv) / len(fv) if fv else 1.0
            cause = '移民' if M >= 0.5 * D else '戦争' if warMean >= 0.05 else '飢え' if foodMean < 0.8 else 'その他'
            ch = []
            for p, q in zip(rows, rows[1:]):
                if q['nat'] != p['nat'] or q['tier'] != p['tier']: ch.append(f"{q['yr']}:{p['nat']}/{p['tier']}→{q['nat']}/{q['tier']}")
            mn = min(range(len(rows)), key=lambda i: rows[i]['pop'] if math.isfinite(rows[i]['pop']) and i >= pkI else 1e300)
            summ.write('\t'.join(str(x) for x in [lab, sd, reg, int(ex), a['tier'], a['nat'], z['nat'], ';'.join(ch) or '-', f"{a['pop']:.4g}", a['yr'], f"{z['pop']:.4g}", z['yr'],
                                                  f"{rows[mn]['pop']:.4g}", rows[mn]['yr'], f"{rows[-1]['pop']:.4g}",
                                                  f'{D:.4g}', f'{mig:.4g}', f'{col:.4g}', f'{xb:.4g}', f'{other:.4g}', f'{epi:.4g}', f'{expN:.4g}', f'{regSpec:.4g}',
                                                  (f'{famA:.4g}' if famOK else f'{famA:.4g}(欠)'), f'{M / D:.3f}' if D > 0 else 'nan', cause, f'{foodMean:.3f}', f'{warMean:.3f}']) + '\n')
            # 10 年ごとの時系列（全期間）
            by = collections.defaultdict(list)
            for s in rows: by[(s['yr'] - 1780) // 10 * 10 + 1780].append(s)
            for y in sorted(by):
                g = by[y]; last = g[-1]
                nm = sum(s['mOut'] - s['mIn'] + s['cOut'] - s['cIn'] + s['xOut'] - s['xIn'] for s in g)
                idx0 = rows.index(g[0])
                prev = rows[idx0 - 1]['pop'] if idx0 > 0 else g[0]['pop']
                dpop = last['pop'] - prev
                oth = -(dpop + nm)   # 減りの側の正
                ep10 = sum(EPI.get((reg, s['q']), 0.0) for s in g)
                natR = []; famR = []
                for s in g:
                    n = NAT.get((s['q'], s['nat']))
                    if n and n[0] > 0: natR.append((n[1] - n[2] - n[3] - n[4] - n[5]) / n[0] * 4000); famR.append(n[3] / n[0] * 4000)
                popm = sum(s['pop'] for s in g) / len(g)
                yrs = len(g) / 4
                ts.write('\t'.join(str(x) for x in [lab, sd, reg, y, last['nat'], last['tier'], f"{last['pop']:.4g}", f"{last['rural']:.4g}", f"{last['food']:.3f}",
                                                    f"{last['earn'] / last['pop']:.4g}" if last['pop'] > 0 else 'nan', f"{last['opp']:.3f}", f"{last['keep']:.3f}", f"{last['wage']:.3f}",
                                                    f"{last['hous']:.3f}", f"{last['emp']:.3f}", f'{nm:.4g}', f'{oth:.4g}', f'{ep10:.4g}',
                                                    f'{sum(natR) / len(natR):.2f}' if natR else 'nan', f'{sum(famR) / len(famR):.2f}' if famR else 'nan',
                                                    f'{-oth / popm / yrs * 1000:.2f}' if popm > 0 and yrs > 0 else 'nan']) + '\n')
summ.close(); ts.close()
print('書いた:', outp + '_summary.tsv', outp + '_series.tsv')
