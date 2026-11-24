# 問い 4 の続き: 越境の流出を出どころの食の帯で分ける（region の crossBorderOutKPerQuarter）
import sys,collections
B=lambda f:'<0.5' if f<0.5 else '0.5-0.9' if f<0.9 else '0.9-0.99' if f<0.99 else '>=0.99'
for spec in sys.argv[1:]:
    name,d=spec.split('|'); C=collections.Counter(); D=collections.Counter()
    for s in range(1,11):
        with open(f'{d}/region_s{s}.tsv',encoding='utf-8') as f:
            h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
            for line in f:
                a=line.split('\t'); b=B(float(a[ix['foodFillShare']]))
                C[b]+=float(a[ix['crossBorderOutKPerQuarter']]); D[b]+=float(a[ix['migrationOutKPerQuarter']])
    print(f'{name}\t越境の流出 '+' '.join(f'{k}:{C[k]:.0f}' for k in ['<0.5','0.5-0.9','0.9-0.99','>=0.99'])+f' 和 {sum(C.values()):.0f}\t国内の流出 '+' '.join(f'{k}:{D[k]:.0f}' for k in ['<0.5','0.5-0.9','0.9-0.99','>=0.99'])+f' 和 {sum(D.values()):.0f}')
