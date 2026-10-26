# (572) 段0 の探りの読み手：同じペースで形を比べる観測量（事前登録 追記4 ④）。
# 使い方：PYTHONUTF8=1 python -I an_probe.py <ins の親フォルダ>/<tag>_s{seed}/ins ... （1 つ以上）  各 ins は種ごとに別フォルダ。基線は keep_560/m560_ins（種 2・3）。
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import an_dsc as a

BASE = 'D:/PoB_work/keep_560/m560_ins'
KEYS = ['蒸気機関', '汽船', '機関車', '転炉鋼', '石炭火力']


def read(d, s, year):
    ev = [r for r in a.events(d, s) if r['year'] <= year]
    rows_main = {r['row'] for r in ev if a.mainname(r['nation'])}
    by = collections.defaultdict(list)
    for r in ev:
        if r['decade'] > 1780:
            by[r['row']].append((r['year'], r['quarter'], r['nation']))
    cn = tot = 0
    strict = 0
    share = 0.0
    sizes = []
    for row, L in by.items():
        q0 = min(q for _, q, _ in L)
        first = {n for _, q, n in L if q == q0}
        tot += 1
        has_cn = any(a.mainname(n) == '清' for n in first)
        cn += has_cn
        strict += has_cn and len(first) == 1
        share += (1.0 / len(first)) if has_cn else 0.0
        sizes.append(len(first))
    d2 = sum(1 for r in ev if r['decade'] >= 1800 and r['year'] <= 1786)
    key = {}
    for k in KEYS:
        L = sorted((r['quarter'], r['year'], a.mainname(r['nation'])) for r in ev if r['row'] == k and a.mainname(r['nation']))
        key[k] = (L[0][1], L[0][2], len({n for _, _, n in L})) if L else None
    nonuk_steam = sum(1 for r in ev if r['row'] == '蒸気機関' and a.mainname(r['nation']) not in (None, 'イギリス'))
    return dict(pace=len(rows_main), cn=cn, tot=tot, strict=strict, share=share, tie=sorted(sizes)[len(sizes) // 2] if sizes else 0, d2=d2, key=key, nonuk_steam=nonuk_steam,
                bad=len(a.prereq_violations(d, [s])))


def main():
    year = 1830
    for arg in sys.argv[1:]:
        s = int(arg.split('_s')[-1].split('/')[0]) if '_s' in arg else 2
        r = read(arg, s, year)
        print(f'{arg.split("probe/")[-1]} seed{s}: pace(≤{year} 主要国の行数)={r["pace"]}  清が最初の行={r["cn"]}/{r["tot"]}（単独で最初 {r["strict"]}・分け合い {r["share"]:.1f}・最初の四半期に開いた国数の中央 {r["tie"]}）  d≥2(≤1786)={r["d2"]}  蒸気機関を開いた英以外の主要国={r["nonuk_steam"]}  前提違反={r["bad"]}')
        print('   ' + '  '.join(f'{k}={v}' for k, v in r['key'].items()))
    for s in (2, 3):
        r = read(BASE, s, year)
        print(f'基線 m560 seed{s}: pace={r["pace"]}  清が最初の行={r["cn"]}/{r["tot"]}（単独で最初 {r["strict"]}・分け合い {r["share"]:.1f}・最初の四半期に開いた国数の中央 {r["tie"]}）  d≥2(≤1786)={r["d2"]}  英以外の蒸気機関={r["nonuk_steam"]}')
        print('   ' + '  '.join(f'{k}={v}' for k, v in r['key'].items()))


if __name__ == '__main__':
    main()
