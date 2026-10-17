#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""(395) 線 ic2 の計器の読み手（読むだけ）。

使い方：PYTHONUTF8=1 python -I scripts/instruments/ic2/an_ic2.py <計器の出力フォルダ（secession_s*.tsv・factory_s*.tsv・scalars_s*.tsv がある所）> [出力ファイル]

読む表（定義と限界の申告は SimCore/InstrumentsSecession.cs・InstrumentsCapacity.cs・InstrumentsScalars.cs の頭）：
  ①secession_s ＝ 州ごとの離脱圧力・保持力・支配度の内訳。節 A＝計器の自己申告（組み直した圧力と本体の差の最大・中央の保持力の組み直しの差の最大）。
     節 B＝挙げた国ごとに〈分離の戦争へ入った州〉（goFlag=1 の最初の四半期）と、その前の 8 四半期の平均（圧力・保持力・余裕・支配度・国民意識・大衆の飽和）。
     節 C＝圧力の因数の分解（関税の項・民族の項・国民意識の割引・参政権の割引・大衆の飽和）の中央値（州×四半期）。
  ②factory_s の末尾 27 列 ＝ 労働の 3 財ごとの供給・需要・配給率・律速（時代別・挙げた国の作りたい量の重みの割合）。
  ③scalars_s の末尾 4 列 ＝ 前提つきの法（累進課税・失業保険以上・独占禁止・国有化）が最初に入った年（国別・種の中央値）。
