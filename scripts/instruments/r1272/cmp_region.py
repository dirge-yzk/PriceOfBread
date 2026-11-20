# (1272) 州の region_s を 2 本の腕で四半期ごとに並べる。引数: al の表 基準の表 出力
import sys,csv
def rd(p):
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.DictReader(f,delimiter='\t'); return [row for row in r]
A=rd(sys.argv[1]); B=rd(sys.argv[2])
cols=['populationK','ruralPopulationK','foodFillShare','opportunityIndex','holdingFactor','wageFactor','employmentShare','pushFactor','wagePremium','migrationOutKPerQuarter','migrationInKPerQuarter','crossBorderOutKPerQuarter','earnedPerMonth','factoryOutputRefValuePerMonth','extractionOutputRefValuePerMonth','factoryActiveCount','extractionActiveCount','warIntensity','nation','tier']
first={}
out=open(sys.argv[3],'w',encoding='utf-8',newline='\n')
out.write('quarter\tyear\t'+'\t'.join(c+'_al\t'+c+'_b' for c in cols)+'\n')
for a,b in zip(A,B):
    assert a['quarter']==b['quarter']
    for c in cols:
        if a[c]!=b[c] and c not in first: first[c]=(a['quarter'],a['year'],a[c],b[c])
    q=int(a['quarter'])
    out.write(a['quarter']+'\t'+a['year']+'\t'+'\t'.join(a[c]+'\t'+b[c] for c in cols)+'\n')
out.close()
for c,v in sorted(first.items(),key=lambda x:int(x[1][0])): print('first diff',c,v)
