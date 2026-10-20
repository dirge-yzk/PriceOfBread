import csv,sys,collections
def rd(p,q0):
    S=collections.defaultdict(lambda: collections.defaultdict(float))
    for r in csv.DictReader(open(p,encoding="utf-8"),delimiter="\t"):
        q=int(r["quarter"]); 
        if q<q0: continue
        n=r["nation"]
        for k in ("needInvestor","lossSumInvestor","lossSumInvestorSel","producersCut","needSum","lossSum"):
            try: S[n][k]+=float(r[k])
            except: pass
    return S
q0=int(sys.argv[3])
A=rd(sys.argv[1],q0); B=rd(sys.argv[2],q0)
print("国\tF0 needInv\tF1 needInv\tF0 lossInv\tF1 lossInv\tF0 sel\tF1 sel\tF0 非sel\tF1 非sel\tF1 閉じた数")
tot=collections.defaultdict(float)
for n in sorted(set(A)|set(B)):
    a=A[n]; b=B[n]
    row=[a["needInvestor"],b["needInvestor"],a["lossSumInvestor"],b["lossSumInvestor"],a["lossSumInvestorSel"],b["lossSumInvestorSel"],a["lossSumInvestor"]-a["lossSumInvestorSel"],b["lossSumInvestor"]-b["lossSumInvestorSel"],b["producersCut"]]
    for i,v in enumerate(row): tot[i]+=v
    print(n+"\t"+"\t".join(f"{v:.3g}" for v in row))
print("計\t"+"\t".join(f"{tot[i]:.3g}" for i in range(9)))
