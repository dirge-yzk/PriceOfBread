# 段0 の量（読むだけ）：keep_675/m675_ins の spend_s・factory_s・class_s から、資本家の貯蓄の流れ・建設の額・資金÷資本の値打ち（年）
import csv,sys,statistics as st,collections as C
R=sys.argv[1] if len(sys.argv)>1 else "D:/PoB_work/keep_675/m675_ins"   # (701) smu1：生のフォルダを引数で渡せる（既定は (677) の keep_675）
NAT=["イギリス","フランス","プロイセン","オーストリア","ロシア","オスマン","清","日本","アメリカ"]
BK={0:"1783",1:"1784",2:"1785-92",3:"1793-1822",4:"1823-29",5:"1830-39",6:"1840-49",7:"1850-82",8:"1883-1950"}
res=C.defaultdict(lambda:C.defaultdict(list))
for s in range(1,11):
    sp=C.defaultdict(lambda:C.defaultdict(float))
    with open(f"{R}/spend_s{s}.tsv",encoding="utf-8") as f:
        r=csv.DictReader(f,delimiter="\t")
        for x in r:
            if x["className"]!="資本家" or x["nation"] not in NAT: continue
            k=(int(x["bucket"]),x["nation"]); kd=x["kind"]
            pay=float(x["payPerMonth"] or 0)
            if kd=="class": sp[k]["E"]+=float(x["receiptPerMonth"]); sp[k]["P"]+=pay
            elif kd=="extra":
                n=x["slotName"]
                if n.startswith("建設の材料"): sp[k]["C"]+=pay; sp[k]["Q"]+=float(x["qtyPerMonth"] or 0)
                elif n.startswith("建設に付く"): sp[k]["C"]+=pay
                else: sp[k]["M"]+=pay
            elif kd=="good": sp[k]["G"]+=pay
    fac=C.defaultdict(list)
    with open(f"{R}/factory_s{s}.tsv",encoding="utf-8") as f:
        r=csv.DictReader(f,delimiter="\t")
        for x in r:
            if x["nation"] not in NAT: continue
            q=int(x["quarter"]); b=0 if q<=3 else 1 if q<=7 else 2 if q<=39 else 3 if q<=159 else 4 if q<=187 else 5 if q<=227 else 6 if q<=267 else 7 if q<=399 else 8
            fac[(b,x["nation"])].append(float(x["factoryLaborPopK"] or 0))
    wage=C.defaultdict(list); gdp=C.defaultdict(list); bond=C.defaultdict(list)
    def bk(q): return 0 if q<=3 else 1 if q<=7 else 2 if q<=39 else 3 if q<=159 else 4 if q<=187 else 5 if q<=227 else 6 if q<=267 else 7 if q<=399 else 8
    with open(f"{R}/class_s{s}.tsv",encoding="utf-8") as f:
        r=csv.DictReader(f,delimiter="	")
        for x in r:
            if x["className"]!="工場労働者" or x["nation"] not in NAT or not x["time"].startswith("q"): continue
            sz=float(x["sizeK"] or 0); e=float(x["earnedPerMonth"] or 'nan')
            if sz>0 and e==e: wage[(bk(int(x["time"][1:])),x["nation"])].append(12*e/sz)
    with open(f"{R}/nation_s{s}.tsv",encoding="utf-8") as f:
        r=csv.DictReader(f,delimiter="	")
        for x in r:
            if x["nation"] not in NAT: continue
            k=(bk(int(x["quarter"])),x["nation"])
            try: gdp[k].append(float(x["nominalGdpPerYear"])); bond[k].append(float(x["bondRateMeanPerYear"]))
            except: pass
    for k,v in sp.items():
        if v["E"]<=0: continue
        cons=v["P"]-v["C"]-v["M"]
        flow=v["E"]-cons
        N=st.mean(fac[k]) if fac[k] else float('nan')
        uc=v["C"]/v["Q"] if v["Q"]>0 else float('nan')
        K=N*uc
        res[k]["chk"].append((v["G"])/(v["P"]-v["C"]-v["M"]) if cons>0 else float('nan'))
        res[k]["flow/E"].append(flow/v["E"])
        res[k]["flow/C"].append(flow/v["C"] if v["C"]>0 else float('nan'))
        res[k]["F/K(1/yr)"].append(12*flow/K if K>0 else float('nan'))
        res[k]["C/K(1/yr)"].append(12*v["C"]/K if K>0 else float('nan'))
        wg=st.mean(wage[k]) if wage[k] else float('nan')
        res[k]["uc/wage(yr)"].append(uc/wg)
        res[k]["E/K(1/yr)"].append(12*v["E"]/K if K>0 else float('nan'))
        i_=st.mean(bond[k]) if bond[k] else float('nan')
        res[k]["i"].append(i_)
        res[k]["(E/K)/i"].append(12*v["E"]/K/i_ if K>0 and i_>0 else float('nan'))
        res[k]["C/GDP"].append(12*v["C"]/st.mean(gdp[k]) if gdp[k] else float('nan'))
def med(a):
    a=[x for x in a if x==x]; return st.median(a) if a else float('nan')
def rng(a):
    a=sorted(x for x in a if x==x); return (a[0],a[-1]) if a else (float('nan'),)*2
cols=["flow/E","flow/C","F/K(1/yr)","C/K(1/yr)","uc/wage(yr)","E/K(1/yr)","i","(E/K)/i","C/GDP"]
print("bucket\tnation\t"+"\t".join(cols)+"\t(F/K min..max over seeds)")
for b in range(9):
    for n in NAT:
        k=(b,n)
        if k not in res: continue
        lo,hi=rng(res[k]["F/K(1/yr)"])
        print(BK[b]+"\t"+n+"\t"+"\t".join(f"{med(res[k][c]):.3g}" for c in cols)+f"\t{lo:.3g}..{hi:.3g}")
