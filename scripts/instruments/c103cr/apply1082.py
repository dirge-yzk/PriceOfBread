# (1082) world.json に data1082 の値を書く（バイトで読み書き・州ごとに文化と宗教の欄をちょうど 1 回ずつ置き換える）。
#   使い方: python -X utf8 apply1082.py <world.json>
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from data1082 import DATA
p = sys.argv[1]
b = open(p, 'rb').read()
cr0 = b.count(b'\r')
for (nn, city, cu, re_, _) in DATA:
    key = f'"city": "{city}",'.encode('utf-8')
    assert b.count(key) == 1, (city, b.count(key))
    i = b.index(key)
    j = b.find(b'"city":', i + len(key)); j = len(b) if j < 0 else j
    blk = b[i:j]
    oc, orl = b'"culture": ""', b'"religion": ""'
    assert blk.count(oc) == 1 and blk.count(orl) == 1, (city, blk.count(oc), blk.count(orl))
    nb = blk.replace(oc, f'"culture": "{cu}"'.encode('utf-8'), 1).replace(orl, f'"religion": "{re_}"'.encode('utf-8'), 1)
    b = b[:i] + nb + b[j:]
assert b.count(b'\r') == cr0
open(p, 'wb').write(b)
print(f"書いた州 {len(DATA)}・CR {cr0}")
