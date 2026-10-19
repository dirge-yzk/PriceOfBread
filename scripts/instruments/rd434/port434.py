# ★(434) 清算の片方向の錠・後半の読み手：計器 port_s<seed>.tsv（港の州×四半期）から、判定の入力を国×時代で出す（読むだけ・シミュを回さない）。
# 事前登録 measurements/cl_20261019_prereg.txt の追記 2。使い方：
#   PYTHONUTF8=1 python port434.py <計器の生のフォルダ> [--seeds 1,2,3,...] [--nations 清,ロシア,イギリス]
# 出す物（時代は T2 の 3 区分＝1783〜1850・1851〜1913・1914〜1949）：
#   pot＝港の州の潜在量（LastTradePotential）の四半期あたりの和の平均／cap＝港の容量の同じ量／gate＝建設の門を通った州×四半期の数（1 試合あたり）／need＝要る分の和（1 試合あたり）／
#   thr＝律速の州×四半期の割合（tradeThrottleIndex＜1）／flip＝律速の状態（＜1 か否か）が前の四半期から変わった数（1 試合あたり・境の往復）／cv＝潜在量の四半期間の変動（差分の絶対値の平均÷平均・州×種の中央値）。
# 別に `--json` で機械可読の辞書を出す（judge434.py が読む）。
import csv, sys, json, statistics, collections, argparse
ap = argparse.ArgumentParser()
ap.add_argument('folder'); ap.add_argument('--seeds', default='1,2,3,4,5,6,7,8,9,10'); ap.add_argument('--nations', default='清,ロシア,イギリス'); ap.add_argument('--json', action='store_true')
a = ap.parse_args()
SEEDS = [int(x) for x in a.seeds.split(',')]; NAT = a.nations.split(',')
def era(y): return 0 if y < 1851 else 1 if y < 1914 else 2
ERA = ['1783-1850', '1851-1913', '1914-1949']
# acc[era] -> 合計（3 国の和）。国別は accN[nation][era]
def newacc(): return {'pot': 0.0, 'cap': 0.0, 'gate': 0.0, 'need': 0.0, 'rows': 0, 'thr': 0, 'flip': 0.0, 'pairs': 0}
accN = {n: [newacc() for _ in range(3)] for n in NAT}
quarters = [set() for _ in range(3)]
cvs = collections.defaultdict(list)
used = 0
for s in SEEDS:
    try: f = open(f'{a.folder}/port_s{s}.tsv', encoding='utf-8')
    except OSError: continue
    used += 1
    ser = collections.defaultdict(list)
    with f:
        for r in csv.DictReader(f, delimiter='\t'):
            n = r['nation']
            if n not in accN: continue
            e = era(int(r['year'])); q = int(r['quarter']); quarters[e].add(q)
            A = accN[n][e]
            pot = float(r['tradePotentialWeight']); thr = float(r['tradeThrottleIndex'])
            A['pot'] += pot; A['cap'] += float(r['portCapacityEnd']); A['gate'] += float(r['congestedGate']); A['need'] += float(r['needMoneyAtQuarterEnd'])
            A['rows'] += 1; A['thr'] += 1 if not (thr >= 1.0) else 0
            ser[(n, r['region'])].append((q, e, pot, thr))
    for (n, rg), v in ser.items():
        v.sort()
        for i in range(1, len(v)):
            A = accN[n][v[i][1]]; A['pairs'] += 1
            if (not (v[i][3] >= 1.0)) != (not (v[i - 1][3] >= 1.0)): A['flip'] += 1
        pots = [x[2] for x in v]
        if len(pots) >= 40:
            m = sum(pots) / len(pots)
            if m > 1e-9: cvs[n].append(sum(abs(pots[i] - pots[i - 1]) for i in range(1, len(pots))) / (len(pots) - 1) / m)
if used == 0: raise SystemExit('port_s が読めない')
nq = [max(1, len(q)) for q in quarters]
out = {'seeds': used, 'nations': NAT, 'era': ERA, 'cell': {}, 'cv': {n: (statistics.median(v) if v else None) for n, v in cvs.items()},
       'cv_all': statistics.median([x for v in cvs.values() for x in v]) if cvs else None}
for e in range(3):
    S = newacc()
    for n in NAT:
        A = accN[n][e]
        out['cell'][f'{n}|{e}'] = {'pot': A['pot'] / used / nq[e], 'cap': A['cap'] / used / nq[e], 'gate': A['gate'] / used, 'need': A['need'] / used,
                                  'thr': A['thr'] / max(1, A['rows']), 'flip': A['flip'] / used, 'flipRate': A['flip'] / max(1, A['pairs']), 'rows': A['rows'] / used}
        for k in S: S[k] += A[k]
    out['cell'][f'SUM|{e}'] = {'pot': S['pot'] / used / nq[e], 'cap': S['cap'] / used / nq[e], 'gate': S['gate'] / used, 'need': S['need'] / used,
                              'thr': S['thr'] / max(1, S['rows']), 'flip': S['flip'] / used, 'flipRate': S['flip'] / max(1, S['pairs']), 'rows': S['rows'] / used}
if a.json: print(json.dumps(out))
else:
    print(f'folder={a.folder} seeds={used}')
    print('nation era | pot cap | gate need | thr% flip/game flipRate | rows/game')
    for k, c in out['cell'].items():
        n, e = k.split('|')
        print(f"{n} {ERA[int(e)]} | {c['pot']:.4g} {c['cap']:.4g} | {c['gate']:.1f} {c['need']:.3g} | {100*c['thr']:.1f} {c['flip']:.1f} {c['flipRate']:.4f} | {c['rows']:.0f}")
    print('cv', {n: round(v, 4) for n, v in out['cv'].items() if v is not None}, 'all', round(out['cv_all'], 4))
