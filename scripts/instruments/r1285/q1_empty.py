# (1285) c132rd 問い1：空の主流の札（新の規則＝植民地格を数えない・データの札）の国×四半期を分類し、続いた四半期の区間を数える。
# エンジンの関数を呼ばない。gate_cr.py（c131cr）と同じ数え方で空を判定し、分類だけを足す。
# 使い方: python -X utf8 -I q1_empty.py <生> <種の起点> <種の数> <world.json> <colony_labels.tsv> <出力の接頭>
import sys, csv, json, collections
raw, sb, ns, wpath, lpath, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5], sys.argv[6]
csv.field_size_limit(10**8)
w = json.load(open(wpath, encoding='utf-8-sig'))
prov = {p['city']: (p.get('culture') or '', p.get('religion') or '') for n in w['nations'] for p in n['provinces']}
lab = {}
with open(lpath, encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'): lab[r['name']] = (r['culture'], r['religion'])
def labof(reg):
    if reg in prov: return prov[reg]
    return lab.get(reg, ('', ''))

cat_q = collections.Counter(); eps = []
for s in range(sb, sb + ns):
    byq = collections.defaultdict(dict); everhome = collections.defaultdict(lambda: None); first = {}
    with open(f"{raw}/region_s{s}.tsv", encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for c in rd:
            if len(c) < len(h) or c[ix['activeFlag']] != '1': continue
            q = int(c[ix['quarter']]); nat = c[ix['nation']]
            byq[q].setdefault(nat, []).append((c[ix['region']], c[ix['tier']], float(c[ix['populationK']]), int(c[ix['year']])))
    cur = {}  # nat -> 区間（開始の四半期・分類の数・州）
    for q in sorted(byq):
        seen = set()
        for nat, regs in byq[q].items():
            if nat not in first: first[nat] = (q, all(t == 'Colonial' for (_, t, _, _) in regs))
            home = [(r, t, p) for (r, t, p, _) in regs if t != 'Colonial']
            ct = {}
            for (reg, t, pk) in home:
                if not (pk > 0): continue
                cu = labof(reg)[0]
                if cu: ct[cu] = ct.get(cu, 0) + pk
            if home and any(pk > 0 for (_, _, pk) in home) and everhome[nat] is None: everhome[nat] = q
            if ct:
                if nat in cur: eps.append(cur.pop(nat) | {'endq': q, 'end': 'home_label'})
                continue
            if not home: k = 'A_colonial_only'
            elif not any(pk > 0 for (_, _, pk) in home): k = 'B_home_pop0'
            elif all(labof(r)[0] == '' for (r, _, pk) in home if pk > 0): k = 'C_home_label_empty'
            else: k = 'D_other'
            cat_q[k] += 1; seen.add(nat)
            yr = regs[0][3]
            if nat not in cur:
                cur[nat] = {'seed': s, 'nat': nat, 'startq': q, 'year0': yr, 'n': 0, 'cats': collections.Counter(),
                            'born_q': first[nat][0], 'born_colonial': first[nat][1], 'had_home_before': everhome[nat] is not None and everhome[nat] < q,
                            'regs': ';'.join(sorted({r for (r, _, _, _) in regs}))[:200], 'tiers': ';'.join(sorted({t for (_, t, _, _) in regs})),
                            'popK': round(sum(p for (_, _, p, _) in regs), 1), 'homeregs': ';'.join(sorted({r for (r, _, _) in home}))[:200]}
            cur[nat]['n'] += 1; cur[nat]['cats'][k] += 1; cur[nat]['lastyear'] = yr
        for nat in [n for n in cur if n not in seen and n not in byq[q]]:
            eps.append(cur.pop(nat) | {'endq': q, 'end': 'nation_gone'})
    for nat in list(cur): eps.append(cur.pop(nat) | {'endq': -1, 'end': 'run_end'})

with open(outp + '_eps.tsv', 'w', encoding='utf-8', newline='\n') as f:
    cols = ['seed', 'nat', 'startq', 'year0', 'lastyear', 'n', 'cats', 'born_q', 'born_colonial', 'had_home_before', 'end', 'endq', 'tiers', 'popK', 'regs', 'homeregs']
    f.write('\t'.join(cols) + '\n')
    for e in sorted(eps, key=lambda e: (e['seed'], e['startq'])):
        e = dict(e); e['cats'] = ','.join(f"{k}:{v}" for k, v in sorted(e['cats'].items()))
        f.write('\t'.join(str(e.get(c, '')) for c in cols) + '\n')
tot = sum(cat_q.values())
lines = [f"raw={raw} seeds={sb}..{sb+ns-1} 空の国×四半期 計 {tot}"]
for k in ('A_colonial_only', 'B_home_pop0', 'C_home_label_empty', 'D_other'): lines.append(f"  {k} {cat_q[k]}")
ln = sorted(e['n'] for e in eps)
lines.append(f"区間 {len(eps)} 本・長さ 最小 {ln[0] if ln else 0} 中央 {ln[len(ln)//2] if ln else 0} 最大 {ln[-1] if ln else 0}・長さ 20 以上の区間 {sum(1 for x in ln if x >= 20)} 本で {sum(x for x in ln if x >= 20)} 国×四半期")
bys = collections.Counter();
for e in eps: bys[e['seed']] += e['n']
lines.append("種ごと " + ' '.join(f"s{k}:{v}" for k, v in sorted(bys.items())))
ends = collections.Counter(e['end'] for e in eps); lines.append("区間の終わり " + ' '.join(f"{k}:{v}" for k, v in sorted(ends.items())))
bc = collections.Counter((e['born_colonial'], e['had_home_before']) for e in eps)
lines.append("区間の国の生まれ（植民地格だけで生まれた, 前に本国格の人が居た）" + ' '.join(f"{k}:{v}" for k, v in sorted(bc.items())))
open(outp + '_sum.txt', 'w', encoding='utf-8', newline='\n').write('\n'.join(lines) + '\n')
print('\n'.join(lines))
