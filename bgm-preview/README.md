# パンの帳簿 / The Bread Ledger

『パンの値段』の1783年開幕を想定した通常プレイ用BGM。ニ短調、3/4拍子、88 BPM、52小節。クラリネットの主題をヴァイオリンが引き継ぎ、交易の場面ではフルートがヘ長調に転じる。低弦とハープが残る終盤から、冒頭へ戻る。

| ファイル | 用途 |
| --- | --- |
| [WAV](the-bread-ledger-1783-loop.wav) | 試聴・編集用マスター。48 kHz / 24 bit / ステレオ |
| [Ogg Vorbis](the-bread-ledger-1783-loop.ogg) | ゲーム組み込み用。全ファイルを先頭から末尾までループ |
| [MIDI](the-bread-ledger-1783.mid) | 7パートの演奏データ。DAWで音源を割り当てて編集できる |

WAVとOggは約106.364秒。音声の先頭と末尾で残響がつながるように書き出している。

## 制作環境の再現

リポジトリのルートで Python 3.12 を使用する。音源とライブラリの取得にはネット接続が必要。

```powershell
python -m pip install --target bgm-preview/.toolchain -r bgm-preview/requirements.txt
python bgm-preview/fetch_samples.py
$env:PYTHONPATH = 'bgm-preview/.toolchain'
python bgm-preview/render.py
```

`score.py` に和声、旋律、各パートの編曲があり、`render.py` で演奏データとミックスを書き出す。

## 使用音源

木管、弦、ハープの録音素材は Versilian Studios の VSCO 2 Community Edition（CC0 1.0）。`fetch_samples.py` は固定コミットから使用する音色のサンプルを取得し、Gitのハッシュを照合する。[音源ライブラリとライセンス](https://github.com/sgossner/VSCO-2-CE/tree/440300901dfe9275fd84e0b7763af1f8443ae62e)
