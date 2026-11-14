# (1126) 1 国の最低賃金の段の動き（上げも下げも）を種ごとに 1 行で：年:前→後(上げの時の政体 polityCode＝前の行)
#   python -X utf8 -I q3.py <国> <出力> <名=生 ins> [...]
import sys, os, csv
sys.stdout.reconfigure(newline='\n')
nat, out = sys.argv[1], sys.argv[2]
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    for a in sys.argv[3:]:
        name, d = a.split('=', 1)
        seeds = sorted(int(f[len('monlaw_s'):-4]) for f in os.listdir(d) if f.startswith('monlaw_s') and f.endswith('.tsv'))
        for s in seeds:
            L = []
            with open(os.path.join(d, f'monlaw_s{s}.tsv'), encoding='utf-8', newline='') as f:
                r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
                for x in r:
                    if len(x) >= len(h) and x[ix['nation']] == nat:
                        L.append((int(x[ix['quarter']]), int(x[ix['year']]), int(x[ix['polityCode']]), int(x[ix['MinimumWage']]), int(x[ix['MinimumWageTarget']])))
            L.sort(); ev = []
            if L and L[0][3] > 0: ev.append(f'{L[0][1]}q{L[0][0]}:start{L[0][3]}')
            pol = []
            for i in range(1, len(L)):
                if L[i][3] != L[i - 1][3]: ev.append(f'{L[i][1]}:{L[i-1][3]}>{L[i][3]}(p{L[i-1][2]})')
                if L[i][2] != L[i - 1][2]: pol.append(f'{L[i][1]}:{L[i-1][2]}>{L[i][2]}')
            wantQ = sum(1 for x in L if x[4] > x[3])
            fo.write(f'{name}\ts{s}\tmw: {" ".join(ev) or "-"}\twantUpQ={wantQ}\tpol: {" ".join(pol[:14])}{" …" if len(pol) > 14 else ""}\n')
