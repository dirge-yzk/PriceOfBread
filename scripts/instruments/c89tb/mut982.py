# (982) c89tb 突然変異の台本（使い捨ての木で 1 本ずつ当て、試験を名指しで回す）。
# 使い方：python -X utf8 -I mut982.py <使い捨ての木> <出力.txt> <artifacts のフォルダ>
# 最初に対照の回（突然変異なし）を回す。落ちた Assert の文（エラー メッセージの最初の行）も写す。
import subprocess, sys, re, os
TREE, OUT, ART = sys.argv[1], sys.argv[2], sys.argv[3]
IT, RK = 'PriceOfBread.Tests/InstrumentsTests.cs', 'PriceOfBread.Tests/RouteKmTests.cs'
T_B2 = 'FullyQualifiedName~RouteKmTests.陸路は貿易の経路の表と一致'
T_B5 = 'FullyQualifiedName~InstrumentsTests.軍事費の施行の一歩の内訳が別の列と合い'
T_B8 = 'FullyQualifiedName~InstrumentsTests.幽霊戦線の列が'
T_B11 = 'FullyQualifiedName~InstrumentsTests.富裕層のお金の行き先の表が'
T_B12 = 'FullyQualifiedName~InstrumentsTests.計器を出しても軌道は変わらず'
MUT = [
    ('M-B2a', RK, 'xNations.Add(Str(xh, r, "fromNation")); xNations.Add(Str(xh, r, "toNation"));', 'xNations.Add("x");', T_B2, '越境に関わった国を 1 つに潰す（新しい Assert だけが赤）'),
    ('M-B2b', RK, '[NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()]', '[NationConfig.France(), NationConfig.Prussia()]', T_B2, 'M9 の取り直し：盤面を仏・普だけ'),
    ('M-B5a', IT, 'pushNations.Add(row[Array.IndexOf(nh, "nation")]);', 'pushNations.Add("x");', T_B5, '政治予算を突き合わせた国を 1 つに潰す'),
    ('M-B8a', IT, 'frontNations.Add(row[Array.IndexOf(nh, "nation")]);', 'frontNations.Add("x");', T_B8, '戦線を数えた国を 1 つに潰す'),
    ('M-B11a', IT, 'otherNations.Add(cls[iU]);', 'otherNations.Add("x");', T_B11, '他の国を 1 つに潰す'),
    ('M-B12a', IT, 'liveNationQuarters += sim.Nations.Count;', 'liveNationQuarters += sim.Nations.Count - 1;', T_B12, '生きている国の数え方を 1 国分外す（和の Assert が赤）'),
    ('M-B12b', IT, 'NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk, NationConfig.Austria()];', 'NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk];', T_B12, 'Make を 3 国にする（四半期ごとの述語が赤）'),
    ('M-B12c', IT, 'Assert.True(sim.Nations.Count >= 4, $"前提が崩れた：四半期 {q}', 'Assert.True(true, $"前提が崩れた：四半期 {q}', T_B12, '四半期ごとの述語を外す（Make を 3 国にしても和の Assert が赤）'),
]
def run(filt):
    r = subprocess.run(['dotnet', 'test', os.path.join(TREE, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', ART, '--filter', filt],
                       capture_output=True, text=True, encoding='utf-8', errors='replace')
    out = r.stdout + r.stderr
    red = sorted(set(m.group(1) for m in re.finditer(r'失敗 PriceOfBread\.Tests\.\w+\.(\w+)', out)))
    msg = re.findall(r'エラー メッセージ:\s*\n\s*(.+)', out)
    return r.returncode, red, (msg[0][:160] if msg else ''), ('error CS' in out)
L = []
allf = '|'.join(sorted(set(m[4] for m in MUT)))
rc, red, msg, ce = run(allf)
L.append(f'M-0\t対照（突然変異なし）\t{"緑" if rc == 0 else "赤"}\t{",".join(red)}\t{msg}')
print(L[-1], flush=True)
for mid, f, a, b, filt, what in MUT:
    p = os.path.join(TREE, f)
    raw = open(p, 'rb').read(); t = raw.decode('utf-8'); crlf = b'\r\n' in raw
    aa, bb = (a.replace('\n', '\r\n'), b.replace('\n', '\r\n')) if crlf else (a, b)
    n = t.count(aa)
    if n != 1:
        L.append(f'{mid}\t{what}\t当てられない（一致 {n}）'); print(L[-1], flush=True)
        if mid == 'M-B12c':
            pass
        continue
    open(p, 'wb').write(t.replace(aa, bb).encode('utf-8'))
    try:
        if mid == 'M-B12c':   # 述語を外したうえで Make も 3 国にして、和の Assert が先に赤になることを見る
            q = os.path.join(TREE, IT); t2 = open(q, 'rb').read().decode('utf-8')
            c = 'NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk, NationConfig.Austria()];'
            open(q, 'wb').write(t2.replace(c, 'NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk];').encode('utf-8'))
        rc, red, msg, ce = run(filt)
    finally:
        subprocess.run(['git', '-C', TREE, 'checkout', '--', f], check=True)
    L.append(f'{mid}\t{what}\t{"ビルドの誤り" if ce else ("赤" if rc != 0 else "緑")}\t{",".join(red)}\t{msg}'); print(L[-1], flush=True)
open(OUT, 'wb').write(('# (982) 突然変異（c89tb）\n' + '\n'.join(L) + '\n').encode('utf-8'))
