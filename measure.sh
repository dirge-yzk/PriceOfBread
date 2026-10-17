#!/usr/bin/env bash
# 隔離測定ハーネス（2026-07-28）
# ── なぜ要るか ────────────────────────────────────────────────────────────
# 作業ツリーを複数セッションが同時に編集する運用のため、「ビルド→測定」の間に他人の編集が
# 混入し、決定論の検証が成立しなくなる事故が起きた（2026-07-28・詳細は docs/2026-07-28_測定汚染.md）。
# 症状は「挙動不変のはずの変更で軌道が全面的に動く」で、原因の切り分けに約1時間を要した。
#
# ── 何をするか ────────────────────────────────────────────────────────────
# ソース一式（bin/obj/docs/dist を除く実体 約8MB）を隔離ディレクトリへ複製し、**そこで**
# ビルドと測定を行う。以後どれだけ作業ツリーが編集されても測定結果は汚染されない。
# 併せてソースの SHA-256 マニフェストを出し、「どのソースを測ったか」を後から検証可能にする。
#
# ── 使い方 ────────────────────────────────────────────────────────────────
#   bash measure.sh <出力パス> [Runnerの引数...]
#   例: bash measure.sh /tmp/batch.txt selfplay-batch 10
#       RUN_TESTS=1 bash measure.sh /tmp/batch.txt selfplay-batch 10   ← テストも隔離側で回す（作業ツリーをロックしない）
#       bash measure.sh /tmp/chron.txt chronicle 1
#       MEASURE_CACHE=1 bash measure.sh /tmp/b10.txt selfplay-batch 10   ← 同じ条件の走りが前にあれば回さずにその結果を写す（下の MEASURE_CACHE）
#       SEEDS=5 bash measure.sh /tmp/b5.txt selfplay-batch     ← 探索の腕（5種）。採否を決める1本は種の数を書かない＝既定の10種
# 出力パスの隣に <出力パス>.manifest（ソースのハッシュ一覧と要約）も書く。
# 2つの測定が同じソースかは manifest 冒頭の SOURCE-HASH 行を比べれば分かる。

set -u
# ★2026-09-03（キューD-38）：文字化けの自衛。⚠測定結果の日本語が化けると「数字は合っているのに読めない」事故になる。
export PYTHONUTF8=1                 # このハーネスから呼ぶ補助スクリプト（将来含む）が黙って化けないように
export DOTNET_CLI_UI_LANGUAGE=ja    # dotnet の診断メッセージを固定（環境差でログの語が変わらない）
SRC_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="${1:?出力パスを指定してください}"; shift
# ★2026-09-30（高速化の段 S4）：探索の腕は5種・採否を決める1本は10種。selfplay-batch の種の数は Runner の引数か SEEDS（既定10）で渡す
#   （旧＝種の数を書かないと Runner の既定の30種）。⚠ 引数と SEEDS が食い違えば止める＝10種と5種の帯を取り違えない。
#   種の数は報告の名乗り行（先頭行の seeds 1..N・史実忠実度の「完走N試合」）とマニフェストの RUN 行に出る。
RUN_NOTE=""
if [ "${1:-}" = "selfplay-batch" ]; then
  if [ -z "${2:-}" ]; then set -- selfplay-batch "${SEEDS:-10}"
  elif [ -n "${SEEDS:-}" ] && [ "$2" != "$SEEDS" ]; then echo "!! 種の数が食い違う（引数 $2・SEEDS=$SEEDS）＝測定を中止"; exit 1
  fi
  case "$2" in 10) RUN_NOTE="（10種＝採否）" ;; 5) RUN_NOTE="（5種＝探索）" ;; *) RUN_NOTE="（${2}種）" ;; esac
fi
# ★(248) 標準の計器を出した走行はマニフェストの RUN 行に名乗る（帯の本体は計器の有無で1バイトも変わらない＝本体からは区別できない）。
if [ -n "${POB_INSTRUMENTS:-}" ]; then RUN_NOTE="$RUN_NOTE（計器 POB_INSTRUMENTS=$POB_INSTRUMENTS${POB_INSTRUMENTS_NATIONS:+ NATIONS=$POB_INSTRUMENTS_NATIONS}）"; fi
# dotnet と隔離先は環境ごとに違う（機体を跨ぐと旧既定は存在しない）。明示指定＞既定パス＞PATH の順で解決する。
DOTNET="${DOTNET:-}"
if [ -z "$DOTNET" ] || [ ! -x "$DOTNET" ]; then
  for cand in /c/Users/takuto.motoki/.dotnet/dotnet.exe "$HOME/.dotnet/dotnet.exe" "$(command -v dotnet || true)"; do
    [ -n "$cand" ] && [ -x "$cand" ] && DOTNET="$cand" && break
  done
