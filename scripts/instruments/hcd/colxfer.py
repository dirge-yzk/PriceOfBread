# hcd (696)：講和で移った植民地格の州の件数と距離・勝者の艦の世界比を数える読み手（読むだけ・シミュは回さない）
# 使い方：python colxfer.py <places.json> <world.json> <出力tsv> <生のフォルダ>...   （各フォルダは region_s{n}.tsv・wars_s{n}.tsv・stock_s{n}.tsv を持つ。種の番号は自動検出）
# 距離は大圏（海路距離の下限）＝件数は少なめに出る。講和＝wars_s の内戦でない戦争の終わり（開戦年＋月数/12）が持ち主の替わった四半期の±1 年以内。
# 勝者の艦の世界比＝stock_s の navalPowerIndex（その年の最後の行）÷その年の世界最大（講和の月の値ではない）。
# 予測の欄（pred_*）＝旗 HcdColonyCessionDistance=1 の費用の式 cost×(1＋ColonyCaptureDistanceWeight×km/10000×(1−艦の世界比)) を、移った州の組（同じ講和）に当てはめて残る件数を貪欲に数えた物。
#   費用＝30＋10×資源数＋2×人口÷ColonyUnitPop（資源数は不明＝0 と置く＝費用の下限）。予算は 2 通り＝(上)100（戦勝点 1）・(下)観測された組の費用の和（その講和で必要だった最小の予算）。
import sys, json, math, os, csv, collections, glob, re, statistics
places = {p["name"]:(p["lat"],p["lon"]) for p in json.load(open(sys.argv[1], encoding="utf-8"))}
wd = json.load(open(sys.argv[2], encoding="utf-8"))
pops=[]
for n in wd["nations"]:
    for c in n.get("colonies") or []: pops.append(c["popK"])
for a in wd["abstractTargets"]:
    for c in a.get("colonies") or []: pops.append(c["popK"])
UNIT = sum(pops)/len(pops)
W = 1.0; NORM = 10000.0
def gc(a,b):
    if a is None or b is None: return float('nan')
    (la1,lo1),(la2,lo2)=a,b
    p1,p2=math.radians(la1),math.radians(la2); dl=math.radians(lo2-lo1)
    h=math.sin((p2-p1)/2)**2+math.cos(p1)*math.cos(p2)*math.sin(dl/2)**2
    return 2*6371*math.asin(math.sqrt(h))
out=open(sys.argv[3],"w",encoding="utf-8"); dirs=sys.argv[4:]
def greedy(items, budget):
    # items=[(value,cost)]  価値の高い順に買える範囲で（最大3）＝残った添字の集合を返す
    kept=set(); 
    for i,(v,c) in sorted(enumerate(items), key=lambda x:-x[1][0]):
        if len(kept)>=3: break
        if c<=budget: budget-=c; kept.add(i)
    return kept
