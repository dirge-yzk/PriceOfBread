# (648) 段 0：加硫ゴム（ゴムの財の解禁の行）が各種で最初に解禁された年と国。読むだけ。使い方：python -I s0_disc.py <計器の出力フォルダ>
import csv, sys
sys.stdout.reconfigure(encoding='utf-8')
root = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_645/m645_ins'
for s in range(1, 11):
    first = None
    with open(f'{root}/disc_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['row'] == '加硫ゴム' and r['kind'] == 'E':
                q = int(r['quarter'])
                if first is None or q < first[0]: first = (q, r['year'], r['nation'])
    print(s, first if first else '1950 年まで一度も解禁されない')
