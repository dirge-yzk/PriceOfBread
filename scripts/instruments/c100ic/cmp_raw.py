# (1056) 旗 0 の ident の生を INSRAW と表ごとにバイトで比べる（hash1044 と別の道＝filecmp の全バイト比較）。imp_s は先頭 16 列を行ごとに比べる。
# 使い方: python cmp_raw.py <腕の ins> <INSRAW> <一致した表の名簿の出力>
import sys, os, filecmp
A, B, out = sys.argv[1], sys.argv[2], sys.argv[3]
fa = sorted(os.listdir(A)); fb = set(os.listdir(B))
same = []; diff = []; onlya = [x for x in fa if x not in fb]; onlyb = sorted(fb - set(fa)); pref_ok = []
for f in fa:
    if f not in fb: continue
    if filecmp.cmp(os.path.join(A, f), os.path.join(B, f), shallow=False): same.append(f); continue
    diff.append(f)
    if f.startswith('imp_s'):
        la = open(os.path.join(A, f), 'rb').read().replace(b'\r\n', b'\n').split(b'\n')
        lb = open(os.path.join(B, f), 'rb').read().replace(b'\r\n', b'\n').split(b'\n')
        n = len(lb[0].split(b'\t'))
        if len(la) == len(lb) and all(x.split(b'\t')[:n] == y.split(b'\t')[:n] for x, y in zip(la, lb)): pref_ok.append(f)
open(out, 'w', encoding='utf-8').write('\n'.join(same) + '\n')
print(f'共通 {len(fa) - len(onlya)}・バイト一致 {len(same)}・違う {len(diff)}・腕だけ {len(onlya)}・基準だけ {len(onlyb)}')
print('違う表: ' + ' '.join(sorted(set(x.split("_s")[0] for x in diff))) + f'・imp の先頭の列が全行一致 {len(pref_ok)}/{sum(1 for x in diff if x.startswith("imp_s"))}')
