# (1215) 止める線〈銀と金の値段の 100 倍〉：silverst_s の pAg・pAu を州ごとに、1 四半期以内（seq の差 1 以下）の 2 行だけで比べ（★(1254)・silverst_s は疎な表＝離れた行は比べない）、上がり・下がりの比の最大と、開幕の値への比の最大を出す。
# 100 倍以上の比があれば赤（基準と同じ台本で数え直して並べる）。非有限の値も数える。
# 使い方: python -X utf8 -I metal100.py <生> <種0> <種1>
import sys, csv, math
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
worst = {'pAg': (0, None), 'pAu': (0, None)}; nonfin = 0; open_w = {'pAg': 0, 'pAu': 0}
for s in range(s0, s1 + 1):
    last = {}; lastsq = {}; first = {}
    try: f = open(f'{raw}/silverst_s{s}.tsv', encoding='utf-8')
    except FileNotFoundError: print(f'seed {s} no silverst'); continue
    for r in csv.DictReader(f, delimiter='\t'):
        for c in ('pAg', 'pAu'):
            try: v = float(r[c])
            except (TypeError, ValueError): continue
            if not math.isfinite(v): nonfin += 1; continue
            if not v > 0: continue
            k = (r['region'], c)
            sq = int(r['seq'])
            if k in last and sq - lastsq[k] <= 1:
                q = max(v / last[k], last[k] / v)
                if q > worst[c][0]: worst[c] = (q, (s, r['year'], r['region']))
            if k not in first: first[k] = v
            o = max(v / first[k], first[k] / v)
            if o > open_w[c]: open_w[c] = o
            last[k] = v; lastsq[k] = sq
for c in ('pAg', 'pAu'):
    print(f'{c}\t1 四半期以内の行の比の最大 {worst[c][0]:.3f} {worst[c][1]}\t開幕比の最大（記録） {open_w[c]:.3f}')
red = any(worst[c][0] >= 100 for c in worst) or nonfin > 0
print(f'非有限 {nonfin}\t判定 {"赤" if red else "緑"}（1 四半期以内の行の比 100 以上か非有限）')
