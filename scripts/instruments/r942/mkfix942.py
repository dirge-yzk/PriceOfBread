# (942) InstrumentsTests.cs:2119 の試験の ① だけを直した写しを作る（バイトで読み書き・BOM と LF を保つ）
import sys
src, dst = sys.argv[1], sys.argv[2]
s = open(src, 'rb').read().decode('utf-8')
o1 = '''            // ① 州の払った額の和 ＝ spend_s の建設の材料と工賃（窓の区切りごと）
            var consPaid = new Dictionary<int, double>();'''
n1 = '''            // ① 州の払った額の和 ＝ spend_s の建設の材料と工賃（窓の区切りごと）
            // ★(942) 州の表は四半期の末に所有する国の州を回し、支払いの表は払った時点の国で積む＝四半期の途中で州が国を出入りすると帰属が分かれる〔計器の限界・★(613) と同じ〕。
            //   外交の凍結は Diplomacy.Step だけを止め、内戦と分離の建国（SpawnSecededNations）と再統合（ReabsorbNations）は止めない（旗 LandTaxFixedBasis 1 の軌道で 1787 年に パリ が 立憲王政フランス へ移り、区切り 2 が 4,714 開いた）。
            //   出た州はその四半期の払いが支払いの表にだけ載り、戻った州は出た四半期の積み残しを州の表に載せる＝州の名の集合が前の四半期と違う四半期を含む区切りは比べない（出入りの両側）。比べた区切りが無ければ赤。
            int cR = Array.IndexOf(ch, "region");
            var regionsByQ = new SortedDictionary<int, HashSet<string>>();
            foreach (var r in cr) { int q = int.Parse(r[cQ]); if (!regionsByQ.TryGetValue(q, out var set)) regionsByQ[q] = set = new HashSet<string>(); set.Add(r[cR]); }
            var movedBuckets = new HashSet<int>();
            int prevQ = int.MinValue; HashSet<string>? prevSet = null;
            foreach (var (q, set) in regionsByQ)
            {
                if (prevSet is not null && (q != prevQ + 1 || !prevSet.SetEquals(set))) movedBuckets.Add(BucketOf(q));
                prevQ = q; prevSet = set;
            }
            var consPaid = new Dictionary<int, double>();'''
o2 = '''            foreach (var b in consPaid.Keys.Union(spendPaid.Keys))
            {
                double a = consPaid.GetValueOrDefault(b), c = spendPaid.GetValueOrDefault(b);
                Assert.True(Math.Abs(a - c) <= 1e-9 * Math.Max(1.0, Math.Abs(c)), $"区切り{b}：州の払った額の和 {a} が支払いの表の建設 {c} に閉じない");
            }
'''
n2 = '''            int comparedBuckets = 0;
            foreach (var b in consPaid.Keys.Union(spendPaid.Keys))
            {
                if (movedBuckets.Contains(b)) continue;
                comparedBuckets++;
                double a = consPaid.GetValueOrDefault(b), c = spendPaid.GetValueOrDefault(b);
                Assert.True(Math.Abs(a - c) <= 1e-9 * Math.Max(1.0, Math.Abs(c)), $"区切り{b}：州の払った額の和 {a} が支払いの表の建設 {c} に閉じない");
            }
            Assert.True(comparedBuckets >= 1, $"対照：比べた区切りが無い（州が国を出入りした区切り {string.Join(",", movedBuckets)}）");
'''
assert s.count(o1) == 1 and s.count(o2) == 1
s = s.replace(o1, n1).replace(o2, n2)
assert '\r' not in s
open(dst, 'wb').write(s.encode('utf-8'))
print('ok')
