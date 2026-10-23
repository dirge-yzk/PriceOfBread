# (511) 線 fs：F1（凶作で下がる）が外れた理由の切り分け（記録・採否に使わない＝F1 の判定は事前登録のとおり an_fs.py の値）。
# 使い方: python -I diag_f1.py <腕 B の計器の出力フォルダ>
# 同じ凶作の行（harvestMean がその州の全行の中央値の 0.8 倍以下）について、fillBase（旗なしの反事実）でも F1 と同じ検査を当てる＝州が買って凶作を凌ぐので旗なしでも下がらないのか、形が下げ止めているのかを分ける。
import csv, sys, glob, os, statistics, collections
csv.field_size_limit(1 << 30)
sys.stdout.reconfigure(encoding='utf-8')
d = sys.argv[1]
def fl(x):
    try: return float(x)
    except: return float('nan')
grp = collections.defaultdict(list)
for p in sorted(glob.glob(os.path.join(d, 'fs_s*.tsv'))):
    with open(p, encoding='utf-8', newline='') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            grp[(r['nation'], r['region'], r['tier'])].append((fl(r['harvestMean']), fl(r['fillBase']), fl(r['fillNew']), fl(r['peasantPopK']), fl(r['idleShare']), fl(r['unmetPerMonth']) / max(1e-12, fl(r['peasantNeedPerMonth']))))
def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float('nan')
tot = qual = okB = okN = 0
dr_b = []; dr_n = []; nd_b = []; nd_n = []; dr_unmet = []; nd_unmet = []
by_tier = collections.defaultdict(lambda: [0, 0, 0])
for k, g in grp.items():
    hm = med([x[0] for x in g])
    dr = [x for x in g if x[0] <= 0.8 * hm]; nd = [x for x in g if x[0] > 0.8 * hm]
    if len(dr) < 5: continue
    qual += 1
    b_ok = med([x[1] for x in dr]) <= med([x[1] for x in g]) - 0.01
    n_ok = med([x[2] for x in dr]) <= med([x[2] for x in g]) - 0.01
    okB += b_ok; okN += n_ok
    t = by_tier[k[2]]; t[0] += 1; t[1] += b_ok; t[2] += n_ok
    dr_b += [x[1] for x in dr]; dr_n += [x[2] for x in dr]; nd_b += [x[1] for x in nd]; nd_n += [x[2] for x in nd]
    dr_unmet += [x[5] for x in dr]; nd_unmet += [x[5] for x in nd]
print('対象の州 %d（凶作の行 5 行以上）：旗なし(fillBase)で凶作に反応した州 %d（%.3f）・旗あり(fillNew) %d（%.3f）' % (qual, okB, okB / max(1, qual), okN, okN / max(1, qual)))
for t, v in sorted(by_tier.items()): print('   %s：州 %d・fillBase で反応 %d・fillNew で反応 %d' % (t, v[0], v[1], v[2]))
print('凶作の行の fillBase 中央値 %.4f／それ以外 %.4f・fillNew 中央値 凶作の行 %.4f／それ以外 %.4f' % (med(dr_b), med(nd_b), med(dr_n), med(nd_n)))
print('凶作の行の平均 fillBase %.4f／それ以外 %.4f・平均 fillNew 凶作の行 %.4f／それ以外 %.4f' % (sum(dr_b) / len(dr_b), sum(nd_b) / len(nd_b), sum(dr_n) / len(dr_n), sum(nd_n) / len(nd_n)))
print('不足の割合（unmet÷need）の中央値 凶作の行 %.4f／それ以外 %.4f' % (med(dr_unmet), med(nd_unmet)))
