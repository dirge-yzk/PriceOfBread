# (1081) c103dm：DM-B の門 T0〜T3（事前登録 measurements/m1081_20261112_prereg_dm.txt の §門）。読むだけ。
#   python -X utf8 -I gate_dm.py <成績表 b10_*.txt> <計器の出力フォルダ ins> [--seeds 1-10] [--flowseeds 1-3]
#   門は〈0 でない行が 0〉の形＝欠けた表・欠けた列・行の少ない表は緑にせず〈欠け〉で赤にする（何もしない門にしない）。
#   nation_s の nan：反乱の国の最初の四半期の計器の nan（cm＝0 で割る・1 行に nan の欄が 50 以上）は数えない。それ以外の nan は違反に数える。
import sys, os, csv, math
csv.field_size_limit(1 << 30)
args = [a for a in sys.argv[1:] if not a.startswith('--')]
opt = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--') and '=' in a)
rep, ins = args[0], args[1]
def rng(s): lo, hi = map(int, s.split('-')); return list(range(lo, hi + 1))
seeds = rng(opt.get('seeds', '1-10')); fseeds = rng(opt.get('flowseeds', '1-3'))
red = []
def bad(g, why): red.append(g); print(f'  ✕ {g}: {why}')

def isnan(v): return v.strip().lower() in ('nan', '-nan', '+nan')
def num(v):
    try: return float(v)
    except ValueError: return float('nan')

# T0 見出し
print('== T0 見出し（成績表の〈★係数の上書き（POB_COEF）〉の行に 2 つの名が =0 で出る）')
hdr = None
if os.path.exists(rep):
    with open(rep, encoding='utf-8') as f:
        for ln in f:
            if ln.startswith('★係数の上書き（POB_COEF）:'): hdr = ln.rstrip('\n'); break
print(f'  行：{hdr}')
if hdr is None: bad('T0', '行が無い')
else:
    toks = hdr.split(':', 1)[1].split()
    for nm in ('MegaprojectEnabled=0', 'FormationLegitimacyBoost=0'):
        if nm not in toks: bad('T0', f'{nm} が無い')

def scan(table, cols, floor, nan_rows_ok=False, seedlist=seeds):
    tot = {c: 0 for c in cols}; nanart = 0; rows = 0
    for s in seedlist:
        p = os.path.join(ins, f'{table}{s}.tsv')
        if not os.path.exists(p): bad(table, f'種 {s} の表が無い（欠け）'); continue
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r)
            idx = {}
            for c in cols:
                if c not in h: bad(table, f'種 {s} に列 {c} が無い（欠け）')
                else: idx[c] = h.index(c)
            n = 0
            for row in r:
                n += 1
                art = None
                for c, i in idx.items():
                    v = row[i] if i < len(row) else ''
                    if isnan(v):
                        if nan_rows_ok:
                            if art is None: art = sum(1 for x in row if isnan(x)) >= 50
                            if art: nanart += 1; continue
                        tot[c] += 1
                    elif num(v) != 0.0: tot[c] += 1
            rows += n
            if n < floor: bad(table, f'種 {s} の行 {n} が床 {floor} 未満（欠け）')
    return tot, rows, nanart

print('== T1 払い（0 でない行の数・和でなく行で数える）')
t, n, art = scan('nation_s', ['monumentPaidPerMonth'], 10000, nan_rows_ok=True)
print(f'  nation_s 行 {n}・反乱の最初の四半期型の nan（数えない）{art}・0 でない行 {t}')
for c, v in t.items():
    if v: bad('T1', f'nation_s:{c} 0 でない行 {v}')
t, n, _ = scan('fiscal_s', ['claimMonumentBuildPerMonth', 'claimMegaForeignYieldAtQuarterEnd', 'ledgerMegaprojectPerMonth'], 5000)
print(f'  fiscal_s 行 {n}・0 でない行 {t}')
for c, v in t.items():
    if v: bad('T1', f'fiscal_s:{c} 0 でない行 {v}')

print('== T2 着工（scalars_s megaStartedCount の 0 でない行）')
t, n, _ = scan('scalars_s', ['megaStartedCount'], 5000)
print(f'  scalars_s 行 {n}・0 でない行 {t}')
if t.get('megaStartedCount'): bad('T2', f'0 でない行 {t["megaStartedCount"]}')

print(f'== T3 経路（flow_s の sourceFile=Megaprojects.cs の行・種 {fseeds[0]}〜{fseeds[-1]}）')
for s in fseeds:
    p = os.path.join(ins, f'flow_s{s}.tsv')
    if not os.path.exists(p): bad('T3', f'種 {s} の flow_s が無い（欠け）'); continue
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        if 'sourceFile' not in h: bad('T3', f'種 {s} に sourceFile が無い'); continue
        i = h.index('sourceFile'); mega = other = 0; files = set()
        for row in r:
            if row[i] == 'Megaprojects.cs': mega += 1
            else: other += 1; files.add(row[i])
    print(f'  種 {s}：Megaprojects.cs {mega} 行・他 {other} 行（{len(files)} ファイル）')
    if mega: bad('T3', f'種 {s} Megaprojects.cs {mega} 行')
    if other < 100000 or len(files) < 10: bad('T3', f'種 {s} の他の経路が少ない（欠け）')

print('== T4 建国の威信：構成の門（T0 の FormationLegitimacyBoost=0 で代える）')
print(f'判定: {"赤 " + "・".join(sorted(set(red))) if red else "緑（T0〜T3 全て）"}')
sys.exit(1 if red else 0)
