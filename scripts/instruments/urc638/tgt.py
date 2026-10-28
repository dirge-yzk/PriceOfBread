import csv,collections,sys,statistics as st
d=sys.argv[1]; seeds=range(1,11)
KEY=['イギリス','フランス','プロイセン','ネーデルラント','日本','清','アメリカ','ロシア','オーストリア']
# per nation: expected hits by province (sum over quarters of maxUS/4), per seed
res=collections.defaultdict(list)   # nation -> list of per-seed (dict prov->weight)
ratio=collections.defaultdict(list) # nation, era -> maxUS / UrbanShare
marg=collections.defaultdict(list)
for s in seeds:
    byq=collections.defaultdict(list)
    for r in csv.DictReader(open(f'{d}/region_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
        if r['nation'] not in KEY: continue
        byq[(int(r['quarter']),r['nation'])].append((r['region'],r['tier'],float(r['populationK']),float(r['ruralPopulationK'])))
    W=collections.defaultdict(lambda: collections.defaultdict(float))
    for (q,n),L in byq.items():
        L=[x for x in L if x[2]>1e-9]
        if not L: continue
        us=[(1-x[3]/x[2],x) for x in L]; us.sort(key=lambda t:-t[0])
        mx=us[0][0]; tot=sum(x[2] for x in L); urb=sum(x[2]-x[3] for x in L)
        W[n][us[0][1][0]]+=mx/4
        yr=1783+q//4
        era=(yr-1783)//40
        ratio[(n,era)].append(mx/(urb/tot) if urb>0 else 1)
        if len(us)>1: marg[n].append(mx-us[1][0])
    for n in W: res[n].append(W[n])
for n in KEY:
    # average share of expected hits by province across seeds (province names are seed-specific for colonies; core names stable)
    sh=collections.defaultdict(list)
    for W in res[n]:
        T=sum(W.values())
        for p,w in W.items(): sh[p].append(w/T)
    top=sorted(((sum(v)/10,p) for p,v in sh.items()),reverse=True)[:4]
    nh=st.mean(sum(W.values()) for W in res[n])
    print(n,'期待発生件数/試合(係数前) %.1f'%nh,' 標的の上位:',', '.join(f'{p} {v:.2f}' for v,p in top),'| 最大と次点の割合の差の中央値 %.3f'%st.median(marg[n]) if marg[n] else '')
    print('   最大の割合÷国の割合(40年刻み 1783,1823,..):',' '.join('%.2f'%st.median(ratio[(n,e)]) for e in range(5) if ratio[(n,e)]))
