import csv,sys,os,math
d=sys.argv[1]
def f(s):
    try: return float(s)
    except: return math.nan
from collections import Counter
cnt=Counter(); vals=[]
for k in range(1,11):
    p=os.path.join(d,f"crownpc_s{k}.tsv")
    with open(p,encoding="utf-8",newline="") as fh:
        r=csv.reader(fh,delimiter="\t"); h=next(r); ix={x:i for i,x in enumerate(h)}
        for l in r:
            if l[ix["firstRowFlag"]]=="1" or l[ix["politicsEnabledFlag"]]!="1": continue
            rev=f(l[ix["revenuePerMonth"]]); it=f(l[ix["interestPerMonth"]]); x=0 if not rev>0 else it/rev
            if x<0.67: continue
            cnt["hi"]+=1
            ev=l[ix["regimeEventFlag"]]=="1"
            if ev: cnt["ev"]+=1; continue
            pcp=f(l[ix["pcPrevShare"]]); pcn=f(l[ix["pcNowShare"]])
            oth=sum(f(l[ix[c]]) for c in ("rollbackTermShare","term1Share","term2Share","term2pShare"))
            sm=f(l[ix["summonTermShare"]]); res=f(l[ix["residualShare"]])
            act=pcn-pcp-oth
            m1=act-sm   # engine without summon (paper M1)
            cnt["ex"+l[ix["explainCode"]]]+=1
            for tol in (1e-12,1e-9,1e-6):
                if act>tol: cnt[f"act>{tol}"]+=1
                if m1>tol: cnt[f"m1>{tol}"]+=1
            if abs(m1-res)>1e-9: cnt["m1!=res"]+=1
for k,v in sorted(cnt.items()): print(k,v)
