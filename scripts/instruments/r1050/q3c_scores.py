import csv,collections,sys
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
b=collections.Counter(); ukb=collections.Counter(); cpm=collections.Counter()
for s in seeds:
    cp={(r['year'],r['loser']) for r in rows(f'{K}/{d}/cpeace_s{s}.tsv')}
    for r in rows(f'{K}/{d}/cessp_s{s}.tsv'):
        a=float(r['absScoreIndex'])
        k='<1.0' if a<1.0 else '1.0-1.2' if a<1.2 else '1.2-1.4' if a<1.4 else '1.4-1.4999' if a<1.4999 else '1.5'
        b[k]+=1
        if r['loser']=='イギリス': ukb[k]+=1
        if a>=1.4: cpm[(r['year'],r['loser']) in cp]+=1
print(arm,'全ての講和の |戦勝点| の帯:',sorted(b.items()),' 英が敗者:',sorted(ukb.items()),' ≥1.4 が同じ年・同じ敗者の崩壊の講和にある:',dict(cpm))
