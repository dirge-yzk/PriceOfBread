"""(183) 写しの木の編集（BOM と改行の形を保つ）。edit(root, rel, [(old, new), ...])＝old は LF で書く（CRLF のファイルは置き換える）・各 old は1回だけ現れること。"""
def edit(root, rel, pairs):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:120], s.count(old))
        s = s.replace(old, new)
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
