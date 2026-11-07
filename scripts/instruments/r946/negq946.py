# (946) 29 州×種の 10 年（四半期<40）で marginIndex が負だった行の数（和と最大）。paper946.py の negq は種の中央だけを印字していた＝ここで全数を数える。
# 使い方: python -X utf8 -I negq946.py <world.json> <keep_dir> <out>
import sys, os, json
wp, keep, out = sys.argv[1:4]
w = json.load(open(wp, encoding='utf-8'))
tg = {(n['name'], p['city']) for n in w['nations'] for p in n['provinces'] if 'heldSince' in p}
L = []
for arm in ['base_ins', 'n1', 'n2']:
    tot = 0; rows = 0; mx = {}; minm = 9e9
    for s in range(1, 11):
        f = open(os.path.join(keep, arm, f'secession_s{s}.tsv'), encoding='utf-8')
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        cnt = {}
        for line in f:
            a = line.rstrip('\n').split('\t'); q = int(a[0])
            if q >= 40: break
            k = (a[ix['nation']], a[ix['region']])
            if k not in tg: continue
            try: m = float(a[ix['marginIndex']])
            except ValueError: continue
            rows += 1; minm = min(minm, m)
            if m < 0: tot += 1; cnt[k] = cnt.get(k, 0) + 1
        for k, v in cnt.items(): mx[k] = max(mx.get(k, 0), v)
    L.append(f'{arm}\t29州×種の10年の行 {rows}\tmargin<0 の行 {tot}\t最小の margin {minm:.4f}\t州ごとの最大 {mx}')
open(out, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
