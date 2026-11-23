import sys,os
INFRA={"水道局","ガス灯","電灯網"}
HOME={"イギリス":{"ロンドン","マンチェスター","バーミンガム","リヴァプール","グラスゴー","エディンバラ","ダブリン","ニューカッスル","リーズ","ブリストル","カーディフ","ベルファスト"}}
for lab,raw in (("腕",sys.argv[1]),("基",sys.argv[2])):
    for s in range(1,11):
        p=f"{raw}/prodcap_s{s}.tsv"
        f=open(p,encoding="utf-8"); h=f.readline().rstrip("\n").split("\t"); ix={k:i for i,k in enumerate(h)}
        qs=set(); res={}
        for l in f:
            c=l.rstrip("\n").split("\t")
            if c[ix["year"]]!="1913": continue
            qs.add(c[ix["q"]])
            n=c[ix["ownerNation"]]
            if n not in ("イギリス","フランス"): continue
            d=res.setdefault(n,{"reg":set(),"I":0.0,"Iscale":0.0,"Ireg":{},"nI":0,"F":0.0})
            d["reg"].add(c[ix["region"]])
            if c[ix["recipe"]] in INFRA:
                cap=float(c[ix["capacity"]]); d["I"]+=cap; d["nI"]+=1; d["Iscale"]+=float(c[ix["scale"]])
                d["Ireg"][c[ix["region"]]]=d["Ireg"].get(c[ix["region"]],0)+cap
        out=[]
        for n in ("イギリス","フランス"):
            d=res.get(n)
            if not d: out.append(f"{n} なし"); continue
            top=sorted(d["Ireg"].items(),key=lambda x:-x[1])[:3]
            out.append(f"{n} 州{len(d['reg'])} I{d['I']:.1f} 行{d['nI']} 規模和{d['Iscale']:.1f} 上位{[(r,round(v,1)) for r,v in top]}")
        print(lab,s,"q",sorted(qs)," | ".join(out))
