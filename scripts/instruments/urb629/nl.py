import sys
d=sys.argv[1]
print('--fd_s ネーデルラント')
h=None
for line in open(d+'/fd_s7.tsv',encoding='utf-8'):
    c=line.rstrip('\n').split('\t')
    if h is None: h=c; ix={k:i for i,k in enumerate(h)}; continue
    if c[ix['nation']]=='ネーデルラント' and c[ix['region']]=='アムステルダム' and int(c[0])%40==0:
        print(c[1],'pop%.0f need%.0f mkt%.0f dem%.0f sub%.0f fillOld%.2f fillNew%.2f harv%.2f land%s'%(float(c[ix['popK']]),float(c[ix['stapleNeedPerMonth']]),float(c[ix['marketNeedPerMonth']]),float(c[ix['demandPerMonth']]),float(c[ix['subPerMonth']]),float(c[ix['fillOld']]),float(c[ix['fillNew']]),float(c[ix['harvestMean']]),c[ix['landFactor']]))
print('--pop_s (N, ネーデルラント)')
h=None
for line in open(d+'/pop_s7.tsv',encoding='utf-8'):
    c=line.rstrip('\n').split('\t')
    if h is None: h=c; ix={k:i for i,k in enumerate(h)}; continue
    if c[0]=='N' and c[ix['nation']]=='ネーデルラント' and int(c[ix['quarter']])%40==0:
        print(c[ix['year']],'pop%.0f births%.1f dNorm%.1f dFamine%.1f war%s cbr%.1f cdr%.1f'%(float(c[ix['popK']]),float(c[ix['birthsK']]),float(c[ix['deathsNormalK']]),float(c[ix['deathsFamineK']]),c[ix['warDeadK']],float(c[ix['cbrPerMille']]),float(c[ix['cdrPerMille']])))
