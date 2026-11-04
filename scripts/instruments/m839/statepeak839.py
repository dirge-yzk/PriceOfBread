# (839) 1 州の〈ピーク→その後の最小〉の比を、腕ごと・種ごとに並べる（読むだけ・an_h2 の閾値の近くにいる州の揺れを見る記録）。
#   python -X utf8 -I statepeak839.py <州の名> <名=ins フォルダ>...
#   出力：腕ごとに種ごとの〈ピーク（年）・ピークの後の最小（年）・比〉と、比 <1/4・<1/10 の種の数。
import sys, os, csv, glob
name = sys.argv[1]
for arg in sys.argv[2:]:
    lab, d = arg.split('=', 1)
    res = []
    for p in sorted(glob.glob(os.path.join(d, 'region_s*.tsv')), key=lambda p: int(os.path.basename(p)[8:-4])):
        s = int(os.path.basename(p)[8:-4]); ser = []
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                if x[ix['region']] == name:
                    try: ser.append((int(x[ix['year']]), float(x[ix['populationK']])))
                    except ValueError: pass
        if not ser: continue
        pk = max(range(len(ser)), key=lambda i: ser[i][1]); mn = min(ser[pk:], key=lambda t: t[1])
        res.append((s, ser[pk][0], ser[pk][1], mn[0], mn[1], mn[1] / ser[pk][1] if ser[pk][1] > 0 else float('nan')))
    q = sum(1 for t in res if t[5] < 0.25); t10 = sum(1 for t in res if t[5] < 0.1)
    print(f'{lab}\t種 {len(res)}\t1/4 未満 {q}\t1/10 未満 {t10}\t' + '  '.join(f's{s}:{r:.3f}' for s, _, _, _, _, r in res))
