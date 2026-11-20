# (1272) popclose_s から 国×格の州の人口の出入りを 20 年ごとに積む（種を足し合わせる）・千人年あたり
# 引数: 生のフォルダ 種の始め 種の終わり 国 格 出力
import sys,collections
d,s0,s1,nat,tier,out=sys.argv[1],int(sys.argv[2]),int(sys.argv[3]),sys.argv[4],sys.argv[5],sys.argv[6]
K=['birthsK','deathsNormalK','deathsFamineK','migNet','cbNet','garNet','speNet','mergeIn','delta']
acc=collections.defaultdict(lambda:collections.defaultdict(float))
for s in range(s0,s1+1):
    with open(f'{d}/popclose_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        for line in f:
            c=line.rstrip('\n').split('\t')
            if c[ix['nation']]!=nat or c[ix['tier']]!=tier: continue
            g=lambda k:float(c[ix[k]])
            per=int(c[ix['year']])//20*20; a=acc[per]
            a['popQ']+=g('prevPopulationK')
            a['birthsK']+=g('birthsK'); a['deathsNormalK']+=g('deathsNormalK'); a['deathsFamineK']+=g('deathsFamineK')
            a['migNet']+=g('migrationInK')-g('migrationOutK'); a['cbNet']+=g('crossBorderInK')-g('crossBorderOutK')
            a['garNet']+=g('garrisonInK')-g('garrisonOutK'); a['speNet']+=g('specialistInK')-g('specialistOutK')
            a['mergeIn']+=g('colonyMergeInK'); a['delta']+=g('deltaPopulationK')
o=open(out,'w',encoding='utf-8',newline='\n')
o.write('period\tpopYearK\t'+'\t'.join(k+'PerMille' for k in K)+'\n')
for per in sorted(acc):
    a=acc[per]; py=a['popQ']/4
    o.write(f"{per}\t{py:.1f}\t"+'\t'.join(f"{1000*a[k]/py:.2f}" if py>0 else 'nan' for k in K)+'\n')
o.close()
