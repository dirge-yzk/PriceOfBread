"""(270) 線B2：全ての国の解禁エピソードの静的な反事実（読むだけ）。python cf_pool.py <計器の出力ディレクトリ>
各エピソード（warStageOpenAfterGovFlag=1 が続く塊）を、記録した現在の施行シェアから再生する。歩幅＝記録した歩幅÷記録した政治予算の係数、
目標＝AI の整形どおり次の1段（段÷4）。届いた＝施行が段4。器が立つ四半期＝シェアが 0.75 を超え目標が 1.0。フィードバックは含まない。
基線（政治予算を掛けた木）の数は columns をそのまま数えた値（base_*）。"""
import csv, collections, math, sys
D = sys.argv[1]
def fl(x):
    try: return float(x)
    except: return float('nan')
def stage(c): return min(4, max(1, int(math.floor(c * 4 + 0.5))))
by = collections.defaultdict(list)
for s in range(1, 11):
    with open(f'{D}/nation_s{s}.tsv', encoding='utf-8', errors='replace') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            by[(s, r['nationIndex'])].append(r)
T = collections.Counter()
for rows in by.values():
    eps = []; cur = None
    for i, r in enumerate(rows):
        if r['warStageOpenAfterGovFlag'] == '1':
            if cur is not None and cur[-1] == i - 1: cur.append(i)
            else: cur = [i]; eps.append(cur)
    for e in eps:
        rs = [rows[k] for k in e]
        T['episodes'] += 1; T['open_q'] += len(rs)
        T['base_reach'] += any(stage(fl(r['militarySpendingAfterGovShare'])) == 4 for r in rs)
        T['base_vessel_q'] += sum(1 for r in rs if fl(r['militarySpendingAfterGovShare']) > 0.75 and int(r['militaryStageTargetAfterGovStage']) == 4)
        c = fl(rs[0]['militaryMoveCurrentShare']); reached = False
        for r in rs:
            f = fl(r['militaryMoveStepFraction']); pb = fl(r['militaryMovePushBudgetFactor'])
            f2 = f / pb if pb > 0 and f == f else f
            tgt = min(stage(c) + 1, 4) / 4
            c = c + f2 * (tgt - c)
            reached |= stage(c) == 4
            if c > 0.75 and tgt == 1.0: T['cf_vessel_q'] += 1
        T['cf_reach'] += reached
print(dict(T))
