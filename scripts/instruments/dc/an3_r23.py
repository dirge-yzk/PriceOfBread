import csv,glob,sys,collections,statistics as st
sys.stdout.reconfigure(encoding='utf-8'); csv.field_size_limit(1<<30)
def rd(d,pat):
    for f in sorted(glob.glob(d+'/'+pat)):
        seed=int(f.split('_s')[-1].split('.')[0])
        with open(f,encoding='utf-8') as fh:
            for r in csv.DictReader(fh,delimiter='\t'):
                r['seed']=seed; yield r
def fl(x):
    try: return float(x)
    except: return float('nan')
for tag,d in (('m412_ins','D:/PoB_work/keep_412/m412_ins'),('B','D:/PoB_work/dc/out_DB_10/ins')):
    trans=collections.Counter(); last={}; leg=collections.defaultdict(list)
    for r in rd(d,'scalars_s*.tsv'):
        k=(r['seed'],r['nation']); p=r['polityCode']
        if k in last and last[k]!=p: trans[r['nation']]+=1
        last[k]=p
        v=fl(r['legitimacy']) if 'legitimacy' in r else fl(r.get('legitimacyIndex'))
        if v==v: leg[r['nation']].append(v)
    go=0
    for r in rd(d,'secession_s*.tsv'):
        if r['goFlag']=='1': go+=1
    allv=[v for a in leg.values() for v in a]
    allv.sort()
    print(tag,'政体の移行 8国×10種:',sum(trans.values()),' 分離 goFlag=1 行:',go,' 正統性 最小 %.3f p5 %.3f 中央 %.3f'%(allv[0],allv[int(len(allv)*.05)],allv[len(allv)//2]))
    print('   最小(国別):',{n:round(min(a),3) for n,a in leg.items()})
