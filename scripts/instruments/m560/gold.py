import csv,glob,os,sys,statistics as st
def rows(p):
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.reader(f,delimiter='\t');h=next(r)
        for x in r:
            if len(x)==len(h): yield dict(zip(h,x))
def fl(x):
    try: return float(x)
    except: return float('nan')
for d in sys.argv[1:]:
    on=0;low=0;exits=0;pr=[];infra=[];sb=[];tot=0;cons=[]
    for p in sorted(glob.glob(os.path.join(d,'fiscal_s*.tsv'))):
        prev={}
        for x in rows(p):
            n=x['nation'];g=x['onGold']=='1' or x['onGold']=='True'
            if prev.get(n) and not g: exits+=1
            prev[n]=g
            if g:
                on+=1;r=fl(x['payRateShare']);pr.append(r)
                if r<0.7: low+=1
            ct=fl(x['claimTotalPerMonth'])
            if ct>0:
                infra.append(fl(x['claimInfraPerMonth'])/ct); sb.append((fl(x['claimStateBuildPerMonth'])+fl(x['claimStateConstructionPerMonth']))/ct)
    print(d); print(f'  onGold月 {on}・その内 payRate<0.7 {low}・1→0 の遷移 {exits}・payRate中央値 {st.median(pr) if pr else float("nan"):.3f}・infra/claim 中央値 {st.median(infra):.4f}・state build/claim 中央値 {st.median(sb):.4f}')
