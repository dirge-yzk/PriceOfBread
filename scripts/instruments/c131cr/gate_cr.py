# (1278) c131cr 史実の桁の線（この線が一番動かす量）と記録。エンジンの関数を呼ばない（読む州と札を台本が自分で選ぶ）。
#   H1 国の主流の文化の札が首都の州の札と違う国×四半期の数（region_s の州の人口・腕の規則で数える：old＝植民地は昇格の時の持ち主の首都の札・全ての州／new＝データの札・植民地格を数えない）
#   H2 植民地格の州の分離の判定が通った回数（secession_s の goFlag=1・tier Colonial）＋植民地格の州だけで現れた国の数（region_s）
#   記録：内戦の国×四半期（nation_s civilWarMonths>0）・植民地格の州の人口（1783・1850・1913・1950 の四半期）
# 使い方: python -I gate_cr.py <腕の生> <腕の規則 old|new> <基準の生> <基準の規則 old|new> <world.json> <colony_labels.tsv> [種の起点 1|11]
import sys, csv, json, collections
sys.stdout.reconfigure(encoding='utf-8')
arm, armrule, base, baserule, wpath, lpath = sys.argv[1:7]
sb = int(sys.argv[7]) if len(sys.argv) > 7 else 1
seeds = list(range(sb, sb + 10))
csv.field_size_limit(10**8)
w = json.load(open(wpath, encoding='utf-8-sig'))
prov = {p['city']: (p.get('culture') or '', p.get('religion') or '') for n in w['nations'] for p in n['provinces']}
lab = {}
with open(lpath, encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'): lab[r['name']] = (r['culture'], r['religion'])

def rows(path):
    with open(path, encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for c in rd:
            if len(c) >= len(h): yield ix, c

def best(t):
    b, bv = '', 0.0
    for k, v in t.items():
        if v > bv: bv, b = v, k
    return b

def read(raw, rule):
    out = collections.Counter(); colpop = collections.defaultdict(float)
    for s in seeds:
        byq = collections.defaultdict(lambda: collections.OrderedDict()); oldlab = {}; seen_nat = {}
        for ix, c in rows(f"{raw}/region_s{s}.tsv"):
            if c[ix['activeFlag']] != '1': continue
            q = int(c[ix['quarter']]); nat = c[ix['nation']]
            byq[q].setdefault(nat, []).append((c[ix['region']], c[ix['tier']], float(c[ix['populationK']])))
        for q in sorted(byq):
            for nat, regs in byq[q].items():
                cap = regs[0][0]
                if nat not in seen_nat:
                    seen_nat[nat] = q
                    if q > 0 and all(t == 'Colonial' for (_, t, _) in regs): out['H2_colonial_born_nations'] += 1
                for (reg, tier, pk) in regs:
                    if reg not in prov and reg not in oldlab:
                        oldlab[reg] = prov.get(cap) or oldlab.get(cap) or ('', '')
                def labof(reg):
                    if reg in prov: return prov[reg]
                    return oldlab.get(reg, ('', '')) if rule == 'old' else lab.get(reg, ('', ''))
                ct = {}
                for (reg, tier, pk) in regs:
                    if tier == 'Colonial' and q in (0, 268, 520, 667): colpop[q] += pk
                    if not (pk > 0): continue
                    if rule == 'new' and tier == 'Colonial': continue
                    cu = labof(reg)[0]
                    if cu: ct[cu] = ct.get(cu, 0) + pk
                m = best(ct); out['nq'] += 1
                if m != labof(cap)[0]: out['H1_cult_ne_cap'] += 1
                if m == '': out['rec_cult_empty'] += 1
        for ix, c in rows(f"{raw}/secession_s{s}.tsv"):
            if c[ix['goFlag']] == '1' and c[ix['tier']] == 'Colonial': out['H2_colonial_go'] += 1
        for ix, c in rows(f"{raw}/nation_s{s}.tsv"):
            try:
                if float(c[ix['civilWarMonths']]) > 0: out['rec_civilwar_nq'] += 1
            except ValueError: pass
    return out, colpop

a, ap = read(arm, armrule); b, bp = read(base, baserule)
print(f"arm {arm} rule={armrule}: " + ' '.join(f"{k}={v}" for k, v in sorted(a.items())))
print(f"base {base} rule={baserule}: " + ' '.join(f"{k}={v}" for k, v in sorted(b.items())))
ng = []
x, y = a['H1_cult_ne_cap'], b['H1_cult_ne_cap']
r1 = x / y if y > 0 else float('nan')
print(f"H1 主流の文化≠首都の札の国×四半期 腕 {x}／基準 {y}＝{r1:.4f}（NG＝0.1 未満か 10 超）")
if y > 0 and not (0.1 <= r1 <= 10): ng.append('H1')
x2 = a['H2_colonial_go'] + a['H2_colonial_born_nations']; y2 = b['H2_colonial_go'] + b['H2_colonial_born_nations']
print(f"H2 植民地格の分離の判定＋植民地格だけで現れた国 腕 {x2}（{a['H2_colonial_go']}＋{a['H2_colonial_born_nations']}）／基準 {y2}（{b['H2_colonial_go']}＋{b['H2_colonial_born_nations']}）（NG＝腕＞10×max(基準,1)・基準≧10 で腕＜基準÷10）")
if x2 > 10 * max(y2, 1) or (y2 >= 10 and x2 < y2 / 10): ng.append('H2')
print("記録 植民地格の州の人口（千人・種の和）四半期 0/268/520/667：腕 " + ' '.join(f"{ap[q]:.0f}" for q in (0, 268, 520, 667)) + " 基準 " + ' '.join(f"{bp[q]:.0f}" for q in (0, 268, 520, 667)))
print(f"記録 内戦の国×四半期 腕 {a['rec_civilwar_nq']} 基準 {b['rec_civilwar_nq']}・空の主流の札 腕 {a['rec_cult_empty']} 基準 {b['rec_cult_empty']}")
print("GATE_CR " + ("NG " + ','.join(ng) if ng else "緑"))
