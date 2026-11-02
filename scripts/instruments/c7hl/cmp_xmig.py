import csv,sys,collections
sys.stdout.reconfigure(encoding='utf-8')
H='ヘルシングフォシュ'
def scan(root):
    tot=collections.defaultdict(float); rows=collections.defaultdict(int); partners=collections.defaultdict(float)
    for s in range(1,11):
        with open(f'{root}/xmig_s{s}.tsv',encoding='utf-8') as f:
            r=csv.reader(f,delimiter='\t'); h=next(r); i={n:k for k,n in enumerate(h)}
            for row in r:
                if row[i['fromRegion']]==H or row[i['toRegion']]==H:
                    v=float(row[i['movedKPerQuarter']]); d='out' if row[i['fromRegion']]==H else 'in'
                    tot[d]+=v; rows[d]+=1; partners[(d,row[i['toRegion']] if d=='out' else row[i['fromRegion']])]+=v
    return tot,rows,partners
for name,root in (('arm','D:/PoB_work/c7hl_arm/out_c7hlB_10/ins'),('base','D:/PoB_work/keep_773/base_ins')):
    t,r,p=scan(root)
    print(name,'ヘルシンキ越境 移民の和(千人・10種)',{k:round(v,2) for k,v in t.items()},'行数',dict(r))
    print('  上位',[(k,round(v,2)) for k,v in sorted(p.items(),key=lambda kv:-kv[1])[:6]])
