import csv
# (1076) 播いた国（stock_s の warm 行で fleetUnitsStock>0）のうち、1784 年の軍艦の能力（prodcap の capacity の国の和）が 1e-3 未満の国の数（種ごと）
K='D:/PoB_work/keep_1069'
for arm,d,seeds in (('base','base_ins',range(1,11)),('n1','n1',range(1,11)),('n2','n2',range(1,11)),('a11','a11',range(11,21))):
    cnt=[];tot=[]
    for s in seeds:
        seeded=set()
        with open(f'{K}/{d}/stock_s{s}.tsv',encoding='utf-8') as f:
            for r in csv.DictReader(f,delimiter='\t'):
                if r['time']=='warm':
                    try:
                        if float(r['fleetUnitsStock'])>0: seeded.add(r['nation'])
                    except: pass
        cap={}
        with open(f'{K}/{d}/prodcap_s{s}.tsv',encoding='utf-8') as f:
            for r in csv.DictReader(f,delimiter='\t'):
                if r['good']=='軍艦' and r['year']=='1784': cap[r['ownerNation']]=cap.get(r['ownerNation'],0)+float(r['capacity'])
        cnt.append(sum(1 for n in seeded if cap.get(n,0)<1e-3)); tot.append(len(seeded))
    print(arm,'播いた国のうち 1784 の軍艦の能力が 1e-3 未満の国（種ごと）',cnt,'播いた国',tot)
