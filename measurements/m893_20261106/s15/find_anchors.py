# (893) c76lg 読むだけ：木 <commit> のファイルで、文字列の行番号を取る（錨の取り直し）。
# 使い方: python -X utf8 find_anchors.py <repo> <commit> <spec.txt>   spec の 1 行＝<ファイル名>|<文字列>
import sys, subprocess
repo, commit, spec = sys.argv[1:4]
cache = {}
def lines(fn):
    if fn not in cache:
        p = fn if '/' in fn else ('PriceOfBread.SimCore/' + fn)
        cache[fn] = subprocess.run(['git', '-C', repo, 'show', f'{commit}:{p}'], capture_output=True).stdout.decode('utf-8-sig', 'replace').split('\n')
    return cache[fn]
for l in open(spec, 'rb').read().decode('utf-8').split('\n'):
    l = l.rstrip('\r')
    if not l or l.startswith('#'): continue
    fn, pat = l.split('|', 1)
    hits = [i + 1 for i, s in enumerate(lines(fn)) if pat in s]
    print(f'{fn}|{pat}|{",".join(map(str, hits)) or "無い"}')
