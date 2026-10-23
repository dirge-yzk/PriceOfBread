# (505) 線 ir：腕 B（計器つき 10 種）の生の計器を読む道具（読むだけ）。事前登録 measurements/ir505_20261023_prereg.txt 追記 1-2 の G-M（購入が u を縮める）・E-M（希望が立つ場所）・[4] の M4（上限の削除）と記録の読み手。
# 使い方：PYTHONUTF8=1 python an_ir.py <腕 B の ins フォルダ> [<比べる側の ins フォルダ＝keep_500/m500_ins>]
# 出力：
#   (1) ir_s（腕 B）：挙げた国の年代別（20 年刻み）の u の平均・u＝0 の呼びの割合・希望が立った割合・計画の割合・guard の非増加の割合・indMax
#   (2) G-M：全ての種×国の guardNonIncrease の合計÷guardPairs の合計（0.95 以上＝働いた・未満＝機構が壊れている）
#   (3) E-M：挙げた 9 国の eqr_s から〈新しいレシピが開いた四半期〉（四半期 0 でなく (既定) でない・(国,財,レシピ) で gateOpenRegions が初めて正）を作り、国×四半期がその 12 四半期以内か否かで desireCalls÷gapCalls を分けた比 R（2.0 以上＝働いた・1.2〜2.0＝弱い・1.2 未満＝働いていない）
#   (4) M4：indMax の最大（発散の線 10）・indOver5 の最大
#   (5) 比べる側が与えられたとき eq_s の pointsBought（購入が足した点の和）の年代別の比（腕 B÷比べる側）と indMean・indMax の年代別（記録）
import csv, glob, os, re, sys, collections

arm = sys.argv[1]
base = sys.argv[2] if len(sys.argv) > 2 else None
PICK = ['イギリス', 'フランス', '清', '日本', 'アメリカ']
NINE = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
WINDOW = 12


def rows(path):
    with open(path, encoding='utf-8', newline='') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))


