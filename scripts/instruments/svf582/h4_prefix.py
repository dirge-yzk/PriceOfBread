#!/usr/bin/env python
"""(582) 線 svf2：H4（旗が動かすのは天井に触れた後だけ）の読み手。
使い方：PYTHONUTF8=1 python h4_prefix.py <基線の ins（m576_ins）> <腕 B の ins> [-v]
  腕 B の pclamp_s<seed>.tsv の最初の kind=ceil の行（全ての国・助走を含む）の時刻 t_c を求める（無ければ t_c＝∞）。
  時刻の順＝w0…w19（助走）→q0…（本番）。money_s は time の順位が t_c より小さい行、nation_s は quarter が t_c の q 番号より小さい行（t_c が助走〔w〕なら nation_s は比べる行が無い）を、
  基線と B で行ごとにバイトで比べる（行の順は表の順のまま）。t_c が無い種は表を全行比べる。
  出力＝種ごとに t_c・比べた行数・食い違い行数・最初の食い違い。食い違いが 1 行でもあれば H4 は外れ（旗が他の経路を動かしている）。
"""
import sys, re, glob, os


def trank(t):
    m = re.fullmatch(r'([wq])(\d+)', t)
    return (0 if m.group(1) == 'w' else 1, int(m.group(2))) if m else (9, 0)


def lines(path):
    if not os.path.exists(path):
        return None
    with open(path, 'rb') as fh:
        return fh.read().split(b'\n')


def t_c_of(path):
    L = lines(path)
    if L is None:
        return None
    head = L[0].decode('utf-8').split('\t')
    iT, iK = head.index('time'), head.index('kind')
    best = None
    for ln in L[1:]:
        if not ln:
            continue
        c = ln.decode('utf-8').rstrip('\r').split('\t')
        if c[iK] == 'ceil':
            r = trank(c[iT])
            if best is None or r < best[0]:
                best = (r, c[iT])
    return best


def compare(a_path, b_path, keep):
    A, B = lines(a_path), lines(b_path)
    if A is None or B is None:
        return None
    ha = A[0].decode('utf-8').rstrip('\r').split('\t')
    assert A[0] == B[0], 'ヘッダーが違う'
    ra = [l for l in A[1:] if l and keep(l.decode('utf-8').rstrip('\r').split('\t'), ha)]
    rb = [l for l in B[1:] if l and keep(l.decode('utf-8').rstrip('\r').split('\t'), ha)]
    n = min(len(ra), len(rb))
    diff = 0
    first = None
    for i in range(n):
        if ra[i] != rb[i]:
            diff += 1
            if first is None:
                first = i
    diff += abs(len(ra) - len(rb))
    return n, diff, first


def main(base, arm, verbose):
    seeds = sorted(int(re.search(r'money_s(\d+)', p).group(1)) for p in glob.glob(f'{arm}/money_s*.tsv'))
    bad = 0
    for s in seeds:
        tc = t_c_of(f'{arm}/pclamp_s{s}.tsv')
        if tc is None:
            def keep_m(c, h):
                return True

            def keep_n(c, h):
                return True
            label = 't_c＝∞（天井に触れない）'
        else:
            def keep_m(c, h, tc=tc):
                return trank(c[h.index('time')]) < tc[0]

            def keep_n(c, h, tc=tc):
                if tc[0][0] == 0:
                    return False
                return int(c[h.index('quarter')]) < tc[0][1]
            label = f't_c＝{tc[1]}'
        rm = compare(f'{base}/money_s{s}.tsv', f'{arm}/money_s{s}.tsv', keep_m)
        rn = compare(f'{base}/nation_s{s}.tsv', f'{arm}/nation_s{s}.tsv', keep_n)
        dm = rm[1] if rm else -1
        dn = rn[1] if rn else -1
        if dm != 0 or dn != 0:
            bad += 1
        extra = f'  最初の食い違い money_s#{rm[2] if rm else None} nation_s#{rn[2] if rn else None}' if verbose and (dm or dn) else ''
        print(f'種{s}: {label}  money_s 比べた{rm[0] if rm else "-"}行 食い違い{dm}  nation_s 比べた{rn[0] if rn else "-"}行 食い違い{dn}{extra}')
    print(f'H4 外れた種（0）={bad}／{len(seeds)}')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], '-v' in sys.argv)
