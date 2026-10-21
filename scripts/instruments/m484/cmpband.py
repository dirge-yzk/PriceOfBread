import sys,re
def norm(p):
    L=open(p,'rb').read().decode('utf-8').split('\n')
    out=[]
    for i,l in enumerate(L):
        if l.startswith('★年齢の層の数'): continue
        if l.strip().startswith('前回の基線：'): continue
        l=re.sub(r'　前回比 [^　]*$','',l); l=re.sub(r'　前回値なし.*$','',l)
        out.append(l)
    return out
a,b=norm(sys.argv[1]),norm(sys.argv[2])
d=[(i,x,y) for i,(x,y) in enumerate(zip(a,b)) if x!=y]
print(f'lines {len(a)} vs {len(b)} diffs {len(d)}')
for i,x,y in d[:20]: print(i,'\n  A:',x[:200],'\n  B:',y[:200])
