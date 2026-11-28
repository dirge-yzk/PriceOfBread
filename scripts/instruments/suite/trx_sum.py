#!/usr/bin/env python3
# (1426) c153tp：フルスイートを 2 本に分けた時の trx の和を出し、--list-tests の数と突き合わせる。
#   python scripts/instruments/suite/trx_sum.py --trx <H.trx> <R.trx> [--ui <ui.trx>] --list-total <Category!=FullLength の Tests＋Ui の --list-tests の数> [--list-names <名の行の写し>] [--out <表>]
# 出す物：trx ごとの合格・失敗・スキップ・合計、和、赤の名、スキップの名、H と R の重複・和の欠け。
# 終了コード：0＝重複 0・欠け 0・数が一致（失敗の数は別の行に出す＝失敗があれば 3）・1＝重複か欠けか数の不一致・2＝読めない。
import sys, argparse, collections
import xml.etree.ElementTree as ET
NS = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'

def read(p):
    res = []
    for r in ET.parse(p).getroot().iter(NS + 'UnitTestResult'):
        res.append((r.get('testName'), r.get('outcome')))
    return res

ap = argparse.ArgumentParser()
ap.add_argument('--trx', nargs='+', required=True)
ap.add_argument('--ui', nargs='*', default=[])
ap.add_argument('--list-total', type=int, required=True)
ap.add_argument('--list-names', default='')
ap.add_argument('--out', default='')
a = ap.parse_args()
out = []
def say(s): out.append(s); print(s)
try:
    groups = [('trx', p) for p in a.trx] + [('ui', p) for p in a.ui]
    allres = []; seen = collections.Counter(); failed = []; skipped = []
    for kind, p in groups:
        r = read(p)
        c = collections.Counter(o for _, o in r)
        say(f'trx {p.replace(chr(92), "/").split("/")[-1]}：合計 {len(r)}・合格 {c.get("Passed", 0)}・失敗 {c.get("Failed", 0)}・スキップ {c.get("NotExecuted", 0)}')
        for n, o in r:
            key = (kind == 'ui', n)
            seen[key] += 1
            if o == 'Failed': failed.append(n)
            if o == 'NotExecuted': skipped.append(n)
        allres += r
except Exception as e:
    print('読めない', e); sys.exit(2)
dup = sorted(k[1] for k, v in seen.items() if v > 1)
tot = len(allres)
say(f'和：合計 {tot}・合格 {sum(1 for _, o in allres if o == "Passed")}・失敗 {len(failed)}・スキップ {len(skipped)}')
say(f'重複（同じ名が 2 度以上）{len(dup)}' + (' ' + ';'.join(dup[:5]) if dup else ''))
say('赤の名 ' + (';'.join(sorted(failed)) if failed else 'なし'))
say('スキップの名 ' + (';'.join(sorted(skipped)) if skipped else 'なし'))
rc = 0
if tot != a.list_total or dup:
    rc = 1
say(f'--list-tests の数 {a.list_total}・欠け（list−和）{a.list_total - tot + len(dup)}・{"一致" if tot == a.list_total and not dup else "不一致"}')
if a.list_names:
    L = set(l.strip() for l in open(a.list_names, encoding='utf-8', errors='replace') if l.startswith('    '))
    T = set(n for _, n in allres)
    mis_l = len(L - T); mis_t = len(T - L)
    say(f'名の突き合わせ：list にあって trx に無い {mis_l}・trx にあって list に無い {mis_t}')
    if mis_l or mis_t: rc = 1
if rc == 0 and failed: rc = 3
say(f'trx_sum rc={rc}')
if a.out: open(a.out, 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
sys.exit(rc)
