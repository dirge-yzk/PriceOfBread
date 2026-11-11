# (978) c88cs 突然変異の台本（使い捨ての木で 1 本ずつ当てて試験の束を回す・読むだけの出力）
# 使い方：python -X utf8 -I mut978.py <使い捨ての木> <出力.txt> <artifacts のフォルダ>
# 木は器のコミットの detached worktree（この台本はファイルをバイトで書き換え、各突然変異の後に git checkout で戻す）。
import subprocess, sys, re, os

TREE, OUT, ART = sys.argv[1], sys.argv[2], sys.argv[3]
S = 'PriceOfBread.SimCore/'
MUT = [
    ('M-S1', S + 'World.cs', 'f += (1.0 - f) * ls[4];', 'f += 0.0 * ls[4];', 'StateProfitShare に畳まない'),
    ('M-S2', S + 'World.cs', 'if (c.LandShareCrownSlot > 0 && p.LandShares is { Length: 5 } ls)',
     'if (c.LandShareCrownSlot > 0 && p.IsRgo && (p.LandShares ?? System.Linq.Enumerable.FirstOrDefault(r.Producers, q => q.LandShares is { Length: 5 })?.LandShares) is { Length: 5 } ls)', '賦存の RGO にも国家の枠'),
    ('M-S3', S + 'EconomyStep.cs', 'double wk = ls is null ? 1.0 : crown ? ls[k] / (1.0 - ls[4]) : ls[k];', 'double wk = ls is null ? 1.0 : ls[k];', '私の 4 枠を 1−c で割らない'),
    ('M-S4', S + 'InternationalMarket.cs', 'double sc = rent * World.StateProfitShare(pr, rr, coef);',
     'double sc = rent * (pr.BuiltScale > 1e-9 ? Math.Min(1.0, pr.BuiltScale / Math.Max(pr.Scale, 1e-9)) : 0.0);', '輸出の側で畳まない'),
    ('M-S6', S + 'Simulation.cs', 'InitialRentSeedScale * lsk[k] :', 'InitialRentSeedScale * (lsk.Length == 5 ? lsk[k] / (1.0 - lsk[4]) : lsk[k]) :', '地代の種も 1−c で割る'),
    ('M-S7', S + 'World.cs', 'f += (1.0 - f) * ls[4];', 'f += ls[4];', 'f の分を引かない'),
    ('M-S8', S + 'World.cs', 'if (c.LandShareCrownSlot > 0 && p.LandShares is { Length: 5 } ls)', 'if (p.LandShares is { Length: 5 } ls)', '旗 0 でも畳む'),
    ('M-S9', S + 'Simulation.cs', 'return o.Length == 5 && !(1.0 - o[4] > 0) ? null : o;', 'return o;', 'c≥1 を受ける'),
    ('M-S10', S + 'EconomyStep.cs', '(ls is null ? f : f * wk)', '(ls is null ? f : f * ls[k])', '凍結の差分の基数は ls[k]'),
    ('Mu-M1', S + 'InstrumentsLandshare.cs', '107, 108, 110];', '107, 108];', '鉱物の表から銀を落とす'),
    ('Mu-M2', S + 'InstrumentsLandshare.cs', 'if (endow) a.QMinEndow += rent; else { a.QMinNonEndow += rent; a.QMinByOwner[ownerCls] += rent; }',
     '{ if (endow) a.QMinEndow += rent; a.QMinNonEndow += rent; a.QMinByOwner[ownerCls] += rent; }', '賦存も列 1 に'),
    ('Mu-M3', S + 'InstrumentsLandshare.cs', 'else { a.QMinNonEndow += rent; a.QMinByOwner[ownerCls] += rent; }',
     'else { a.QMinNonEndow += rent; if (s.LandShares is { } lm) { for (int km = 0; km < lm.Length; km++) a.QMinByOwner[LsClass(r.Pops[LsRecipient(r, s, km, ref a.Fallback)].ClassId)] += rent * lm[km]; } else a.QMinByOwner[ownerCls] += rent; }', '受け手の階級で割る'),
    ('Mu-M4', S + 'InstrumentsLandshare.cs', 'a.QMinNonEndow = a.QMinEndow = a.QExpMin = a.QExpNeNoble = a.QExpNeNobleMin = 0; System.Array.Clear(a.QMinByOwner);', '', '四半期で 0 へ戻さない'),
    ('Mu-M6', S + 'InstrumentsLandshare.cs', '&& s.OwnerPopIndex < r.Pops.Count && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble;\n', '&& s.OwnerPopIndex < r.Pops.Count;\n', '列 9 の判別から貴族を落とす'),
]
FILTER = 'FullyQualifiedName~LandShareCrownTests|FullyQualifiedName~LandShareTests|FullyQualifiedName~LandOwnershipDataTests'
L = []
# (982) 対照の回：突然変異なしで同じ束を回し、緑でなければ以後の赤は突然変異のせいと言えない（木か束が元から赤）
r0 = subprocess.run(['dotnet', 'test', os.path.join(TREE, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', ART, '--filter', FILTER],
                    capture_output=True, text=True, encoding='utf-8', errors='replace')
out0 = r0.stdout + r0.stderr
red0 = sorted(set(m.group(1) for m in re.finditer(r'失敗 PriceOfBread\.Tests\.\w+\.(\w+)', out0)))
L.append(f'M-0	対照（突然変異なし）	{"緑（期待どおり）" if r0.returncode == 0 and not red0 else "赤（対照が崩れた＝以後の表は読めない）"}	{",".join(red0)}')
print(L[-1], flush=True)
for mid, f, a, b, what in MUT:
    p = os.path.join(TREE, f)
    raw = open(p, 'rb').read()
    crlf = b'\r\n' in raw
    t = raw.decode('utf-8')
    aa, bb = (a.replace('\n', '\r\n'), b.replace('\n', '\r\n')) if crlf else (a, b)
    n = t.count(aa)
    if n != 1:
        L.append(f'{mid}\t{what}\t当てられない（一致 {n}）'); continue
    open(p, 'wb').write(t.replace(aa, bb).encode('utf-8'))
    r = subprocess.run(['dotnet', 'test', os.path.join(TREE, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', ART, '--filter', FILTER],
                       capture_output=True, text=True, encoding='utf-8', errors='replace')
    subprocess.run(['git', '-C', TREE, 'checkout', '--', f], check=True)
    out = r.stdout + r.stderr
    red = sorted(set(m.group(1) for m in re.finditer(r'失敗 PriceOfBread\.Tests\.\w+\.(\w+)', out)))
    tot = re.findall(r'(失敗!|成功!)\s+-失敗:\s+(\d+)、合格:\s+(\d+)', out)
    if 'error CS' in out:
        L.append(f'{mid}\t{what}\tビルドの誤り'); continue
    L.append(f'{mid}\t{what}\t{"赤" if red else "緑"}\t{",".join(red)}\t{tot[-1] if tot else "集計なし"}')
    print(L[-1], flush=True)
open(OUT, 'wb').write(('# (978) 突然変異（試験の束：LandShareCrownTests・LandShareTests・LandOwnershipDataTests）\n' + '\n'.join(L) + '\n').encode('utf-8'))
