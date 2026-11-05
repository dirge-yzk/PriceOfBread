# (849) 腕 B の門 G3b・G4 の落ちの読み（読むだけ・門は替えない）
import os, collections
A="D:/PoB_work/c69ss_work/armB/out_c69ssB_10/ins"; B="D:/PoB_work/keep_845/base_ins"; N=["D:/PoB_work/keep_839/n1","D:/PoB_work/keep_839/n2"]
def f(x):
    try: return float(x)
    except ValueError: return float("nan")
def med(xs):
    xs=sorted(v for v in xs if v==v); return xs[len(xs)//2] if xs else float("nan")
def q0(path):
    D={}
    with open(path,encoding="utf-8") as fh:
        h=fh.readline().rstrip("\n").split("\t"); ix={k:i for i,k in enumerate(h)}
        for line in fh:
            c=line.rstrip("\n").split("\t")
            if c[ix["quarter"]]!="0": continue
            sp=f(c[ix["subPopK"]]); need=f(c[ix["stapleNeedPerCapita"]]); eng=f(c[ix["engineSubOutputPerMonth"]])
            if sp>0: D[c[ix["nation"]]+"/"+c[ix["region"]]]=eng/(need*sp)
    return D
W=[];Dd=[]
mis=[]; cnt=collections.Counter()
for s in range(1,11):
    a=q0(os.path.join(A,"subst_s%d.tsv"%s)); bs=[q0(os.path.join(x,"subst_s%d.tsv"%s)) for x in [B]+N]
    for k,v in a.items():
        if all(k in b for b in bs):
            vals=[b[k] for b in bs]; W.append(max(vals)-min(vals)); Dd.append(abs(v-vals[0]))
    with open(os.path.join(A,"subst_s%d.tsv"%s),encoding="utf-8") as fh:
        h=fh.readline().rstrip("\n").split("\t"); ix={k:i for i,k in enumerate(h)}
        for line in fh:
            c=line.rstrip("\n").split("\t")
            if c[ix["s2BasisKind"]]!="2" or c[ix["s2RhoSource"]]!="1": continue
            e=f(c[ix["s2EEnd"]]); ue=f(c[ix["s2UnempEngine"]]); ub=f(c[ix["s2UnempB"]])
            if e==e and e>0 and ue==ue:
                if abs(ue-ub)>1e-9:
                    cnt["mis"]+=1
                    if len(mis)<8: mis.append((s,c[ix["quarter"]],c[ix["nation"]],c[ix["region"]],ue,ub,e,f(c[ix["s2CapEnd"]]),f(c[ix["s2HeadsEnd"]]),f(c[ix["s2SigmaEngine"]])))
                    cnt["ue0" if ue==0 else "ue>0"]+=1
print("G3b の q0：行 %d・|d| 中央 %.5f・w 中央 %.6f・w＝0 の行 %d"%(len(W),med(Dd),med(W),sum(1 for x in W if x==0)))
print("G4 の E＞0 で食い違う行 %d（エンジンの失業 0 の行 %d・正 %d）"%(cnt["mis"],cnt["ue0"],cnt["ue>0"]))
for m in mis: print("  ",m)
