# (1149) 機関車を知る国の数（railspend_s の discEnd・四半期 160/268/400/667）＝c110lo/disc_count.py の known() と同じ数え（写しでなく同じ規則を書き直した）。
#   python -X utf8 -I known_count.py <ins> <種の始め> <種の終わり>
import sys, os, statistics
ins, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
QS = (160, 268, 400, 667)
rows = []; firstq = []
for s in range(s0, s1 + 1):
    p = os.path.join(ins, f'railspend_s{s}.tsv')
    if not os.path.exists(p): print(f'種 {s}：railspend_s が無い'); continue
    last = {}; fq = None
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            q = int(c[ix['q']]); d = c[ix['discEnd']]
            last.setdefault(q, []).append(d)
            if d == '1' and fq is None: fq = q
    res = []
    for qq in QS:
        q = max(k for k in last if k <= qq); res.append(sum(1 for x in last[q] if x == '1'))
    rows.append(res); firstq.append(fq)
print(f'== {ins} 種 {s0}〜{s1}')
print('種ごと（q160/268/400/667）：' + ' '.join('/'.join(map(str, r)) for r in rows))
print('中央：' + '・'.join(f'q{q} {statistics.median(r[i] for r in rows):g}' for i, q in enumerate(QS)))
print('世界で最初に知る国が出た四半期（種ごと）：' + ' '.join(str(x) for x in firstq))
