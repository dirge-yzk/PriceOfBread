import csv,collections,sys,io
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8')
D='D:/PoB_work/keep_655/m655_ins/'
th=[0,0.01,0.05,0.10,0.12,0.14,0.25,0.34]
cnt=collections.Counter(); n=0
for s in range(1,11):
    for r in csv.DictReader(open(D+'ideology_s%d.tsv'%s,encoding='utf-8',errors='replace'),delimiter='\t'):
        n+=1; v=float(r['ideoNationalCommunismShare'])
        for t in th:
            if v>t: cnt[t]+=1
print('rows',n,{t:cnt[t] for t in th})
# Russia per seed
for s in range(1,11):
    fs=fc=None; mc=(0,None); pol=collections.Counter(); nr=0
    for r in csv.DictReader(open(D+'idn_s%d.tsv'%s,encoding='utf-8',errors='replace'),delimiter='\t'):
        if r['nation']=='ロシア':
            y=int(r['year'])
            if fs is None and float(r['knownSoc'])>0: fs=y
            if fc is None and float(r['knownCom'])>0: fc=y
    for r in csv.DictReader(open(D+'ideology_s%d.tsv'%s,encoding='utf-8',errors='replace'),delimiter='\t'):
        if r['nation']=='ロシア':
            v=float(r['ideoNationalCommunismShare'])
            if v>mc[0]: mc=(round(v,3),r['year'])
    for r in csv.DictReader(open(D+'reform_s%d.tsv'%s,encoding='utf-8',errors='replace'),delimiter='\t'):
        if r['nation']=='ロシア': pol[r['polityCode']]+=1
    print('seed',s,'ロシア knownSoc',fs,'knownCom',fc,'max com share',mc,'polity quarters',dict(pol))
