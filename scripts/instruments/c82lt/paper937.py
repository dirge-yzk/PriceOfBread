# (937) c82lt 紙の台本（腕の前・旗 0 の盤面 keep_933 の 3 腕）：門 G7・G8c の紙の値と、旗 0 で門が落ちること・物差しが常に落ちる形でないこと・no-go が恒真でないことを数える。
#   python -X utf8 paper937.py <出力.txt> <腕の名=ins のフォルダ> ...
#   [G8c] 名簿の国（イギリス・清）の |log baseOverFirstSeenRatio| の中央（腕ごと）→ 窓 [最小×1/2, 最大×2]・旗 0 の B を自分の窓に当てる・M3（B≡1＝|log|=0）を当てる
#   [G7] 凍結の事象の見込み＝名簿の国×種で q0 の landClaimPerMonth＞0 かつ baseRPerMonth＞0 の数（20 の見込み）・旗 0 の盤面には landfix_s が無い＝事象 0＝落ちる
#   [NG-ii] no-go (ii)〈名簿の国の q0 の請求＞0 の種が 10 未満〉を全ての国に当てる（述語が定数でないこと＝q0 の請求 0 の国×種がある）
import csv, glob, math, os, sys
from collections import defaultdict

csv.field_size_limit(1 << 30)
LIST = ['イギリス', '清']


def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs:
        return float('nan')
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])


def num(s):
    try:
        return float(s)
    except Exception:
        return float('nan')


def read(ins):
    rows = []
    for p in sorted(glob.glob(os.path.join(ins, 'landbase_s*.tsv'))):
        b = os.path.basename(p)
        if b.startswith('landbasereg'):
            continue
        seed = int(b[len('landbase_s'):-4])
        with open(p, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                rows.append((seed, int(r['quarter']), r['nation'], num(r['landClaimPerMonth']), num(r['baseRPerMonth']), num(r['baseOverFirstSeenRatio'])))
    return rows


def main():
    out = []
    W = out.append
    arms = [a.split('=', 1) for a in sys.argv[2:]]
    med_by = defaultdict(dict)
    for name, ins in arms:
        rows = read(ins)
        fix = len(glob.glob(os.path.join(ins, 'landfix_s*.tsv')))
        W('== 腕 %s（%s）：landbase_s の行 %d・landfix_s のファイル %d' % (name, ins, len(rows), fix))
        for n in LIST:
            lg = [abs(math.log(t[5])) for t in rows if t[2] == n and t[5] == t[5] and t[5] > 0]
            med_by[n][name] = med(lg)
            W('  [G8c] %s：|log B| の中央 %.4f（行 %d）' % (n, med_by[n][name], len(lg)))
        q0 = defaultdict(dict)
        for s, q, n, cl, R, B in rows:
            if q == 0:
                q0[n][s] = (cl, R)
        seeds = sorted({t[0] for t in rows})
        ev = sum(1 for n in LIST for s in seeds if s in q0[n] and q0[n][s][0] > 0 and q0[n][s][1] > 0)
        W('  [G7] 凍結の事象の見込み（名簿の国×種で q0 の請求＞0 かつ R＞0）＝%d（種 %d）・旗 0 の事象＝0（landfix_s が無い＝凍結の欄が無い木）' % (ev, len(seeds)))
        zero = []
        for n, d in sorted(q0.items()):
            k = sum(1 for s in seeds if s in d and d[s][0] > 0)
            if k < len(seeds):
                zero.append('%s %d/%d' % (n, k, len(seeds)))
        W('  [NG-ii] 名簿の国：%s・q0 の請求＞0 の種が全部でない国（述語が定数でない例）：%s' % (
            '・'.join('%s %d/%d' % (n, sum(1 for s in seeds if s in q0[n] and q0[n][s][0] > 0), len(seeds)) for n in LIST),
            '・'.join(zero[:12]) + ('…ほか %d' % (len(zero) - 12) if len(zero) > 12 else '') if zero else 'なし'))
    W('== 窓（G8c・腕の前に固定）')
    for n in LIST:
        v = [x for x in med_by[n].values() if x == x]
        lo, hi = min(v) / 2, max(v) * 2
        W('  %s：[%.4f, %.4f]（3 腕の中央 %s）・旗 0 の B を自分に当てる＝%s・M3（|log B|=0）＝%s' % (
            n, lo, hi, '・'.join('%.4f' % x for x in v),
            '全て内' if all(lo <= x <= hi for x in v) else '外あり', '落ちる' if not (lo <= 0.0) else '通る'))
    with open(sys.argv[1], 'wb') as fh:
        fh.write(('\n'.join(out) + '\n').encode('utf-8'))


if __name__ == '__main__':
    main()
