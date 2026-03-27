#ifndef VAT_SAMPLE_INCLUDED
#define VAT_SAMPLE_INCLUDED

// ──────────────────────────────────────────────────────────────────────────────
// VAT Position 샘플링
//
// [Shader Graph Custom Function Node 설정]
//   Name   : VATSamplePosition
//   Source : VATSample.hlsl
//   Inputs : PosTex(Texture2D), PosSampler(SamplerState),
//            VertexID(Float), VertexCount(Float),
//            CurrentFrame(Float), FrameCount(Float),
//            PosMin(Float), PosRange(Float)
//   Outputs: Position(Vector3)
// ──────────────────────────────────────────────────────────────────────────────
void VATSamplePosition_float(
    UnityTexture2D      PosTex,
    UnitySamplerState   PosSampler,
    float               VertexID,
    float               VertexCount,
    float               CurrentFrame,
    float               FrameCount,
    float               PosMin,
    float               PosRange,
    out float3          Position)
{
    float  vertU      = (VertexID + 0.5) / VertexCount;
    float  frameF     = frac(CurrentFrame);              // 소수점 = 보간 비율
    float  frameA     = floor(CurrentFrame);
    float  frameB     = fmod(frameA + 1.0, FrameCount);  // 루프 처리

    float2 uvA = float2(vertU, (frameA + 0.5) / FrameCount);
    float2 uvB = float2(vertU, (frameB + 0.5) / FrameCount);

    // 현재 프레임 + 다음 프레임 샘플링 후 보간
    float3 posA = SAMPLE_TEXTURE2D_LOD(PosTex.tex, PosSampler.samplerstate, uvA, 0).rgb;
    float3 posB = SAMPLE_TEXTURE2D_LOD(PosTex.tex, PosSampler.samplerstate, uvB, 0).rgb;

    // [0,1] → 실제 절대 위치 복원 + 프레임 보간
    Position = lerp(posA, posB, frameF) * PosRange + PosMin;
}

// ──────────────────────────────────────────────────────────────────────────────
// VAT Normal 샘플링
//
// [Shader Graph Custom Function Node 설정]
//   Name   : VATSampleNormal
//   Source : VATSample.hlsl
//   Inputs : NormTex(Texture2D), NormSampler(SamplerState),
//            VertexID(Float), VertexCount(Float),
//            CurrentFrame(Float), FrameCount(Float)
//   Outputs: Normal(Vector3)
// ──────────────────────────────────────────────────────────────────────────────
void VATSampleNormal_float(
    UnityTexture2D      NormTex,
    UnitySamplerState   NormSampler,
    float               VertexID,
    float               VertexCount,
    float               CurrentFrame,
    float               FrameCount,
    out float3          Normal)
{
    float  vertU  = (VertexID + 0.5) / VertexCount;
    float  frameF = frac(CurrentFrame);
    float  frameA = floor(CurrentFrame);
    float  frameB = fmod(frameA + 1.0, FrameCount);

    float2 uvA = float2(vertU, (frameA + 0.5) / FrameCount);
    float2 uvB = float2(vertU, (frameB + 0.5) / FrameCount);

    float3 normA = SAMPLE_TEXTURE2D_LOD(NormTex.tex, NormSampler.samplerstate, uvA, 0).rgb;
    float3 normB = SAMPLE_TEXTURE2D_LOD(NormTex.tex, NormSampler.samplerstate, uvB, 0).rgb;

    // [0,1] → [-1,1] 복원 + 보간
    Normal = normalize(lerp(normA, normB, frameF) * 2.0 - 1.0);
}

#endif // VAT_SAMPLE_INCLUDED
