# (1326) 問い3：内戦の勝敗を rulepeace_s（講和の勝者・敗者）と imp_s（政体の押し付け）から名前で引く（wars_s の sideA＝親・sideB＝反乱国）
import sys,os,csv,collections,statistics as st
for a in sys.argv[1:]:
    name,d=a.split('=',1); c=collections.Counter(); dead=collections.defaultdict(float); mo=collections.defaultdict(list)
    for f in sorted(os.listdir(d)):
        if not (f.startswith('wars_s') and f.endswith('.tsv')): continue
        s=f[6:-4]; peace=collections.defaultdict(list)
        p=os.path.join(d,f'rulepeace_s{s}.tsv')
        for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'): peace[frozenset((r['winner'],r['loser']))].append(r)
        for r in csv.DictReader(open(os.path.join(d,f),encoding='utf-8'),delimiter='\t'):
            if r['civilWarFlag']!='1': continue
            l=[x for x in peace.get(frozenset((r['sideA'],r['sideB'])),[]) if int(x['year'])>=int(r['startYear'])]
            k='引けない' if not l else ('反乱の勝ち' if l[0]['winner']==r['sideB'] else '親の勝ち')
            c[k]+=1; dead[k]+=float(r['deadK']); mo[k].append(float(r['months']))
    print(name,'\t'.join(f'{k} {c[k]} 件・死者 {dead[k]:.0f}・長さの中央 {st.median(mo[k]):.0f}' for k in sorted(c)))
