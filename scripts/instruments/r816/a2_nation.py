# (816) 入った州の金鉱の需要（excluded の落ち）を、挙げた 9 国の AuDemand と、挙げていない国の和（metal_s − 9 国）で上から押さえる。
import sys
F1 = 'D:/PoB_work/keep_813/rej/c12svB_10/ins'
def rd(p):
    with open(p, 'rb') as f: t = f.read().decode('utf-8').splitlines()
    h = t[0].split('\t'); return [dict(zip(h, l.split('\t'))) for l in t[1:]]
out = ['seed\tquarter\tyear\tworldGoldDemand\tpicked9Sum\tnonPickedSum\tbritainAuD\tchinaAuD\tmaxOtherPickedAuD\tbritainAuStk\tbritainAuAtOrBelowParityRegions\tbritainAgStk\tbritainAgD']
for s, qs in ((2, range(16, 26)), (4, range(16, 26))):
    met = {int(r['quarter']): r for r in rd(f'{F1}/metal_s{s}.tsv')}
    mn = rd(f'{F1}/metalnat_s{s}.tsv')
    for q in qs:
        rows = [r for r in mn if int(r['quarter']) == q]
        tot = sum(float(r['AuDemand']) for r in rows)
        br = [r for r in rows if r['nation'] == 'イギリス'][0]
        ch = [r for r in rows if r['nation'] == '清'][0]
        oth = max(float(r['AuDemand']) for r in rows if r['nation'] not in ('イギリス',))
        wg = float(met[q]['goldDemand'])
        out.append('\t'.join(str(x) for x in (s, q, met[q]['year'], f'{wg:.0f}', f'{tot:.0f}', f'{wg-tot:.0f}', f"{float(br['AuDemand']):.0f}", f"{float(ch['AuDemand']):.0f}", f'{oth:.0f}', f"{float(br['AuStock']):.0f}", br['AuRegionsAtOrBelowParity'], f"{float(br['AgStock']):.0f}", f"{float(br['AgDemand']):.0f}")))
open('D:/PoB_work/r816_work/out/a2_nation.tsv', 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
