# (1025) c96rd 門 N1・N2・N1′（両側・開幕の断面）と、足す行の級の数え（紙）
#   使い方：python gates1025.py <rows.tsv> <基準の生> <腕の生> [出力]
#     生＝prodcap_s<種>.tsv のあるフォルダ（種 1〜10 を全て読む）。基準と腕に同じフォルダを渡すと〈旗 0 の腕〉＝新÷旧が全て 1.0。
#   読む行：各種の prodcap_s の最初の (year, q) の行だけ（開幕の断面）・isRgo=1・ownerNation が world の開幕の持ち主（rows.tsv の nation）と
#           同じ行だけ（所有の変わった州の年は使わない）。鍵＝(region, good, recipe)・値＝productivity の合計（同じ鍵の個体を足す）。
#   門（事前登録 §4・結果を見る前に固定）：
#     N1  足した (州, 財) の新÷旧 ≧ 8.0（全ての種・全ての足した行の最小で読む）
#     N2  同じ比 ≦ 12.5（最大で読む）
#     N1′ 足していない鍵の新÷旧が |比−1| ≦ 1e-9 の割合＝100%（両方の生にある鍵だけ・片方にしか無い鍵は件数を名乗る）
#   級：足した (州, 財) の基準の級（class 列）＝全種で N か（形の必要性の確かめ）
import sys, os, glob, collections

ROWS, BASE, ARM = sys.argv[1], sys.argv[2], sys.argv[3]
OUT = sys.argv[4] if len(sys.argv) > 4 else None

added = {}   # (province, good) -> nation
for l in open(ROWS, encoding='utf-8'):
    if l.startswith('#') or l.startswith('nation\t'): continue
    c = l.rstrip('\n').split('\t')
    if c[-1] == '1': added[(c[2], c[1])] = c[0]

def opening(path):
    d = collections.defaultdict(float); cls = collections.defaultdict(set); owner = {}
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        first = None
        for l in f:
            c = l.rstrip('\n').split('\t')
            yq = (c[ix['year']], c[ix['q']])
            if first is None: first = yq
            if yq != first: break
            if c[ix['isRgo']] != '1': continue
            key = (c[ix['region']], c[ix['good']], c[ix['recipe']])
            d[key] += float(c[ix['productivity']]); cls[key].add(c[ix['class']]); owner[c[ix['region']]] = c[ix['ownerNation']]
    return first, d, cls, owner

seeds = sorted(int(os.path.basename(p)[len('prodcap_s'):-4]) for p in glob.glob(os.path.join(BASE, 'prodcap_s*.tsv')))
out = ['# gates1025 rows=%s base=%s arm=%s seeds=%s added=%d' % (ROWS, BASE, ARM, ','.join(map(str, seeds)), len(added)),
       'seed\tprovince\tgood\tbaseClass\tarmClass\tbaseProd\tarmProd\tratio']
r_add = []; n_eq = n_ne = n_only = 0; allN = True; ne_ex = []
for s in seeds:
    fb, b, cb, ob = opening(os.path.join(BASE, 'prodcap_s%d.tsv' % s))
    fa, a, ca, oa = opening(os.path.join(ARM, 'prodcap_s%d.tsv' % s))
    assert fb == fa, (s, fb, fa)
    for (prov, good), nat in sorted(added.items()):
        if ob.get(prov) != nat or oa.get(prov) != nat:
            out.append('%d\t%s\t%s\t(持ち主 %s/%s)\t\t\t\tskip' % (s, prov, good, ob.get(prov), oa.get(prov))); continue
        keys = [k for k in b if k[0] == prov and k[1] == good]
        bs = sum(b[k] for k in keys); as_ = sum(a.get(k, 0.0) for k in keys)
        bc = ''.join(sorted(set().union(*[cb[k] for k in keys]))) if keys else '-'
        ac = ''.join(sorted(set().union(*[ca.get(k, set()) for k in keys]))) if keys else '-'
        if bc != 'N': allN = False
        ratio = as_ / bs if bs > 0 else float('nan')
        r_add.append(ratio)
        out.append('%d\t%s\t%s\t%s\t%s\t%.6g\t%.6g\t%.6g' % (s, prov, good, bc, ac, bs, as_, ratio))
    for k in set(b) | set(a):
        if (k[0], k[1]) in added: continue
        if k not in b or k not in a: n_only += 1; continue
        if b[k] == 0 and a[k] == 0: n_eq += 1; continue
        if b[k] > 0 and abs(a[k] / b[k] - 1.0) <= 1e-9: n_eq += 1
        else:
            n_ne += 1
            if len(ne_ex) < 5: ne_ex.append((s, k, b[k], a[k]))
fin = [x for x in r_add if x == x]
mn = min(fin) if fin else float('nan'); mx = max(fin) if fin else float('nan')
out.append('# N1 min=%.6g（門 ≧8.0）=%s' % (mn, '通る' if mn >= 8.0 else '落ちる'))
out.append('# N2 max=%.6g（門 ≦12.5）=%s' % (mx, '通る' if mx <= 12.5 else '落ちる'))
tot = n_eq + n_ne
out.append('# N1′ 一致 %d/%d=%.6f（門 100%%）=%s・片方だけの鍵 %d・不一致の例 %s' % (n_eq, tot, n_eq / tot if tot else float('nan'), '通る' if n_ne == 0 else '落ちる', n_only, ne_ex))
out.append('# 足した行の基準の級が全て N=%s・読んだ比の数 %d（非有限 %d）' % (allN, len(r_add), len(r_add) - len(fin)))
txt = '\n'.join(out) + '\n'
if OUT: open(OUT, 'wb').write(txt.encode('utf-8'))
print('\n'.join(out[-4:]))
