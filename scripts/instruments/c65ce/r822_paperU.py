"""(822) 記録（門ではない）：形 U の紙の予言＝人頭税の請求に物差しを掛けたら請求がいくつ倍になるか（請求の重みの平均）。
物差し A＝costPriceLevel×localPriceMultiplier（事前登録の素案の物差し）・B＝regionPriceLevel（記録として並べる）。格ごと（本国格＝Core と Integrated・植民地格）・凍結の行（frozenFlag＝1）は別。
使い方: python -I r822_paperU.py <cearn の生> <出力.tsv>"""
import sys, os, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
raw, outp = sys.argv[1], sys.argv[2]
acc = collections.defaultdict(lambda: [0.0, 0.0, 0.0])   # (seed, 区分) -> [Σclaim, ΣclaimA, ΣclaimB]
for sd in range(1, 21):
    p = os.path.join(raw, 'cearn_s%d.tsv' % sd)
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.rstrip('\n').split('\t')
            c = fl(a[ix['pollClaimPerMonth']])
            if not (c > 0): continue
            cls = 'colonial' if a[ix['tier']] == 'Colonial' else 'home'
            if a[ix['frozenFlag']] == '1': cls += '_frozen'
            A = fl(a[ix['costPriceLevel']]) * fl(a[ix['localPriceMultiplier']]); B = fl(a[ix['regionPriceLevel']])
            for key in ((sd, cls), ('all', cls)):
                v = acc[key]; v[0] += c; v[1] += c * A; v[2] += c * B
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('seed\tclass\tpollClaimSum\tratioA_costPriceLevelTimesLpm\tratioB_regionPriceLevel\n')
    for k in sorted(acc, key=lambda k: (str(k[0]), k[1])):
        v = acc[k]
        o.write('%s\t%s\t%.6g\t%.4f\t%.4f\n' % (k[0], k[1], v[0], v[1] / v[0], v[2] / v[0]))
print(open(outp, encoding='utf-8').read())
