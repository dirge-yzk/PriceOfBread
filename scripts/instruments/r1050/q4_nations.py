import csv,collections,sys
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
cpn=collections.Counter(); cpw=collections.Counter(); imp=collections.Counter(); wars=collections.Counter(); impm=collections.Counter()
imp_cp=0; imp_all=0; imp15=0; multi=collections.Counter()
pol0={}
for s in seeds:
    cp=rows(f'{K}/{d}/cpeace_s{s}.tsv'); cpk=set()
    for r in cp:
        cpn[r['loser']]+=1; cpw[r['loser']]+=int(r['nCollapse']); cpk.add((r['month'],r['loser']))
    im=rows(f'{K}/{d}/imp_s{s}.tsv'); mon=collections.Counter()
    for r in im:
        imp[r['loser']]+=1; imp_all+=1; imp_cp+=(r['month'],r['loser']) in cpk; imp15+=float(r['absScoreIndex'])>=1.4999
        mon[(r['month'],r['loser'])]+=1
    for (m,l),c in mon.items():
        if c>=2: multi[l]+=1
    for r in rows(f'{K}/{d}/wars_s{s}.tsv'):
        if r['civilWarFlag']=='0':
            wars[r['sideA']]+=1; wars[r['sideB']]+=1
    if s==seeds[0]:
        for r in rows(f'{K}/{d}/scalars_s{s}.tsv'):
            if r['quarter']=='0' or r['nation'] not in pol0: pol0.setdefault(r['nation'],r['polityCode'])
print(f'{arm}: imp {imp_all}・崩壊の講和と同じ月同じ敗者 {imp_cp}・abs>=1.4999 {imp15}')
print('国\t開幕の政体\t戦争の本数\t崩壊の講和(回)\t崩壊した戦争(本)\t矯正\t矯正÷崩壊\t同じ月に2回以上')
for n,_ in imp.most_common(14):
    print(f'{n}\t{pol0.get(n,"?")}\t{wars[n]}\t{cpn[n]}\t{cpw[n]}\t{imp[n]}\t{imp[n]/max(1,cpn[n]):.2f}\t{multi[n]}')
print('崩壊の講和の多い国（上位10）:',[(n,c,imp[n]) for n,c in cpn.most_common(10)])
print('戦争の本数の多い国（上位8）:',wars.most_common(8))
