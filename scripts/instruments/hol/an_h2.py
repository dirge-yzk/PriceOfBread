"""州の空洞化（作者 2026-09-30＝不具合）の読み手。python an_h2.py <計器の出力ディレクトリ> [-v] [-min K]
  入力＝標準の計器（`INS=1 bash batch.sh run <腕>` の <出力>/ins または POB_INSTRUMENTS）の region_s<seed>.tsv と migration_s<seed>.tsv（(255)＝旧の注入 u4e/inj_ghost.py の g<seed>.txt の代わり）。
  事象＝州の人口が自分のそれまでのピークの 1/10 未満（と 1/4 未満）へ初めて落ちた四半期（ピークは K 千人以上＝既定 1）。
  原因＝ピークから落ちた四半期までの減り D のうち、移民（州の間の出−入・入植の出−入・越境の出−入）の純の流出 Mig が
        D の半分以上なら「移民」、それ以外は「戦争・飢饉・その他」（窓の中の戦場の強さ・主食の充足の最小で細分）。
  -v5＝全ての原因の事象を1件ずつ（-v4 は移民だけ）。-v＝移民の事象を1件ずつ（行き先の上位・距離・始めと終わりの因子）。
  -ex rule｜old＝例外の版（(722) 既定は rule＝規則の版・開幕の人口は world.json の popK。old＝旧の名簿・出力は (722) の前の既定とバイトで同じ）。定義は hol_common.py。"""
import sys, os, collections, math, json
def fl(x):
    try: return float(x)
    except: return float('nan')
# 標準の計器（SimCore/Instruments.cs・INS=1 または POB_INSTRUMENTS）の region_s<seed>.tsv の列名 → この読み手の短い名前
COLS = {'quarter': 'q', 'year': 'yr', 'nationIndex': 'idx', 'nation': 'nation', 'region': 'region', 'tier': 'tier', 'activeFlag': 'state', 'populationK': 'pop', 'ruralPopulationK': 'rural',
        'opportunityIndex': 'opp', 'holdingFactor': 'keep', 'wageFactor': 'wage', 'landFactor': 'land', 'employmentShare': 'emp', 'pushFactor': 'push', 'housingFactor': 'hous',
        'wagePremium': 'wprem', 'arableLand': 'arable', 'migrationOutKPerQuarter': 'mOut', 'migrationInKPerQuarter': 'mIn', 'settlerOutKPerQuarter': 'cOut', 'settlerInKPerQuarter': 'cIn',
        'crossBorderOutKPerQuarter': 'xOut', 'crossBorderInKPerQuarter': 'xIn', 'warIntensity': 'war', 'foodFillShare': 'food'}
