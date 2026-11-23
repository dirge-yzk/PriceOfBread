# (1322) c138sm 門 G1 の実際の側（ak＝旗 SecessionTreasuryByMoney の分離の国庫）：sectreas_s の各行の actualT（実際に生まれた国庫）が、
#   treasledger_s の item＝_bornTreasury の記帳（同じ四半期・相対 1e-9）と 1 対 1 で突き合う。sectreas_s の列どうしの比べ（c137ak/st_gate.py の G1）と違い、
#   実際の側を別の表（国庫の台帳）で読む＝計器が自分の局所値を信じない（(1318) v・v2・scripts/instruments/m1321/rev/akv2_born.py の写し）。
#   rc 0＝全ての行が突き合った・行が 1 以上・表の欠け 0・余った記帳 0／rc 1＝突き合わない行か余りがある／rc 2＝読めない・行が 0。
#   余った記帳（sectreas_s に行が無い生まれの国庫）は内乱など別の道の生まれ＝記録として数を出すが rc は 1 にしない（--strict で 1 にする）。
#   使い方: python -X utf8 -I st_gate_born.py <生のフォルダ> [種の最初=1] [種の数=10] [--strict]
import sys, csv, os, collections
args = [a for a in sys.argv[1:] if not a.startswith('--')]
strict = '--strict' in sys.argv
if not args:
    print('使い方：st_gate_born.py <生のフォルダ> [種の最初] [種の数] [--strict]'); sys.exit(2)
raw = args[0]
sb = int(args[1]) if len(args) > 1 else 1
ns = int(args[2]) if len(args) > 2 else 10
tot = match = extra = missing = 0
unmatched = []
for s in range(sb, sb + ns):
    p1 = os.path.join(raw, f'sectreas_s{s}.tsv'); p2 = os.path.join(raw, f'treasledger_s{s}.tsv')
    if not (os.path.exists(p1) and os.path.exists(p2)):
        missing += 1; continue
    try:
        with open(p1, encoding='utf-8') as f: st = list(csv.DictReader(f, delimiter='\t'))
        born = collections.defaultdict(list)
        with open(p2, encoding='utf-8') as f:
            for row in csv.DictReader(f, delimiter='\t'):
                if row['item'] == '_bornTreasury': born[row['quarter']].append(float(row['value']))
        for r in st:
            tot += 1; a = float(r['actualT'])
            L = born.get(r['quarter'], [])
            hit = [x for x in L if abs(x - a) <= 1e-9 * max(1, abs(a))]
            if hit: match += 1; L.remove(hit[0])
            else: unmatched.append((s, r['quarter'], r.get('parent'), a, L[:]))
        extra += sum(len(v) for v in born.values())
    except Exception as e:
        print('読めない：', s, e); sys.exit(2)
print('rows', tot, 'matched_born', match, 'unmatched', len(unmatched), 'extra_born', extra, 'missing_tables', missing)
for u in unmatched[:10]: print('UNMATCHED', u)
if tot == 0 or missing > 0: print('G1born 読めない（行が 0 か表の欠け）'); sys.exit(2)
bad = len(unmatched) > 0 or (strict and extra > 0)
print('G1born', '赤' if bad else '緑')
sys.exit(1 if bad else 0)
