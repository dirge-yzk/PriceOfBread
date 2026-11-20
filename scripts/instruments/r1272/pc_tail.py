# (1272) al で比が 0.4 未満・基準で 0.4 以上の州×種（植民地格）について、al のピーク→最小の窓の出入りを両腕で積む（popclose_s）
# 引数: al の生 基準の生 al の dist 基準の dist 種の始め 種の終わり 出力
import sys,csv,collections
da={(r['seed'],r['region']):r for r in csv.DictReader(open(sys.argv[3],encoding='utf-8'),delimiter='\t')}
db={(r['seed'],r['region']):r for r in csv.DictReader(open(sys.argv[4],encoding='utf-8'),delimiter='\t')}
EXC={'イカルイト','チャーチル','ナイン','ヌーク','バロー','フォートシンプソン','フォートユーコン'}
sel={k:v for k,v in da.items() if v['tierLast']=='Colonial' and k[1] not in EXC and float(v['ratio'])<0.4 and k in db and float(db[k]['ratio'])>=0.4}
K=['birthsK','deathsNormalK','deathsFamineK','mig','cb','gar','spe','merge']
def run(dr,s):
    w={k[1]:(int(v['peakYear']),int(v['minYear'])) for k,v in sel.items() if k[0]==str(s)}
    res={r:collections.defaultdict(float) for r in w}
    if not w: return res
    with open(f'{dr}/popclose_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        for line in f:
            c=line.rstrip('\n').split('\t'); r=c[ix['region']]
            if r not in w: continue
            y=int(c[ix['year']])
            if not (w[r][0]<=y<=w[r][1]): continue
            g=lambda k:float(c[ix[k]]); a=res[r]
            a['birthsK']+=g('birthsK'); a['deathsNormalK']+=g('deathsNormalK'); a['deathsFamineK']+=g('deathsFamineK')
            a['mig']+=g('migrationInK')-g('migrationOutK'); a['cb']+=g('crossBorderInK')-g('crossBorderOutK')
            a['gar']+=g('garrisonInK')-g('garrisonOutK'); a['spe']+=g('specialistInK')-g('specialistOutK'); a['merge']+=g('colonyMergeInK')
            a['pop']+=g('prevPopulationK')/4
    return res
o=open(sys.argv[7],'w',encoding='utf-8',newline='\n')
o.write('seed\tregion\tnation\tratioAl\tratioBase\twindow\tarm\tpopYearK\t'+'\t'.join(k+'PerMille' for k in K)+'\n')
tot={'al':collections.defaultdict(float),'base':collections.defaultdict(float)}
for s in range(int(sys.argv[5]),int(sys.argv[6])+1):
    A=run(sys.argv[1],s); B=run(sys.argv[2],s)
    for r in A:
        v=sel[(str(s),r)]
        for arm,R in (('al',A[r]),('base',B[r])):
            py=R['pop']
            for k in K+['pop']: tot[arm][k]+=R[k]
            o.write(f"{s}\t{r}\t{v['nationLast']}\t{v['ratio']}\t{db[(str(s),r)]['ratio']}\t{v['peakYear']}-{v['minYear']}\t{arm}\t{py:.1f}\t"+'\t'.join(f"{1000*R[k]/py:.2f}" if py>0 else 'nan' for k in K)+'\n')
o.close()
print('対の数',len(sel))
for arm in ('al','base'):
    t=tot[arm]; py=t['pop']; print(arm,'窓の合計（千人年あたり）',' '.join(f"{k}:{1000*t[k]/py:.2f}" for k in K))
