"""[計測の写しだけ] (166) 線M の計器＝m162/inj_mil5.py を当てたうえで、A 行の末尾に（列 61〜）
  61 AI の軍事費の目標（門の前・率）|62 門の後の目標（率）|63 門の前の目標が今の段より上のとき、次の1段の門の理由（-1＝該当なし・0 通る・1 総動員が前提・2 賛成不足・3 政治資本の残りが不足・4 その他）
  64 総動員の判断の劣勢（陣営の戦力で劣勢＝1）|65 敗色（war.Score の負の側の最大）|66 交戦の相手が全て海を越える（IsSeaCrossing＝1・陸続きの相手あり＝0・平時＝-1）
  67 徴兵法の目標（門の前・int）|68 徴兵の縮小（下げる向き）の純支持|69 旧支配層|70 近代エリート|71 大衆|72 小営業|73 議会|74 軍
  75 縮小の門（今の法から1段下げる＝1 通る・0 通らない・-1 志願制で下げる段なし）|76 縮小の門の理由（上と同じ符号）
を足す。力学は読まない（副作用の無い照会〔GateInfo・NetSupport・IsSeaCrossing〕と記録だけ）。使い方：python inj_mil6.py <写しの木>"""
import sys, subprocess, os
root = sys.argv[1]
here = os.path.dirname(os.path.abspath(__file__))
sp = os.path.dirname(here)
subprocess.run([sys.executable, os.path.join(sp, 'm162', 'inj_mil5.py'), root], check=True, stdout=subprocess.DEVNULL)
MARK = '[' + '計器の写しだけ' + ']'


def load(rel):
    p = root + '/' + rel
    raw = open(p, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    s = raw.decode('utf-8-sig')
    return p, bom, s, ('\r\n' in s)


def save(p, bom, s):
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


def edit(rel, pairs):
    p, bom, s, crlf = load(rel)

    def nl(x):
        return x.replace('\n', '\r\n') if crlf else x
    for a, b, mode, cnt in pairs:
        a = nl(a); b = nl(b)
        assert s.count(a) == cnt, (rel, a[:70], s.count(a))
        if mode == 'after': s = s.replace(a, a + b)
        elif mode == 'before': s = s.replace(a, b + a)
        else: s = s.replace(a, b)
    save(p, bom, s)


edit('PriceOfBread.SimCore/World.cs', [
    ('    public double YMn, YMwar,', '    public double YM6milWant = -1, YM6milGated = -1, YM6milReason = -1, YM6mobOut = -1, YM6mobLose = -1, YM6mobSea = -1, YM6csWant = -1;   // ' + MARK + '\n', 'before', 1),
])
edit('PriceOfBread.SimCore/PolicyPreferences.cs', [
    ('    internal static double ArmyPower(World w)\n',
     '    internal static double[] YM6Parts(World w, PolicyAxis a, bool increasing)   // ' + MARK + ' NetSupport の項を分けて読む（同じ式・同じ順）\n'
     '    {\n'
     '        var e = Build(w); double sign = increasing ? 1 : -1;\n'
     '        double street = w.Coef.ParliamentStreetSupersede > 0 && w.ParliamentEnabled && HasLegislature(w.Polity) ? 1.0 - PoliticalQuantities.Clamp01(w.Coef.ParliamentStreetSupersede) : 1.0;\n'
     '        var o = new double[6];\n'
     '        for (int b = 0; b < BlocCount; b++) o[b] = street * e.Power[b] * Pref(a, b, e, w) * sign;\n'
     '        o[3] = PettyNet(w, a, sign, e); o[4] = ParliamentNet(w, a, sign, e); o[5] = ArmyPower(w) * ArmyStake(a) * sign;\n'
     '        return o;\n'
     '    }\n', 'before', 1),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        if (w.Coef.AiLawGateUnified > 0) GateAllLawTargets(w, p);\n    }\n',
     '        w.YM6milWant = p.MilitarySpendingTarget; w.YM6csWant = (int)p.ConscriptionLaw;   // ' + MARK + '\n'
     '        { int ymCur = Government.MilitaryStage(w.MilitarySpending), ymWs = Government.MilitaryStage(p.MilitarySpendingTarget);\n'
     '          if (ymWs > ymCur) { var ymR = Government.GateInfo(w, PolicyAxis.Coercion, true, ymCur + 1).Reason; w.YM6milReason = ymR == "" ? 0 : ymR == "総動員が前提" ? 1 : ymR == Government.ReasonNoMandate ? 2 : ymR == Government.ReasonNoCapitalStock ? 3 : 4; } else w.YM6milReason = -1; }   // ' + MARK + '\n', 'before', 1),
])
# 門の後の目標は GateAllLawTargets の直後（同じ行の後ろ）
p, bom, s, crlf = load('PriceOfBread.SimCore/SelfPlay.cs')
nl = (lambda x: x.replace('\n', '\r\n')) if crlf else (lambda x: x)
a = nl('        if (w.Coef.AiLawGateUnified > 0) GateAllLawTargets(w, p);\n')
assert s.count(a) == 1, s.count(a)
s = s.replace(a, a + nl('        w.YM6milGated = p.MilitarySpendingTarget;   // ' + MARK + '\n'))
a = nl('            else if (mOutmatched || mLosing > MobilizeLosingScore) w.Mobilized = true;')
assert s.count(a) == 1, s.count(a)
i = s.index(a); j = s.index(nl('\n'), i) + len(nl('\n'))
s = s[:j] + nl('            { w.YM6mobOut = mAtWar ? (mOutmatched ? 1 : 0) : -1; w.YM6mobLose = mAtWar ? mLosing : -1; bool ymAllSea = true;   // ' + MARK + '\n'
               '              foreach (var ywar in mDip.Wars) { Power? yf = ywar.A == mSelf ? ywar.B : ywar.B == mSelf ? ywar.A : null; if (yf is null) continue; if (!mDip.IsSeaCrossing(yf, mSelf, w.Coef)) ymAllSea = false; }\n'
               '              w.YM6mobSea = mAtWar ? (ymAllSea ? 1 : 0) : -1; }\n') + s[j:]
