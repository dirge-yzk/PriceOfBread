"""(698) rd47：国全体の工場労働者の割合（兵士を除く人口に対する）を、開幕（open＝構築の直後）・助走の直後（warm）・q0 以後の四半期で並べる（読むだけ）。
使い方：python -X utf8 -I class_share.py <class_s<種>.tsv> ...
限界：class_s は計器の 9 国だけ・国の名で引く（改名した国は途中で切れる）。
"""
import csv,sys,collections
csv.field_size_limit(10**9)
def go(p, nats, times):
    t=collections.defaultdict(lambda: collections.defaultdict(float))
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r)
        for x in r:
            if len(x)!=len(h): continue
            if x[0] not in times or x[1] not in nats: continue
            try: s=float(x[5])
            except: continue
            if not s>0: continue
            t[(x[1],x[0])][x[4]]+=s
    for n in nats:
        out=[]
        for tm in times:
            d=t[(n,tm)]; tot=sum(v for k,v in d.items() if k!='兵士')
            out.append(f"{tm}:{d['工場労働者']/tot:.3f}" if tot>0 else f"{tm}:-")
        print(n,' '.join(out))
times=['open','warm','q0','q1','q4','q8','q12','q16','q20','q24','q40','q80']
for p in sys.argv[1:]:
    print('#',p); go(p,['アメリカ','イギリス','フランス','プロイセン'],times)
