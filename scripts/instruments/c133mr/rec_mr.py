# (1290) c133mr 記録（採否に使わない）：割譲の sameCore の 1 の割合（cederule_s）・統一の宣戦（unifwar_s の engineCode 0＝通った評価）・
#   民族の緊張（tensionwhy_s の Ethnic の列が正の行＝反目・負の行＝親近）。エンジンの関数を呼ばない（表の列を数えるだけ）。
# 使い方: python -X utf8 -I rec_mr.py <生> <種の起点> <種の数> <タグ>
import sys, csv
raw, sb, ns, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
csv.field_size_limit(10**8)
def rows(name, s):
    try: f = open(f'{raw}/{name}{s}.tsv', encoding='utf-8')
    except FileNotFoundError: return None
    rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
    return ix, rd, f
tot = {'cede': 0, 'cede1': 0, 'cedenan': 0, 'unifEval': 0, 'unifGo': 0, 'ethPos': 0, 'ethNeg': 0, 'ethRows': 0}
miss = []
print(f'# (1290) rec_mr {raw} 種 {sb}..{sb+ns-1} タグ {tag}')
for s in range(sb, sb + ns):
    o = dict.fromkeys(tot, 0)
    r = rows('cederule_s', s)
    if r is None: miss.append(f'cederule_s{s}')
    else:
        ix, rd, f = r
        for c in rd:
            if len(c) <= ix['sameCoreFlag']: continue
            v = c[ix['sameCoreFlag']]; o['cede'] += 1
            if v == 'nan': o['cedenan'] += 1
            elif float(v) == 1: o['cede1'] += 1
        f.close()
    r = rows('unifwar_s', s)
    if r is None: miss.append(f'unifwar_s{s}')
    else:
        ix, rd, f = r
        for c in rd:
            if len(c) <= ix['engineCode']: continue
            o['unifEval'] += 1
            if c[ix['engineCode']] == '0': o['unifGo'] += 1
        f.close()
    r = rows('tensionwhy_s', s)
    if r is None: miss.append(f'tensionwhy_s{s}')
    else:
        ix, rd, f = r
        for c in rd:
            if len(c) <= ix['Ethnic']: continue
            e = float(c[ix['Ethnic']]); o['ethRows'] += 1
            if e > 0: o['ethPos'] += 1
            elif e < 0: o['ethNeg'] += 1
        f.close()
    for k in tot: tot[k] += o[k]
    print(f'種 {s}\t' + '\t'.join(f'{k}={v}' for k, v in o.items()))
sh = tot['cede1'] / (tot['cede'] - tot['cedenan']) if tot['cede'] - tot['cedenan'] > 0 else float('nan')
print('計\t' + '\t'.join(f'{k}={v}' for k, v in tot.items()) + f'\tsameCoreShare={sh:.6f}')
if miss: print('欠け: ' + ' '.join(miss))
