# (902) c77ce 段 C-1（C1＋H-a・旗 CedeRuleToLocal）の門 G1〜G6 と紙の突然変異の台本（cederule_s を読む）。
#   python -X utf8 -I scripts/instruments/c77ce/gate902.py <ins の生のフォルダ> [出力のファイル]
#   python -X utf8 -I scripts/instruments/c77ce/gate902.py --paper <ins の生のフォルダ（旗 0 の盤面）> [出力のファイル]
#   python -X utf8 -I scripts/instruments/c77ce/gate902.py --selftest
# 母集合＝how＝Ceded かつ movedAgainFlag＝0 かつ foreignAddedFlag＝0（事前登録 measurements/m902_20261106_prereg_ce.txt [4]）。
# 計器の列は印字 G10（相対 5e-10）＝門はその印字を double に読んで比べる。両辺が同じ C# の double の印字なら差は 0（R1 の後＝c1LocalShare・R2・R3・RuleInit・dominance の後＝前）。
#   エンジンの値の double のビットの一致は機構の試験（CedeRuleToLocalTests）が見る（二層）。identBreakFlag は旗 0 の ident だけで読む（旗 1 では割譲の全ての行で 1 になる）。
# 門（全て両側）：
#   G1 形：後の R0＝0・後の R1＝c1LocalShare（前の R0＋R1）・後の R2・R3・RuleInit[1]・dominance＝前（各 |差|≦TOL）。
#   G2 大きさ：種ごとに Σ(前の R0−後の R0) が Σ前の R0 の [1−REL, 1＋REL]。
#   G3 対照：Seceded・Returned（movedAgain・foreignAdded を除く）で identBreakFlag＝0（計器が double で数えた差）。
#   G4 穴：Ceded の後の receiverOwnAfterShare＝0・thirdPartyAfterSum＝thirdPartyBeforeSum（|差|≦TOL）。母集合（receiverOwnBeforeShare＞0）の数は別に印字。
#   G5 保存：前と後の 4 区分の和＝1（|差|≦SUMTOL＝4 つの G10 の印字の丸めの和の上界）。
#   G6 届き：pickedReceiverFlag＝1 の行で deliveryAfterShare＝c1DeliveryShare・reachAfterShare＝c1ReachShare（|差|≦TOL・両辺とも四半期の末の AdminFill）。
import sys, os, glob, collections

TOL = 1e-12
REL = 1e-9
SUMTOL = 2e-9
PTD = 0.3   # PrivilegeTaxDiscount の既定（Coefficients.cs・紙の台本の届きの式だけに使う）

def fnum(s):
    if s == 'nan': return float('nan')
    if s == 'inf': return float('inf')
    if s == '-inf': return float('-inf')
    return float(s)

def read(path):
    with open(path, 'rb') as fh:
        lines = fh.read().decode('utf-8').split('\n')
    lines = [l.rstrip('\r') for l in lines if l.strip()]
    return lines[0].split('\t'), [l.split('\t') for l in lines[1:]]

def near(a, b, tol=TOL):
    return abs(a - b) <= tol

def gates(header, rows):
    ix = {c: i for i, c in enumerate(header)}
    g = lambda x, c: fnum(x[ix[c]])
    o = collections.Counter(); s_b = 0.0; s_moved = 0.0
    for x in rows:
        how = x[ix['how']]
        if x[ix['movedAgainFlag']] == '1' or x[ix['foreignAddedFlag']] == '1':
            o['excl'] += 1; continue
        if how != 'Ceded':
            o['pop_G3'] += 1
            if x[ix['identBreakFlag']] != '0': o['G3_fail'] += 1
            continue
        o['pop'] += 1
        r0b, r1b, r2b, r3b = (g(x, 'rule%sBeforeShare' % p) for p in ('State', 'Local', 'Foreign', 'Ungov'))
        r0a, r1a, r2a, r3a = (g(x, 'rule%sAfterShare' % p) for p in ('State', 'Local', 'Foreign', 'Ungov'))
        ok1 = (near(r0a, 0.0) and near(r1a, g(x, 'c1LocalShare')) and near(r2a, r2b) and near(r3a, r3b)
               and near(g(x, 'ruleInitLocalAfterShare'), g(x, 'ruleInitLocalBeforeShare'))
               and near(g(x, 'dominanceAfterShare'), g(x, 'dominanceBeforeShare')))
        if not ok1: o['G1_fail'] += 1
        s_b += r0b; s_moved += r0b - r0a
        if g(x, 'receiverOwnBeforeShare') > 0: o['G4_ownBefore_pos'] += 1
        if not (near(g(x, 'receiverOwnAfterShare'), 0.0) and near(g(x, 'thirdPartyAfterSum'), g(x, 'thirdPartyBeforeSum'))): o['G4_fail'] += 1
        if not (near(r0b + r1b + r2b + r3b, 1.0, SUMTOL) and near(r0a + r1a + r2a + r3a, 1.0, SUMTOL)): o['G5_fail'] += 1
        if x[ix['pickedReceiverFlag']] == '1':
            o['pop_G6'] += 1
            if not (near(g(x, 'deliveryAfterShare'), g(x, 'c1DeliveryShare')) and near(g(x, 'reachAfterShare'), g(x, 'c1ReachShare'))): o['G6_fail'] += 1
    g2 = s_b > 0 and (1 - REL) * s_b <= s_moved <= (1 + REL) * s_b
    return o, s_b, s_moved, g2

