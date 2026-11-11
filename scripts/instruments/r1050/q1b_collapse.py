import csv,collections,sys
K='D:/PoB_work/keep_1044'
for arm,d,seeds in [('base','base_ins',range(1,11)),('n1','n1',range(1,11)),('n2','n2',range(1,11)),('a11','a11',range(11,21))]:
    c=collections.Counter()
    for s in seeds:
        cp={(r['month'],r['loser']):r for r in csv.DictReader(open(f'{K}/{d}/cpeace_s{s}.tsv',encoding='utf-8'),delimiter='\t')}
        for r in csv.DictReader(open(f'{K}/{d}/imp_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
            x=cp.get((r['month'],r['loser']))
            k='行なし' if x is None else ('崩壊だけ' if int(x['nOther'])==0 and int(x['nCollapse'])>0 else '崩壊と押しが混在' if int(x['nCollapse'])>0 else '押しだけ')
            c[(r['loser']=='イギリス',k)]+=1
    print(arm,'矯正講和の月×敗者の cpeace（英か, 区分）:',sorted(c.items()))
