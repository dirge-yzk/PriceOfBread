# 問い 3: 〈膨らんでから抜ける州〉の型を腕ごとに数える（読むだけ）
# 型＝ピーク÷開幕の人口 ≧ K（既定 3）かつ ピークの後の最小÷ピーク ＜ 1/4。開幕の人口 1 千人以上の州・ピーク 5 千人以上。
# 持ち主の移り＝開幕の持ち主とピークの時の持ち主が違う。名指しの州は型に当たらなくても最小÷ピークを並べる。
import sys,collections
K=float(sys.argv[1]); arms=sys.argv[2:]
WATCH=['フォートウィリアム','ニューヨーク','チャールストン','セントポール','バハマ諸島','バーミンガム']
for spec in arms:
    name,d,seeds=spec.split('|'); seeds=[int(x) for x in seeds.split(',')]
    hits=[]; watch=collections.defaultdict(list); n_seed=0
    for s in seeds:
        try: f=open(f'{d}/region_s{s}.tsv',encoding='utf-8')
        except FileNotFoundError: continue
        n_seed+=1
        h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
        T=collections.defaultdict(list)
        for line in f:
            a=line.split('\t')
            T[a[ix['region']]].append((int(a[0]),float(a[ix['populationK']]),a[ix['nation']],a[ix['tier']]))
        f.close()
        for r,L in T.items():
            L.sort(); p0=L[0][1]
            ip=max(range(len(L)),key=lambda i:L[i][1]); pk=L[ip][1]
            mn=min(x[1] for x in L[ip:]); im=min(range(ip,len(L)),key=lambda i:L[i][1])
            rr=mn/pk if pk>0 else 1
            if r in WATCH: watch[r].append(f's{s}:{pk/max(p0,1e-9):.1f}x→{rr:.2f}')
            if p0>=1 and pk>=5 and pk/p0>=K and rr<0.25:
                owners=[]; 
                for x in L:
                    if not owners or owners[-1]!=x[2]: owners.append(x[2])
                hits.append((s,r,L[0][3],p0,pk,1783+L[ip][0]//4,mn,1783+L[im][0]//4,L[0][2]!=L[ip][2],'→'.join(o[:4] for o in owners)))
    print(f'## {name}（{d}・種 {n_seed}）型の件数 {len(hits)}（持ち主の移りあり {sum(1 for h in hits if h[8])}・1/10 未満 {sum(1 for h in hits if h[6]/h[4]<0.1)}）')
    for h in hits: print(f'   s{h[0]} {h[1]}（{h[2]}）開幕 {h[3]:.1f} → ピーク {h[4]:.1f}（{h[5]}）×{h[4]/h[3]:.1f} → 最小 {h[6]:.2f}（{h[7]}）={h[6]/h[4]:.3f}  持ち主の移り {"あり" if h[8] else "なし"} [{h[9]}]')
    for r in WATCH: print(f'   名指し {r}: '+' '.join(watch[r]))
