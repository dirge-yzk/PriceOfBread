#!/usr/bin/env python
"""(983) landsale_s の門と no-go の読み手（読むだけ）。
使い方：
  python gates983.py gates <生のフォルダ>            ... 向きの門・等式の門・件数の下限（全ての 種×国）
  python gates983.py nogo <M のフォルダ> [<N1> <N2>]  ... 名簿の国ごとの saleExpectedSharePerYear の 10 種の中央と no-go の判定
  python gates983.py selftest                         ... no-go の規則が go も no-go も返すことの紙の確かめ（恒真でない・★(879)）
名簿（9 か国）＝欄のある 8 か国（フランス・日本・イギリス・ロシア・プロイセン・スペイン・デンマーク・ポーランド）＋定額の清（英は二重に数えない）。
"""
import sys, glob, os, csv, statistics

ROSTER = ["フランス", "日本", "イギリス", "ロシア", "プロイセン", "スペイン", "デンマーク", "ポーランド", "清"]
TOL = 1e-9


def rows(path):
    with open(path, encoding='utf-8', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def num(x):
    return float('nan') if x == 'nan' else float(x)


def files(d):
    return sorted(glob.glob(os.path.join(d, 'landsale_s*.tsv')))


def gates(d):
    fs = files(d)
    bad = []
    n_rows = 0
    per = {}
    for p in fs:
        seed = os.path.basename(p)
        for r in rows(p):
            n_rows += 1
            over, rel = num(r['paidCopyOverEngine']), num(r['paidCopyRp1RelDiff'])
            if not (over <= 1 + TOL):
                bad.append(('向き', seed, r['nation'], r['quarter'], over))
            if not (rel <= TOL):
                bad.append(('等式', seed, r['nation'], r['quarter'], rel))
            k = (seed, r['nation'])
            a = per.setdefault(k, [0.0, 0.0, 0.0])
            a[0] += num(r['scanMonths']); a[1] += num(r['popsScanned']); a[2] += num(r['rp1NationMonths'])
    for (seed, nat), a in per.items():
        if nat in ROSTER and not (a[0] > 0 and a[1] > 0):
            bad.append(('件数の下限', seed, nat, '-', a[:2]))
        if nat in ROSTER and not (a[2] > 0):
            bad.append(('等式の門が一度も働かない', seed, nat, '-', a[2]))
    print(f'表 {len(fs)} 本・行 {n_rows}・違反 {len(bad)}')
    for b in bad[:30]:
        print(b)
    return 0 if not bad and fs else 1


def per_seed_share(d):
    """名簿の国ごと：種ごとの全期の割合（Σ 割合×純の地代÷Σ 純の地代）の一覧。"""
    out = {}
    for p in files(d):
        acc = {}
        for r in rows(p):
            if r['nation'] not in ROSTER:
                continue
            net = num(r['saleNetRentPerQuarter']); sh = num(r['saleExpectedSharePerYear'])
            a = acc.setdefault(r['nation'], [0.0, 0.0])
            a[0] += sh * net; a[1] += net
        for nat, (s, n) in acc.items():
            out.setdefault(nat, []).append(s / n if n > 0 else 0.0)
    return out


def median_of(m):
    return {k: statistics.median(v) for k, v in m.items() if v}


def decide(med, n1=None, n2=None):
    """no-go：名簿の全ての国で 中央 ≤ 3×|N1 の中央−N2 の中央| か、全ての国で中央 ＜ 0.0005（0.05%／年）。ヌル腕が無ければ判定待ち。"""
    if n1 is None or n2 is None:
        return 'pending', {}
    diffs = {k: abs(n1.get(k, 0.0) - n2.get(k, 0.0)) for k in med}
    all_small = all(med[k] <= 3 * diffs[k] for k in med)
    all_tiny = all(med[k] < 0.0005 for k in med)
    return ('no-go' if (all_small or all_tiny) else 'go'), diffs


def nogo(argv):
    med = median_of(per_seed_share(argv[0]))
    n1 = median_of(per_seed_share(argv[1])) if len(argv) > 1 else None
    n2 = median_of(per_seed_share(argv[2])) if len(argv) > 2 else None
    verdict, diffs = decide(med, n1, n2)
    for k in ROSTER:
        if k in med:
            print(f'{k}\t中央={med[k]:.6g}\t差={diffs.get(k, float("nan")) if isinstance(diffs, dict) else float("nan"):.3g}')
    print('判定:', verdict)
    return 0


def selftest():
    go = {'a': 0.01, 'b': 0.0002}; z = {'a': 0.0, 'b': 0.0}
    v1, _ = decide(go, {'a': 0.0, 'b': 0.0}, {'a': 0.001, 'b': 0.0})     # a: 0.01 > 3*0.001 かつ ≥0.0005＝go
    v2, _ = decide(z, {'a': 0.0, 'b': 0.0}, {'a': 0.0, 'b': 0.0})        # 全て 0＝no-go
    v3, _ = decide({'a': 0.002, 'b': 0.001}, {'a': 0.0, 'b': 0.0}, {'a': 0.001, 'b': 0.001})   # 全て ≤3×差＝no-go
    v4, _ = decide({'a': 0.0003, 'b': 0.0001}, {'a': 0.0, 'b': 0.0}, {'a': 0.0, 'b': 0.0})     # 全て 0.05% 未満＝no-go
    print(v1, v2, v3, v4)
    return 0 if (v1, v2, v3, v4) == ('go', 'no-go', 'no-go', 'no-go') else 1


if __name__ == '__main__':
    cmd = sys.argv[1]
    sys.exit({'gates': lambda: gates(sys.argv[2]), 'nogo': lambda: nogo(sys.argv[2:]), 'selftest': selftest}[cmd]())