# A 行の末尾
a = '{(int)yw.Policy.ConscriptionLaw}").AppendLine();'
assert s.count(a) == 1, s.count(a)
b = ('{(int)yw.Policy.ConscriptionLaw}|{yw.YM6milWant:F3}|{yw.YM6milGated:F3}|{yw.YM6milReason:F0}|{yw.YM6mobOut:F0}|{yw.YM6mobLose:F3}|{yw.YM6mobSea:F0}|{yw.YM6csWant:F0}"\n'
     '                            + UtilityAgent.YM6Cons(yw)).AppendLine();')
s = s.replace(a, b)
# 縮小の純支持と門（行を書く時点＝四半期の終わり）
helper = ('    internal static string YM6Cons(World yw)   // ' + MARK + '\n'
          '    {\n'
          '        var pr = PolicyPreferences.YM6Parts(yw, PolicyAxis.ConscriptionLaw, false); double net = PolicyPreferences.NetSupport(yw, PolicyAxis.ConscriptionLaw, false);\n'
          '        int cur = (int)yw.ConscriptionLaw; string ok = "-1", rs = "-1";\n'
          '        if (cur > 0) { var g = Government.GateInfo(yw, PolicyAxis.ConscriptionLaw, false, cur - 1); ok = g.Ok ? "1" : "0"; rs = g.Reason == "" ? "0" : g.Reason == "総動員が前提" ? "1" : g.Reason == Government.ReasonNoMandate ? "2" : g.Reason == Government.ReasonNoCapitalStock ? "3" : "4"; }\n'
          '        return $"|{net:F4}|{pr[0]:F4}|{pr[1]:F4}|{pr[2]:F4}|{pr[3]:F4}|{pr[4]:F4}|{pr[5]:F4}|{ok}|{rs}";\n'
          '    }\n')
a = nl('    internal static void Apply(World w, Stance s)')
assert s.count(a) == 1, s.count(a)
s = s.replace(a, nl(helper) + a)
save(p, bom, s)
print('ok mil6', root)
