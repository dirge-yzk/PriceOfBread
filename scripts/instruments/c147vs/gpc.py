import sys,glob,os
sys.stdout.reconfigure(encoding='utf-8')  # (1384) 記録の読み：gdpr＝名目 GDP の 1 四半期の比 10 倍以上（建国の 4 四半期と人口の比 10 倍の痩せを除く）・gpc＝1 人あたり名目 GDP＜0.1 の国×四半期
for d in sys.argv[1:]:
    low=[]
    for p in sorted(glob.glob(os.path.join(d,'nation_s*.tsv'))):
        s=os.path.basename(p)[8:-4]
        with open(p,encoding='utf-8') as f:
            h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
            for line in f:
                a=line.rstrip('\n').split('\t')
                try: g=float(a[ix['nominalGdpPerYear']]); pop=float(a[ix['populationK']]); y=int(a[ix['year']])
                except: continue
                if pop>=100 and y>=1800 and g>0 and g/pop<0.1: low.append((s,a[ix['nation']]))
    from collections import Counter
    c=Counter(low); print(d[-12:], '1 人あたり名目 GDP＜0.1（人口 10 万以上・1800 年以後）の国×四半期', len(low), c.most_common(4))
