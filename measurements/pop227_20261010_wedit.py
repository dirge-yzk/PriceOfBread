# -*- coding: utf-8 -*-
"""world.json を行単位で書き換える（整形し直さない＝合流の衝突を小さく）。LF のみ・BOM なし・インデントは元のまま。
使い方：python wedit.py <world.json の入力> <出力> <腕の名前をカンマ区切り: C1,C2,C3,C45> [<変更表の TSV 出力>]
"""
import sys, re, json, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pop227_20261010_spec as spec

CLASSES = ['nobility', 'peasant', 'worker', 'bourgeois', 'professional', 'merchant', 'artisan']


def fmt(x):
    if abs(x - round(x)) < 1e-9 and abs(x) < 1e12:
        return str(int(round(x)))
    s = ('%.8f' % x).rstrip('0').rstrip('.')
    return s if s else '0'


class W:
    def __init__(self, text):
        assert '\r' not in text
        self.L = text.split('\n')
        self.log = []   # (腕, 国, 項目, 旧, 新, 下限, 上限, 確からしさ, 根拠)

    def text(self):
        return '\n'.join(self.L)

    # --- ブロックの探索 ---
    def _section(self, key):
        i = self.L.index(' "%s": [' % key)
        j = i + 1
        while self.L[j] != ' ]' and self.L[j] != ' ],':
            j += 1
        return i + 1, j

    def block(self, section, name):
        a, b = self._section(section)
        i = a
        while i < b:
            if self.L[i] == '  {':
                k = i
                while not self.L[k].startswith('  }'):
                    k += 1
                if self.L[i + 1] == '   "name": "%s",' % name:
                    return i, k
                i = k
            i += 1
        raise KeyError((section, name))

    def nation(self, name):
        return self.block('nations', name)

    def target(self, name):
        return self.block('abstractTargets', name)

    # --- 植民地の住民数 ---
    def colony_popK(self, rng, colony):
        s, e = rng
        for i in range(s, e):
            if self.L[i] == '     "name": "%s",' % colony:
                for k in range(i, min(e, i + 12)):
                    m = re.match(r'^(     "popK": )([0-9.]+)$', self.L[k])
                    if m:
                        return k, float(m.group(2))
        raise KeyError(colony)

    def set_colony(self, arm, owner, rng, colony, new, lo, hi, cert, src):
        k, old = self.colony_popK(rng, colony)
        if new is None:
            self.log.append((arm, owner, '植民地/' + colony, old, old, lo, hi, cert, '据え置き：' + src))
            return
        self.L[k] = '     "popK": ' + fmt(new)
        self.log.append((arm, owner, '植民地/' + colony, old, new, lo, hi, cert, src))

    # --- 国の総人口（階級の人数を一括で比例） ---
    def class_lines(self, rng):
        s, e = rng
        out = {}
        for i in range(s, e):
            m = re.match(r'^   "(%s)": ([0-9.]+),$' % '|'.join(CLASSES), self.L[i])
            if m:
                out[m.group(1)] = (i, float(m.group(2)))
        return out

    def total(self, rng):
        return sum(v for _, v in self.class_lines(rng).values())

    def scale_classes(self, rng, f):
        """階級の人数を f 倍。規模に比例する絶対額（国庫・国債）も同じ f 倍＝GDP に対する比を保つ（人口だけ縮めると国庫÷GDP が膨らむ＝StartWarmupTests が赤になる）。"""
        for k, (i, v) in self.class_lines(rng).items():
            nv = max(1, int(round(v * f))) if v > 0 else 0
            self.L[i] = '   "%s": %d,' % (k, nv)
        s, e = rng
        for i in range(s, e):
            m = re.match(r'^   "(treasury|govDebt)": ([0-9.]+),$', self.L[i])
            if m and float(m.group(2)) > 0:
                self.L[i] = '   "%s": %s,' % (m.group(1), fmt(max(1.0, round(float(m.group(2)) * f))))

    # --- 州（provinces） ---
    def provs(self, rng):
        s, e = rng
        i = s
        while self.L[i] != '   "provinces": [':
            i += 1
        out = []
        j = i + 1
        while self.L[j] not in ('   ],', '   ]'):
            assert self.L[j] == '    {', (j, self.L[j])
            k = j
            while not self.L[k].startswith('    }'):
                k += 1
            city = None; share_i = None
            for t in range(j, k):
                m = re.match(r'^     "city": "(.*)",$', self.L[t])
                if m: city = m.group(1)
                m = re.match(r'^     "share": ([0-9.]+),$', self.L[t])
                if m: share_i = t
            out.append((city, j, k, share_i))
            j = k + 1
        return i, j, out

    def shares(self, rng):
        _, _, ps = self.provs(rng)
        return {c: float(re.match(r'^     "share": ([0-9.]+),$', self.L[si]).group(1)) for c, j, k, si in ps}

    def set_shares(self, rng, newshares):
        _, _, ps = self.provs(rng)
        for c, j, k, si in ps:
            if c in newshares:
                self.L[si] = '     "share": %s,' % fmt(newshares[c])

    def cut_province(self, rng, city):
        """県の要素を取り出す（行のリスト）。後の要素の行番号がずれるので呼び手は rng を取り直す。"""
        a, b, ps = self.provs(rng)
        idx = [c for c, *_ in ps].index(city)
        c, j, k, si = ps[idx]
        el = self.L[j:k + 1]
        last = (idx == len(ps) - 1)
        del self.L[j:k + 1]
        if last:   # 直前の要素の閉じを ',' なしにする
            pc, pj, pk, psi = ps[idx - 1]
            assert self.L[pk] == '    },'
            self.L[pk] = '    }'
        el[-1] = '    },'
        return el

    def paste_province(self, rng, el):
        a, b, ps = self.provs(rng)
        c, j, k, si = ps[-1]
        assert self.L[k] == '    }', self.L[k]
        self.L[k] = '    },'
        el = list(el); el[-1] = '    }'
        self.L[k + 1:k + 1] = el


