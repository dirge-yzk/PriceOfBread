# (812) c13ib 編集の道具（sed -i の代わり）。バイトで読み書きする＝CR の数と BOM を保つ。
# どの関数も、探す文字列がちょうど 1 か所のときだけ書く。違えば NG を出して終了コード 1。
import sys


class Editor:
    def __init__(self, root):
        self.root = root

    def _read(self, rel):
        return open(self.root + '/' + rel, 'rb').read()

    def _write(self, rel, b):
        open(self.root + '/' + rel, 'wb').write(b)

    def _fail(self, rel, what, c):
        print('NG', rel, c, what[:80])
        sys.exit(1)

    def edit(self, rel, old, new):
        b = self._read(rel)
        o, n = old.encode('utf-8'), new.encode('utf-8')
        c = b.count(o)
        if c != 1:
            self._fail(rel, old, c)
        self._write(rel, b.replace(o, n))
        print('ok', rel)

    def _lines(self, rel):
        b = self._read(rel)
        nl = b'\r\n' if b'\r\n' in b else b'\n'
        return b.split(nl), nl

    def _find(self, lines, sub, rel, start=0):
        s = sub.encode('utf-8')
        hits = [i for i in range(start, len(lines)) if s in lines[i]]
        if len(hits) != 1:
            self._fail(rel, sub, len(hits))
        return hits[0]

    def remove_line(self, rel, startswith):
        """行の頭がこの文字列で始まる行がちょうど 1 本のとき、その行を消す。"""
        lines, nl = self._lines(rel)
        s = startswith.encode('utf-8')
        hits = [i for i, l in enumerate(lines) if l.startswith(s)]
        if len(hits) != 1:
            self._fail(rel, startswith, len(hits))
        del lines[hits[0]]
        self._write(rel, nl.join(lines))
        print('ok', rel, 'line removed')

    def replace_line(self, rel, startswith, new_line):
        lines, nl = self._lines(rel)
        s = startswith.encode('utf-8')
        hits = [i for i, l in enumerate(lines) if l.startswith(s)]
        if len(hits) != 1:
            self._fail(rel, startswith, len(hits))
        lines[hits[0]] = new_line.encode('utf-8')
        self._write(rel, nl.join(lines))
        print('ok', rel, 'line replaced')

    def remove_range(self, rel, start_sub, end_sub, replacement=None):
        """start_sub を含む唯一の行から、その後ろで end_sub を含む最初の行まで（両端を含む）を消す。replacement があれば 1 行で置く。"""
        lines, nl = self._lines(rel)
        a = self._find(lines, start_sub, rel)
        e = end_sub.encode('utf-8')
        z = next((i for i in range(a, len(lines)) if e in lines[i]), None)
        if z is None:
            self._fail(rel, end_sub, 0)
        new = [] if replacement is None else [replacement.encode('utf-8')]
        lines[a:z + 1] = new
        self._write(rel, nl.join(lines))
        print('ok', rel, 'range removed', z - a + 1)

    def remove_method(self, rel, name):
        """public void <name>( の行とその直前の [Fact]（あれば）から、4 字下げの閉じ括弧までと、続く空行 1 本を消す。"""
        lines, nl = self._lines(rel)
        a = self._find(lines, 'public void ' + name + '(', rel)
        start = a
        while start > 0 and (lines[start - 1].strip().startswith(b'[') or lines[start - 1].strip().startswith(b'///')):
            start -= 1
        z = next((i for i in range(a, len(lines)) if lines[i].rstrip(b'\r') == b'    }'), None)
        if z is None:
            self._fail(rel, name, 0)
        end = z + 1
        if end < len(lines) and lines[end].strip() == b'':
            end += 1
        del lines[start:end]
        self._write(rel, nl.join(lines))
        print('ok', rel, 'method removed', name, end - start)
