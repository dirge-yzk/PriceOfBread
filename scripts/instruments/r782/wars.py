# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
import sys, csv, os, statistics as st, collections
d=sys.argv[1]; s0=int(sys.argv[2]); nat='イギリス'
def rows(p):
    with open(p,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r)
        for x in r:
            if len(x)<len(h): x+=['']*(len(h)-len(x))
            yield dict(zip(h,x))
def F(x):
    try: return float(x)
    except: return 0.0
tot=collections.Counter(); out=[]
for s in range(s0,s0+10):
    W=[x for x in rows(os.path.join(d,f'wars_s{s}.tsv')) if x['civilWarFlag']=='0' and 1784<=int(x['startYear'])<=1815 and nat in (x['sideA'],x['sideB'])]
    att=sum(1 for x in W if x['sideA']==nat); dfd=len(W)-att
    wm=sum(F(x['months']) for x in W)
    lost=sum(1 for x in rows(os.path.join(d,f'cessp_s{s}.tsv')) if x['loser']==nat and 1784<=int(x['year'])<=1815)
    won=sum(1 for x in rows(os.path.join(d,f'cessp_s{s}.tsv')) if x['winner']==nat and 1784<=int(x['year'])<=1815)
    nr=[x for x in rows(os.path.join(d,f'nation_s{s}.tsv')) if x['nation']==nat and 1784<=int(x['year'])<=1815]
    ex=st.mean(F(x['exportsPerClearing']) for x in nr); im=st.mean(F(x['importsPerClearing']) for x in nr)
    atw=st.mean(F(x['atWarMonths']) for x in nr)
    out.append((s,len(W),att,dfd,wm,won,lost,ex,im,atw))
print('seed wars UKattacks UKattacked warMonthsSum peaceWon peaceLost exportsMean importsMean atWarMonthsPerQ')
for o in out: print('%d %d %d %d %.0f %d %d %.0f %.0f %.2f'%o)
c=list(zip(*out))
print('median', *[f'{st.median(v):.2f}' for v in c[1:]])
