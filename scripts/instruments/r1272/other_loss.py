# (1272) 州の人口の四半期の差から州の間と越境の出入りを引いた残り（自然の増減・飢え・兵の移し）を 5 年ごとに積む
# 引数: al の region 抽出 基準の region 抽出 al の cpop 抽出 基準の cpop 抽出
import sys,csv,collections
def reg(p):
    return [r for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t')]
def sol(p):
    d=collections.defaultdict(float)
    for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'):
        if r['className']=='兵士': d[int(r['quarter'])]+=float(r['popK'])
    return d
for nm,rp,cp in (('al',sys.argv[1],sys.argv[3]),('base',sys.argv[2],sys.argv[4])):
    R=reg(rp); S=sol(cp); acc=collections.defaultdict(lambda:[0.0,0.0,0.0,0.0])
    for a,b in zip(R,R[1:]):
        dp=float(b['populationK'])-float(a['populationK'])
        mig=float(b['migrationInKPerQuarter'])-float(b['migrationOutKPerQuarter'])+float(b['crossBorderInKPerQuarter'])-float(b['crossBorderOutKPerQuarter'])
        y=int(b['year']); w=(y-1783)//5*5+1783
        acc[w][0]+=dp; acc[w][1]+=mig; acc[w][2]+=dp-mig; acc[w][3]=max(acc[w][3],float(b['warIntensity']))
    print(nm,'5年の窓: 人口の差 / 移民の純 / 残り / 戦場の強さの最大 / 窓の終わりの兵士')
    print(' '.join(f"{w}:{v[0]:+.2f}/{v[1]:+.2f}/{v[2]:+.2f}/{v[3]:.2f}/{S.get((w+4-1783)*4+3,0):.2f}" for w,v in sorted(acc.items()) if 1848<=w<=1903))
