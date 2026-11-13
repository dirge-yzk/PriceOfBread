# (1097) c105rr G0 の裏づけ：種ごとに、腕と基準の生で最初に違う行の四半期と、基準の disc_s の機関車の最初の発見（E 行）の四半期を並べる。
# 年の列で比べる（quarter の列は表ごとに意味が違う＝建国の国は自分の四半期を数える）。最初に違う年 ≧ 発見の年 なら、発見の前は全行一致（G0 の切り口の直しが行を隠していない）。既知の揺れの列は外し、flow_s は四半期ごとの多重集合で比べる。
# 使い方: python -X utf8 -I firstdiff_rr.py <腕の ins> <基準の ins>
import sys, os, collections
sys.stdout.reconfigure(newline='\n')
A, B = sys.argv[1], sys.argv[2]
KNOWN = {'flow': ('sourceFile', 'sourceLine'), 'burscan': ('seconds',), 'clock': ('worldTotalPerMonth',), 'dep': ('callerLine',), 'cearn': ('outTop1Route', 'outTop2Route', 'inTop1Route'), 'cpop': ('outOtherTop1Route',)}
bad = 0
for s in range(1, 11):
    qd = None; yd = None
    with open(os.path.join(B, f'disc_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if c[ix['kind']] == 'E' and c[ix['row']] == '機関車':
                q = int(c[ix['quarter']])
                if qd is None or q < qd: qd = q; yd = int(c[ix['year']])
    first = None; nt = 0
    for fn in sorted(os.listdir(B)):
        if not fn.endswith(f'_s{s}.tsv'): continue
        stem = fn[:-len(f'_s{s}.tsv')]
        if stem in ('trnation', 'trrail'): continue
        pa = os.path.join(A, fn)
        if not os.path.exists(pa): continue
        with open(pa, encoding='utf-8', errors='replace') as fa, open(os.path.join(B, fn), encoding='utf-8', errors='replace') as fb:
            h = fb.readline().rstrip('\n').split('\t'); fa.readline()
            if 'year' not in h: continue
            qi = h.index('year'); drop = set(h.index(x) for x in KNOWN.get(stem, ()) if x in h)
            nt += 1
            def rows(f):
                out = collections.defaultdict(list)
                for ln in f:
                    c = ln.rstrip('\n').split('\t')
                    try: q = int(float(c[qi]))
                    except (ValueError, IndexError): continue
                    if qd is not None and q > yd + 1: break
                    out[q].append('\t'.join(v for i2, v in enumerate(c) if i2 not in drop))
                return out
            ra, rb = rows(fa), rows(fb)
            for q in sorted(set(ra) | set(rb)):
                x, y = ra.get(q, []), rb.get(q, [])
                if stem == 'flow': x, y = sorted(x), sorted(y)
                if x != y:
                    if first is None or q < first[0]: first = (q, stem)
                    break
    ok = first is None or yd is None or first[0] >= yd
    if not ok: bad += 1
    print(f's{s}\t機関車の最初の発見の四半期 {qd}（年 {yd}）\t最初に違う年 {first[0] if first else "なし（発見の後 4 四半期まで）"}\t表 {first[1] if first else ""}\t比べた表 {nt}\t{"OK" if ok else "発見の前に違う＝G0 赤"}')
print(f'発見の前に違う種 {bad}/10')
