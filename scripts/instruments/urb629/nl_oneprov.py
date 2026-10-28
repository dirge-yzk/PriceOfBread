import csv,sys,collections,statistics
arms={'m627':'D:/PoB_work/keep_627/m627_ins','n1':'D:/PoB_work/keep_627/n1','n2':'D:/PoB_work/keep_627/n2'}
NAT='ネーデルラント'
for a,d in arms.items():
    print('==',a)
    for s in range(1,11):
        by=collections.defaultdict(list)
        try: f=open(f'{d}/region_s{s}.tsv',encoding='utf-8')
        except: print('missing',s); continue
        for row in csv.DictReader(f,delimiter='\t'):
            if row['nation']!=NAT or row['activeFlag']!='1': continue
            by[int(row['quarter'])].append((row['region'],row['tier'],float(row['populationK']),float(row['ruralPopulationK'])))
        qs=sorted(by)
        one=None; ident=None
        ams={}
        for q in qs:
            rs=by[q]
            tot=sum(r[2] for r in rs); urb=sum(r[2]-r[3] for r in rs)
            nat=urb/tot if tot>0 else 0
            mx=max(((r[2]-r[3])/r[2] for r in rs if r[2]>1e-9),default=0)
            if one is None and len(rs)==1: one=q
            if ident is None and abs(nat-mx)<1e-3: ident=q
            for r in rs:
                if r[0]=='アムステルダム': ams[q]=r[2]
        if not ams: print(s,'no ams'); continue
        peak=max(ams.values()); minq=min(ams,key=ams.get)
        at=lambda q: ams.get(q,float('nan'))
        # decline after identity quarter
        def pop_at(q): 
            k=min(ams,key=lambda x:abs(x-q)); return ams[k]
        print(f's{s} oneProvQ={one}(yr{1783+(one or 0)//4}) natEqMaxQ={ident} ams q0={at(0):.0f} q{ident}={pop_at(ident) if ident is not None else float("nan"):.0f} min={ams[minq]:.0f}@q{minq} end={ams[max(ams)]:.0f} min/peak={ams[minq]/peak:.3f}')
