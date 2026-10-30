# urbd (699) 手計算：都市化の目標の式の釣り合いの点の見込み（読むだけ・腕なし）
# 入力：keep_694 の生（urxpd_s・fd_s・pxgoods_s・nation_s・labor_s）。出力：標準出力（国×年代・種の中央値）。
# 自給の値と住居の費用は〈開幕 12 年の相対価格×その四半期の物価水準〉で見積もる（相対価格はその後一定と置く〈推〉）。
import sys, os, csv, math, statistics as st
sys.stdout.reconfigure(encoding='utf-8')
from collections import defaultdict

ROOT = sys.argv[1]
SEEDS = range(1, 11)
ERAS = [(1790, 1814), (1815, 1849), (1850, 1879), (1880, 1913), (1914, 1950)]
HOUSING_NEED = 0.03   # Coefficients.HousingNeedPerUrban（都市の POP 1 人あたりの住居の必要量）

def era(y):
    for a, b in ERAS:
        if a <= y <= b: return f"{a}-{b}"
    return None

def rows(path):
    with open(path, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        for x in r:
            yield dict(zip(h, x))

def fl(x):
    try: return float(x)
    except: return float('nan')

res = defaultdict(lambda: defaultdict(list))
for s in SEEDS:
    # 開幕 12 年の相対価格（値段÷物価水準）：主食 5 財の中央値と住居
    relS = defaultdict(list); relH = defaultdict(list)
    for d in rows(os.path.join(ROOT, f"pxgoods_s{s}.tsv")):
        if not d['time'].startswith('q'): continue
        r_ = fl(d['refPriceIndex']) * fl(d['priceOverRefTimesLevelMedianRatio'])
        if not (r_ == r_): continue
        if d['tier'] == 'Staple': relS[(d['nation'], d['time'])].append(r_)
        if d['goodName'] == '住居': relH[d['nation']].append(r_)
    tmp = defaultdict(list)
    for (n, t), v in relS.items(): tmp[n].append(st.median(v))
    S = {n: st.median(v) for n, v in tmp.items()}
    H = {n: st.median(v) for n, v in relH.items() if v}
    plev = {}
    for d in rows(os.path.join(ROOT, f"nation_s{s}.tsv")):
        plev[(d['nation'], d['quarter'])] = fl(d['priceLevel'])
    subq = defaultdict(float)
    for d in rows(os.path.join(ROOT, f"fd_s{s}.tsv")):
        if d['tier'] != 'Core': continue
        subq[(d['nation'], d['quarter'])] += fl(d['subPerMonth'])
    acc = defaultdict(lambda: defaultdict(float))
    for d in rows(os.path.join(ROOT, f"urxpd_s{s}.tsv")):
        y = int(d['year']); e = era(y)
        if not e: continue
        n, q = d['nation'], d['quarter']
        a = acc[(n, e)]
        T = fl(d['sumTotal']); PU = fl(d['sumPairUrban'])
        for k in ['regionMonths', 'famineGated', 'serfGated', 'sumFlowPos', 'sumFlowNeg', 'sumTotal', 'sumPairUrban',
                  'sumRegionPop', 'sumRegionUrban', 'wTargetPre', 'wCur', 'wIncU', 'wIncR', 'wZ']:
            a[k] += fl(d[k])
        pl = plev.get((n, q))
        farm = (T - PU) / 3.0   # 対の農村の側の規模（州×月の和÷3＝四半期の平均の和）
        if pl is not None and pl == pl and farm > 0 and n in S and T > 0:
            subV = subq.get((n, q), 0.0) * S[n] * pl / farm   # 農村の 1 人あたりの自給の値（月・名目）
            a['wSubV'] += subV * T; a['wSubN'] += T
            if n in H:
                a['wHous'] += HOUSING_NEED * H[n] * pl * T; a['wHousN'] += T
    for key, a in acc.items():
        if a['sumTotal'] <= 0: continue
        m = res[key]; T = a['sumTotal']
        iu = a['wIncU'] / T; ir = a['wIncR'] / T
        m['incU'].append(iu); m['incR'].append(ir)
        m['Rnom'].append(iu / ir if ir > 0 else float('nan'))
        m['z'].append(a['wZ'] / T)
        m['tgt'].append(a['wTargetPre'] / T)
        m['curPair'].append(a['wCur'] / T)
        m['regU'].append(a['sumRegionUrban'] / a['sumRegionPop'])
        m['pairCov'].append(T / a['sumRegionPop'])
        m['fam'].append(a['famineGated'] / a['regionMonths'])
        m['serf'].append(a['serfGated'] / a['regionMonths'])
        m['netPerMo'].append((a['sumFlowPos'] - a['sumFlowNeg']) / T)
        m['grossPerMo'].append((a['sumFlowPos'] + a['sumFlowNeg']) / T)
        if a['wSubN'] > 0:
            sv = a['wSubV'] / a['wSubN']
            m['subV'].append(sv)
            m['subShR'].append(sv / (ir + sv) if ir + sv > 0 else float('nan'))
            m['Rsub'].append(iu / (ir + sv) if ir + sv > 0 else float('nan'))
            if a['wHousN'] > 0:
                hu = a['wHous'] / a['wHousN']
                m['housU'].append(hu)
                m['Rsh'].append((iu - hu) / (ir + sv) if ir + sv > 0 else float('nan'))
                m['dsym'].append(2 * ((iu - hu) - (ir + sv)) / ((iu - hu) + (ir + sv)))
    lacc = defaultdict(list)
    for d in rows(os.path.join(ROOT, f"labor_s{s}.tsv")):
        e = era(int(d['year']))
        if e: lacc[(d['nation'], e)].append(fl(d['plainUnemploymentShare']))
    for key, v in lacc.items():
        v = [x for x in v if x == x]
        if v: res[key]['unemp'].append(sum(v) / len(v))
    print(f"# seed {s} done", file=sys.stderr)

def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')

cols = ['incU', 'incR', 'Rnom', 'z', 'tgt', 'curPair', 'regU', 'pairCov', 'fam', 'serf', 'netPerMo', 'grossPerMo',
        'subV', 'subShR', 'Rsub', 'housU', 'Rsh', 'dsym', 'unemp']
print('nation\tera\tn\t' + '\t'.join(cols))
for n in sorted({k[0] for k in res}):
    for a, b in ERAS:
        e = f"{a}-{b}"
        m = res.get((n, e))
        if not m or not m['z']: continue
        print(f"{n}\t{e}\t{len(m['z'])}\t" + '\t'.join(f"{med(m[c]):.4g}" for c in cols))
