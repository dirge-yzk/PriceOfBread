# (983) 突然変異：使い捨てに元のファイルを写して書き換え→LandsaleInstrumentTests を回す→元へ戻す（バイトで読み書き）。
# 使い方：python mut983.py <worktree> <出力 txt>   各行：名前 / 赤の試験 / 緑か赤か
import subprocess, sys, shutil, re, os
wt, out = sys.argv[1], sys.argv[2]
LS = os.path.join(wt, 'PriceOfBread.SimCore', 'InstrumentsLandsale.cs')
FI = os.path.join(wt, 'PriceOfBread.SimCore', 'Fiscal.cs')
M = [
 ('M1 受け口を外す(Fiscal.Step の呼び口)', FI, 'Instruments.LandsaleStep(w);', '/*Instruments.LandsaleStep(w);*/'),
 ('M2 rp を 1 に固定', LS, 'return held >= claims ? 1.0 : held / claims;', 'return 1.0;'),
 ('M3 寄せの枠を買い手に入れる', LS, 'if (fallback > f0) continue;', ''),
 ('M4 輸出の地代を二重に足す', LS, 'a.QExport += ls.MExp;', 'a.QExport += 2 * ls.MExp;'),
 ('M5a k<4 を外す(持ち分の判定の枠の数)', LS, 'System.Math.Min(LdPrivateSlots, ls?.Length ?? LdPrivateSlots)', '(ls?.Length ?? LdPrivateSlots)'),
 ('M5b k<4 を外す(買い手の枠の周回)', LS, 'for (int k = 0; k < LdPrivateSlots; k++)', 'for (int k = 0; k < (s.LandShares?.Length ?? LdPrivateSlots); k++)'),
]
res = []
for name, path, old, new in M:
    orig = open(path, 'rb').read()
    txt = orig.decode('utf-8')
    assert txt.count(old) == 1, (name, txt.count(old))
    open(path, 'wb').write(txt.replace(old, new).encode('utf-8'))
    try:
        p = subprocess.run(['dotnet', 'test', os.path.join(wt, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c89ls_art',
                            '--filter', 'FullyQualifiedName~LandsaleInstrumentTests'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        o = p.stdout + p.stderr
        red = sorted(set(re.findall(r'失敗 PriceOfBread\.Tests\.LandsaleInstrumentTests\.(\w+)', o)))
        build_err = 'error CS' in o
        res.append((name, p.returncode, red, build_err))
    finally:
        open(path, 'wb').write(orig)
with open(out, 'w', encoding='utf-8') as f:
    for name, rc, red, be in res:
        f.write(f"{name}\t結果={'赤' if red else ('ビルド失敗' if be else '緑')}\t赤の試験={','.join(red) if red else '-'}\trc={rc}\n")
print(open(out, encoding='utf-8').read())
