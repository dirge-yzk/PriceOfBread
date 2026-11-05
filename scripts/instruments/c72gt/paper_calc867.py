import sys, os, collections
R="D:/PoB_work/keep_852"; DIRS={"M":R+"/base_ins","N1":R+"/n1","N2":R+"/n2"}
nan=float("nan")
def fn(s):
    try: return float(s)
    except: return nan
def win(y): return "1783-85" if y<=1785 else "1786-99" if y<=1799 else "1800-49" if y<=1849 else "1850-99" if y<=1899 else "1900-50"
def med(xs):
    xs=sorted(x for x in xs if x==x); n=len(xs)
    return nan if n==0 else xs[n//2] if n%2 else 0.5*(xs[n//2-1]+xs[n//2])
def load(d,seed):
    D={}
    with open("%s/subst_s%d.tsv"%(d,seed),encoding="utf-8") as f:
        h=f.readline().rstrip("\r\n").split("\t"); ix={k:i for i,k in enumerate(h)}
        for l in f:
            c=l.rstrip("\r\n").split("\t"); sp=fn(c[ix["subPopK"]])
            if not sp>0: continue
            nom=fn(c[ix["nominalSubPerMonth"]]); eng=fn(c[ix["engineSubOutputPerMonth"]])
            H=eng/nom if nom>0 else nan; d0=fn(c[ix["s2D0Inst"]]); sg=fn(c[ix["s2SigmaInst"]])
            x=H*(sg-d0) if int(c[ix["s2RhoSource"]])==1 and d0>0 else nan
            cap=fn(c[ix["farmCapPerMonth"]]); sold=fn(c[ix["farmSoldPerMonth"]])
            D[(int(c[ix["quarter"]]),c[ix["nation"]]+"/"+c[ix["region"]])]=(int(c[ix["year"]]),x,cap,sold,fn(c[ix["unsoldD1Share"]]),H*d0)
    return D
acc=collections.defaultdict(list); tot=collections.Counter()
for seed in range(1,11):
    L={k:load(v,seed) for k,v in DIRS.items()}
    M=L["M"]; o0={k[1]:r for k,r in M.items() if k[0]==0}
    for key,r in M.items():
        if key[0]==0: continue
        o=o0.get(key[1])
        if o is None or not(r[2]>0 and o[2]>0) or abs(r[3]/r[2]-o[3]/o[2])>0.01: continue
        xs=[L[k].get(key,(0,nan))[1] for k in L]
        if any(x!=x for x in xs): continue
        w=win(r[0]); acc[(w,"x")].append(xs[0]); acc[(w,"wp")].append(max(xs)-min(xs)); acc[(w,"hd0")].append(r[5])
for w in ("1783-85","1786-99","1800-49","1850-99","1900-50"):
    print(w,len(acc[(w,"x")]),"med x_M %.5f"%med(acc[(w,"x")]),"med wp %.5f"%med(acc[(w,"wp")]),"med H*d0 %.4f"%med(acc[(w,"hd0")]))
