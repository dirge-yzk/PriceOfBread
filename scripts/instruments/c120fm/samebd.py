# (1201) c120fm：同じ盤面の新÷旧（記録）。nation_s の人口（populationK）と名目 GDP（nominalGdpPerYear）を、オスマン・プロイセン・オーストリアで
#   1800・1850・1900・1949 年の最初の四半期の行を腕と基準で並べ、種ごとの比を出す（読むだけ）。
#   python -X utf8 -I samebd.py <腕の ins> <基準の ins> <種の起点> <種の終わり>
import sys, csv, os
sys.stdout.reconfigure(newline='\n')
A, B, SB, SE = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
NAT = ['オスマン', 'プロイセン', 'オーストリア']; YRS = [1800, 1850, 1900, 1949]
def rd(d, s):
    out = {}
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for c in r:
            if len(c) < len(h): continue
            k = (c[ix['nation']], int(c[ix['year']]))
            if k not in out: out[k] = (float(c[ix['populationK']] or 0), float(c[ix['nominalGdpPerYear']] or 0))
    return out
print('seed\tnation\tyear\tpopA/popB\tgdpA/gdpB')
for s in range(SB, SE + 1):
    try: a, b = rd(A, s), rd(B, s)
    except FileNotFoundError: print(f'{s}\t欠け'); continue
    for n in NAT:
        for y in YRS:
            if (n, y) in a and (n, y) in b:
                pa, ga = a[(n, y)]; pb, gb = b[(n, y)]
                print(f"{s}\t{n}\t{y}\t{pa / pb if pb else float('nan'):.3f}\t{ga / gb if gb else float('nan'):.3f}")
            else: print(f"{s}\t{n}\t{y}\t腕{'有' if (n, y) in a else '無'}・基準{'有' if (n, y) in b else '無'}")