def run_dir(d, mutate=None):
    files = sorted(glob.glob(os.path.join(d, 'cederule_s*.tsv')), key=lambda p: int(os.path.basename(p)[len('cederule_s'):-4]))
    tot = collections.Counter(); lines = []; g2fail = 0; sb_all = []
    for p in files:
        h, rows = read(p)
        if mutate: rows = [mutate(h, x) for x in rows]
        o, sb, sm, g2 = gates(h, rows)
        tot.update(o); sb_all.append(sb)
        if not g2: g2fail += 1
        seed = os.path.basename(p)[len('cederule_s'):-4]
        lines.append('seed %s\tpop %d\tG1_fail %d\tsumR0before %.6f\tsumMoved %.6f\tG2 %s\tG3_pop %d\tG3_fail %d\tG4_fail %d\tG4_ownBefore_pos %d\tG5_fail %d\tG6_pop %d\tG6_fail %d' % (
            seed, o['pop'], o['G1_fail'], sb, sm, 'ok' if g2 else 'NG', o['pop_G3'], o['G3_fail'], o['G4_fail'], o['G4_ownBefore_pos'], o['G5_fail'], o['pop_G6'], o['G6_fail']))
    red = [k for k, bad in (('G1', tot['G1_fail'] > 0), ('G2', g2fail > 0), ('G3', tot['G3_fail'] > 0), ('G4', tot['G4_fail'] > 0),
                            ('G5', tot['G5_fail'] > 0), ('G6', tot['G6_fail'] > 0)) if bad]
    nopop = tot['pop'] == 0 or len(files) == 0
    lines.append('total\tseeds %d\tpop %d\texcl %d\tG1_fail %d\tG2_fail_seeds %d\tG3_pop %d\tG3_fail %d\tG4_fail %d\tG4_ownBefore_pos %d\tG5_fail %d\tG6_pop %d\tG6_fail %d\tsumR0before_per_seed_mean %.3f' % (
        len(files), tot['pop'], tot['excl'], tot['G1_fail'], g2fail, tot['pop_G3'], tot['G3_fail'], tot['G4_fail'], tot['G4_ownBefore_pos'], tot['G5_fail'], tot['pop_G6'], tot['G6_fail'],
        sum(sb_all) / len(sb_all) if sb_all else float('nan')))
    lines.append('GATE ' + ('表なし' if nopop else ('赤 ' + ' '.join(red) if red else '全て緑')))
    return lines, red, nopop

# ---- 紙の突然変異：旗 0 の盤面の行（前＝後）から、各変異の規則で〈後〉の列を作る ----
def G10(v):
    return 'nan' if v != v else ('%.10g' % v)

def deliv(r0, r1, af):
    d = 1.0 - PTD
    return max(0.0, min(1.0, r0)) * (d + (1 - d) * max(0.0, min(1.0, af))) + max(0.0, min(1.0, r1)) * d

def make_mut(name):
    def m(h, x):
        ix = {c: i for i, c in enumerate(h)}
        y = list(x); g = lambda c: fnum(x[ix[c]])
        how = x[ix['how']]
        r = [g('rule%sBeforeShare' % p) for p in ('State', 'Local', 'Foreign', 'Ungov')]
        init = g('ruleInitLocalBeforeShare'); dom = g('dominanceBeforeShare')
        own = g('receiverOwnBeforeShare'); third = g('thirdPartyBeforeSum')
        applies = how == 'Ceded' or (name == 'M4' and how in ('Seceded', 'Returned'))
        if name == 'M8' or not applies:
            return y
        a = list(r); initA = init; domA = dom; ownA = own; thirdA = third
        if name in ('M0', 'M2', 'M3', 'M4', 'M5', 'M7'): a[1] = r[0] + r[1]; a[0] = 0.0
        if name == 'M1': a[3] = r[3] + r[0]; a[0] = 0.0
        if name == 'M2': initA = init + r[0]
        if name == 'M5': domA = 0.0
        if name == 'M6': a[1] = r[1] + 0.5 * r[0]; a[0] = 0.5 * r[0]
        if name == 'M9': a[0] = 0.0
        if name != 'M3': ownA = 0.0
        if name == 'M7': thirdA = 0.0
        for k, p in enumerate(('State', 'Local', 'Foreign', 'Ungov')): y[ix['rule%sAfterShare' % p]] = G10(a[k])
        y[ix['ruleInitLocalAfterShare']] = G10(initA); y[ix['dominanceAfterShare']] = G10(domA)
        y[ix['receiverOwnAfterShare']] = G10(ownA); y[ix['thirdPartyAfterSum']] = G10(thirdA)
        af = g('adminFillAfterShare')
        y[ix['deliveryAfterShare']] = G10(deliv(a[0], a[1], af)); y[ix['reachAfterShare']] = G10(a[0])
        if name in ('M0', 'M2', 'M3', 'M4', 'M5', 'M7') and how == 'Ceded':
            # R0 と R1 の移しは正しい形＝R1 の後と届きは計器が C# の double で当てた列の印字そのもの（Python の和の丸めで門を偽に落とさない）
            y[ix['ruleLocalAfterShare']] = x[ix['c1LocalShare']]
            y[ix['deliveryAfterShare']] = x[ix['c1DeliveryShare']]; y[ix['reachAfterShare']] = x[ix['c1ReachShare']]
        if how != 'Ceded':   # M4：建国・再併合の行で Rule が動いた＝計器の差の印が立つ
            dm = max(abs(a[k] - r[k]) for k in range(4))
            y[ix['identBreakFlag']] = '1' if not (dm <= TOL) else '0'
        return y
    return m

