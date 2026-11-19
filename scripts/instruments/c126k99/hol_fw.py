import sys,csv
def traj(p, name):
    f=open(p,encoding='utf-8'); r=csv.reader(f,delimiter='\t'); h=next(r)
    ir=h.index('region'); ip=[i for i,c in enumerate(h) if c in('popK','populationK','totalPopK')][0]
    iy=[i for i,c in enumerate(h) if c in('year','label','yearLabel')][0]
    io=[i for i,c in enumerate(h) if c in('owner','nation')]
    t=[(row[iy],float(row[ip]),row[io[0]] if io else '') for row in r if row[ir]==name]
    return t
for p in sys.argv[2:]:
    t=traj(p,sys.argv[1]); pk=max(range(len(t)),key=lambda i:t[i][1]); mn=min(t[pk:],key=lambda x:x[1])
    print(p, 'rows',len(t),'peak',t[pk],'min after',mn,'ratio',round(mn[1]/t[pk][1],3),'last',t[-1])
