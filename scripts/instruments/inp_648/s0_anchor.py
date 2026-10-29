import csv,collections,statistics,sys,os
sys.stdout.reconfigure(encoding='utf-8')
N9=['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本','アメリカ']
eras=[(1783,1829),(1830,1869),(1870,1909),(1910,1950)]
def load(root,seeds):
    out=collections.defaultdict(lambda: collections.defaultdict(list))  # (era,nation)-> metric -> per-seed means
    for s in seeds:
        p=f'{root}/factory_s{s}.tsv'
        if not os.path.exists(p): continue
        tmp=collections.defaultdict(lambda: collections.defaultdict(list))
        with open(p,encoding='utf-8') as f:
            for row in csv.DictReader(f,delimiter='\t'):
                nat=row['nation']; y=int(row['year'])
                if nat not in N9: continue
                des=float(row['desiredRefValuePerMonth'])
                if des<=0: continue
                e=[i for i,(a,b) in enumerate(eras) if a<=y<=b]
                if not e: continue
                e=e[0]
                tmp[(e,nat)]['fill'].append(float(row['inputFillShare']))
                tmp[(e,nat)]['oth'].append(float(row['otherBoundLossRefValuePerMonth'])/des)
                tmp[(e,nat)]['lab'].append(float(row['laborBoundLossRefValuePerMonth'])/des)
        for k,v in tmp.items():
            for m,l in v.items(): out[k][m].append(sum(l)/len(l))
    return out
R='D:/PoB_work/keep_645'
res={n:load(f'{R}/{d}',range(1,11)) for n,d in (('m645','m645_ins'),('N1','n1'),('N2','n2'))}
print('入力充足 inputFillShare (種の中央値; 括弧=N1,N2) / 労働以外の失い otherLoss')
for e,(a,b) in enumerate(eras):
    print(f'--- {a}-{b}')
    for nat in N9:
        k=(e,nat)
        row=[]
        for n in ('m645','N1','N2'):
            v=res[n].get(k,{})
            row.append((statistics.median(v['fill']) if v.get('fill') else float('nan'), statistics.median(v['oth']) if v.get('oth') else float('nan'), len(v.get('fill',[]))))
        print(f'{nat:6s} fill {row[0][0]:.3f} (N1 {row[1][0]:.3f}, N2 {row[2][0]:.3f})  otherLoss {row[0][1]:.3f} (N1 {row[1][1]:.3f}, N2 {row[2][1]:.3f}) nseeds {row[0][2]}/{row[1][2]}/{row[2][2]}')
