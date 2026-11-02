# r788：英が敗者の講和（1784〜1815）を種ごとに数え、同じ年に並んだ講和と invade_s の英が標的の数を並べる（keep の生だけ）
import sys, os, csv, collections, statistics as st
d=sys.argv[1]; s0=int(sys.argv[2]); UK='イギリス'
P=[];Y3=[];INV=[];ATK=[]
for s in range(s0,s0+10):
    ps=[r for r in csv.DictReader(open(os.path.join(d,f'cessp_s{s}.tsv'),encoding='utf-8'),delimiter='\t') if r['loser']==UK and 1784<=int(r['year'])<=1815]
    P.append(len(ps)); c=collections.Counter(r['year'] for r in ps); Y3.append(sum(v for v in c.values() if v>=3))
    iv=[r for r in csv.DictReader(open(os.path.join(d,f'invade_s{s}.tsv'),encoding='utf-8'),delimiter='\t') if r['target']==UK and 1784<=int(r['year'])<=1815]
    INV.append(len(iv))
    ws=[r for r in csv.DictReader(open(os.path.join(d,f'wars_s{s}.tsv'),encoding='utf-8'),delimiter='\t') if r['civilWarFlag']=='0' and r['sideB']==UK and 1784<=int(r['startYear'])<=1815]
    ATK.append(len(ws))
print(d,'英の敗戦の講和 中央',st.median(P),P,'・同じ年に3本以上の講和に入った数 中央',st.median(Y3),'・invade の標的 中央',st.median(INV),'・B側として立った戦争 中央',st.median(ATK))
