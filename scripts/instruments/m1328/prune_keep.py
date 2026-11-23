# (1328) 合流：採らない腕の生を〈門と止める線が読む表〉だけに刈る（合流が自分で作った出力のフォルダだけに使う）。
#   python -X utf8 -I prune_keep.py <生の ins フォルダ> <要約の出力（keepsum の形・刈る前の全ての表）>
#   残す表＝ident.cfg の SMALL の表＋lp の門（lawdir_s・monlaw_s）＋df の門と記録（mintcap_s・fiscal_s・arrears_s）＋landsale_s。
#   刈る前に keepsum.py で全ての表の sha256・バイト・行の数を書く（要約を書けなければ刈らない）。
import sys, os, subprocess
d, summ = sys.argv[1], sys.argv[2]
here = os.path.dirname(os.path.abspath(__file__))
rc = subprocess.call([sys.executable, '-I', os.path.join(here, '..', 'tools', 'keepsum.py'), d, summ])
if rc != 0: print('要約を書けない＝刈らない'); sys.exit(1)
keep = ('nation_s,stock_s,wars_s,casualty_s,region_s,migration_s,pxmonth_s,scalars_s,aib_s,canal_s,class_s,disc_s,engineer_s,'
        'epi_s,goods_s,ideology_s,idn_s,invade_s,labor_s,monai_s,mongood_s,monlaw_s,lawdir_s,mintcap_s,fiscal_s,arrears_s,landsale_s').split(',')
n = 0; b = 0
for f in sorted(os.listdir(d)):
    p = os.path.join(d, f)
    if not os.path.isfile(p): continue
    base = f.rsplit('.', 1)[0].rstrip('0123456789')
    if base in keep: continue
    b += os.path.getsize(p); os.remove(p); n += 1
print(f'刈った {n} 本・{b/1e9:.2f}GB・残した表 {len(os.listdir(d))} 本')
