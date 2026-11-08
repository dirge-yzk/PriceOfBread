# (964) c86lt sup945.py の写し：種 11〜20 を読む（S11）
# (945) c83lt 議会の支持の新÷旧（記録・読むだけ）：tab937 に議会の支持の列が無いので、reform_s の supportShare（政府の議会の支持の割合）と nation_s の parliamentGripIndex を
#   英・清の窓ごとに種と四半期の平均で新÷旧に並べる。土地税の法案そのものへの支持ではない（限界）。
#   使い方: python -X utf8 sup945.py <腕の ins> <基準の ins>
import csv, glob, sys, collections
W = [(1783,1785),(1786,1799),(1800,1849),(1850,1899),(1900,1950)]
def mean(I, tab, col):
    s = collections.defaultdict(float); c = collections.Counter()
    for f in glob.glob(f'{I}/{tab}_s*.tsv'):
        sd = int(f.rsplit('_s',1)[1][:-4])
        if not 11 <= sd <= 20: continue
        for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
            if r['nation'] not in ('イギリス','清'): continue
            y = int(r['year']); v = r.get(col)
            try: v = float(v)
            except: continue
            if v != v: continue
            for i,(a,b) in enumerate(W):
                if a <= y <= b: s[(r['nation'],i)] += v; c[(r['nation'],i)] += 1
    return {k: s[k]/c[k] for k in c}
A, B = sys.argv[1], sys.argv[2]
for tab, col in (('reform','supportShare'), ('nation','parliamentGripIndex')):
    a, b = mean(A, tab, col), mean(B, tab, col)
    for n in ('イギリス','清'):
        print(n, f'{tab}.{col} 新÷旧：' + '・'.join(f'{W[i][0]}-{str(W[i][1])[2:]} ' + (f'{a[(n,i)]/b[(n,i)]:.3f}' if (n,i) in a and (n,i) in b and b[(n,i)] else 'nan') for i in range(5)))
