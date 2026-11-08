import sys, os, csv, collections, statistics
SB=int(sys.argv[1]); dirs=sys.argv[2:]
N=["イギリス","フランス","ロシア","清","プロイセン","日本","アメリカ"]
out=[]
for d in dirs:
    tot=collections.Counter()
    for s in range(SB,SB+10):
        p=os.path.join(d,f"nation_s{s}.tsv")
        if not os.path.exists(p): continue
        last={}
        with open(p,encoding="utf-8") as f:
            for r in csv.DictReader(f,delimiter="\t"):
                try: last[r["nation"]]=float(r["ledgerArmyCumulative"])
                except: pass
        for n in N: tot[n]+=last.get(n,0)
    out.append(tot)
b=out[0]
for i,n in enumerate(N):
    print(i, " ".join(f"{o[n]/b[n]:.3f}" if b[n] else "-" for o in out[1:]))
