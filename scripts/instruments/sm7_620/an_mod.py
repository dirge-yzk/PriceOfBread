# (573) 線 sm5：近代化投資の廃止（旗 ModernizationAbolished）の門 M1〜M3 の読み手（読むだけ）。事前登録 追記 3 ④。
# 使い方：python -X utf8 an_mod.py <腕 B_mod の計器の出力フォルダ> <基線の計器の出力フォルダ>
# M1＝州の建設の請求（fiscal_s の claimStateBuildPerMonth）が全ての行で 0（基線は正であること＝計器が生きている確かめ）
# M2＝私的な投資の限界収益−ハードル（modsig_s の grossMean−hurdleMean・1840 年・全国の中央値）が基線の 0.5 倍以上（基線が正のとき）
# M3＝教育の民間の需要÷必要量（ed_s の privateDemandPerMonth÷needPerMonth・1880 年・全国の中央値）が基線の 0.8 倍以上
import sys, glob, os, statistics as st


def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            yield dict(zip(h, v))


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def seeds(d, pat):
    out = {}
    for p in glob.glob(os.path.join(d, pat)):
        b = os.path.basename(p)
        s = int(''.join(c for c in b.split('_s')[-1] if c.isdigit()))
        out[s] = p
    return out


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


def m1(d):
    tot = 0.0; mx = 0.0; n = 0
    for s, p in sorted(seeds(d, 'fiscal_s*.tsv').items()):
        for r in rows(p):
            x = fl(r.get('claimStateBuildPerMonth'))
            if x == x:
                tot += x; mx = max(mx, abs(x)); n += 1
    return tot, mx, n


def by_nation(p, year, f):
    """国ごとに 1 値（その年の行の平均）にしてから返す。計器の 9 国は毎四半期・他は年末の行＝行の中央値だと 9 国に重みが乗る。"""
    acc = {}
    for r in rows(p):
        if r['year'] != year: continue
        x = f(r)
        if x == x: acc.setdefault(r['nation'], []).append(x)
    return {n: sum(v) / len(v) for n, v in acc.items()}


def m2(d, rowwise=False):
    per = []
    for s, p in sorted(seeds(d, 'modsig_s*.tsv').items()):
        if rowwise:
            v = [fl(r['grossMean']) - fl(r['hurdleMean']) for r in rows(p) if r['year'] == '1840']
        else:
            v = list(by_nation(p, '1840', lambda r: fl(r['grossMean']) - fl(r['hurdleMean'])).values())
        per.append(med(v))
    return per


def m3(d, rowwise=False):
    per = []
    for s, p in sorted(seeds(d, 'ed_s*.tsv').items()):
        if rowwise:
            v = [fl(r['privateDemandPerMonth']) / fl(r['needPerMonth']) for r in rows(p)
                 if r['year'] == '1880' and fl(r['needPerMonth']) > 0]
        else:
            v = list(by_nation(p, '1880', lambda r: fl(r['privateDemandPerMonth']) / fl(r['needPerMonth']) if fl(r['needPerMonth']) > 0 else float('nan')).values())
        per.append(med(v))
    return per


def nn(d):
    """1840 年の modsig_s に出る国の数（種 1）。61 を確かめる。"""
    p = sorted(seeds(d, 'modsig_s*.tsv').items())[0][1]
    return len(by_nation(p, '1840', lambda r: 1.0))


def main():
    a, b = sys.argv[1], sys.argv[2]
    ta, ma, na = m1(a); tb, mb, nb = m1(b)
    print(f'[M1] B_mod の請求の合計={ta:.6g}・最大={ma:.6g}・行={na}／基線の合計={tb:.6g}・最大={mb:.6g}・行={nb}  [{"PASS" if ma == 0 and mb > 0 else "FAIL"}]')
    pa, pb = m2(a), m2(b)
    ra = med(pa); rb = med(pb)
    ok = (ra >= 0.5 * rb) if rb > 0 else None
    print(f'[M2] 1840 年の限界収益−ハードル（全国の中央値・種の中央値）B_mod={ra:.4g}・基線={rb:.4g}・比={(ra / rb) if rb else float("nan"):.3g}  種ごと B_mod={[round(x, 3) for x in pa]} 基線={[round(x, 3) for x in pb]}  [{"PASS" if ok else ("FAIL" if ok is False else "NA")}]')
    print(f'[M2 行の中央値＝計器の 9 国に重みが乗る形（記録）] B_mod={med(m2(a, True)):.4g}・基線={med(m2(b, True)):.4g}・国数（1840・種 1）={nn(a)}')
    print(f'[M3 行の中央値＝同上（記録）] B_mod={med(m3(a, True)):.4g}・基線={med(m3(b, True)):.4g}')
    qa, qb = m3(a), m3(b)
    ea = med(qa); eb = med(qb)
    ok3 = (ea >= 0.8 * eb) if eb > 0 else None
    print(f'[M3] 1880 年の教育の民間の需要÷必要量（全国の中央値・種の中央値）B_mod={ea:.4g}・基線={eb:.4g}・比={(ea / eb) if eb else float("nan"):.3g}  種ごと B_mod={[round(x, 3) for x in qa]} 基線={[round(x, 3) for x in qb]}  [{"PASS" if ok3 else ("FAIL" if ok3 is False else "NA")}]')


main()
