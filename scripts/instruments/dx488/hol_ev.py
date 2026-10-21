import re,csv,sys,collections,statistics as st
sys.stdout.reconfigure(encoding="utf-8")
I="D:/PoB_work/dx_488/out_dxB_10/ins"
ev=[]
for ln in open("D:/PoB_work/wt_dx/measurements/dx488_20261021/B_hol.txt",encoding="utf-8"):
    m=re.match(r"\s*\[(飢え|その他|移民|戦争)[^\]]*1/10\] s(\d+) (\S+?)（(\S+?)・(\w+)）\s+([\d.e+]+)\((\d+)\)→([\d.e+]+)\((\d+)\)",ln)
    if m: ev.append((m.group(1),int(m.group(2)),m.group(3),m.group(4),m.group(5),float(m.group(6)),int(m.group(7)),float(m.group(8)),int(m.group(9))))
print(len(ev))
bys=collections.defaultdict(list)
for e in ev: bys[e[1]].append(e)
rows=[]
for s,es in bys.items():
    ser=collections.defaultdict(list)
    with open(f"{I}/mort_s{s}.tsv",encoding="utf-8") as f:
        for r in csv.DictReader(f,delimiter="\t"):
            ser[(r["nation"],r["regionName"])].append(r)
    for e in es:
        lst=ser.get((e[3],e[2]),[])
        w=[r for r in lst if e[6]<=int(r["year"])<=e[8]]
        if not w: rows.append((e,None)); continue
        b=sum(float(r["birthsK"]) for r in w); d=sum(float(r["deathsK"]) for r in w); h=sum(float(r["deathsHungerK"]) for r in w)
        py=sum(float(r["popMeanK"])*float(r["monthsCount"])/12 for r in w)
        fill=sum(float(r["foodFill"]) for r in w)/len(w); us=sum(float(r["urbanShare"]) for r in w)/len(w)
        rows.append((e,(1000*b/py,1000*d/py,h/d if d else 0,fill,us,len(w))))
ok=[r for r in rows if r[1]]
print("matched",len(ok))
print("CBR median",st.median(r[1][0] for r in ok),"CDR median",st.median(r[1][1] for r in ok),"hunger share of deaths median",st.median(r[1][2] for r in ok),"fill median",st.median(r[1][3] for r in ok),"urban median",st.median(r[1][4] for r in ok))
for r in sorted(ok,key=lambda r:-r[1][2])[:10]: print(r[0][:5],r[0][5],r[0][6],r[0][7],r[0][8],[round(x,3) for x in r[1][:5]])
print("nations:",collections.Counter(r[0][3] for r in ok).most_common())
