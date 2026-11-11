# (1025) c96rd world.json の州の resources の末尾に足す行を書く（バイトで読み書き・LF・BOM なしを保つ）
#   使い方：python addrows.py <world.json>   （同じ行が既にあれば足さない＝2 回目は何もしない）
import sys

ADD = [('バーミンガム', 2), ('マンチェスター', 6), ('グラスゴー', 20), ('ブレスラウ', 0), ('セントポール', 3),
       ('広州', 0), ('グラーツ', 6), ('ビルバオ', 0), ('セビーリャ', 2), ('セビーリャ', 3)]
p = sys.argv[1]
b = open(p, 'rb').read()
assert b'\r' not in b
lines = b.decode('utf-8').split('\n')
for city, g in ADD:
    key = '"city": "%s",' % city
    idx = [i for i, l in enumerate(lines) if l.strip() == key]
    assert len(idx) == 1, (city, idx)
    j = idx[0] + 1
    while not lines[j].lstrip().startswith('"resources":'): j += 1
    ind = lines[j][:len(lines[j]) - len(lines[j].lstrip())]
    if lines[j].strip() == '"resources": [],':
        lines[j:j + 1] = [ind + '"resources": [', ind + ' ' + str(g), ind + '],']
        continue
    assert lines[j].strip() == '"resources": [', lines[j]
    k = j + 1; vals = []
    while lines[k].strip() not in (']', '],'): vals.append(int(lines[k].strip().rstrip(','))); k += 1
    if g in vals: continue
    lines[k - 1] = lines[k - 1].rstrip(',') + ','
    lines.insert(k, ind + ' ' + str(g))
open(p, 'wb').write('\n'.join(lines).encode('utf-8'))