fi
[ -n "$DOTNET" ] && [ -x "$DOTNET" ] || { echo "!! dotnet が見つからない（DOTNET=... で明示指定してください）"; exit 1; }
# ★2026-10-18（開発の高速化・結果は回したときと同じ）：MEASURE_CACHE=1＝selfplay-batch の結果を使い回す。シミュは決定論的＝同じ入力なら同じ出力。
#   鍵＝作業ツリーの SOURCE-HASH（マニフェストと同じ数え方）＋SimCore と Runner のフォルダの全ファイル（埋め込みの landmask.bin など＝SOURCE-HASH の外）
#   ＋Runner の引数＋POB_ で始まる環境変数の全て（出力先のパスの変数は有無だけ）＋走る前の忠実度の基線ファイルの中身（報告の〈前回比〉の入力）。
#   当たり＝報告・計算量（.cost・先頭に写しの名乗りの行）・走った後の基線・5種の報告（POB_FIRST5_OUT）・計器の生ファイル（POB_INSTRUMENTS・ハードリンク）を写して終わる。
#   外れ＝ふだんどおり回し、終了コードが 0/1/2 のときだけ写しを置く。置き場＝MEASURE_CACHE_DIR（既定 D:/PoB_work/measure_cache）。
#   使わない＝RUN_TESTS=1（テストは回す）・POB_TIME（時間を印字＝揺れる）・selfplay-batch 以外。⚠置き場は自分で片付ける（計器つきの1本は 4GB 前後・作業フォルダとハードリンクで共有）。
CACHE_KEY=""
CACHE_DIR="${MEASURE_CACHE_DIR:-/d/PoB_work/measure_cache}"
src_manifest() {   # 作業ツリーから SOURCE-HASH の元を作る（写した後と同じ値＝除く名前を prune する・SNAP_REUSE と共用）
  ( cd "$SRC_DIR" && find . \( -name bin -o -name obj -o -name docs -o -name dist -o -name .vs -o -name .git -o -name measurements -o -name uishot \) -prune \
      -o -type f \( -name '*.cs' -o -name '*.json' -o -name '*.axaml' -o -name '*.csproj' -o -name '*.slnx' \) -print | sort | xargs sha256sum )
}
if [ "${MEASURE_CACHE:-0}" = "1" ] && [ "${1:-}" = "selfplay-batch" ] && [ "${RUN_TESTS:-0}" != "1" ] && [ -z "${POB_TIME:-}" ]; then
  CHASH=$(src_manifest | sha256sum | cut -c1-16)
  CINPUTS=$( cd "$SRC_DIR" && find PriceOfBread.SimCore PriceOfBread.Runner \( -name bin -o -name obj \) -prune -o -type f -print | sort | xargs sha256sum | sha256sum | cut -c1-16 )
  CBASE="${POB_FIDELITY_BASELINE:-$SRC_DIR/.pob-fidelity-baseline.tsv}"
  CACHE_KEY=$( { echo "SOURCE-HASH $CHASH"; echo "INPUTS $CINPUTS"; echo "ARGS $*"
                 env | grep '^POB_' | grep -v -E '^POB_(INSTRUMENTS|FIRST5_OUT|COST_OUT|FIDELITY_BASELINE|SOURCE_HASH)=' | sort
                 [ -n "${POB_INSTRUMENTS:-}" ] && echo "INSTRUMENTS on"; [ -n "${POB_FIRST5_OUT:-}" ] && echo "FIRST5 on"
                 if [ -f "$CBASE" ]; then echo "BASELINE $(sha256sum < "$CBASE" | cut -c1-16)"; else echo "BASELINE none"; fi; } | sha256sum | cut -c1-24 )
  CE="$CACHE_DIR/$CACHE_KEY"
  if [ -f "$CE/rc" ] && [ -f "$CE/out" ]; then
    echo "[1-4/4] MEASURE_CACHE：同じ条件の走りの写しを使う（鍵 $CACHE_KEY・SOURCE-HASH=$CHASH・元 $(cat "$CE/origin" 2>/dev/null)）"
    src_manifest > "$OUT.manifest.tmp"
    { echo "SOURCE-HASH: $CHASH"; echo "FILES: $(wc -l < "$OUT.manifest.tmp")"; echo "RUN: $*$RUN_NOTE（MEASURE_CACHE の写し・鍵 $CACHE_KEY）"; echo "---"; cat "$OUT.manifest.tmp"; } > "$OUT.manifest"
    rm -f "$OUT.manifest.tmp"
    cp "$CE/out" "$OUT"
    if [ -f "$CE/cost" ]; then { echo "# ★MEASURE_CACHE の写し（鍵 $CACHE_KEY）：件数と推定は同じ・実測の CPU 秒と壁は元の走りの値"; cat "$CE/cost"; } > "$OUT.cost"; fi
    [ -f "$CE/baseline_after" ] && cp "$CE/baseline_after" "$CBASE"
    if [ -n "${POB_FIRST5_OUT:-}" ] && [ -f "$CE/first5" ]; then
      cp "$CE/first5" "$POB_FIRST5_OUT"
      [ -f "$CE/first5.cost" ] && { echo "# ★MEASURE_CACHE の写し（鍵 $CACHE_KEY）：件数と推定は同じ・実測の CPU 秒と壁は元の走りの値"; cat "$CE/first5.cost"; } > "$POB_FIRST5_OUT.cost"
      [ -f "$CE/first5.baseline" ] && cp "$CE/first5.baseline" "$POB_FIRST5_OUT.fidelity-baseline.tsv"
      sed 's/^RUN: .*/RUN: selfplay-batch 5（5種＝探索）（10種の走りの先頭5試合＝POB_FIRST5_OUT・MEASURE_CACHE の写し）/' "$OUT.manifest" > "$POB_FIRST5_OUT.manifest"
    fi
    if [ -n "${POB_INSTRUMENTS:-}" ] && [ -d "$CE/ins" ]; then
      mkdir -p "$POB_INSTRUMENTS"
      for f in "$CE/ins"/*; do b=$(basename "$f"); rm -f "$POB_INSTRUMENTS/$b"; ln "$f" "$POB_INSTRUMENTS/$b" 2>/dev/null || cp "$f" "$POB_INSTRUMENTS/$b"; done
    fi
    RC=$(cat "$CE/rc")
    echo "      終了コード=$RC　テスト=0　出力=$OUT$RUN_NOTE　マニフェスト=$OUT.manifest（SOURCE-HASH=$CHASH・MEASURE_CACHE の写し）"
    [ -f "$OUT.cost" ] && echo "      計算量＝$OUT.cost　$(grep -E '^判定:' "$OUT.cost")"
    [ -f "$OUT.cost" ] && grep -E '^揺れ:' "$OUT.cost" | sed 's/^/      /'
    exit "$RC"
  fi
  # 外れ：計器の出力先に残った古いハードリンク（写しと同じ実体）を外す＝Runner の上書きが写しの中身を壊さない
  [ -n "${POB_INSTRUMENTS:-}" ] && [ -d "$POB_INSTRUMENTS" ] && find "$POB_INSTRUMENTS" -maxdepth 1 -type f -links +1 -delete
fi
# ★2026-09-26（fis-23）：既定の隔離先を**この実行だけのもの**にする（旧＝固定の pob-measure-snap を全実行で共有＝SNAP_DIR を渡し忘れた
#   2本目の measure.sh が1本目の走行中のスナップショットを rm -rf していた）。既定を使ったときだけ終わりに片付ける。
SNAP_OWNED=0
if [ -z "${SNAP_DIR:-}" ]; then SNAP_OWNED=1; fi
SNAP="${SNAP_DIR:-${TMPDIR:-/c/Users/$USERNAME/AppData/Local/Temp}/claude/pob-measure-snap-$$}"

# ★2026-10-17（開発の高速化）：SNAP_REUSE=1 と SNAP_DIR＝同じ木の写しとビルドを使い回す。作業ツリーから直接求めた SOURCE-HASH（写した後と同じ値になる
#   ＝除く名前を prune する）が、その写しのビルドの印 .built_hash と同じで、Runner の dll があれば、複製とビルドを飛ばす（隔離は保つ＝測るのは写しの dll）。
#   MEASURE_BUILD_ONLY=1＝複製とビルドだけして終わる（同じ木で成績表を2本同時に回すとき：先にこれで1回ビルドし、2本を SNAP_REUSE=1 で起こす）。
if [ "${SNAP_REUSE:-0}" = "1" ] && [ "$SNAP_OWNED" = "0" ] && [ "${RUN_TESTS:-0}" != "1" ] && [ -f "$SNAP/.built_hash" ] \
   && [ -f "$SNAP/PriceOfBread.Runner/bin/Release/net10.0/PriceOfBread.Runner.dll" ]; then
  src_manifest > "$OUT.manifest.tmp" || exit 1
  HASH=$(sha256sum < "$OUT.manifest.tmp" | cut -c1-16)
  if [ "$HASH" = "$(cat "$SNAP/.built_hash")" ]; then
    { echo "SOURCE-HASH: $HASH"; echo "FILES: $(wc -l < "$OUT.manifest.tmp")"; echo "RUN: $*$RUN_NOTE"; echo "---"; cat "$OUT.manifest.tmp"; } > "$OUT.manifest"
    rm -f "$OUT.manifest.tmp"
    echo "[1-3/4] 同じ木の写しとビルドを使い回す（SNAP_REUSE・SOURCE-HASH=$HASH）"
    REUSED=1
  else
    rm -f "$OUT.manifest.tmp"; echo "      SNAP_REUSE：写しの木と違う（作業ツリー $HASH・写し $(cat "$SNAP/.built_hash")）＝複製し直す"
  fi
fi
if [ "${REUSED:-0}" != "1" ]; then
echo "[1/4] ソースを隔離ディレクトリへ複製（bin/obj/docs/dist/measurements/uishot を除く）"
rm -rf "$SNAP" && mkdir -p "$SNAP" || exit 1
# ★2026-09-29（高速化の段 S1）：measurements（帯と計器の表 46MB）と uishot（撮影 18MB）も外す＝ビルドにも測定にも読まれない
#   （マニフェストが数える .cs/.json/.axaml/.csproj/.slnx はどちらにも無い＝SOURCE-HASH は変わらない）。
tar -cf - -C "$SRC_DIR" --exclude=bin --exclude=obj --exclude=docs --exclude=dist --exclude='.vs' --exclude='.git' --exclude=measurements --exclude=uishot . \
  | tar -xf - -C "$SNAP" || exit 1

echo "[2/4] ソースのマニフェストを作成"
( cd "$SNAP" && find . -type f \( -name '*.cs' -o -name '*.json' -o -name '*.axaml' -o -name '*.csproj' -o -name '*.slnx' \) \
    -not -path './obj/*' -not -path './bin/*' | sort | xargs sha256sum ) > "$OUT.manifest.tmp" || exit 1
HASH=$(sha256sum < "$OUT.manifest.tmp" | cut -c1-16)
{ echo "SOURCE-HASH: $HASH"; echo "FILES: $(wc -l < "$OUT.manifest.tmp")"; echo "RUN: $*$RUN_NOTE"; echo "---"; cat "$OUT.manifest.tmp"; } > "$OUT.manifest"
rm -f "$OUT.manifest.tmp"
echo "      SOURCE-HASH=$HASH"

echo "[3/4] 隔離ディレクトリでビルド"
# ★2026-09-29（高速化の段 S1）：テストを回さない測定は Runner（と SimCore）だけをビルドする＝App（Avalonia）と Tests を外す（測る Runner は同じ）。
BUILD_TARGET="$SNAP/PriceOfBread.slnx"
[ "${RUN_TESTS:-0}" = "1" ] || BUILD_TARGET="$SNAP/PriceOfBread.Runner/PriceOfBread.Runner.csproj"
"$DOTNET" build "$BUILD_TARGET" -c Release 2>&1 | grep -E "error|エラー|警告|成功" | head -5
[ "${PIPESTATUS[0]}" = "0" ] || { echo "!! ビルド失敗＝測定を中止"; exit 1; }
[ "${RUN_TESTS:-0}" = "1" ] || echo "$HASH" > "$SNAP/.built_hash"   # SNAP_REUSE の印（Runner だけのビルドのとき）
fi
if [ "${MEASURE_BUILD_ONLY:-0}" = "1" ]; then echo "      MEASURE_BUILD_ONLY：複製とビルドだけで終える（$SNAP・SOURCE-HASH=$HASH）"; exit 0; fi

# テストも隔離側で回す（--test を付けたとき）。作業ツリーで dotnet test を叩くと testhost が
# PriceOfBread.SimCore.dll をロックし、並行してビルドできなくなる（2026-07-28 に実際に踏んだ）。
TEST_RC=0
if [ "${RUN_TESTS:-0}" = "1" ]; then
  echo "[3.5] 隔離ディレクトリでテスト"
  # ★2026-08-01：**テストの終了コードを捨てない**。旧は `| tail -1` でパイプ末尾の RC になり、
  #   テストが赤でも measure.sh は 0 を返していた＝Runner のゲート0が「全ゲート緑」と印字した裏でテストが落ちていた（実際に踏んだ）。
  # ★2026-09-27（作者裁定 2026-09-26）：全長の保存則の番人（約7分）は普段外す＝印 Category=FullLength。保存則に触る回は別に
  #   `dotnet test ... --filter "Category=FullLength"` で回す（README §1-2）。
  "$DOTNET" test "$SNAP/PriceOfBread.slnx" -c Release --no-build --filter "Category!=FullLength" > "$OUT.tests" 2>&1
  TEST_RC=$?
  tail -1 "$OUT.tests"
  [ "$TEST_RC" = "0" ] || echo "  ★赤：テストが失敗している（詳細＝$OUT.tests）"
fi

echo "[4/4] 測定: $*$RUN_NOTE"
# ★忠実度の基線（裁定①の条件2＝前回値との差分を必ず併記）は**作業ツリー直下**に置く。
# 隔離スナップショットは毎回作り直されるので、そこに置くと「前回」が常に存在しない。
export POB_FIDELITY_BASELINE="${POB_FIDELITY_BASELINE:-$SRC_DIR/.pob-fidelity-baseline.tsv}"
# ★2026-09-26（fis-23）：Runner は基線の1行目に走行条件（シード・長さ・係数の上書き・このソースの SOURCE-HASH）を書き、次の走行で突き合わせる
#   ＝長さや上書きの違う腕の値と黙って差を取らない（条件が違えば「前回値なし（条件違い）」）。
export POB_SOURCE_HASH="$HASH"
# ★2026-09-30（作者裁定「計算量の予算」）：selfplay-batch は計算量の測り（1試合あたりの CPU 秒・全長の件数の1試合の平均・推定の計算量・覆う割合・
#   予算の上限と目標・「予算の内／超過」の1行）を帯の報告とは**別のファイル** <出力>.cost へ書く（報告は恒等の検証で全行バイト一致を見る＝揺れる値を入れない）。
if [ "${1:-}" = "selfplay-batch" ]; then export POB_COST_OUT="$OUT.cost"; rm -f "$POB_COST_OUT"; fi
"$DOTNET" "$SNAP/PriceOfBread.Runner/bin/Release/net10.0/PriceOfBread.Runner.dll" "$@" > "$OUT" 2>&1
RC=$?
# ★2026-10-17（開発の高速化）：POB_FIRST5_OUT＝同じ10種の走りの先頭5試合（seeds 1..5）の報告（Runner が書く・selfplay-batch 5 と同じ報告）。マニフェストも同じ木の物を写す。
if [ -n "${POB_FIRST5_OUT:-}" ] && [ -f "$POB_FIRST5_OUT" ]; then
  sed 's/^RUN: .*/RUN: selfplay-batch 5（5種＝探索）（10種の走りの先頭5試合＝POB_FIRST5_OUT）/' "$OUT.manifest" > "$POB_FIRST5_OUT.manifest"
  echo "      5種＝$POB_FIRST5_OUT（10種の走りから切り出し・マニフェスト=$POB_FIRST5_OUT.manifest）"
