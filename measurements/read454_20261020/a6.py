import sys,csv,collections
sys.stdout.reconfigure(encoding="utf-8")
D='D:/PoB_work/keep_451/m451_ins/'
W=['化学薬品','機械','石炭','染料','砂糖','灯油','原油','漁獲','絹織物','生糸','電力','ガス','加工肉','缶詰','家事奉仕','労働力','鋼','鉄','ゴム','生ゴム','肥料','グアノ','住居','製材','セメント','紙','木','蒸気機関','帆船','汽船','馬','硫黄','革','畜産','冷蔵庫','電化製品','内燃機関','新聞','ラジオ','自動車','自転車']
P=[(1783,1809),(1810,1839),(1840,1869),(1870,1899),(1900,1950)]
def per(y):
    for i,(a,b) in enumerate(P):
        if a<=y<=b: return i
g=collections.defaultdict(list); nat=set()
for s in range(1,11):
    with open(D+f'pxgoods_s{s}.tsv',encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t');h=next(r)
        ix={k:i for i,k in enumerate(h)}
        for x in r:
            nat.add(x[1])
            if x[3] in W:
                try: v=float(x[ix['priceOverRefTimesLevelMedianRatio']]); pm=float(x[ix['priceMean']]); lv=float(x[ix['regionPriceLevelMeanIndex']])
                except: continue
                g[(x[3],per(int(x[2])))].append((v,pm,lv,float(x[ix['lastProducedQty']]),float(x[ix['lastDemandQty']])))
print(sorted(nat))
def med(L):
    L=sorted(L); return L[len(L)//2] if L else float('nan')
for w in W:
    s=f"{w:6}"
    for i in range(len(P)):
        L=g.get((w,i),[])
        s+=f" | {med([x[0] for x in L]):.2f}" if L else " | -"
    print(s)
