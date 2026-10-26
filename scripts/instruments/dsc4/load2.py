import csv, collections, math, sys
def num(x):
    try: return float(x)
    except: return float('nan')
def load2(path):
    N={}; R={}; P=collections.defaultdict(dict); Q=[]
    with open(path,encoding='utf-8') as fh:
        rd=csv.reader(fh,delimiter='\t'); hdr=next(rd)
        for r in rd:
            if len(r)<21: continue
            k=r[0]; q=int(r[1]); y=int(r[2]); nat=r[3]; row=r[4]; dec=r[5]; c=[num(x) for x in r[6:]]
            if k=='N': N[(q,nat)]=dict(year=y,lit=c[0],popCore=c[1],popAll=c[2],engCore=c[3],engAll=c[4],imp=c[5],techLv=c[6],spec=c[7],hedu=c[8],gdp=c[9])
            elif k=='R': R[(q,nat,row)]=dict(year=y,decade=int(dec),known=int(c[0]),open=int(c[1]),tech=c[2],need=c[3],trans=c[4],v=c[5],cost=c[6],out=c[7],xT=c[8],xR=c[9],xN=c[10],xA=c[11],A=c[12],RU=c[13],dem=c[14])
            elif k=='P': P[(q,nat)][row]=c[0]
            elif k=='Q': Q.append((q,y,nat,row,c[0],c[1]))
    return N,R,P,Q
