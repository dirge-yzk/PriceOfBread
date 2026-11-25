# (1362) 合流：消す前の作業フォルダの再帰の要約（keepsum.py は直下だけ＝★(1348) の誤り〈直下だけを数えた〉の再発を防ぐ）。
#   python -I rsum.py <出力.tsv> <フォルダ> [<フォルダ> ...]   ＝各ファイルの相対パス・sha256・バイト数・行数（ins/ の下は別の keepsum があるので名前とバイト数だけ）
import sys, os, hashlib
out = sys.argv[1]; rows = []
for d in sys.argv[2:]:
    for r, _, fs in os.walk(d):
        for f in sorted(fs):
            p = os.path.join(r, f); rel = os.path.relpath(p, os.path.dirname(d)).replace(os.sep, '/')
            n = os.path.getsize(p)
            if '/ins/' in '/' + rel + '/' or rel.split('/')[-2:-1] == ['ins']:
                rows.append((rel, '-', n, '-')); continue
            h = hashlib.sha256(); k = 0
            with open(p, 'rb') as fh:
                for b in iter(lambda: fh.read(1 << 22), b''): h.update(b); k += b.count(b'\n')
            rows.append((rel, h.hexdigest(), n, k))
with open(out, 'wb') as o:
    o.write(f'# rsum (1362)\tファイル {len(rows)} 本・合計 {sum(r[2] for r in rows)} バイト\nrel\tsha256\tbytes\tnewlines\n'.encode('utf-8'))
    for r in rows: o.write(('\t'.join(map(str, r)) + '\n').encode('utf-8'))
print(len(rows))
