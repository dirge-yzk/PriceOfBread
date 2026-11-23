# (1326) 問い2：直す線の素案の紙の見込み＝電灯網の投入から 送電網・電化製品 を外して 電力 q を足したときの投入費÷電化インフラの値段（国の平均の値段＝近似）
import csv,sys,collections,statistics as st
R=sys.argv[1]; YRS=[1890,1913,1930]; Q=[0.0,0.3,0.6,1.0]
NAT=['イギリス','フランス','プロイセン','清','アメリカ合衆国','ロシア','日本']
res=collections.defaultdict(list)
for s in range(1,11):
    px=collections.defaultdict(dict)
    with open(f'{R}/nationgood_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            y=int(r['year'])
            if y in YRS and r['nation'] in NAT:
                try: px[(y,r['nation'])][r['good']]=float(r['priceMeanGold'])
                except: pass
    for (y,n),p in px.items():
        need=('電化インフラ','鋼','労働力','高度労働力','ガス','電力')
        if not all(k in p and p[k]>0 for k in need): continue
        base=0.2*p['鋼']+0.5*p['労働力']+0.2*p['高度労働力']; gl=0.6*p['ガス']+base
        for q in Q: res[(y,q)].append(((base+q*p['電力'])/p['電化インフラ'], (base+q*p['電力'])/gl))
        res[(y,'P電力/Pガス')].append((p['電力']/p['ガス'],0))
for k in sorted(res,key=str):
    a=res[k]; print(k,'n=',len(a),'費÷P 中央=%.3g'%st.median(x[0] for x in a),'(<1:%d)'%sum(x[0]<1 for x in a),'費÷ガス灯費 中央=%.3g'%st.median(x[1] for x in a),'(<1:%d)'%sum(x[1]<1 for x in a))
