"""[計測の写しだけ] (160) 解禁した時点の3項の計器（読むだけ・(159) 反証役の指摘 vb#4＝必要性がほぼ2値〔参照を超えたか〕になっていないかを行ごとに見る）。
環境変数 POB_NEED_DIR の下に n<seed>.txt。未設定なら1命令も走らない。inj_ghost を当てた木に当てる（SelfPlay の ghM の行の後ろに足す）。
  U 行＝国が行を解禁した四半期（Discovery.Step の門を越えた行・開幕の既知を含む）：q・年・国（BaseName）・行・分野・技術力・必要性・伝わり・点数
使い方：python inj_need.py <木の根>"""
import sys
root = sys.argv[1]

def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))

TAIL = r'''

/// <summary>[計測の写しだけ] (160) 解禁した時点の3項の計器（読むだけ）。</summary>
public static class NeedDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_NEED_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    [System.ThreadStatic] public static Match? Cur;
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"n{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#U\tyr\tqtr\tnation\trow\tfield\ttech\tneed\ttransmit\tscore\n");
        }
        public void Hit(World w, int row, Discovery.Terms t)
        {
            string bn = w.SelfPower is { } sp0 && sp0.BaseName.Length > 0 ? sp0.BaseName : (w.SelfPower?.Name ?? w.Region.Name);
            var d = Catalog.Discoveries[row];
            W.Write($"U\t{w.Year}\t{w.Quarter}\t{bn}\t{d.Name}\t{d.Field}\t{F(t.Tech)}\t{F(t.Need)}\t{F(t.Transmit)}\t{F(t.Score)}\n");
        }
        public void Quarter(int q, int quarters) { if (q == quarters - 1) W.Dispose(); }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [
    ('            if (known[row] || !(terms[row].Score >= GateScore)) continue;\n',
     '            if (known[row] || !(terms[row].Score >= GateScore)) continue;\n            NeedDiag.Cur?.Hit(w, row, terms[row]);\n'),
], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n',
     '        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n        var needM = NeedDiag.On ? new NeedDiag.Match(seed) : null; NeedDiag.Cur = needM;\n'),
    ('                ghM?.Quarter(sim, q, quarters);\n',
     '                ghM?.Quarter(sim, q, quarters);\n                needM?.Quarter(q, quarters);\n'),
])
print('ok', root)