fi
# MEASURE_CACHE の外れ：終了コードが 0/1/2（走り切った）なら写しを置く（一時の名前で作ってから名前を替える＝途中の写しを当たりにしない）
if [ -n "$CACHE_KEY" ] && { [ "$RC" = "0" ] || [ "$RC" = "1" ] || [ "$RC" = "2" ]; }; then
  if [ "$HASH" != "$CHASH" ]; then echo "      MEASURE_CACHE：測った木（$HASH）が鍵を作った時の作業ツリー（$CHASH）と違う＝写しを置かない"
  else
    CT="$CACHE_DIR/.tmp-$CACHE_KEY-$$"; rm -rf "$CT"; mkdir -p "$CT"
    cp "$OUT" "$CT/out"; [ -f "$OUT.cost" ] && cp "$OUT.cost" "$CT/cost"
    [ -f "$POB_FIDELITY_BASELINE" ] && cp "$POB_FIDELITY_BASELINE" "$CT/baseline_after"
    if [ -n "${POB_FIRST5_OUT:-}" ] && [ -f "$POB_FIRST5_OUT" ]; then
      cp "$POB_FIRST5_OUT" "$CT/first5"; [ -f "$POB_FIRST5_OUT.cost" ] && cp "$POB_FIRST5_OUT.cost" "$CT/first5.cost"
      [ -f "$POB_FIRST5_OUT.fidelity-baseline.tsv" ] && cp "$POB_FIRST5_OUT.fidelity-baseline.tsv" "$CT/first5.baseline"
    fi
    if [ -n "${POB_INSTRUMENTS:-}" ] && [ -d "$POB_INSTRUMENTS" ]; then
      mkdir -p "$CT/ins"   # この走りが書いた物だけ（マニフェストより新しい）＝出力先に残った前の走りのファイルを混ぜない
      find "$POB_INSTRUMENTS" -maxdepth 1 -type f -newer "$OUT.manifest" -print0 | while IFS= read -r -d '' f; do ln "$f" "$CT/ins/$(basename "$f")" 2>/dev/null || cp "$f" "$CT/ins/$(basename "$f")"; done
    fi
    echo "$OUT（$(date '+%Y-%m-%d %H:%M')）" > "$CT/origin"; echo "$RC" > "$CT/rc"
    if [ -d "$CACHE_DIR/$CACHE_KEY" ]; then rm -rf "$CT"; else mv "$CT" "$CACHE_DIR/$CACHE_KEY" && echo "      MEASURE_CACHE：写しを置いた（鍵 $CACHE_KEY）"; fi
  fi
