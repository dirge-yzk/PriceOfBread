# (1196) 問い2：兵→厭戦→内戦の鎖を数える（scalars_s の国×四半期・全ての国／wars_s の内戦）。
#  厭戦の基の速さは 兵÷人口 に比例（Diplomacy.cs:2257〜:2262）。数える物：厭戦≧0.99 の国×四半期・内戦の始まりの年に、その国の厭戦の年の最大≧0.9 だった割合。
#  使い方: python -X utf8 -I q2_exh.py <名>=<ins> ... --seeds=1-10
import sys, csv, os, collections
sys.stdout.reconfigure(newline='\n')
args = [a for a in sys.argv[1:] if not a.startswith('--')]
s0, s1 = map(int, next(a for a in sys.argv[1:] if a.startswith('--seeds=')).split('=')[1].split('-'))
print('arm\t厭戦≧0.99 の国×四半期\t国×四半期\t内戦の始まり\tうち始まりの年の厭戦の最大≧0.9\t同≧0.5\tうち交戦中の年')
for a in args:
    nm, d = a.split('=', 1)
    sat = tot = cw = c9 = c5 = cwar = 0
    for s in range(s0, s1 + 1):
        mx = collections.defaultdict(float); war = set()
        with open(os.path.join(d, f'scalars_s{s}.tsv'), encoding='utf-8') as f:
            for x in csv.DictReader(f, delimiter='\t'):
                e = float(x['warExhaustion']); tot += 1; sat += e >= 0.99
                k = (x['nation'], int(x['year'])); mx[k] = max(mx[k], e)
                if x['atWar'] not in ('0', '', 'False'): war.add(k)
        with open(os.path.join(d, f'wars_s{s}.tsv'), encoding='utf-8') as f:
            for x in csv.DictReader(f, delimiter='\t'):
                if x['civilWarFlag'] != '1' or x['entryCode'] == 'secession': continue
                k = (x['sideA'], int(x['startYear'])); cw += 1
                c9 += mx.get(k, 0) >= 0.9; c5 += mx.get(k, 0) >= 0.5; cwar += k in war
    print(f'{nm}\t{sat}\t{tot}\t{cw}\t{c9}\t{c5}\t{cwar}')
