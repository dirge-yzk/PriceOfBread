# (1161) S22 の行の係数と識別子：宣言の行・既定の値・読み手の数（SimCore〔Coefficients* を除く〕・App・Runner・Tests・scripts）を数える（読むだけ）
import sys, os, re
REPO = sys.argv[1]; NAMES = sys.argv[2:]
sys.stdout.reconfigure(newline='\n')
def walk(sub, exts):
    out = []
    for root, _, fs in os.walk(os.path.join(REPO, sub)):
        if os.sep + 'bin' in root or os.sep + 'obj' in root: continue
        for f in fs:
            if f.endswith(exts): out.append(os.path.join(root, f))
    return out
groups = {'SimCore': walk('PriceOfBread.SimCore', ('.cs',)), 'App': walk('PriceOfBread.App', ('.cs', '.axaml')),
          'Runner': walk('PriceOfBread.Runner', ('.cs',)), 'Tests': walk('PriceOfBread.Tests', ('.cs',)) + walk('PriceOfBread.Tests.Ui', ('.cs',)),
          'scripts': walk('scripts', ('.py', '.sh'))}
txt = {}
def T(p):
    if p not in txt: txt[p] = open(p, encoding='utf-8-sig', errors='replace').read().split('\n')
    return txt[p]
for nm in NAMES:
    pat = re.compile(r'\b' + re.escape(nm) + r'\b')
    decl = '—'; cnt = {}
    for g, ps in groups.items():
        n = 0
        for p in ps:
            for i, l in enumerate(T(p)):
                if not pat.search(l): continue
                b = os.path.basename(p)
                if g == 'SimCore' and b.startswith('Coefficients'):
                    if decl == '—' and re.search(r'(public|private|internal)\s+(const\s+)?[\w<>]+\s+' + re.escape(nm) + r'\b', l):
                        m = re.search(r'=\s*([^;]+);', l); decl = f'{b}:{i+1}={m.group(1).strip() if m else "?"}'
                    continue
                n += 1
        cnt[g] = n
    print(nm + '\t' + decl + '\t' + ' '.join(f'{g}{v}' for g, v in cnt.items()))
