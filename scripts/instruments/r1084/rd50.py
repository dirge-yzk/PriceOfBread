# (1084) 休眠 50 本の読み手を main decb50fa5 の作業ツリーで数える（読むだけ）
import os, re, sys
ROOT = 'C:/Users/dirge/Documents/PriceOfBread'
W = 'D:/PoB_work/r1084_work'
names = [l.strip() for l in open(W + '/paper50.txt', encoding='utf-8') if l.strip()]
areas = {'SimCore': 'PriceOfBread.SimCore', 'App': 'PriceOfBread.App', 'Runner': 'PriceOfBread.Runner',
         'Tests': 'PriceOfBread.Tests', 'scripts': 'scripts'}
files = {}
for k, d in areas.items():
    for dp, dn, fn in os.walk(os.path.join(ROOT, d)):
        if any(x in dp for x in ('\\bin', '/bin', '\\obj', '/obj', '__pycache__')):
            continue
        for f in fn:
            if f.endswith(('.cs', '.axaml', '.py', '.sh', '.json', '.tsv')):
                p = os.path.join(dp, f)
                try:
                    files[p] = (k, open(p, 'rb').read().decode('utf-8', 'replace').split('\n'))
                except Exception:
                    pass
coef = os.path.join(ROOT, 'PriceOfBread.SimCore', 'Coefficients.cs')
out = open(W + '/rd50.tsv', 'w', encoding='utf-8', newline='\n')
out.write('名\t既定(Coef行の字)\tCoef行\t読み手SimCore(コード行)\tApp\tRunner\tTests\tscripts\tSimCoreの読み手のファイル:行\n')
for n in names:
    pat = re.compile(r'\b' + re.escape(n) + r'\b')
    decl = ''
    dline = ''
    cnt = {k: 0 for k in areas}
    where = []
    for p, (k, lines) in files.items():
        for i, L in enumerate(lines, 1):
            if not pat.search(L):
                continue
            s = L.strip()
            if s.startswith('//') or s.startswith('///') or s.startswith('#'):
                continue
            if p == coef and re.search(r'public\s+(double|int|bool|UnitValue|[A-Za-z<>]+)\s+' + re.escape(n) + r'\b', L):
                decl = s[:90]
                dline = str(i)
                continue
            # 行の中のコメント部を外して名がコードにあるか
            code = L.split('//')[0]
            if not pat.search(code):
                continue
            cnt[k] += 1
            if k == 'SimCore':
                where.append(os.path.relpath(p, ROOT).replace('\\', '/').replace('PriceOfBread.SimCore/', '') + ':' + str(i))
    out.write('\t'.join([n, decl, dline] + [str(cnt[k]) for k in areas] + [' '.join(where[:12]) + (' …' if len(where) > 12 else '')]) + '\n')
out.close()
print('ok')
