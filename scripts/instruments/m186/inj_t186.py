"""[計測の写しだけ] (186) 線B 後半＝貿易/GDP の計器（読むだけ）。
環境変数 POB_T186_DIR の下に t<seed>.txt。未設定なら1命令も走らない。
  T 行＝国×四半期（四半期の終わりの断面）：輸出の和・輸入の和（清算1回分＝ExportsByGood/ImportsByGood）・月の名目GDP・年の清算の回数
  ＝SelfPlay.MajorsTradeGdp と同じ物差し（(輸出＋輸入)×清算/年 ÷ (月の名目GDP×12)）を四半期ごとに出す＝読み手が10年の窓と10種の中央で読む。
力学は読まない（加算と記録だけ）。使い方：python inj_t186.py <木の根>（fullM の後でも前でも当たる）"""
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

/// <summary>[計測の写しだけ] (186) 貿易/GDP の四半期の断面（読むだけ）。</summary>
public static class T186
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_T186_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; var p = w.SelfPower; if (p is null) continue;
            double ex = 0, im = 0;
            if (w.ExportsByGood is { } eg) foreach (var v in eg) ex += v;
            if (w.ImportsByGood is { } ig) foreach (var v in ig) im += v;
            sb.Append("T|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(p.BaseName.Length > 0 ? p.BaseName : p.Name)
              .Append('|').Append(F(ex)).Append('|').Append(F(im)).Append('|').Append(F(w.NominalGdp)).Append('|').Append(F(w.Coef.ClearingsPerYear)).Append('\n');
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"t{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#T|q|yr|idx|base|exports|imports|nomGdpMonth|clearingsPerYear\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Fiscal.cs', [], TAIL)
sp = open(root + '/PriceOfBread.SimCore/SelfPlay.cs', encoding='utf-8-sig').read()
if 'if (F181.On) F181.Quarter(sim, seed, q, quarters);' in sp:
    edit('PriceOfBread.SimCore/SelfPlay.cs', [
        ("                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n",
         "                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n                if (T186.On) T186.Quarter(sim, seed, q, quarters);\n"),
    ])
else:
    edit('PriceOfBread.SimCore/SelfPlay.cs', [
        ("                sim.StepQuarter();\n",
         "                sim.StepQuarter();\n                if (T186.On) T186.Quarter(sim, seed, q, quarters);\n"),
    ])
print('ok t186', root)
