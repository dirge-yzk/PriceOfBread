# (833) 両側の門の小さな盤面（事前登録 [5] の (a)〜(e)）
H = ['quarter','year','nationIndex','nation','totalPopulationK','populationK','nominalGdpPerYear','nationUid']
rows = []
def series(uid, name, start, gdps, pops):
    for k, (g, p) in enumerate(zip(gdps, pops)):
        q = start + k; rows.append([q, 1783 + q // 4, 0, name, p, p, g, uid])
S = [1000.0] * 10
series('A', 'a安定200倍', 0, [100]*8 + [20000, 100], S)                          # (a) それ以外
series('B', 'b痩せ', 0, [100]*6 + [1, 150, 150, 150], [1000]*6 + [13, 1000, 1000, 1000])   # (b) 痩せ
series('C', 'c建国3', 10, [100]*3 + [20000], [1000]*4)                          # (c) 建国の直後
series('D', 'd建国5', 10, [100]*5 + [20000], [1000]*6)                          # (c) それ以外
series('E', 'e比9.9', 0, [100]*6 + [20000], [1000]*6 + [9900])                  # (d) それ以外
series('F', 'f比10', 0, [100]*6 + [20000], [1000]*6 + [10000])                  # (d) 痩せ
series('G', 'g比0.1', 0, [100]*6 + [20000], [1000]*6 + [100])                   # (d) 痩せ
# (839) 合流が反証役（low）の指摘で端の 4 系列を足した（区分の規則は変えない・期待は盤面の意図から書く）
series('I', 'i建国4', 10, [100]*4 + [20000], [1000]*5)                         # q-first=4＝建国の直後（K の端）
series('J', 'j痩せ無限', 0, [100]*6 + [20000], [1000]*5 + [0, 1000])           # pop_p=0・pop_x>0＝比 ∞＝痩せ
series('K', 'k人口非有限', 0, [100]*6 + [20000], [1000]*6 + ['NaN'])           # 人口が非有限＝それ以外
series('L', 'l人口0と0', 0, [100]*6 + [20000], [1000]*5 + [0, 0])              # 0÷0＝それ以外
series('H', 'h非有限1e10', 0, [100]*5 + ['NaN', 100, 2e12], [1000]*8)            # (e) 非有限 1・1e10 1・それ以外
rows.sort(key=lambda r: (r[0], r[7]))
with open('fixture/nation_s1.tsv', 'wb') as f:
    f.write(('\t'.join(H) + '\n').encode())
    for r in rows: f.write(('\t'.join(str(v) for v in r) + '\n').encode())
