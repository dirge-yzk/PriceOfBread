"""(814) 型 A の 10 件の州の時系列（region_s・sbrreg_s・rule_s・fd_s）を 5 年ごとに並べる（読むだけ）。
使い方: python -I r814_ts.py <出力.tsv>"""
import sys, os, math, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
EV = [('base','D:/PoB_work/keep_813/base_ins',4,'フィリピン'),('base','D:/PoB_work/keep_813/base_ins',6,'バハマ諸島'),
      ('a11','D:/PoB_work/keep_797/a11',18,'内陸ベンガル'),('n1','D:/PoB_work/keep_797/n1',3,'トンブクトゥ'),
      ('n1','D:/PoB_work/keep_797/n1',5,'アデレード'),('n1','D:/PoB_work/keep_797/n1',5,'ダーウィン'),
      ('n2','D:/PoB_work/keep_797/n2',7,'アデレード'),('n2','D:/PoB_work/keep_797/n2',7,'アリススプリングス'),('n2','D:/PoB_work/keep_797/n2',7,'ホバート'),
      ('rdf1','D:/PoB_work/keep_813/rd806/out_r806f1_4_s14/ins',14,'フィリピン')]
def read(path, keycols, want):
    out = {}
    if not os.path.exists(path): return out
    with open(path, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k:i for i,k in enumerate(h)}
        if 'region' not in ix: return out
        for line in fh:
            a = line.rstrip('\n').split('\t')
            if len(a) < len(h) or a[ix['region']] not in want: continue
            out[(a[ix['region']], int(a[ix[keycols]]))] = {k: a[i] for k,i in ix.items()}
    return out
o = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
cols = ['set','seed','region','quarter','year','nation','tier','popK','ruralK','arable','landF','holding','wPrem','emp','food','earnPM','earnPC','factOut','extrOut','factN','extrN','sbrRev','sbrInCost','sbrVa','sbrRent','sbrWages','ruleRev','ruleGdp','ruleCut','fdSub','fdHarv','fdFillNew','fdLandF']
o.write('\t'.join(cols)+'\n')
bykey = collections.defaultdict(list)
for e in EV: bykey[(e[1], e[2])].append(e)
for (d, sd), lst in bykey.items():
    want = set(e[3] for e in lst)
    reg = read(os.path.join(d,'region_s%d.tsv'%sd),'quarter',want)
    sbr = read(os.path.join(d,'sbrreg_s%d.tsv'%sd),'quarter',want)
    rul = read(os.path.join(d,'rule_s%d.tsv'%sd),'year',want)
    fdd = read(os.path.join(d,'fd_s%d.tsv'%sd),'quarter',want)
    for e in lst:
        for q in range(0, 668, 20):
            r = reg.get((e[3], q))
            if r is None: continue
            s = sbr.get((e[3], q), {}); ru = rul.get((e[3], int(r['year'])), {}); f = fdd.get((e[3], q), {})
            pop = fl(r['populationK']); earn = fl(r['earnedPerMonth'])
            row = [e[0], sd, e[3], q, r['year'], r['nation'], r['tier'], r['populationK'], r['ruralPopulationK'], r['arableLand'], r['landFactor'], r['holdingFactor'], r['wagePremium'], r['employmentShare'], r['foodFillShare'],
                   r['earnedPerMonth'], (earn/pop if pop>0 else 'nan'), r['factoryOutputRefValuePerMonth'], r['extractionOutputRefValuePerMonth'], r.get('factoryActiveCount',''), r.get('extractionActiveCount',''),
                   s.get('rev',''), s.get('inCost',''), s.get('va',''), s.get('rent',''), s.get('wages',''), ru.get('revenuePerMonth',''), ru.get('gdpPerMonth',''), ru.get('ruleCutPerMonth',''),
                   f.get('subPerMonth',''), f.get('harvestMean',''), f.get('fillNew',''), f.get('landFactor','')]
            def g(v):
                if isinstance(v, float): return format(v, '.4g')
                try: return format(float(v), '.4g') if v not in ('',) and not isinstance(v,int) else str(v)
                except: return str(v)
            o.write('\t'.join(g(v) for v in row)+'\n')
o.close(); print('書いた', sys.argv[1])
