# (924) 突き合わせ：英の専制の入口の四半期の前後で、参政権・報道（reform_s）と戦争の月（nation_s）
# 使い方: python -X utf8 -I q6_cross.py <out>
import csv, sys, os
csv.field_size_limit(10**8)
EV = [('base_ins', 3, 156, 'A'), ('base_ins', 7, 226, 'A'), ('base_ins', 7, 361, 'B'), ('base_ins', 7, 499, 'B'), ('base_ins', 8, 174, 'A'), ('base_ins', 8, 291, 'B'),
      ('base_ins', 8, 509, 'B'), ('base_ins', 8, 639, 'B'), ('base_ins', 9, 250, 'A'), ('base_ins', 9, 529, 'B'), ('n1', 2, 50, 'A'), ('n1', 5, 147, 'A'), ('n1', 5, 413, 'B')]
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
out.write('arm\t種\t四半期\t型\t参政権(前→後)\t報道(前→後)\t政体の符号(前→後)\t正統性(前→後)\t戦争の月(前の四半期・入口)\n')
for sub, s, q, typ in EV:
    root = f'D:/PoB_work/keep_919/{sub}'
    rf = {}
    with open(f'{root}/reform_s{s}.tsv', encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if r['nation'] == 'イギリス' and int(r['quarter']) in (q - 1, q): rf[int(r['quarter'])] = r
    ns = {}
    with open(f'{root}/nation_s{s}.tsv', encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if r['nation'] == 'イギリス' and int(r['quarter']) in (q - 1, q): ns[int(r['quarter'])] = r
    g = lambda d, k, c: d.get(k, {}).get(c, '?')
    out.write(f"{sub}\ts{s}\t{q}\t{typ}\t{g(rf, q - 1, 'suffrageLawCode')}→{g(rf, q, 'suffrageLawCode')}\t{g(rf, q - 1, 'censorshipLawCode')}→{g(rf, q, 'censorshipLawCode')}\t"
              f"{g(rf, q - 1, 'polityCode')}→{g(rf, q, 'polityCode')}\t{g(rf, q - 1, 'legitimacyShare')[:6]}→{g(rf, q, 'legitimacyShare')[:6]}\t{g(ns, q - 1, 'atWarMonths')}・{g(ns, q, 'atWarMonths')}\n")
out.close()
