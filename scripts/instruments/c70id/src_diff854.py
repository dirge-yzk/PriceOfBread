#!/usr/bin/env python
# (846) c69id 紙の台本：基準のコミットとの SimCore の差が注の文字だけであることを数える。
#   各 .cs で、行末の // 以降を落とし（注だけの行は行ごと落とす）、空白を詰めて並べた列が基準と同じかを比べる。
#   python src_diff846.py <基準のコミット> [木のパス=.]
import subprocess, sys, re

base = sys.argv[1]
tree = sys.argv[2] if len(sys.argv) > 2 else '.'

def git(*a):
    return subprocess.run(['git', '-C', tree, *a], capture_output=True, check=True).stdout

def code_of(b):
    out = []
    for ln in b.decode('utf-8').replace('\r\n', '\n').split('\n'):
        i = ln.find('//')
        code = (ln if i < 0 else ln[:i]).strip()
        if code:
            out.append(code)
    return out

files = git('diff', '--name-only', base, '--', 'PriceOfBread.SimCore').decode('utf-8').split()
ncomment = 0
bad = []
for f in files:
    old = git('show', f'{base}:{f}')
    new = open(tree.rstrip('/\\') + '/' + f, 'rb').read()
    if code_of(old) != code_of(new):
        bad.append(f)
    else:
        ncomment += 1
print(f'SimCore の差のあるファイル {len(files)} 本・注の文字だけの差 {ncomment} 本・コードの差 {len(bad)} 本')
for f in files:
    print('  ', f, 'CODE-DIFF' if f in bad else 'comment-only')
sys.exit(1 if bad else 0)
