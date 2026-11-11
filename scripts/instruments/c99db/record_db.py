# (1048) c99db 記録（門にしない）：国ごとの官僚制度・国教の上げの最初の年（種の中央）と到達した種の数・国教の上げの純支持の中央。
#   python scripts/instruments/c99db/record_db.py <基線の生> <腕の生> [<ヌル腕 n1> <ヌル腕 n2>]（どれも lordstat_s のあるフォルダ）
import glob, csv, sys, os, statistics as st

def first_years(d):
    """国→{(軸,段): [種ごとの最初の年 or None]}・国→純支持の列"""
    out = {}; net = {}
    for f in sorted(glob.glob(d + '/lordstat_s*.tsv')):
        seen = {}
        with open(f, encoding='utf-8') as fh:
            for x in csv.DictReader(fh, delimiter='\t'):
                n = x['nation']; y = int(x['year'])
                for ax, col in (('官僚制度', 'bureaucracyCode'), ('国教', 'stateReligionCode')):
                    v = int(x[col])
                    for lv in (1, 2):
                        k = (n, ax, lv)
                        if v >= lv and k not in seen: seen[k] = y
                        seen.setdefault(('all', n), True)
                net.setdefault(n, []).append(float(x['relGateNetUp']))
        nations = {k[1] for k in seen if k[0] == 'all'}
        for n in nations:
            for ax in ('官僚制度', '国教'):
                for lv in (1, 2):
                    out.setdefault(n, {}).setdefault((ax, lv), []).append(seen.get((n, ax, lv)))
    return out, net

def summ(v):
    got = [y for y in v if y is not None]
    return f'{len(got)}/{len(v)} 種・中央 {st.median(got):.0f}' if got else f'0/{len(v)} 種'

dirs = [('基線', sys.argv[1]), ('腕', sys.argv[2])] + ([('n1', sys.argv[3]), ('n2', sys.argv[4])] if len(sys.argv) > 4 else [])
res = {nm: first_years(d) for nm, d in dirs if os.path.isdir(d)}
nations = sorted(set().union(*[set(r[0].keys()) for r in res.values()]))
print('国\t軸\t段\t' + '\t'.join(res.keys()))
for n in nations:
    for ax in ('官僚制度', '国教'):
        for lv in (1, 2):
            print(f'{n}\t{ax}\t{lv}\t' + '\t'.join(summ(res[k][0].get(n, {}).get((ax, lv), [])) for k in res))
print()
print('国教の上げの純支持（relGateNetUp）の中央：国\t' + '\t'.join(res.keys()))
for n in nations:
    print(n + '\t' + '\t'.join((f'{st.median(res[k][1][n]):.4f}' if res[k][1].get(n) else '-') for k in res))
