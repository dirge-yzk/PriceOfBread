import re
L='C:/Users/dirge/Documents/PriceOfBread/docs/202609/99_実装ログ_20260903.md'
PATS=[r'失敗\s*[1-9]', r'赤\s*[1-9]', r'赤が\s*[1-9]', r'[1-9]\s*本が赤', r'赤[はの]\s*[1-9]']
pat=re.compile('|'.join(PATS))
print('PATTERNS', PATS, 'restricted to lines containing フルスイート and line>=50000')
hdr=''
for i,l in enumerate(open(L,encoding='utf-8'),1):
    if l.startswith('## '): hdr=l[3:60].strip()
    if i<50000 or 'フルスイート' not in l: continue
    m=pat.search(l)
    if m:
        s=max(0,m.start()-160); print(i,hdr[:45],'|',l[s:m.end()+120].replace('\n',' '))
