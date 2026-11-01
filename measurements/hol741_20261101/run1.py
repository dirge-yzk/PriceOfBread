import sys; sys.path.insert(0,'D:/PoB_work/hol52_work/py')
from rw import *
base='D:/PoB_work/keep_736'
for arm in ['base_ins','n1','n2','a11']:
    d=f'{base}/{arm}'
    seeds=sorted(int(f[11:-4]) for f in os.listdir(d) if f.startswith('migration_s'))
    res={v:([],[],[]) for v in VN}
    for sd in seeds:
        o,miss=predict(load_mig(d,sd))
        for v in VN:
            res[v][0].append(o[v][1]/o[v][0]); res[v][1].append(o[v][1]); res[v][2].append(o[v][0])
    print('##',arm,'seeds',len(seeds))
    for v in VN:
        print(f'{v}\tfarShareMed {st.median(res[v][0]):.3f}\tfarVolMed {st.median(res[v][1]):.0f}\ttotMed {st.median(res[v][2]):.0f}\ttotRatio {st.median(res[v][2])/st.median(res["gc"][2]):.3f}')
