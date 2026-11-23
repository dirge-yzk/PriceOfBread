# (1326) 問い2：1 種 1 年の州ごとに 電灯網（fill・cap・scale・profit）と 送電網・電力・電化製品の作り手の産出を並べる
import csv,sys,collections
R,s,Y=sys.argv[1],sys.argv[2],sys.argv[3]; out=open(sys.argv[4],'w',encoding='utf-8')
rows=collections.defaultdict(dict); lq='0'
with open(f'{R}/prodcap_s{s}.tsv',encoding='utf-8') as f:
    for r in csv.DictReader(f,delimiter='\t'):
        if r['year']!=Y: continue
        lq=max(lq,r['q'])
        rows[(r['q'],r['region'])][(r['good'],r['recipe'])]=r
cnt=collections.Counter()
out.write('region\towner\tEL_fill\tEL_cap\tEL_scale\tEL_profit\tgrid_out\tgrid_cap\telec_out\tappl_out\tgas_out\tGL_fill\tGL_cap\n')
for (q,reg),d in rows.items():
    if q!=lq: continue
    e=d.get(('電化インフラ','電灯網')); g=d.get(('送電網','(既定)')); gl=d.get(('電化インフラ','ガス灯'))
    el=sum(float(d[k]['output'] or 0) for k in d if k[0]=='電力'); ap=d.get(('電化製品','(既定)')); gs=d.get(('ガス','(既定)'))
    if not e: continue
    f=lambda r,c: (r[c] if r else '')
    out.write('\t'.join([reg,e['ownerNation'],e['lastInputFill'],e['capacity'],e['scale'],e['profitFactor'],f(g,'output'),f(g,'capacity'),f'{el:.4g}',f(ap,'output'),f(gs,'output'),f(gl,'lastInputFill'),f(gl,'capacity')])+'\n')
    cnt['州']+=1; cnt['EL fill>0']+= float(e['lastInputFill'] or 0)>0; cnt['送電網 out>0']+= (g is not None and float(g['output'] or 0)>0)
    cnt['両方']+= float(e['lastInputFill'] or 0)>0 and (g is not None and float(g['output'] or 0)>0)
out.write(f'# {dict(cnt)}\n')
