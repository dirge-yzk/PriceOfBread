# (949) 下がりの自己試験の小さな盤面。期待は盤面の意図から書く（selftest949.py の EXP・台本の出力から写していない）。使い方: cd c84gz && python -I mkfix949.py
H = ['quarter','year','nationIndex','nation','totalPopulationK','populationK','nominalGdpPerYear','nationUid']
rows = []
def series(uid, name, start, gdps, pops=None):
    pops = pops or [1000] * len(gdps)
    for k, (g, p) in enumerate(zip(gdps, pops)):
        q = start + k; rows.append([q, 1783 + q // 4, 0, name, p, p, g, uid])
series('A', 'a安定の下がり', 0, [100] * 8 + [0.5])                     # 人口は変わらず 200 分の 1＝それ以外
series('B', 'b痩せの下がり', 0, [100] * 8 + [0.5], [1000] * 8 + [50])  # 人口の比 0.05＝分母の痩せ
series('C', 'c建国の直後の下がり', 0, [100, 0.5])                       # 初出から 1 四半期＝建国の直後
series('D', 'd99分の1', 0, [99] * 8 + [1] + [1] * 2)                    # 99 倍＝数えない
series('E', 'e100分の1ちょうど', 0, [100] * 8 + [1])                     # 100 倍ちょうど＝数える（それ以外）
series('F', 'f0への落ち', 0, [100] * 8 + [0])                           # 0 以下への落ち＝別の列
series('G', 'g上がりだけ', 0, [1] * 8 + [200])                          # 上がりは下がりに数えない
rows.sort(key=lambda r: (r[0], r[7]))
with open('fixture/nation_s1.tsv', 'wb') as f:
    f.write(('\t'.join(H) + '\n').encode())
    for r in rows: f.write(('\t'.join(str(v) for v in r) + '\n').encode())
