import re
L='C:/Users/dirge/Documents/PriceOfBread/docs/202609/99_実装ログ_20260903.md'
pat=re.compile(r'フルスイート[^。]{0,80}?(\d+)\s*分\s*(?:(\d+)\s*秒)?')
out=[]
for i,l in enumerate(open(L,encoding='utf-8'),1):
    if i<50000: continue
    for m in pat.finditer(l):
        mi=int(m.group(1)); s=int(m.group(2) or 0); out.append((i,mi*60+s,l[m.start():m.end()][:90]))
for o in out: print(o[0], round(o[1]/60,1), o[2].replace('\n',' '))
v=sorted(o[1]/60 for o in out if 3<o[1]/60<80)
if v: print('n',len(v),'min',v[0],'med',v[len(v)//2],'max',v[-1])
