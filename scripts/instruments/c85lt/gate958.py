# (958) c85lt：gate937.py の写し（窓の定数だけ今の盤面へ）。元：(937) c82lt 門の台本（事前登録 measurements/m937_20261107_prereg_lt.txt [4]）：landfix_s を読んで G4・G7・G8a・G8b・G8c と記録（Gi・計器の写しの B）を書く。
#   python -X utf8 gate937.py <ins のフォルダ> [種の起点=1]
#   最後の行  GATES PASS|FAIL <落ちた門>  。窓は事前登録の値を写した定数（結果を見た後に変えない）。
import csv, glob, math, os, sys
from collections import defaultdict

csv.field_size_limit(1 << 30)
LIST = ['イギリス', '清']
WIN = {'イギリス': (0.0985, 0.4725), '清': (0.2314, 1.2594)}   # (958) 事前登録 [2]：同じ形の窓を今の盤面（keep_954/base_ins・keep_947/n1・n2）で取り直した（measurements/m958_20261108/paper937_m947.txt）
G7_FLOOR = 20


def num(s):
    try:
        return float(s)
    except Exception:
        return float('nan')


def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs:
        return float('nan')
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])


def main():
    ins = sys.argv[1]
    sb = int(sys.argv[2]) if len(sys.argv) > 2 else 1
    files = sorted(glob.glob(os.path.join(ins, 'landfix_s*.tsv')))
    rows = []
    for p in files:
        seed = int(os.path.basename(p)[len('landfix_s'):-4])
        if not (sb <= seed < sb + 10):
            continue
        with open(p, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                r['_seed'] = seed
                rows.append(r)
    out = []
    W = out.append
    seeds = sorted({r['_seed'] for r in rows})
    W('# (937) gate937 %s・種 %s・landfix_s のファイル %d・行 %d' % (ins, seeds, len(files), len(rows)))
    fail = []
    ev = 0
    for n in LIST:
        for s in seeds:
            q0 = [r for r in rows if r['_seed'] == s and r['nation'] == n and r['quarter'] == '0']
            if q0 and num(q0[0]['lbFreezeEventFlag']) == 1 and num(q0[0]['lbWarmupBreakPopMonths']) == 0:
                ev += 1
    W('G7 凍結の事象（名簿の国×種・開幕の四半期〔助走の後の最初の月〕で事象 1 かつ助走の月の差分の破れ 0）＝%d（床 %d）' % (ev, G7_FLOOR))
    if ev < G7_FLOOR:
        fail.append('G7')
    L = [r for r in rows if r['nation'] in LIST]
    s4 = sum(num(r['lbOverBasePopMonths']) for r in L)
    s8a = sum(num(r['lbAdjBreakPopMonths']) for r in L)
    s8b = sum(num(r['lbFrozenInvariantBreak']) for r in L)
    s8bn = sum(num(r['lbFrozenCopyNoEngine']) for r in L)
    s8bf = sum(num(r['lbFrozenForeignEngine']) for r in L)
    sf_all = sum(num(r['lbFrozenForeignEngine']) for r in rows if r['nation'] not in LIST)
    W('G4 掛けない（名簿の国の lbOverBasePopMonths の和）＝%g（0 で通る）' % s4)
    W('G8a 実装の点検（名簿の国の lbAdjBreakPopMonths の和）＝%g（0 で通る）' % s8a)
    W('G8b 凍結の不変（lbFrozenInvariantBreak・lbFrozenCopyNoEngine・lbFrozenForeignEngine の和）＝%g・%g・%g（全て 0 で通る）・記録：名簿の外の国の lbFrozenForeignEngine＝%g' % (s8b, s8bn, s8bf, sf_all))
    if s4 != 0:
        fail.append('G4')
    if s8a != 0:
        fail.append('G8a')
    if s8b != 0 or s8bn != 0 or s8bf != 0:
        fail.append('G8b')
    for n in LIST:
        lg, lgc = [], []
        for r in rows:
            if r['nation'] != n:
                continue
            R, E, C = num(r['baseRAtTaxPerMonth']), num(r['lbEngBaseRAtTaxPerMonth']), num(r['lbExpBaseRAtTaxPerMonth'])
            if R > 0 and E > 0:
                lg.append(abs(math.log(E / R)))
            if R > 0 and C > 0:
                lgc.append(abs(math.log(C / R)))
        m = med(lg)
        lo, hi = WIN[n]
        ok = lo <= m <= hi
        W('G8c 働いた %s：|log(エンジンの基数÷地代)| の中央 %.4f（行 %d）窓 [%.4f, %.4f] → %s・記録：計器の写しの |log B| の中央 %.4f（行 %d）' % (n, m, len(lg), lo, hi, '内' if ok else '外', med(lgc), len(lgc)))
        if not ok:
            fail.append('G8c:' + n)
    gi = sum(1 for r in rows if num(r['landPaidOverClaimAtTax']) > 1 + 1e-9)
    gin = sum(1 for r in rows if num(r['landPaidOverClaimAtTax']) == num(r['landPaidOverClaimAtTax']))
    W('Gi（記録）：landPaidOverClaimAtTax＞1+1e-9 の行 %d／比のある行 %d（全ての国）' % (gi, gin))
    W('GATES %s %s' % ('PASS' if not fail else 'FAIL', ','.join(fail)))
    sys.stdout.buffer.write(('\n'.join(out) + '\n').encode('utf-8'))   # LF のまま（Windows の文字モードの CRLF にしない）


if __name__ == '__main__':
    main()
