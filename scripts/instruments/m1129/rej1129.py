# (1129) 合流：採らない c109rc の作業フォルダを keep_1129_rej へ移し、腕 B の生を門と止める線の表だけに縮める（★(852) の型）。
#   python -X utf8 rej1129.py <c109rc_work> <keep_1129_rej> <sha1 の出力 tsv>
#   1) 腕 B の生の全ての表の sha1 を取る 2) フォルダごと移す（同じドライブ＝名前の付け替え）3) 残す表の頭に当たらない腕 B の表を消す（消した名は tsv の kind 列）
import hashlib, os, re, shutil, sys

src, dst_root, out = sys.argv[1:4]
KEEP = ('rulepower_s', 'rulepoweralloc_s', 'rule_s', 'secession_s', 'nation_s', 'pxmonth_s', 'region_s',
        'migration_s', 'cessp_s', 'subst_s', 'fiscal_s', 'stock_s')
ins_rel = os.path.join('armB', 'out_c109rcB_10', 'ins')
ins = os.path.join(src, ins_rel)
rows = []
for f in sorted(os.listdir(ins)):
    p = os.path.join(ins, f)
    h = hashlib.sha1()
    with open(p, 'rb') as r:
        for b in iter(lambda: r.read(1 << 22), b''):
            h.update(b)
    m = re.match(r'^(.+_s)(\d+)\.tsv$', f)   # 表の名＝<表>_s<種>.tsv（形の合わない名は残す＝安全側）
    keep = (m is None) or (m.group(1) in KEEP)
    rows.append((f, os.path.getsize(p), h.hexdigest(), 'keep' if keep else 'del'))
os.makedirs(dst_root, exist_ok=True)
dst = os.path.join(dst_root, os.path.basename(src.rstrip('/\\')))
shutil.move(src, dst)
dins = os.path.join(dst, ins_rel)
freed = 0
for f, size, _, kind in rows:
    if kind == 'del':
        os.remove(os.path.join(dins, f))
        freed += size
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('# file\tbytes\tsha1\tkind（keep＝keep_1129_rej に残す・del＝消した）\n')
    for r in rows:
        w.write('\t'.join(map(str, r)) + '\n')
print('tables', len(rows), 'keep', sum(1 for r in rows if r[3] == 'keep'), 'freedGB', round(freed / 1e9, 2), 'dst', dst)
