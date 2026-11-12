"""(1074) H017：開幕の徴兵法（world.json の initialConscription）が最初の四半期の行で保たれているか。種 1 の q0〜q8 の法の並び。"""
import csv, json, collections
W = json.load(open('C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json', encoding='utf-8'))
def nations(o):
    if isinstance(o, dict):
        if 'initialConscription' in o or ('name' in o and 'regions' in o): yield o
        for v in o.values(): yield from nations(v)
    elif isinstance(o, list):
        for v in o: yield from nations(v)
data = {}
for n in nations(W):
    nm = n.get('name') or n.get('nameJa') or n.get('id')
    data[str(nm)] = n.get('initialConscription', 'Selective(既定)')
out = [f'world.json の国（名を持つ物）{len(data)}・志願 {sum(1 for v in data.values() if v=="Volunteer")}']
out.append('名の例: ' + ', '.join(list(data)[:8]))
seq = collections.defaultdict(list); athr = collections.defaultdict(list)
for s in [1, 2]:
    f = open(f'D:/PoB_work/keep_1069/base_ins/nation_s{s}.tsv', encoding='utf-8', newline='')
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    for row in r:
        q = int(row[ix['quarter']])
        if q > 8: break
        seq[(s, row[ix['nation']])].append(row[ix['conscriptionLaw']])
        athr[(s, row[ix['nation']])].append(row[ix['armyThreatShare']][:5])
    f.close()
for (s, n), v in sorted(seq.items()):
    out.append(f's{s}\t{n}\tdata={data.get(n, "?")}\tlaw q0..8={"".join(v)}\tarmyThr q0={athr[(s,n)][0]}')
open('D:/PoB_work/r1074_work/out_openlaw.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