fi
[ "$SNAP_OWNED" = "1" ] && rm -rf "$SNAP"   # 既定の隔離先だけ片付ける（SNAP_DIR を渡した腕は呼び手のもの＝残す）
echo "      終了コード=$RC　テスト=$TEST_RC　出力=$OUT$RUN_NOTE　マニフェスト=$OUT.manifest（SOURCE-HASH=$HASH）"
[ -n "${POB_COST_OUT:-}" ] && [ -f "$POB_COST_OUT" ] && echo "      計算量＝$POB_COST_OUT　$(grep -E '^判定:' "$POB_COST_OUT")"
# ★2026-09-30（止める規則＝作者裁定「計算量の予算」の6・README §1-2）：種ごとの exact／near の平均と標準誤差（<出力>.cost の「揺れ:」の1行）。
[ -n "${POB_COST_OUT:-}" ] && [ -f "$POB_COST_OUT" ] && grep -E '^揺れ:' "$POB_COST_OUT" | sed 's/^/      /'
# ★★終了コードは3値（裁定①・2026-08-01）。裁定順序「破綻しない ＞ 正しい挙動 ＞ バランス」を大小へ写す。
#   0 ＝ 硬性ゲート全緑・テスト緑・6次元も到達
#   1 ＝ 硬性ゲートが赤 **または テストが赤** ← 差し戻しの理由になる唯一の値
#   2 ＝ 硬性ゲート緑・テスト緑だが6次元が未達 ← 記録して進む（差し戻しの理由にならない）
# ⚠テストの赤は必ず1へ畳む（6次元の未達より重い＝「壊れていない」の側の判定だから）。
if [ "$TEST_RC" != "0" ] || [ "$RC" = "1" ]; then exit 1; fi
exit "$RC"
