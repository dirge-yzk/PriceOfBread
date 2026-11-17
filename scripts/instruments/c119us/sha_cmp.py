# (1195) c119us 腕の生と基線の生を表ごとに sha1 で突き合わせる（予言＝全ての表がバイト一致）。
#   python -X utf8 -I sha_cmp.py <腕の ins> <基線の ins>   ＝一致しない表の名と、wars_s の最初に分かれる行を出す
import sys, os, hashlib
a, b = sys.argv[1], sys.argv[2]
def h(p):
    with open(p, 'rb') as f: return hashlib.sha1(f.read()).hexdigest()
fa = sorted(f for f in os.listdir(a) if f.endswith('.tsv'))
fb = set(f for f in os.listdir(b) if f.endswith('.tsv'))
same = diff = only = 0; diffs = []
for f in fa:
    if f not in fb: only += 1; print('ONLY_ARM', f); continue
    if h(os.path.join(a, f)) == h(os.path.join(b, f)): same += 1
    else: diff += 1; diffs.append(f)
for f in sorted(fb - set(fa)): print('ONLY_BASE', f)
print(f'表 {len(fa)}・一致 {same}・不一致 {diff}・腕だけ {only}・基線だけ {len(fb - set(fa))}')
for f in diffs[:40]: print('DIFF', f)
for f in diffs:
    if f.startswith('wars_s'):
        la = open(os.path.join(a, f), encoding='utf-8').read().splitlines(); lb = open(os.path.join(b, f), encoding='utf-8').read().splitlines()
        for i, (x, y) in enumerate(zip(la, lb)):
            if x != y: print(f'{f} 最初に分かれる行 {i}: 腕 {x} ／ 基線 {y}'); break
print('SHA_ALL_SAME' if diff == 0 and only == 0 and not (fb - set(fa)) else 'SHA_DIFFER')
