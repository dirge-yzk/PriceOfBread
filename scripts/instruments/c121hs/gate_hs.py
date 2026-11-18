# (1209) 門 G2：〈満足度・最小国〉か〈ハーネス不合格〉を含む行を除いた全ての行が基線の成績表と全行一致か（世界を変えていないこと）
# 使い方: python -X utf8 -I gate_hs.py <基線の成績表> <腕の成績表> <出力>
# 出力：除いた行（両側・行番号つき）・残りの行の不一致の数と最初の 20 件・判定の 1 行（G2 一致／G2 不一致）
import sys
base, arm, outp = sys.argv[1], sys.argv[2], sys.argv[3]
KEYS = ('満足度・最小国', 'ハーネス不合格')
def split(p):
    keep, drop = [], []
    for i, l in enumerate(open(p, 'rb').read().decode('utf-8').split('\n'), 1):
        (drop if any(k in l for k in KEYS) else keep).append((i, l.rstrip('\r')))
    return keep, drop
bk, bd = split(base); ak, ad = split(arm)
out = open(outp, 'w', encoding='utf-8', newline='\n')
out.write(f'基線 {base}\n腕 {arm}\n')
for nm, d in (('基線', bd), ('腕', ad)):
    out.write(f'除いた行（{nm}）: {len(d)}\n')
    for i, l in d: out.write(f'  :{i} {l}\n')
diff = []
for j in range(max(len(bk), len(ak))):
    b = bk[j] if j < len(bk) else (None, None); a = ak[j] if j < len(ak) else (None, None)
    if b[1] != a[1]: diff.append((b, a))
out.write(f'残りの行：基線 {len(bk)}・腕 {len(ak)}・不一致 {len(diff)}\n')
for b, a in diff[:20]: out.write(f'  基線:{b[0]} {b[1]}\n  腕  :{a[0]} {a[1]}\n')
out.write('判定: G2 一致\n' if not diff and len(bk) == len(ak) else '判定: G2 不一致\n')
out.close()
print(open(outp, encoding='utf-8').read().splitlines()[-1])
