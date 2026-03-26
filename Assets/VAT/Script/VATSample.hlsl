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
//   Outputs: PositionOffset(Vector3)
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
    // 텍스처 UV : X = 버텍스, Y = 프레임
    float2 uv = float2(
        (VertexID + 0.5) / VertexCount,
        (CurrentFrame + 0.5) / FrameCount
    );

    // 샘플링 (LOD 고정 : mip 0)
    float3 encoded = SAMPLE_TEXTURE2D_LOD(PosTex.tex, PosSampler.samplerstate, uv, 0).rgb;

    // [0,1] → 실제 절대 위치 복원
    Position = encoded * PosRange + PosMin;
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
    float2 uv = float2(
        (VertexID + 0.5) / VertexCount,
        (CurrentFrame + 0.5) / FrameCount
    );

    float3 encoded = SAMPLE_TEXTURE2D_LOD(NormTex.tex, NormSampler.samplerstate, uv, 0).rgb;

    // [0,1] → [-1,1] 복원
    Normal = encoded * 2.0 - 1.0;
}

#endif // VAT_SAMPLE_INCLUDED
