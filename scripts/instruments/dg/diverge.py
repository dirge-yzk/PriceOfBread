# 流れG (320)：2本の計器の出力（ins のフォルダ）で、同じ名前の表が最初に違う行の四半期（か年）を種ごとに出す（読むだけ）。ヌル腕が雑音の床として働くかの検算。
# 使い方：PYTHONUTF8=1 python diverge.py <A の ins> <N の ins> [表の接頭辞=nation]
import sys, glob, os, re
a, n = sys.argv[1], sys.argv[2]; stem = sys.argv[3] if len(sys.argv) > 3 else 'nation'
res = []
for f in sorted(glob.glob(os.path.join(a, stem + '_s*.tsv'))):
    g = os.path.join(n, os.path.basename(f))
    if not os.path.exists(g): continue
    la = open(f, encoding='utf-8').read().split('\n'); lb = open(g, encoding='utf-8').read().split('\n')
    hdr = la[0].split('\t')
    iq = hdr.index('quarter') if 'quarter' in hdr else -1; iy = hdr.index('year') if 'year' in hdr else -1
    first = None
    for i in range(1, min(len(la), len(lb))):
        if la[i] != lb[i]:
            p = la[i].split('\t'); first = (p[iq] if iq >= 0 else '?', p[iy] if iy >= 0 else '?'); break
    if first is None and len(la) != len(lb): first = ('長さ', '')
    sd = re.search(r'_s(\d+)\.tsv', f).group(1)
    res.append((int(sd), first))
for sd, fr in sorted(res): print(f'種{sd}: 最初に違う行 quarter={fr[0] if fr else "なし"} year={fr[1] if fr else ""}')
