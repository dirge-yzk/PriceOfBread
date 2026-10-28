import csv,sys
d=sys.argv[1]; seeds=[int(x) for x in sys.argv[2].split(',')]; name=sys.argv[3] if len(sys.argv)>3 else 'アムステルダム'; step=int(sys.argv[4]) if len(sys.argv)>4 else 40
for s in seeds:
    f=open(f'{d}/region_s{s}.tsv',encoding='utf-8')
    r=csv.DictReader(f,delimiter='\t')
    print('seed',s)
    for row in r:
        if row['region']==name and int(row['quarter'])%step==0:
            p=float(row['populationK']);ru=float(row['ruralPopulationK'])
            print(row['year'],row['nation'],'pop%.0f rural%.0f urb%.2f food%.2f mOut%.2f mIn%.2f xOut%.2f opp%.2f hous%.2f land%.2f arable%.0f war%s'%(p,ru,1-ru/p if p>0 else 0,float(row['foodFillShare']),float(row['migrationOutKPerQuarter']),float(row['migrationInKPerQuarter']),float(row['crossBorderOutKPerQuarter']),float(row['opportunityIndex']),float(row['housingFactor']),float(row['landFactor']),float(row['arableLand']),row['warIntensity'][:4]))
