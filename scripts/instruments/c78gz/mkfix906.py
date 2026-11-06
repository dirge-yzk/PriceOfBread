# (906) 自己試験の小さな盤面（r833/mkfix.py の型）。期待は盤面の意図から書く（selftest906.py の EXP_JUMP・EXP_ZERO・台本の出力から写さない）。
H = ['quarter','year','nationIndex','nation','totalPopulationK','populationK','nominalGdpPerYear','nationUid']
rows = []
def series(uid, name, start, gdps, pops=None):
    pops = pops or [1000] * len(gdps)
    for k, (g, p) in enumerate(zip(gdps, pops)):
        q = start + k; rows.append([q, 1783 + q // 4, 0, name, p, p, g, uid])
series('Z', 'z先頭', 0, [100] * 40)                                                  # 全体の最初の四半期を 0 にする国（初出の違いを見る）
series('A', 'a建国0が7行', 10, [0] * 7 + [100] * 6)                                   # (b) 旧 0 件・新 1 件（跳びは無い＝0 から正）
series('B', 'b建国後0からの跳び', 0, [1, 0, 0, 0, 0, 0.5, 100])                       # 0 の続き 4 行＋跳び 200 倍＝0の続き
series('C', 'c建国の直後', 0, [0, 0, 0, 0.5, 100])                                    # q-first=4＝建国の直後
series('D', 'd窓の外', 0, [0, 0, 0, 0] + [1] * 10 + [100])                            # 窓 8 行に 0 無し＝それ以外
series('E', 'e三行だけ', 0, [1, 0, 0, 0, 1, 1, 100])                                  # 窓の 0 が 3 行＝それ以外
series('F', 'f痩せ', 0, [1] * 7 + [100], [1000] * 7 + [10000])                        # 痩せ
series('G', 'g0の続きと痩せ', 0, [1, 0, 0, 0, 0, 1, 100], [1000] * 6 + [10000])         # 0の続き（痩せに優先）
series('I', 'i建国2から跳び', 20, [0.5, 100])                                         # 初出が全体の先頭でない＝建国の直後
series('J', 'j旧でも見える0', 0, [0] * 12)                                            # 旧 1 区間（8 行）・新 12 行
series('K', 'k窓の端', 0, [1, 1, 0, 0, 0, 0, 1, 1, 1, 100])                          # 直前 8 行に 0 が 4 行ちょうど＝0の続き
series('L', 'l窓の外の端', 0, [0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 100])             # 直前 8 行に 0 無し＝それ以外
series('M', 'm窓7で落ちる', 0, [0, 0, 0, 0, 1, 1, 1, 1, 100])                      # 直前 8 行の先頭が 4 つ目の 0（WIN=7 なら 3 行＝それ以外）
series('N', 'n窓9で入る', 0, [0, 1, 0, 0, 0, 1, 1, 1, 1, 100])                        # 直前 8 行に 0 が 3・9 行目に 4 つ目（WIN=9 なら 0の続き）
rows.sort(key=lambda r: (r[0], r[7]))
with open('fixture/nation_s1.tsv', 'wb') as f:
    f.write(('\t'.join(H) + '\n').encode())
    for r in rows: f.write(('\t'.join(str(v) for v in r) + '\n').encode())