MUTS = [('M0', 'C1＋H-a（正しい形）'), ('M1', 'R0 を無統治へ（形 C3）'), ('M2', 'RuleInit[1] にも足す（C1′）'), ('M3', '穴を消さない'),
        ('M4', '建国・再併合でも動かす'), ('M5', 'Dominance も 0'), ('M6', '半分だけ移す'), ('M7', '第三国の持分も消す'),
        ('M8', '旗 0（何もしない実装）'), ('M9', 'R1 を足し忘れる（R0 だけ 0）')]

def selftest():
    h = ['how', 'movedAgainFlag', 'foreignAddedFlag', 'identBreakFlag', 'pickedReceiverFlag',
         'ruleStateBeforeShare', 'ruleLocalBeforeShare', 'ruleForeignBeforeShare', 'ruleUngovBeforeShare',
         'ruleStateAfterShare', 'ruleLocalAfterShare', 'ruleForeignAfterShare', 'ruleUngovAfterShare',
         'ruleInitLocalBeforeShare', 'ruleInitLocalAfterShare', 'dominanceBeforeShare', 'dominanceAfterShare',
         'c1LocalShare', 'receiverOwnBeforeShare', 'receiverOwnAfterShare', 'thirdPartyBeforeSum', 'thirdPartyAfterSum',
         'deliveryAfterShare', 'c1DeliveryShare', 'reachAfterShare', 'c1ReachShare']
    good = ['Ceded', '0', '0', '1', '1', '0.5', '0.3', '0.1', '0.1', '0', '0.8', '0.1', '0.1', '0.4', '0.4', '0.7', '0.7', '0.8', '0.2', '0', '0.1', '0.1', '0.56', '0.56', '0', '0']
    bad = list(good); bad[h.index('ruleUngovAfterShare')] = '0.7'; bad[h.index('ruleStateAfterShare')] = '0'; bad[h.index('ruleLocalAfterShare')] = '0.3'
    sec = list(good); sec[0] = 'Seceded'; sec[h.index('identBreakFlag')] = '1'
    o1, sb1, sm1, g21 = gates(h, [good])
    o2, sb2, sm2, g22 = gates(h, [bad, sec])
    ok = (o1['G1_fail'] == 0 and g21 and o1['G4_fail'] == 0 and o1['G5_fail'] == 0 and o1['G6_fail'] == 0
          and o2['G1_fail'] == 1 and o2['G3_fail'] == 1 and o2['G5_fail'] == 1)
    print('selftest', 'OK' if ok else 'NG', dict(o1), dict(o2))
    return 0 if ok else 1

def main():
    a = sys.argv[1:]
    if a and a[0] == '--selftest': sys.exit(selftest())
    out = []
    if a and a[0] == '--paper':
        d = a[1]
        out.append('# (902) 紙の突然変異（盤面 %s の行に各変異の規則を当てて〈後〉の列を作り、門を数える）' % d)
        for k, desc in MUTS:
            lines, red, nopop = run_dir(d, make_mut(k))
            out.append('%s %s\t%s\t%s' % (k, desc, lines[-2], lines[-1]))
        out.append('M10 独立の確定（Sovereign）でも動かす\t表に行が無い（RegionMoved を通らない）＝機構の試験だけ')
        dst = a[2] if len(a) > 2 else None
    else:
        lines, red, nopop = run_dir(a[0]); out += lines
        dst = a[1] if len(a) > 1 else None
    text = '\n'.join(out) + '\n'
    sys.stdout.write(text)
    if dst:
        with open(dst, 'wb') as fh: fh.write(text.encode('utf-8'))

if __name__ == '__main__':
    main()
