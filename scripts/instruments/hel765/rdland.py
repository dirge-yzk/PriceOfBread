import sys,collections
def load(p):
    R={}
    for ln in open(p,encoding='utf-8'):
        c=ln.rstrip('\n').split('\t')
        if c[0]=='a': continue
        R[(c[0],c[1])]=(float(c[4]),float(c[5]))
    return R
A=load(sys.argv[1]);B=load(sys.argv[2])
H='ヘルシングフォシュ';S='サンクトペテルブルク'
cnt=collections.Counter();ex=[]
for k in A:
    if A[k][1]!=B[k][1]:
        who='H' if H in k else ('S' if S in k else 'other')
        up=B[k][1]>A[k][1]
        cnt[(who,'up' if up else 'down')]+=1
        if who=='S' or up: ex.append((k,round(A[k][0]),round(A[k][1]),round(B[k][1])))
print(cnt)
for e in ex[:30]: print(e)
for k in [(H,S),(H,'ストックホルム'),(H,'リガ'),(H,'モスクワ'),(H,'トゥルク'),(H,'オーボ')]:
    if k in A: print(k, 'gc',round(A[k][0]),'old',round(A[k][1]),'new',round(B[k][1]))
r=[B[k][1]/A[k][0] for k in A if H in k and A[k][0]>0]
r0=[A[k][1]/A[k][0] for k in A if H in k and A[k][0]>0]
import statistics as st
print('Helsinki pairs land/gc median old',round(st.median(r0),3),'new',round(st.median(r),3))
