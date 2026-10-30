import sys,importlib.util,collections
dirs={'m655':'D:/PoB_work/keep_655/m655_ins','N1':'D:/PoB_work/keep_655/n1','N2':'D:/PoB_work/keep_655/n2'}
for lab,d in dirs.items():
    sys.argv=['g','%s'%d,'10','1']
    spec=importlib.util.spec_from_file_location('gl','D:/PoB_work/wt_navf3/scripts/instruments/navf606/g_lines.py')
    m=importlib.util.module_from_spec(spec)
    src=open('D:/PoB_work/wt_navf3/scripts/instruments/navf606/g_lines.py',encoding='utf-8').read()
    # main を走らせない
    src=src.replace("\nmain()","\n#main()")
    exec(compile(src,'gl','exec'),m.__dict__)
    per=collections.defaultdict(lambda:[0,0]); tot=[0,0]
    peaks=collections.defaultdict(list)
    for s in m.SEEDS:
        fs=m.fleet_series(s)
        if not fs: continue
        for nat in m.SEVEN:
            if nat not in fs: continue
            o,t=m.halving_events({nat:fs[nat]},8)
            per[nat][0]+=o; per[nat][1]+=t
    print(lab,{n:tuple(v) for n,v in per.items()}, 'sum', sum(v[0] for v in per.values()),sum(v[1] for v in per.values()))
