# (888) c75ce 旗 0 の恒等の門（cederule_s）と段 C-1 の G4 の母集合の数え。
#   python -X utf8 scripts/instruments/c75ce/gate888.py <ins の生のフォルダ> [出力のファイル]
#   python -X utf8 scripts/instruments/c75ce/gate888.py --selftest
# 門：母集合＝movedAgainFlag＝0 かつ foreignAddedFlag＝0 の全ての行（Ceded・Seceded・Returned）。identBreakFlag（計器が C# の double で数えた差＞1e-12）の和＝0。
#   門は印字の桁（G10）で数え直さない＝計器の旗を数えるだけ（★(885)〈門の許しを印字の桁の床に置かない〉）。旗と identDiffMaxIndex の食い違い（旗 0 なのに差＞1e-12 と印字された行）も数える。
# 記録（採否の言い方にしない）：割譲の件数・格の内訳・前の receiverOwnBeforeShare＞0（G4 の母集合の上界）・印で外れる件数・受け手が計器の国の件数。
import sys, os, glob, collections

TOL = 1e-12   # 計器の CedeIdentTol と同じ値（旗の検算だけに使う）

def fnum(s):
    if s == 'nan': return float('nan')
    if s == 'inf': return float('inf')
    if s == '-inf': return float('-inf')
    return float(s)

def gate_rows(header, rows):
    """門の数え（1 つの種の行）。返り＝dict。"""
    ix = {c: i for i, c in enumerate(header)}
    out = collections.Counter()
    for x in rows:
        how = x[ix['how']]
        out['rows'] += 1; out['rows_' + how] += 1
        mv = x[ix['movedAgainFlag']] == '1'; ad = x[ix['foreignAddedFlag']] == '1'
        if mv: out['excl_movedAgain'] += 1
        if ad: out['excl_foreignAdded'] += 1
        if mv or ad: continue
        out['pop'] += 1; out['pop_' + how] += 1
        brk = x[ix['identBreakFlag']] == '1'
        if brk:
            out['breaks'] += 1; out['breaks_' + how] += 1
        dm = fnum(x[ix['identDiffMaxIndex']])
        if (not brk) and not (dm <= TOL): out['flag_mismatch'] += 1
        if how == 'Ceded':
            out['ceded_tier_' + x[ix['tierBefore']]] += 1
            if fnum(x[ix['receiverOwnBeforeShare']]) > 0: out['ceded_ownBefore_pos'] += 1
            if fnum(x[ix['thirdPartyBeforeSum']]) > 0: out['ceded_thirdBefore_pos'] += 1
            if x[ix['pickedReceiverFlag']] == '1': out['ceded_pickedReceiver'] += 1
    return out

def read(path):
    with open(path, 'rb') as fh:
        lines = fh.read().decode('utf-8').split('\n')
    lines = [l.rstrip('\r') for l in lines if l.strip()]
    return lines[0].split('\t'), [l.split('\t') for l in lines[1:]]

def selftest():
    """人工の行で門が破れを落とすか（旗 1 の行を数える・印の行を除く・旗と差の食い違いを拾う）。"""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    h = ['quarter', 'how', 'tierBefore', 'receiverOwnBeforeShare', 'thirdPartyBeforeSum', 'pickedReceiverFlag', 'movedAgainFlag', 'foreignAddedFlag', 'identDiffMaxIndex', 'identBreakFlag']
    rows = [
        ['0', 'Ceded', 'Core', '0', '0', '0', '0', '0', '0', '0'],
        ['0', 'Ceded', 'Colonial', '0.2', '0', '1', '0', '0', '1e-06', '1'],    # 人工の破れ
        ['0', 'Seceded', 'Core', '0', '0', '0', '1', '0', '0.5', '1'],          # 印で外す
        ['0', 'Returned', 'Core', '0', '0', '0', '0', '1', 'inf', '1'],         # 印で外す
        ['0', 'Returned', 'Core', '0', '0', '0', '0', '0', '1e-06', '0'],       # 旗と差の食い違い
    ]
    o = gate_rows(h, rows)
    ok = (o['pop'] == 3 and o['breaks'] == 1 and o['excl_movedAgain'] == 1 and o['excl_foreignAdded'] == 1 and o['flag_mismatch'] == 1
          and o['ceded_ownBefore_pos'] == 1 and o['ceded_pickedReceiver'] == 1)
    print('selftest', 'OK' if ok else 'NG', dict(o))
    return 0 if ok else 1

def main():
    if len(sys.argv) > 1 and sys.argv[1] == '--selftest':
        sys.exit(selftest())
    d = sys.argv[1]
    files = sorted(glob.glob(os.path.join(d, 'cederule_s*.tsv')), key=lambda p: int(os.path.basename(p)[len('cederule_s'):-4]))
    tot = collections.Counter(); lines = []
    for p in files:
        h, rows = read(p)
        o = gate_rows(h, rows)
        tot.update(o)
        seed = os.path.basename(p)[len('cederule_s'):-4]
        lines.append('seed %s\trows %d\tpop %d\tbreaks %d\texcl_movedAgain %d\texcl_foreignAdded %d\tflag_mismatch %d\tCeded %d\tSeceded %d\tReturned %d' % (
            seed, o['rows'], o['pop'], o['breaks'], o['excl_movedAgain'], o['excl_foreignAdded'], o['flag_mismatch'], o['rows_Ceded'], o['rows_Seceded'], o['rows_Returned']))
    lines.append('total\t' + '\t'.join('%s %d' % (k, tot[k]) for k in sorted(tot)))
    verdict = 'GATE 恒等の門 破れ 0' if tot['breaks'] == 0 and tot['flag_mismatch'] == 0 and len(files) > 0 else 'GATE 恒等の門 破れあり（または表なし）'
    lines.append(verdict + '\tseeds %d' % len(files))
    text = '\n'.join(lines) + '\n'
    sys.stdout.write(text)
    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as fh:
            fh.write(text.encode('utf-8'))

if __name__ == '__main__':
    main()
