# (1087) 合流：c103dm (1081) の門を合わせた木で読む台本（予言 measurements/m1087_20261113/prereg_merge.txt [4] の dm の段・腕の前に書いた）。読むだけ・エンジンを呼ばない。
#   DM-D′ で係数 11 本と計器の列 8 本が消える＝gate_dm.py の T0〜T2 は合わせた木で列の欠けとして赤になる作り＝読み替えずに次の形で読む：
#   T3  ：flow_s の sourceFile=Megaprojects.cs の行＝0（gate_dm.py の T3 と同じ規則・種は --flowseeds）。
#   T1′ ：残した帳簿の列 fiscal_s の ledgerMegaprojectPerMonth の 0 でない行（nan も数える）＝0。
#   T2′ ：nation_s・fiscal_s・scalars_s の見出しに消した 8 列が 1 本も無い（構造）。
#   T0′ ：--tree を渡したとき、その木の PriceOfBread.SimCore/Coefficients.cs に係数 11 本の名が 0 件（構造）。
#   欠けた表・欠けた列・床未満の行は緑にせず〈欠け〉で赤（何もしない門にしない）。
#   python -X utf8 -I gate_dm1087.py <ins> [--seeds 1-10] [--flowseeds 1-3] [--tree <木>]
import sys, os, csv, re
csv.field_size_limit(1 << 30)
args = [a for a in sys.argv[1:] if not a.startswith('--')]
opt = {}
av = sys.argv[1:]
i = 0
while i < len(av):
    if av[i].startswith('--'):
        if '=' in av[i]: k, v = av[i][2:].split('=', 1)
        else: k, v = av[i][2:], av[i + 1]; i += 1
        opt[k] = v
    i += 1
ins = args[0]
def rng(s): lo, hi = map(int, s.split('-')); return list(range(lo, hi + 1))
seeds = rng(opt.get('seeds', '1-10')); fseeds = rng(opt.get('flowseeds', '1-3'))
red = []
def bad(g, why): red.append(g); print(f'  ✕ {g}: {why}')
DEL = {'fiscal_s': ['claimMegaForeignYieldAtQuarterEnd', 'claimMonumentBuildPerMonth'],
       'nation_s': ['monumentPaidPerMonth', 'megaCrisisPassCount', 'megaCrisisGateOpenCount'],
       'scalars_s': ['megaStartedCount', 'megaStartedUnmetCount', 'megaStartedOldGateClosedCount']}
COEF = ['MegaprojectEnabled', 'MegaprojectStateFrac', 'MegaprojectStateCap', 'MegaprojectPrestige', 'MegaprojectCounterCyclical',
        'MegaprojectCrisisDrawdown', 'MegaprojectForeignShare', 'MegaprojectForeignMinRank', 'MegaprojectForeignDrawFrac',
        'MegaprojectForeignYield', 'FormationLegitimacyBoost']

print('== T3 経路（flow_s の sourceFile=Megaprojects.cs の行）')
for s in fseeds:
    p = os.path.join(ins, f'flow_s{s}.tsv')
    if not os.path.exists(p): bad('T3', f'種 {s} の flow_s が無い（欠け）'); continue
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        if 'sourceFile' not in h: bad('T3', f'種 {s} に列 sourceFile が無い'); continue
        j = h.index('sourceFile'); mp = oth = 0
        for row in r:
            if j < len(row) and row[j] == 'Megaprojects.cs': mp += 1
            else: oth += 1
    print(f'  種 {s}：Megaprojects.cs {mp} 行・他 {oth} 行')
    if oth < 1000: bad('T3', f'種 {s} の行 {oth} が床 1000 未満（欠け）')
    if mp != 0: bad('T3', f'種 {s} に Megaprojects.cs の行 {mp}')

print('== T1′ 帳簿（fiscal_s の ledgerMegaprojectPerMonth の 0 でない行・nan も数える）')
nz = rows = 0
for s in seeds:
    p = os.path.join(ins, f'fiscal_s{s}.tsv')
    if not os.path.exists(p): bad('T1′', f'種 {s} の fiscal_s が無い（欠け）'); continue
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        if 'ledgerMegaprojectPerMonth' not in h: bad('T1′', f'種 {s} に列が無い（欠け）'); continue
        j = h.index('ledgerMegaprojectPerMonth'); n = 0
        for row in r:
            n += 1
            v = row[j] if j < len(row) else ''
            try: x = float(v)
            except ValueError: x = float('nan')
            if not (x == 0.0): nz += 1
        rows += n
        if n < 1000: bad('T1′', f'種 {s} の行 {n} が床 1000 未満（欠け）')
print(f'  行 {rows}・0 でない行 {nz}')
if nz != 0: bad('T1′', f'0 でない行 {nz}')

print('== T2′ 見出し（消した 8 列が無い）')
for t, cols in DEL.items():
    p = os.path.join(ins, f'{t}{seeds[0]}.tsv')
    if not os.path.exists(p): bad('T2′', f'{t}{seeds[0]} が無い（欠け）'); continue
    with open(p, encoding='utf-8') as f: h = f.readline().rstrip('\n').split('\t')
    have = [c for c in cols if c in h]
    print(f'  {t}：列 {len(h)}・消した列のうち残る物 {have}')
    if have: bad('T2′', f'{t} に {have}')

if 'tree' in opt:
    print('== T0′ 係数の名（木の Coefficients.cs）')
    src = open(os.path.join(opt['tree'], 'PriceOfBread.SimCore', 'Coefficients.cs'), encoding='utf-8').read()
    hits = {c: len(re.findall(r'\b' + c + r'\b', src)) for c in COEF}
    left = {c: n for c, n in hits.items() if n}
    print(f'  残る名 {left}')
    if left: bad('T0′', f'{left}')

print('判定: ' + ('緑（全て）' if not red else '赤 ' + ','.join(sorted(set(red)))))
