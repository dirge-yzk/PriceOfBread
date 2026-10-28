import csv,sys,collections
d=sys.argv[1]; s=int(sys.argv[2]); nat=sys.argv[3]
rows=collections.defaultdict(dict)
f=open(f'{d}/region_s{s}.tsv',encoding='utf-8')
for row in csv.DictReader(f,delimiter='\t'):
    q=int(row['quarter'])
    if row['nation']==nat and q%40==0:
        rows[q][row['region']]=(row['year'],float(row['populationK']),float(row['ruralPopulationK']),float(row['opportunityIndex']),float(row['holdingFactor']),float(row['foodFillShare']),float(row['arableLand']),row['tier'],float(row['migrationOutKPerQuarter']),float(row['migrationInKPerQuarter']))
for q in sorted(rows):
    print('q',q)
    for rg,v in rows[q].items():
        print('  %s %s pop%.0f rur%.0f opp%.2f hold%.2f stay%.2f food%.2f arable%.0f %s out%.2f in%.2f'%(rg,v[0],v[1],v[2],v[3],v[4],v[3]*v[4],v[5],v[6],v[7],v[8],v[9]))
