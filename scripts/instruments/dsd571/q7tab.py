import csv,statistics as st,collections
d='D:/PoB_work/keep_560/m560_ins'
MAIN=['イギリス','フランス','プロイセン','オーストリア','ロシア','アメリカ','清','日本']
ROWS=['蒸気機関','鋳鉄の骨組み','ルブラン法','甜菜糖','ガス灯','汽船','機関車','転炉鋼','石炭火力','電動機','送電網']
A=collections.defaultdict(list); N=collections.defaultdict(list); T=collections.defaultdict(list)
passi=collections.Counter(); passck=collections.Counter(); tot=collections.Counter()
for s in range(1,11):
    with open(f'{d}/engineer_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['kind']!='T' or r['quarter']!='7' or r['nation'] not in MAIN: continue
            a=float(r['engineerPerMonth']); nd=float(r['needTerm']); te=float(r['techTerm'])
            k=(r['row'],r['nation']); A[k].append(a); N[k].append(nd); T[k].append(te)
            if r['knownFlag']=='0' and int(r['decade'])>=1800:
                tot[s]+=1
                C=a/(a+12.5677) if a>0 else 0
                if 70*C+nd>=100: passck[s]+=1
                if C*nd/70>=0.5102: passi[s]+=1
print('row\t'+'\t'.join(MAIN))
for row in ROWS:
    print(row+'\t'+'\t'.join(f"{st.median(A[(row,n)]):.0f}/{st.median(T[(row,n)]):.0f}/{st.median(N[(row,n)]):.0f}" if A[(row,n)] else '-' for n in MAIN))
print('d>=2 未発見の主要国×行（1785）: 時計なし和100を越える', dict(passck), ' 形(i)積0.51を越える', dict(passi), ' 総数', dict(tot))
