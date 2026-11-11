# (863) c71ip 突然変異の台本（使い捨ての木を書き換える・事前登録 [3]）：python -I mut863.py <E1|E2|E3|E4|F1> <木の根>
#   どの型も旗 ImportPayerLive の既定を 1 にしてから、BilateralMarket.cs の払い手の選びの 1 行を書き換える（行の数は変えない）。F1＝書き換えなし（旗 1 だけ＝正しい形の対照）。
#   ファイルはバイトで読み書きする（CR の数と BOM を保つ）。置き換えの元が 1 回だけ現れないなら止める。
import sys, os
mode, root = sys.argv[1], sys.argv[2]
def sub(rel, old, new):
    p = os.path.join(root, rel)
    with open(p, 'rb') as f: b = f.read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    if b.count(o) != 1: sys.exit(f'MUTFAIL {mode} {rel}: 元が {b.count(o)} 回')
    with open(p, 'wb') as f: f.write(b.replace(o, n))
with open(os.path.join(root, 'PriceOfBread.SimCore/Coefficients.cs'), 'rb') as f0: already = f0.read().count(b'public double ImportPayerLive = 1;') == 1
if not already: sub('PriceOfBread.SimCore/Coefficients.cs', 'public double ImportPayerLive = 0;', 'public double ImportPayerLive = 1;')   # 点火の後の木は既に 1
BM = 'PriceOfBread.SimCore/BilateralMarket.cs'
A = 'if (r2.MerchantPop is { } merchPay && !(c.ImportPayerLive > 0 && !(merchPay.Size > 1e-12)))'
B = 'else if (r2.Pops.Count > 0) payerOf[t2] = !(c.ImportPayerLive > 0) || r2.Pops.TrueForAll(p => p.Size > 1e-12) ? r2.Pops : r2.Pops.Exists(p => p.Size > 1e-12) ? r2.Pops.FindAll(p => p.Size > 1e-12) : null;'
if mode == 'E1':   # 条件を逆さ：人の居ない商人だけが [商人] で払い（手形あり）、人の居る商人の州は人の居る POP へ落ちる
    sub(BM, A, 'if (r2.MerchantPop is { } merchPay && !(c.ImportPayerLive > 0 && (merchPay.Size > 1e-12)))')
elif mode == 'E2':   # 落とした枝で手形を∞のまま（商人の債務が 0 なら振り出せる）
    sub(BM, B, 'else if (r2.Pops.Count > 0) { payerOf[t2] = !(c.ImportPayerLive > 0) || r2.Pops.TrueForAll(p => p.Size > 1e-12) ? r2.Pops : r2.Pops.Exists(p => p.Size > 1e-12) ? r2.Pops.FindAll(p => p.Size > 1e-12) : null; bill[t2] = r2.MerchantPop is { } mpb && !(mpb.Debt.Value > 0); }')
elif mode == 'E3':   # 落とした枝の払い手を別の州（清算の行の次の行の州）の人の居る POP にする
    sub(BM, B, 'else if (r2.Pops.Count > 0) payerOf[t2] = !(c.ImportPayerLive > 0) ? r2.Pops : tr[(t2 + 1) % tc].r.Pops.FindAll(p => p.Size > 1e-12) is { Count: > 0 } oth ? oth : null;')
elif mode == 'E3b':   # E3 の同じ国の版（E3 は別の国の POP で貨幣保存の番人が止めた＝U3 を全長で確かめる型）：落とした枝の払い手を同じ国の最初の州の人の居る POP にする
    sub(BM, B, 'else if (r2.Pops.Count > 0) payerOf[t2] = !(c.ImportPayerLive > 0) ? r2.Pops : w2.Regions[0].Pops.FindAll(p => p.Size > 1e-12) is { Count: > 0 } oth ? oth : null;')
elif mode == 'E4':   # 門を Size ＞1e-9 に変える（記録）
    sub(BM, A, A.replace('1e-12', '1e-9'))
    sub(BM, B, B.replace('1e-12', '1e-9'))
elif mode == 'F1': pass
else: sys.exit(f'MUTFAIL 型 {mode}')
print(f'MUTDONE {mode} {root}')
