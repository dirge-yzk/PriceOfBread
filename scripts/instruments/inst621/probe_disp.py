import csv,glob,re,collections,sys,statistics as st
sys.stdout.reconfigure(encoding="utf-8")
maj=["イギリス","フランス","プロイセン","オーストリア","ロシア","アメリカ","日本","清","オスマン"]
rows=["株式会社","社会主義","共産主義","帝国主義","ファシズム"]
def load(d):
    ev=collections.defaultdict(dict)  # (seed,row)->{nation:q}
    for f in glob.glob(d+"/disc_s*.tsv"):
        s=int(re.search(r"s(\d+)\.tsv",f).group(1))
        for r in csv.DictReader(open(f,encoding="utf-8"),delimiter="\t"):
            if r["kind"]=="E" and r["row"] in rows and r["nation"] in maj:
                ev[(s,r["row"])][r["nation"]]=int(r["quarter"])
    return ev
for name in ["m617_ins","n1","n2"]:
    ev=load("D:/PoB_work/keep_617/"+name)
    print("==",name)
    for row in rows:
        same=[];rng=[];nopen=[];first=[]
        for s in range(1,11):
            d=ev.get((s,row),{})
            nopen.append(len(d))
            if d:
                m=min(d.values()); same.append(sum(1 for v in d.values() if v==m)); rng.append((max(d.values())-m)/4); first.append(1783+m/4)
        print(row,"開いた主要国(中央)",st.median(nopen),"最初と同四半期(中央)",st.median(same),"年の幅(中央)",round(st.median(rng),1),"最初の年(中央)",round(st.median(first),1))
