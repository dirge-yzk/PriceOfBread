# (1357) c143sp 記録の読み手（採否に使わない）：同じ盤面の新÷旧（腕 B÷基準）の国家が買う財の値段と、stproc_s の国家の購入の値打ち。
#  使い方 python compare_sp.py <腕 B の ins> <基準の ins> <腕 Z の stproc_s のフォルダ> [種の起点 1] [種の数 10]
import sys, os, csv, statistics as st
csv.field_size_limit(1 << 30)
B, BASE, Z = sys.argv[1], sys.argv[2], sys.argv[3]
sb = int(sys.argv[4]) if len(sys.argv) > 4 else 1; n = int(sys.argv[5]) if len(sys.argv) > 5 else 10
STATE_BUYS = ['石炭', '原油', '帆船', '汽船', '缶詰', 'キニーネ']
NATS = ['イギリス', 'フランス', 'ロシア', 'アメリカ', 'プロイセン', '清']
YEARS = [1800, 1850, 1880, 1913, 1940]

def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for v in r: yield ix, v

def prices(ins):
    out = {}
    for s in range(sb, sb + n):
        p = os.path.join(ins, f'nationgood_s{s}.tsv')
        if not os.path.exists(p): continue
        for ix, v in rows(p):
            nm, g = v[ix['nation']], v[ix['good']]
            if nm not in NATS or g not in STATE_BUYS: continue
            y = int(float(v[ix['year']]))
            if y not in YEARS: continue
            try: px = float(v[ix['priceMeanGold']])
            except ValueError: continue
            out.setdefault((nm, g, y), {})[s] = px
    return out

pb, p0 = prices(B), prices(BASE)
print('# 同じ盤面の新÷旧：種ごとの比の中央（両方に行のある種だけ）・国×財×年')
for nm in NATS:
    for g in STATE_BUYS:
        cells = []
        for y in YEARS:
            a, b = pb.get((nm, g, y), {}), p0.get((nm, g, y), {})
            rs = [a[s] / b[s] for s in a if s in b and b[s] > 0]
            cells.append(f'{y}:{st.median(rs):.3g}(n{len(rs)})' if rs else f'{y}:-')
        print(f'{nm}\t{g}\t' + '\t'.join(cells))

def stproc(d):
    tot = {}
    for s in range(sb, sb + n):
        p = os.path.join(d, f'stproc_s{s}.tsv')
        if not os.path.exists(p): continue
        for ix, v in rows(p):
            k = v[ix['nation']]
            t = tot.setdefault(k, [0.0, 0.0, 0]); t[0] += float(v[ix['buyValueQ']]); t[1] += float(v[ix['wantValueQ']]); t[2] += int(v[ix['procCount']])
    return tot
tb, tz = stproc(B), stproc(Z)
print('# stproc_s：国家の購入の代金の和・want の値打ちの和・回数（腕 B／腕 Z＝旗 0）')
for k in NATS + ['アメリカ']:
    if k in tb or k in tz:
        b = tb.get(k, [0, 0, 0]); z = tz.get(k, [0, 0, 0])
        print(f'{k}\tB buy {b[0]:.4g} want {b[1]:.4g} n {b[2]}\tZ buy {z[0]:.4g} want {z[1]:.4g} n {z[2]}\t比 buy {b[0] / z[0] if z[0] > 0 else float("nan"):.3g}')
sb_ = sum(v[0] for v in tb.values()); sz = sum(v[0] for v in tz.values())
print(f'*全体\tB buy {sb_:.4g}\tZ buy {sz:.4g}\t比 {sb_ / sz if sz > 0 else float("nan"):.3g}')
