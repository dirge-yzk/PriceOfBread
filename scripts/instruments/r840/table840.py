# (840) c68hl 腕ごとの表（読むだけ）：holclass840 の区分・an_h2 の 1/4 未満の件数・件数の規則（記録＝腕の件数 ≦ 新旧のヌル腕 6 本の件数の最大）
#   python -X utf8 -I table840.py <all840 の出力フォルダ> <an の出力フォルダ>
import sys, re, os
sys.stdout.reconfigure(newline='\n')   # Windows の文字モードの CRLF を避ける（出力は LF）
O, A = sys.argv[1], sys.argv[2]
ORDER = ['abDW0', 'c67svB', 'mM', 'mS', 'c65mdB', 'c66dwB', 'mS832', 'b839', 'a839', 'n839a', 'n839b', 'n826a', 'n826b', 'n797a', 'n797b', 'a826', 'a797']
NAME = {'abDW0': 'abDW0（c65md だけ旗 1・m826）', 'c67svB': 'c67svB（c67sv の腕 B・m826）', 'mM': 'mM（(839) 合わせた木 M）', 'mS': 'mS（(839) 合わせた木 S11）',
        'c65mdB': 'c65mdB（c65md の腕 B・m797・基線なし）', 'c66dwB': 'c66dwB（c66dw の腕 B・m826）', 'mS832': 'mS832（(832) 合流 S11）', 'b839': 'M′（m839）', 'a839': 'S11′（m839）',
        'n839a': '新 N1（m839）', 'n839b': '新 N2（m839）', 'n826a': '旧 N1（m826）', 'n826b': '旧 N2（m826）', 'n797a': '旧 N1（m797）', 'n797b': '旧 N2（m797）',
        'a826': 'S11（m826）', 'a797': 'S11（m797）'}
NULL6 = ['n839a', 'n839b', 'n826a', 'n826b', 'n797a', 'n797b']
def cls(a):
    t = open(os.path.join(O, f'new_{a}.txt'), encoding='utf-8').read()
    m = re.search(r'例外 (\d+)・元からの弱さ (\d+)・ヌル腕の幅の内 (\d+)・線が原因（新） (\d+)・食い違い (\d+)', t)
    return tuple(int(x) for x in m.groups())
def q4(a):
    m = re.search(r'ピークの1/4 未満: (\d+) 件', open(os.path.join(A, f'{a}.txt'), encoding='utf-8').read())
    return int(m.group(1))
C = {a: cls(a) for a in ORDER}
nullmax = max(C[a][1] + C[a][2] + C[a][3] for a in NULL6)
print(f'# (840) 腕ごとの区分（holclass840・同じ種・同じ州の最小）と件数の規則（記録・新旧のヌル腕 6 本の名簿の外の 1/10 の件数の最大＝{nullmax}）')
print('腕\t1/10 の行（例外を含む）\t例外\t元からの弱さ\tヌル腕の幅の内 (i)\t線が原因（新） (ii)\t食い違い\t1/4 未満（記録）\t件数の規則（名簿の外の件数 ≦ ' + str(nullmax) + '）')
for a in ORDER:
    ex, wk, i, ii, bad = C[a]; n = ex + wk + i + ii; out = wk + i + ii
    print(f'{NAME[a]}\t{n}\t{ex}\t{wk}\t{i}\t{ii}\t{bad}\t{q4(a)}\t{out}（{"内" if out <= nullmax else "外"}）')
