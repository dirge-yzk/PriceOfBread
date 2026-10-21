import csv,sys,statistics as st
sys.stdout.reconfigure(encoding="utf-8")
def p99(D,NS=10):
    out=[]
    for s in range(1,NS+1):
        v=[]
        with open(f"{D}/region_s{s}.tsv",encoding="utf-8") as f:
            for r in csv.DictReader(f,delimiter="\t"):
                a=float(r["arableLand"]); p=float(r["populationK"])
                if a>0 and p>=1: v.append(p/a)
        v.sort(); out.append(v[int(0.99*len(v))])
    return out
a=p99("D:/PoB_work/keep_484/m484_ins"); b=p99("D:/PoB_work/dx_488/out_dxB_10/ins")
print("m484 p99 per seed",[round(x,2) for x in a],"median",round(st.median(a),2),"max",round(max(a),2))
print("B    p99 per seed",[round(x,2) for x in b],"median",round(st.median(b),2),"max",round(max(b),2))
print("ratio max/max",round(max(b)/max(a),2),"median/median",round(st.median(b)/st.median(a),2),"max B / median m484",round(max(b)/st.median(a),2))
