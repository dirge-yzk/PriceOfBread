import re,sys
L='C:/Users/dirge/Documents/PriceOfBread/docs/202609/99_実装ログ_20260903.md'
R='C:/Users/dirge/Documents/PriceOfBread/docs/README.md'
pat=re.compile(r'(束の外|フルスイートで初めて|フルスイートだけ|フルスイートまで見逃|フルスイートが赤|フルスイートで赤|フルスイートの赤|合わせた木のフルスイート[^。]{0,40}赤)')
for f in (L,R):
    hdr=''
    for i,l in enumerate(open(f,encoding='utf-8'),1):
        if l.startswith('## '): hdr=l[3:40].strip()
        for m in pat.finditer(l):
            s=max(0,m.start()-150); print(f[-10:],i,hdr,'|',l[s:m.end()+150].replace('\n',' '))
            break
