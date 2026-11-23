# (1326) 問い3 (a)：分与（sectreas_s）の新÷旧が 0.01 未満の行（ほぼ 0 の国庫で生まれる国）を腕ごとに数える
import sys,os,csv
for a in sys.argv[1:]:
    name,d=a.split('=',1); n=low=lowcap=0; ex=[]
    for f in sorted(os.listdir(d)):
        if not (f.startswith('sectreas_s') and f.endswith('.tsv')): continue
        for r in csv.DictReader(open(os.path.join(d,f),encoding='utf-8'),delimiter='\t'):
            n+=1; old=float(r['oldRuleT']); new=float(r['newRuleT'])
            if old>0 and new/old<0.01:
                low+=1; lowcap+= r['capitalRisingFlag']=='1'
                if len(ex)<4: ex.append(f"{f[9:-4]} {r['year']} {r['parent']}→{r['firstRegion']} 新÷旧={new/old:.4f} 移る人口={float(r['movedPopK']):.0f}")
    print(f'{name}\t分与の行 {n}\t新÷旧<0.01 {low}（首都発 {lowcap}）\t例 {"; ".join(ex)}')
