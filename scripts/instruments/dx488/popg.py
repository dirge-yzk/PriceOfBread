import csv,sys,statistics as st,collections
sys.stdout.reconfigure(encoding="utf-8")
N=["イギリス","フランス","プロイセン","ロシア","日本","清"]
def g(D):
    res=collections.defaultdict(list)
    for s in range(1,11):
        first={};last={}
        with open(f"{D}/pop_s{s}.tsv",encoding="utf-8") as f:
            for r in csv.DictReader(f,delimiter="\t"):
                if r["scope"]!="N" or r["nation"] not in N: continue
                y=int(r["year"]); n=r["nation"]
                if n not in first or y<first[n][0]: first[n]=(y,float(r["popK"]))
                if n not in last or y>=last[n][0]: last[n]=(y,float(r["popK"]))
        for n in first: res[n].append(last[n][1]/first[n][1])
    return res
a=g("D:/PoB_work/keep_484/m484_ins"); b=g("D:/PoB_work/dx_488/out_dxB_10/ins")
for n in N: print(n,"m484 median",round(st.median(a[n]),2),"min",round(min(a[n]),2),"max",round(max(a[n]),2),"| B median",round(st.median(b[n]),2),"min",round(min(b[n]),2),"max",round(max(b[n]),2))
