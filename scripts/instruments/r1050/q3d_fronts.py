import csv,collections,sys,statistics as st
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
UK='イギリス'
fr=collections.defaultdict(list); sh=collections.defaultdict(list); ukw=[]
for s in seeds:
    wars=[(r['sideA'],r['sideB'],int(r['q'])*3,int(r['q'])*3+int(r['months'])) for r in rows(f'{K}/{d}/wars_s{s}.tsv')]
    def conc(n,m): return sum(1 for a,b,s0,e in wars if n in (a,b) and s0<=m<=e)
    soc={}
    for r in rows(f'{K}/{d}/stock_s{s}.tsv'):
        if r['time'].startswith('q'):
            try: soc[(r['nation'],int(r['time'][1:]))]=float(r['soldierToPopulationShare'])
            except: pass
    imps={(r['month'],r['loser']) for r in rows(f'{K}/{d}/imp_s{s}.tsv')}
    for r in rows(f'{K}/{d}/cpeace_s{s}.tsv'):
        if int(r['nCollapse'])==0: continue
        m=int(r['month']); L=r['loser']; key=('英' if L==UK else '他')+('・矯正あり' if (r['month'],L) in imps else '・矯正なし')
        fr[key].append(conc(L,m)); v=soc.get((L,m//3-1))
        if v is not None and v==v: sh[key].append(v)
    # 英の平時を含む全月の同時の戦争の数（1年ごと）
    for m in range(0,2004,12): ukw.append(conc(UK,m))
print(arm,'崩壊の講和の月の同時の戦争の数（中央・平均・件）と、前の四半期の兵士÷人口（中央・件）')
for k in sorted(fr): print(' ',k,st.median(fr[k]),round(st.mean(fr[k]),2),len(fr[k]),'| 兵÷人口',round(st.median(sh[k]),4) if sh[k] else None,len(sh[k]))
print('  英の同時の戦争の数（毎年の1月・全種）中央',st.median(ukw),'平均',round(st.mean(ukw),2))
