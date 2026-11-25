# (1366) 検算の台本：基準の cwend_s（旧の札）から新しい札を出し直し、腕の cwend_s と一致を見る。使い方：cwend_check.py <基準の表> <腕の表>。0＝一致。
# 旧 endPath の順＝forced → collapse → decisive → weary → other／新＝forced → decisive → weary → other＋末尾に collapseFlag。
# 基準の列だけから導く（腕のエンジンは呼ばない）。weary の閾値 0.9＝WarPeaceExhaustion の既定。reunifyReservedFlag は旧と同じ値でなければ不一致。
import sys
WPE = 0.9
def load(p):
    L = open(p, encoding='utf-8').read().split('\n')
    L = [x for x in L if x != '']
    return [x.split('\t') for x in L]
def main():
    b, a = load(sys.argv[1]), load(sys.argv[2])
    if a[0] != b[0] + ['collapseFlag']:
        print('見出しが違う'); return 1
    H = {n: i for i, n in enumerate(b[0])}
    if len(a) != len(b):
        print(f'行数 {len(b)-1} → {len(a)-1}'); return 1
    for k in range(1, len(b)):
        r = b[k]; n = r[:]
        sc = float(r[H['scoreAtPeace']]); eP, eR = float(r[H['exhParent']]), float(r[H['exhRebel']])
        cP, cR = float(r[H['revCutParent']]), float(r[H['revCutRebel']])
        col = eP >= cP or eR >= cR
        if r[H['endPath']] == 'collapse' and not col or r[H['endPath']] in ('decisive', 'weary', 'other') and col:
            print(f'行 {k} 旧の札と collapse の式が食い違う'); return 1
        if r[H['endPath']] != 'forced':
            weary = not (eP <= WPE * cP) and not (eR <= WPE * cR)
            n[H['endPath']] = 'decisive' if abs(sc) >= 1.0 else 'weary' if weary else 'other'
        n.append('1' if col else '0')
        if n != a[k]:
            print(f'行 {k} 不一致 {n[H["endPath"]]} 対 {a[k][H["endPath"]]}'); return 1
    print(f'一致 {len(b)-1} 行'); return 0
sys.exit(main())
