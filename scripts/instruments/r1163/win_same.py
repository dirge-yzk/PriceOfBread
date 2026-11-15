# 窓（四半期 0〜79）の中で、木 m1150 と m1157 の同じヌル腕の表が一致するか（行のバイト）。
import sys, hashlib
def h(p):
    m = hashlib.sha1(); n = 0
    with open(p, 'rb') as f:
        f.readline()
        for line in f:
            q = int(line.split(b'\t', 1)[0])
            if q < 80: m.update(line); n += 1
    return m.hexdigest()[:12], n
for arm in ('n1', 'n2'):
    for t in ('rponset_s', 'rule_s', 'cederule_s'):
        for s in range(1, 11):
            a = h(f'D:/PoB_work/keep_1150/{arm}/{t}{s}.tsv'); b = h(f'D:/PoB_work/keep_1157/{arm}/{t}{s}.tsv')
            if a != b: print(arm, t, s, a, b)
print('done')
