# (900) c77lg：monlaw_s から S19 の法の段の動きを数える（読むだけ）。
# 使い方: python -X utf8 s19_monlaw.py <生のフォルダ> [種の数]
# 出す物：軸ごとの 行数・段の値の分布・上がった回・下がった回・1 四半期の |差| の最大・|差|≥2 の回・目標≠現在の行・目標が現在より上の行
import sys, os, collections
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
st = collections.defaultdict(lambda: dict(rows=0, up=0, dn=0, big=0, mx=0, tne=0, tup=0, vals=collections.Counter(), dnpol=collections.Counter(), dnwar=0))
prev = {}
for s in range(1, ns + 1):
    p = os.path.join(d, f'monlaw_s{s}.tsv')
    f = open(p, 'rb'); hdr = f.readline().decode().rstrip('\n').split('\t')
    ix = {h: i for i, h in enumerate(hdr)}
    axes = [h for h in hdr[11:] if not h.endswith('Target')]
    for raw in f:
        r = raw.decode().rstrip('\n').split('\t')
        key = (s, r[ix['nationUid']]); q = int(r[0])
        pv = prev.get(key)
        cur = {a: r[ix[a]] for a in axes}
        for a in axes:
            v = cur[a]; t = st[a]; t['rows'] += 1; t['vals'][v] += 1
            tg = r[ix[a + 'Target']] if (a + 'Target') in ix else ''
            try:
                fv = float(v); ft = float(tg) if tg not in ('', 'NA') else None
            except ValueError: continue
            if ft is not None and abs(ft - fv) > 1e-9:
                t['tne'] += 1
                if ft > fv: t['tup'] += 1
            if pv and pv[0] == q - 1 and a in pv[1]:
                try: d0 = fv - float(pv[1][a])
                except ValueError: continue
                if d0 > 1e-9: t['up'] += 1
                if d0 < -1e-9: t['dn'] += 1; t['dnpol'][r[ix['polityCode']]] += 1
                t['mx'] = max(t['mx'], abs(d0))
                if abs(d0) >= 2 - 1e-9: t['big'] += 1
        prev[key] = (q, cur)
print('軸\t行\t上がった\t下がった\t|差|最大\t|差|>=2\t目標≠現在\t目標が上\t値の分布（上位6）\t下がった時の政体コード')
for a, t in st.items():
    vs = ' '.join(f'{k}:{n}' for k, n in t['vals'].most_common(6))
    dp = ' '.join(f'{k}:{n}' for k, n in t['dnpol'].most_common(5))
    print(f"{a}\t{t['rows']}\t{t['up']}\t{t['dn']}\t{t['mx']:g}\t{t['big']}\t{t['tne']}\t{t['tup']}\t{vs}\t{dp}")
