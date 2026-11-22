# (1306) 鎖の環ごとの差（重税−軽税・助走の後の 20 四半期の平均）を勢いの重み 0.3 と 0 で並べる
# 使い方: python -X utf8 -I a1_chain.py <out のフォルダ> <tag...>
import sys, csv, collections
out = sys.argv[1]
def load(path):
    with open(path, encoding='utf-8', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))
res = []
for tag in sys.argv[2:]:
    rows = load(f'{out}/pop_{tag}.tsv')
    reg = load(f'{out}/reg_{tag}.tsv')
    # 世界ごと：全体の充足・闘争度（規模加重）、階級ごとの平均
    W = {}
    for tax in ('0', '0.6'):
        acc = collections.defaultdict(float)
        cls = collections.defaultdict(lambda: collections.defaultdict(float))
        for r in rows:
            if r['tax'] != tax or int(r['q']) < 1 or r.get(None): continue
            sz = float(r['size'])
            acc['size'] += sz; acc['sat'] += sz * float(r['sat']); acc['mil'] += sz * float(r['mil'])
            c = cls[int(r['class'])]
            c['name'] = r['name']
            for k in ('size',):
                c[k] += sz
            for k in ('sat', 'mil', 'milVoice', 'milFamine', 'milWar', 'milNation', 'consc', 'savings', 'earned', 'paid', 'unemp'):
                c[k] += sz * float(r[k])
            fn = float(r['foodNeed']); c['foodfill'] += sz * (min(1.0, float(r['foodMet']) / fn) if fn > 1e-9 else 1.0)
            c['foodRef'] += sz * float(r['foodRef'])
        W[tax] = (acc, cls)
    # 試験と同じ構成を固定した闘争度の差は四半期ごとの和の比（milC/sizeC）
    l, h = W['0'][1], W['0.6'][1]
    num = den = 0
    for c in l:
        if c in h and l[c]['size'] > 0 and h[c]['size'] > 0:
            num += l[c]['size'] * (h[c]['mil'] / h[c]['size'] - l[c]['mil'] / l[c]['size']); den += l[c]['size']
    # 試験の充足は四半期ごとの平均の平均＝ここは近似（規模がほぼ一定）
    satd = W['0.6'][0]['sat'] / W['0.6'][0]['size'] - W['0'][0]['sat'] / W['0'][0]['size']
    print(f'== {tag}: 構成固定の闘争度の差 {num/den:+.4f}・充足の差（近似） {satd:+.4f}')
    print('class name        size_l   size_h  | sat_l  sat_h | mil_l  mil_h | voice_l voice_h | famine_l famine_h | food_l food_h | fRef_l fRef_h | consc_l consc_h | earned_l earned_h | sav_l sav_h | unemp_l unemp_h')
    for c in sorted(l):
        if c not in h: continue
        L, H = l[c], h[c]
        def m(d, k): return d[k] / d['size'] if d['size'] > 0 else float('nan')
        print(f"{c:>3} {L['name']:<8} {L['size']/20:8.0f} {H['size']/20:8.0f} | {m(L,'sat'):.3f} {m(H,'sat'):.3f} | {m(L,'mil'):.3f} {m(H,'mil'):.3f} | {m(L,'milVoice'):.3f} {m(H,'milVoice'):.3f} | {m(L,'milFamine'):.3f} {m(H,'milFamine'):.3f} | {m(L,'foodfill'):.3f} {m(H,'foodfill'):.3f} | {m(L,'foodRef'):.3f} {m(H,'foodRef'):.3f} | {m(L,'consc'):.3f} {m(H,'consc'):.3f} | {m(L,'earned'):.4g} {m(H,'earned'):.4g} | {m(L,'savings'):.4g} {m(H,'savings'):.4g} | {m(L,'unemp'):.3f} {m(H,'unemp'):.3f}")
