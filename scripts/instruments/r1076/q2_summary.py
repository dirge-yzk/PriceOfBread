import sys
# (1076) out_q1_<arm>.txt から国ごとの要約：播種の時の海の割合と翌年の割合・予算 0 の年・艦の数の推移・損耗だけの予言との比
W=0.995  # 軍艦の月の損耗（stock_s の q0→q1 の比 4634→4565＝0.985/四半期 で確かめた・Catalog.WearRatePerMonthOf）
for arm in sys.argv[1:]:
    cur=None; T={}
    for line in open(f'D:/PoB_work/r1076_work/out_q1_{arm}.txt',encoding='utf-8'):
        f=line.rstrip('\n').split('\t')
        if line.startswith('# '): cur=line.split()[1]; T[cur]={}; continue
        if f[0]=='年': continue
        T[cur][f[0]]=f
    print(f'## {arm}')
    print('国\t播種(warm)\t海割warm\t海割1784\t海割1786\t予算0の年(1784-1800)\t最初の発注年\t艦1790\t損耗だけ1790\t艦1800\t艦1810\t艦1830')
    for n,t in T.items():
        def v(y,i):
            try: return float(t[str(y)][i])
            except: return float('nan')
        sea=lambda y: v(y,4)/(v(y,3)+v(y,4))
        z=[y for y in range(1784,1801) if v(y,6)+v(y,7)==0]
        first=next((y for y in range(1784,1831) if v(y,6)+v(y,7)>0),None)
        pred=v('warm',1)*W**(12*7)
        print(n,f"{v('warm',1):.0f}",f"{sea('warm'):.3f}",f'{sea(1784):.3f}',f'{sea(1786):.3f}',f'{len(z)}({min(z) if z else "-"}-{max(z) if z else "-"})',first,f'{v(1790,1):.0f}',f'{pred:.0f}',f'{v(1800,1):.0f}',f'{v(1810,1):.0f}',f'{v(1830,1):.0f}',sep='\t')
