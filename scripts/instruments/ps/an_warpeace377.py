"""(377) 合流の読み：交戦中と平時の闘争度の差（国×種ごとに atWar=1 と 0 の行の militancyMean の平均の差・各 8 行以上）を腕ごとに。入力＝枝 ps の腕の scalars_s（D:/PoB_work/ps/keep/out_<腕>_10・採否の後に消した＝再走は枝 ps の 5ce34151 の木で腕を回し直す）。出力の写し＝measurements/m377_20261016/warpeace.txt"""
import sys,glob,statistics as st,collections
for arm in ["A","N1","N2","BD","B","BCW"]:
    diffs=[];rat=[]
    for f in sorted(glob.glob(f"D:/PoB_work/ps/keep/out_{arm}_10/scalars_s*.tsv")):
        acc=collections.defaultdict(lambda:[[],[]])
        with open(f,encoding="utf-8") as fh:
            h=fh.readline().rstrip("\n").split("\t"); iw=h.index("atWar"); im=h.index("militancyMean"); inn=h.index("nation"); ib=h.index("born")
            for line in fh:
                r=line.rstrip("\n").split("\t")
                try: m=float(r[im])
                except: continue
                acc[r[inn]][1 if r[iw]=="1" else 0].append(m)
        for n,(p,w) in acc.items():
            if len(p)>=8 and len(w)>=8:
                a=st.mean(w)-st.mean(p); diffs.append(a)
                if st.mean(p)>0: rat.append(st.mean(w)/st.mean(p))
    print(arm, "pairs",len(diffs),"median(war-peace)=%.4f mean=%.4f  median ratio war/peace=%.3f  share>0=%.2f"%(st.median(diffs),st.mean(diffs),st.median(rat),sum(d>0 for d in diffs)/len(diffs)))
