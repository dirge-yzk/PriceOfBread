import csv,collections,sys,json,math
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
W=json.load(open('C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json',encoding='utf-8'))['nations']
LL={n['name']:(n['latitude'],n['longitude']) for n in W}
def km(a,b):
    (la1,lo1),(la2,lo2)=a,b; p=math.pi/180
    h=math.sin((la2-la1)*p/2)**2+math.cos(la1*p)*math.cos(la2*p)*math.sin((lo2-lo1)*p/2)**2
    return 2*6371*math.asin(math.sqrt(h))
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
GREAT=None
res=collections.Counter(); far=collections.Counter(); cedeTier=collections.Counter(); tot=collections.Counter()
for s in seeds:
    cess=rows(f'{K}/{d}/cess_s{s}.tsv')
    cs=collections.defaultdict(list)
    for r in cess: cs[(r['year'],r['winner'],r['loser'])].append(r['tier'])
    for r in rows(f'{K}/{d}/imp_s{s}.tsv'):
        L=r['loser']; Wn=r['winner']
        if Wn not in LL or L not in LL: res[('座標なし',L=='イギリス')]+=1; continue
        dk=km(LL[Wn],LL[L]); grp='英' if L=='イギリス' else '他'
        tot[grp]+=1
        if dk>6000: far[grp]+=1
        t=cs.get((r['year'],Wn,L))
        cedeTier[(grp, 'なし' if not t else '/'.join(sorted(set(t))))]+=1
print(arm,'矯正講和の件数',dict(tot),' 首都間 6000km 超（条約港の門の距離）',dict(far))
print('同じ年・同じ勝者敗者の割譲の行（tier）:',sorted(cedeTier.items(),key=lambda kv:-kv[1])[:10])
