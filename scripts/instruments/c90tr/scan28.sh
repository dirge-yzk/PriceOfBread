#!/usr/bin/env bash
# (982) ㉘ ソースを走査する番人の束（既定のフィルタ）。★(981)：GoodIdGuardTests を束に足した（財の名前の走査）。
# 使い方：bash scan28.sh <木> <artifacts のフォルダ>   （1 分を超える＝bg.sh 経由で起動する）
F="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~GoodIdGuardTests"
exec dotnet test "${1:?木}/PriceOfBread.Tests" -c Release --artifacts-path "${2:?artifacts}" --filter "$F"
