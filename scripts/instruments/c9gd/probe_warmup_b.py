#!/usr/bin/env python3
# (786) c9gd 段0の補い：助走（_calendarFrozen・20 四半期）を含む全ての月で Fiscal.cs の金建て債務の負担 b の評価を数える使い捨ての木の編集（読むだけ＝値を変えない・ファイルへ追記するだけ）。
# 使い方：POB_WORK=<作業> bash scripts/instruments/batch.sh prep probe dir:<木> plain scripts/instruments/c9gd/probe_warmup_b.py  →  batch.sh run probe 2（種 1〜2）。コミットする木には当てない。
# 出力：D:/PoB_work/c9gd/probe_b.txt の行  B＝b>=3.0 または非有限・非正（上端に届いた／ガードに入った）・S＝評価の件数が 2 のべき乗に届いた月か最大の b を更新した月の見本。
#   列：タグ・プロセス・助走中(1)か本走(0)・評価の通し番号（助走・本走別）・b・WorldSilverPrice・これまでの最大 b（助走・本走別）
# b の上端は Fiscal の中の定数 3.0 を直書きしている（係数 GoldDebtBurdenCap の削除前の値）。
import sys, os
root = sys.argv[1]
def rw(rel, fn):
    p = os.path.join(root, rel); b = open(p, 'rb').read(); n = fn(b); assert n != b, rel; open(p, 'wb').write(n)
HELPER = (rb'''    static readonly object PobProbeLock = new object();
    static long[] PobProbeN = new long[2]; static double[] PobProbeMax = new double[2];
    static void PobProbe(double b, bool frozen, double ws)
    {
        int f = frozen ? 1 : 0;
        lock (PobProbeLock)
        {
            PobProbeN[f]++; bool bad = !(b > 0 && b < double.PositiveInfinity); bool hit = bad || b >= 3.0;
            bool newMax = !bad && b > PobProbeMax[f]; if (newMax) PobProbeMax[f] = b;
            long n = PobProbeN[f]; bool pow2 = (n & (n - 1)) == 0;
            if (hit || newMax || pow2)
            {
                try { System.IO.File.AppendAllText(@"D:\PoB_work\c9gd\probe_b.txt", (hit ? "B" : "S") + "\t" + Environment.ProcessId + "\t" + f + "\t" + n + "\t" + b.ToString("R") + "\t" + ws.ToString("R") + "\t" + PobProbeMax[f].ToString("R") + "\n"); } catch { }
            }
        }
    }
''')
def fiscal(b):
    h = b'public static class Fiscal\n{\n'
    assert b.count(h) == 1
    b = b.replace(h, h + HELPER)
    a = b'            b = Math.Clamp(b, 1.0, c.GoldDebtBurdenCap);'
    assert b.count(a) == 1
    return b.replace(a, b'            PobProbe(b, w.WarmupFrozen, w.WorldSilverPrice);\n' + a)
rw('PriceOfBread.SimCore/Fiscal.cs', fiscal)
print('probe edit ok')
