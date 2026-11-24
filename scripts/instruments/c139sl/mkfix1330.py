# (1330) c139sl：合成の盤面（nation_s1.tsv・region_s1.tsv）を fixture/ に書く。期待は selftest1330.py に手で書く（この台本の出力から写していない）。
#   型の根拠：ラヘジ＝m1326 q3 の S11 s16 1875（人口 93→15865・名目 130.7 倍）／キサンガニ＝c138ssB s5 q527→528（産出 1.349→1509.6・名目 839.5 倍・人口比 1.006）／ウジジ＝N1 s6 1924（名目 0 の区間の後の跳び）。
import os
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, 'fixture'); os.makedirs(OUT, exist_ok=True)
NC = 'quarter\tyear\tnationIndex\tnation\ttotalPopulationK\tpopulationK\tnominalGdpPerYear\tnationUid\tpriceLevel'
RC = 'quarter\tyear\tnationIndex\tnation\tregion\tfactoryOutputRefValuePerMonth\textractionOutputRefValuePerMonth'
nat = []; reg = []
def case(tag, idx, g, pop, out, first=0, last=10):
    """q first..last の行。g・pop・out は q→値の関数。out は州の産出の 1 本（工場）。"""
    for q in range(first, last + 1):
        nat.append(f'{q}\t{1783 + q // 4}\t{idx}\t{tag}\t{pop(q)}\t{pop(q)}\t{g(q)}\tu_{tag}\t1.0')
        reg.append(f'{q}\t{1783 + q // 4}\t{idx}\t{tag}\t{tag}_r1\t{out(q)}\t0')
flat = lambda v: (lambda q: v)
jump = lambda lo, hi: (lambda q: hi if q == 10 else lo)
def zeros(ranges, pos, tiny):
    return lambda q: 0 if any(a <= q <= b for a, b in ranges) else (pos if q == 10 else tiny)
case('A', 10, jump(1000.0, 200000.0), flat(1000.0), flat(100.0), first=8)                                   # 建国の直後（初出 q8・跳び q10）
case('B', 20, jump(1000.0, 130700.0), lambda q: 15865.0 if q == 10 else 93.0, flat(100.0))                  # ラヘジ型：人口の比 170.6
case('C', 30, zeros([(0, 5)], 200.0, 1.0), flat(1000.0), flat(100.0))                                       # ウジジ型：0 が q0..5（6 行）・q6..9 は 1・跳び 200 倍（窓 q2..9 に 0 が 4 行）
case('D', 40, jump(1000.0, 839500.0), lambda q: 3341.9 if q == 10 else 3322.8, lambda q: 1509.6 if q == 10 else 1.349)   # キサンガニ型：産出 ×1119・名目 ×839.5・人口 ×1.006
case('E', 50, jump(1000.0, 200000.0), flat(1000.0), flat(100.0))                                            # どれにも当たらない
case('F', 60, zeros([(6, 8)], 200.0, 1.0), flat(1000.0), flat(100.0))                                      # 0 が 3 行だけ（q6..8）＝(ii) でない
case('G', 70, zeros([(0, 3)], 200.0, 1.0), flat(1000.0), flat(100.0))                                      # 0 が 4 行だが窓（q2..9）の外へ半分（窓の中は q2・q3 の 2 行）＝(ii) でない
case('H1', 80, jump(1000.0, 100000.0), flat(1000.0), jump(100.0, 1000.0))                                   # 産出 ×10・名目 ×100（境界・名目÷産出 = 10）＝(iii)
case('H2', 90, jump(1000.0, 100000.0), flat(1000.0), jump(100.0, 990.0))                                    # 産出 ×9.9＝(iii) でない
case('I1', 100, jump(1000.0, 200000.0), lambda q: 10000.0 if q == 10 else 1000.0, flat(100.0))              # 人口の比 ちょうど 10＝(i)
case('I2', 110, jump(1000.0, 200000.0), lambda q: 9900.0 if q == 10 else 1000.0, flat(100.0))               # 人口の比 9.9＝(i) でない
case('J', 120, jump(1000.0, 200000.0), flat(1000.0), lambda q: 500.0 if q == 10 else 0.0)                    # 前の産出が 0・今が正＝(iii)
case('K', 130, jump(1000.0, 200000.0), lambda q: 20000.0 if q == 10 else 1000.0, jump(100.0, 100000.0))     # 人口 ×20 と産出 ×1000 の両方＝(i) が先
case('L', 140, zeros([(0, 5)], 200.0, 1.0), flat(5.0), flat(100.0))                                         # 0 の区間だが人口 5 千人（<10）＝(ii) でない
case('M', 150, zeros([(2, 3), (5, 6)], 200.0, 1.0), flat(1000.0), flat(100.0))                              # 0 が計 4 行だが連続でない（q2・3 と q5・6）＝(ii) でない
case('N', 160, jump(1000.0, 200000.0), flat(1000.0), jump(100.0, 1000.0))                                    # 産出 ×10・名目 ×200（名目÷産出 = 20 > 10）＝(iii) でない
case('O', 170, jump(1000.0, 100000.0), flat(1000.0), jump(100.0, 500000.0))                                  # 産出 ×5000・名目 ×100（名目÷産出 = 0.02 < 0.1）＝(iii) でない
case('P', 180, jump(1000.0, 1000000000.0), flat(1000.0), lambda q: 500.0 if q == 10 else 0.0)                 # (1337) 前の産出が 0・今が正・名目 ×1e6＝(iii) でない（tot=inf の枝は偽）
case('Q', 190, zeros([(0, 5)], 200.0, 1.0), flat(1000.0), jump(100.0, 5000.0))                            # (1337) (ii)（0 の区間が窓に 4 行以上）と (iii)（産出 ×50・名目÷産出 4）が両方当たる＝(ii) が先
for fn, hd, rows in (('nation_s1.tsv', NC, nat), ('region_s1.tsv', RC, reg)):
    with open(os.path.join(OUT, fn), 'wb') as f: f.write(('\n'.join([hd] + rows) + '\n').encode('utf-8'))
print('fixture', len(nat), len(reg))
