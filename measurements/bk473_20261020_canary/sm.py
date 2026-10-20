import sys,statistics as st
sys.stdout.reconfigure(encoding='utf-8')
exec(open('D:/PoB_work/bk473/an.py',encoding='utf-8').read().split("mode=sys.argv[1]")[0])
def row(d,q,c):
    v=[d[n][q][c] for n in MAIN if n in d and q in d[n]]; v=[a for a in v if a==a]
    return f'{st.median(v):.3g} [{min(v):.3g}–{max(v):.3g}]'
for fn,qs in (('A',[4,20]),('B1',[20,60,100,120,200]),('C1',[20,60,100,120,200]),('D2',[20,60,100,120,200])):
    d=load(fn+'.tsv'); print('==',fn)
    for q in qs:
        print(' q',q,'dailyPx',row(d,q,'dailyPx'),'| short',row(d,q,'dailyShortRatio'),'| shortMkt',row(d,q,'dailyShortMktFrac'),'| spend/earn',row(d,q,'peasantSpendOverEarn'),'| peasSavMonths',row(d,q,'peasantSavMonths'),'| capSavMonths',row(d,q,'capitalistSavMonths'),'| worker',row(d,q,'workerShare'))
        if 'イギリス' in d: print('     England bankrupt',d['イギリス'][q]['bankruptN'],'/',d['イギリス'][q]['buyersN'],' cap/q1 world',)
    dd=d
    cs=[(dd[n][120]['capitalistSavTotal']/dd[n][0]['capitalistSavTotal']) for n in MAIN if 120 in dd[n] and dd[n][0]['capitalistSavTotal']>0]
    print(' capSav q120/q0 main8',[f'{a:.2f}' for a in cs])
