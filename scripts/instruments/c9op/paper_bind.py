import sys, glob, math
def load(d):
    rows=[]
    for f in sorted(glob.glob(d+'/habitcap_s*.tsv')):
        with open(f,encoding='utf-8') as fh:
            h=fh.readline().rstrip('\n').split('\t')
            for l in fh:
                p=l.rstrip('\n').split('\t')
                if len(p)<15: continue
                rows.append((f,)+tuple(p))
    return rows
for d in sys.argv[1:]:
    rows=load(d)
    months=sum(int(r[6]) for r in rows); bind=sum(int(r[7]) for r in rows)
    exc=sum(float(r[14]) for r in rows)
    nm=sorted(float(r[15]) for r in rows)
    bef=sorted(float(r[10]) for r in rows)
    rowsbind=sum(1 for r in rows if int(r[7])>0)
    print(d,'rows',len(rows),'months',months,'bindMonths',bind,'%.4f%%'%(100*bind/months),'rowsWithBind',rowsbind,'%.4f%%'%(100*rowsbind/len(rows)))
    print(' sum excess %.4f'%exc, 'newMax max %.6f p99 %.6f p999 %.6f'%(nm[-1],nm[int(len(nm)*0.99)],nm[int(len(nm)*0.999)]))
    print(' before max %.6f p99 %.6f'%(bef[-1],bef[int(len(bef)*0.99)]))
    # excess per binding row distribution
    ex=sorted(float(r[14]) for r in rows if float(r[14])>0)
    if ex: print(' rows excess>0',len(ex),'max %.6f median %.6f'%(ex[-1],ex[len(ex)//2]))
    # nations with bind
    from collections import Counter
    c=Counter(r[4] for r in rows if int(r[7])>0)
    print(' bind rows by nation',c.most_common(8))
