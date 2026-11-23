import sys,statistics as st
INFRA={"水道局","ガス灯","電灯網"}
for lab,raw in (("腕",sys.argv[1]),("基",sys.argv[2])):
    agg={}
    for s in range(1,11):
        f=open(f"{raw}/prodcap_s{s}.tsv",encoding="utf-8"); h=f.readline().rstrip("\n").split("\t"); ix={k:i for i,k in enumerate(h)}
        y0=None
        for l in f:
            c=l.rstrip("\n").split("\t"); y=c[ix["year"]]
            if y0 is None: y0=y
            if y not in ("1800","1850","1880","1913"): continue
            if c[ix["recipe"]] not in INFRA: continue
            d=agg.setdefault((y,s),[0,0.0,0.0,0,0.0])
            sc=float(c[ix["scale"]]); cap=float(c[ix["capacity"]])
            d[0]+=1; d[1]+=sc; d[2]+=cap; d[3]+= cap>0.1; d[4]+=float(c[ix["lastInputFill"]])
    for y in ("1800","1850","1880","1913"):
        rows=[agg.get((y,s)) for s in range(1,11) if agg.get((y,s))]
        ms=[r[1]/r[0] for r in rows]; cp=[r[2] for r in rows]; act=[r[3]/r[0] for r in rows]; fill=[r[4]/r[0] for r in rows]
        print(lab,y,"行",st.median([r[0] for r in rows]),"規模/行 中央",round(st.median(ms),4),"範囲",round(min(ms),3),round(max(ms),3),"能力和 中央",round(st.median(cp),1),"能力>0.1の割合",round(st.median(act),3),"投入充足",round(st.median(fill),3))