TIER = {'Colonial': 'col', 'Core': 'core', 'Integrated': 'int'}
def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            yield dict(zip(head, line.rstrip('\n').split('\t')))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))   # (718) python -I でも同じ場所の hol_common を読む
import hol_common
from hol_common import PL, dist, EXEMPT_OLD
EXMODE, args = hol_common.take_ex(sys.argv[1:])   # (722) -ex rule（既定・規則の版）｜old（旧の名簿・(718) の前の既定）
V = '-v' in args or '-v4' in args or '-v5' in args
V4 = '-v4' in args or '-v5' in args
V5 = '-v5' in args
KMIN = 1.0
if '-min' in args: KMIN = float(args[args.index('-min') + 1])
dirs = [a for i, a in enumerate(args) if not a.startswith('-') and not (i > 0 and args[i - 1] == '-min')]
TOT = collections.Counter()
# 例外の名簿（作者 2026-10-15＝重要でない北極の植民地の州などは最小限の例外・主要国の州は例外にしない）＝州の名前で引く。
# 名簿の州の事象は上の件数（止める線）から外し、下の別の行に数える（名簿の州の事象が無ければ出力は名簿を足す前とバイトで同じ）。
# (368) で入れた＝ヌーク（英領・北極圏）だけ（推奨・作者の承認は未確認）。ペトロパヴロフスクは規模が大きいので入れない。
EXEMPT = EXEMPT_OLD   # (718) 実体は hol_common.py。指示文に写された作者の回答〈例外はそれでよい〉（2026-10-26・原文は未確認）＝北極圏の植民地（(582) で足した）
TOTX = collections.Counter()
EXALL = set()   # (718) 例外と判定した州（rule の版の表示用）
EXSEEN = set()  # 例外の州のうち事象があった州
URB = collections.defaultdict(lambda: [0.0, 0.0])
MIG = collections.defaultdict(lambda: [0.0, 0.0])
EV = []
nseed = 0
for d in dirs:
    for gf in sorted([f for f in os.listdir(d) if f.startswith('region_s') and f.endswith('.tsv')], key=lambda f: int(f[len('region_s'):-4])):
        sd = int(gf[len('region_s'):-4]); nseed += 1
        R = collections.defaultdict(list)
        MP = collections.defaultdict(lambda: collections.Counter())   # 出る州 → 入る州 → 量（全期間）
        MPq = collections.defaultdict(list)                            # 出る州 → [(q, 入る州, 量)]
        caps = set()
        seenQ = set()
        for row in table(os.path.join(d, gf)):
            r = {new: row[col] for col, new in COLS.items()}
            for k in ('pop', 'opp', 'mOut', 'mIn', 'cOut', 'cIn', 'xOut', 'xIn', 'war', 'food', 'wprem', 'land', 'hous', 'emp', 'push', 'wage', 'prod', 'rural', 'arable', 'keep'): r[k] = fl(r.get(k, 'nan'))
            r['q'] = int(r['q']); r['yr'] = int(r['yr'])
            r['tier'] = TIER.get(r['tier'], 'int'); r['state'] = 'a' if r['state'] == '1' else 'l'
            key = (r['q'], r['idx'])
            if key not in seenQ: seenQ.add(key); caps.add((r['q'], r['region']))   # 国の最初の州＝首都
            R[r['region']].append(r)
            if r['state'] == 'a' and r['pop'] > 0:
                if r['q'] % 4 == 3: URB[r['yr']][0] += r['pop'] - r['rural']; URB[r['yr']][1] += r['pop']   # 年末の四半期（0 始まりの 3 番目）
                b = (r['yr'] - 1775) // 25 * 25 + 1775
                MIG[b][0] += r['mOut']; MIG[b][1] += r['pop']
        mg = os.path.join(d, 'migration_s%d.tsv' % sd)
        if os.path.exists(mg):
            for row in table(mg):
                v = fl(row['movedKPerQuarter'])
                MP[row['fromRegion']][row['toRegion']] += v; MPq[row['fromRegion']].append((int(row['quarter']), row['toRegion'], v))
        for reg, rows in R.items():
            exReg = hol_common.is_exempt(EXMODE, reg)   # (722) 規則の版の開幕の人口は world.json の popK（出力の最初の四半期を読まない）
            if exReg: EXALL.add(reg)
            pk = 0.0; pkI = 0
            done = {10: False, 4: False}
            for i, r in enumerate(rows):
                v = r['pop']
                if not math.isfinite(v): continue
                if v > pk: pk = v; pkI = i
                if pk < KMIN: continue
                for div in (10, 4):
                    if done[div] or not (v < pk / div): continue
                    done[div] = True
                    seg = rows[pkI + 1: i + 1]
                    D = pk - v
                    mig = sum(s['mOut'] - s['mIn'] for s in seg); col = sum(s['cOut'] - s['cIn'] for s in seg); xb = sum(s['xOut'] - s['xIn'] for s in seg)
                    M = mig + col + xb
                    other = D - M
                    wv = [s['war'] for s in seg if math.isfinite(s['war'])]; fv = [s['food'] for s in seg if math.isfinite(s['food'])]
                    warMax = max(wv, default=0.0); warMean = sum(wv) / len(wv) if wv else 0.0
                    foodMin = min(fv, default=1.0); foodMean = sum(fv) / len(fv) if fv else 1.0
                    # 原因＝移民（純の流出が減りの半分以上）／戦争（窓の中の戦場の強さの平均が 0.05 以上）／飢え（主食の充足の平均が飢えの死の線 0.8 未満＝恒常の飢え）／その他
                    if M >= 0.5 * D: cause = '移民'
                    elif warMean >= 0.05: cause = '戦争'
                    elif foodMean < 0.8: cause = '飢え'
                    else: cause = 'その他'
                    isCap = (r['q'], reg) in caps
                    if exReg: TOTX[(div, cause)] += 1; EXSEEN.add(reg)
                    else: TOT[(div, cause)] += 1
                    if div == 10 or (V4 and cause == '移民') or V5:
                        dst = collections.Counter()
                        for (qq, dd, vv) in MPq.get(reg, []):
                            if rows[pkI]['q'] < qq <= r['q']: dst[dd] += vv
                        top = dst.most_common(3)
                        EV.append(dict(ex=exReg, div=div, sd=sd, reg=reg, nat=r['nation'], tier=r['tier'], pk=pk, pky=rows[pkI]['yr'], yr=r['yr'], v=v, D=D, mig=mig, col=col, xb=xb, other=other,
                                       cause=cause, cap=isCap, warMax=warMax, foodMin=foodMin, top=top, a=rows[pkI], z=r,
                                       dist=[dist(reg, t[0]) for t in top]))
