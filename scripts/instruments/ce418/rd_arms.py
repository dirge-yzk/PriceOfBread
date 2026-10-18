import csv,glob,os,re,sys,collections,statistics as st
csv.field_size_limit(1<<30)
def fl(x):
    try: return float(x)
    except: return float('nan')
arms=dict(a.split('=') for a in sys.argv[1:])
NAT=['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本']
res={}
for lab,d in arms.items():
    pop=collections.defaultdict(list); ff=collections.defaultdict(lambda:[0,0]); low=collections.defaultdict(lambda:[0,0])
    for f in sorted(glob.glob(os.path.join(d,'nation_s[0-9]*.tsv'))):
        with open(f,encoding='utf-8',newline='') as fh:
            for r in csv.DictReader(fh,delimiter='\t'):
                if r['nation'] in NAT and int(fl(r['quarter']))%4==3 and int(fl(r['year'])) in (1850,1913,1950):
                    pop[(r['nation'],int(fl(r['year'])))].append(fl(r['populationK']))
    for f in sorted(glob.glob(os.path.join(d,'region_s[0-9]*.tsv'))):
        with open(f,encoding='utf-8',newline='') as fh:
            for r in csv.DictReader(fh,delimiter='\t'):
                n=r['nation']
                if n not in NAT: continue
                p=fl(r['populationK']); x=fl(r['foodFillShare'])
                if p==p and x==x and p>0:
                    ff[n][0]+=p*x; ff[n][1]+=p
                    low[n][1]+=1
                    if x<0.9: low[n][0]+=1
    res[lab]=(pop,ff,low)
print('人口 千人 10種の中央値（1850/1913/1950）')
for n in NAT:
    print(n,' | '.join(lab+':'+'/'.join(f"{st.median(res[lab][0][(n,y)]):.0f}" if res[lab][0].get((n,y)) else '-' for y in (1850,1913,1950)) for lab in arms))
print('主食の充足（人口加重の全期間平均）／州×四半期で<0.9の割合')
for n in NAT:
    print(n,' | '.join(f"{lab}:{res[lab][1][n][0]/max(res[lab][1][n][1],1e-9):.3f}/{res[lab][2][n][0]/max(res[lab][2][n][1],1):.3f}" for lab in arms))
