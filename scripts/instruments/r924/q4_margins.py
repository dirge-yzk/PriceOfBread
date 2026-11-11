# (924) 立憲の行の ① の門の余白（rep−cur−inertia）・rep−crown・兵士の傾き・軍の政治力・動員÷保持力の分位（nan は除く）
import csv, math, sys
csv.field_size_limit(10**8)
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
for nat in ['イギリス', 'プロイセン', 'ロシア', 'フランス', 'オーストリア', 'オスマン', '清', '日本']:
    V = {k: [] for k in ('gap', 'rc', 'lean', 'ap', 'mob', 'mil')}
    for s in range(1, 11):
        for r in csv.DictReader(open(f'D:/PoB_work/keep_919/base_ins/crownpc_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
            if r['nation'] != nat or r['polityCode'] != '3': continue
            try:
                rep = float(r['repSupportIndex']); cur = float(r['curSupportIndex']); cr = float(r['crownSupportIndex']); ine = float(r['inertiaIndex'])
                lean = float(r['soldierLeanIndex']); ap = float(r['armyPowerIndex']); mob = float(r['mobilizationIndex']) / max(1e-12, float(r['effectiveCoerceIndex']))
                pc = float(r['pcPrevShare'])
            except Exception:
                continue
            vals = dict(gap=rep - cur - ine, rc=rep - cr, lean=lean, ap=ap, mob=mob, mil=0.25 * ((1 - pc) - pc) if r['crownCode'] != '0' else 0.0)
            if any(math.isnan(v) for v in vals.values()): continue
            for k, v in vals.items(): V[k].append(v)
    q = lambda v, p: sorted(v)[int(p * (len(v) - 1))] if v else math.nan
    out.write(nat + f' 行{len(V["gap"])} ' + ' | '.join(f'{k} p10/50/90 {q(V[k], .1):.3g}/{q(V[k], .5):.3g}/{q(V[k], .9):.3g}' for k in V) + '\n')
out.close()
