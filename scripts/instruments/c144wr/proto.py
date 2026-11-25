import re,sys,glob,os
root='D:/PoB_work/wt_c144wr/PriceOfBread.SimCore'
def strip(t):
    t=re.sub(r'"(?:[^"\\n]|\.)*"','""',t)
    t=re.sub(r'//[^\n]*','',t)
    t=re.sub(r'/\*.*?\*/',lambda m:'\n'*m.group(0).count('\n'),t,flags=re.S)
    return t
files=[f for f in glob.glob(root+'/*.cs')]
for m in sys.argv[1:]:
    rx=re.compile(r'\.%s(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))'%m)
    for f in sorted(files):
        t=strip(open(f,encoding='utf-8').read())
        for mo in rx.finditer(t):
            i=mo.end();d=0;j=i
            while j<len(t):
                ch=t[j]
                if ch in '([{':d+=1
                elif ch in ')]}':
                    d-=1
                    if d<0:break
                elif ch==';' and d==0:break
                j+=1
            ln=t.count('\n',0,mo.start())+1
            print(m,os.path.basename(f),ln,'|',re.sub(r'\s+',' ',t[i:j])[:150])
