# (1383) 合流：生きた文書（実装ログ・00一覧・README・CURRENT）を書き換える（バイトで読み書き・BOM と改行を保つ）。
#   python -X utf8 -I docs1383.py <木の根> <行の定義 doc_rows.py> <実装ログの節のファイル> <フルスイートの文> <KEEPNULL の文>
import sys, importlib.util
R, ROWS, LOGF, SUITE, KN = sys.argv[1:6]
spec = importlib.util.spec_from_file_location('rows', ROWS); m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
def E(s): return s.replace('__SUITE__', SUITE).replace('__KEEPNULL__', KN).encode('utf-8')
def lines(p):
    b = open(p, 'rb').read(); assert b'\r\n' not in b, p
    return b, b.split(b'\n')
# 実装ログ
p = R + '/docs/202609/99_実装ログ_20260903.md'
b, _ = lines(p); log = open(LOGF, 'rb').read()
if not b.endswith(b'\n'): b += b'\n'
open(p, 'wb').write(b + E(log.decode('utf-8')) + (b'' if log.endswith(b'\n') else b'\n'))
# 00一覧
p = R + '/docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
b, L = lines(p)
def find(prefix):
    ix = [i for i, x in enumerate(L) if x.startswith(prefix.encode('utf-8'))]; assert len(ix) == 1, (prefix, ix); return ix[0]
i_ask = find('| **★(1376) 作者に聞く物'); L.insert(i_ask + 1, E(m.ROW_ASK))
i_nx = find('| **次の波（波146〜・(1376) の推奨'); L[i_nx] = E(m.FOLD_146); L.insert(i_nx + 1, E(m.NEXT_147))
i_m = find('| **★(1376) 合流（波145'); L.insert(i_m + 1, E(m.ROW_MERGE))
i_ad = find('| **採用率の表（★(685)'); t = '（★(1376)） | 実装ログ (685)'.encode('utf-8'); assert L[i_ad].count(t) == 1
L[i_ad] = L[i_ad].replace(t, '（★(1376)）'.encode('utf-8') + E(m.ADOPT_ADD) + ' | 実装ログ (685)'.encode('utf-8'))
i_rf = find('| **〈(3) リファクタの候補〉'); t = '〕 | 実装ログ (764) |'.encode('utf-8'); assert L[i_rf].endswith(t)
L[i_rf] = L[i_rf][:-len(' | 実装ログ (764) |'.encode('utf-8'))] + E(m.REFACTOR_ADD) + ' | 実装ログ (764) |'.encode('utf-8')
open(p, 'wb').write(b'\n'.join(L))
# README
p = R + '/docs/README.md'
b, L = lines(p)
ix = [i for i, x in enumerate(L) if x.startswith('- ★(1376)'.encode('utf-8'))]; assert len(ix) == 3, ix
for k, s in enumerate(m.README_LINES): L.insert(ix[-1] + 1 + k, E(s))
open(p, 'wb').write(b'\n'.join(L))
# CURRENT
p = R + '/docs/CURRENT.md'
b, L = lines(p)
old = '| ★ **最終更新（main へ合流済み）** |'.encode('utf-8')
ix = [i for i, x in enumerate(L) if x.startswith(old)]; assert len(ix) == 1
L[ix[0]] = '| 前の更新（(1376)） |'.encode('utf-8') + L[ix[0]][len(old):]
L.insert(ix[0], E(m.CURRENT_LINE))
open(p, 'wb').write(b'\n'.join(L))
print('ok')
