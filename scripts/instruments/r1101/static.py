# (1101) 静的な件数（json の行・試験の件数）。呼び方：python -X utf8 -I static.py <repo> <出力>
import json, os, re, sys, collections
repo, outp = sys.argv[1], sys.argv[2]
S = os.path.join(repo, 'PriceOfBread.SimCore'); T = os.path.join(repo, 'PriceOfBread.Tests')
out = open(outp, 'w', encoding='utf-8', newline='\n')
def P(*a): out.write('\t'.join(str(x) for x in a) + '\n')
def J(n):
    d = json.load(open(os.path.join(S, n), encoding='utf-8-sig')); return d if isinstance(d, list) else list(d.values())[0]
sea = J('sea_nodes.json'); P('static', 'sea_nodes', len(sea)); P('static', 'basins', ' '.join(f'{k} {v}' for k, v in collections.Counter(x.get('basin') for x in sea).most_common()))
pas = J('passages.json'); P('static', 'passages', len(pas))
bar = J('barriers.json'); c = collections.Counter(x.get('kind') for x in bar); P('static', 'barrier', c['barrier']); P('static', 'pass', c['pass'])
P('static', 'landmass', len(J('landmass.json')))
for f in ('SeaGraphTests', 'SeaStitchBudgetTests', 'PathGraphTests', 'LandGraphFixTests', 'LandmassTests', 'PortRewireTests', 'PortByLandTests', 'TransitTests', 'RouteTensionTests'):
    s = open(os.path.join(T, f + '.cs'), encoding='utf-8-sig').read()
    P('tests', f, len(re.findall(r'\[(Fact|Theory)\b', s)))
out.close()
