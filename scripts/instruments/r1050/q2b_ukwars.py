import csv,collections,sys
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
for N in ['イギリス','フランス','ロシア','オーストリア']:
    ent=collections.Counter(); side=collections.Counter(); opp=collections.Counter(); mon=[]; dec=collections.Counter()
    for s in seeds:
        for r in rows(f'{K}/{d}/wars_s{s}.tsv'):
            if r['civilWarFlag']!='0': continue
            if N in (r['sideA'],r['sideB']):
                a = 'A' if r['sideA']==N else 'B'
                ent[(a,r['entryCode'])]+=1; opp[r['sideB'] if a=='A' else r['sideA']]+=1; mon.append(int(r['months'])); dec[r['startYear'][:3]+'0s']+=1
    mon.sort()
    print(f'== {arm} {N}: 本数 {len(mon)} 月数の中央 {mon[len(mon)//2] if mon else 0} 合計月 {sum(mon)}')
    print('  (側,入口):',ent.most_common(10))
    print('  相手:',opp.most_common(10))
    if N=='イギリス': print('  年代:',sorted(dec.items()))
