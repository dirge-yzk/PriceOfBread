# (1170) 旗 0 の盤面の数え（読むだけ・列名で読む＝★(749)）
# 使い方: python -X utf8 -I flag0_count.py <keep_dir> <out>
# 1) base_ins/t1buy_s の全ての数の列の 10 種の和（退きの列・popC 系が旗 0 で 0 かを数える）
# 2) t1buy_s の qtyX と tb_s の impQ を (種, 年, 四半期, 国) で突き合わせた一致の行の数（★(1136)）
# 3) tb_s の impQ・nationgood_s の crossImportQty と crossImportValue の年ごとの 10 種の和を base_ins・n1・n2 で並べる（ヌル腕の幅）
import sys, os, csv, math

keep, outp = sys.argv[1], sys.argv[2]
YEARS = (1830, 1870, 1913)
csv.field_size_limit(1 << 30)

def rows(path):
    with open(path, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t')
        hdr = next(rd)
        ix = {h: i for i, h in enumerate(hdr)}
        for r in rd:
            if len(r) < len(hdr):
                continue
            yield ix, r

def fnum(s):
    try:
        v = float(s)
    except ValueError:
        return None
    return v if math.isfinite(v) else None

out = []
# 1) t1buy_s 旗 0 の和
t1 = {}
t1key = {}
nrows_t1 = 0
for s in range(1, 11):
    p = os.path.join(keep, 'base_ins', f't1buy_s{s}.tsv')
    for ix, r in rows(p):
        nrows_t1 += 1
        for h, i in ix.items():
            if h in ('year', 'q', 'nation'):
                continue
            v = fnum(r[i])
            if v is None:
                continue
            if h == 'u1MaxRel' or h == 'selfMaxRel':
                t1[h] = max(t1.get(h, -1e300), v)
            else:
                t1[h] = t1.get(h, 0.0) + v
        y = int(r[ix['year']])
        t1key[(s, r[ix['year']], r[ix['q']], r[ix['nation']])] = float(r[ix['qtyX']])
        for yy in YEARS:
            if y == yy:
                t1[f'qtyX@{yy}'] = t1.get(f'qtyX@{yy}', 0.0) + float(r[ix['qtyX']])
                t1[f'dealsX@{yy}'] = t1.get(f'dealsX@{yy}', 0.0) + float(r[ix['dealsX']])
out.append(f'[1] base_ins/t1buy_s 旗 0・10 種の和（行 {nrows_t1}）')
for h in sorted(t1):
    out.append(f'  {h}\t{t1[h]:.10g}')

# 2) qtyX と tb_s の impQ の突き合わせ
match = diff = only_t1 = only_tb = 0
maxrel = 0.0
seen = set()
for s in range(1, 11):
    p = os.path.join(keep, 'base_ins', f'tb_s{s}.tsv')
    for ix, r in rows(p):
        k = (s, r[ix['year']], r[ix['q']], r[ix['nation']])
        v = float(r[ix['impQ']])
        seen.add(k)
        if k not in t1key:
            only_tb += 1
            continue
        a = t1key[k]
        rel = abs(a - v) / max(1e-300, abs(v)) if v != 0 else (0.0 if a == 0 else float('inf'))
        if rel <= 1e-9:
            match += 1
        else:
            diff += 1
            maxrel = max(maxrel, rel)
only_t1 = sum(1 for k in t1key if k not in seen)
out.append(f'[2] t1buy_s.qtyX 対 tb_s.impQ（種・年・四半期・国）：一致 {match}・違う {diff}（最大の相対差 {maxrel:.3g}）・t1buy だけ {only_t1}・tb だけ {only_tb}')

# 3) ヌル腕の幅（年ごとの 10 種の和）
def yearsum(arm, table, col):
    acc = {yy: 0.0 for yy in YEARS}
    acc['all'] = 0.0
    n = 0
    for s in range(1, 11):
        p = os.path.join(keep, arm, f'{table}{s}.tsv')
        if not os.path.exists(p):
            return None, 0
        for ix, r in rows(p):
            v = fnum(r[ix[col]])
            if v is None:
                continue
            n += 1
            y = int(r[ix['year']])
            acc['all'] += v
            if y in acc:
                acc[y] += v
    return acc, n

out.append('[3] 年ごとの 10 種の和と n1・n2 の base に対する比')
for table, col in (('tb_s', 'impQ'), ('nationgood_s', 'crossImportQty'), ('nationgood_s', 'crossImportValue'), ('t1buy_s', 'qtyX')):
    res = {}
    for arm in ('base_ins', 'n1', 'n2'):
        res[arm] = yearsum(arm, table, col)
    b = res['base_ins'][0]
    line = [f'  {table}.{col}']
    for key in list(YEARS) + ['all']:
        parts = [f'{key}: base {b[key]:.6g}' if b else f'{key}: base 無し']
        for arm in ('n1', 'n2'):
            a = res[arm][0]
            if a is None or not b or b[key] == 0:
                parts.append(f'{arm} 無し')
            else:
                parts.append(f'{arm} {a[key] / b[key]:.4f}')
        line.append('    ' + '・'.join(parts))
    line.append(f'    行 base {res["base_ins"][1]}・n1 {res["n1"][1]}・n2 {res["n2"][1]}')
    out.extend(line)

with open(outp, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(out) + '\n')
print('done', outp)
