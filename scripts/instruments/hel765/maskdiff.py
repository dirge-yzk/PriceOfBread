import sys
Nx,Ny=3600,1800
BOX=(59.0,61.5,21.0,31.0)
def unpack(p):
    b=open(p,'rb').read(); return b
def bit(b,i): return (b[i>>3]>>(i&7))&1
a=unpack(sys.argv[1]); c=unpack(sys.argv[2])
inb=outb=0; addin=remin=addout=remout=0; outlist=[]
for k in range(len(a)):
    x=a[k]^c[k]
    if not x: continue
    for j in range(8):
        if not (x>>j)&1: continue
        i=k*8+j; iy,ix=divmod(i,Nx)
        lat=90-(iy+0.5)*0.1; lon=-180+(ix+0.5)*0.1
        new=bit(c,i)
        if BOX[0]<=lat<=BOX[1] and BOX[2]<=lon<=BOX[3]:
            inb+=1; addin+=new; remin+=1-new
        else:
            outb+=1; addout+=new; remout+=1-new
            if len(outlist)<20: outlist.append((round(lat,2),round(lon,2),new))
print(f"box(59-61.5N,21-31E) changed {inb} (+land {addin} / -land {remin}) ; outside {outb} (+{addout}/-{remout})")
if outlist: print(' outside sample',outlist)
