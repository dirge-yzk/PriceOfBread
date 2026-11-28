# (1432) 合流：c153rg/rg_gate.py の写し（種の起点を環境変数 SB で渡す＝S11 は SB=11・他は同じ）。
# (1428) c153rg の門と記録の台本（生を読むだけ・エンジンを呼ばない・Coefficients を読まない）。
# 使い方: python -X utf8 -I rg_gate.py arm=<生のフォルダ> base=<基線の生> [n1=<ヌル腕の生> n2=...] [--seeds 10]
#   入力＝tensionwhy_s<種>.tsv（四半期の末・列強を含む組・緊張 1 以上・理由ごとの内訳＝Diplomacy.TensionBreakdown）と wars_s<種>.tsv。
# 門 G1（主・誰に届いたか）：tensionwhy_s の Religion 列の絶対値が 1e-6 以下の行の割合＝各の種で 1（10/10 種）。
#   許し 1e-6 は印字（R 形式＝往復の桁）より広く、旧の最小の 1 か月の寄与（0.03×座標の差の最小 0.07＝0.0021）より狭い。
#   旧の盤面（基線）では宗教の内訳を持つ行が数えられる＝門は落ちる（紙の台本で確かめる）。
# 門 G2（作る物の大きさの下限＝★(1425)）：母集合（tensionwhy_s の行＝列強を含む組×四半期で緊張 1 以上）の行数が、同じ種の基線の 0.5 倍以上（組を消していない）。
#   宗教が空の国は tensionwhy_s から見えない＝母集合は〈表の全ての行〉（上位の集合）で読む。基線の Religion≠0 の行数は記録に書く。
# 記録：緊張の総量に占める宗教の取り分（基線の Σ Religion ÷ Σ tension）・Protector の和の新÷旧・閾値（20・40・50・60・70・110・140）以上の行の数の新÷旧・
#   戦争の数（wars_s の行・内戦の旗 0 と 1）。
import sys, os, collections
SB = int(__import__('os').environ.get('SB', '1'))   # (1432) 合流：種の起点（S11 は SB=11）

def args():
    d = {}; n = 10
    a = sys.argv[1:]
    i = 0
    while i < len(a):
        if a[i] == '--seeds': n = int(a[i + 1]); i += 2; continue
        k, v = a[i].split('=', 1); d[k] = v; i += 1
    return d, n

TH = [20, 40, 50, 60, 70, 110, 140]

def tw(path):
    r = dict(rows=0, zero=0, relnz=0, rel=0.0, relpos=0.0, relneg=0.0, ten=0.0, prot=0.0, th=collections.Counter(), finite=0)
    if not os.path.exists(path): return None
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        ir, it, ip = ix['Religion'], ix['tension'], ix['Protector']
        for line in f:
            c = line.rstrip('\n').split('\t')
            if len(c) <= ir: continue
            r['rows'] += 1
            v = float(c[ir]); t = float(c[it]); p = float(c[ip])
            if v == v and t == t: r['finite'] += 1
            if abs(v) <= 1e-6: r['zero'] += 1
            else: r['relnz'] += 1
            r['rel'] += v; r['ten'] += t; r['prot'] += p
            if v > 0: r['relpos'] += v
            else: r['relneg'] += v
            for x in TH:
                if t >= x: r['th'][x] += 1
    return r

def wars(path):
    if not os.path.exists(path): return None
    n0 = n1 = 0
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ic = h.index('civilWarFlag')
        for line in f:
            c = line.rstrip('\n').split('\t')
            if len(c) <= ic: continue
            if c[ic] == '1': n1 += 1
            else: n0 += 1
    return n0, n1

def main():
    d, n = args()
    sides = [k for k in ('arm', 'base', 'n1', 'n2') if k in d]
    T = {k: {} for k in sides}; W = {k: {} for k in sides}
    for k in sides:
        for s in range(SB, SB + n):
            T[k][s] = tw(os.path.join(d[k], 'tensionwhy_s%d.tsv' % s))
            W[k][s] = wars(os.path.join(d[k], 'wars_s%d.tsv' % s))
    print('# (1428) c153rg 門 G1・G2 と記録  sides=' + ','.join('%s=%s' % (k, d[k]) for k in sides))
    print('side\tseed\trows\tzero\tfracZero\trelNonzero\tsumRel\tsumRelPos\tsumRelNeg\tsumTension\trelShare\tsumProt\twars\tcivil')
    for k in sides:
        for s in range(SB, SB + n):
            r = T[k][s]; w = W[k][s] or (0, 0)
            if r is None: print('%s\t%d\tMISSING' % (k, s)); continue
            fz = r['zero'] / r['rows'] if r['rows'] else float('nan')
            sh = r['rel'] / r['ten'] if r['ten'] else float('nan')
            print('%s\t%d\t%d\t%d\t%.6f\t%d\t%.4f\t%.4f\t%.4f\t%.4f\t%.6f\t%.4f\t%d\t%d' % (k, s, r['rows'], r['zero'], fz, r['relnz'], r['rel'], r['relpos'], r['relneg'], r['ten'], sh, r['prot'], w[0], w[1]))
    # 門は arm と base がそろったときだけ
    if 'arm' in T and 'base' in T:
        g1 = g2 = 0; miss = 0
        for s in range(SB, SB + n):
            a, b = T['arm'][s], T['base'][s]
            if a is None or b is None or a['rows'] == 0: miss += 1; continue
            if a['zero'] == a['rows'] and a['finite'] == a['rows']: g1 += 1
            if a['rows'] >= 0.5 * b['rows']: g2 += 1
        print('G1 Religion 列が |x|<=1e-6 の割合が 1 の種: %d/%d（採る条件＝%d/%d）  欠け=%d' % (g1, n, n, n, miss))
        print('G2 行数が基線の 0.5 倍以上の種: %d/%d（採る条件＝%d/%d）' % (g2, n, n, n))
        print('GATE G1=%s G2=%s' % ('PASS' if g1 == n else 'FAIL', 'PASS' if g2 == n else 'FAIL'))
        # 記録：新÷旧
        def tot(k, f):
            return sum(f(T[k][s]) for s in range(SB, SB + n) if T[k][s] is not None)
        for nm, f in (('rows', lambda r: r['rows']), ('sumTension', lambda r: r['ten']), ('sumProt', lambda r: r['prot'])):
            a, b = tot('arm', f), tot('base', f)
            print('記録 %s 新=%.4f 旧=%.4f 新÷旧=%.4f' % (nm, a, b, a / b if b else float('nan')))
        for x in TH:
            a = tot('arm', lambda r: r['th'][x]); b = tot('base', lambda r: r['th'][x])
            print('記録 緊張>=%d の行 新=%d 旧=%d 新÷旧=%.4f' % (x, a, b, a / b if b else float('nan')))
        wa = [W['arm'][s] or (0, 0) for s in range(SB, SB + n)]; wb = [W['base'][s] or (0, 0) for s in range(SB, SB + n)]
        print('記録 戦争（内戦の旗 0） 新=%d 旧=%d・内戦 新=%d 旧=%d' % (sum(x[0] for x in wa), sum(x[0] for x in wb), sum(x[1] for x in wa), sum(x[1] for x in wb)))
    for k in sides:
        rel = sum(T[k][s]['rel'] for s in range(SB, SB + n) if T[k][s]); ten = sum(T[k][s]['ten'] for s in range(SB, SB + n) if T[k][s])
        print('記録 %s 宗教の取り分 ΣReligion÷Σtension=%.6f' % (k, rel / ten if ten else float('nan')))

if __name__ == '__main__':
    main()
