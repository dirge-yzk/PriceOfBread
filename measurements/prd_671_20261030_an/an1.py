import csv,glob,collections,math,sys,io
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8')
D='D:/PoB_work/keep_655/m655_ins/'
IC=[[0,.4,1,.25],[0,.75,1,.4],[.6,.1,.5,.2],[1,.1,.45,.2],[.45,.5,.5,1],[.75,.3,0,.2],[.3,.85,0,.2],[.7,0,.2,0],[.3,.6,.55,1],[.35,.9,.55,1],[.55,.55,.9,.5]]
PC={0:[0,.5,1,.3],1:[.05,.8,.5,.6],2:[0,.55,1,.15],3:[.6,.2,.55,.3],4:[.95,.1,.4,.25],5:[.3,.6,.5,.45],6:[.3,.85,0,.2],7:[.15,.75,.6,.85]}
SL=2.3
def aff(p,i): 
    d=math.sqrt(sum((a-b)**2 for a,b in zip(p,i)));return min(2,max(-1,2-SL*d))
def sup(agg,p): return sum(a*aff(p,IC[k]) for k,a in enumerate(agg))
names=['Trad','React','Lib','Rep','Nat','Soc','Com','Anarch','Imp','Fas','Roy']
cols=['ideoNational%sShare'%n for n in ['Traditionalism','Reactionism','Liberalism','Republicanism','Nationalism','Socialism','Communism','Anarchism','Imperialism','Fascism','Royalism']]
# polity code per nation-quarter from reform file
def polmap(s):
    m={}
    for r in csv.DictReader(open(D+'reform_s%d.tsv'%s,encoding='utf-8',errors='replace'),delimiter='\t'):
        m[(r['quarter'],r['nation'])]=int(r['polityCode'])
    return m
best=collections.Counter(); n=0
maxcom=collections.defaultdict(float); rows=[]
pdmargin=[]
for s in range(1,11):
    pm=polmap(s)
    for r in csv.DictReader(open(D+'ideology_s%d.tsv'%s,encoding='utf-8',errors='replace'),delimiter='\t'):
        agg=[float(r[c]) for c in cols]
        sp={k:sup(agg,v) for k,v in PC.items()}
        b=max(sp,key=sp.get)
        best[b]+=1; n+=1
        com=agg[6]
        key=(s,r['nation'])
        maxcom[key]=max(maxcom[key],com)
        others=max(v for k,v in sp.items() if k!=6)
        pdmargin.append((sp[6]-others,s,r['nation'],r['year'],com,pm.get((r['quarter'],r['nation']))))
print(n,'argmax counts',sorted(best.items()))
pdmargin.sort(reverse=True)
print('top PD margins',pdmargin[:10])
print('overall max comm share',max(maxcom.values()))
import statistics
# distribution of comm share
allc=[]
for s in range(1,11):
    for r in csv.DictReader(open(D+'ideology_s%d.tsv'%s,encoding='utf-8',errors='replace'),delimiter='\t'):
        allc.append(float(r['ideoNationalCommunismShare']))
allc.sort()
for q in (.5,.9,.99,.999,1): print(q,allc[int(q*(len(allc)-1))])
