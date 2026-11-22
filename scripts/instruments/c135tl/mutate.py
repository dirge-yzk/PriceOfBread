# (1302) 突然変異：ファイルの 1 か所を置き換える（バイトで読み書き）。使い方：mutate.py <file> <old> <new>
import sys
p, old, new = sys.argv[1:4]
b = open(p, 'rb').read().decode('utf-8')
assert b.count(old) == 1, f'一致が {b.count(old)} 件'
open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
