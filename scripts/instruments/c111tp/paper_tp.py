# (1138) c111tp 紙の台本：条約港の読み手から植民地格の港を外す形（H）の旗 0 の盤面での近似。
#   入力＝計器つき 10 種の生の rule_s（9 国は毎四半期・他の国は年の末）。
#   ⚠ rule_s は州の ForeignShares を持たない＝州の外国の取り分は Rule[2]（ruleForeignShare＝Σx に遅れて寄る）で近似する（v の 1：旗 0 ではエンジンと合わない）。
#   エンジンの値（treatyPortInForceFlag・portForeignShare）は rule_s の国の列＝正確。正確な colonialOnly の数は ident の生の tport_s から取る（count_tport.py）。
#   使い方：python -X utf8 paper_tp.py <ins> <出力の.tsv> [種の数=10]
import sys, os, json
from collections import defaultdict
ins, outp = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'PriceOfBread.SimCore')
wd = json.load(open(os.path.join(ROOT, 'world.json'), encoding='utf-8')); port = {}
for n in wd['nations']:
    for p in n['provinces']: port.setdefault(p['city'], bool(p.get('coastal')))
for x in json.load(open(os.path.join(ROOT, 'landmass.json'), encoding='utf-8')): port.setdefault(x['name'], bool(x.get('coastal')))
LINE = 0.25   # 0.5×ForeignRuleFromTreaty
agg = defaultdict(lambda: defaultdict(float))   # nation -> 量
tot = defaultdict(float)
for s in range(1, NS + 1):
    p = os.path.join(ins, f'rule_s{s}.tsv')
    if not os.path.exists(p): print('無い', p); continue
    groups = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            x = ln.rstrip('\n').split('\t')
            key = (x[ix['quarter']], x[ix['nation']])
            g = groups.get(key)
            if g is None:
                g = groups[key] = {'flag': x[ix['treatyPortInForceFlag']] == '1', 'pfs': float(x[ix['portForeignShare']]), 'regs': []}
            reg = x[ix['region']]
            if not port.get(reg, False): continue
            try: r2 = float(x[ix['ruleForeignShare']]); pk = float(x[ix['popK']])
            except ValueError: continue
            if r2 != r2: r2 = 0.0
            g['regs'].append((reg, x[ix['tier']], pk, max(0.0, min(1.0, r2))))
    for (q, nat), g in groups.items():
        a = agg[nat]; a['rows'] += 1
        home = [r for r in g['regs'] if r[1] != 'Colonial']; col = [r for r in g['regs'] if r[1] == 'Colonial']
        def share(rs):
            num = sum(r[2] * r[3] for r in rs if r[2] > 0); den = sum(r[2] for r in rs if r[2] > 0)
            return num / den if den > 0 else 0.0
        sAll, sHome = share(g['regs']), share(home)
        a['pfsEngine'] += g['pfs']; a['pfsAllApprox'] += sAll; a['pfsHomeApprox'] += sHome
        hMax = max([r[3] for r in home], default=0.0); cMax = max([r[3] for r in col], default=0.0)
        if g['flag']:
            a['flagEngine'] += 1
            if cMax > hMax: a['flagColonialTopApprox'] += 1
        if hMax >= LINE: a['homeAboveLineApprox'] += 1
        if cMax >= LINE and hMax < LINE: a['colonialOnlyAboveLineApprox'] += 1
        tot['rows'] += 1; tot['flagEngine'] += g['flag']
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('nation\trows\tflagEngine\tflagColonialTopApprox\thomeAboveLineApprox\tcolonialOnlyAboveLineApprox\tpfsEngineMean\tpfsAllApproxMean\tpfsHomeApproxMean\n')
    for nat in sorted(agg, key=lambda k: -agg[k]['flagEngine']):
        a = agg[nat]; n = a['rows']
        o.write('\t'.join([nat, str(int(n)), str(int(a['flagEngine'])), str(int(a['flagColonialTopApprox'])), str(int(a['homeAboveLineApprox'])), str(int(a['colonialOnlyAboveLineApprox'])),
                           f"{a['pfsEngine']/n:.5f}", f"{a['pfsAllApprox']/n:.5f}", f"{a['pfsHomeApprox']/n:.5f}"]) + '\n')
print('行（国×四半期）', int(tot['rows']), '旗 1（エンジン）', int(tot['flagEngine']))
