import sys,collections
sys.argv=['g','D:/PoB_work/keep_675/m675_ins','10','1']
src=open('D:/PoB_work/wt_navf4/scripts/instruments/navf679/g_lines.py',encoding='utf-8').read()
g={'__name__':'gl'}
exec(compile(src,'gl','exec'),g)
for nat in ['イギリス','フランス','ロシア']:
    for s in [1,2,3]:
        fs=g['fleet_series'](s)[nat]
        pk=0; ev=None
        for (q,y,f) in fs:
            pk=max(pk,f)
            if ev is None and pk>0 and f<0.5*pk and y<=1815: ev=(y,q,round(f),round(pk))
        samp=[(y,round(f)) for (q,y,f) in fs if q%20==0][:12]
        print(nat,s,'event',ev,samp)
