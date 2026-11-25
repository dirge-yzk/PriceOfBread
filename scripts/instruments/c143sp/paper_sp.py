# (1357) c143sp 紙の台本：使い方 python paper_sp.py <ins（D:/ の形）> <種の起点> <種の数>
#  H1 史実の桁の線（錨）：イギリスの石炭の値段（nationgood_s の priceMeanGold）の 1913 年÷最初の年。
#     史実の錨＝1（英の坑口の石炭の値段は 1790 年代と 1913 年がほぼ同じ桁〈推〉）。門＝全ての種で [0.1, 10]（10 倍の線）。
#  R1 記録（stproc_s が有れば）：国家の購入（buyer null）の代金の四半期の和・決済の月の国家の購入が旧の規則の LastExport の値打ちに占める割合（6 大国と全体）。
#  R2 記録：stproc_s の旗の列と、旧の規則と新の規則の輸出の値打ちの差（旗 0 の木では旧＝今・新＝今−決済の月の購入）。
import sys, os, csv
csv.field_size_limit(1 << 30)
ins, sb, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
BIG6 = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ']

def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for v in r: yield ix, v

print('# (1357) paper_sp', ins, 'seeds', sb, '..', sb + n - 1)
bad = 0; vals = []
for s in range(sb, sb + n):
    p = os.path.join(ins, f'nationgood_s{s}.tsv')
    if not os.path.exists(p): print(f'H1 s{s} 表が無い'); bad += 1; continue
    first = None; v13 = None
    for ix, v in rows(p):
        if v[ix['nation']] != 'イギリス' or v[ix['good']] != '石炭': continue
        y = int(float(v[ix['year']])); px = float(v[ix['priceMeanGold']])
        if first is None: first = (y, px)
        if y == 1913: v13 = px
    if first is None or v13 is None or not (first[1] > 0):
        print(f'H1 s{s} 系列が無い（英の石炭の 1913 年が無い＝英が 1913 年に居ない）＝判定しない'); continue
    ratio = v13 / first[1]; ok = 0.1 <= ratio <= 10; vals.append(ratio)
    if not ok: bad += 1
    print(f'H1 s{s} first={first[0]} {first[1]:.4g} y1913={v13:.4g} ratio={ratio:.3g} {"緑" if ok else "赤"}')
print(f'H1 赤の種 {bad}/{n}' + (f' 比の最小 {min(vals):.3g} 最大 {max(vals):.3g}' if vals else ''))

have = os.path.exists(os.path.join(ins, f'stproc_s{sb}.tsv'))
if not have:
    print('R1 stproc_s が無い（器より前の生）＝記録の量は腕 Z の生で数える'); sys.exit(0)
tot = {}; flags = set(); mism = 0; nrow = 0
for s in range(sb, sb + n):
    p = os.path.join(ins, f'stproc_s{s}.tsv')
    if not os.path.exists(p): print(f'R1 s{s} 表が無い'); continue
    for ix, v in rows(p):
        nrow += 1
        nm = v[ix['nation']]; flags.add(v[ix['asSoldFlag']])
        buy = float(v[ix['buyValueQ']]); sett = float(v[ix['settleBuyValueNow']])
        old = float(v[ix['exportValueOldRuleNow']]); new = float(v[ix['exportValueNewRuleNow']]); now = float(v[ix['exportValueNow']])
        fl = v[ix['asSoldFlag']]
        if fl == '0' and abs(old - now) > 1e-9 * max(1.0, abs(now)): mism += 1
        if fl == '1' and abs(new - now) > 1e-9 * max(1.0, abs(now)): mism += 1
        for k in (nm, '*全体'):
            t = tot.setdefault(k, [0.0, 0.0, 0.0, 0.0]); t[0] += buy; t[1] += sett; t[2] += old; t[3] += new
print(f'R2 行 {nrow}・旗の列の値 {sorted(flags)}・〈今〉と旗の側の規則の食い違い {mism}')
for k in BIG6 + ['*全体']:
    if k not in tot: print(f'R1 {k} 行が無い'); continue
    b, st, o, nw = tot[k]
    print(f'R1 {k} 国家の購入の代金の和 {b:.4g}・決済の月の購入÷旧の規則の輸出の値打ち {st / o if o > 0 else float("nan"):.4f}・旧 {o:.4g} 新 {nw:.4g}')
