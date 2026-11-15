# (1148) 消す候補の名の読み手を数える（宣言の行を含む・コードの行のみ＝// の注だけの行は別に数える）
import sys, os, re
REPO = sys.argv[1]; NAMES = sys.argv[2:]
sys.stdout.reconfigure(newline='\n')
AREAS = {'SimCore': 'PriceOfBread.SimCore', 'App': 'PriceOfBread.App', 'Runner': 'PriceOfBread.Runner', 'Tests': 'PriceOfBread.Tests', 'TestsUi': 'PriceOfBread.Tests.Ui', 'scripts': 'scripts'}
EXT = ('.cs', '.axaml', '.xaml', '.py', '.sh')
texts = []
for a, d in AREAS.items():
    for root, _, fs in os.walk(os.path.join(REPO, d)):
        if any(x in root for x in (os.sep + 'bin', os.sep + 'obj')): continue
        for f in fs:
            if f.endswith(EXT):
                p = os.path.join(root, f)
                try: L = open(p, encoding='utf-8-sig').read().split('\n')
                except Exception: continue
                texts.append((a, os.path.relpath(p, REPO).replace(os.sep, '/'), L))
print('name\t' + '\t'.join(AREAS) + '\tcommentOnly\tfiles(SimCore)')
for n in NAMES:
    rx = re.compile(r'\b' + re.escape(n) + r'\b')
    cnt = {a: 0 for a in AREAS}; com = 0; fsim = set()
    for a, p, L in texts:
        for l in L:
            if rx.search(l):
                code = l.split('//')[0] if p.endswith('.cs') else l
                if rx.search(code):
                    cnt[a] += 1
                    if a == 'SimCore': fsim.add(p.split('/')[-1])
                else: com += 1
    print(n + '\t' + '\t'.join(str(cnt[a]) for a in AREAS) + f'\t{com}\t' + ','.join(sorted(fsim)))
