# (954) 合流：生きた文書（実装ログ・00一覧・CURRENT・README）へ波84 の記録を書く（バイトで読み書き・行を消さない・docs947.py の型）。
#   python -X utf8 docs954.py <リポジトリの根> <値の表 key=value の txt>
#   docs954_*.md の {KEY} を値の表で埋めてから差し込む（埋め残しがあれば止まる）。
import os, sys, re
R = sys.argv[1]; T = R + '/docs'
H = os.path.dirname(os.path.abspath(__file__))
V = dict(l.split('=', 1) for l in open(sys.argv[2], 'rb').read().decode('utf-8').split('\n') if '=' in l)
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def txt(name):
    s = rd(os.path.join(H, name)).rstrip('\n')
    for k, v in V.items(): s = s.replace('{' + k + '}', v)
    left = re.findall(r'\{[A-Z_]+\}', s); assert not left, (name, left)
    return s
p = T + '/202609/99_実装ログ_20260903.md'
s = rd(p)
if not s.endswith('\n'): s += '\n'
wr(p, s + '\n' + txt('docs954_log.md') + '\n')
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **★(947) 合流（波83')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = txt('docs954_rows.md').split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **次の波（波84〜')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs954_next.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **★(947) 作者に聞く物')]; assert len(i) == 1
L[i[0]:i[0]] = [txt('docs954_ask.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **採用率の表（★(685)')]; assert len(i) == 1
a = '（★(947)）。'; assert L[i[0]].count(a) == 1
L[i[0]] = L[i[0]].replace(a, '（★(947)） → ' + txt('docs954_rate.md') + '（★(954)）。')
wr(p, '\n'.join(L))
p = T + '/CURRENT.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-08（(947) 合流')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(947)） |', 1)
L[i[0]:i[0]] = [txt('docs954_current.md')]
wr(p, '\n'.join(L))
p = T + '/README.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('- ★(947)：')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs954_readme.md')]
i = [k for k, l in enumerate(L) if l.startswith('| ★ 5種の腕は**5種の基線**と比べる（今は `measurements/b5_20261107_m933`＝')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('（今は `measurements/b5_20261107_m933`＝', '（今は `measurements/b5_20261108_m947`＝2026-11-08 (954) の合流〔波84・挙動の線 0 本＝基線は据え置き・腕 Z〔ident m954z・合わせた木 3c3747bb・SOURCE-HASH 37e382503deb818b・恒等の線 c84ca・c84gz〕は素の 5 種と計器つき 10 種が全行一致・INSRAW `D:/PoB_work/keep_954/base_ins`〔keep_947/base_ins と同じ 1109 本はハードリンク〕・BASE_COMMIT 3c3747bb・TESTS_NONFULL 2425〕・元は 2026-11-08 (947) の合流〔測った木 `e8e2d4ae`・SOURCE-HASH `c20a9220e4a1e5a3`・c83ic〔(943) 旗 ImposeCrownFromLoser 既定 1〕で軌道が変わった・恒等の線 c83tf を入れた・腕 Z〔ident m947z〕は m933 と全行一致〕・10種の基線は `b10_20261108_m947`〔exact 46・near 86・計器つき `b10_20261108_m947_ins`・NG 93〔`rd:measurements/ng947_20261108:m947`〕・推定 210.2 秒＝名目の余り 約 38.3 秒・ヌル腕 `b10_20261108_m947_n1_ins`・`_n2_ins`〔生 `keep_947/n1`・`n2`〕・種 11〜20 `b10s11_20261108_m947_ins`〔生 `keep_947/a11`＝POB_CEARN=0〕〕。旧の基線 `measurements/b5_20261107_m933`＝', 1)
wr(p, '\n'.join(L))
print('ok')
