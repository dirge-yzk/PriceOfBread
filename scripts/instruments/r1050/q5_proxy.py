import csv,collections
K='D:/PoB_work/keep_1044'
NS=['イギリス','オーストリア','フランス','ロシア','オスマン','スペイン','プロイセン']
for arm,d,seeds in [('base','base_ins',range(1,11)),('n1','n1',range(1,11)),('n2','n2',range(1,11)),('a11','a11',range(11,21))]:
    once=collections.defaultdict(set); core=collections.defaultdict(set); cnt_core=collections.Counter(); allc=collections.defaultdict(set)
    for s in seeds:
        cs=collections.defaultdict(set)
        for r in csv.DictReader(open(f'{K}/{d}/cess_s{s}.tsv',encoding='utf-8'),delimiter='\t'): cs[(r['year'],r['winner'],r['loser'])].add(r['tier'])
        for r in csv.DictReader(open(f'{K}/{d}/imp_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
            L=r['loser']; allc[L].add(s)
            if r['orderInMonthIndex']=='1': once[L].add(s)
            t=cs.get((r['year'],r['winner'],L),set())
            if t & {'Core','Integrated'}: core[L].add(s); cnt_core[L]+=1
    print(arm,'国: 矯正のある種 / 月の1本目だけでも残る種 / 本国格の州の割譲と同じ年の矯正（件・種）〈近似の代理〉')
    for n in NS: print(f'  {n}\t{len(allc[n])}\t{len(once[n])}\t{cnt_core[n]}件・{len(core[n])}種')
