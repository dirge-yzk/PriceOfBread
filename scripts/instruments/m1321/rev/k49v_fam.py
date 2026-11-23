import sys, os, itertools, random, statistics as st
INFRA={"水道局","ガス灯","電灯網"}
NAT=["イギリス","フランス","プロイセン","アメリカ","ロシア","オーストリア","清","世界"]
def seedvals(raw,s):
    acc={}
    with open(f"{raw}/prodcap_s{s}.tsv",encoding="utf-8") as f:
        h=f.readline().rstrip("\n").split("\t"); ix={k:i for i,k in enumerate(h)}
        for l in f:
            y=l[:4]
            if y not in ("1850","1913"): continue
            c=l.rstrip("\n").split("\t")
            if c[ix["recipe"]] not in INFRA: continue
            n=c[ix["ownerNation"]]
            if n=="ドイツ帝国": n="プロイセン"
            v=float(c[ix["capacity"]])
            for k in (n,"世界"): acc[(k,y)]=acc.get((k,y),0)+v
    return [acc.get((k,y),0.0) for k in NAT for y in ("1850","1913")]
A=[seedvals(sys.argv[1],s) for s in range(1,11)]
B=[seedvals(sys.argv[2],s) for s in range(1,11)]
rows=[(k,y) for k in NAT for y in ("1850","1913")]
def reds(G1,G2):
    out=[]
    for j,_ in enumerate(rows):
        a=st.median([g[j] for g in G1]); b=st.median([g[j] for g in G2])
        if b>=1 and not (0.1<a/b<10): out.append(j)
    return out
print("観測", [rows[j] for j in reds(A,B)])
allv=A+B; n=0; anyred=0; per=[0]*len(rows)
for idx in itertools.combinations(range(20),10):
    s=set(idx); r=reds([allv[i] for i in idx],[allv[i] for i in range(20) if i not in s]); n+=1
    if r: anyred+=1
    for j in r: per[j]+=1
print("並べ替え 腕と基準を交換可能とみたときの赤が1つ以上の率",anyred/n)
for j,c in enumerate(per):
    if c: print(rows[j],c/n)
# 基準の中だけ 5 対 5
n=0;anyb=0
for idx in itertools.combinations(range(10),5):
    s=set(idx); r=reds([B[i] for i in idx],[B[i] for i in range(10) if i not in s]); n+=1
    if r: anyb+=1
print("基準の中の 5 対 5 で赤が1つ以上の率",anyb/n)
