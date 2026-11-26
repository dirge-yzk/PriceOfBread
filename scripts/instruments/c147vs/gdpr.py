import sys,glob,os
sys.stdout.reconfigure(encoding='utf-8')  # (1384) 記録の読み：gdpr＝名目 GDP の 1 四半期の比 10 倍以上（建国の 4 四半期と人口の比 10 倍の痩せを除く）・gpc＝1 人あたり名目 GDP＜0.1 の国×四半期
def scan(d):
    c10=0; top=[]
    for p in sorted(glob.glob(os.path.join(d,'nation_s*.tsv'))):
        s=os.path.basename(p)[8:-4]
        prev={}; first={}
        with open(p,encoding='utf-8') as f:
            h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
            for line in f:
                a=line.rstrip('\n').split('\t'); n=a[ix['nation']]; q=int(a[ix['quarter']])
                try: g=float(a[ix['nominalGdpPerYear']]); pop=float(a[ix['populationK']])
                except: continue
                first.setdefault(n,q)
                pv=prev.get(n); prev[n]=(q,g,pop)
                if pv is None or pv[0]!=q-1 or q-first[n]<=4 or pv[1]<=0 or g<=0: continue
                if pop<=0 or pv[2]<=0 or pop/pv[2]>=10 or pop/pv[2]<=0.1: continue
                r=max(g/pv[1],pv[1]/g)
                if r>=10: c10+=1; top.append((round(r,1),s,n,a[ix['year']],round(pop,1)))
    top.sort(reverse=True)
    return c10, top[:6]
for d in sys.argv[1:]:
    c,t=scan(d); print(d.split('/')[-2]+'/'+d.split('/')[-1], '10倍以上',c, t)
