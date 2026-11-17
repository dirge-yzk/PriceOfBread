# (1196) 内戦の塊（同じ国で毎年の短い内戦）の国の軌道：兵÷人口・交戦・内戦の月・闘争度・厭戦・政体（scalars_s）を M と Amob で並べる。
#  使い方: python -X utf8 -I q2_cluster.py <M ins> <Amob ins> <種> <国> <始めの年> <終わりの年>
import sys, csv, os
sys.stdout.reconfigure(newline='\n')
M, A, s, nat, y0, y1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4], int(sys.argv[5]), int(sys.argv[6])
for nm, d in (('M', M), ('Amob', A)):
    nr = {}
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as f:
        for x in csv.DictReader(f, delimiter='\t'):
            if x['nation'] != nat: continue
            y = int(x['year'])
            if y0 <= y <= y1 and y not in nr:
                tp = float(x['totalPopulationK'] or 0)
                nr[y] = (float(x['soldierK']) / tp if tp > 0 else float('nan'), x['atWarMonths'], x['civilWarMonths'], x['conscriptionLaw'])
    sr = {}
    with open(os.path.join(d, f'scalars_s{s}.tsv'), encoding='utf-8') as f:
        for x in csv.DictReader(f, delimiter='\t'):
            if x['nation'] != nat: continue
            y = int(x['year'])
            if y0 <= y <= y1 and y not in sr: sr[y] = (x['polityCode'], float(x['militancyMean']), float(x['warExhaustion']), float(x['legitimacy']))
    print(f'## {nm} s{s} {nat}')
    print('  ' + ' | '.join(f'{y}:兵{nr[y][0]*100:.1f}% 戦{nr[y][1]} 内{nr[y][2]} 徴{nr[y][3]}' + (f' 政{sr[y][0]} 闘{sr[y][1]:.2f} 厭{sr[y][2]:.2f} 正{sr[y][3]:.2f}' if y in sr else '') for y in sorted(nr) if (y - y0) % 2 == 0))
