import csv,sys,collections,os
K='D:/PoB_work/keep_1044'
ARMS=[('base','base_ins',range(1,11)),('n1','n1',range(1,11)),('n2','n2',range(1,11)),('a11','a11',range(11,21))]
PN={0:'絶対',1:'専制',2:'幕府',3:'立憲',4:'共和',5:'ブ独',6:'プ独',7:'軍独',8:'無政府'}
def rows(p):
    with open(p,encoding='utf-8') as f:
        return list(csv.DictReader(f,delimiter='\t'))
for name,d,seeds in ARMS:
    byn=collections.Counter(); seedsn=collections.defaultdict(set); tot=0
    crown2=collections.Counter(); 
    for s in seeds:
        for r in rows(f'{K}/{d}/imp_s{s}.tsv'):
            byn[r['loser']]+=1; seedsn[r['loser']].add(s); tot+=1
        p=f'{K}/{d}/crownpc_s{s}.tsv'
        if os.path.exists(p):
            for r in rows(p):
                if r.get('regimeEventFlag')=='1' and r.get('crownRegisterSourceCode')=='2': crown2[r['nation']]+=1
    print(f'== {name}: imp 行 {tot}  crownpc(事象=1&符号2) {sum(crown2.values())}')
    for n,c in byn.most_common(12):
        print(f'  {n}\t{c}\t種{len(seedsn[n])}\tcrown2={crown2.get(n,0)}')
