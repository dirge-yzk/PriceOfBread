# (924) 英の PC の軌道を 1 本の種で抜き出す（4 四半期ごと＋印のある行は全て）
# 使い方: python -X utf8 -I q1_series.py <sub> <seed> <y0> <y1> <out> [nation]
import csv, sys, math
csv.field_size_limit(10**8)
sub, seed, y0, y1, outp = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
nat = sys.argv[6] if len(sys.argv) > 6 else 'イギリス'
p = f'D:/PoB_work/keep_919/{sub}/crownpc_s{seed}.tsv'
C = ['year', 'quarter', 'polityCode', 'crownCode', 'pcNowShare', 'summonTermShare', 'rollbackTermShare', 'term1Share', 'term2Share', 'term2pShare',
     'residualShare', 'grantFlag', 'regimeEventFlag', 'relabelFlag', 'explainCode', 'debtGripShare', 'interestPerRevenueShare', 'legitimacyIndex',
     'term1SupportGateFlag', 'term1CoerceGateFlag', 'term2GateFlag', 'soldierLeanIndex', 'armyPowerIndex', 'repSupportIndex', 'curSupportIndex', 'crownSupportIndex', 'inertiaIndex']
with open(p, encoding='utf-8', newline='') as fh, open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('\t'.join(C) + '\n')
    for row in csv.DictReader(fh, delimiter='\t'):
        if row['nation'] != nat: continue
        y = int(row['year'])
        if y < y0 or y > y1: continue
        mark = row['grantFlag'] == '1' or row['regimeEventFlag'] == '1' or row['relabelFlag'] == '1'
        if int(row['quarter']) % 4 != 0 and not mark: continue
        vals = []
        for c in C:
            v = row[c]
            try:
                fv = float(v)
                vals.append(v if c in ('year', 'quarter') or fv == int(fv) and abs(fv) < 100 and 'Code' in c or 'Flag' in c else f'{fv:.4g}')
            except Exception:
                vals.append(v)
        o.write('\t'.join(vals) + '\n')
