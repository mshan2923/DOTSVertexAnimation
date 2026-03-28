using Unity.Entities;
using Unity.Rendering;

// ──────────────────────────────────────────────────────────────────────────────
// 1. ECS Component : 애니메이션 상태
// ──────────────────────────────────────────────────────────────────────────────
public struct VATAnimationData : IComponentData
{
    public float CurrentFrame;      // 현재 프레임 (소수점 포함)
    public float FrameCount;        // 총 프레임 수
    public float Fps;               // 재생 FPS
    public float PlaybackSpeed;     // 배속 (1.0 = 정배속)
    public bool IsLooping;         // 루프 여부
}

// ──────────────────────────────────────────────────────────────────────────────
// 2. URPMaterialPropertyComponent : 셰이더 프로퍼티 바인딩
//    - 프로퍼티 이름이 Shader Graph의 [PerRendererData] 프로퍼티와 정확히 일치해야 함
//    - EntitiesGraphicsSystem이 자동으로 GPU Instancing 배치 처리
// ──────────────────────────────────────────────────────────────────────────────
[MaterialProperty("_ElapsedTime")]
public struct VATCurrentFrameProperty : IComponentData
{
    public float Value;
}
[MaterialProperty("_AnimID")]
public struct VATAnimIDProperty : IComponentData
{
    public float Value;
}


