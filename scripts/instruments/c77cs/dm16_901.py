import csv,os,sys,math
d=sys.argv[1]; n=0; r=[]
for k in range(1,11):
    p=os.path.join(d,f"crownpc_s{k}.tsv")
    with open(p,encoding="utf-8",newline="") as f:
        rd=csv.reader(f,delimiter="\t"); h=next(rd); ix={x:i for i,x in enumerate(h)}
        for l in rd:
            dm=l[ix["driftMaskCode"]]
            if dm.isdigit() and int(dm)&16: n+=1; r.append(abs(float(l[ix["residualShare"]])))
r.sort(); print(f"driftMaskCode&16 の行 {n}・|残差| 中央 {r[len(r)//2]:.3g}・上位10% {r[int(len(r)*0.9)]:.3g}・最大 {r[-1]:.3g}" if r else "0")
