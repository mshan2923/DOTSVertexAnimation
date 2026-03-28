#ifndef VAT_ATLAS_SAMPLE_INCLUDED
#define VAT_ATLAS_SAMPLE_INCLUDED

// ──────────────────────────────────────────────────────────────────────────────
// VAT 다중 애니메이션(Atlas) 샘플링
// 
// [Shader Graph Custom Function Node 입력값]
// - PosTex (Texture2D) : 통합된 전체 VAT
// - MetaTex (Texture2D) : 애니메이션 정보가 담긴 1D 텍스처 (R: StartFrame, G: FrameCount, B: FPS)
// - VertexID (Float), VertexCount (Float)
// - AnimID (Float) : 현재 재생할 애니메이션 인덱스 (0, 1, 2...)
// - AnimCount (Float) : 아틀라스에 포함된 총 애니메이션 개수
// - ElapsedTime (Float) : 이 애니메이션이 시작된 후 흐른 시간 (CurrentTime - StateStartTime)
// - TotalAtlasFrames (Float) : 통합 VAT의 전체 Y축 해상도(총 프레임 수)
// - PosMin (Float), PosRange (Float)
// ──────────────────────────────────────────────────────────────────────────────
void VATSamplePositionAtlas_float(
    UnityTexture2D      PosTex,
    UnitySamplerState   Sampler_PosTex,
    UnityTexture2D      MetaTex,
    UnitySamplerState   Sampler_MetaTex,
    float               VertexID,
    float               VertexCount,
    float               AnimID,
    float               AnimCount,
    float               ElapsedTime,
    float               TotalAtlasFrames,
    float               PosMin,
    float               PosRange,
    out float3          Position)
{
    // 1. MetaTex에서 현재 AnimID의 메타데이터 읽기
    // UV 픽셀 중앙을 정확히 샘플링하기 위해 +0.5 처리
    float metaU = (AnimID + 0.5) / AnimCount; 
    float4 meta = SAMPLE_TEXTURE2D_LOD(MetaTex.tex, Sampler_MetaTex.samplerstate, float2(metaU, 0.5), 0);
    
    float startFrame = meta.r;
    float frameCount = meta.g;
    float fps = meta.b;

    // 2. 경과 시간을 바탕으로 현재 애니메이션 내의 로컬 프레임 계산
    float localFrame = ElapsedTime * fps;
    localFrame = fmod(localFrame, frameCount); // 루프 애니메이션 기준

    float frameF = frac(localFrame);
    float frameA = floor(localFrame);
    float frameB = fmod(frameA + 1.0, frameCount);

    // 3. 아틀라스 전체 기준의 절대 프레임(Y축) 계산
    float absFrameA = startFrame + frameA;
    float absFrameB = startFrame + frameB;

    float vertU = (VertexID + 0.5) / VertexCount;
    float2 uvA = float2(vertU, (absFrameA + 0.5) / TotalAtlasFrames);
    float2 uvB = float2(vertU, (absFrameB + 0.5) / TotalAtlasFrames);

    // 4. VAT 샘플링
    float3 posA = SAMPLE_TEXTURE2D_LOD(PosTex.tex, Sampler_PosTex.samplerstate, uvA, 0).rgb;
    float3 posB = SAMPLE_TEXTURE2D_LOD(PosTex.tex, Sampler_PosTex.samplerstate, uvB, 0).rgb;

    // [0,1] → 실제 위치 복원 + 보간
    Position = lerp(posA, posB, frameF) * PosRange + PosMin;
}

// ──────────────────────────────────────────────────────────────────────────────
// VAT 다중 애니메이션(Atlas) 노멀 샘플링
//
// [Shader Graph Custom Function Node 입력값]
// - NormTex (Texture2D), NormSampler (SamplerState)
// - MetaTex (Texture2D), Sampler_MetaTex (SamplerState)
// - VertexID (Float), VertexCount (Float)
// - AnimID (Float), AnimCount (Float)
// - ElapsedTime (Float)
// - TotalAtlasFrames (Float)
// ──────────────────────────────────────────────────────────────────────────────
void VATSampleNormalAtlas_float(
    UnityTexture2D      NormTex,
    UnitySamplerState   NormSampler,
    UnityTexture2D      MetaTex,
    UnitySamplerState   Sampler_MetaTex,
    float               VertexID,
    float               VertexCount,
    float               AnimID,
    float               AnimCount,
    float               ElapsedTime,
    float               TotalAtlasFrames,
    out float3          Normal)
{
    // 1. MetaTex 읽기 (포지션과 동일한 로직)
    float metaU = (AnimID + 0.5) / AnimCount;
    float4 meta = SAMPLE_TEXTURE2D_LOD(MetaTex.tex, Sampler_MetaTex.samplerstate, float2(metaU, 0.5), 0);
    
    float startFrame = meta.r;
    float frameCount = meta.g;
    float fps = meta.b;

    // 2. 로컬 프레임 및 보간 비율 계산
    float localFrame = ElapsedTime * fps;
    localFrame = fmod(localFrame, frameCount);

    float frameF = frac(localFrame);
    float frameA = floor(localFrame);
    float frameB = fmod(frameA + 1.0, frameCount);

    // 3. 아틀라스 전체 기준의 절대 프레임(Y축) 계산
    float absFrameA = startFrame + frameA;
    float absFrameB = startFrame + frameB;

    float vertU = (VertexID + 0.5) / VertexCount;
    float2 uvA = float2(vertU, (absFrameA + 0.5) / TotalAtlasFrames);
    float2 uvB = float2(vertU, (absFrameB + 0.5) / TotalAtlasFrames);

    // 4. 노멀 샘플링
    float3 normA = SAMPLE_TEXTURE2D_LOD(NormTex.tex, NormSampler.samplerstate, uvA, 0).rgb;
    float3 normB = SAMPLE_TEXTURE2D_LOD(NormTex.tex, NormSampler.samplerstate, uvB, 0).rgb;

    // 5. [0,1] → [-1,1] 복원 + 프레임 간 보간 + 정규화
    Normal = normalize(lerp(normA, normB, frameF) * 2.0 - 1.0);
}
#endif // VAT_ATLAS_SAMPLE_INCLUDED