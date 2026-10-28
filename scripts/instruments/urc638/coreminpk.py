# 中核の州の最小÷ピーク（種ごと・読むだけ）。使い方：python -X utf8 -I coreminpk.py <ディレクトリ>...
import csv,sys,collections,statistics as st
CORE=[('ネーデルラント','アムステルダム'),('イギリス','ロンドン'),('プロイセン','ベルリン'),('フランス','パリ'),('イギリス','ダブリン')]
for d in sys.argv[1:]:
    out=collections.defaultdict(list)
    for s in range(1,11):
        ser=collections.defaultdict(list)
        for r in csv.DictReader(open(f'{d}/region_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
            k=(r['nation'],r['region'])
            if k in CORE: ser[k].append(float(r['populationK']))
        for k in CORE:
            v=ser[k]
            if not v: continue
            pk=v[0]; m=1e9; 
            for x in v:
                pk=max(pk,x); m=min(m,x/pk)
            out[k].append(m)
    print(d.split('/')[-1])
    for k in CORE: print('  ',k[1],' '.join('%.2f'%x for x in out[k]),'｜最小 %.3f・中央値 %.3f'%(min(out[k]),st.median(out[k])) if out[k] else '')

# 門の下端（[4] ③ の追記 [11]）：m627・N1・N2 の種の中央値の最小 − 2 標準誤差（標準誤差＝m627 の種ごとのばらつき÷√10・pstdev）。使い方：引数は m627 n1 n2 の順の 3 つ。
if len(sys.argv)==4:
    arms={}
    for d in sys.argv[1:]:
        o=collections.defaultdict(list)
        for s in range(1,11):
            ser=collections.defaultdict(list)
            for r in csv.DictReader(open(f'{d}/region_s{s}.tsv',encoding='utf-8'),delimiter='	'):
                k=(r['nation'],r['region'])
                if k in CORE: ser[k].append(float(r['populationK']))
            for k in CORE:
                v=ser[k]
                if not v: continue
                pk=v[0]; m=1e9
                for x in v:
                    pk=max(pk,x); m=min(m,x/pk)
                o[k].append(m)
        arms[d]=o
    print('## 門の下端（中央値の最小 − 2 標準誤差）')
    for k in CORE:
        meds=[st.median(arms[d][k]) for d in sys.argv[1:]]
        se=st.pstdev(arms[sys.argv[1]][k])/10**0.5
        print('  ',k[1],'中央値 m627/n1/n2 = %.3f/%.3f/%.3f'%tuple(meds),'標準誤差 %.3f'%se,'→ 下端 %.3f'%(min(meds)-2*se))
