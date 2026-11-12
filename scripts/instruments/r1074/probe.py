import csv, sys, collections
B = 'D:/PoB_work/keep_1069/base_ins/'
def head(t, s=1):
    f = open(B + f'{t}_s{s}.tsv', encoding='utf-8', newline='')
    r = csv.reader(f, delimiter='\t')
    return f, r, next(r)
out = []
# nation domains
f, r, h = head('nation'); ix = {k: i for i, k in enumerate(h)}
dom = collections.defaultdict(collections.Counter); nats = set(); qs = set()
for row in r:
    nats.add(row[ix['nation']]); qs.add(row[ix['quarter']])
    for k in ['conscriptionLaw', 'militaryStageTargetEndStage', 'militaryStageEnforcedEndStage', 'serfEmancipation', 'mobilizedFlag', 'militaryMoveOutcomeCode', 'aiPickedWarStageFlag', 'warStageOpenEndFlag']:
        dom[k][row[ix[k]]] += 1
out.append(f'nation s1: nations={len(nats)} quarters={len(qs)}')
for k, c in dom.items(): out.append(f'  {k}: {dict(c.most_common(8))}')
f.close()
for t in ['stock', 'fm', 'monlaw', 'wp', 'crownpc', 'admrule', 'warentry']:
    f, r, h = head(t); ix = {k: i for i, k in enumerate(h)}
    nk = 'nation' if 'nation' in ix else h[1]
    tk = 'quarter' if 'quarter' in ix else ('time' if 'time' in ix else 'year')
    nats = collections.Counter(); ts = set(); n = 0
    for row in r:
        n += 1; nats[row[ix[nk]]] += 1; ts.add(row[ix[tk]])
    out.append(f'{t}: rows={n} nations={len(nats)} times={len(ts)} key={tk} top={nats.most_common(3)} cols={len(h)}')
    if t == 'monlaw': out.append('  monlaw head: ' + ','.join(h[:12]))
    if t == 'admrule': out.append('  admrule head: ' + ','.join(h[:20]))
    f.close()
open('D:/PoB_work/r1074_work/out_probe.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
