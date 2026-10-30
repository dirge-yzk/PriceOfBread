import csv,sys,collections,statistics as st
D=sys.argv[1]; NS=int(sys.argv[2]); SB=int(sys.argv[3]) if len(sys.argv)>3 else 1
def f(x):
    try:return float(x)
    except:return float('nan')
FIVE=['イギリス','フランス','プロイセン','ロシア','オーストリア']
N5={n:[] for n in FIVE}; ev_n=[]; used=[];tot_moved=0;ratios=[]
recv_budget0=0;recv_noport=0;recv_n=0; fleet1913={n:[] for n in FIVE}
sharesmall=[]
for s in range(SB,SB+NS):
    rows=list(csv.DictReader(open(f'{D}/stock_s{s}.tsv',encoding='utf-8-sig'),delimiter='\t'))
    byq=collections.defaultdict(dict)
    for r in rows:
        t=r['time']
        if t.startswith('q') and t[1:].isdigit(): byq[int(t[1:])][r['nation']]=r
    last=byq[max(byq)]
    for n in FIVE:
        if n in last: N5[n].append(f(last[n]['fleetUnitsMovedOutHomeCum']))
    for q,cur in byq.items():
        if str(cur.get(FIVE[0],{}).get('year',''))=='1913' or any(r['year']=='1913' for r in cur.values()):
            for n in FIVE:
                if n in cur: fleet1913[n].append(f(cur[n]['fleetUnitsStock']))
    prev={}
    for q in sorted(byq):
        cur=byq[q]
        ev=[]
        for n,r in cur.items():
            p=prev.get(n)
            if p is not None:
                d=f(r['fleetUnitsMovedOutHomeCum'])-p[1]
                if d>0.5: ev.append((n,d))
            prev[n]=(f(r['fleetUnitsStock']),f(r['fleetUnitsMovedOutHomeCum']))
        # prev now overwritten for senders; recompute pf
        if len(ev)==1:
            n,d=ev[0]
            # receivers: dF within 0.6..1.4 d
            pq=byq.get(q-1,{})
            for m,rr in cur.items():
                if m==n or m not in pq: continue
                dF=f(rr['fleetUnitsStock'])-f(pq[m]['fleetUnitsStock'])
                if 0.6*d<=dF<=1.6*d:
                    recv_n+=1
                    if not f(rr['navalOrderStatesCount'])>0: recv_budget0+=1   # 艦を発注した州が 0（その四半期）
                    if not f(rr['navyPaidPerMonth'])>0: recv_noport+=1         # 海軍の区分の支払いが 0
for n in FIVE:
    print(n,'N 種ごと',[round(x) for x in N5[n]],'中央値',round(st.median(N5[n])), '1913艦 中央値',round(st.median(fleet1913[n])) if fleet1913[n] else None)
print('受け手が一意に見える単独の送り手の事象:',recv_n,'件・うち受け手の navalOrderStatesCount=0:',recv_budget0,'・navyPaidPerMonth=0:',recv_noport)
