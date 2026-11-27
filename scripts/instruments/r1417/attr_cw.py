# (1417) 内戦の終わり（cwend）を型で数える：首都の蜂起・分離・その他（勢力の蜂起）。旗 0（base_ins）と旗 1（c150sdB）
import csv, collections
for name,d in (('flag0_M','D:/PoB_work/keep_1411/base_ins'),('flag1_sdB','D:/PoB_work/keep_1411_rej/c150sdB/ins')):
    c=collections.Counter(); n=0
    for s in range(1,11):
        for x in csv.DictReader(open(f'{d}/cwend_s{s}.tsv', newline='', encoding='utf-8'), delimiter='\t'):
            n+=1; c[(x['capitalRisingFlag'], x['secessionFlag'])]+=1
    print(name, 'cwend rows', n, ' (capitalRising,secession):', dict(c))
