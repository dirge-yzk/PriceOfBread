# (1133) 英の本国（Colonial 以外）の港で外国の取り分 ≧0.25 になった州と四半期の数（腕 B・10 種の和）。
import sys, os, json
from collections import Counter
ins = sys.argv[1]; nat = sys.argv[2]
ROOT = 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore'
wd = json.load(open(ROOT + '/world.json', encoding='utf-8')); port = {}
for n in wd['nations']:
    for p in n['provinces']: port.setdefault(p['city'], bool(p.get('coastal')))
for x in json.load(open(ROOT + '/landmass.json', encoding='utf-8')): port.setdefault(x['name'], bool(x.get('coastal')))
c = Counter()
for s in range(1, 11):
    with open(os.path.join(ins, f'rulepower_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            x = ln.rstrip('\n').split('\t')
            if x[ix['nation']] != nat or x[ix['tier']] == 'Colonial' or not port.get(x[ix['region']], False): continue
            try: F = float(x[ix['nowRule2Share']])
            except ValueError: continue
            if F >= 0.25: c[(x[ix['region']], x[ix['tier']])] += 1
for k, v in c.most_common(10): print(nat, k[0], k[1], v)
