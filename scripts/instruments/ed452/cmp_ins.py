#!/usr/bin/env python
# (452) 計器つき 1 種の生の表を基線（m451_ins）の同じ種と比べる。恒等の証明ではなく事故の検出。
# 使い方：python cmp_ins.py <新しい ins フォルダ> <基線 ins フォルダ> <種=1>
#   同名の表をバイトで比べる。違った表は行ごとに列を比べ、違う列の名前と件数を出す。
#   flow_s は sourceLine（ソースの行番号＝EconomyStep の行がずれると変わる）を除いて比べる。burscan_s は壁時計なので別扱い。
import sys, os, hashlib, csv

new, old, seed = sys.argv[1].rstrip('/'), sys.argv[2].rstrip('/'), sys.argv[3] if len(sys.argv) > 3 else '1'
names = sorted(f for f in os.listdir(new) if f.endswith(f'_s{seed}.tsv'))
same, diff, onlynew = [], [], []
for f in names:
    po = os.path.join(old, f)
    if not os.path.exists(po):
        onlynew.append(f); continue
    a, b = open(os.path.join(new, f), 'rb').read(), open(po, 'rb').read()
    if a == b:
        same.append(f); continue
    la, lb = a.decode('utf-8').splitlines(), b.decode('utf-8').splitlines()
    cols = la[0].split('\t')
    bad = {}
    n = min(len(la), len(lb))
    for i in range(n):
        x, y = la[i].split('\t'), lb[i].split('\t')
        for j in range(min(len(x), len(y))):
            if x[j] != y[j]:
                bad[cols[j] if j < len(cols) else str(j)] = bad.get(cols[j] if j < len(cols) else str(j), 0) + 1
    diff.append((f, len(la), len(lb), bad))
print(f'一致 {len(same)} 表／違い {len(diff)} 表／新しい表 {len(onlynew)}（{onlynew}）')
for f, na, nb, bad in diff:
    print(f'  違い {f}: 行数 新 {na} 旧 {nb}・違った列={bad}')
oldonly = sorted(f for f in os.listdir(old) if f.endswith(f'_s{seed}.tsv') and not os.path.exists(os.path.join(new, f)))
print(f'旧にだけある表 {oldonly}')
