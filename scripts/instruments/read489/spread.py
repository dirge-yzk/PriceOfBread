import io,csv,sys,collections,statistics as st
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding="utf-8")
D='D:/PoB_work/keep_484/m484_ins'
want={'黄鉄鉱法','キニーネの単離','ガス灯','機関車','鉄筋コンクリート','転炉鋼','木材パルプ製紙','甜菜糖','汽船','電化製品','蒸気タービン'}
out=collections.defaultdict(list)
for s in range(1,11):
    E={};T=collections.defaultdict(lambda: collections.defaultdict(dict))
    with open(f'{D}/engineer_s{s}.tsv',encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t');next(r)
        for x in r:
            if x[4] not in want: continue
            q=int(x[1]);a,te,ne=float(x[6]),float(x[7]),float(x[9])
            if x[0]=='E':
                if x[4] not in E or q<E[x[4]][0]: E[x[4]]=(q,x[3],int(x[2]))
            else:
                if x[8]=='0': T[x[4]][q][x[3]]=(a,te,ne)
    for row,(q,nat,y) in E.items():
        qs=[qq for qq in T[row] if qq<=q and len(T[row][qq])>=20]
        if not qs: continue
        snap=T[row][max(qs)]
        top=sorted(snap.items(),key=lambda kv:-(kv[1][1]+kv[1][2]))[:10]
        sdT=st.pstdev(v[1] for _,v in top); sdN=st.pstdev(v[2] for _,v in top)
        china=snap.get('清'); uk=snap.get('イギリス')
        out[row].append((s,y,nat,round(sdT,1),round(sdN,1),top[0][0],
            None if not china else (round(china[0]),round(china[1],1),round(china[2],1)),
            None if not uk else (round(uk[0],1),round(uk[1],1),round(uk[2],1)), max(qs), q))
for row,L in out.items():
    print('##',row)
    for t in L: print('  ',t)