⚠限界：挙げた国（POB_INSTRUMENTS_NATIONS）は毎四半期、他の国は年末の行だけ。節 B の前の 8 四半期は毎四半期の行がある国だけで平均する。
"""
import csv, glob, sys, statistics, collections, os


def read(path):
    with open(path, encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t')
        h = next(r)
        return h, list(r)


def num(s):
    try:
        return float(s)
    except ValueError:
        return float('nan')


def main(folder, out):
    P = lambda *a: print(*a, file=out)
    # ---------------- ① secession_s
    files = sorted(glob.glob(os.path.join(folder, 'secession_s*.tsv')), key=lambda f: int(''.join(c for c in os.path.basename(f) if c.isdigit()) or 0))
    P('=== A. 計器の自己申告（secession_s 全行・%d 試合）===' % len(files))
    maxp = maxh = 0.0
    rows_total = 0
    factor = collections.defaultdict(list)
    first_go = collections.defaultdict(list)   # (nation, region) -> [(seed, quarter, snapshot dict)]
    for f in files:
        seed = int(''.join(c for c in os.path.basename(f) if c.isdigit()))
        h, rows = read(f)
        ix = {n: i for i, n in enumerate(h)}
        hist = collections.defaultdict(list)
        for r in rows:
            rows_total += 1
            maxp = max(maxp, abs(num(r[ix['pressureRebuildGapIndex']])))
            maxh = max(maxh, abs(num(r[ix['holdBaseRebuildGapIndex']])))
            key = (r[ix['nation']], r[ix['region']])
            q = int(r[ix['quarter']])
            rec = {n: num(r[ix[n]]) for n in ('pressureIndex', 'holdShareIndex', 'marginIndex', 'dominanceShare', 'consciousnessMeanIndex', 'crowdFactorShare',
                                               'militancyMeanIndex', 'tariffGapIndex', 'legitimacyIndex', 'goFlag', 'popK')}
            rec['q'] = q
            hist[key].append(rec)
            if num(r[ix['pressureIndex']]) > 0:
                for n in ('tariffTermIndex', 'ethnicTermIndex', 'consciousnessFactorShare', 'suffrageFactorShare', 'crowdFactorShare'):
                    factor[n].append(num(r[ix[n]]))
        for key, lst in hist.items():
            lst.sort(key=lambda d: d['q'])
            for i, d in enumerate(lst):
                if d['goFlag'] == 1:
                    prev = [x for x in lst[max(0, i - 8):i] if d['q'] - x['q'] <= 8]
                    first_go[key].append((seed, d['q'], d, prev))
                    break
    P('行数 %d・pressureRebuildGapIndex の絶対値の最大 %.3g・holdBaseRebuildGapIndex の絶対値の最大 %.3g（0 に近いほど写しが本体に合う）' % (rows_total, maxp, maxh))
    P()
    P('=== B. 分離の戦争へ入った州（goFlag=1 の最初の四半期）と、その前の最大 8 四半期の平均 ===')
    P('国\t州\t種の数\t入った四半期の中央値\t前の平均：圧力\t保持力\t余裕\t支配度\t国民意識\t大衆の飽和\t闘争度\t関税の乖離\t正統性')
    for key in sorted(first_go, key=lambda k: (-len(first_go[k]), k)):
        lst = first_go[key]
        if len(lst) < 2:
            continue
        def avg(n):
            vals = [statistics.mean([x[n] for x in prev]) for (_, _, _, prev) in lst if prev]
            return statistics.mean(vals) if vals else float('nan')
        P('\t'.join([key[0], key[1], str(len(lst)), '%.0f' % statistics.median([q for (_, q, _, _) in lst]),
                     '%.3f' % avg('pressureIndex'), '%.3f' % avg('holdShareIndex'), '%.3f' % avg('marginIndex'), '%.3f' % avg('dominanceShare'),
                     '%.3f' % avg('consciousnessMeanIndex'), '%.3f' % avg('crowdFactorShare'), '%.3f' % avg('militancyMeanIndex'), '%.3f' % avg('tariffGapIndex'), '%.3f' % avg('legitimacyIndex')]))
    P()
    P('=== C. 圧力が正の行（州×四半期）の因数の中央値 ===')
    for n, v in factor.items():
        if v:
            P('%s\t中央値 %.4g\t平均 %.4g\t件数 %d' % (n, statistics.median(v), statistics.mean(v), len(v)))
    # ---------------- ② factory_s 労働の財ごと
    P()
    P('=== D. 労働の 3 財ごと（factory_s の末尾 27 列・時代別・全ての行の和の比）===')
    era = lambda y: (y - 1783) // 40
    acc = collections.defaultdict(lambda: collections.defaultdict(float))
    for f in sorted(glob.glob(os.path.join(folder, 'factory_s*.tsv'))):
        h, rows = read(f)
        ix = {n: i for i, n in enumerate(h)}
        if 'advLaborBoundDesiredRefValuePerMonth' not in ix:
            P('(列が無い＝古い計器の出力) ' + f)
            break
        for r in rows:
            e = era(int(r[ix['year']]))
            a = acc[e]
            a['desired'] += num(r[ix['desiredRefValuePerMonth']])
            a['loss'] += num(r[ix['laborBoundLossRefValuePerMonth']]) + num(r[ix['otherBoundLossRefValuePerMonth']]) + num(r[ix['unmatchedBoundLossRefValuePerMonth']])
            for p in ('plainLabor', 'advLabor', 'farmLabor'):
                a[p + 'BoundDesired'] += num(r[ix[p + 'BoundDesiredRefValuePerMonth']])
                a[p + 'BoundLoss'] += num(r[ix[p + 'BoundLossRefValuePerMonth']])
                a[p + 'Need'] += num(r[ix[p + 'NeedAtDesiredUnitsPerMonth']])
                a[p + 'Supply'] += num(r[ix[p + 'SupplyCapacityUnitsPerMonth']])
    P('時代\t作りたい量に占める律速（労働力／高度労働力／農業労働力）\t失った量に占める（同）\t需要÷器（同）')
    for e in sorted(acc):
        a = acc[e]
        d, l = a['desired'] or float('nan'), a['loss'] or float('nan')
        P('%d-\t%s\t%s\t%s' % (1783 + 40 * e,
                              ' / '.join('%.2f%%' % (100 * a[p + 'BoundDesired'] / d) for p in ('plainLabor', 'advLabor', 'farmLabor')),
                              ' / '.join('%.2f%%' % (100 * a[p + 'BoundLoss'] / l) for p in ('plainLabor', 'advLabor', 'farmLabor')),
                              ' / '.join('%.1f%%' % (100 * a[p + 'Need'] / a[p + 'Supply']) if a[p + 'Supply'] else 'nan' for p in ('plainLabor', 'advLabor', 'farmLabor'))))
    # ---------------- ③ scalars_s 法
    P()
    P('=== E. 前提つきの法が最初に入った年（国別・種の中央値・入らなかった種は数えない）===')
    first = collections.defaultdict(lambda: collections.defaultdict(list))
    for f in sorted(glob.glob(os.path.join(folder, 'scalars_s*.tsv'))):
        h, rows = read(f)
        ix = {n: i for i, n in enumerate(h)}
        if 'nationalizedMaskCode' not in ix:
            P('(列が無い＝古い計器の出力) ' + f)
            break
        seen = {}
        for r in rows:
            nat, y = r[ix['nation']], int(r[ix['year']])
            s = seen.setdefault(nat, {})
            tests = {'累進課税': num(r[ix['progressiveTaxShare']]) > 0, '失業保険以上': int(r[ix['socialSecurityLawCode']]) >= 2,
                     '独占禁止': int(r[ix['antitrustLawCode']]) >= 1, '国有化': int(r[ix['nationalizedMaskCode']]) != 0}
            for k, v in tests.items():
                if v and k not in s:
                    s[k] = y
        for nat, s in seen.items():
            for k, y in s.items():
                first[nat][k].append(y)
    P('国\t累進課税\t失業保険以上\t独占禁止\t国有化（年の中央値〔入った種の数〕）')
    for nat in sorted(first):
        if nat not in ('イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', '日本', '清', 'オスマン', 'アメリカ'):
            continue
        P('\t'.join([nat] + ['%.0f〔%d〕' % (statistics.median(first[nat][k]), len(first[nat][k])) if first[nat].get(k) else '-' for k in ('累進課税', '失業保険以上', '独占禁止', '国有化')]))


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    if len(sys.argv) > 2:
        with open(sys.argv[2], 'w', encoding='utf-8', newline='\n') as fh:
            main(sys.argv[1], fh)
    else:
        main(sys.argv[1], sys.stdout)
