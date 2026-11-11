# (902) c77ce 記録（門ではない）：割譲の州の支配度（Dominance）の前・後・4 年後・10 年後と、国家区分 R0 の戻り。
#   python -X utf8 -I scripts/instruments/c77ce/dom902.py <ins の生のフォルダ> [出力のファイル]
# 母集合＝cederule_s の Ceded・movedAgainFlag＝0・foreignAddedFlag＝0。追跡＝rule_s の同じ州の行（計器の国は毎四半期・他の国は年末＝粒度が粗いと名乗る）。
#   割譲の後の最初の rule_s の行の持ち主を受け手とみなし、+16・+40 四半期以上の最初の行まで持ち主が同じ州だけを数える（替わった州・試合の終わりで切れた州は打切りとして数える）。
# 印字：中央値と p10・p90。R0 の戻り＝R0(t)÷割譲の前の R0（前の R0＞1e-9 の州）。
import sys, os, glob, collections

def read(path):
    with open(path, 'rb') as fh:
        lines = fh.read().decode('utf-8').split('\n')
    lines = [l.rstrip('\r') for l in lines if l.strip()]
    return lines[0].split('\t'), [l.split('\t') for l in lines[1:]]

def q(v, p):
    if not v: return float('nan')
    s = sorted(v); return s[min(len(s) - 1, int(p * (len(s) - 1) + 0.5))]

def fmt(v):
    return 'n %d\tp10 %.4f\tmed %.4f\tp90 %.4f' % (len(v), q(v, 0.1), q(v, 0.5), q(v, 0.9))

def main():
    d = sys.argv[1]; dst = sys.argv[2] if len(sys.argv) > 2 else None
    acc = collections.defaultdict(list); cens = collections.Counter(); n = 0
    for p in sorted(glob.glob(os.path.join(d, 'cederule_s*.tsv'))):
        seed = os.path.basename(p)[len('cederule_s'):-4]
        h, rows = read(p); ix = {c: i for i, c in enumerate(h)}
        ev = []
        for x in rows:
            if x[ix['how']] != 'Ceded' or x[ix['movedAgainFlag']] == '1' or x[ix['foreignAddedFlag']] == '1': continue
            ev.append((int(x[ix['quarter']]), x[ix['region']], float(x[ix['ruleStateBeforeShare']]), float(x[ix['ruleStateAfterShare']]),
                       float(x[ix['dominanceBeforeShare']]), float(x[ix['dominanceAfterShare']])))
        if not ev: continue
        want = {e[1] for e in ev}
        rp = os.path.join(d, 'rule_s%s.tsv' % seed)
        hr, rr = read(rp); jx = {c: i for i, c in enumerate(hr)}
        byreg = collections.defaultdict(list)
        for x in rr:
            if x[jx['region']] in want:
                byreg[x[jx['region']]].append((int(x[jx['quarter']]), x[jx['nation']], float(x[jx['ruleStateShare']]), float(x[jx['dominanceShare']])))
        for v in byreg.values(): v.sort()
        for (q0, reg, r0b, r0a, db, da) in ev:
            n += 1
            acc['dom_before'].append(db); acc['dom_after'].append(da); acc['r0_before'].append(r0b); acc['r0_after'].append(r0a)
            tr = [t for t in byreg.get(reg, []) if t[0] > q0]
            if not tr: cens['no_track'] += 1; continue
            owner = tr[0][1]
            for lag, tag in ((16, '4y'), (40, '10y')):
                hit = None
                for t in tr:
                    if t[1] != owner: break
                    if t[0] >= q0 + lag: hit = t; break
                if hit is None: cens['cens_' + tag] += 1; continue
                acc['dom_' + tag].append(hit[3]); acc['r0_' + tag].append(hit[2])
                if r0b > 1e-9: acc['r0ratio_' + tag].append(hit[2] / r0b)
                acc['domdiff_' + tag].append(hit[3] - db)
    out = ['# (902) 割譲の州の支配度と R0 の戻り（%s・Ceded の母集合 %d 件）' % (d, n)]
    for k in ('dom_before', 'dom_after', 'dom_4y', 'dom_10y', 'domdiff_4y', 'domdiff_10y', 'r0_before', 'r0_after', 'r0_4y', 'r0_10y', 'r0ratio_4y', 'r0ratio_10y'):
        out.append('%s\t%s' % (k, fmt(acc[k])))
    out.append('dominance＜0.999 の件数\t前 %d\t後 %d\t4 年後 %d\t10 年後 %d' % tuple(sum(1 for v in acc[k] if v < 0.999) for k in ('dom_before', 'dom_after', 'dom_4y', 'dom_10y')))
    out.append('打切り\t' + '\t'.join('%s %d' % (k, cens[k]) for k in sorted(cens)))
    text = '\n'.join(out) + '\n'
    sys.stdout.write(text)
    if dst:
        with open(dst, 'wb') as fh: fh.write(text.encode('utf-8'))

if __name__ == '__main__':
    main()
