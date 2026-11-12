import csv,collections,sys,statistics as st
# (1076) 門の素案の基線の数：全ての国で、播種の時の海の割合（warm 行）÷1784 年の最初の行の海の割合、1784〜1789 の予算 0 の年、播種の艦
K='D:/PoB_work/keep_1069'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
R=collections.defaultdict(list); Z=collections.defaultdict(list); F=collections.defaultdict(list)
for s in seeds:
    w={}; y84={}; bud=collections.defaultdict(lambda:collections.defaultdict(float)); seen=set()
    with open(f'{K}/{d}/stock_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            n=r['nation']; t=r['time']
            try: a=float(r['armyUpkeepShare']); v=float(r['navyUpkeepShare']); fl=float(r['fleetUnitsStock'])
            except: continue
            if t=='warm': w[n]=(v/(a+v) if a+v>0 else float('nan'),fl); continue
            if t=='open': continue
            y=int(float(r['year']))
            if y==1784 and n not in y84: y84[n]=v/(a+v) if a+v>0 else float('nan')
            if 1784<=y<=1789:
                try: bud[n][y]+=float(r['navalBudgetCapitalPerMonth'])+float(r['navalBudgetOtherPerMonth'])
                except: pass
    for n,(sw,fl) in w.items():
        if not fl>0 or n not in y84 or not y84[n]>0: continue
        R[n].append(sw/y84[n]); F[n].append(fl); Z[n].append(sum(1 for y in range(1784,1790) if bud[n][y]==0))
rows=sorted(R,key=lambda n:-st.median(R[n]))
print(arm,'国\t海割warm÷1784（種の中央）\t予算0の年1784-89（中央）\t播種の艦（中央）')
for n in rows: print(n,f'{st.median(R[n]):.2f}',f'{st.median(Z[n]):.1f}',f'{st.median(F[n]):.0f}',sep='\t')
big=[n for n in rows if st.median(R[n])>2]; z6=[n for n in rows if st.median(Z[n])>=3]
print('# 比が2を超える国',len(big),'／艦を播いた国',len(rows),':',' '.join(big))
print('# 予算0が3年以上の国',len(z6),':',' '.join(z6))
