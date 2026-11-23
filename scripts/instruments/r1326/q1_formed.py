# (1326) 問い1：FormedElsewhere の当たりを四半期の末の盤面で数える（近似）。
# q の FormationTier は名（nation_s の displayName）の規則の段から読む（名を変える書き手は Formations.Check の RenameNation の 1 か所で、段を同時に入れる）。
# マラーター帝国だけ段が 2 か 3 のどちらか＝名から段が決まらない＝両方を数える。
import csv,sys,collections
R=sys.argv[1]; out=open(sys.argv[2],'w',encoding='utf-8')
MAR={'マラーター帝国':(2,3),'マラーター王国':(2,2),'バーラト':(3,3)}
KEY={'ドイツ':{'神聖ローマ':3,'ドイツ':2,'ドイツ帝国':2,'神聖ローマ帝国':3},'イタリア':{'イタリア':2,'イタリア王国':2,'ローマ帝国':3},'北ドイツ連邦':{'北ドイツ連邦':1}}
tot=collections.Counter()
for s in range(1,11):
    disp=collections.defaultdict(dict); first={}; marq=collections.Counter()
    with open(f'{R}/nation_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            q=int(r['quarter']); b=r['nation']; d=r['displayName']; disp[q][b]=d
            if d in MAR:
                marq[d]+=1
                first.setdefault(d,(r['year'],b))
    cand=hit=0; hitq=collections.Counter()
    with open(f'{R}/formation2_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['year']=='year': continue
            rt=int(r['ruleTier']);
            if rt<=int(r['formationTier']): continue
            cand+=1; q=int(r['q']); p=r['nation']; nm=KEY[r['rule'] if r['rule']!='北ドイツ連邦' else '北ドイツ連邦']
            h=any(b!=p and d in nm and nm[d]>=rt for b,d in disp.get(q,{}).items())
            hit+=h
            if h: hitq[r['rule']]+=1
    out.write(f's{s}\tマラーター系の名の国×四半期={dict(marq)}\t最初={first}\t候補の行(V1)={cand}\tFormedElsewhere の当たり={hit} {dict(hitq)}\n')
    tot['cand']+=cand; tot['hit']+=hit; tot['mar_seeds']+= (1 if marq else 0)
out.write(f'計\t{dict(tot)}\n')
