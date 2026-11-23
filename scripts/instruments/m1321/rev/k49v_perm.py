import itertools, statistics as st
d={"UK":([0.03,379.01,301.78,29.53,0.27,249.22,0.0,98.77,92.82,66.8],[73.7,1.15,7.01,1.03,105.44,2.71,0.01,5.51,37.62,22.5]),
   "FR":([26.75,0.38,54.76,0.23,4.34,196.14,305.51,3.82,2.34,278.11],[276.31,37.74,79.33,51.23,366.36,36.87,360.63,524.65,0.01,558.61])}
for k,(a,b) in d.items():
    allv=a+b; n=0; red=0; mw_obs=sum((x>y)+0.5*(x==y) for x in a for y in b); ext=0
    for idx in itertools.combinations(range(20),10):
        s=set(idx); A=[allv[i] for i in idx]; B=[allv[i] for i in range(20) if i not in s]
        ma,mb=st.median(A),st.median(B); n+=1
        if mb>=1:
            r=ma/mb
            if not (0.1<r<10): red+=1
        mw=sum((x>y)+0.5*(x==y) for x in A for y in B)
        if abs(mw-50)>=abs(mw_obs-50): ext+=1
    print(k,"perm false-red rate",red/n,"MW U",mw_obs,"two-sided p",ext/n)
