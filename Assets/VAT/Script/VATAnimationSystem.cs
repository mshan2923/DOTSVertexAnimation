using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;


// ──────────────────────────────────────────────────────────────────────────────
// 3. ComponentSystem : 프레임 업데이트
// ──────────────────────────────────────────────────────────────────────────────
[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct VATAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (anim, frameProp, animID) in
                 SystemAPI.Query<RefRW<VATAnimationData>, RefRW<VATCurrentFrameProperty>, RefRW<VATAnimIDProperty>>())
        {
            animID.ValueRW.Value = 1;
            ref var data = ref anim.ValueRW;
            float nextFrame = data.CurrentFrame + (deltaTime * data.PlaybackSpeed);

            if (data.IsLooping)
            {
                // fmod를 써서 한 줄로 처리 (나머지 연산)
                data.CurrentFrame = math.fmod(nextFrame, data.FrameCount);
            }
            else
            {
                // 마지막 프레임에 고정
                data.CurrentFrame = math.min(nextFrame, data.FrameCount - 1);
            }
            frameProp.ValueRW.Value = data.CurrentFrame;
        }

        // deltaTime은 루프 밖에서 한 번만 선언해서 넘겨주는 게 좋아
        // foreach (var (anim, frameProp, animID, velocity) in
        //      SystemAPI.Query<RefRW<VATAnimationData>, RefRW<VATCurrentFrameProperty>, RefRW<VATAnimIDProperty>, PhysicsVelocity>())
        // {
        //     ref var data = ref anim.ValueRW;
        //     float3 vel = velocity.Linear;

        //     // 1. 애니메이션 ID 결정 (속도 기준)
        //     // math.select를 쓰면 CPU 분기 예측 실패를 줄일 수 있어 (if-else 대신 사용 가능)
        //     int targetAnim = math.select(1, 0, math.lengthsq(vel) < 0.1f);

        //     // 만약 애니메이션이 바뀌었다면 프레임 초기화 (필요시)
        //     if (animID.ValueRO.Value != targetAnim)
        //     {
        //         animID.ValueRW.Value = targetAnim;
        //         // data.CurrentFrame = 0; // 즉시 전환 시 필요하면 주석 해제
        //     }

        //     // 2. 프레임 진행 및 루프 계산
        //     float nextFrame = data.CurrentFrame + (deltaTime * data.PlaybackSpeed);

        //     if (data.IsLooping)
        //     {
        //         // fmod를 써서 한 줄로 처리 (나머지 연산)
        //         data.CurrentFrame = math.fmod(nextFrame, data.FrameCount);
        //     }
        //     else
        //     {
        //         // 마지막 프레임에 고정
        //         data.CurrentFrame = math.min(nextFrame, data.FrameCount - 1);
        //     }

        //     // 3. 셰이더 프로퍼티 업데이트
        //     frameProp.ValueRW.Value = data.CurrentFrame;
        // }
    }
}
