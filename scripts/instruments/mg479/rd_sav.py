# (479) 段0の読み：農民・労働者の貯蓄÷月の所得（階級の平均）の国×階級×期の中央値。使い方：python rd_sav.py <計器の出力フォルダ> 1,2,3,4,5
import csv, sys, statistics as st, collections
base=sys.argv[1]; seeds=[int(x) for x in sys.argv[2].split(',')]
want=['イギリス','アメリカ','清','ロシア','フランス','プロイセン']
res=collections.defaultdict(lambda:collections.defaultdict(list))
for sd in seeds:
    with open(f'{base}/class_s{sd}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['nation'] not in want or r['classId'] not in ('1','2'): continue
            try: y=int(r['year']); v=float(r['savingsToEarnedMonths'])
            except: continue
            if r['event']=='open' if 'event' in r else False: pass
            per='1783-99' if y<1800 else '1800-49' if y<1850 else '1850-99' if y<1900 else '1900-49'
            res[(r['nation'],r['className'],per)][sd].append(v)
print('nation class period : savings-to-earned months (median over seeds of per-seed median)')
for k in sorted(res):
    meds=[st.median(v) for v in res[k].values() if v]
    print(*k, f"{st.median(meds):.2f}")
