import csv, sys, collections
MAIN = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'アメリカ', '清', '日本']
def mn(n):
    for m in MAIN:
        if n == m or n.endswith(m): return m
d = sys.argv[1]; seeds = [int(x) for x in sys.argv[2].split(',')]
for s in seeds:
    E = [r for r in csv.DictReader(open(f'{d}/engineer_s{s}.tsv', encoding='utf-8'), delimiter='\t') if r['kind'] == 'E']
    first = collections.defaultdict(dict)
    for r in E:
        m = mn(r['nation'])
        if m and m not in first[r['row']]: first[r['row']][m] = int(r['year'])
    early = sum(1 for r in E if int(r['decade']) >= 1800 and int(r['year']) <= 1786)
    earlyM = sum(1 for r in E if int(r['decade']) >= 1800 and int(r['year']) <= 1786 and mn(r['nation']))
    print(f's{s} 1783-86 d>=2 全国×行={early} 主要国={earlyM}')
    for row in ['蒸気機関', '汽船', '機関車', '転炉鋼', '石炭火力', '電動機', '送電網', 'ガス灯', '木材パルプ製紙']:
        f = first.get(row, {})
        print(f'   {row}: ' + ' '.join(f'{m}{f[m]}' for m in MAIN if m in f))
