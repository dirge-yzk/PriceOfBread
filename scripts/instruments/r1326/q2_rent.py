# (1326) 問い2：sellr_s の電化インフラの行（州ごとに 2 行＝作り手の順＝電灯網〔Simulation.cs:1164〕→ガス灯〔:1167〕）の rentR と laborWeight を年の最後の四半期で数える
import csv,sys,collections
R=sys.argv[1]; out=open(sys.argv[2],'w',encoding='utf-8'); YRS=sys.argv[3].split(',')
out.write('種\t年\t州\t電灯網 rentR>0\t電灯網 laborWeight>0\tガス灯 rentR>0\tガス灯 laborWeight>0\t電灯網 rentR 中央\n')
import statistics as st
for s in range(1,11):
    rows=collections.defaultdict(list)
    with open(f'{R}/sellr_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['good']=='電化インフラ' and r['year'] in YRS: rows[(r['year'],r['q'])].append(r)
    last={}
    for (y,q) in rows: last[y]=max(last.get(y,-1),int(q))
    for y,q in sorted(last.items()):
        per=collections.defaultdict(list)
        for r in rows[(y,str(q))]: per[r['region']].append(r)
        c=collections.Counter(); er=[]
        for reg,l in per.items():
            if len(l)!=2: c['2行でない']+=1; continue
            e,g=l; c['州']+=1
            c['e_r']+=float(e['rentR'])>0; c['e_w']+=float(e['laborWeight'])>0; c['g_r']+=float(g['rentR'])>0; c['g_w']+=float(g['laborWeight'])>0
            er.append(float(e['rentR']))
        out.write(f's{s}\t{y}\t{c["州"]}\t{c["e_r"]}\t{c["e_w"]}\t{c["g_r"]}\t{c["g_w"]}\t{st.median(er):.3g}\n')