def f(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float('nan')


def decade(y):
    return int(y) // 20 * 20


def seed_of(path):
    return int(re.search(r'_s(\d+)\.tsv$', path).group(1))


ir = []
for p in sorted(glob.glob(os.path.join(arm, 'ir_s*.tsv'))):
    for d in rows(p):
        d['_seed'] = seed_of(p)
        ir.append(d)
if not ir:
    print('ir_s が無い（旗 InvestReturnFromMargin が 0 の木か、計器が無い）')
    sys.exit(1)

print('== (1) ir_s の年代別（20 年刻み・挙げた国だけ・全ての種の合計から）==')
agg = collections.defaultdict(lambda: collections.defaultdict(float))
for d in ir:
    if d['nation'] not in PICK:
        continue
    k = (d['nation'], decade(d['year']))
    a = agg[k]
    for c in ['gapCalls', 'uSum', 'zeroCalls', 'desireCalls', 'desireGapCalls', 'plannedCalls', 'guardPairs', 'guardNonIncrease']:
        a[c] += f(d[c])
    a['uMax'] = max(a['uMax'], f(d['uMax']))
    a['indMax'] = max(a['indMax'], f(d['indMax']))
print('nation\tyears\tgapCalls\tuMean\tuMax\tzeroShare\tdesireShare\tplannedShare\tguardPairs\tguardNonShare\tindMax')
for (nat, dec), a in sorted(agg.items(), key=lambda kv: (PICK.index(kv[0][0]), kv[0][1])):
    gc = a['gapCalls']
    gp = a['guardPairs']
    print(f"{nat}\t{dec}-{dec + 19}\t{gc:.0f}\t{(a['uSum'] / gc if gc else float('nan')):.3f}\t{a['uMax']:.2f}\t{(a['zeroCalls'] / gc if gc else float('nan')):.3f}\t{(a['desireCalls'] / gc if gc else float('nan')):.3f}\t{(a['plannedCalls'] / gc if gc else float('nan')):.3f}\t{gp:.0f}\t{(a['guardNonIncrease'] / gp if gp else float('nan')):.3f}\t{a['indMax']:.2f}")

gp = sum(f(d['guardPairs']) for d in ir)
gn = sum(f(d['guardNonIncrease']) for d in ir)
share = gn / gp if gp else float('nan')
print('\n== (2) G-M 購入が u を縮める ==')
print(f'guardNonIncrease {gn:.0f} / guardPairs {gp:.0f} = {share:.4f}  →', '働いた（0.95 以上）' if share >= 0.95 else ('判定不能（購入が無い）' if gp == 0 else '機構が壊れている（0.95 未満）'))

print('\n== (3) E-M 希望が立つ場所（新しいレシピが開いた 12 四半期以内か否か・挙げた 9 国）==')
openings = collections.defaultdict(list)   # (seed, nation) -> [開いた四半期]
for p in sorted(glob.glob(os.path.join(arm, 'eqr_s*.tsv'))):
    sd = seed_of(p)
    first = {}
    for d in rows(p):
        if d['nation'] not in NINE or d['recipe'] == '(既定)':
            continue
        if f(d['gateOpenRegions']) > 0:
            key = (d['nation'], d['good'], d['recipe'])
            q = int(d['quarter'])
            if key not in first or q < first[key]:
                first[key] = q
    for (nat, _, _), q in first.items():
        if q > 0:
            openings[(sd, nat)].append(q)
tot = {True: [0.0, 0.0, 0.0], False: [0.0, 0.0, 0.0]}   # in window? -> [desireCalls, gapCalls, 行数]
for d in ir:
    nat = d['nation']
    if nat not in NINE:
        continue
    q = int(d['quarter'])
    inw = any(0 <= q - o < WINDOW for o in openings.get((d['_seed'], nat), []))
    t = tot[inw]
    t[0] += f(d['desireCalls']); t[1] += f(d['gapCalls']); t[2] += 1
r_in = tot[True][0] / tot[True][1] if tot[True][1] else float('nan')
r_out = tot[False][0] / tot[False][1] if tot[False][1] else float('nan')
print(f'窓の内：希望 {tot[True][0]:.0f} / 呼び {tot[True][1]:.0f}（行 {tot[True][2]:.0f}）＝ {r_in:.4f}')
print(f'窓の外：希望 {tot[False][0]:.0f} / 呼び {tot[False][1]:.0f}（行 {tot[False][2]:.0f}）＝ {r_out:.4f}')
if tot[False][1] and r_out > 0:
    R = r_in / r_out
    print(f'R ＝ {R:.3f}  →', '働いた（2.0 以上）' if R >= 2.0 else '弱い（1.2〜2.0）' if R >= 1.2 else '働いていない（1.2 未満）')
else:
    print('R は定義できない（窓の外の呼びが無いか希望が 0）＝判定不能')
print(f'希望率の全体 ＝ {(tot[True][0] + tot[False][0]) / max(1.0, tot[True][1] + tot[False][1]):.4f}（m500 は net が常に正＝ほぼ 1）')

print('\n== (4) M4 上限の削除 ==')
print('indMax の最大 =', max(f(d['indMax']) for d in ir), '（発散の線 10）・indOver5 の最大 =', max(f(d['indOver5']) for d in ir))

if base:
    print('\n== (5) eq_s：購入が足した点（pointsBought）と州の工業化度（腕 B と比べる側・挙げた国・年代別）==')
    def eq_agg(dirp):
        r = collections.defaultdict(lambda: collections.defaultdict(float))
        for p in sorted(glob.glob(os.path.join(dirp, 'eq_s*.tsv'))):
            for d in rows(p):
                if d['nation'] not in PICK:
                    continue
                k = (d['nation'], decade(d['year']))
                r[k]['pts'] += f(d['pointsBought'])
                r[k]['n'] += 1
                r[k]['indMean'] += f(d['indMean'])
                r[k]['indMax'] = max(r[k]['indMax'], f(d['indMax']))
        return r
    a, b = eq_agg(arm), eq_agg(base)
    print('nation\tyears\tpointsB\tpointsBase\tratio\tindMeanB\tindMeanBase\tindMaxB\tindMaxBase')
    for k in sorted(set(a) | set(b), key=lambda k: (PICK.index(k[0]), k[1])):
        x, y = a.get(k, {}), b.get(k, {})
        pb, pr = x.get('pts', 0.0), y.get('pts', 0.0)
        ib = x.get('indMean', 0.0) / x['n'] if x.get('n') else float('nan')
        ir_ = y.get('indMean', 0.0) / y['n'] if y.get('n') else float('nan')
        print(f"{k[0]}\t{k[1]}-{k[1] + 19}\t{pb:.3f}\t{pr:.3f}\t{(pb / pr if pr else float('nan')):.3f}\t{ib:.3f}\t{ir_:.3f}\t{x.get('indMax', float('nan')):.2f}\t{y.get('indMax', float('nan')):.2f}")
