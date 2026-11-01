# (728) 生を消す前の要約の自動保存：計器の生のフォルダの各ファイルの sha256・バイト数・行数・見出しを 1 本の TSV に残す。
#   python -I keepsum.py <生のフォルダ> <要約の出力ファイル>   （要約は measurements/ へ置いてコミットする物＝生は消してよい）
import sys, os, hashlib

sys.stdout.reconfigure(encoding="utf-8")
from concurrent.futures import ThreadPoolExecutor

def one(p):
    h = hashlib.sha256(); n = 0; rows = 0; head = ''
    with open(p, 'rb') as f:
        first = True
        for b in iter(lambda: f.read(1 << 22), b''):
            if first:
                head = b.split(b'\n', 1)[0].decode('utf-8', 'replace').rstrip('\r'); first = False
            h.update(b); n += len(b); rows += b.count(b'\n')
    return os.path.basename(p), h.hexdigest(), n, rows, head

def main():
    d, out = sys.argv[1], sys.argv[2]
    fs = sorted(os.path.join(d, x) for x in os.listdir(d) if os.path.isfile(os.path.join(d, x)))
    with ThreadPoolExecutor(8) as ex:
        rs = list(ex.map(one, fs))
    with open(out, 'wb') as o:
        o.write(f'# keepsum (728)\t{d}\tファイル {len(rs)} 本・合計 {sum(r[2] for r in rs)} バイト\n'.encode('utf-8'))
        o.write('name\tsha256\tbytes\tnewlines\theader\n'.encode('utf-8'))
        for r in rs:
            o.write(f'{r[0]}\t{r[1]}\t{r[2]}\t{r[3]}\t{r[4][:300]}\n'.encode('utf-8'))
    print(f'要約 {len(rs)} 本 → {out}')

main()
