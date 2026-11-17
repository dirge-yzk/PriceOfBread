M=(1<<64)-1
def sm(x):
    x=(x+0x9E3779B97F4A7C15)&M; z=x
    z=((z^(z>>30))*0xBF58476D1CE4E5B9)&M
    z=((z^(z>>27))*0x94D049BB133111EB)&M
    return x, z^(z>>31)
def rotl(x,k): return ((x<<k)|(x>>(64-k)))&M
class R:
    def __init__(s,seed):
        st=[]
        for _ in range(4): seed,v=sm(seed); st.append(v)
        s.s=st
    def nd(s):
        s0,s1,s2,s3=s.s
        res=(rotl((s1*5)&M,7)*9)&M
        t=(s1<<17)&M
        s2^=s0; s3^=s1; s1^=s2; s0^=s3; s2^=t; s3=rotl(s3,45)
        s.s=[s0,s1,s2,s3]
        return (res>>11)*(1.0/9007199254740992.0)
import sys
for seed in range(1,21):
    r=R(seed^0xF0E1D2C3B4A5)
    print(seed,[round(r.nd(),3) for _ in range(5)])
