import os,re,sys,glob
root='C:/Users/dirge/Documents/PriceOfBread'
names=sys.argv[1].split(',')
def files(sub,ext=('.cs','.axaml','.xaml')):
    for dp,dn,fn in os.walk(os.path.join(root,sub)):
        if '\bin' in dp or '/bin' in dp or 'obj' in dp.split(os.sep)[-1] or '/obj' in dp or '\obj' in dp: continue
        for f in fn:
            if f.endswith(ext): yield os.path.join(dp,f)
groups={'SimCore':'PriceOfBread.SimCore','Tests':'PriceOfBread.Tests','App':'PriceOfBread.App','Runner':'PriceOfBread.Runner','Disp':'PriceOfBread.Display'}
cache={}
for g,s in groups.items():
    cache[g]=[]
    if not os.path.isdir(os.path.join(root,s)): continue
    for p in files(s):
        try: t=open(p,'rb').read().decode('utf-8','replace')
        except: continue
        cache[g].append((p,t.split('\n')))
for n in names:
    rx=re.compile(r'\b'+re.escape(n)+r'\b')
    out=[];decl=''
    for g in groups:
        c=0;nf=0
        for p,lines in cache.get(g,[]):
            hit=0
            for i,l in enumerate(lines):
                if rx.search(l):
                    if 'Coefficients' in os.path.basename(p) and g=='SimCore':
                        if re.search(r'(public|internal).*\b'+re.escape(n)+r'\b\s*(\{|=)',l): decl=os.path.basename(p)+':'+str(i+1)+' '+l.strip()[:110]
                        continue
                    c+=1;hit=1
            nf+=hit
        out.append(f'{g}={c}/{nf}f')
    print(n,'|',' '.join(out),'|',decl)
