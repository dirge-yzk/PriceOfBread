# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
import sys, csv, os
d=sys.argv[1]; s=int(sys.argv[2]); m0=int(sys.argv[3]); m1=int(sys.argv[4]); nat='イギリス'
with open(os.path.join(d,f'fiscal_s{s}.tsv'),encoding='utf-8') as f:
    r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
    for x in r:
        if x[ix['nation']]!=nat: continue
        m=int(x[0])
        if m0<=m<=m1:
            print(m, ' '.join(f'{k[6:] if k.startswith("ledger") else k}={float(x[ix[k]]):.4g}' for k in h if (k.startswith('ledger') or k.startswith('treasury')) and x[ix[k]] not in ('','0') and abs(float(x[ix[k]]))>1))
