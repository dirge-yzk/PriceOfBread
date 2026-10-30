import csv,sys,collections,statistics as st
D=sys.argv[1]; NS=int(sys.argv[2]); SB=int(sys.argv[3]) if len(sys.argv)>3 else 1
def f(x):
    try:return float(x)
    except:return float('nan')
SEVEN=['イギリス','フランス','プロイセン','ロシア','オーストリア','アメリカ','日本']
tot=collections.Counter(); ev_all=[]
for s in range(SB,SB+NS):
    rows=list(csv.DictReader(open(f'{D}/stock_s{s}.tsv',encoding='utf-8-sig'),delimiter='\t'))
    byq=collections.defaultdict(dict)
    for r in rows:
        t=r['time']
        if t.startswith('q') and t[1:].isdigit(): byq[int(t[1:])][r['nation']]=r
    qs=sorted(byq)
    prev={}
    for q in qs:
        cur=byq[q]
        for n,r in cur.items():
            mo=f(r['fleetUnitsMovedOutHomeCum'])+f(r['fleetUnitsMovedOutColonialCum'])
            moH=f(r['fleetUnitsMovedOutHomeCum']);moC=f(r['fleetUnitsMovedOutColonialCum'])
            p=prev.get(n)
            if p is not None:
                dH=moH-p[1];dC=moC-p[2]
                if dH+dC>0.5:
                    # receivers: nations whose fleet rose this quarter
                    rec=[]
                    for m,rr in cur.items():
                        if m==n: continue
                        pf=prev.get(m)
                        if pf is None: continue
                        dF=f(rr['fleetUnitsStock'])-pf[0]
                        if dF>0.5*(dH+dC)*0.3: rec.append((m,round(dF,1),round(f(rr['navalBudgetCapitalPerMonth'])+f(rr['navalBudgetOtherPerMonth']),1),round(f(rr['portFleetGoodsStock']),1)))
                    ev_all.append((s,q,r['year'],n,round(dH,1),round(dC,1),round(p[0],1),round(f(r['fleetUnitsStock']),1),rec))
            prev[n]=(f(r['fleetUnitsStock']),moH,moC)
print('events',len(ev_all))
for e in ev_all[:60]: print(e)
bys=collections.defaultdict(lambda:[0,0.0,0.0])
for e in ev_all:
    bys[e[3]][0]+=1;bys[e[3]][1]+=e[4];bys[e[3]][2]+=e[5]
print('by sender (events, home ships, colonial ships)')
for n,v in sorted(bys.items(),key=lambda kv:-kv[1][1])[:20]: print(n,v)