def apply(w, arms):
    """腕ごとの変更を適用して w.log に記帳。"""
    if 'C1' in arms:
        for owner, colony, new, lo, hi, cert, src in spec.C1:
            w.set_colony('C1', owner, w.nation(owner), colony, new, lo, hi, cert, src)
    if 'C45' in arms:
        for tgt, colony, new, lo, hi, cert, src in spec.C5:
            w.set_colony('C5', tgt, w.target(tgt), colony, new, lo, hi, cert, src)
        small_nations(w)
    if 'C2' in arms:
        for nm, newtot, src, cert in [
            ('マイソール', 6000, 'ハイダル・アリー没時（1782）の領域 約8万平方マイル・人口 約600万（WP Sultanate of Mysore・WP Maratha–Mysore wars の要約・二次）', 'S'),
            ('デリー', 1800, 'ムガルの残存領（デリー・アグラ周辺）。旧 381万は 2〜4倍の過大（棚卸し担当の判定）→180万【推・直接の出典なし】', 'R')]:
            r = w.nation(nm)
            old = w.total(r)
            w.scale_classes(r, newtot / old)
            w.log.append(('C2', nm, '総人口（階級の和）', old, w.total(w.nation(nm)), '', '', cert, src))
    if 'C3' in arms:
        shares_c3(w)


def small_nations(w):
    rows = [
        ('ラヘジ', 100, 50, 150, 'R', 'ラヘジ首長国（アデン後背）約5〜15万。旧 99.6万は 7〜20倍の過大（棚卸し担当の判定）【推】'),
        ('オマーン', 400, 275, 470, 'R', 'オマーン本国 約28〜47万。旧 165万は 3.5〜6倍の過大【推】'),
        ('阮氏', 800, 450, 900, 'R', '阮氏（ジャーディン・ザーディン南部）1780年代 約45〜90万。旧 362万は 4〜8倍の過大【推】'),
        ('カンボジア', 800, 600, 1000, 'R', 'カンボジア 1800 約60〜100万。旧 232万は 2〜3.5倍の過大【推】'),
    ]
    for nm, newtot, lo, hi, cert, src in rows:
        r = w.nation(nm)
        old = w.total(r)
        w.scale_classes(r, newtot / old)
        w.log.append(('C4', nm, '総人口（階級の和）', old, w.total(w.nation(nm)), lo, hi, cert, src))
    # 西山朝：ベトナム全体 600〜700万（北部＋中部 約550万＋南部 約80万）
    r = w.nation('西山朝')
    old = w.total(r)
    w.scale_classes(r, 5500 / old)
    w.log.append(('C4', '西山朝', '総人口（階級の和）', old, w.total(w.nation('西山朝')), 5000, 6000, 'R', 'ベトナム全体 600〜700万（棚卸し担当の判定・WP 1800 一覧の 9.06 万千は粗い推定として採らない）のうち 北部（トンキン）約400万＋中部約150万【推】'))
    r = w.nation('西山朝')
    old_sh = w.shares(r)
    w.set_shares(r, {'ハノイ': 0.73, 'フエ': 0.27})
    w.log.append(('C4', '西山朝', '州share ハノイ/フエ', '%s/%s' % (old_sh['ハノイ'], old_sh['フエ']), '0.73/0.27', '', '', 'R', 'トンキン:中部＝約400:150 万【推】'))


