# (936) c82hd プローブ（読むだけ・決定を変えない）：InstrumentsTests.私的な建設の工業化度の累積… の赤（フランス パリ Q17）の行を、旗 1 と旗 0 の木で並べて書き出す。
#   python -X utf8 -I probe936.py <使い捨ての木> <出力フォルダ> <artifacts>
#   使い捨ての木の試験の Assert の直前に、パリの Q14〜Q20 の行（Cum の差・日ごとの ipc の最小と最大・建った束）と cearn_s のパリの estateDebtMovedPerQuarter を書く 1 文を差し込む（試験の判定は変えない）。
import sys, os, subprocess
tree, out, art = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
T = os.path.join(tree, 'PriceOfBread.Tests', 'InstrumentsTests.cs')
CO = os.path.join(tree, 'PriceOfBread.SimCore', 'Coefficients.cs')
t0 = open(T, 'rb').read(); co0 = open(CO, 'rb').read()
A = b'            Assert.True(bad.Count == 0, string.Join(" | ", bad.Take(30)) + $" \xef\xbc\x88{bad.Count}\xef\xbc\x8f{checkedRows}\xef\xbc\x89");'
assert t0.count(A) == 1, t0.count(A)
def ins(path):
    p = path.replace('\\', '/')
    return ('            { var sb = new System.Text.StringBuilder(); sb.AppendLine("bad " + bad.Count + "/" + checkedRows + " : " + string.Join(" | ", bad));'
            ' foreach (var x in rows) if (x.Nation == "フランス" && x.Region == "パリ" && x.Q >= 14 && x.Q <= 20) { built.TryGetValue((x.Nation, x.Region, x.Q), out double bb);'
            ' sb.AppendLine($"Q{x.Q} cum0 {x.Cum0:R} cum1 {x.Cum1:R} gain {x.Cum1 - x.Cum0:G6} ipcLo {x.Ipc0:G6} ipcHi {x.Ipc1:G6} built {bb:G6} gain/built {(bb > 0 ? (x.Cum1 - x.Cum0) / bb : double.NaN):G6}"); }'
            ' var ce = System.IO.Path.Combine(dir, "cearn_s1789.tsv"); if (System.IO.File.Exists(ce)) { var (eh, er) = Table(ce); int iq = Array.IndexOf(eh, "quarter"), ir = Array.IndexOf(eh, "region"), im = Array.IndexOf(eh, "estateDebtMovedPerQuarter"), ie = Array.IndexOf(eh, "emptyDebtAfterEstate");'
            ' foreach (var rr in er) if (rr[ir] == "パリ" && int.Parse(rr[iq]) >= 14 && int.Parse(rr[iq]) <= 20) sb.AppendLine($"cearn Q{rr[iq]} estateDebtMoved {rr[im]} emptyDebtAfter {rr[ie]}"); }'
            ' foreach (var rr in cr) if (rr[Array.IndexOf(ch, "region")] == "パリ") { int qq = (int)Col(ch, rr, "quarter"); if (qq >= 14 && qq <= 20) sb.AppendLine($"cons Q{qq} builtUnitsPerMonth {rr[Array.IndexOf(ch, "builtUnitsPerMonth")]} monthsCount {rr[Array.IndexOf(ch, "monthsCount")]}"); }'
            f' System.IO.File.WriteAllText(@"{p}", sb.ToString()); }}\n').encode('utf-8')
try:
    for tag, flag in (('flag1', b'1.0'), ('flag0', b'0.0')):
        cb = co0.replace(b'public double EmptyPopDebtToHeirs = 1.0;', b'public double EmptyPopDebtToHeirs = ' + flag + b';')
        assert cb.count(b'public double EmptyPopDebtToHeirs = ' + flag + b';') == 1
        open(CO, 'wb').write(cb)
        open(T, 'wb').write(t0.replace(A, ins(os.path.join(out, f'probe_{tag}.txt')) + A))
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art,
                            '--filter', 'FullyQualifiedName~InstrumentsTests.私的な建設の工業化度の累積は建った束と1束の工業化度の積に閉じる'], capture_output=True)
        txt = r.stdout.decode('utf-8', 'replace') + r.stderr.decode('utf-8', 'replace')
        keep = [l for l in txt.splitlines() if ('成功' in l or '失敗!' in l or ' error ' in l.lower())]
        print(tag, 'rc', r.returncode, ' / '.join(keep)[:300], flush=True)
finally:
    open(T, 'wb').write(t0); open(CO, 'wb').write(co0)
