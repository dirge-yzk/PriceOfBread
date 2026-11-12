# 字体の第三者表記

## 同梱している字体：Noto Sans JP（NotoSansJP-VF.ttf）

字体ファイルの名前の表（name テーブル）の記載をそのまま写す。

- 0 番（著作権）：© 2014-2021 Adobe (http://www.adobe.com/), with Reserved Font Name 'Source'.
- 13 番（許諾の文）：This Font Software is licensed under the SIL Open Font License, Version 1.1. This Font Software is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the SIL Open Font License for the specific language, permissions and limitations governing your use of this Font Software.
- 14 番（許諾の場所）：http://scripts.sil.org/OFL

字体の名前（1 番）は Noto Sans JP。この字体は SIL Open Font License 1.1 のもとで同梱・再配布している。字体ファイルそのものは改変していない。

## 同梱していない字体：Times New Roman

英数字（州名・国名・数字）の表示に Times New Roman を名指しているが、これは利用者の Windows に付属するシステムの字体を使うだけで、このリポジトリにも配布物にも含めていない。入っていない環境では次の字体（Noto Sans JP）へ落ちる。

## 同梱している字体ファイルについて

- `NotoSansJP-Regular.ttf`（太さ 400）と `NotoSansJP-Bold.ttf`（太さ 700）は、同じく Noto Sans JP の可変字体（`PriceOfBread.App/FontSource/NotoSansJP-VF.ttf`・太さ 100〜900）から、太さの軸だけを固定して作った静的な字体。作り方は `PriceOfBread.App/FontSource/make_static_instances.py`（標準ライブラリだけ・外部の部品は使っていない）。
- 作った理由：Avalonia は可変字体の太さの軸を選べず、既定の太さ（100＝細字）でしか描けないため。
- 可変字体の原本は配布物には埋め込まない（`FontSource` は埋め込みの対象外）。字体の名前（Noto Sans JP）と上の著作権・許諾の文は原本のまま。
