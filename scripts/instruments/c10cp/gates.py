# (791) c10cp 門の読み手：計器 cpeace_s（崩壊の事象〔月×敗者〕ごとに 1 行・旗 0 でも旗 1 でも同じ列）から、事前登録の門を読む。
#   使い方: python -I gates.py <ins フォルダ> [--seeds 1..10] [--p0 0.xx] [--l1 0.80]
#   事象 N>=2（nCollapse>=2）だけに門を当てる。比は repRealIndex／repNewIndex（期待は式で書いた取り分から・エンジンの取り分を読まない）。
#   門（事前登録 measurements/m791_20261103_prereg_cp.txt）：
#     R_up 賠償の元の実際÷1回分 ≦1.5（全事象）／R_lo ≧0.999（全事象）／B_up・B_lo 植民地の奪取の予算も同じ／
#     C_up 本国の割譲は集合で 1 回以下（全事象）／C_lo 門を通る講和を持つ事象のうち 1 回以上割譲した割合 ≧ p0−0.10（p0＝旗 0 の紙の台本の値）／
#     U2 集合の中の崩壊でない講和＝0／U3 押しが正なのに取り分 0 の勝者＝0／
#     L1（働いた）Σ実際÷Σ旧の式 ≦ l1（旗 0 は 1.0 ＝落ちる）。
import sys, os, glob, csv, collections, statistics as st
args = sys.argv[1:]
d = args[0]
def opt(name, default):
    return float(args[args.index(name) + 1]) if name in args else default
p0 = opt('--p0', 0.0); l1 = opt('--l1', 0.80)
def F(x):
    try: return float(x)
    except: return float('nan')
ev = []
for f in sorted(glob.glob(os.path.join(d, 'cpeace_s*.tsv')), key=lambda p: int(os.path.basename(p)[len('cpeace_s'):-4])):
    seed = int(os.path.basename(f)[len('cpeace_s'):-4])
    for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
        r['seed'] = seed; ev.append(r)
N = [int(e['nCollapse']) for e in ev]
E = [e for e in ev if int(e['nCollapse']) >= 2]
print(f'# {d}・種 {len(set(e["seed"] for e in ev))}・崩壊の事象 {len(ev)}（N=1: {sum(1 for n in N if n == 1)}・N>=2: {len(E)}）・N の分布 {sorted(collections.Counter(N).items())}')
ok = True
def gate(name, val, cond, note=''):
    global ok
    p = bool(cond)
    ok = ok and p
    print(f'{"通る" if p else "落ちる"}\t{name}\t{val}\t{note}')
rr = [F(e['repRealIndex']) / F(e['repNewIndex']) for e in E if F(e['repNewIndex']) > 0]
bb = [F(e['budRealIndex']) / F(e['budNewIndex']) for e in E if F(e['budNewIndex']) > 0]
if rr:
    gate('R_up 賠償の元 実際÷1回分 の最大 ≦1.5', f'{max(rr):.4f}', max(rr) <= 1.5, f'事象 {len(rr)}')
    gate('R_lo 同 最小 ≧0.999', f'{min(rr):.4f}', min(rr) >= 0.999)
if bb:
    gate('B_up 植民地の奪取の予算 実際÷1回分 の最大 ≦1.5', f'{max(bb):.4f}', max(bb) <= 1.5, f'事象 {len(bb)}')
    gate('B_lo 同 最小 ≧0.999', f'{min(bb):.4f}', min(bb) >= 0.999)
cm = max([int(e['cedeRealCount']) for e in E] or [0])
gate('C_up 本国の割譲 集合あたり 最大 ≦1', cm, cm <= 1, f'N>=2 の事象 {len(E)}')
Ec = [e for e in E if int(e['cedeEligCount']) >= 1]
p1 = sum(int(e['cedeRealCount']) >= 1 for e in Ec) / max(1, len(Ec))
gate('C_lo 門を通る講和を持つ事象で 1 回以上割譲した割合 ≧ p0−0.10', f'{p1:.3f}（p0={p0:.3f}・事象 {len(Ec)}）', p1 >= p0 - 0.10)
u2 = sum(int(e['u2Count']) for e in ev); u3 = sum(int(e['u3Count']) for e in ev)
gate('U2 集合の中の崩壊でない講和 =0', u2, u2 == 0)
gate('U3 押しが正で取り分 0 の勝者 =0', u3, u3 == 0)
sr = sum(F(e['repRealIndex']) for e in E); so = sum(F(e['repOldIndex']) for e in E)
gate(f'L1 働いた：Σ実際÷Σ旧の式 ≦{l1}', f'{sr / so:.4f}' if so > 0 else 'nan', so > 0 and sr / so <= l1, 'N>=2 の事象の合計')
print('総合\t' + ('全て通る' if ok else '落ちる門あり'))
# 記録（門にしない）
UK = 'イギリス'
U = [e for e in ev if e['loser'] == UK and 1784 <= int(e['year']) <= 1815]
print(f'[記録] 英が敗者の崩壊の事象（1784〜1815）{len(U)}・講和の数 {sum(int(e["nCollapse"]) for e in U)}・Σ賠償の実際 {sum(F(e["repRealIndex"]) for e in U):.0f}（旧の式 {sum(F(e["repOldIndex"]) for e in U):.0f}）')
print(f'[記録] 全事象 Σ賠償の実際 {sum(F(e["repRealIndex"]) for e in ev):.0f}・旧の式 {sum(F(e["repOldIndex"]) for e in ev):.0f}・奪った植民地 {sum(int(e["capturedCount"]) for e in ev)}・割譲 {sum(int(e["cedeRealCount"]) for e in ev)}')
mixed = sum(1 for e in ev if int(e['nOther']) > 0)
print(f'[記録] 同じ月・同じ敗者に崩壊でない講和も有った事象 {mixed}')
