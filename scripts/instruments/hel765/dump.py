import sys
Nx,Ny=3600,1800
def load(p):
    b=open(p,'rb').read()
    return b
def isl(b,lat,lon):
    yi=int((90-lat)/0.1); xi=int((lon+180)/0.1)%Nx
    i=yi*Nx+xi
    return (b[i>>3]>>(i&7))&1
p=sys.argv[1]; la0,la1,lo0,lo1=map(float,sys.argv[2:6])
b=load(p)
print("     "+"".join(str(int(round(lo0+k*0.1))%10) if abs((lo0+k*0.1)-round(lo0+k*0.1))<0.01 else ' ' for k in range(int(round((lo1-lo0)/0.1)))))
la=la1
while la>la0-1e-9:
    print(f"{la:5.1f}"+"".join('#' if isl(b,la-0.05,lo0+k*0.1+0.05) else '.' for k in range(int(round((lo1-lo0)/0.1)))))
    la=round(la-0.1,1)
