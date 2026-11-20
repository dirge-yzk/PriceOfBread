# popclose_s の残差・招聘/守備兵/吸収の量の分布を読む（記録だけ・門にしない）
# 使い方：python -X utf8 -I resid.py <popclose_s*.tsv を含むフォルダ> [しきい=0.01]
import sys, glob, os, collections
d = sys.argv[1]; thr = float(sys.argv[2]) if len(sys.argv) > 2 else 0.01
files = sorted(glob.glob(os.path.join(d, 'popclose_s*.tsv')))
tot = 0; fresh = 0; over = 0
byreg = collections.Counter(); sumres = collections.Counter()
flow = {k: [] for k in ('specialistInK','garrisonInK','colonyMergeInK')}
for f in files:
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            if len(c) != len(h): continue
            tot += 1
            if c[ix['fresh']] == '1': fresh += 1; continue
            try: rate = float(c[ix['residualPerPrev']]); res = float(c[ix['residualK']])
            except ValueError: continue
            if rate > thr: over += 1; byreg[c[ix['nation']] + '/' + c[ix['region']]] += 1; sumres[c[ix['nation']] + '/' + c[ix['region']]] += res
            for k in flow:
                v = float(c[ix[k]])
                if v > 0: flow[k].append(v)
print(f'seeds={len(files)} rows={tot} fresh={fresh} 残差率>{thr}={over}')
print('上位の州（件数・残差の和K）：')
for k, n in byreg.most_common(15): print(f'  {k}\t{n}\t{sumres[k]:.3f}')
for k, v in flow.items():
    v.sort(); n = len(v)
    if n == 0: print(f'{k}: 0 件'); continue
    q = lambda p: v[min(n - 1, int(p * n))]
    print(f'{k}: n={n} 合計={sum(v):.3f} 中央={q(.5):.4f} p90={q(.9):.4f} 最大={v[-1]:.4f}')
