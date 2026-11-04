import sys, glob, os, statistics
d = sys.argv[1]
res = []; rel = []
for p in sorted(glob.glob(os.path.join(d, 'cpworld_s*.tsv'))):
    with open(p,'rb') as f:
        h = f.readline().decode().rstrip('\r\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        for line in f:
            r = line.decode().rstrip('\r\n').split('\t')
            hd = float(r[ix['hostFlightDropGold']]) + float(r[ix['hostWoDropGold']])
            od = float(r[ix['ownerDropGold']])
            if hd > 0 and int(r[ix['clipNations']]) == 0:
                res.append(abs(od - hd)); rel.append(abs(od-hd)/max(1e-300, float(r[ix['abroadSumGold']])))
res.sort(); rel.sort()
print('cpworld calls (host drop>0, clip 0)', len(res), 'median |owner-host|', repr(res[len(res)//2]), 'p90', repr(res[int(len(res)*0.9)]), 'max', repr(res[-1]), 'min', repr(res[0]))
print('  rel to abroadSum median', repr(rel[len(rel)//2]))
# 0 < claim-paid <= 1e-9 の行（門を >0 にしたときの新しい事象）
n = 0; small_fci = 0; ex = []
for p in sorted(glob.glob(os.path.join(d, 'cpwd_s*.tsv'))):
    with open(p,'rb') as f:
        h = f.readline().decode().rstrip('\r\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        for line in f:
            r = line.decode().rstrip('\r\n').split('\t')
            c = float(r[ix['claimIndex']]); pd = float(r[ix['paidIndex']]); f1 = float(r[ix['fciAtCoupon']])
            u = c - pd
            if 0 < u <= 1e-9 and min(u, f1) > 0:
                n += 1
                if f1 < 1e7: small_fci += 1
                if len(ex) < 5: ex.append((os.path.basename(p), r[ix['quarter']], r[ix['nation']], c, pd, u, f1))
print('rows 0<claim-paid<=1e-9', n, 'with fci<1e7', small_fci)
for x in ex: print(' ', *x)
