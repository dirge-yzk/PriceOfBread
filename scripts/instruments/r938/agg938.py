# (938) found938.tsv の集計（腕ごと）。読むだけ。
import csv, sys
sys.stdout.reconfigure(encoding='utf-8')
rows = list(csv.DictReader(open(sys.argv[1], encoding='utf-8'), delimiter='\t'))
arms = []
for r in rows:
    if r['arm'] not in arms: arms.append(r['arm'])
print('腕\t建国\t植民地格だけ\t建国前に0の州だけ\t植民地格だけ∧建国前に0\t建国から0≧1行\t≧4行\t≧4行∧人口≧10千\t前0∧0≧1\t前0の行の長さ(続いた行/系列の行)')
for a in arms:
    rs = [r for r in rows if r['arm'] == a]
    co = [r for r in rs if r['colonialOnly0'] == '1']
    pz = [r for r in rs if r['preZeroAll'] == '1']
    both = [r for r in co if r['preZeroAll'] == '1']
    z1 = [r for r in rs if int(r['zeroRunFromStart']) >= 1]
    z4 = [r for r in rs if int(r['zeroRunFromStart']) >= 4]
    z4p = [r for r in z4 if float(r['pop0K']) >= 10]
    pz1 = [r for r in pz if int(r['zeroRunFromStart']) >= 1]
    det = ' '.join(f"s{r['seed']}:{r['nation']}:{r['zeroRunFromStart']}/{r['rows']}:{r['endHow']}" for r in pz)
    print(f"{a}\t{len(rs)}\t{len(co)}\t{len(pz)}\t{len(both)}\t{len(z1)}\t{len(z4)}\t{len(z4p)}\t{len(pz1)}\t{det}")
print()
print('前0で建国から0 が無い（建国の最初の行で正）＝行の型')
for r in rows:
    if r['preZeroAll'] == '1' and int(r['zeroRunFromStart']) == 0:
        print(r['arm'], r['seed'], r['nation'], r['q0'], r['tiers0'], r['out0'], r['regSav0'], r['rows'])