TOT10 = {k[1]: v for k, v in TOT.items() if k[0] == 10}
TOT4 = {k[1]: v for k, v in TOT.items() if k[0] == 4}
print(f'## 種 {nseed}・ピーク {KMIN:g} 千人以上')
print(f'## ピークの1/10 未満: {sum(TOT10.values())} 件 {TOT10}')
print(f'## ピークの1/4 未満: {sum(TOT4.values())} 件 {TOT4}')
if EXMODE == 'rule':   # (718) 規則の版＝規則の文と、当たった州を毎回出す（名簿が見えない版は検算できない）
    print(f'## 例外の規則 [{hol_common.RULE_TEXT}] に当たる州 {sorted(EXALL)}')
if TOTX and EXMODE == 'rule':
    print(f'## 例外の規則の州の事象（上の件数の外）{sorted(EXSEEN)}: 1/10 ' + str({k[1]: v for k, v in TOTX.items() if k[0] == 10}) + ' 1/4 ' + str({k[1]: v for k, v in TOTX.items() if k[0] == 4}))
elif TOTX:
    print(f'## 例外の名簿 {sorted(EXEMPT)} の事象（上の件数の外）: 1/10 ' + str({k[1]: v for k, v in TOTX.items() if k[0] == 10}) + ' 1/4 ' + str({k[1]: v for k, v in TOTX.items() if k[0] == 4}))
EV10 = [e for e in EV if e['div'] == 10 and not e['ex']]
by = collections.Counter((e['tier'], e['cause']) for e in EV10)
print('## 1/10 の格×原因: ' + '  '.join(f'{k[0]}/{k[1]} {v}' for k, v in sorted(by.items())))
szc = collections.Counter(('≥1000' if e['pk'] >= 1000 else '100-1000' if e['pk'] >= 100 else '10-100' if e['pk'] >= 10 else '<10', e['cause']) for e in EV10)
print('## 1/10 のピークの大きさ×原因: ' + '  '.join(f'{k[0]}/{k[1]} {v}' for k, v in sorted(szc.items())))
print('## 1/10 の首都: ' + str(sum(1 for e in EV10 if e['cap'])) + ' ' + str([(e['sd'], e['reg'], e['yr'], e['cause']) for e in EV10 if e['cap']]))
print('## 都市化率（世界）: ' + '  '.join(f'{y} {u / t:.3f}' for y, (u, t) in sorted(URB.items()) if y in (1790, 1820, 1850, 1880, 1910, 1940, 1950) and t > 0))
print('## 国内移民（州の間の出÷州の人口・四半期あたり‰）: ' + '  '.join(f'{b} {1000 * m / max(p, 1e-9):.2f}' for b, (m, p) in sorted(MIG.items())))
if V:
    F = ['opp', 'keep', 'wage', 'land', 'emp', 'push', 'hous', 'wprem', 'food', 'war']
    print('## 1/10 の事象（種・州・国・格・ピーク(年)→落ちた年の人口・減りの内訳〔州の間・入植・越境・その他〕・行き先の上位(距離km)）')
    print('##   因子（始め→終わり）: ' + ' '.join(F))
    for e in sorted(EV, key=lambda e: (e['cause'] != '移民', e['sd'], e['yr'])):
        tops = ', '.join(f'{t[0]}:{t[1]:.3g}({dd:.0f})' for t, dd in zip(e['top'], e['dist']))
        print(f"  [{e['cause']}・1/{e['div']}{'・例外の名簿' if e['ex'] else ''}] s{e['sd']} {e['reg']}（{e['nat']}・{e['tier']}{'・首都' if e['cap'] else ''}） {e['pk']:.4g}({e['pky']})→{e['v']:.3g}({e['yr']})  州間 {e['mig']:.3g} 入植 {e['col']:.3g} 越境 {e['xb']:.3g} 他 {e['other']:.3g}  → {tops}")
        print('      ' + ' '.join(f"{k} {e['a'][k]:.3g}→{e['z'][k]:.3g}" for k in F) + f"  耕地 {e['a']['arable']:.3g} 農村 {e['a']['rural']:.3g}→{e['z']['rural']:.3g}")
