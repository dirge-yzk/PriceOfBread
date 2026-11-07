# (930) c81la：G-B1 ③ の分母の前提（(909) [4]）を基線の q0 で種ごとに確かめる。読むだけ。
#   使い方: python premise930.py <生のフォルダ> <種の起点 1|11>
#   前提＝名簿 7 国とも 貴族の実際＝NE（持ち主が貴族の非賦存）＋E（賦存）・教会と農民の実際 0・市民の実際＝送金・持ち主が貴族でない非賦存の地代 0。
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gate930 as G

folder = sys.argv[1]; sb = int(sys.argv[2]) if len(sys.argv) > 2 else 1
worst = {'noble_rel': 0.0, 'clergy_abs': 0.0, 'peasant_abs': 0.0, 'cap_minus_remit_abs': 0.0, 'ne_nonnoble_abs': 0.0}
for s in range(sb, sb + 10):
    ls, gdp = G.q0(folder, s)
    for n in G.ROSTER:
        r = ls[n]; f = lambda k: G.f(r, k)
        ne, e = f('rentNonEndowByOwnerClassNoblePerQuarter'), f('rentEndowPerQuarter')
        nob = f('landRentActualNoblePerQuarter')
        rel = abs(nob - (ne + e)) / max(1e-300, abs(ne + e))
        worst['noble_rel'] = max(worst['noble_rel'], rel)
        worst['clergy_abs'] = max(worst['clergy_abs'], abs(f('landRentActualClergyPerQuarter')))
        worst['peasant_abs'] = max(worst['peasant_abs'], abs(f('landRentActualPeasantPerQuarter')))
        worst['cap_minus_remit_abs'] = max(worst['cap_minus_remit_abs'], abs(f('landRentActualCapitalistPerQuarter') - f('colonialRemitPerQuarter')))
        nn = sum(abs(f(f'rentNonEndowByOwnerClass{c}PerQuarter')) for c in ('Clergy', 'Peasant', 'Capitalist', 'Other'))
        worst['ne_nonnoble_abs'] = max(worst['ne_nonnoble_abs'], nn)
print(f'# premise930 {folder} 種 {sb}〜{sb+9}（名簿 7 国・q0 の最悪）')
for k, v in worst.items(): print(f'{k}\t{v:.3g}')
