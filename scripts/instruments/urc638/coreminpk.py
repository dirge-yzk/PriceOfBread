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
