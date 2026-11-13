# Coefficients.cs を元の順に連続した切片へ分ける。使い方: python -X utf8 -I scripts/instruments/c106cf/split.py <木>
# 元は git show ffb383805 の版。BOM と LF を保つ。自己検査：部品の本体を順に繋ぐと元の本体とバイト一致。
import subprocess, sys, os
root = sys.argv[1]
BASE = 'ffb3838055a49bc32ef0fa901b473ddeccd52253'
src = subprocess.check_output(['git', '-C', root, 'show', BASE + ':PriceOfBread.SimCore/Coefficients.cs'])
BOM = b'\xef\xbb\xbf'
assert src.startswith(BOM)
L = src[3:].split(b'\n')
assert L[15] == b'public sealed class Coefficients' and L[16] == b'{' and L[-1] == b'' and L[-2] == b'}'
# 切片の開始行（1始まり）と名前。1枚目は Coefficients.cs
PARTS = [(18, 'Coefficients.cs'), (456, 'Coefficients.p02_borrow_polity_epidemic.cs'),
         (736, 'Coefficients.p03_debt_money.cs'), (1073, 'Coefficients.p04_trade.cs'),
         (1498, 'Coefficients.p05_port_insurance.cs'), (1641, 'Coefficients.p06_rank_discovery.cs'),
         (1973, 'Coefficients.p07_wiring_prewarm.cs'), (2368, 'Coefficients.p08_audit_soldiers.cs'),
         (2875, 'Coefficients.p09_kingship_tax.cs'), (3231, 'Coefficients.p10_ai_silver_overrides.cs')]
bodyEnd = len(L) - 2          # 本体の最後の行（1始まり）= 3650
head = L[:15]                 # 1..15 using/namespace/doc
HEAD_ALL = head
out = []
joined = []
for k, (s, name) in enumerate(PARTS):
    e = (PARTS[k + 1][0] - 1) if k + 1 < len(PARTS) else bodyEnd
    body = L[s - 1:e]
    joined += body
    if k == 0:
        lines = L[:15] + [b'public sealed partial class Coefficients', b'{'] + body + [b'}', b'']
    else:
        lines = [b'using System.Collections.Generic;', b'using System.Linq;', b'namespace PriceOfBread.SimCore;', b'',
                 b'public sealed partial class Coefficients', b'{'] + body + [b'}', b'']
    data = b'\n'.join(lines)
    if k > 0: data = BOM + data
    else: data = BOM + data
    out.append((name, data, s, e))
assert joined == L[17:bodyEnd], 'body mismatch'
assert b'\n'.join(joined) == b'\n'.join(L[17:bodyEnd])
# 1枚目の先頭は BOM + 元の1行目（元の L[0] には BOM が無い＝src[3:]から）
for name, data, s, e in out:
    open(os.path.join(root, 'PriceOfBread.SimCore', name), 'wb').write(data)
    print(f'{name}\t{s}-{e}\t{len(data.decode("utf-8"))}chars')
