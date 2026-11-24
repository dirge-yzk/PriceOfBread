# (1340) 問い 0：開幕の最初の四半期の PublicOrder 0 と窃盗の跳ねを生から数える（読むだけ・コミットしない）
# 使い方: python q0_order.py <生のフォルダ> [<生のフォルダ> ...]  → 標準出力に TSV
# 列は名前で引く（列の位置に頼らない）。rule_s*.tsv の publicOrderIndex・secession_s*.tsv の orderOldIndex・region_s*.tsv の theftPoolPerMonth。
import sys, os, re, glob

def cols(path):
    with open(path, 'rb') as f:
        head = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        for line in f:
            yield head, line.decode('utf-8').rstrip('\r\n').split('\t')

def count(folder):
    out = []
    for p in sorted(glob.glob(os.path.join(folder, 'rule_s*.tsv'))):
        seed = re.search(r'_s(\d+)\.tsv$', p).group(1)
        nat = {}   # (q, nation) -> PO
        for h, r in cols(p):
            q = int(r[h.index('quarter')])
            if q > 4: continue
            nat[(q, r[h.index('nation')])] = float(r[h.index('publicOrderIndex')])
        sec = {}
        sp = os.path.join(folder, f'secession_s{seed}.tsv')
        if os.path.exists(sp):
            for h, r in cols(sp):
                q = int(r[h.index('quarter')])
                if q > 4: continue
                a = sec.setdefault(q, [0, 0]); a[0] += 1; a[1] += float(r[h.index('orderOldIndex')]) < 1e-9
        th = {}
        rp = os.path.join(folder, f'region_s{seed}.tsv')
        if os.path.exists(rp):
            for h, r in cols(rp):
                q = int(r[h.index('quarter')])
                if q > 4: continue
                th[q] = th.get(q, 0.0) + float(r[h.index('theftPoolPerMonth')])
        for q in range(5):
            vals = [v for (qq, _), v in nat.items() if qq == q]
            z = sum(1 for v in vals if v < 1e-9)
            s = sec.get(q, [0, 0])
            out.append((os.path.basename(folder.rstrip('/')), seed, q, len(vals), z,
                        sum(vals) / len(vals) if vals else float('nan'), s[0], s[1], th.get(q, float('nan'))))
    return out

print('arm\tseed\tquarter\tnations\tpoZero\tpoMean\tsecRegions\tsecOrderOldZero\ttheftPoolSum')
for d in sys.argv[1:]:
    for row in count(d):
        print('\t'.join(str(x) if not isinstance(x, float) else f'{x:.6g}' for x in row))
