# (805) c12cm 門 G：計器 cpeace_s の末尾 3 列（repExpectIndex・repMovedOverExpect・repFactors）で、崩壊の講和の期待の側を完全にして読み直す。読むだけ。
#   使い方: python -X utf8 -I gates_expect.py <ins フォルダ> [--seeds N] [--tol 1e-9]
#   事前登録 measurements/m805_20261103_prereg_cm.txt の [3] 門 G：
#     R_up' repMovedOverExpect（実際に移った賠償 repMovedIndex ÷ 期待の側 repExpectIndex）の最大 ≦ 1+tol ／ R_lo' 最小 ≧ 1−tol。全ての崩壊の事象（repExpectIndex>0）と、N>=2 の事象だけの両方を出す。
#     期待の側＝Σ 崩壊の講和 RepUnit0×saMax×式の取り分×(1−repCut)×条約港の倍率×r（計器が式で書く）。実際の側＝ResolvePeace が移した rep。
#   落ちた事象があれば一覧（種・年・月・敗者・比・repFactors）を出す（c10cp の採否は変えない＝記録）。比は計器の中で double で割って G10 で書かれる（印字の分解能 5e-10 < tol）。
#   [記録]：因子が 1 から外れる事象の数（repCut>0・条約港の倍率>1・r<1）＝門が自明に通る盤面でないことの証拠。
import sys, os, glob, csv, collections
args = sys.argv[1:]
d = args[0]
tol = float(args[args.index('--tol') + 1]) if '--tol' in args else 1e-9
def F(x):
    try: return float(x)
    except Exception: return float('nan')
ev = []
for f in sorted(glob.glob(os.path.join(d, 'cpeace_s*.tsv')), key=lambda p: int(os.path.basename(p)[len('cpeace_s'):-4])):
    seed = int(os.path.basename(f)[len('cpeace_s'):-4])
    for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
        r['seed'] = seed; ev.append(r)
if not ev:
    print('# 事象 0（cpeace_s が無い）'); sys.exit(0)
if 'repMovedOverExpect' not in ev[0]:
    print('# repMovedOverExpect の列が無い（(805) より前の木の出力）'); sys.exit(0)
E1 = [e for e in ev if F(e['repExpectIndex']) > 0 and F(e['repMovedOverExpect']) == F(e['repMovedOverExpect'])]
E2 = [e for e in E1 if int(e['nCollapse']) >= 2]
print(f'# {d}・種 {len(set(e["seed"] for e in ev))}・崩壊の事象 {len(ev)}（repExpectIndex>0: {len(E1)}・うち N>=2: {len(E2)}）・許容 {tol:g}')
ok = True
def gate(name, E, must=False):
    global ok
    q = [F(e['repMovedOverExpect']) for e in E]
    if not q:
        if must: ok = False   # (808) 合流：全ての事象の母集合が空＝期待の側が全て 0 に落ちる誤り（何もしない計器）でも通る門にしない
        print(f'{"落ちる" if must else "空"}\t{name}\t事象 0'); return
    hi, lo = max(q), min(q)
    up = hi <= 1 + tol; lw = lo >= 1 - tol
    ok = ok and up and lw
    print(f'{"通る" if up else "落ちる"}\tR_up\' {name} 実際÷期待 の最大 ≦1+{tol:g}\t{hi:.10g}\t事象 {len(q)}')
    print(f'{"通る" if lw else "落ちる"}\tR_lo\' {name} 同 最小 ≧1-{tol:g}\t{lo:.10g}')
gate('全ての崩壊の事象', E1, must=True)
gate('N>=2 の事象', E2)
# (808) 合流：期待≦0 の事象は実際に移った額も 0 であること（期待の側が 0 に落ちる誤りを母集合の外へ逃がさない）
Z = [e for e in ev if not (F(e['repExpectIndex']) > 0)]
Zbad = [e for e in Z if not (F(e['repMovedIndex']) == 0)]
ok = ok and not Zbad
print(f'{"落ちる" if Zbad else "通る"}\tR_zero 期待≦0 の事象で 実際に移った額 repMovedIndex＝0\t違反 {len(Zbad)}\t事象 {len(Z)}')
for e in Zbad[:20]:
    print('  ', e['seed'], e['year'], e['month'], e['loser'], e['repExpectIndex'], e['repMovedIndex'])
print(f'[記録] 事象 {len(ev)}・repExpectIndex が 0 以下の事象 {len(Z)}（うち repMovedIndex≠0 は {len(Zbad)}）')
bad = [e for e in E1 if abs(F(e['repMovedOverExpect']) - 1) > tol]
if bad:
    print(f'落ちた事象 {len(bad)} 件（上位 20 件・比が 1 から遠い順）：種・年・月・敗者・nCollapse・比・repFactors')
    for e in sorted(bad, key=lambda e: -abs(F(e['repMovedOverExpect']) - 1))[:20]:
        print('  ', e['seed'], e['year'], e['month'], e['loser'], e['nCollapse'], e['repMovedOverExpect'], e['repFactors'])
print('総合\t' + ('全て通る' if ok else '落ちる門あり'))
# 記録：因子が 1 から外れる事象（門が自明に通る盤面でない証拠）
cut = tp = rr = tot = 0
for e in E1:
    fs = [x.split(':') for x in e['repFactors'].split('/') if x]
    tot += len(fs)
    for c_, t_, r_ in fs:
        if F(c_) > 0: cut += 1
        if F(t_) > 1 + 1e-12: tp += 1
        if F(r_) < 1 - 1e-12: rr += 1
print(f'[記録] 講和の数 {tot}：repCut>0 {cut}・条約港の倍率>1 {tp}・r<1 {rr}')
