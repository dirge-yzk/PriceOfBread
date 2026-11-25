# (1372) 検算の台本：基準の tsreg_s（xCap 1 列・import は LastImport）と腕の tsreg_s（xCapSeller・xCapBuyer の 2 列・import は PrevImport）。使い方：tsreg_check.py <基準の表> <腕の表>。0＝宣言どおり。
# 基準の列だけから導く（腕のエンジンは呼ばない）。条件：見出しが xCap → xCapSeller,xCapBuyer 以外同じ・行数と先頭 5 列（年・国・州・港・財）が同じ・
# xCap 以外の整数の列は全て同じ・xCapSeller+xCapBuyer＝基準の xCap・demand と shortage は同じ文字列・import は非負の数（基準は常に 0＝LastImport は清算の頭で 0 に戻る）。
import sys
def load(p):
    return [x.split('\t') for x in open(p, encoding='utf-8').read().split('\n') if x != '']
def main():
    b, a = load(sys.argv[1]), load(sys.argv[2])
    i = b[0].index('xCap')
    if a[0] != b[0][:i] + ['xCapSeller', 'xCapBuyer'] + b[0][i + 1:]:
        print('見出しが違う'); return 1
    if len(a) != len(b):
        print(f'行数 {len(b)-1} → {len(a)-1}'); return 1
    H = {n: k for k, n in enumerate(b[0])}
    for k in range(1, len(b)):
        r, n = b[k], a[k]
        if n[:i] != r[:i]:
            print(f'行 {k} 先頭の列が違う'); return 1
        if int(n[i]) + int(n[i + 1]) != int(r[i]) or int(n[i]) < 0 or int(n[i + 1]) < 0:
            print(f'行 {k} xCap の和が違う'); return 1
        rest_b, rest_a = r[i + 1:], n[i + 2:]
        for name, kb in H.items():
            if kb <= i or name == 'import': continue
            if rest_b[kb - i - 1] != rest_a[kb - i - 1]:
                print(f'行 {k} 列 {name} が違う'); return 1
        imp = float(n[H['import'] + 1])
        if not (imp >= 0):
            print(f'行 {k} import が負か非数'); return 1
    print(f'一致 {len(b)-1} 行'); return 0
sys.exit(main())
