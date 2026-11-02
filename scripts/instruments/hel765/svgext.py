import re,sys,xml.etree.ElementTree as ET
SVG='C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.App/Assets/worldmap.svg'
vbW,vbH=2752.766,1537.631
def lon(x): return -166.4+x/vbW*360
def lat(y): return 98-y/vbH*198
tree=ET.parse(SVG); root=tree.getroot()
ns='{http://www.w3.org/2000/svg}'
parent={c:p for p in root.iter() for c in p}
def cls(e):
    n=e
    while n is not None:
        c=n.get('class')
        if c: return c
        n=parent.get(n)
    return ''
tok=re.compile(r'[MmLlCcZzHhVvSsQqAa]|-?\d*\.?\d+(?:e-?\d+)?')
def pts(d):
    out=[];cmd=None;cx=cy=0;sx=sy=0;nums=[]
    toks=tok.findall(d);i=0
    def flush(): pass
    while i<len(toks):
        t=toks[i]
        if t.isalpha():
            cmd=t;i+=1
            if cmd in 'Zz': cx,cy=sx,sy
            continue
        if cmd in 'ML':
            cx,cy=float(toks[i]),float(toks[i+1]);i+=2
            if cmd=='M': sx,sy=cx,cy; cmd='L'
            out.append((cx,cy))
        elif cmd in 'ml':
            cx+=float(toks[i]);cy+=float(toks[i+1]);i+=2
            if cmd=='m': sx,sy=cx,cy; cmd='l'
            out.append((cx,cy))
        elif cmd=='C':
            cx,cy=float(toks[i+4]),float(toks[i+5]);i+=6;out.append((cx,cy))
        elif cmd=='c':
            cx+=float(toks[i+4]);cy+=float(toks[i+5]);i+=6;out.append((cx,cy))
        elif cmd=='H': cx=float(toks[i]);i+=1;out.append((cx,cy))
        elif cmd=='h': cx+=float(toks[i]);i+=1;out.append((cx,cy))
        elif cmd=='V': cy=float(toks[i]);i+=1;out.append((cx,cy))
        elif cmd=='v': cy+=float(toks[i]);i+=1;out.append((cx,cy))
        else: raise Exception('cmd '+cmd)
    return out
bycc={}
for e in root.iter(ns+'path'):
    d=e.get('d')
    if not d: continue
    c=cls(e)
    if 'land' not in c: continue
    if e.get('transform') or (parent.get(e) is not None and parent[e].get('transform')): pass
    cc=c.split()[-1]
    bycc.setdefault(cc,[]).append(pts(d))
import pickle;pickle.dump(bycc,open('svgpts.pkl','wb'))
print(len(bycc))
