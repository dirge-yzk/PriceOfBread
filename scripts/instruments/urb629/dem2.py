import sys
f=sys.argv[1]
rows=[]
for line in open(f,encoding='utf-8'):
    c=line.rstrip('\n').split('\t')
    sz=float(c[1]);rk=float(c[2]);mbk=float(c[3]);mp=float(c[4]);dws=float(c[5]);mdw=float(c[6]);tb=float(c[7]);dr=float(c[8]);rec=c[9];net=float(c[10])
    bs=rk/mbk if mbk>0 else 0
    births=(net*bs if rec=='True' else tb*bs); deaths=0 if rec=='True' else dr*dws
    rows.append((sz,births,deaths))
# per-decade check: size change vs sum(b-d)
for a in range(0,len(rows)-120,240):
    b=a+240
    ds=rows[b][0]-rows[a][0]
    nb=sum(r[1] for r in rows[a:b]); nd=sum(r[2] for r in rows[a:b])
    print('yrs %d-%d size %.0f->%.0f dSize %+.0f births %.0f deaths %.0f b-d %+.0f unexplained %+.0f'%(1783+a//12,1783+b//12,rows[a][0],rows[b][0],ds,nb,nd,nb-nd,ds-(nb-nd)))
