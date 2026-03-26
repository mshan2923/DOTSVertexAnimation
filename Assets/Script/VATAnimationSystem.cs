using Unity.Entities;
using Unity.Mathematics;
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
    public bool  IsLooping;         // 루프 여부
}

// ──────────────────────────────────────────────────────────────────────────────
// 2. URPMaterialPropertyComponent : 셰이더 프로퍼티 바인딩
//    - 프로퍼티 이름이 Shader Graph의 [PerRendererData] 프로퍼티와 정확히 일치해야 함
//    - EntitiesGraphicsSystem이 자동으로 GPU Instancing 배치 처리
// ──────────────────────────────────────────────────────────────────────────────
[MaterialProperty("_CurrentFrame")]
public struct VATCurrentFrameProperty : IComponentData
{
    public float Value;
}

// ──────────────────────────────────────────────────────────────────────────────
// 3. ComponentSystem : 프레임 업데이트
// ──────────────────────────────────────────────────────────────────────────────
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct VATAnimationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (anim, frameProp) in
            SystemAPI.Query<RefRW<VATAnimationData>, RefRW<VATCurrentFrameProperty>>())
        {
            ref var a = ref anim.ValueRW;

            // 프레임 진행
            a.CurrentFrame += deltaTime * a.Fps * a.PlaybackSpeed;

            // 루프 처리
            if (a.IsLooping)
            {
                if (a.CurrentFrame >= a.FrameCount)
                    a.CurrentFrame -= a.FrameCount;
            }
            else
            {
                a.CurrentFrame = math.min(a.CurrentFrame, a.FrameCount - 1);
            }

            // 셰이더 프로퍼티 동기화
            frameProp.ValueRW.Value = math.floor(a.CurrentFrame);
        }
    }
}
