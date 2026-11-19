# (1226) 合流の docs の書き込み（バイトで読み書き＝LF と BOM を保つ）。
#   python -X utf8 -I docs1226.py <リポジトリ> <下書きのフォルダ>
#   下書き：log_lines.md（(1220)〜(1225)）・log_1226.md（(1226)）・next_wave.txt（次の波の 0〜5 節）・misc_1226.txt（@@README・@@CURRENT・@@ADOPT・@@Q・@@ROW）
import sys
R, W = sys.argv[1], sys.argv[2]

def rd(p):
    return open(p, 'rb').read().decode('utf-8').replace('\r\n', '\n')

def sections(s):
    out = {}; cur = None
    for l in s.split('\n'):
        if l.startswith('@@'):
            cur = l[2:].strip(); out[cur] = []
        elif cur is not None:
            out[cur].append(l)
    return {k: '\n'.join(v).strip('\n') for k, v in out.items()}

mis = sections(rd(f'{W}/misc_1226.txt'))

# 実装ログ
p = f'{R}/docs/202609/99_実装ログ_20260903.md'
b = open(p, 'rb').read()
add = rd(f'{W}/log_lines.md').strip('\n') + '\n\n' + rd(f'{W}/log_1226.md').strip('\n') + '\n'
if not b.endswith(b'\n'): b += b'\n'
open(p, 'wb').write(b + b'\n' + add.encode('utf-8'))

# 00一覧
p = f'{R}/docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = open(p, 'rb').read().decode('utf-8').split('\n')
def find(prefix):
    ix = [i for i, l in enumerate(L) if l.startswith(prefix)]
    assert len(ix) == 1, (prefix, len(ix)); return ix[0]
# ★(1226) の行（★(1219) の行の次）
i = find('| **★(1219) 合流（波122')
L.insert(i + 1, mis['ROW'])
# 次の波
i = find('| **次の波（波123〜・(1219) の推奨')
old = L[i]; parts = old.split(' | ')
assert len(parts) == 3, len(parts)
struck = ('| ~~**次の波（波123〜・(1219) の推奨・AI・作者の承認は未確認）**~~〔2026-11-19 ★(1226) で波123 完了＝挙動 1/2（c123k3 を採った＝新しい基線 m1226・c123iv は線の結論〈採らない〉）・'
          '恒等 2/2（c123cu・c123tc）・読む 1（c123rb）・設計の材料 1（c123nf）〕 | 中身は ★(1226) の行と下の〈次の波（波124〜）〉へ運んだ | 実装ログ (1213)〜(1219) |')
nw = rd(f'{W}/next_wave.txt').strip('\n').split('\n')
head = ('| **次の波（波124〜・(1226) の推奨・AI・作者の承認は未確認）** | 基準は **m1226**（波123 で挙動の線 c123k3 を採った＝成績表 `b5_20261119_m1226`／`b10_20261119_m1226`／`b10_20261119_m1226_ins`／`mon_20261119_m1226.txt`・'
        '**ヌル腕は N1 の 1 本** `b10_20261119_m1226_n1_ins`・`b10s11_20261119_m1226_ins`・NG `ng1226_20261119`〔100 件〕・止める線の基準 `measurements/m1226_20261119/readM/`・`readS11/`）・'
        '**INSRAW `D:/PoB_work/keep_1226/base_ins`**（木 T の M の全表 1580 本）・ヌル腕と種 11〜20 の生 keep_1226/n1・a11・**TESTS_NONFULL 2922**（Tests 2688＋Tests.Ui 234）・FullLength 3・BASE_COMMIT 0b65b3dde（stage.py は段 1・計器か段 2 の条件に触れる線は段 2）。'
        '推定 237.8 秒（上限 248.5＝名目の余り約 10.7 秒）。D: は keep_1226（約 46GB）＋keep_1226_rej（iv の腕 B 約 28GB）＋古い keep_1212・keep_1219・keep_1219_rej（§0-2 ★(1226) ③）＝挙動の線の腕 B は 1 本 約 26〜28GB＝起こす前に床を数える。')
tail = ('**(1226) の足し**：記録＝英の国債の不履行 M 3/10・S11 2/10・N1 5/10（m1212 と同じ数）・ヌル腕の空洞化 1/10 未満 1 件（N1 種 3 セントマイケル）・NG M 100（新 7・外 2）・N1 94。'
        '効率化（決まり⑦）＝挙動の線を採る合流は、採る線の腕 B の生を keepsum で残してから消し、腕 Z・N1・S11 を同時・全表の M は腕 Z の刈り込みの後（README ★(1226)）・走査と計器の束（bundle）は腕と並べて 28 分＝フルスイートの trx で読めるので次から合流の束は scan だけにする案（約 25 分）。')
carry = ('**以下は前の行（波123〜）の本文をそのまま運んだ物**（閉じた物＝0 節 ①〔cu〕②〔tc の盤面の探索＝R3 は §0-2 ★(1226) ①〕③〔tc の項 2〕・1 節 ①〔k3＝採った〕②〔iv＝採らない・閉じた〕・2 節 ①②③〔rb〕・3 節 ①〔nf〕。上の 0〜5 が並べ方を上書きする・中身は削らない）：')
new = head + '<br>' + '<br>'.join(nw) + '<br>' + tail + '<br>' + carry + parts[1] + ' | 実装ログ (1220)〜(1226) |'
L[i] = struck
L.insert(i + 1, new)
# §0-2
i = find('| **★(1219) 作者に聞く物（波122 の合流')
L[i] = L[i].replace('| **★(1219) 作者に聞く物（波122 の合流・本当に迷う物だけ・推奨つき）**',
                    '| ~~**★(1219) 作者に聞く物（波122 の合流・本当に迷う物だけ・推奨つき）**~~〔2026-11-19 ③ は c123iv で回した〔採らない〕・① は運ぶ＝★(1226) へ〕', 1)
L.insert(i + 1, mis['Q'])
# 採用率の表
i = find('| **採用率の表（★(685)')
key = '採らなかった物の多くは 5 テーマの繰り返し'
assert L[i].count(key) == 1
L[i] = L[i].replace(key, mis['ADOPT'] + key, 1)
open(p, 'wb').write('\n'.join(L).encode('utf-8'))

# CURRENT
p = f'{R}/docs/CURRENT.md'
L = open(p, 'rb').read().decode('utf-8').split('\n')
i = find('| ★ **最終更新（main へ合流済み）**')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）**', '| 前の更新（(1219)）', 1)
L.insert(i, mis['CURRENT'])
open(p, 'wb').write('\n'.join(L).encode('utf-8'))

# README（★(1219) の行の次）
p = f'{R}/docs/README.md'
L = open(p, 'rb').read().decode('utf-8').split('\n')
i = find('- ★(1219)：')
L.insert(i + 1, mis['README'])
open(p, 'wb').write('\n'.join(L).encode('utf-8'))
print('docs を書いた')
