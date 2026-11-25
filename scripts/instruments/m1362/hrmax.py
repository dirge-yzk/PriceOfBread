# (1362) 合流の記録の読み手（読むだけ・門にしない）：時短（ShorterHours）の段の後退（前の四半期の値より大きい）の回数を（種・国）ごとに数え、最大と上位 5 組を出す。
#   線 lr の反証役 v2 の指摘＝1 国の往復の大きさを基準・ヌル腕・a11 と同じ台本で並べる（数え方は c142lr/rec_lr.py の ① と同じ）。種の番号はファイル名から（★(1348)）。
#   使い方：python hrmax.py <ins> [<ins> ...]
import sys, os, glob, re
def seed(p): return int(re.search(r'_s(\d+)\.tsv$', os.path.basename(p)).group(1))
for ins in sys.argv[1:]:
    res = []
    for p in sorted(glob.glob(os.path.join(ins, 'monlaw_s*.tsv')), key=seed):
        prev = {}; cnt = {}; name = {}
        with open(p, 'rb') as fh:
            h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in fh:
                c = line.rstrip(b'\r\n').decode('utf-8').split('\t')
                u = c[ix['nationUid']]; v = int(float(c[ix['ShorterHours']])); name[u] = c[ix['nation']]
                if u in prev and v > prev[u]: cnt[u] = cnt.get(u, 0) + 1
                prev[u] = v
        res += [(k, seed(p), name[u]) for u, k in cnt.items()]
    res.sort(reverse=True)
    top = ' / '.join(f's{s} {n} {k}' for k, s, n in res[:5])
    print(f'{ins}\t種 {len({s for _, s, _ in res})}\t最大 {res[0][0] if res else 0}\t5 回以上の組 {sum(1 for r in res if r[0] >= 5)}\t上位 {top}')