print("dir\tseed\tmetric\tvalue", file=out)
agg=collections.defaultdict(lambda: collections.defaultdict(float))
evkm=collections.defaultdict(list)
detail=[]
WIN=collections.Counter()
for d in dirs:
    key=os.path.basename(d.rstrip("/\\"))
    seeds=sorted(int(re.search(r"region_s(\d+)\.tsv",f).group(1)) for f in glob.glob(os.path.join(d,"region_s*.tsv")))
    for s in seeds:
        byq=collections.defaultdict(dict); cap=collections.defaultdict(dict)
        with open(os.path.join(d,f"region_s{s}.tsv"),encoding="utf-8") as f:
            r=csv.reader(f,delimiter="\t"); h=next(r); ix={k:i for i,k in enumerate(h)}
            for row in r:
                q=int(row[0]); nat=row[ix["nation"]]; reg=row[ix["region"]]
                if nat not in cap[q]: cap[q][nat]=reg
                byq[q][reg]=(nat,row[ix["tier"]],float(row[ix["populationK"]]),int(row[1]))
        wars=[]
        with open(os.path.join(d,f"wars_s{s}.tsv"),encoding="utf-8") as f:
            r=csv.reader(f,delimiter="\t"); next(r)
            for row in r: wars.append((int(row[0]),row[1]=="1",row[2],row[3],int(row[4])))
        nav=collections.defaultdict(dict)
        with open(os.path.join(d,f"stock_s{s}.tsv"),encoding="utf-8") as f:
            r=csv.reader(f,delimiter="\t"); h=next(r); ix2={k:i for i,k in enumerate(h)}
            for row in r:
                try: nav[int(row[ix2["year"]])][row[ix2["nation"]]]=float(row[ix2["navalPowerIndex"]])
                except: pass
        qs=sorted(byq)
        events=collections.defaultdict(list)   # (年,旧,新) -> [(km,share,pop,value)]
        M=collections.Counter(); kms=[]
        for a,b in zip(qs,qs[1:]):
            for reg,(n0,t0,p0,y0) in byq[a].items():
                if reg not in byq[b] or t0!="Colonial": continue
                n1,t1,p1,y1=byq[b][reg]
                if n1==n0: continue
                dn=gc(places.get(cap[b].get(n1)),places.get(reg))
                peace=any({w[2],w[3]}=={n0,n1} and not w[1] and abs(w[0]+w[4]/12.0-y1)<=1.0 for w in wars)
                if not peace: M["nonpeace"]+=1; continue
                ny=nav.get(y0,{}); mx=max([v for v in ny.values() if v==v] or [0]); sh=(ny.get(n1,float('nan'))/mx) if mx>0 else float('nan')
                if sh!=sh: sh=0.0
                M["peace"]+=1
                if dn==dn: kms.append(dn)
                if dn>=3000: M["p3k"]+=1
                if dn>=3000 and sh<0.5: M["p3k_lowS"]+=1
                if dn>=8000 and sh<0.5: M["p8k_lowS"]+=1
                if dn>=8000: M["p8k"]+=1
                units=p0/UNIT
                events[(y1,n0,n1,a)].append((units+0.5, 30+2*units, dn, sh))
                if dn>=3000 and sh<0.5: WIN[(key,n1)]+=1
        M["peace_events"]=len(events)
        # 経路によらない量・経路の見分け（同じ講和 [年・旧・新] で移った組が 2 件以上＝複数割譲〔ColonyList 奪取か植民地格の州の複数割譲〕・1 件＝境界の割譲〔CedeTerritory〕も混ざる）。
        for (y1,n0,n1,a),its in events.items():
            multi = len(its)>=2
            for v,c,dn,sh in its:
                if dn>=3000 and sh<0.5: M["multi_p3k_lowS" if multi else "single_p3k_lowS"]+=1
                if dn>=8000 and sh<0.5: M["multi_p8k_lowS" if multi else "single_p8k_lowS"]+=1
                if dn>=3000: M["multi_p3k" if multi else "single_p3k"]+=1
            if multi: M["multi_events"]+=1
            else: M["single_events"]+=1
            dmax=max([dn for _,_,dn,_ in its if dn==dn] or [float('nan')])
            if dmax==dmax: evkm[key].append(dmax)
        # 予測（旗 1）
        P=collections.Counter()
        for (y1,n0,n1,a),its in events.items():
            base=[(v,c) for v,c,_,_ in its]
            adj=[(v, c*(1+W*(dn/NORM if dn==dn else 0)*(1-sh))) for v,c,dn,sh in its]
            bmin=sum(c for _,c in base)
            for tag,B in (("up",100.0),("lo",bmin)):
                kept=greedy(adj,B)
                P[tag+"_all"]+=len(kept)
                for i,(v,c,dn,sh) in enumerate(its):
                    if i in kept:
                        if dn>=3000: P[tag+"_p3k"]+=1
                        if dn>=3000 and sh<0.5: P[tag+"_p3k_lowS"]+=1
                        if dn>=3000 and sh<0.5 and len(its)>=2: P[tag+"_multi_p3k_lowS"]+=1
                        if dn>=8000 and sh<0.5 and len(its)>=2: P[tag+"_multi_p8k_lowS"]+=1
                        if dn>=8000 and sh<0.5: P[tag+"_p8k_lowS"]+=1
        for k,v in P.items(): M["pred_"+k]=v
        if kms: M["km_median"]=statistics.median(kms)
        # 開幕（1783 年の最初の行）の艦の強さが 0 の国の集合が、1850・1900・1950 年に持つ首都から 3,000km 以上の植民地格の州の数（hol の記録の量）。
        # 弱い海軍＝その年の艦の強さが世界最大の 0.1 未満の国が持つ遠い植民地格の州（記録）。
        Z={n for n,v in nav.get(1783,{}).items() if v==v and v<=1e-9}
        for yr in (1850,1900,1950):
            qq=[q for q in qs if byq[q] and next(iter(byq[q].values()))[3]==yr]
            if not qq: continue
            q=qq[-1]; n=0; wk=0
            ny=nav.get(yr,{}); mx=max([v for v in ny.values() if v==v] or [0])
            for reg,(nat,t,p,y) in byq[q].items():
                if t!="Colonial": continue
                dist=gc(places.get(cap[q].get(nat)),places.get(reg))
                if not dist>=3000: continue
                if nat in Z: n+=1
                nv=ny.get(nat)
                if nv is not None and mx>0 and nv/mx<0.1: wk+=1
            M[f"openZeroNav_far_{yr}"]=n; M[f"weakNav_far_{yr}"]=wk
        for k,v in M.items():
            print(key,s,k,v,sep="\t",file=out); agg[key][k]+=v
print("# 合算（種の和・km_median は種の中央値の中央値）",file=out)
for key in agg:
    for k,v in sorted(agg[key].items()):
        if k=="km_median": continue
        print(key,"ALL",k,round(v,1),sep="\t",file=out)
print("# 講和 1 件（同じ年・旧・新）の中で最も遠い移転の距離 km の中央値（全種の講和をまとめた値・経路によらない量）",file=out)
for key in evkm: print(key,"ALL","event_maxkm_median",round(statistics.median(evkm[key]),1),sep="	",file=out)
print("# 勝者別（3,000km 以上・艦の世界比 0.5 未満の講和移転・件数の多い順の上位 6）",file=out)
for key in agg:
    top=sorted(((v,n) for (k,n),v in WIN.items() if k==key), reverse=True)[:6]
    print(key,"BYWINNER"," ".join(f"{n}:{v}" for v,n in top),sep="	",file=out)
