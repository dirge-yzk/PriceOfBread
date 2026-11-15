# (1164) 合流（m1157/trim1157.py の写し・木と作業フォルダ・名を m1164 へ・下の行の m1164 は写しの置き換え＝元は m1157）
# (1157) 合流（m1150/trim1150.py の写し・中身は同じ）
# (1150) 合流（m1143/trim1143.py の写し・中身は同じ）
# (1143) 合流：線の腕の生を、表ごとの sha1 を取ってから門と止める線の表だけへ縮める（(1129) rej1129.py の型・移さない＝その場で縮める）。
#   python -X utf8 trim1143.py <sha1 の出力 tsv> <残す表の頭（カンマ区切り）> <ins のフォルダ> [<ins のフォルダ> ...]
#   表の名＝<表>_s<種>.tsv。形の合わない名は残す（安全側）。tsv の列＝dir・file・bytes・sha1・kind（keep／del）。
import hashlib, os, re, sys

out, keep_s = sys.argv[1], sys.argv[2]
KEEP = set(k for k in keep_s.split(',') if k)
rows = []; freed = 0
for ins in sys.argv[3:]:
    for f in sorted(os.listdir(ins)):
        p = os.path.join(ins, f)
        if not os.path.isfile(p): continue
        h = hashlib.sha1()
        with open(p, 'rb') as r:
            for b in iter(lambda: r.read(1 << 22), b''):
                h.update(b)
        m = re.match(r'^(.+_s)(\d+)\.tsv$', f)
        keep = (m is None) or (m.group(1) in KEEP)
        size = os.path.getsize(p)
        rows.append((ins, f, size, h.hexdigest(), 'keep' if keep else 'del'))
        if not keep:
            os.remove(p); freed += size
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('dir\tfile\tbytes\tsha1\tkind\n')
    for r in rows: w.write('\t'.join(str(x) for x in r) + '\n')
print('files', len(rows), 'del', sum(1 for r in rows if r[4] == 'del'), 'freedGB', round(freed / 1e9, 2))
