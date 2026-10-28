import csv,glob,re,collections,sys,statistics as st
sys.stdout.reconfigure(encoding="utf-8")
L=0.5
rows=["株式会社","社会主義","共産主義","帝国主義","ファシズム"]
def run(d):
    lit={}
    for f in glob.glob(d+"/lawgate_s*.tsv"):
        s=int(re.search(r"s(\d+)\.tsv",f).group(1))
        for r in csv.DictReader(open(f,encoding="utf-8"),delimiter="\t"):
            lit[(s,r["nation"],int(r["quarter"]))]=float(r["literacyHome"])
    def litat(s,n,q):
        for k in range(0,8):
            v=lit.get((s,n,q-k))
            if v is not None: return v
        return None
    ev=collections.defaultdict(list)
    for f in glob.glob(d+"/disc_s*.tsv"):
        s=int(re.search(r"s(\d+)\.tsv",f).group(1))
        for r in csv.DictReader(open(f,encoding="utf-8"),delimiter="\t"):
            if r["kind"]=="E" and r["row"] in rows:
                ev[(s,r["row"])].append((int(r["quarter"]),r["nation"]))
    firstlow=0;tot=0;early=0;earlylow=0;nev=0
    for (s,row),l in ev.items():
        l.sort(); q0=l[0][0]
        lt=litat(s,l[0][1],l[0][0]); tot+=1
        if lt is not None and lt<0.9*L: firstlow+=1
        for q,n in l:
            if q<=q0+80:
                x=litat(s,n,q)
                if x is None: continue
                early+=1
                if x<0.9*L: earlylow+=1
    return firstlow,tot,earlylow,early
for n in ["m617_ins","n1","n2"]:
    print(n,run("D:/PoB_work/keep_617/"+n))
