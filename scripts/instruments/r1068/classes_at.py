import re,sys
L='C:/Users/dirge/Documents/PriceOfBread/docs/202609/99_実装ログ_20260903.md'
lines=open(L,encoding='utf-8').read().split('\n')
for n in map(int,sys.argv[1:]):
    seg=' '.join(lines[n-1:n+2])
    print(n, sorted(set(re.findall(r'[A-Z][A-Za-z0-9]+Tests\d?',seg))))
