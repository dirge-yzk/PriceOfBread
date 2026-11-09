# (996) c92gf：合成の盤面（nation_s1.tsv・region_s1.tsv）を fixture/ に書く。期待は selftest1002.py に手で書く（この台本の出力から写していない）。
import os
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, 'fixture'); os.makedirs(OUT, exist_ok=True)
NC = 'quarter\tyear\tnationIndex\tnation\ttotalPopulationK\tpopulationK\tnominalGdpPerYear\tnationUid\tpriceLevel'
RC = 'quarter\tyear\tnationIndex\tnation\tregion\tfactoryOutputRefValuePerMonth\textractionOutputRefValuePerMonth'
nat = []; reg = []
def case(tag, idx, regs_prev, regs_cur, ratio, pop_cur=1000.0, price_cur=1.0, idx_cur=None, first=0, last=10):
    """q first..9 は regs_prev・q10 が regs_cur の跳び。regs＝{州: (工場, 採取)}"""
    idx_cur = idx if idx_cur is None else idx_cur
    for q in range(first, last + 1):
        cur = (q == 10)
        g = 1000.0 * (ratio if cur else 1.0); p = pop_cur if cur else 1000.0; pl = price_cur if cur else 1.0
        ix = idx_cur if cur else idx
        nat.append(f'{q}\t{1783 + q // 4}\t{ix}\t{tag}\t{p}\t{p}\t{g}\tu_{tag}\t{pl}')
        for k, (fa, ex) in (regs_cur if cur else regs_prev).items():
            reg.append(f'{q}\t{1783 + q // 4}\t{ix}\t{tag}\t{tag}_{k}\t{fa}\t{ex}')
case('A', 10, {'r1': (100, 0)}, {'r1': (110, 0), 'r2': (20000, 0)}, 200.0)
case('B', 20, {'r1': (100, 0)}, {'r1': (20000, 0)}, 200.0)
case('C', 30, {'r1': (100, 0), 'r2': (100, 0)}, {'r1': (100, 0), 'r2': (100, 0)}, 200.0, price_cur=200.0)
case('D', 40, {'r1': (0, 0)}, {'r1': (0, 0), 'r2': (20000, 0)}, 200.0)
case('E', 50, {'r1': (0, 100)}, {'r1': (0, 105), 'r2': (0, 20000)}, 200.0)
case('F', 60, {'r1': (100, 0)}, {'r1': (110, 0), 'r2': (20000, 0)}, 200.0, pop_cur=50.0)
case('G', 70, {'r1': (100, 0)}, {'r1': (110, 0), 'r2': (20000, 0)}, 200.0, idx_cur=80)
case('H', 90, {'r1': (100, 0)}, {'r1': (20000, 0), 'r2': (100, 0)}, 200.0)
case('I', 100, {'r1': (100, 0), 'r2': (20000, 0)}, {'r1': (100, 0)}, 1.0 / 200.0)
case('Jdecoy', 80, {'d1': (50000, 0)}, {'d1': (50000, 0)}, 1.0, last=9)   # G の M4 の罠：q<=9 の nationIndex 80 の持ち主（跳びなし）
case('K', 110, {'r1': (100, 0)}, {'r1': (20000, 0)}, 200.0, first=8)   # 建国の直後（初出 q8・跳び q10）
case('P', 120, {'r1': (100, 0)}, {'r1': (100, 0), 'r2': (1, 0)}, 200.0, price_cur=200.0)   # (1002) 1 単位の州が入る＋物価だけ 200 倍（条件 (iii) で〈それ以外〉）
case('Q', 130, {'r1': (100, 0), 'r2': (1, 0)}, {'r1': (100, 0)}, 1.0 / 200.0, price_cur=1.0 / 200.0)   # 州が 1 つ抜ける＋物価だけ 1/200
case('R', 140, {'r1': (100, 0)}, {'r1': (200, 0)}, 12.0)   # (1002) 州の集合は同じ・州の和 ×2・名目 ×12（(iii) は真・(i) が偽＝M6 を赤にする）
case('S', 150, {'r1': (100, 0)}, {'r1': (161.3, 0), 'r2': (18500, 0)}, 211.7)   # (1002) シャム型（m988 N2 種 7 q445 の実測：名目 ×211.7・州の和 ×186.6・共通 ×1.613・1/1→2）
for fn, hd, rows in (('nation_s1.tsv', NC, nat), ('region_s1.tsv', RC, reg)):
    with open(os.path.join(OUT, fn), 'wb') as f: f.write(('\n'.join([hd] + rows) + '\n').encode('utf-8'))
print('fixture', len(nat), len(reg))