def renorm(w, rng, changed_pop, total):
    """changed_pop: 州→人数（千人）。他の州は旧の share の比を保って 残りを埋める。"""
    sh = w.shares(rng)
    ch = {c: p / total for c, p in changed_pop.items()}
    rest_old = sum(v for c, v in sh.items() if c not in ch)
    rest_new = 1.0 - sum(ch.values())
    out = {}
    for c, v in sh.items():
        out[c] = ch[c] if c in ch else v * rest_new / rest_old
    # 丸め後の和を1にする（最大の州で吸収）
    outr = {c: float(fmt(v)) for c, v in out.items()}
    diff = 1.0 - sum(outr.values())
    big = max((c for c in outr if c not in ch), key=lambda c: outr[c])
    outr[big] = float(fmt(outr[big] + diff))
    return sh, outr


def shares_c3(w):
    # プロイセン 5,601千人（1786）：州の人数を出典なしの【推】で置き直す（棚卸し担当の判定：ポンメルン3.2〜3.5倍・シュレージエン0.57倍）
    r = w.nation('プロイセン')
    sh = w.shares(r)
    new = {'ベルリン': 0.36, 'シュテッティン': 0.09, 'ケーニヒスベルク': 0.15, 'ブレスラウ': 0.30, 'ダンツィヒ': 0.10}
    assert abs(sum(new.values()) - 1) < 1e-9
    w.set_shares(r, new)
    for c in new:
        w.log.append(('C3', 'プロイセン', '州share/' + c, sh[c], new[c], '', '', 'R',
                      '1786 約560万の割り：ブランデンブルク・マクデブルク・西部領 約200万／ポンメルン約50万／東プロイセン約85万／シュレージエン約168万／西プロイセン約56万【推・直接の出典なし】'))
    # ロシア：バクーを外す（ペルシャへ）／トビリシ＝カルトリ・カヘティ 32万／アルハンゲリスク 約17万
    r = w.nation('ロシア')
    tot = w.total(r)
    el = w.cut_province(w.nation('ロシア'), 'バクー')
    r = w.nation('ロシア')
    sh0 = w.shares(r)
    sh, outr = renorm(w, r, {'トビリシ': 320, 'アルハンゲリスク': 170}, tot)
    # バクーの旧 share を除いた 旧の和を使うので、renorm は 現在の shares（バクー無し）に対して行う
    w.set_shares(r, outr)
    w.log.append(('C3', 'ロシア', '州/バクー', '0.02919418', '外す（ペルシャへ移す）', '', '', 'S',
                  'バクー・ハン国は 1806 まで露領でない（ペルシャの封臣）＝時代錯誤の領土（棚卸し担当の判定）。heldSince 1783 の欄ごとペルシャへ'))
    w.log.append(('C3', 'ロシア', '州share/トビリシ', sh['トビリシ'], outr['トビリシ'], 100, 400, 'S',
                  'カルトリ・カヘティ 320,000（WP 1800 一覧・Avakov）。1783-07 ゲオルギエフスク条約は保護国＝露領は1801。保護国の扱い（間接統治）はそのまま'))
    w.log.append(('C3', 'ロシア', '州share/アルハンゲリスク', sh['アルハンゲリスク'], outr['アルハンゲリスク'], 120, 250, 'S',
                  'アルハンゲリスク 1783 男子（市民を除く）80,546・全体は約170,000（Britannica 7th 1842 の要約）'))
    # ペルシャ：バクーを受ける
    r = w.nation('ペルシャ')
    old = w.total(r)
    w.scale_classes(r, (old + 100) / old)
    r = w.nation('ペルシャ')
    tot2 = w.total(r)
    sh_old = w.shares(r)
    w.paste_province(r, el)
    r = w.nation('ペルシャ')
    # バクーの share と他の州の比例
    sh_now = w.shares(r)
    shb = 100.0 / tot2
    rest = sum(v for c, v in sh_now.items() if c != 'バクー')
    newsh = {c: (shb if c == 'バクー' else v * (1 - shb) / rest) for c, v in sh_now.items()}
    outr = {c: float(fmt(v)) for c, v in newsh.items()}
    diff = 1.0 - sum(outr.values())
    big = max((c for c in outr if c != 'バクー'), key=lambda c: outr[c])
    outr[big] = float(fmt(outr[big] + diff))
    w.set_shares(r, outr)
    w.log.append(('C3', 'ペルシャ', '州/バクー（ロシアから移す）', '', 'share %s・総人口 %d→%d' % (fmt(shb), old, tot2), 50, 200, 'R',
                  'バクー・ハン国 約5〜20万【推】・ペルシャの封臣（Zand〜Qajar）'))
    # オスマン：エルサレム 30万・バスラ 35万・マサワ 2万（エチオピアから）
    el2 = w.cut_province(w.nation('エチオピア'), 'マサワ')
    r = w.nation('エチオピア')
    w.set_shares(r, {'ゴンダール': 1.0})
    w.log.append(('C3', 'エチオピア', '州/マサワ', '0.15', '外す（オスマンへ移す）', '', '', 'S',
                  '紅海岸のマサワは 1557 からオスマンの海岸州（ハベシュ州・ナイブ＝アルキコの太守が統治）＝1783 のエチオピア領でない。エチオピアは内陸国になる'))
    el2 = [l.replace('"culture": "アムハラ"', '"culture": "アラブ"').replace('"religion": "エチオピア正教"', '"religion": "イスラム"') for l in el2]   # オスマン領の海岸の町＝現地の文化と宗教
    r = w.nation('オスマン')
    tot = w.total(r)
    w.paste_province(r, el2)
    r = w.nation('オスマン')
    sh, outr = renorm(w, r, {'エルサレム': 300, 'バスラ': 350, 'マサワ': 20}, tot)
    w.set_shares(r, outr)
    for c, nv in [('エルサレム', 300), ('バスラ', 350), ('マサワ', 20)]:
        w.log.append(('C3', 'オスマン', '州share/' + c, sh.get(c, ''), outr[c], '', '', 'S' if c == 'エルサレム' else 'R',
                      {'エルサレム': 'パレスチナ 1800 250,000（WP 1800 一覧・Avakov）＋周辺＝30万', 'バスラ': 'バスラ州 約20〜40万【推】', 'マサワ': 'マサワの町と海岸 約1〜3万【推】'}[c]))
    # オーストリア：ミラノ 120万・トリエステ 35万
    r = w.nation('オーストリア')
    tot = w.total(r)
    sh, outr = renorm(w, r, {'ミラノ': 1200, 'トリエステ': 350}, tot)
    w.set_shares(r, outr)
    for c, src in [('ミラノ', 'ロンバルディア 1780年代 約110〜130万（Romani 1955 系・記憶）【推】'), ('トリエステ', 'トリエステ市と領域＋ゴリツィア・イストリア（墺領）約30〜40万【推・1777-78 の徴兵名簿は原典に未当たり】')]:
        w.log.append(('C3', 'オーストリア', '州share/' + c, sh[c], outr[c], '', '', 'R', src))


if __name__ == '__main__':
    src, dst, arms = sys.argv[1], sys.argv[2], sys.argv[3].split(',')
    w = W(open(src, 'rb').read().decode('utf-8'))
    apply(w, arms)
    open(dst, 'wb').write(w.text().encode('utf-8'))
    if len(sys.argv) > 4:
        with open(sys.argv[4], 'w', encoding='utf-8', newline='\n') as f:
            f.write('腕\t国\t項目\t旧\t新\t下限\t上限\t確からしさ\t根拠\n')
            for row in w.log:
                f.write('\t'.join(str(x) for x in row) + '\n')
    json.loads(w.text())   # 構文の検査
    print('ok', len(w.log))
