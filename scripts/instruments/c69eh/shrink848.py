# (848) c69eh 採らなかった腕の生を、門と止める線の読み手が開く表だけへ縮める（読むだけの要約を先に書く・keep_839_rej/abDW0_md_10 の型）。
#   python -I scripts/instruments/c69eh/shrink848.py <ins フォルダ> <要約の出力 keepsum_all_tables.tsv>
#   要約（keepsum.py と同じ列）を書けたら、残す表の頭に当たらない *.tsv を消す。
import sys, os, subprocess
ins, out = sys.argv[1], sys.argv[2]
KEEP = ('nation_s', 'region_s', 'migration_s', 'pxmonth_s', 'wars_s', 'stock_s', 'casualty_s', 'fiscal_s', 'cessp_s', 'canal_s', 'tnation_s', 'flow_s', 'cearn_s', 'cpop_s')
here = os.path.dirname(os.path.abspath(__file__))
r = subprocess.run([sys.executable, '-I', os.path.join(here, '..', 'tools', 'keepsum.py'), ins, out])
if r.returncode != 0 or not os.path.exists(out) or os.path.getsize(out) == 0: print('要約を書けない＝消さない'); sys.exit(1)
n = kept = 0
for f in sorted(os.listdir(ins)):
    p = os.path.join(ins, f)
    if not os.path.isfile(p): continue
    if any(f.startswith(k) and f[len(k):len(k) + 1].isdigit() for k in KEEP): kept += 1; continue
    os.remove(p); n += 1
print(f'消した {n} 本・残した {kept} 本')
