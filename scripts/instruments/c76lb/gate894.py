# (894) c76lb：R の照合の門（ident の生・計器つき 10 種）。
#   python -X utf8 gate894.py <ins のフォルダ> <出力.txt>      門＝(i) baseRCheckBreakFlag の和 0（計器が double で数えた 1e-12）・(iii) landbase_s と cornint_s の行の集合が同じ（種ごとの行の数と quarter・nation の並び）
#   python -X utf8 gate894.py --selftest                      人工の行（旗 1・行の数の食い違い・nation の食い違い）を落とし、正しい行を通すことを確かめる
#   (ii) は記録：baseRPerMonth と cornint_s の rentDomTotalPerMonth の G10 の文字列の一致の数と、食い違う行の相対差の最大（印字の床つき・門ではない）。
import csv, glob, os, re, sys


def rows(path):
    with open(path, encoding='utf-8', newline='') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))


def check(lb, cn):
    """1 つの種の行どうしの門。戻り値＝(破れの一覧, 記録の辞書)。"""
    bad = []
    flag = sum(int(r['baseRCheckBreakFlag']) for r in lb)
    if flag != 0:
        bad.append('baseRCheckBreakFlag の和 %d' % flag)
    if len(lb) != len(cn):
        bad.append('行の数 landbase %d・cornint %d' % (len(lb), len(cn)))
    same = diff = keybad = 0; relmax = 0.0
    for a, b in zip(lb, cn):
        if a['quarter'] != b['quarter'] or a['nation'] != b['nation']:
            keybad += 1; continue
        if a['baseRPerMonth'] == b['rentDomTotalPerMonth']:
            same += 1
        else:
            diff += 1
            x, y = float(a['baseRPerMonth']), float(b['rentDomTotalPerMonth'])
            relmax = max(relmax, abs(x - y) / max(abs(x), abs(y), 1e-300))
    if keybad:
        bad.append('quarter・nation の並びが違う行 %d' % keybad)
    return bad, {'rows': len(lb), 'flagSum': flag, 'g10Same': same, 'g10Diff': diff, 'g10DiffRelMax': relmax}


def selftest():
    good = [{'quarter': '0', 'nation': 'A', 'baseRPerMonth': '1.5', 'baseRCheckBreakFlag': '0'},
            {'quarter': '0', 'nation': 'B', 'baseRPerMonth': '2', 'baseRCheckBreakFlag': '0'}]
    corn = [{'quarter': '0', 'nation': 'A', 'rentDomTotalPerMonth': '1.5'}, {'quarter': '0', 'nation': 'B', 'rentDomTotalPerMonth': '2'}]
    ok = True
    b, _ = check(good, corn); ok &= (b == [])
    flag1 = [dict(good[0]), dict(good[1], baseRCheckBreakFlag='1')]
    b, _ = check(flag1, corn); ok &= (len(b) == 1)
    b, _ = check(good[:1], corn); ok &= (len(b) == 1)
    swap = [dict(good[0], nation='C'), good[1]]
    b, _ = check(swap, corn); ok &= (len(b) == 1)
    print('selftest', 'OK' if ok else 'NG')
    return 0 if ok else 1


def main():
    if sys.argv[1] == '--selftest':
        return selftest()
    ins, out = sys.argv[1], sys.argv[2]
    lines = []; allbad = []
    seeds = sorted(int(re.search(r'landbase_s(\d+)\.tsv$', p).group(1)) for p in glob.glob(os.path.join(ins, 'landbase_s*.tsv')))
    if not seeds:
        allbad.append('landbase_s が無い')
    for s in seeds:
        lb = rows(os.path.join(ins, 'landbase_s%d.tsv' % s)); cn = rows(os.path.join(ins, 'cornint_s%d.tsv' % s))
        bad, rec = check(lb, cn)
        allbad += ['種 %d：%s' % (s, x) for x in bad]
        lines.append('種 %d\t行 %d\t旗の和 %d\tG10 一致 %d\t食い違い %d\t食い違いの相対差の最大 %.3g' % (s, rec['rows'], rec['flagSum'], rec['g10Same'], rec['g10Diff'], rec['g10DiffRelMax']))
    lines.append('門 (i)(iii)：' + ('通る' if not allbad else '破れ ' + '・'.join(allbad)))
    with open(out, 'wb') as fh:
        fh.write(('\n'.join(lines) + '\n').encode('utf-8'))
    print('\n'.join(lines))
    return 0 if not allbad else 1


sys.exit(main())
