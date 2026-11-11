# (902) c77ce 突然変異の置き換え（使い捨ての木の Territory.cs を 1 本ずつ壊す）。
#   python -X utf8 -I mutapply902.py <Territory.cs> <M1..M12>
# 置き換えの元の文字列は木にちょうど 1 回だけある物（無い・2 回以上なら失敗で止める）。バイトで読み書きする（改行を保つ）。
import sys

A = "if (how == Rehoming.Ceded && to is not null && from.Coef.CedeRuleToLocal > 0)"
B = "if (r.Rule.Length == 4) { r.Rule[1] += r.Rule[0]; r.Rule[0] = 0; }"
C = "if (r.ForeignRulers[k] == key) r.ForeignShares[k] = 0;"
D = "string key = tsp.BaseName.Length > 0 ? tsp.BaseName : tsp.Name;"
S = "r.Dominance = 1.0;"

MUT = {
    'M1': [(B, "if (r.Rule.Length == 4) { r.Rule[3] += r.Rule[0]; r.Rule[0] = 0; }")],
    'M2': [(B, "if (r.Rule.Length == 4) { r.Rule[1] += r.Rule[0]; r.RuleInit[1] += r.Rule[0]; r.Rule[0] = 0; }")],
    'M3': [(C, "if (r.ForeignRulers[k] == key) { }")],
    'M4': [(A, "if (how != Rehoming.Sovereign && to is not null && from.Coef.CedeRuleToLocal > 0)")],
    'M5': [(B, "if (r.Rule.Length == 4) { r.Rule[1] += r.Rule[0]; r.Rule[0] = 0; r.Dominance = 0; }")],
    'M6': [(B, "if (r.Rule.Length == 4) { double hv = 0.5 * r.Rule[0]; r.Rule[1] += hv; r.Rule[0] -= hv; }")],
    'M7': [(C, "if (r.ForeignRulers[k].Length >= 0) r.ForeignShares[k] = 0;")],
    'M8': [(A, "if (how == Rehoming.Ceded && to is not null && from.Coef.CedeRuleToLocal > 1e300)")],
    'M9': [(B, "if (r.Rule.Length == 4) { r.Rule[0] = 0; }")],
    'M10': [(S, "r.Dominance = 1.0; if (r.Rule.Length == 4) { r.Rule[1] += r.Rule[0]; r.Rule[0] = 0; }")],
    'M11': [(A, "if (how == Rehoming.Ceded && to is not null)")],
    'M12': [(D, "string key = tsp.Name;")],
}

def main():
    path, name = sys.argv[1], sys.argv[2]
    b = open(path, 'rb').read()
    for old, new in MUT[name]:
        o, n = old.encode('utf-8'), new.encode('utf-8')
        c = b.count(o)
        if c != 1:
            print('%s: 元の文字列の数が %d（1 でない）' % (name, c)); sys.exit(2)
        b = b.replace(o, n)
    open(path, 'wb').write(b)
    print('%s 当てた' % name)

if __name__ == '__main__':
    main()
